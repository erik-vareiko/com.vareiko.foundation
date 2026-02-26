using Zenject;

namespace Vareiko.Foundation.Config
{
    public static class FoundationConfigInstaller
    {
        public static void Install(DiContainer container)
        {
            if (container.HasBinding<IConfigService>())
            {
                return;
            }

            if (!container.HasBinding<SignalBus>())
            {
                SignalBusInstaller.Install(container);
            }

            container.DeclareSignal<ConfigRegisteredSignal>();
            container.DeclareSignal<ConfigMissingSignal>();
            container.Bind<IConfigService>().To<ConfigService>().AsSingle();
        }
    }
}
