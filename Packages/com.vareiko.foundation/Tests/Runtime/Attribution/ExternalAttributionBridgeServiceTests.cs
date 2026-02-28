using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Vareiko.Foundation.Attribution;
using Vareiko.Foundation.Consent;
using Vareiko.Foundation.Tests.TestDoubles;
using Zenject;

namespace Vareiko.Foundation.Tests.Attribution
{
    public sealed class ExternalAttributionBridgeServiceTests
    {
        [SetUp]
        public void SetUp()
        {
            ExternalAttributionBridge.ClearHandlers();
        }

        [TearDown]
        public void TearDown()
        {
            ExternalAttributionBridge.ClearHandlers();
        }

        [Test]
        public async Task Initialize_WhenHandlersConfigured_Succeeds()
        {
            AttributionConfig config = CreateConfig(requireTrackingConsent: false);
            try
            {
                ExternalAttributionBridge.SetTrackEventHandler((eventName, properties, _) => Cysharp.Threading.Tasks.UniTask.FromResult(AttributionTrackResult.Succeed(eventName)));
                ExternalAttributionBridge.SetTrackRevenueHandler((data, _) => Cysharp.Threading.Tasks.UniTask.FromResult(AttributionRevenueTrackResult.Succeed(data.ProductId, data.Currency, data.Amount)));

                ExternalAttributionBridgeService service = new ExternalAttributionBridgeService(config, null, null);
                AttributionInitializeResult result = await service.InitializeAsync();

                Assert.That(result.Success, Is.True);
                Assert.That(service.IsInitialized, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public async Task TrackEvent_WhenConsentMissing_ReturnsConsentDenied()
        {
            AttributionConfig config = CreateConfig(requireTrackingConsent: true);
            try
            {
                ExternalAttributionBridge.SetTrackEventHandler((eventName, properties, _) => Cysharp.Threading.Tasks.UniTask.FromResult(AttributionTrackResult.Succeed(eventName)));
                ExternalAttributionBridge.SetTrackRevenueHandler((data, _) => Cysharp.Threading.Tasks.UniTask.FromResult(AttributionRevenueTrackResult.Succeed(data.ProductId, data.Currency, data.Amount)));

                ExternalAttributionBridgeService service = new ExternalAttributionBridgeService(
                    config,
                    new FakeConsentService(isLoaded: true, isCollected: false, analyticsConsent: false),
                    null);
                await service.InitializeAsync();

                AttributionTrackResult result = await service.TrackEventAsync("session_start");

                Assert.That(result.Success, Is.False);
                Assert.That(result.ErrorCode, Is.EqualTo(AttributionErrorCode.ConsentDenied));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public async Task TrackEvent_WhenSucceeded_FiresSignal_AndInjectsUserIdProperty()
        {
            AttributionConfig config = CreateConfig(requireTrackingConsent: false);
            SignalBus signalBus = CreateSignalBus();
            int trackedCount = 0;
            signalBus.Subscribe<AttributionEventTrackedSignal>(_ => trackedCount++);

            IReadOnlyDictionary<string, string> capturedProperties = null;

            try
            {
                ExternalAttributionBridge.SetTrackEventHandler((eventName, properties, _) =>
                {
                    capturedProperties = properties;
                    return Cysharp.Threading.Tasks.UniTask.FromResult(AttributionTrackResult.Succeed(eventName));
                });
                ExternalAttributionBridge.SetTrackRevenueHandler((data, _) => Cysharp.Threading.Tasks.UniTask.FromResult(AttributionRevenueTrackResult.Succeed(data.ProductId, data.Currency, data.Amount)));

                ExternalAttributionBridgeService service = new ExternalAttributionBridgeService(config, null, signalBus);
                await service.InitializeAsync();
                service.SetUserId("player-1");

                AttributionTrackResult result = await service.TrackEventAsync("level_start", new Dictionary<string, string>
                {
                    ["level"] = "1"
                });

                Assert.That(result.Success, Is.True);
                Assert.That(trackedCount, Is.EqualTo(1));
                Assert.That(capturedProperties, Is.Not.Null);
                Assert.That(capturedProperties.ContainsKey("user_id"), Is.True);
                Assert.That(capturedProperties["user_id"], Is.EqualTo("player-1"));
                Assert.That(capturedProperties["level"], Is.EqualTo("1"));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public async Task TrackRevenue_WhenInvalidPayload_ReturnsInvalidPayload()
        {
            AttributionConfig config = CreateConfig(requireTrackingConsent: false);
            try
            {
                ExternalAttributionBridge.SetTrackEventHandler((eventName, properties, _) => Cysharp.Threading.Tasks.UniTask.FromResult(AttributionTrackResult.Succeed(eventName)));
                ExternalAttributionBridge.SetTrackRevenueHandler((data, _) => Cysharp.Threading.Tasks.UniTask.FromResult(AttributionRevenueTrackResult.Succeed(data.ProductId, data.Currency, data.Amount)));

                ExternalAttributionBridgeService service = new ExternalAttributionBridgeService(config, null, null);
                await service.InitializeAsync();

                AttributionRevenueTrackResult result = await service.TrackRevenueAsync(new AttributionRevenueData(string.Empty, "USD", 9.99d, "tx-1"));

                Assert.That(result.Success, Is.False);
                Assert.That(result.ErrorCode, Is.EqualTo(AttributionErrorCode.InvalidPayload));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public async Task TrackRevenue_WhenBridgeThrows_ReturnsTrackFailed()
        {
            AttributionConfig config = CreateConfig(requireTrackingConsent: false);
            try
            {
                ExternalAttributionBridge.SetTrackEventHandler((eventName, properties, _) => Cysharp.Threading.Tasks.UniTask.FromResult(AttributionTrackResult.Succeed(eventName)));
                ExternalAttributionBridge.SetTrackRevenueHandler((data, _) => throw new System.InvalidOperationException("revenue-bridge-crash"));

                ExternalAttributionBridgeService service = new ExternalAttributionBridgeService(config, null, null);
                await service.InitializeAsync();

                AttributionRevenueTrackResult result = await service.TrackRevenueAsync(new AttributionRevenueData("coins_pack", "USD", 4.99d, "tx-42"));

                Assert.That(result.Success, Is.False);
                Assert.That(result.ErrorCode, Is.EqualTo(AttributionErrorCode.TrackFailed));
                Assert.That(result.Error, Does.Contain("revenue-bridge-crash"));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public async Task NullService_ReturnsProviderUnavailableFailures()
        {
            NullAttributionService service = new NullAttributionService();

            AttributionInitializeResult init = await service.InitializeAsync();
            AttributionTrackResult track = await service.TrackEventAsync("session_start");
            AttributionRevenueTrackResult revenue = await service.TrackRevenueAsync(new AttributionRevenueData("coins_pack", "USD", 4.99d, "tx-42"));

            Assert.That(init.Success, Is.False);
            Assert.That(track.Success, Is.False);
            Assert.That(revenue.Success, Is.False);
            Assert.That(revenue.ErrorCode, Is.EqualTo(AttributionErrorCode.ProviderUnavailable));
        }

        private static SignalBus CreateSignalBus()
        {
            DiContainer container = new DiContainer();
            SignalBusInstaller.Install(container);
            container.DeclareSignal<AttributionInitializedSignal>();
            container.DeclareSignal<AttributionEventTrackedSignal>();
            container.DeclareSignal<AttributionEventTrackFailedSignal>();
            container.DeclareSignal<AttributionRevenueTrackedSignal>();
            container.DeclareSignal<AttributionRevenueTrackFailedSignal>();
            return container.Resolve<SignalBus>();
        }

        private static AttributionConfig CreateConfig(bool requireTrackingConsent)
        {
            AttributionConfig config = ScriptableObject.CreateInstance<AttributionConfig>();
            ReflectionTestUtil.SetPrivateField(config, "_provider", AttributionProviderType.ExternalBridge);
            ReflectionTestUtil.SetPrivateField(config, "_autoInitializeOnStart", false);
            ReflectionTestUtil.SetPrivateField(config, "_requireTrackingConsent", requireTrackingConsent);
            return config;
        }

        private sealed class FakeConsentService : IConsentService
        {
            private readonly bool _analyticsConsent;

            public FakeConsentService(bool isLoaded, bool isCollected, bool analyticsConsent)
            {
                IsLoaded = isLoaded;
                IsConsentCollected = isCollected;
                _analyticsConsent = analyticsConsent;
            }

            public bool IsLoaded { get; }
            public bool IsConsentCollected { get; }

            public bool HasConsent(ConsentScope scope)
            {
                return scope == ConsentScope.Analytics && _analyticsConsent;
            }

            public Cysharp.Threading.Tasks.UniTask LoadAsync(CancellationToken cancellationToken = default)
            {
                return Cysharp.Threading.Tasks.UniTask.CompletedTask;
            }

            public Cysharp.Threading.Tasks.UniTask SaveAsync(CancellationToken cancellationToken = default)
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
