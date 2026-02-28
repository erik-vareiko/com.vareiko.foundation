using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Vareiko.Foundation.Consent;
using Zenject;

namespace Vareiko.Foundation.Push
{
    public sealed class SimulatedPushNotificationService : IPushNotificationService, IInitializable
    {
        private readonly PushNotificationConfig _config;
        private readonly IConsentService _consentService;
        private readonly SignalBus _signalBus;
        private readonly HashSet<string> _topics = new HashSet<string>(System.StringComparer.Ordinal);

        private bool _initialized;
        private string _deviceToken = string.Empty;
        private PushNotificationPermissionStatus _permissionStatus = PushNotificationPermissionStatus.Unknown;

        [Inject]
        public SimulatedPushNotificationService(
            [InjectOptional] PushNotificationConfig config = null,
            [InjectOptional] IConsentService consentService = null,
            [InjectOptional] SignalBus signalBus = null)
        {
            _config = config;
            _consentService = consentService;
            _signalBus = signalBus;
        }

        public PushNotificationProviderType Provider => PushNotificationProviderType.Simulated;
        public bool IsConfigured => _config != null && _config.Provider == PushNotificationProviderType.Simulated;
        public bool IsInitialized => _initialized;
        public PushNotificationPermissionStatus PermissionStatus => _permissionStatus;

        public void Initialize()
        {
            if (_config != null && _config.AutoInitializeOnStart)
            {
                InitializeAsync().Forget();
            }
        }

        public async UniTask<PushInitializeResult> InitializeAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_config == null)
            {
                return FailInitialize("Push notifications config is not assigned.", PushNotificationErrorCode.ConfigurationInvalid);
            }

            if (_config.Provider != PushNotificationProviderType.Simulated)
            {
                return FailInitialize("Push notifications provider is not set to Simulated.", PushNotificationErrorCode.ProviderUnavailable);
            }

            if (!ValidateDefaultTopics(out PushInitializeResult validationFailure))
            {
                return validationFailure;
            }

            _topics.Clear();
            _permissionStatus = PushNotificationPermissionStatus.Unknown;
            _deviceToken = string.IsNullOrWhiteSpace(_config.SimulatedDeviceToken) ? "SIMULATED_PUSH_TOKEN" : _config.SimulatedDeviceToken.Trim();
            _initialized = true;

            _signalBus?.Fire(new PushInitializedSignal(true, string.Empty));

            if (_config.AutoRequestPermissionOnInitialize)
            {
                PushPermissionResult permissionResult = await RequestPermissionAsync(cancellationToken);
                if (permissionResult.Success && _config.AutoSubscribeDefaultTopics)
                {
                    SubscribeDefaultTopics();
                }
            }

