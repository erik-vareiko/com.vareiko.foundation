using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Vareiko.Foundation.Consent;
using Zenject;

namespace Vareiko.Foundation.Attribution
{
    public sealed class ExternalAttributionBridgeService : IAttributionService, IInitializable
    {
        private readonly AttributionConfig _config;
        private readonly IConsentService _consentService;
        private readonly SignalBus _signalBus;

        private bool _initialized;
        private string _userId = string.Empty;

        [Inject]
        public ExternalAttributionBridgeService(
            [InjectOptional] AttributionConfig config = null,
            [InjectOptional] IConsentService consentService = null,
            [InjectOptional] SignalBus signalBus = null)
        {
            _config = config;
            _consentService = consentService;
            _signalBus = signalBus;
        }

        public AttributionProviderType Provider => AttributionProviderType.ExternalBridge;
        public bool IsConfigured => _config != null && _config.Provider == AttributionProviderType.ExternalBridge;
        public bool IsInitialized => _initialized;

        public void Initialize()
        {
            if (_config != null && _config.AutoInitializeOnStart)
            {
                InitializeAsync().Forget();
            }
        }

        public async UniTask<AttributionInitializeResult> InitializeAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_initialized)
            {
                return AttributionInitializeResult.Succeed();
            }

            if (_config == null)
            {
                return FailInitialize("Attribution config is not assigned.", AttributionErrorCode.ConfigurationInvalid);
            }

            if (_config.Provider != AttributionProviderType.ExternalBridge)
            {
                return FailInitialize("Attribution provider is not set to ExternalBridge.", AttributionErrorCode.ProviderUnavailable);
            }

            if (!ExternalAttributionBridge.TryGetTrackEventHandler(out _))
            {
                return FailInitialize("External attribution track-event handler is not configured.", AttributionErrorCode.ProviderUnavailable);
            }

            if (!ExternalAttributionBridge.TryGetTrackRevenueHandler(out _))
            {
                return FailInitialize("External attribution track-revenue handler is not configured.", AttributionErrorCode.ProviderUnavailable);
            }

            if (ExternalAttributionBridge.TryGetInitializeHandler(out System.Func<CancellationToken, UniTask<AttributionInitializeResult>> initializeHandler))
            {
                try
                {
                    AttributionInitializeResult result = await initializeHandler(cancellationToken);
                    if (!result.Success)
                    {
                        return FailInitialize(result.Error, result.ErrorCode);
                    }
                }
                catch (System.Exception exception)
                {
                    return FailInitialize($"External attribution initialize handler failed: {exception.Message}", AttributionErrorCode.Unknown);
                }
            }

