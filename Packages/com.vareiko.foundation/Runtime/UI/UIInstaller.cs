using Vareiko.Foundation.Signals;
using VContainer;
using VContainer.Unity;
using MessagePipe;

namespace Vareiko.Foundation.UI
{
    public static class FoundationUIInstaller
    {
        public static void Install(IContainerBuilder builder)
        {
            // UIRegistry is host-provided (a scene MonoBehaviour) and therefore optional; resolve
            // it leniently so UIService composes whether or not a registry is bound.
            builder.RegisterEntryPoint<UIService>(resolver =>
                {
                    resolver.TryResolve<UIRegistry>(out UIRegistry registry);
                    return new UIService(registry, resolver.Resolve<IFoundationSignalBus>());
                }, Lifetime.Singleton)
                .AsSelf();
            builder.Register<UIWindowManager>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<UIConfirmDialogService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<UIValueEventService>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
        }

        // Message brokers live in the project scope (GlobalMessagePipe provider), so the
        // project composition calls this even when the module services install in the
        // scene scope.
        public static void RegisterSignals(IContainerBuilder builder, MessagePipeOptions signalOptions)
        {
            builder.RegisterMessageBroker<UIReadySignal>(signalOptions);
            builder.RegisterMessageBroker<UIElementShownSignal>(signalOptions);
            builder.RegisterMessageBroker<UIElementHiddenSignal>(signalOptions);
            builder.RegisterMessageBroker<UIScreenShownSignal>(signalOptions);
            builder.RegisterMessageBroker<UIScreenHiddenSignal>(signalOptions);
            builder.RegisterMessageBroker<UIWindowQueuedSignal>(signalOptions);
            builder.RegisterMessageBroker<UIWindowShownSignal>(signalOptions);
            builder.RegisterMessageBroker<UIWindowClosedSignal>(signalOptions);
            builder.RegisterMessageBroker<UIWindowResolvedSignal>(signalOptions);
            builder.RegisterMessageBroker<UIWindowQueueDrainedSignal>(signalOptions);
            builder.RegisterMessageBroker<UIIntValueChangedSignal>(signalOptions);
            builder.RegisterMessageBroker<UIFloatValueChangedSignal>(signalOptions);
            builder.RegisterMessageBroker<UIBoolValueChangedSignal>(signalOptions);
            builder.RegisterMessageBroker<UIStringValueChangedSignal>(signalOptions);
        }
    }
}
