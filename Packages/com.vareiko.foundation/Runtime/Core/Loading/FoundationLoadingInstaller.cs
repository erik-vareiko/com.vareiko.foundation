using VContainer;
using VContainer.Unity;
using MessagePipe;

namespace Vareiko.Foundation.Loading
{
    public static class FoundationLoadingInstaller
    {
        public static void Install(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<LoadingService>(Lifetime.Singleton).As<ILoadingService>().AsSelf();
        }

        // Message brokers live in the project scope (GlobalMessagePipe provider), so the
        // project composition calls this even when the module services install in the
        // scene scope.
        public static void RegisterSignals(IContainerBuilder builder, MessagePipeOptions signalOptions)
        {
            builder.RegisterMessageBroker<LoadingStateChangedSignal>(signalOptions);
        }
    }
}
