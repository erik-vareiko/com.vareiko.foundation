using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Vareiko.Foundation.Consent;
using Vareiko.Foundation.Push;
using Vareiko.Foundation.Tests.TestDoubles;
using Zenject;

namespace Vareiko.Foundation.Tests.Push
{
    public sealed class SimulatedPushNotificationServiceTests
    {
        [Test]
        public async Task Initialize_WithAutoPermissionAndTopics_SucceedsAndSubscribesDefaults()
        {
            PushNotificationConfig config = CreateConfig(requireConsent: false, autoRequestPermission: true, autoSubscribeDefaults: true);
            try
            {
                SimulatedPushNotificationService service = new SimulatedPushNotificationService(config, null, null);
                PushInitializeResult init = await service.InitializeAsync();
                PushDeviceTokenResult token = await service.GetDeviceTokenAsync();
                IReadOnlyList<string> topics = await service.GetSubscribedTopicsAsync();

                Assert.That(init.Success, Is.True);
                Assert.That(service.PermissionStatus, Is.EqualTo(PushNotificationPermissionStatus.Granted));
                Assert.That(token.Success, Is.True);
                Assert.That(token.DeviceToken, Is.EqualTo("SIM_TOKEN"));
                Assert.That(topics, Is.EquivalentTo(new[] { "general", "offers" }));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public async Task RequestPermission_WhenConsentMissing_ReturnsConsentDenied()
        {
            PushNotificationConfig config = CreateConfig(requireConsent: true, autoRequestPermission: false, autoSubscribeDefaults: false);
            try
            {
                SimulatedPushNotificationService service = new SimulatedPushNotificationService(
                    config,
                    new FakeConsentService(isLoaded: true, isCollected: false, pushConsent: false),
                    null);
                await service.InitializeAsync();

                PushPermissionResult permission = await service.RequestPermissionAsync();

                Assert.That(permission.Success, Is.False);
                Assert.That(permission.ErrorCode, Is.EqualTo(PushNotificationErrorCode.ConsentDenied));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public async Task Subscribe_WhenPermissionGranted_FiresTopicSignal()
        {
            PushNotificationConfig config = CreateConfig(requireConsent: false, autoRequestPermission: false, autoSubscribeDefaults: false);
            SignalBus signalBus = CreateSignalBus();
            string subscribedTopic = string.Empty;
            signalBus.Subscribe<PushTopicSubscribedSignal>(signal => subscribedTopic = signal.Topic);

            try
            {
                SimulatedPushNotificationService service = new SimulatedPushNotificationService(config, null, signalBus);
                await service.InitializeAsync();
                await service.RequestPermissionAsync();

                PushTopicResult subscribe = await service.SubscribeAsync("event.weekend");

                Assert.That(subscribe.Success, Is.True);
                Assert.That(subscribe.Topic, Is.EqualTo("event.weekend"));
                Assert.That(subscribedTopic, Is.EqualTo("event.weekend"));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public async Task Unsubscribe_WhenTopicIsMissing_ReturnsTopicNotSubscribed()
        {
            PushNotificationConfig config = CreateConfig(requireConsent: false, autoRequestPermission: false, autoSubscribeDefaults: false);
            try
            {
                SimulatedPushNotificationService service = new SimulatedPushNotificationService(config, null, null);
                await service.InitializeAsync();

                PushTopicResult unsubscribe = await service.UnsubscribeAsync("missing.topic");

                Assert.That(unsubscribe.Success, Is.False);
                Assert.That(unsubscribe.ErrorCode, Is.EqualTo(PushNotificationErrorCode.TopicNotSubscribed));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public async Task NullService_ReturnsProviderUnavailableFailures()
        {
            NullPushNotificationService service = new NullPushNotificationService();
            PushInitializeResult init = await service.InitializeAsync();
            PushPermissionResult permission = await service.RequestPermissionAsync();
            PushDeviceTokenResult token = await service.GetDeviceTokenAsync();
            PushTopicResult subscribe = await service.SubscribeAsync("topic");

            Assert.That(init.Success, Is.False);
            Assert.That(permission.Success, Is.False);
            Assert.That(token.Success, Is.False);
            Assert.That(subscribe.Success, Is.False);
            Assert.That(token.ErrorCode, Is.EqualTo(PushNotificationErrorCode.ProviderUnavailable));
        }

        private static SignalBus CreateSignalBus()
        {
            DiContainer container = new DiContainer();
            SignalBusInstaller.Install(container);
            container.DeclareSignal<PushInitializedSignal>();
            container.DeclareSignal<PushPermissionChangedSignal>();
            container.DeclareSignal<PushTokenUpdatedSignal>();
            container.DeclareSignal<PushTopicSubscribedSignal>();
            container.DeclareSignal<PushTopicSubscriptionFailedSignal>();
            container.DeclareSignal<PushTopicUnsubscribedSignal>();
            container.DeclareSignal<PushTopicUnsubscriptionFailedSignal>();
            return container.Resolve<SignalBus>();
        }

        private static PushNotificationConfig CreateConfig(bool requireConsent, bool autoRequestPermission, bool autoSubscribeDefaults)
        {
            PushNotificationConfig config = ScriptableObject.CreateInstance<PushNotificationConfig>();
            ReflectionTestUtil.SetPrivateField(config, "_provider", PushNotificationProviderType.Simulated);
            ReflectionTestUtil.SetPrivateField(config, "_autoInitializeOnStart", false);
            ReflectionTestUtil.SetPrivateField(config, "_requirePushConsent", requireConsent);
            ReflectionTestUtil.SetPrivateField(config, "_autoRequestPermissionOnInitialize", autoRequestPermission);
            ReflectionTestUtil.SetPrivateField(config, "_autoSubscribeDefaultTopics", autoSubscribeDefaults);
            ReflectionTestUtil.SetPrivateField(config, "_simulatePermissionDenied", false);
            ReflectionTestUtil.SetPrivateField(config, "_simulatedDeviceToken", "SIM_TOKEN");

            PushNotificationConfig.TopicDefinition general = new PushNotificationConfig.TopicDefinition();
            ReflectionTestUtil.SetPrivateField(general, "_topic", "general");

            PushNotificationConfig.TopicDefinition offers = new PushNotificationConfig.TopicDefinition();
            ReflectionTestUtil.SetPrivateField(offers, "_topic", "offers");

            ReflectionTestUtil.SetPrivateField(config, "_defaultTopics", new List<PushNotificationConfig.TopicDefinition> { general, offers });
            return config;
        }

        private sealed class FakeConsentService : IConsentService
        {
            private readonly bool _pushConsent;

            public FakeConsentService(bool isLoaded, bool isCollected, bool pushConsent)
            {
                IsLoaded = isLoaded;
                IsConsentCollected = isCollected;
                _pushConsent = pushConsent;
            }

            public bool IsLoaded { get; }
            public bool IsConsentCollected { get; }

            public bool HasConsent(ConsentScope scope)
            {
                return scope == ConsentScope.PushNotifications && _pushConsent;
            }

            public Cysharp.Threading.Tasks.UniTask LoadAsync(System.Threading.CancellationToken cancellationToken = default)
            {
                return Cysharp.Threading.Tasks.UniTask.CompletedTask;
            }

            public Cysharp.Threading.Tasks.UniTask SaveAsync(System.Threading.CancellationToken cancellationToken = default)
            {
                return Cysharp.Threading.Tasks.UniTask.CompletedTask;
            }

            public void SetConsent(ConsentScope scope, bool granted, bool saveImmediately = false)
            {
            }

            public void SetConsentCollected(bool isCollected, bool saveImmediately = false)
            {
            }
        }
    }
}
