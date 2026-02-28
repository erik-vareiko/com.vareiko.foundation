using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Vareiko.Foundation.Ads;
using Vareiko.Foundation.Consent;
using Vareiko.Foundation.Tests.TestDoubles;
using Zenject;

namespace Vareiko.Foundation.Tests.Ads
{
    public sealed class ExternalAdsBridgeServiceTests
    {
        [SetUp]
        public void SetUp()
        {
            ExternalAdsBridge.ClearHandlers();
        }

        [TearDown]
        public void TearDown()
        {
            ExternalAdsBridge.ClearHandlers();
        }

        [Test]
        public async Task Initialize_WhenHandlersConfigured_Succeeds()
        {
            AdsConfig config = CreateConfig();
            try
            {
                ExternalAdsBridge.SetLoadHandler((placementId, _) => Cysharp.Threading.Tasks.UniTask.FromResult(AdLoadResult.Succeed(placementId, AdPlacementType.Rewarded)));
                ExternalAdsBridge.SetShowHandler((placementId, _) => Cysharp.Threading.Tasks.UniTask.FromResult(AdShowResult.Succeed(placementId, AdPlacementType.Rewarded, true, "reward.coins", 25)));

                ExternalAdsBridgeService service = new ExternalAdsBridgeService(config, null, null);
                AdsInitializeResult result = await service.InitializeAsync();

                Assert.That(result.Success, Is.True);
                Assert.That(service.IsInitialized, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public async Task Initialize_WhenLoadHandlerMissing_Fails()
        {
            AdsConfig config = CreateConfig();
            try
            {
                ExternalAdsBridge.SetShowHandler((placementId, _) => Cysharp.Threading.Tasks.UniTask.FromResult(AdShowResult.Succeed(placementId, AdPlacementType.Rewarded, true, "reward.coins", 25)));

                ExternalAdsBridgeService service = new ExternalAdsBridgeService(config, null, null);
                AdsInitializeResult result = await service.InitializeAsync();

                Assert.That(result.Success, Is.False);
                Assert.That(result.ErrorCode, Is.EqualTo(AdsErrorCode.ProviderUnavailable));
                Assert.That(service.IsInitialized, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public async Task LoadAndShow_WhenRewardedPlacement_FiresRewardAndUsesFallbackRewardPayload()
        {
            AdsConfig config = CreateConfig();
            SignalBus signalBus = CreateSignalBus();
            int rewardGrantedAmount = 0;
            signalBus.Subscribe<AdRewardGrantedSignal>(signal => rewardGrantedAmount = signal.RewardAmount);

            try
            {
                ExternalAdsBridge.SetLoadHandler((placementId, _) => Cysharp.Threading.Tasks.UniTask.FromResult(AdLoadResult.Succeed(placementId, AdPlacementType.Rewarded)));
                ExternalAdsBridge.SetShowHandler((placementId, _) => Cysharp.Threading.Tasks.UniTask.FromResult(AdShowResult.Succeed(placementId, AdPlacementType.Rewarded, true, string.Empty, 0)));

                ExternalAdsBridgeService service = new ExternalAdsBridgeService(
                    config,
                    new FakeConsentService(isLoaded: true, isCollected: true, adConsent: true),
                    signalBus);
                await service.InitializeAsync();

                AdLoadResult load = await service.LoadAsync("rewarded.default");
                AdShowResult show = await service.ShowAsync("rewarded.default");

                Assert.That(load.Success, Is.True);
                Assert.That(show.Success, Is.True);
                Assert.That(show.RewardGranted, Is.True);
                Assert.That(show.RewardId, Is.EqualTo("reward.coins"));
                Assert.That(show.RewardAmount, Is.EqualTo(25));
                Assert.That(rewardGrantedAmount, Is.EqualTo(25));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public async Task ShowAsync_WhenPlacementNotLoaded_ReturnsPlacementNotLoaded()
        {
            AdsConfig config = CreateConfig();
            try
            {
                ExternalAdsBridge.SetLoadHandler((placementId, _) => Cysharp.Threading.Tasks.UniTask.FromResult(AdLoadResult.Succeed(placementId, AdPlacementType.Rewarded)));
                ExternalAdsBridge.SetShowHandler((placementId, _) => Cysharp.Threading.Tasks.UniTask.FromResult(AdShowResult.Succeed(placementId, AdPlacementType.Rewarded, true, "reward.coins", 25)));

                ExternalAdsBridgeService service = new ExternalAdsBridgeService(config, null, null);
                await service.InitializeAsync();

                AdShowResult show = await service.ShowAsync("rewarded.default");

                Assert.That(show.Success, Is.False);
                Assert.That(show.ErrorCode, Is.EqualTo(AdsErrorCode.PlacementNotLoaded));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public async Task LoadAsync_WhenConsentDenied_ReturnsConsentDenied_AndSkipsBridgeCall()
        {
            AdsConfig config = CreateConfig(requireConsent: true);
            bool loadInvoked = false;
            try
            {
                ExternalAdsBridge.SetLoadHandler((placementId, _) =>
                {
                    loadInvoked = true;
                    return Cysharp.Threading.Tasks.UniTask.FromResult(AdLoadResult.Succeed(placementId, AdPlacementType.Rewarded));
                });
                ExternalAdsBridge.SetShowHandler((placementId, _) => Cysharp.Threading.Tasks.UniTask.FromResult(AdShowResult.Succeed(placementId, AdPlacementType.Rewarded, true, "reward.coins", 25)));

                ExternalAdsBridgeService service = new ExternalAdsBridgeService(
                    config,
                    new FakeConsentService(isLoaded: true, isCollected: false, adConsent: false),
                    null);
                await service.InitializeAsync();

                AdLoadResult load = await service.LoadAsync("rewarded.default");

                Assert.That(load.Success, Is.False);
                Assert.That(load.ErrorCode, Is.EqualTo(AdsErrorCode.ConsentDenied));
                Assert.That(loadInvoked, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public async Task LoadAsync_WhenBridgeThrows_ReturnsLoadFailed()
        {
            AdsConfig config = CreateConfig(requireConsent: false);
            try
            {
                ExternalAdsBridge.SetLoadHandler((placementId, _) => throw new System.InvalidOperationException("bridge-load-crash"));
                ExternalAdsBridge.SetShowHandler((placementId, _) => Cysharp.Threading.Tasks.UniTask.FromResult(AdShowResult.Succeed(placementId, AdPlacementType.Rewarded, true, "reward.coins", 25)));

                ExternalAdsBridgeService service = new ExternalAdsBridgeService(config, null, null);
                await service.InitializeAsync();

                AdLoadResult load = await service.LoadAsync("rewarded.default");

                Assert.That(load.Success, Is.False);
                Assert.That(load.ErrorCode, Is.EqualTo(AdsErrorCode.LoadFailed));
                Assert.That(load.Error, Does.Contain("bridge-load-crash"));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        private static SignalBus CreateSignalBus()
        {
            DiContainer container = new DiContainer();
            SignalBusInstaller.Install(container);
            container.DeclareSignal<AdsInitializedSignal>();
            container.DeclareSignal<AdLoadedSignal>();
            container.DeclareSignal<AdLoadFailedSignal>();
            container.DeclareSignal<AdShownSignal>();
            container.DeclareSignal<AdShowFailedSignal>();
            container.DeclareSignal<AdRewardGrantedSignal>();
            container.DeclareSignal<AdsOperationTelemetrySignal>();
            return container.Resolve<SignalBus>();
        }

        private static AdsConfig CreateConfig(bool requireConsent = false)
        {
            AdsConfig config = ScriptableObject.CreateInstance<AdsConfig>();
            ReflectionTestUtil.SetPrivateField(config, "_provider", AdsProviderType.ExternalBridge);
            ReflectionTestUtil.SetPrivateField(config, "_autoInitializeOnStart", false);
            ReflectionTestUtil.SetPrivateField(config, "_requireAdvertisingConsent", requireConsent);

            AdsConfig.Placement rewarded = new AdsConfig.Placement();
            ReflectionTestUtil.SetPrivateField(rewarded, "_placementId", "rewarded.default");
            ReflectionTestUtil.SetPrivateField(rewarded, "_placementType", AdPlacementType.Rewarded);
            ReflectionTestUtil.SetPrivateField(rewarded, "_rewardId", "reward.coins");
            ReflectionTestUtil.SetPrivateField(rewarded, "_rewardAmount", 25);

            ReflectionTestUtil.SetPrivateField(config, "_placements", new List<AdsConfig.Placement> { rewarded });
            return config;
        }

        private sealed class FakeConsentService : IConsentService
        {
            private readonly bool _adConsent;

            public FakeConsentService(bool isLoaded, bool isCollected, bool adConsent)
            {
                IsLoaded = isLoaded;
                IsConsentCollected = isCollected;
                _adConsent = adConsent;
            }

            public bool IsLoaded { get; }
            public bool IsConsentCollected { get; }

            public bool HasConsent(ConsentScope scope)
            {
                return scope == ConsentScope.Advertising && _adConsent;
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
