using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Vareiko.Foundation.Consent;
using Zenject;

namespace Vareiko.Foundation.Ads
{
    public sealed class ExternalAdsBridgeService : IAdsService, IInitializable
    {
        private readonly AdsConfig _config;
        private readonly IConsentService _consentService;
        private readonly SignalBus _signalBus;
        private readonly Dictionary<string, AdsConfig.Placement> _placementsById = new Dictionary<string, AdsConfig.Placement>(System.StringComparer.Ordinal);
        private readonly HashSet<string> _loadedPlacements = new HashSet<string>(System.StringComparer.Ordinal);

        private bool _initialized;

        [Inject]
        public ExternalAdsBridgeService(
            [InjectOptional] AdsConfig config = null,
            [InjectOptional] IConsentService consentService = null,
            [InjectOptional] SignalBus signalBus = null)
        {
            _config = config;
            _consentService = consentService;
            _signalBus = signalBus;
        }

        public AdsProviderType Provider => AdsProviderType.ExternalBridge;
        public bool IsConfigured => _config != null && _config.Provider == AdsProviderType.ExternalBridge && _config.Placements != null && _config.Placements.Count > 0;
        public bool IsInitialized => _initialized;

        public void Initialize()
        {
            if (_config != null && _config.AutoInitializeOnStart)
            {
                InitializeAsync().Forget();
            }
        }

        public async UniTask<AdsInitializeResult> InitializeAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_config == null)
            {
                return FailInitialize("Ads config is not assigned.", AdsErrorCode.ConfigurationInvalid);
            }

            if (_config.Provider != AdsProviderType.ExternalBridge)
            {
                return FailInitialize("Ads provider is not set to ExternalBridge.", AdsErrorCode.ProviderUnavailable);
            }

            _placementsById.Clear();
            _loadedPlacements.Clear();

            IReadOnlyList<AdsConfig.Placement> placements = _config.Placements;
            for (int i = 0; i < placements.Count; i++)
            {
                AdsConfig.Placement placement = placements[i];
                if (placement == null || !placement.Enabled)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(placement.PlacementId))
                {
                    return FailInitialize("Ads placement has empty id.", AdsErrorCode.ConfigurationInvalid);
                }

                if (_placementsById.ContainsKey(placement.PlacementId))
                {
                    return FailInitialize($"Duplicate ads placement id '{placement.PlacementId}'.", AdsErrorCode.ConfigurationInvalid);
                }

