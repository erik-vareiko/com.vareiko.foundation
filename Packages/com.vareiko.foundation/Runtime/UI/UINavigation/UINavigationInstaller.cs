using VContainer;
using MessagePipe;

namespace Vareiko.Foundation.UINavigation
{
    public static class FoundationUINavigationInstaller
    {
        public static void Install(IContainerBuilder builder)
        {
            builder.Register<UINavigationService>(Lifetime.Singleton).As<IUINavigationService>();
        }

        // Message brokers live in the project scope (GlobalMessagePipe provider), so the
        // project composition calls this even when the module services install in the
        // scene scope.
        public static void RegisterSignals(IContainerBuilder builder, MessagePipeOptions signalOptions)
        {
            builder.RegisterMessageBroker<UINavigationChangedSignal>(signalOptions);
        }
    }
}
