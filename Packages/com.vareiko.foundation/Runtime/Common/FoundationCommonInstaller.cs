using System.Collections.Generic;
using Vareiko.Foundation.Signals;
using VContainer;
using MessagePipe;

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

        // Message brokers live in the project scope (GlobalMessagePipe provider), so the
        // project composition calls this even when the module services install in the
        // scene scope.
        public static void RegisterSignals(IContainerBuilder builder, MessagePipeOptions signalOptions)
        {
            builder.RegisterMessageBroker<HealthCheckPassedSignal>(signalOptions);
            builder.RegisterMessageBroker<HealthCheckFailedSignal>(signalOptions);
        }
    }
}