                _placementsById[placement.PlacementId] = placement;
            }

            if (_placementsById.Count == 0)
            {
                return FailInitialize("Ads placement catalog is empty.", AdsErrorCode.ConfigurationInvalid);
            }

            if (!ExternalAdsBridge.TryGetLoadHandler(out _))
            {
                return FailInitialize("External ads load handler is not configured.", AdsErrorCode.ProviderUnavailable);
            }

            if (!ExternalAdsBridge.TryGetShowHandler(out _))
            {
                return FailInitialize("External ads show handler is not configured.", AdsErrorCode.ProviderUnavailable);
            }

            if (ExternalAdsBridge.TryGetInitializeHandler(out System.Func<CancellationToken, UniTask<AdsInitializeResult>> initializeHandler))
            {
                try
                {
                    AdsInitializeResult initResult = await initializeHandler(cancellationToken);
                    if (!initResult.Success)
                    {
                        return FailInitialize(initResult.Error, initResult.ErrorCode);
                    }
                }
                catch (System.Exception exception)
                {
                    return FailInitialize($"External ads initialize handler failed: {exception.Message}", AdsErrorCode.Unknown);
                }
            }

            _initialized = true;
            AdsInitializeResult success = AdsInitializeResult.Succeed();
            _signalBus?.Fire(new AdsInitializedSignal(true, string.Empty));
            return success;
        }

        public UniTask<IReadOnlyList<string>> GetPlacementIdsAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return UniTask.FromResult((IReadOnlyList<string>)new List<string>(_placementsById.Keys));
        }

        public async UniTask<AdLoadResult> LoadAsync(string placementId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            float startedAt = UnityEngine.Time.realtimeSinceStartup;

            AdLoadResult FinalizeResult(AdLoadResult result)
            {
                EmitTelemetry("load", result.PlacementId, result.Success, result.ErrorCode, startedAt);
                return result;
            }

            if (!_initialized)
            {
                return FinalizeResult(FailLoad(placementId, AdPlacementType.Interstitial, "Ads service is not initialized.", AdsErrorCode.NotInitialized));
            }

            if (!TryGetPlacement(placementId, out AdsConfig.Placement placement, out AdLoadResult placementError))
            {
                return FinalizeResult(placementError);
            }

            if (!HasAdvertisingConsent())
            {
                return FinalizeResult(FailLoad(placement.PlacementId, placement.PlacementType, "Advertising consent is required.", AdsErrorCode.ConsentDenied));
            }

            if (!ExternalAdsBridge.TryGetLoadHandler(out System.Func<string, CancellationToken, UniTask<AdLoadResult>> loadHandler))
            {
                return FinalizeResult(FailLoad(placement.PlacementId, placement.PlacementType, "External ads load handler is not configured.", AdsErrorCode.ProviderUnavailable));
            }

            AdLoadResult rawResult;
            try
            {
                rawResult = await loadHandler(placement.PlacementId, cancellationToken);
            }
            catch (System.Exception exception)
            {
                return FinalizeResult(FailLoad(placement.PlacementId, placement.PlacementType, $"External ads load handler failed: {exception.Message}", AdsErrorCode.LoadFailed));
            }

            AdLoadResult result = NormalizeLoadResult(rawResult, placement);

            if (result.Success)
            {
                _loadedPlacements.Add(placement.PlacementId);
                _signalBus?.Fire(new AdLoadedSignal(result.PlacementId, result.PlacementType));
            }
            else
            {
                _signalBus?.Fire(new AdLoadFailedSignal(result.PlacementId, result.PlacementType, result.Error, result.ErrorCode));
            }

            return FinalizeResult(result);
        }

        public async UniTask<AdShowResult> ShowAsync(string placementId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            float startedAt = UnityEngine.Time.realtimeSinceStartup;

            AdShowResult FinalizeResult(AdShowResult result)
            {
                EmitTelemetry("show", result.PlacementId, result.Success, result.ErrorCode, startedAt);
                return result;
            }

            if (!_initialized)
            {
                return FinalizeResult(FailShow(placementId, AdPlacementType.Interstitial, "Ads service is not initialized.", AdsErrorCode.NotInitialized));
            }

            if (!TryGetPlacement(placementId, out AdsConfig.Placement placement, out AdShowResult placementError))
            {
                return FinalizeResult(placementError);
            }

            if (!HasAdvertisingConsent())
            {
                return FinalizeResult(FailShow(placement.PlacementId, placement.PlacementType, "Advertising consent is required.", AdsErrorCode.ConsentDenied));
            }

            if (!_loadedPlacements.Contains(placement.PlacementId))
            {
                return FinalizeResult(FailShow(placement.PlacementId, placement.PlacementType, "Ads placement is not loaded.", AdsErrorCode.PlacementNotLoaded));
            }

            if (!ExternalAdsBridge.TryGetShowHandler(out System.Func<string, CancellationToken, UniTask<AdShowResult>> showHandler))
            {
                return FinalizeResult(FailShow(placement.PlacementId, placement.PlacementType, "External ads show handler is not configured.", AdsErrorCode.ProviderUnavailable));
            }

            AdShowResult rawResult;
            try
            {
                rawResult = await showHandler(placement.PlacementId, cancellationToken);
            }
            catch (System.Exception exception)
            {
                return FinalizeResult(FailShow(placement.PlacementId, placement.PlacementType, $"External ads show handler failed: {exception.Message}", AdsErrorCode.ShowFailed));
            }

            AdShowResult result = NormalizeShowResult(rawResult, placement);

            if (result.Success)
            {
                _loadedPlacements.Remove(placement.PlacementId);

                if (result.RewardGranted)
                {
                    _signalBus?.Fire(new AdRewardGrantedSignal(result.PlacementId, result.RewardId, result.RewardAmount));
                }

                _signalBus?.Fire(new AdShownSignal(result.PlacementId, result.PlacementType, result.RewardGranted));
            }
            else
            {
                _signalBus?.Fire(new AdShowFailedSignal(result.PlacementId, result.PlacementType, result.Error, result.ErrorCode));
            }

            return FinalizeResult(result);
        }

        private bool HasAdvertisingConsent()
        {
            if (_config == null || !_config.RequireAdvertisingConsent)
            {
                return true;
            }

            if (_consentService == null)
            {
                return false;
            }

            return _consentService.IsLoaded &&
                   _consentService.IsConsentCollected &&
                   _consentService.HasConsent(ConsentScope.Advertising);
        }

        private bool TryGetPlacement(string placementId, out AdsConfig.Placement placement, out AdLoadResult failure)
        {
            placement = null;
            if (string.IsNullOrWhiteSpace(placementId))
            {
                failure = FailLoad(string.Empty, AdPlacementType.Interstitial, "Ads placement id is empty.", AdsErrorCode.ConfigurationInvalid);
                return false;
            }

            string id = placementId.Trim();
            if (!_placementsById.TryGetValue(id, out placement))
            {
                failure = FailLoad(id, AdPlacementType.Interstitial, "Ads placement not found.", AdsErrorCode.PlacementNotFound);
                return false;
            }

            failure = default;
            return true;
        }

        private bool TryGetPlacement(string placementId, out AdsConfig.Placement placement, out AdShowResult failure)
        {
            placement = null;
            if (string.IsNullOrWhiteSpace(placementId))
            {
                failure = FailShow(string.Empty, AdPlacementType.Interstitial, "Ads placement id is empty.", AdsErrorCode.ConfigurationInvalid);
                return false;
            }

            string id = placementId.Trim();
            if (!_placementsById.TryGetValue(id, out placement))
            {
                failure = FailShow(id, AdPlacementType.Interstitial, "Ads placement not found.", AdsErrorCode.PlacementNotFound);
                return false;
            }

            failure = default;
            return true;
        }

        private AdsInitializeResult FailInitialize(string error, AdsErrorCode errorCode)
        {
            _initialized = false;
            AdsInitializeResult result = AdsInitializeResult.Fail(error, errorCode);
            _signalBus?.Fire(new AdsInitializedSignal(false, result.Error));
            return result;
        }

        private AdLoadResult FailLoad(string placementId, AdPlacementType placementType, string error, AdsErrorCode errorCode)
        {
            AdLoadResult result = AdLoadResult.Fail(placementId, placementType, error, errorCode);
            _signalBus?.Fire(new AdLoadFailedSignal(result.PlacementId, result.PlacementType, result.Error, result.ErrorCode));
            return result;
        }

        private AdShowResult FailShow(string placementId, AdPlacementType placementType, string error, AdsErrorCode errorCode)
        {
            AdShowResult result = AdShowResult.Fail(placementId, placementType, error, errorCode);
            _signalBus?.Fire(new AdShowFailedSignal(result.PlacementId, result.PlacementType, result.Error, result.ErrorCode));
            return result;
        }

        private static AdLoadResult NormalizeLoadResult(AdLoadResult result, AdsConfig.Placement placement)
        {
            if (result.Success)
            {
                return AdLoadResult.Succeed(placement.PlacementId, placement.PlacementType);
            }

            return AdLoadResult.Fail(
                placement.PlacementId,
                placement.PlacementType,
                string.IsNullOrWhiteSpace(result.Error) ? "Ad load failed." : result.Error,
                result.ErrorCode == AdsErrorCode.None ? AdsErrorCode.LoadFailed : result.ErrorCode);
        }

        private static AdShowResult NormalizeShowResult(AdShowResult result, AdsConfig.Placement placement)
        {
            if (result.Success)
            {
                bool rewardGranted = placement.PlacementType == AdPlacementType.Rewarded && result.RewardGranted;
                string rewardId = rewardGranted ? (string.IsNullOrWhiteSpace(result.RewardId) ? placement.RewardId : result.RewardId) : string.Empty;
                int rewardAmount = rewardGranted ? (result.RewardAmount > 0 ? result.RewardAmount : placement.RewardAmount) : 0;
                return AdShowResult.Succeed(placement.PlacementId, placement.PlacementType, rewardGranted, rewardId, rewardAmount);
            }

            return AdShowResult.Fail(
                placement.PlacementId,
                placement.PlacementType,
                string.IsNullOrWhiteSpace(result.Error) ? "Ad show failed." : result.Error,
                result.ErrorCode == AdsErrorCode.None ? AdsErrorCode.ShowFailed : result.ErrorCode);
        }

        private void EmitTelemetry(string operation, string placementId, bool success, AdsErrorCode errorCode, float startedAt)
        {
            float elapsedMs = UnityEngine.Mathf.Max(0f, (UnityEngine.Time.realtimeSinceStartup - startedAt) * 1000f);
            _signalBus?.Fire(new AdsOperationTelemetrySignal(operation, placementId, success, elapsedMs, errorCode));
        }
    }
}
