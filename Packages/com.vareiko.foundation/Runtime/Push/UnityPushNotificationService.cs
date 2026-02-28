using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Vareiko.Foundation.Consent;
using Zenject;

namespace Vareiko.Foundation.Push
{
    public sealed class UnityPushNotificationService : IPushNotificationService, IInitializable, IDisposable
    {
        private readonly PushNotificationConfig _config;
        private readonly IConsentService _consentService;
        private readonly SignalBus _signalBus;
        private readonly HashSet<string> _topics = new HashSet<string>(StringComparer.Ordinal);

        private bool _initialized;
        private string _deviceToken = string.Empty;
        private PushNotificationPermissionStatus _permissionStatus = PushNotificationPermissionStatus.Unknown;

        [Inject]
        public UnityPushNotificationService(
            [InjectOptional] PushNotificationConfig config = null,
            [InjectOptional] IConsentService consentService = null,
            [InjectOptional] SignalBus signalBus = null)
        {
            _config = config;
            _consentService = consentService;
            _signalBus = signalBus;
            UnityPushNotificationBridge.DeviceTokenReceived += OnDeviceTokenReceived;
        }

        public PushNotificationProviderType Provider => PushNotificationProviderType.UnityNotifications;
        public bool IsConfigured => _config != null && _config.Provider == PushNotificationProviderType.UnityNotifications;
        public bool IsInitialized => _initialized;
        public PushNotificationPermissionStatus PermissionStatus => _permissionStatus;

        public void Dispose()
        {
            UnityPushNotificationBridge.DeviceTokenReceived -= OnDeviceTokenReceived;
        }

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
            float startedAt = UnityEngine.Time.realtimeSinceStartup;

            PushInitializeResult FinalizeResult(PushInitializeResult result)
            {
                EmitTelemetry("initialize", result.Success, result.ErrorCode, startedAt);
                return result;
            }

            if (_config == null)
            {
                return FinalizeResult(FailInitialize("Push notifications config is not assigned.", PushNotificationErrorCode.ConfigurationInvalid));
            }

            if (_config.Provider != PushNotificationProviderType.UnityNotifications)
            {
                return FinalizeResult(FailInitialize("Push notifications provider is not set to UnityNotifications.", PushNotificationErrorCode.ProviderUnavailable));
            }

            if (!ValidateDefaultTopics(out PushInitializeResult validationFailure))
            {
                return FinalizeResult(validationFailure);
            }

            if (!IsDependencyAvailable)
            {
                await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
                return FinalizeResult(FailInitialize("Push notifications dependency is unavailable. Define FOUNDATION_UNITY_NOTIFICATIONS and install mobile notifications/push SDK.", PushNotificationErrorCode.ProviderUnavailable));
            }

            _topics.Clear();
            _permissionStatus = PushNotificationPermissionStatus.Unknown;
            _deviceToken = NormalizeToken(UnityPushNotificationBridge.LastDeviceToken);
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

            return FinalizeResult(PushInitializeResult.Succeed());
        }

        public async UniTask<PushPermissionResult> RequestPermissionAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            float startedAt = UnityEngine.Time.realtimeSinceStartup;

            PushPermissionResult FinalizeResult(PushPermissionResult result)
            {
                EmitTelemetry("request_permission", result.Success, result.ErrorCode, startedAt);
                return result;
            }

            if (!_initialized)
            {
                return FinalizeResult(PushPermissionResult.Fail(PushNotificationPermissionStatus.Unknown, "Push notifications service is not initialized.", PushNotificationErrorCode.NotInitialized));
            }

            if (!HasPushConsent())
            {
                _permissionStatus = PushNotificationPermissionStatus.Denied;
                _signalBus?.Fire(new PushPermissionChangedSignal(_permissionStatus));
                return FinalizeResult(PushPermissionResult.Fail(_permissionStatus, "Push notifications consent is required.", PushNotificationErrorCode.ConsentDenied));
            }

