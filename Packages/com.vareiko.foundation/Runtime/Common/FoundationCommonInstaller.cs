using System.Collections.Generic;
using Vareiko.Foundation.Signals;
using VContainer;

namespace Vareiko.Foundation.Common
{
    public static class FoundationCommonInstaller
    {
        public static void Install(IContainerBuilder builder)
        {
            builder.Register<HealthCheckRunner>(resolver => new HealthCheckRunner(
                    new List<IHealthCheck>(resolver.Resolve<IEnumerable<IHealthCheck>>()),
                    resolver.Resolve<IFoundationSignalBus>()),
                Lifetime.Singleton)
                .As<IHealthCheckRunner>();
        }
    }
}
