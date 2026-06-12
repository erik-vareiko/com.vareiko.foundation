using UnityEngine;
using VContainer;
using VContainer.Unity;

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
    }
}
