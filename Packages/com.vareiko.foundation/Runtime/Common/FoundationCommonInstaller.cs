using Vareiko.Foundation.Signals;
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

            container.Bind<IFoundationSignalBus>().To<ZenjectFoundationSignalBus>().AsSingle().IfNotBound();

            container.DeclareSignal<HealthCheckPassedSignal>();
            container.DeclareSignal<HealthCheckFailedSignal>();
            container.Bind<IHealthCheckRunner>().To<HealthCheckRunner>().AsSingle();
        }
    }
}
