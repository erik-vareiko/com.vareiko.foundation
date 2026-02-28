using Zenject;

namespace Vareiko.Foundation.Ads
{
    public static class FoundationAdsInstaller
    {
        public static void Install(DiContainer container, AdsConfig config = null)
        {
            if (container.HasBinding<IAdsService>())
            {
                return;
            }

            if (!container.HasBinding<SignalBus>())
            {
                SignalBusInstaller.Install(container);
            }

            container.DeclareSignal<AdsInitializedSignal>();
            container.DeclareSignal<AdLoadedSignal>();
            container.DeclareSignal<AdLoadFailedSignal>();
            container.DeclareSignal<AdShownSignal>();
            container.DeclareSignal<AdShowFailedSignal>();
            container.DeclareSignal<AdRewardGrantedSignal>();
            container.DeclareSignal<AdsOperationTelemetrySignal>();

            if (config != null)
            {
                container.BindInstance(config).IfNotBound();
            }

            if (config != null && config.Provider == AdsProviderType.Simulated)
            {
                container.BindInterfacesAndSelfTo<SimulatedAdsService>().AsSingle().NonLazy();
            }
            else if (config != null && config.Provider == AdsProviderType.ExternalBridge)
            {
                container.BindInterfacesAndSelfTo<ExternalAdsBridgeService>().AsSingle().NonLazy();
            }
            else
            {
                container.Bind<IAdsService>().To<NullAdsService>().AsSingle();
            }
        }
    }
}
