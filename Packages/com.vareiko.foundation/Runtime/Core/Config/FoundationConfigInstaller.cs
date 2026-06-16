using VContainer;
using MessagePipe;

namespace Vareiko.Foundation.Config
{
    public static class FoundationConfigInstaller
    {
        public static void Install(IContainerBuilder builder)
        {
            builder.Register<ConfigService>(Lifetime.Singleton).As<IConfigService>();
        }

        // Message brokers live in the project scope (GlobalMessagePipe provider), so the
        // project composition calls this even when the module services install in the
        // scene scope.
        public static void RegisterSignals(IContainerBuilder builder, MessagePipeOptions signalOptions)
        {
            builder.RegisterMessageBroker<ConfigRegisteredSignal>(signalOptions);
            builder.RegisterMessageBroker<ConfigMissingSignal>(signalOptions);
        }
    }
}
