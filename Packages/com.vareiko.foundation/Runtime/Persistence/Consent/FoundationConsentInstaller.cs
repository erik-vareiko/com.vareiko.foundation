using VContainer;
using VContainer.Unity;
using MessagePipe;

namespace Vareiko.Foundation.Consent
{
    public static class FoundationConsentInstaller
    {
        public static void Install(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<ConsentService>(Lifetime.Singleton).As<IConsentService>().AsSelf();
        }

        // Message brokers live in the project scope (GlobalMessagePipe provider), so the
        // project composition calls this even when the module services install in the
        // scene scope.
        public static void RegisterSignals(IContainerBuilder builder, MessagePipeOptions signalOptions)
        {
            builder.RegisterMessageBroker<ConsentLoadedSignal>(signalOptions);
            builder.RegisterMessageBroker<ConsentChangedSignal>(signalOptions);
        }
    }
}
