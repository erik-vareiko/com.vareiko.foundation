using System.Collections.Generic;
using Vareiko.Foundation.Signals;
using VContainer;
using VContainer.Unity;
using MessagePipe;

namespace Vareiko.Foundation.Validation
{
    public static class FoundationValidationInstaller
    {
        public static void Install(IContainerBuilder builder)
        {
            // Only the rule framework lives here; the foundation's cross-module rules
            // (save security, backend, observability) are registered by the composition
            // root, and hosts add their own IStartupValidationRule bindings the same way.
            builder.RegisterEntryPoint<StartupValidationRunner>(resolver => new StartupValidationRunner(
                    new List<IStartupValidationRule>(resolver.Resolve<IEnumerable<IStartupValidationRule>>()),
                    resolver.Resolve<IFoundationSignalBus>()),
                Lifetime.Singleton)
                .AsSelf();
        }

        // Message brokers live in the project scope (GlobalMessagePipe provider), so the
        // project composition calls this even when the module services install in the
        // scene scope.
        public static void RegisterSignals(IContainerBuilder builder, MessagePipeOptions signalOptions)
        {
            builder.RegisterMessageBroker<StartupValidationPassedSignal>(signalOptions);
            builder.RegisterMessageBroker<StartupValidationWarningSignal>(signalOptions);
            builder.RegisterMessageBroker<StartupValidationFailedSignal>(signalOptions);
            builder.RegisterMessageBroker<StartupValidationCompletedSignal>(signalOptions);
        }
    }
}
