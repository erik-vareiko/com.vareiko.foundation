using Zenject;

namespace Vareiko.Foundation.App
{
    public static class FoundationAppInstaller
    {
        public static void Install(DiContainer container)
        {
            if (container.HasBinding<IAppStateMachine>())
            {
                return;
            }

            if (!container.HasBinding<SignalBus>())
            {
                SignalBusInstaller.Install(container);
            }

            container.DeclareSignal<AppStateChangedSignal>();
            container.BindInterfacesAndSelfTo<AppStateMachine>().AsSingle().NonLazy();
        }
    }
}
