using VContainer;
using VContainer.Unity;
using MessagePipe;

namespace Vareiko.Foundation.Settings
{
    public static class FoundationSettingsInstaller
    {
        public static void Install(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<SettingsService>(Lifetime.Singleton).As<ISettingsService>().AsSelf();
        }

        // Message brokers live in the project scope (GlobalMessagePipe provider), so the
        // project composition calls this even when the module services install in the
        // scene scope.
        public static void RegisterSignals(IContainerBuilder builder, MessagePipeOptions signalOptions)
        {
            builder.RegisterMessageBroker<SettingsLoadedSignal>(signalOptions);
            builder.RegisterMessageBroker<SettingsChangedSignal>(signalOptions);
        }
    }
}
