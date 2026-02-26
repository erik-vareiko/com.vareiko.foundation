using System.Collections.Generic;
using System.Reflection;
using NUnit.Framework;
using UnityEngine;
using Vareiko.Foundation.Analytics;
using Vareiko.Foundation.Consent;
using Vareiko.Foundation.Tests.TestDoubles;

namespace Vareiko.Foundation.Tests.Analytics
{
    public sealed class AnalyticsServiceTests
    {
        [Test]
        public void TrackEvent_WhenConsentRequiredAndMissing_DoesNotBuffer()
        {
            AnalyticsConfig config = CreateConfig(requireConsent: true, maxBufferedEvents: 128);
            try
            {
                FakeConsentService consent = new FakeConsentService(isLoaded: true, isCollected: false, analyticsConsent: false);
                FakeTimeProvider time = new FakeTimeProvider { Time = 10f };
                AnalyticsService service = new AnalyticsService(time, consent, config, null);

                service.TrackEvent("session_start");
                Assert.That(GetBufferedCount(service), Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void TrackEvent_WhenConsentGranted_BuffersEventWithMergedProperties()
        {
            AnalyticsConfig config = CreateConfig(requireConsent: true, maxBufferedEvents: 128);
            try
            {
                FakeConsentService consent = new FakeConsentService(isLoaded: true, isCollected: true, analyticsConsent: true);
                FakeTimeProvider time = new FakeTimeProvider { Time = 25f };
                AnalyticsService service = new AnalyticsService(time, consent, config, null);

                service.SetUserId("player-1");
                service.SetSessionProperty("build", "dev");
                service.TrackEvent("level_start", new Dictionary<string, string>
                {
                    ["level"] = "1",
                    ["build"] = "override"
                });

                List<AnalyticsEventModel> buffer = GetBuffer(service);
                Assert.That(buffer.Count, Is.EqualTo(1));
                Assert.That(buffer[0].EventName, Is.EqualTo("level_start"));
                Assert.That(buffer[0].UserId, Is.EqualTo("player-1"));
                Assert.That(buffer[0].TimeFromStartup, Is.EqualTo(25f).Within(0.0001f));
                Assert.That(buffer[0].Properties["build"], Is.EqualTo("override"));
                Assert.That(buffer[0].Properties["level"], Is.EqualTo("1"));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void TrackEvent_RespectsMaxBufferCapacity()
        {
            AnalyticsConfig config = CreateConfig(requireConsent: false, maxBufferedEvents: 32);
            try
            {
                FakeTimeProvider time = new FakeTimeProvider { Time = 1f };
                AnalyticsService service = new AnalyticsService(time, null, config, null);

                for (int i = 0; i < 40; i++)
                {
                    service.TrackEvent("event_" + i);
                }

                List<AnalyticsEventModel> buffer = GetBuffer(service);
                Assert.That(buffer.Count, Is.EqualTo(32));
                Assert.That(buffer[0].EventName, Is.EqualTo("event_8"));
                Assert.That(buffer[31].EventName, Is.EqualTo("event_39"));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        private static AnalyticsConfig CreateConfig(bool requireConsent, int maxBufferedEvents)
        {
            AnalyticsConfig config = ScriptableObject.CreateInstance<AnalyticsConfig>();
            ReflectionTestUtil.SetPrivateField(config, "_enabled", true);
            ReflectionTestUtil.SetPrivateField(config, "_requireConsent", requireConsent);
            ReflectionTestUtil.SetPrivateField(config, "_maxBufferedEvents", maxBufferedEvents);
            return config;
        }

        private static int GetBufferedCount(AnalyticsService service)
        {
            return GetBuffer(service).Count;
        }

        private static List<AnalyticsEventModel> GetBuffer(AnalyticsService service)
        {
            FieldInfo field = typeof(AnalyticsService).GetField("_buffer", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.That(field, Is.Not.Null);
            List<AnalyticsEventModel> buffer = field.GetValue(service) as List<AnalyticsEventModel>;
            Assert.That(buffer, Is.Not.Null);
            return buffer;
        }

        private sealed class FakeConsentService : IConsentService
        {
            private readonly bool _analyticsConsent;
            public bool IsLoaded { get; }
            public bool IsConsentCollected { get; }

            public FakeConsentService(bool isLoaded, bool isCollected, bool analyticsConsent)
            {
                IsLoaded = isLoaded;
                IsConsentCollected = isCollected;
                _analyticsConsent = analyticsConsent;
            }

            public bool HasConsent(ConsentScope scope)
            {
                if (scope != ConsentScope.Analytics)
                {
                    return false;
                }

                return _analyticsConsent;
            }

            public Cysharp.Threading.Tasks.UniTask LoadAsync(System.Threading.CancellationToken cancellationToken = default)
            {
                return Cysharp.Threading.Tasks.UniTask.CompletedTask;
            }

            public Cysharp.Threading.Tasks.UniTask SaveAsync(System.Threading.CancellationToken cancellationToken = default)
            {
                return Cysharp.Threading.Tasks.UniTask.CompletedTask;
            }

            public void SetConsent(ConsentScope scope, bool granted, bool saveImmediately = false)
            {
            }

            public void SetConsentCollected(bool isCollected, bool saveImmediately = false)
            {
            }
        }
    }
}
