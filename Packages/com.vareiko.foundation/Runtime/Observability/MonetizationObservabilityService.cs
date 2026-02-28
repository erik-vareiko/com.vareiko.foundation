using Vareiko.Foundation.Ads;
using Vareiko.Foundation.Iap;
using Vareiko.Foundation.Push;
using Zenject;

namespace Vareiko.Foundation.Observability
{
    public sealed class MonetizationObservabilityService : IMonetizationObservabilityService, IInitializable, System.IDisposable
    {
        private readonly SignalBus _signalBus;
        private readonly MonetizationObservabilitySnapshot _snapshot = new MonetizationObservabilitySnapshot();

        private int _iapPurchaseLatencySamples;
        private float _iapPurchaseLatencyTotalMs;
        private int _adShowLatencySamples;
        private float _adShowLatencyTotalMs;
        private int _pushPermissionLatencySamples;
        private float _pushPermissionLatencyTotalMs;

        [Inject]
        public MonetizationObservabilityService([InjectOptional] SignalBus signalBus = null)
        {
            _signalBus = signalBus;
        }

        public MonetizationObservabilitySnapshot Snapshot => _snapshot;

        public void Initialize()
        {
            if (_signalBus == null)
            {
                return;
            }

            TrySubscribe<IapPurchaseSucceededSignal>(OnIapPurchaseSucceeded);
            TrySubscribe<IapPurchaseFailedSignal>(OnIapPurchaseFailed);
            TrySubscribe<IapOperationTelemetrySignal>(OnIapTelemetry);

            TrySubscribe<AdShownSignal>(OnAdShown);
            TrySubscribe<AdShowFailedSignal>(OnAdShowFailed);
            TrySubscribe<AdsOperationTelemetrySignal>(OnAdsTelemetry);

            TrySubscribe<PushPermissionChangedSignal>(OnPushPermissionChanged);
            TrySubscribe<PushOperationTelemetrySignal>(OnPushTelemetry);
            TrySubscribe<PushTopicSubscribedSignal>(OnPushTopicSubscribed);
            TrySubscribe<PushTopicSubscriptionFailedSignal>(OnPushTopicSubscriptionFailed);
        }

        public void Dispose()
        {
            if (_signalBus == null)
            {
                return;
            }

            TryUnsubscribe<IapPurchaseSucceededSignal>(OnIapPurchaseSucceeded);
            TryUnsubscribe<IapPurchaseFailedSignal>(OnIapPurchaseFailed);
            TryUnsubscribe<IapOperationTelemetrySignal>(OnIapTelemetry);

            TryUnsubscribe<AdShownSignal>(OnAdShown);
            TryUnsubscribe<AdShowFailedSignal>(OnAdShowFailed);
            TryUnsubscribe<AdsOperationTelemetrySignal>(OnAdsTelemetry);

            TryUnsubscribe<PushPermissionChangedSignal>(OnPushPermissionChanged);
            TryUnsubscribe<PushOperationTelemetrySignal>(OnPushTelemetry);
            TryUnsubscribe<PushTopicSubscribedSignal>(OnPushTopicSubscribed);
            TryUnsubscribe<PushTopicSubscriptionFailedSignal>(OnPushTopicSubscriptionFailed);
        }

        private void OnIapPurchaseSucceeded(IapPurchaseSucceededSignal signal)
        {
            _snapshot.IapPurchaseSuccessCount++;
        }

        private void OnIapPurchaseFailed(IapPurchaseFailedSignal signal)
        {
            _snapshot.IapPurchaseFailureCount++;
        }

        private void OnIapTelemetry(IapOperationTelemetrySignal signal)
        {
            if (!IsPurchaseOperation(signal.Operation))
            {
                return;
            }

            _snapshot.IapPurchaseLastLatencyMs = ClampMs(signal.DurationMs);
            _iapPurchaseLatencySamples++;
            _iapPurchaseLatencyTotalMs += _snapshot.IapPurchaseLastLatencyMs;
            _snapshot.IapPurchaseAvgLatencyMs = _iapPurchaseLatencySamples > 0 ? _iapPurchaseLatencyTotalMs / _iapPurchaseLatencySamples : 0f;
        }

        private void OnAdShown(AdShownSignal signal)
        {
            _snapshot.AdShowSuccessCount++;
        }

        private void OnAdShowFailed(AdShowFailedSignal signal)
        {
            _snapshot.AdShowFailureCount++;
        }

        private void OnAdsTelemetry(AdsOperationTelemetrySignal signal)
        {
            if (!IsShowOperation(signal.Operation))
            {
                return;
            }

            _snapshot.AdShowLastLatencyMs = ClampMs(signal.DurationMs);
            _adShowLatencySamples++;
            _adShowLatencyTotalMs += _snapshot.AdShowLastLatencyMs;
            _snapshot.AdShowAvgLatencyMs = _adShowLatencySamples > 0 ? _adShowLatencyTotalMs / _adShowLatencySamples : 0f;
        }

        private void OnPushPermissionChanged(PushPermissionChangedSignal signal)
        {
            if (signal.Status == PushNotificationPermissionStatus.Granted)
            {
                _snapshot.PushPermissionGrantedCount++;
            }
            else if (signal.Status == PushNotificationPermissionStatus.Denied)
            {
                _snapshot.PushPermissionDeniedCount++;
            }
        }

        private void OnPushTelemetry(PushOperationTelemetrySignal signal)
        {
            if (!IsPermissionOperation(signal.Operation))
            {
                return;
            }

            _snapshot.PushPermissionLastLatencyMs = ClampMs(signal.DurationMs);
            _pushPermissionLatencySamples++;
            _pushPermissionLatencyTotalMs += _snapshot.PushPermissionLastLatencyMs;
            _snapshot.PushPermissionAvgLatencyMs = _pushPermissionLatencySamples > 0 ? _pushPermissionLatencyTotalMs / _pushPermissionLatencySamples : 0f;
        }

        private void OnPushTopicSubscribed(PushTopicSubscribedSignal signal)
        {
            _snapshot.PushTopicSubscribeSuccessCount++;
        }

        private void OnPushTopicSubscriptionFailed(PushTopicSubscriptionFailedSignal signal)
        {
            _snapshot.PushTopicSubscribeFailureCount++;
        }

        private static bool IsPurchaseOperation(string operation)
        {
            return string.Equals(operation, "purchase", System.StringComparison.Ordinal);
        }

        private static bool IsShowOperation(string operation)
        {
            return string.Equals(operation, "show", System.StringComparison.Ordinal);
        }

        private static bool IsPermissionOperation(string operation)
        {
            return string.Equals(operation, "request_permission", System.StringComparison.Ordinal);
        }

        private static float ClampMs(float value)
        {
            return value < 0f ? 0f : value;
        }

        private void TrySubscribe<TSignal>(System.Action<TSignal> handler)
        {
            try
            {
                _signalBus.Subscribe(handler);
            }
            catch (System.Exception)
            {
            }
        }

        private void TryUnsubscribe<TSignal>(System.Action<TSignal> handler)
        {
            try
            {
                _signalBus.Unsubscribe(handler);
            }
            catch (System.Exception)
            {
            }
        }
    }
}