            return PushInitializeResult.Succeed();
        }

        public UniTask<PushPermissionResult> RequestPermissionAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_initialized)
            {
                return UniTask.FromResult(PushPermissionResult.Fail(PushNotificationPermissionStatus.Unknown, "Push notifications service is not initialized.", PushNotificationErrorCode.NotInitialized));
            }

            if (!HasPushConsent())
            {
                _permissionStatus = PushNotificationPermissionStatus.Denied;
                _signalBus?.Fire(new PushPermissionChangedSignal(_permissionStatus));
                return UniTask.FromResult(PushPermissionResult.Fail(_permissionStatus, "Push notifications consent is required.", PushNotificationErrorCode.ConsentDenied));
            }

            if (_config != null && _config.SimulatePermissionDenied)
            {
                _permissionStatus = PushNotificationPermissionStatus.Denied;
                _signalBus?.Fire(new PushPermissionChangedSignal(_permissionStatus));
                return UniTask.FromResult(PushPermissionResult.Fail(_permissionStatus, "Simulated push permission denial.", PushNotificationErrorCode.PermissionDenied));
            }

            _permissionStatus = PushNotificationPermissionStatus.Granted;
            _signalBus?.Fire(new PushPermissionChangedSignal(_permissionStatus));
            _signalBus?.Fire(new PushTokenUpdatedSignal(_deviceToken));
            return UniTask.FromResult(PushPermissionResult.Succeed());
        }

        public UniTask<PushDeviceTokenResult> GetDeviceTokenAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_initialized)
            {
                return UniTask.FromResult(PushDeviceTokenResult.Fail("Push notifications service is not initialized.", PushNotificationErrorCode.NotInitialized));
            }

            if (_permissionStatus != PushNotificationPermissionStatus.Granted)
            {
                return UniTask.FromResult(PushDeviceTokenResult.Fail("Push notifications permission is not granted.", PushNotificationErrorCode.PermissionDenied));
            }

            return UniTask.FromResult(PushDeviceTokenResult.Succeed(_deviceToken));
        }

        public UniTask<PushTopicResult> SubscribeAsync(string topic, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_initialized)
            {
                return UniTask.FromResult(FailSubscribe(topic, "Push notifications service is not initialized.", PushNotificationErrorCode.NotInitialized));
            }

            if (_permissionStatus != PushNotificationPermissionStatus.Granted)
            {
                return UniTask.FromResult(FailSubscribe(topic, "Push notifications permission is not granted.", PushNotificationErrorCode.PermissionDenied));
            }

            string normalizedTopic;
            if (!TryNormalizeTopic(topic, out normalizedTopic))
            {
                return UniTask.FromResult(FailSubscribe(topic, "Push notifications topic is empty.", PushNotificationErrorCode.TopicInvalid));
            }

            _topics.Add(normalizedTopic);
            _signalBus?.Fire(new PushTopicSubscribedSignal(normalizedTopic));
            return UniTask.FromResult(PushTopicResult.Succeed(normalizedTopic, true));
        }

        public UniTask<PushTopicResult> UnsubscribeAsync(string topic, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_initialized)
            {
                return UniTask.FromResult(FailUnsubscribe(topic, "Push notifications service is not initialized.", PushNotificationErrorCode.NotInitialized));
            }

            string normalizedTopic;
            if (!TryNormalizeTopic(topic, out normalizedTopic))
            {
                return UniTask.FromResult(FailUnsubscribe(topic, "Push notifications topic is empty.", PushNotificationErrorCode.TopicInvalid));
            }

            if (!_topics.Remove(normalizedTopic))
            {
                return UniTask.FromResult(FailUnsubscribe(normalizedTopic, "Push notifications topic is not subscribed.", PushNotificationErrorCode.TopicNotSubscribed));
            }

            _signalBus?.Fire(new PushTopicUnsubscribedSignal(normalizedTopic));
            return UniTask.FromResult(PushTopicResult.Succeed(normalizedTopic, false));
        }

        public UniTask<IReadOnlyList<string>> GetSubscribedTopicsAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return UniTask.FromResult((IReadOnlyList<string>)new List<string>(_topics));
        }

        private bool ValidateDefaultTopics(out PushInitializeResult failure)
        {
            HashSet<string> seen = new HashSet<string>(System.StringComparer.Ordinal);

            if (_config != null && _config.DefaultTopics != null)
            {
                IReadOnlyList<PushNotificationConfig.TopicDefinition> topics = _config.DefaultTopics;
                for (int i = 0; i < topics.Count; i++)
                {
                    PushNotificationConfig.TopicDefinition topic = topics[i];
                    if (topic == null || !topic.Enabled)
                    {
                        continue;
                    }

                    string normalizedTopic = topic.Topic;
                    if (string.IsNullOrWhiteSpace(normalizedTopic))
                    {
                        failure = FailInitialize("Push notifications default topic is empty.", PushNotificationErrorCode.ConfigurationInvalid);
                        return false;
                    }

                    if (!seen.Add(normalizedTopic))
                    {
                        failure = FailInitialize($"Duplicate push notifications default topic '{normalizedTopic}'.", PushNotificationErrorCode.ConfigurationInvalid);
                        return false;
                    }
                }
            }

            failure = default;
            return true;
        }

        private void SubscribeDefaultTopics()
        {
            if (_config == null || _config.DefaultTopics == null)
            {
                return;
            }

            IReadOnlyList<PushNotificationConfig.TopicDefinition> topics = _config.DefaultTopics;
            for (int i = 0; i < topics.Count; i++)
            {
                PushNotificationConfig.TopicDefinition topic = topics[i];
                if (topic == null || !topic.Enabled)
                {
                    continue;
                }

                string normalizedTopic = topic.Topic;
                if (string.IsNullOrWhiteSpace(normalizedTopic))
                {
                    continue;
                }

                if (_topics.Add(normalizedTopic))
                {
                    _signalBus?.Fire(new PushTopicSubscribedSignal(normalizedTopic));
                }
            }
        }

        private bool HasPushConsent()
        {
            if (_config == null || !_config.RequirePushConsent)
            {
                return true;
            }

            if (_consentService == null)
            {
                return false;
            }

            return _consentService.IsLoaded &&
                   _consentService.IsConsentCollected &&
                   _consentService.HasConsent(ConsentScope.PushNotifications);
        }

        private static bool TryNormalizeTopic(string topic, out string normalizedTopic)
        {
            normalizedTopic = string.IsNullOrWhiteSpace(topic) ? string.Empty : topic.Trim();
            return normalizedTopic.Length > 0;
        }

        private PushInitializeResult FailInitialize(string error, PushNotificationErrorCode errorCode)
        {
            _initialized = false;
            _permissionStatus = PushNotificationPermissionStatus.Unknown;
            _topics.Clear();
            PushInitializeResult result = PushInitializeResult.Fail(error, errorCode);
            _signalBus?.Fire(new PushInitializedSignal(false, result.Error));
            return result;
        }

        private PushTopicResult FailSubscribe(string topic, string error, PushNotificationErrorCode errorCode)
        {
            PushTopicResult result = PushTopicResult.Fail(topic, error, errorCode);
            _signalBus?.Fire(new PushTopicSubscriptionFailedSignal(result.Topic, result.Error, result.ErrorCode));
            return result;
        }

        private PushTopicResult FailUnsubscribe(string topic, string error, PushNotificationErrorCode errorCode)
        {
            PushTopicResult result = PushTopicResult.Fail(topic, error, errorCode);
            _signalBus?.Fire(new PushTopicUnsubscriptionFailedSignal(result.Topic, result.Error, result.ErrorCode));
            return result;
        }
    }
}
