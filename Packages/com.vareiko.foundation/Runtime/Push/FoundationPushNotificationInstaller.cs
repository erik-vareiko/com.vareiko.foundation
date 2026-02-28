using Zenject;

namespace Vareiko.Foundation.Push
{
    public static class FoundationPushNotificationInstaller
    {
        public static void Install(DiContainer container, PushNotificationConfig config = null)
        {
            if (container.HasBinding<IPushNotificationService>())
            {
                return;
            }

            if (!container.HasBinding<SignalBus>())
            {
                SignalBusInstaller.Install(container);
            }

            container.DeclareSignal<PushInitializedSignal>();
            container.DeclareSignal<PushPermissionChangedSignal>();
            container.DeclareSignal<PushTokenUpdatedSignal>();
            container.DeclareSignal<PushTopicSubscribedSignal>();
            container.DeclareSignal<PushTopicSubscriptionFailedSignal>();
            container.DeclareSignal<PushTopicUnsubscribedSignal>();
            container.DeclareSignal<PushTopicUnsubscriptionFailedSignal>();
            container.DeclareSignal<PushOperationTelemetrySignal>();

            if (config != null)
            {
                container.BindInstance(config).IfNotBound();
            }

            if (config != null && config.Provider == PushNotificationProviderType.Simulated)
            {
                container.BindInterfacesAndSelfTo<SimulatedPushNotificationService>().AsSingle().NonLazy();
            }
            else if (config != null && config.Provider == PushNotificationProviderType.UnityNotifications)
            {
                container.BindInterfacesAndSelfTo<UnityPushNotificationService>().AsSingle().NonLazy();
            }
            else
            {
                container.Bind<IPushNotificationService>().To<NullPushNotificationService>().AsSingle();
            }
        }
    }
}
