using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Vareiko.Foundation.Ads;
using Vareiko.Foundation.Consent;
using Vareiko.Foundation.Tests.TestDoubles;
using Zenject;

namespace Vareiko.Foundation.Tests.Ads
{
    public sealed class SimulatedAdsServiceTests
    {
        [Test]
        public async Task Initialize_WithValidConfig_Succeeds()
        {
            AdsConfig config = CreateConfig(requireConsent: false);
            try
            {
                SimulatedAdsService service = new SimulatedAdsService(config, null, null);
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
        public async Task LoadAsync_WhenConsentMissing_ReturnsConsentDenied()
        {
            AdsConfig config = CreateConfig(requireConsent: true);
            try
            {
                SimulatedAdsService service = new SimulatedAdsService(
                    config,
                    new FakeConsentService(isLoaded: true, isCollected: false, adConsent: false),
                    null);
                await service.InitializeAsync();

                AdLoadResult load = await service.LoadAsync("rewarded.default");

                Assert.That(load.Success, Is.False);
                Assert.That(load.ErrorCode, Is.EqualTo(AdsErrorCode.ConsentDenied));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public async Task RewardedFlow_LoadThenShow_GrantsReward_AndFiresSignal()
        {
            AdsConfig config = CreateConfig(requireConsent: true);
            SignalBus signalBus = CreateSignalBus();
            int rewardAmount = 0;
            signalBus.Subscribe<AdRewardGrantedSignal>(signal => rewardAmount = signal.RewardAmount);

            try
            {
                SimulatedAdsService service = new SimulatedAdsService(
                    config,
                    new FakeConsentService(isLoaded: true, isCollected: true, adConsent: true),
                    signalBus);
                await service.InitializeAsync();

                AdLoadResult load = await service.LoadAsync("rewarded.default");
                AdShowResult show = await service.ShowAsync("rewarded.default");

                Assert.That(load.Success, Is.True);
                Assert.That(show.Success, Is.True);
                Assert.That(show.RewardGranted, Is.True);
                Assert.That(show.RewardAmount, Is.EqualTo(25));
                Assert.That(rewardAmount, Is.EqualTo(25));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public async Task InterstitialFlow_ShowWithoutLoad_ReturnsPlacementNotLoaded()
        {
            AdsConfig config = CreateConfig(requireConsent: false);
            try
            {
                SimulatedAdsService service = new SimulatedAdsService(config, null, null);
                await service.InitializeAsync();

                AdShowResult show = await service.ShowAsync("interstitial.default");

                Assert.That(show.Success, Is.False);
                Assert.That(show.ErrorCode, Is.EqualTo(AdsErrorCode.PlacementNotLoaded));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public async Task NullService_ReturnsProviderUnavailableFailures()
        {
            NullAdsService service = new NullAdsService();
            AdsInitializeResult init = await service.InitializeAsync();
            AdLoadResult load = await service.LoadAsync("any");
            AdShowResult show = await service.ShowAsync("any");

            Assert.That(init.Success, Is.False);
            Assert.That(load.Success, Is.False);
            Assert.That(show.Success, Is.False);
            Assert.That(show.ErrorCode, Is.EqualTo(AdsErrorCode.ProviderUnavailable));
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

        private static AdsConfig CreateConfig(bool requireConsent)
        {
            AdsConfig config = ScriptableObject.CreateInstance<AdsConfig>();
            ReflectionTestUtil.SetPrivateField(config, "_provider", AdsProviderType.Simulated);
            ReflectionTestUtil.SetPrivateField(config, "_autoInitializeOnStart", false);
            ReflectionTestUtil.SetPrivateField(config, "_requireAdvertisingConsent", requireConsent);

            AdsConfig.Placement rewarded = new AdsConfig.Placement();
            ReflectionTestUtil.SetPrivateField(rewarded, "_placementId", "rewarded.default");
            ReflectionTestUtil.SetPrivateField(rewarded, "_placementType", AdPlacementType.Rewarded);
            ReflectionTestUtil.SetPrivateField(rewarded, "_rewardId", "reward.coins");
            ReflectionTestUtil.SetPrivateField(rewarded, "_rewardAmount", 25);

            AdsConfig.Placement interstitial = new AdsConfig.Placement();
            ReflectionTestUtil.SetPrivateField(interstitial, "_placementId", "interstitial.default");
            ReflectionTestUtil.SetPrivateField(interstitial, "_placementType", AdPlacementType.Interstitial);

            ReflectionTestUtil.SetPrivateField(config, "_placements", new List<AdsConfig.Placement> { rewarded, interstitial });
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