            if (!IsDependencyAvailable)
            {
                _permissionStatus = PushNotificationPermissionStatus.Denied;
                _signalBus?.Fire(new PushPermissionChangedSignal(_permissionStatus));
                return FinalizeResult(PushPermissionResult.Fail(_permissionStatus, "Push notifications dependency is unavailable.", PushNotificationErrorCode.ProviderUnavailable));
            }

            bool granted = await RequestPlatformPermissionAsync(cancellationToken);
            if (!granted)
            {
                _permissionStatus = PushNotificationPermissionStatus.Denied;
                _signalBus?.Fire(new PushPermissionChangedSignal(_permissionStatus));
                return FinalizeResult(PushPermissionResult.Fail(_permissionStatus, "Push notifications permission denied.", PushNotificationErrorCode.PermissionDenied));
            }

            _permissionStatus = PushNotificationPermissionStatus.Granted;
            _signalBus?.Fire(new PushPermissionChangedSignal(_permissionStatus));

            if (string.IsNullOrWhiteSpace(_deviceToken))
            {
                _deviceToken = "UNITY_PUSH_" + Guid.NewGuid().ToString("N");
            }

            _signalBus?.Fire(new PushTokenUpdatedSignal(_deviceToken));
            return FinalizeResult(PushPermissionResult.Succeed());
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

            if (string.IsNullOrWhiteSpace(_deviceToken))
            {
                return UniTask.FromResult(PushDeviceTokenResult.Fail("Push notifications device token is unavailable.", PushNotificationErrorCode.OperationFailed));
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

        private static bool IsDependencyAvailable
        {
            get
            {
#if FOUNDATION_UNITY_NOTIFICATIONS
                return true;
#else
                return false;
#endif
            }
        }

        private async UniTask<bool> RequestPlatformPermissionAsync(CancellationToken cancellationToken)
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            const string notificationsPermission = "android.permission.POST_NOTIFICATIONS";
            if (UnityEngine.Android.Permission.HasUserAuthorizedPermission(notificationsPermission))
            {
                return true;
            }

            UnityEngine.Android.Permission.RequestUserPermission(notificationsPermission);
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            return UnityEngine.Android.Permission.HasUserAuthorizedPermission(notificationsPermission);
#else
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            return true;
#endif
        }

        private bool ValidateDefaultTopics(out PushInitializeResult failure)
        {
            HashSet<string> seen = new HashSet<string>(StringComparer.Ordinal);
            IReadOnlyList<PushNotificationConfig.TopicDefinition> topics = _config.DefaultTopics;
            if (topics == null)
            {
                failure = default;
                return true;
            }

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

            failure = default;
            return true;
        }

        private void SubscribeDefaultTopics()
        {
            IReadOnlyList<PushNotificationConfig.TopicDefinition> topics = _config.DefaultTopics;
            if (topics == null)
            {
                return;
            }

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
            if (!_config.RequirePushConsent)
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

        private void OnDeviceTokenReceived(string token)
        {
            string normalized = NormalizeToken(token);
            if (string.IsNullOrWhiteSpace(normalized))
            {
                return;
            }

            _deviceToken = normalized;
            if (_permissionStatus == PushNotificationPermissionStatus.Granted)
            {
                _signalBus?.Fire(new PushTokenUpdatedSignal(_deviceToken));
            }
        }

        private static string NormalizeToken(string token)
        {
            return string.IsNullOrWhiteSpace(token) ? string.Empty : token.Trim();
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

        private void EmitTelemetry(string operation, bool success, PushNotificationErrorCode errorCode, float startedAt)
        {
            float elapsedMs = Mathf.Max(0f, (UnityEngine.Time.realtimeSinceStartup - startedAt) * 1000f);
            _signalBus?.Fire(new PushOperationTelemetrySignal(operation, success, elapsedMs, errorCode));
        }
    }
}