            _initialized = true;
            _signalBus?.Fire(new AttributionInitializedSignal(true, string.Empty));
            return AttributionInitializeResult.Succeed();
        }

        public void SetUserId(string userId)
        {
            _userId = string.IsNullOrWhiteSpace(userId) ? string.Empty : userId.Trim();

            if (!ExternalAttributionBridge.TryGetSetUserIdHandler(out System.Action<string> handler))
            {
                return;
            }

            try
            {
                handler(_userId);
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        public async UniTask<AttributionTrackResult> TrackEventAsync(
            string eventName,
            IReadOnlyDictionary<string, string> properties = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_initialized)
            {
                return FailEvent(eventName, "Attribution service is not initialized.", AttributionErrorCode.NotInitialized);
            }

            if (string.IsNullOrWhiteSpace(eventName))
            {
                return FailEvent(string.Empty, "Attribution event name is empty.", AttributionErrorCode.InvalidPayload);
            }

            string normalizedEventName = eventName.Trim();
            if (!HasTrackingConsent())
            {
                return FailEvent(normalizedEventName, "Attribution tracking consent is required.", AttributionErrorCode.ConsentDenied);
            }

            if (!ExternalAttributionBridge.TryGetTrackEventHandler(out System.Func<string, IReadOnlyDictionary<string, string>, CancellationToken, UniTask<AttributionTrackResult>> handler))
            {
                return FailEvent(normalizedEventName, "External attribution track-event handler is not configured.", AttributionErrorCode.ProviderUnavailable);
            }

            IReadOnlyDictionary<string, string> payload = BuildEventProperties(properties);

            AttributionTrackResult rawResult;
            try
            {
                rawResult = await handler(normalizedEventName, payload, cancellationToken);
            }
            catch (System.Exception exception)
            {
                return FailEvent(normalizedEventName, $"External attribution track-event handler failed: {exception.Message}", AttributionErrorCode.TrackFailed);
            }

            AttributionTrackResult normalized = NormalizeTrackResult(rawResult, normalizedEventName);
            if (!normalized.Success)
            {
                return FailEvent(normalized.EventName, normalized.Error, normalized.ErrorCode);
            }

            _signalBus?.Fire(new AttributionEventTrackedSignal(normalized.EventName));
            return normalized;
        }

        public async UniTask<AttributionRevenueTrackResult> TrackRevenueAsync(
            AttributionRevenueData revenueData,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (!_initialized)
            {
                return FailRevenue(
                    revenueData.ProductId,
                    revenueData.Currency,
                    revenueData.Amount,
                    "Attribution service is not initialized.",
                    AttributionErrorCode.NotInitialized);
            }

            if (!revenueData.IsValid)
            {
                return FailRevenue(
                    revenueData.ProductId,
                    revenueData.Currency,
                    revenueData.Amount,
                    "Attribution revenue payload is invalid.",
                    AttributionErrorCode.InvalidPayload);
            }

            if (!HasTrackingConsent())
            {
                return FailRevenue(
                    revenueData.ProductId,
                    revenueData.Currency,
                    revenueData.Amount,
                    "Attribution tracking consent is required.",
                    AttributionErrorCode.ConsentDenied);
            }

            if (!ExternalAttributionBridge.TryGetTrackRevenueHandler(out System.Func<AttributionRevenueData, CancellationToken, UniTask<AttributionRevenueTrackResult>> handler))
            {
                return FailRevenue(
                    revenueData.ProductId,
                    revenueData.Currency,
                    revenueData.Amount,
                    "External attribution track-revenue handler is not configured.",
                    AttributionErrorCode.ProviderUnavailable);
            }

            AttributionRevenueTrackResult rawResult;
            try
            {
                rawResult = await handler(revenueData, cancellationToken);
            }
            catch (System.Exception exception)
            {
                return FailRevenue(
                    revenueData.ProductId,
                    revenueData.Currency,
                    revenueData.Amount,
                    $"External attribution track-revenue handler failed: {exception.Message}",
                    AttributionErrorCode.TrackFailed);
            }

            AttributionRevenueTrackResult normalized = NormalizeRevenueResult(rawResult, revenueData);
            if (!normalized.Success)
            {
                return FailRevenue(
                    normalized.ProductId,
                    normalized.Currency,
                    normalized.Amount,
                    normalized.Error,
                    normalized.ErrorCode);
            }

            _signalBus?.Fire(new AttributionRevenueTrackedSignal(
                normalized.ProductId,
                normalized.Currency,
                normalized.Amount,
                revenueData.TransactionId));
            return normalized;
        }

        private bool HasTrackingConsent()
        {
            if (_config == null || !_config.RequireTrackingConsent)
            {
                return true;
            }

            if (_consentService == null)
            {
                return false;
            }

            return _consentService.IsLoaded &&
                   _consentService.IsConsentCollected &&
                   _consentService.HasConsent(ConsentScope.Analytics);
        }

        private IReadOnlyDictionary<string, string> BuildEventProperties(IReadOnlyDictionary<string, string> properties)
        {
            Dictionary<string, string> merged = new Dictionary<string, string>(System.StringComparer.Ordinal);

            if (properties != null)
            {
                foreach (KeyValuePair<string, string> pair in properties)
                {
                    if (string.IsNullOrWhiteSpace(pair.Key))
                    {
                        continue;
                    }

                    merged[pair.Key] = pair.Value ?? string.Empty;
                }
            }

            if (!string.IsNullOrWhiteSpace(_userId) && !merged.ContainsKey("user_id"))
            {
                merged["user_id"] = _userId;
            }

            return merged;
        }

        private AttributionInitializeResult FailInitialize(string error, AttributionErrorCode errorCode)
        {
            _initialized = false;
            AttributionInitializeResult result = AttributionInitializeResult.Fail(error, errorCode);
            _signalBus?.Fire(new AttributionInitializedSignal(false, result.Error));
            return result;
        }

        private AttributionTrackResult FailEvent(string eventName, string error, AttributionErrorCode errorCode)
        {
            AttributionTrackResult result = AttributionTrackResult.Fail(eventName, error, errorCode);
            _signalBus?.Fire(new AttributionEventTrackFailedSignal(result.EventName, result.Error, result.ErrorCode));
            return result;
        }

        private AttributionRevenueTrackResult FailRevenue(
            string productId,
            string currency,
            double amount,
            string error,
            AttributionErrorCode errorCode)
        {
            AttributionRevenueTrackResult result = AttributionRevenueTrackResult.Fail(productId, currency, amount, error, errorCode);
            _signalBus?.Fire(new AttributionRevenueTrackFailedSignal(result.ProductId, result.Error, result.ErrorCode));
            return result;
        }

        private static AttributionTrackResult NormalizeTrackResult(AttributionTrackResult result, string fallbackEventName)
        {
            if (result.Success)
            {
                return AttributionTrackResult.Succeed(fallbackEventName);
            }

            return AttributionTrackResult.Fail(
                fallbackEventName,
                string.IsNullOrWhiteSpace(result.Error) ? "Attribution event tracking failed." : result.Error,
                result.ErrorCode == AttributionErrorCode.None ? AttributionErrorCode.TrackFailed : result.ErrorCode);
        }

        private static AttributionRevenueTrackResult NormalizeRevenueResult(
            AttributionRevenueTrackResult result,
            AttributionRevenueData fallbackData)
        {
            if (result.Success)
            {
                return AttributionRevenueTrackResult.Succeed(
                    fallbackData.ProductId,
                    fallbackData.Currency,
                    fallbackData.Amount);
            }

            return AttributionRevenueTrackResult.Fail(
                fallbackData.ProductId,
                fallbackData.Currency,
                fallbackData.Amount,
                string.IsNullOrWhiteSpace(result.Error) ? "Attribution revenue tracking failed." : result.Error,
                result.ErrorCode == AttributionErrorCode.None ? AttributionErrorCode.TrackFailed : result.ErrorCode);
        }
    }
}
