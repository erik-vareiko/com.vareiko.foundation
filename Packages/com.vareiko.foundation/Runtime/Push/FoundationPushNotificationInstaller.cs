using UnityEngine;
using VContainer;
using VContainer.Unity;
using MessagePipe;

namespace Vareiko.Foundation.Push
{
    public static class FoundationPushNotificationInstaller
    {
        public static void Install(IContainerBuilder builder, PushNotificationConfig config = null)
        {
            builder.RegisterInstance(config != null ? config : ScriptableObject.CreateInstance<PushNotificationConfig>());

            if (config != null && config.Provider == PushNotificationProviderType.Simulated)
            {
                builder.RegisterEntryPoint<SimulatedPushNotificationService>(Lifetime.Singleton).As<IPushNotificationService>().AsSelf();
            }
            else if (config != null && config.Provider == PushNotificationProviderType.UnityNotifications)
            {
                builder.RegisterEntryPoint<UnityPushNotificationService>(Lifetime.Singleton).As<IPushNotificationService>().AsSelf();
            }
            else
            {
                builder.Register<NullPushNotificationService>(Lifetime.Singleton).As<IPushNotificationService>();
            }
        }

        // Message brokers live in the project scope (GlobalMessagePipe provider), so the
        // project composition calls this even when the module services install in the
        // scene scope.
        public static void RegisterSignals(IContainerBuilder builder, MessagePipeOptions signalOptions)
        {
            builder.RegisterMessageBroker<PushInitializedSignal>(signalOptions);
            builder.RegisterMessageBroker<PushPermissionChangedSignal>(signalOptions);
            builder.RegisterMessageBroker<PushTokenUpdatedSignal>(signalOptions);
            builder.RegisterMessageBroker<PushTopicSubscribedSignal>(signalOptions);
            builder.RegisterMessageBroker<PushTopicUnsubscribedSignal>(signalOptions);
            builder.RegisterMessageBroker<PushTopicSubscriptionFailedSignal>(signalOptions);
            builder.RegisterMessageBroker<PushTopicUnsubscriptionFailedSignal>(signalOptions);
            builder.RegisterMessageBroker<PushOperationTelemetrySignal>(signalOptions);
        }
    }
}
