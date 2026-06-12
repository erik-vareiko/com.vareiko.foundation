using System.Collections.Generic;
using Vareiko.Foundation.App;
using Vareiko.Foundation.Signals;
using VContainer;
using VContainer.Unity;
using MessagePipe;

namespace Vareiko.Foundation.Bootstrap
{
    public static class FoundationBootstrapInstaller
    {
        public static void Install(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<BootstrapRunner>(resolver => new BootstrapRunner(
                    new List<IBootstrapTask>(resolver.Resolve<IEnumerable<IBootstrapTask>>()),
                    resolver.Resolve<IFoundationSignalBus>(),
                    resolver.Resolve<IAppStateMachine>()),
                Lifetime.Singleton)
                .AsSelf();
        }

        // Message brokers live in the project scope (GlobalMessagePipe provider), so the
        // project composition calls this even when the module services install in the
        // scene scope.
        public static void RegisterSignals(IContainerBuilder builder, MessagePipeOptions signalOptions)
        {
            builder.RegisterMessageBroker<ApplicationBootStartedSignal>(signalOptions);
            builder.RegisterMessageBroker<ApplicationBootTaskStartedSignal>(signalOptions);
            builder.RegisterMessageBroker<ApplicationBootTaskCompletedSignal>(signalOptions);
            builder.RegisterMessageBroker<ApplicationBootCompletedSignal>(signalOptions);
            builder.RegisterMessageBroker<ApplicationBootFailedSignal>(signalOptions);
        }
    }
}
