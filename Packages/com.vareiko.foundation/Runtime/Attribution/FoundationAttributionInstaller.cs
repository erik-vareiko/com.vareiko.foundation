using Zenject;

namespace Vareiko.Foundation.Attribution
{
    public static class FoundationAttributionInstaller
    {
        public static void Install(DiContainer container, AttributionConfig config = null)
        {
            if (container.HasBinding<IAttributionService>())
            {
                return;
            }

            if (!container.HasBinding<SignalBus>())
            {
                SignalBusInstaller.Install(container);
            }

            container.DeclareSignal<AttributionInitializedSignal>();
            container.DeclareSignal<AttributionEventTrackedSignal>();
            container.DeclareSignal<AttributionEventTrackFailedSignal>();
            container.DeclareSignal<AttributionRevenueTrackedSignal>();
            container.DeclareSignal<AttributionRevenueTrackFailedSignal>();

            if (config != null)
            {
                container.BindInstance(config).IfNotBound();
            }

            if (config != null && config.Provider == AttributionProviderType.ExternalBridge)
            {
                container.BindInterfacesAndSelfTo<ExternalAttributionBridgeService>().AsSingle().NonLazy();
            }
            else
            {
                container.Bind<IAttributionService>().To<NullAttributionService>().AsSingle();
            }
        }
    }
}
