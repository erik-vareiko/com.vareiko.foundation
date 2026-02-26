using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Vareiko.Foundation.Consent;
using Vareiko.Foundation.Save;
using Vareiko.Foundation.Settings;
using Vareiko.Foundation.Tests.TestDoubles;

namespace Vareiko.Foundation.Tests.Save
{
    public sealed class AutosaveServiceTests
    {
        [Test]
        public async Task Tick_WhenDirtyAndIntervalReached_SavesSettingsAndConsent()
        {
            FakeTimeProvider time = new FakeTimeProvider { Time = 0f, UnscaledTime = 0f };
            AutosaveConfig config = ScriptableObject.CreateInstance<AutosaveConfig>();
            ReflectionTestUtil.SetPrivateField(config, "_intervalSeconds", 1f);
            ReflectionTestUtil.SetPrivateField(config, "_saveOnApplicationPause", false);
            ReflectionTestUtil.SetPrivateField(config, "_saveOnApplicationQuit", false);

            FakeSettingsService settings = new FakeSettingsService();
            FakeConsentService consent = new FakeConsentService();
            AutosaveService service = new AutosaveService(time, config, settings, consent, null);
            service.Initialize();

            SetDirtyFlag(service, "_dirtySettings", true);
            SetDirtyFlag(service, "_dirtyConsent", true);
            time.Time = 1f;

            service.Tick();
            await UniTask.DelayFrame(2);

            Assert.That(settings.SaveCalls, Is.EqualTo(1));
            Assert.That(consent.SaveCalls, Is.EqualTo(1));
            service.Dispose();
        }

        private static void SetDirtyFlag(AutosaveService service, string name, bool value)
        {
            BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
            FieldInfo field = typeof(AutosaveService).GetField(name, flags);
            Assert.That(field, Is.Not.Null);
            field.SetValue(service, value);
        }

        private sealed class FakeSettingsService : ISettingsService
        {
            public int SaveCalls { get; private set; }
            public bool IsLoaded => true;
            public GameSettings Current { get; } = new GameSettings();

            public UniTask LoadAsync(CancellationToken cancellationToken = default)
            {
                return UniTask.CompletedTask;
            }

            public UniTask SaveAsync(CancellationToken cancellationToken = default)
            {
                SaveCalls++;
                return UniTask.CompletedTask;
            }

            public void Apply(GameSettings settings, bool saveImmediately = false)
            {
            }
        }

        private sealed class FakeConsentService : IConsentService
        {
            public int SaveCalls { get; private set; }
            public bool IsLoaded => true;
            public bool IsConsentCollected => true;

            public bool HasConsent(ConsentScope scope)
            {
                return true;
            }

            public UniTask LoadAsync(CancellationToken cancellationToken = default)
            {
                return UniTask.CompletedTask;
            }

            public UniTask SaveAsync(CancellationToken cancellationToken = default)
            {
                SaveCalls++;
                return UniTask.CompletedTask;
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
