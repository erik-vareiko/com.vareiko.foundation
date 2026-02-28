using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Vareiko.Foundation.Consent;
using Zenject;

namespace Vareiko.Foundation.Ads
{
    public sealed class SimulatedAdsService : IAdsService, IInitializable
    {
        private readonly AdsConfig _config;
        private readonly IConsentService _consentService;
        private readonly SignalBus _signalBus;
        private readonly Dictionary<string, AdsConfig.Placement> _placementsById = new Dictionary<string, AdsConfig.Placement>(System.StringComparer.Ordinal);
        private readonly HashSet<string> _loadedPlacements = new HashSet<string>(System.StringComparer.Ordinal);

        private bool _initialized;

        [Inject]
        public SimulatedAdsService(
            [InjectOptional] AdsConfig config = null,
            [InjectOptional] IConsentService consentService = null,
            [InjectOptional] SignalBus signalBus = null)
        {
            _config = config;
            _consentService = consentService;
            _signalBus = signalBus;
        }

        public AdsProviderType Provider => AdsProviderType.Simulated;
        public bool IsConfigured => _config != null && _config.Provider == AdsProviderType.Simulated && _config.Placements != null && _config.Placements.Count > 0;
        public bool IsInitialized => _initialized;

        public void Initialize()
        {
            if (_config != null && _config.AutoInitializeOnStart)
            {
                InitializeAsync().Forget();
            }
        }

        public UniTask<AdsInitializeResult> InitializeAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_config == null)
            {
                return UniTask.FromResult(FailInitialize("Ads config is not assigned.", AdsErrorCode.ConfigurationInvalid));
            }

            if (_config.Provider != AdsProviderType.Simulated)
            {
                return UniTask.FromResult(FailInitialize("Ads provider is not set to Simulated.", AdsErrorCode.ProviderUnavailable));
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
                    return UniTask.FromResult(FailInitialize("Ads placement has empty id.", AdsErrorCode.ConfigurationInvalid));
                }

                if (_placementsById.ContainsKey(placement.PlacementId))
                {
                    return UniTask.FromResult(FailInitialize($"Duplicate ads placement id '{placement.PlacementId}'.", AdsErrorCode.ConfigurationInvalid));
                }

                _placementsById[placement.PlacementId] = placement;
            }

            if (_placementsById.Count == 0)
            {
                return UniTask.FromResult(FailInitialize("Ads placement catalog is empty.", AdsErrorCode.ConfigurationInvalid));
            }

            _initialized = true;
            AdsInitializeResult success = AdsInitializeResult.Succeed();
            _signalBus?.Fire(new AdsInitializedSignal(true, string.Empty));
            return UniTask.FromResult(success);
        }

        public UniTask<IReadOnlyList<string>> GetPlacementIdsAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return UniTask.FromResult((IReadOnlyList<string>)new List<string>(_placementsById.Keys));
        }

        public UniTask<AdLoadResult> LoadAsync(string placementId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_initialized)
            {
                return UniTask.FromResult(FailLoad(placementId, AdPlacementType.Interstitial, "Ads service is not initialized.", AdsErrorCode.NotInitialized));
            }

            AdsConfig.Placement placement;
            if (!TryGetPlacement(placementId, out placement, out AdLoadResult placementError))
            {
                return UniTask.FromResult(placementError);
            }

            if (!HasAdvertisingConsent())
            {
                return UniTask.FromResult(FailLoad(placement.PlacementId, placement.PlacementType, "Advertising consent is required.", AdsErrorCode.ConsentDenied));
            }

            if (placement.SimulateLoadFailure)
            {
                return UniTask.FromResult(FailLoad(placement.PlacementId, placement.PlacementType, "Simulated ad load failure.", AdsErrorCode.LoadFailed));
            }

            _loadedPlacements.Add(placement.PlacementId);
            AdLoadResult success = AdLoadResult.Succeed(placement.PlacementId, placement.PlacementType);
            _signalBus?.Fire(new AdLoadedSignal(success.PlacementId, success.PlacementType));
            return UniTask.FromResult(success);
        }

        public UniTask<AdShowResult> ShowAsync(string placementId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_initialized)
            {
                return UniTask.FromResult(FailShow(placementId, AdPlacementType.Interstitial, "Ads service is not initialized.", AdsErrorCode.NotInitialized));
            }

            AdsConfig.Placement placement;
            if (!TryGetPlacement(placementId, out placement, out AdShowResult placementError))
            {
                return UniTask.FromResult(placementError);
            }

            if (!HasAdvertisingConsent())
            {
                return UniTask.FromResult(FailShow(placement.PlacementId, placement.PlacementType, "Advertising consent is required.", AdsErrorCode.ConsentDenied));
            }

            if (!_loadedPlacements.Contains(placement.PlacementId))
            {
                return UniTask.FromResult(FailShow(placement.PlacementId, placement.PlacementType, "Ads placement is not loaded.", AdsErrorCode.PlacementNotLoaded));
            }

            if (placement.SimulateShowFailure)
            {
                return UniTask.FromResult(FailShow(placement.PlacementId, placement.PlacementType, "Simulated ad show failure.", AdsErrorCode.ShowFailed));
            }

            _loadedPlacements.Remove(placement.PlacementId);

            bool rewardGranted = placement.PlacementType == AdPlacementType.Rewarded;
            string rewardId = rewardGranted ? placement.RewardId : string.Empty;
            int rewardAmount = rewardGranted ? placement.RewardAmount : 0;
            AdShowResult success = AdShowResult.Succeed(placement.PlacementId, placement.PlacementType, rewardGranted, rewardId, rewardAmount);

            if (rewardGranted)
            {
                _signalBus?.Fire(new AdRewardGrantedSignal(success.PlacementId, success.RewardId, success.RewardAmount));
            }

            _signalBus?.Fire(new AdShownSignal(success.PlacementId, success.PlacementType, success.RewardGranted));
            return UniTask.FromResult(success);
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
    }
}
