using Zenject;

namespace Vareiko.Foundation.Connectivity
{
    public static class FoundationConnectivityInstaller
    {
        public static void Install(DiContainer container, ConnectivityConfig config = null)
        {
            if (container.HasBinding<IConnectivityService>())
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

            container.DeclareSignal<ConnectivityChangedSignal>();
            container.BindInterfacesAndSelfTo<ConnectivityService>().AsSingle().NonLazy();
        }
    }
}
