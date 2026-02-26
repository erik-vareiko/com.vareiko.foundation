using Zenject;

namespace Vareiko.Foundation.Bootstrap
{
    public static class FoundationBootstrapInstaller
    {
        public static void Install(DiContainer container)
        {
            if (container.HasBindingId(typeof(BootstrapRunner), null, InjectSources.Local))
            {
                return;
            }

            if (!container.HasBinding<SignalBus>())
            {
                SignalBusInstaller.Install(container);
            }

            container.DeclareSignal<ApplicationBootStartedSignal>();
            container.DeclareSignal<ApplicationBootTaskStartedSignal>();
            container.DeclareSignal<ApplicationBootTaskCompletedSignal>();
            container.DeclareSignal<ApplicationBootCompletedSignal>();
            container.DeclareSignal<ApplicationBootFailedSignal>();

            container.BindInterfacesAndSelfTo<BootstrapRunner>().AsSingle().NonLazy();
        }
    }
}
