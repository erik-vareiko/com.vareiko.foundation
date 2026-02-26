using Zenject;

namespace Vareiko.Foundation.Common
{
    public static class FoundationCommonInstaller
    {
        public static void Install(DiContainer container)
        {
            if (container.HasBinding<IHealthCheckRunner>())
            {
                return;
            }

            if (!container.HasBinding<SignalBus>())
            {
                SignalBusInstaller.Install(container);
            }

            container.DeclareSignal<HealthCheckPassedSignal>();
            container.DeclareSignal<HealthCheckFailedSignal>();
            container.Bind<IHealthCheckRunner>().To<HealthCheckRunner>().AsSingle();
        }
    }
}
