using Zenject;

namespace Vareiko.Foundation.Validation
{
    public static class FoundationValidationInstaller
    {
        public static void Install(DiContainer container)
        {
            if (container.HasBinding<StartupValidationRunner>())
            {
                return;
            }

            if (!container.HasBinding<SignalBus>())
            {
                SignalBusInstaller.Install(container);
            }

            container.DeclareSignal<StartupValidationPassedSignal>();
            container.DeclareSignal<StartupValidationFailedSignal>();
            container.DeclareSignal<StartupValidationWarningSignal>();
            container.DeclareSignal<StartupValidationCompletedSignal>();
            container.Bind<IStartupValidationRule>().To<SaveSecurityStartupValidationRule>().AsSingle();
            container.Bind<IStartupValidationRule>().To<BackendStartupValidationRule>().AsSingle();
            container.Bind<IStartupValidationRule>().To<ObservabilityStartupValidationRule>().AsSingle();
            container.BindInterfacesAndSelfTo<StartupValidationRunner>().AsSingle().NonLazy();
        }
    }
}
