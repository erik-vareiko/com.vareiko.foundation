using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Vareiko.Foundation.Push;
using Vareiko.Foundation.Tests.TestDoubles;
using Zenject;

namespace Vareiko.Foundation.Tests.Push
{
    public sealed class UnityPushNotificationServiceTests
    {
#if !FOUNDATION_UNITY_NOTIFICATIONS
        [Test]
        public async Task Initialize_WithoutNotificationsDefine_ReturnsProviderUnavailable()
        {
            PushNotificationConfig config = CreateUnityConfig();
            try
            {
                UnityPushNotificationService service = new UnityPushNotificationService(config, null, null);
                PushInitializeResult init = await service.InitializeAsync();

                Assert.That(init.Success, Is.False);
                Assert.That(init.ErrorCode, Is.EqualTo(PushNotificationErrorCode.ProviderUnavailable));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }
#endif

        [Test]
        public async Task RequestPermission_WhenNotInitialized_ReturnsNotInitialized()
        {
            PushNotificationConfig config = CreateUnityConfig();
            try
            {
                UnityPushNotificationService service = new UnityPushNotificationService(config, null, null);
                PushPermissionResult permission = await service.RequestPermissionAsync();

                Assert.That(permission.Success, Is.False);
                Assert.That(permission.ErrorCode, Is.EqualTo(PushNotificationErrorCode.NotInitialized));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void Installer_WhenProviderUnityNotifications_BindsUnityPushService()
        {
            PushNotificationConfig config = CreateUnityConfig();
            try
            {
                DiContainer container = new DiContainer();
                FoundationPushNotificationInstaller.Install(container, config);

                IPushNotificationService service = container.Resolve<IPushNotificationService>();
                Assert.That(service, Is.TypeOf<UnityPushNotificationService>());
                Assert.That(service.Provider, Is.EqualTo(PushNotificationProviderType.UnityNotifications));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        private static PushNotificationConfig CreateUnityConfig()
        {
            PushNotificationConfig config = ScriptableObject.CreateInstance<PushNotificationConfig>();
            ReflectionTestUtil.SetPrivateField(config, "_provider", PushNotificationProviderType.UnityNotifications);
            ReflectionTestUtil.SetPrivateField(config, "_autoInitializeOnStart", false);
            ReflectionTestUtil.SetPrivateField(config, "_requirePushConsent", false);
            ReflectionTestUtil.SetPrivateField(config, "_autoRequestPermissionOnInitialize", false);
            ReflectionTestUtil.SetPrivateField(config, "_autoSubscribeDefaultTopics", false);

            PushNotificationConfig.TopicDefinition topic = new PushNotificationConfig.TopicDefinition();
            ReflectionTestUtil.SetPrivateField(topic, "_topic", "general");
            ReflectionTestUtil.SetPrivateField(config, "_defaultTopics", new List<PushNotificationConfig.TopicDefinition> { topic });
            return config;
        }
    }
}
