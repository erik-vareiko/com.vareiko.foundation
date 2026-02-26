using Zenject;

namespace Vareiko.Foundation.Features
{
    public static class FoundationFeatureFlagsInstaller
    {
        public static void Install(DiContainer container, FeatureFlagsConfig config = null)
        {
            if (container.HasBinding<IFeatureFlagService>())
            {
                return;
            }

            if (!container.HasBinding<SignalBus>())
            {
                SignalBusInstaller.Install(container);
            }

            if (config != null)
            {
                container.BindInstance(config).IfNotBound();
            }

            container.DeclareSignal<FeatureFlagsRefreshedSignal>();
            container.DeclareSignal<FeatureFlagOverriddenSignal>();
            container.BindInterfacesAndSelfTo<FeatureFlagService>().AsSingle().NonLazy();
        }
    }
}
