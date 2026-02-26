using Zenject;

namespace Vareiko.Foundation.Environment
{
    public static class FoundationEnvironmentInstaller
    {
        public static void Install(DiContainer container, EnvironmentConfig config = null)
        {
            if (container.HasBinding<IEnvironmentService>())
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

            container.DeclareSignal<EnvironmentProfileChangedSignal>();
            container.DeclareSignal<EnvironmentValueMissingSignal>();
            container.BindInterfacesAndSelfTo<EnvironmentService>().AsSingle().NonLazy();
        }
    }
}
