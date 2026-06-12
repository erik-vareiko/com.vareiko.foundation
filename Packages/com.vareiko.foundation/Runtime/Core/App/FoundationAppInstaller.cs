using Vareiko.Foundation.Signals;
using VContainer;
using VContainer.Unity;
using MessagePipe;

namespace Vareiko.Foundation.App
{
    public static class FoundationAppInstaller
    {
        public static void Install(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<AppStateMachine>(Lifetime.Singleton).As<IAppStateMachine>().AsSelf();

            // IApplicationLifecycleSource is optional/host-provided; when absent the service
            // creates and owns its own Unity source (preserving the Zenject optional behaviour).
            builder.RegisterEntryPoint<ApplicationLifecycleService>(resolver =>
                {
                    resolver.TryResolve<IApplicationLifecycleSource>(out IApplicationLifecycleSource source);
                    return new ApplicationLifecycleService(resolver.Resolve<IFoundationSignalBus>(), source);
                }, Lifetime.Singleton)
                .AsSelf();
        }

        // Message brokers live in the project scope (GlobalMessagePipe provider), so the
        // project composition calls this even when the module services install in the
        // scene scope.
        public static void RegisterSignals(IContainerBuilder builder, MessagePipeOptions signalOptions)
        {
            builder.RegisterMessageBroker<AppStateChangedSignal>(signalOptions);
            builder.RegisterMessageBroker<ApplicationPauseChangedSignal>(signalOptions);
            builder.RegisterMessageBroker<ApplicationFocusChangedSignal>(signalOptions);
            builder.RegisterMessageBroker<ApplicationQuitSignal>(signalOptions);
        }
    }
}
