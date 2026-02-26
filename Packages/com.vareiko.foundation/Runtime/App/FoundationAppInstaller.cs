using Zenject;

namespace Vareiko.Foundation.App
{
    public static class FoundationAppInstaller
    {
        public static void Install(DiContainer container)
        {
            if (container.HasBinding<IAppStateMachine>() && container.HasBinding<IApplicationLifecycleService>())
            {
                return;
            }

            if (!container.HasBinding<SignalBus>())
            {
                SignalBusInstaller.Install(container);
            }

            container.DeclareSignal<AppStateChangedSignal>();
            container.DeclareSignal<ApplicationPauseChangedSignal>();
            container.DeclareSignal<ApplicationFocusChangedSignal>();
            container.DeclareSignal<ApplicationQuitSignal>();

            if (!container.HasBinding<IAppStateMachine>())
            {
                container.BindInterfacesAndSelfTo<AppStateMachine>().AsSingle().NonLazy();
            }

            if (!container.HasBinding<IApplicationLifecycleService>())
            {
                container.BindInterfacesAndSelfTo<ApplicationLifecycleService>().AsSingle().NonLazy();
            }
        }
    }
}
