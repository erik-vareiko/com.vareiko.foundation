namespace Vareiko.Foundation.Ads
{
    public readonly struct AdsInitializedSignal
    {
        public readonly bool Success;
        public readonly string Error;

        public AdsInitializedSignal(bool success, string error)
        {
            Success = success;
            Error = error ?? string.Empty;
        }
    }

    public readonly struct AdLoadedSignal
    {
        public readonly string PlacementId;
        public readonly AdPlacementType PlacementType;

        public AdLoadedSignal(string placementId, AdPlacementType placementType)
        {
            PlacementId = placementId ?? string.Empty;
            PlacementType = placementType;
        }
    }

    public readonly struct AdLoadFailedSignal
    {
        public readonly string PlacementId;
        public readonly AdPlacementType PlacementType;
        public readonly string Error;
        public readonly AdsErrorCode ErrorCode;

        public AdLoadFailedSignal(string placementId, AdPlacementType placementType, string error, AdsErrorCode errorCode)
        {
            PlacementId = placementId ?? string.Empty;
            PlacementType = placementType;
            Error = error ?? string.Empty;
            ErrorCode = errorCode;
        }
    }

    public readonly struct AdShownSignal
    {
        public readonly string PlacementId;
        public readonly AdPlacementType PlacementType;
        public readonly bool RewardGranted;

        public AdShownSignal(string placementId, AdPlacementType placementType, bool rewardGranted)
        {
            PlacementId = placementId ?? string.Empty;
            PlacementType = placementType;
            RewardGranted = rewardGranted;
        }
    }

    public readonly struct AdShowFailedSignal
    {
        public readonly string PlacementId;
        public readonly AdPlacementType PlacementType;
        public readonly string Error;
        public readonly AdsErrorCode ErrorCode;

        public AdShowFailedSignal(string placementId, AdPlacementType placementType, string error, AdsErrorCode errorCode)
        {
            PlacementId = placementId ?? string.Empty;
            PlacementType = placementType;
            Error = error ?? string.Empty;
            ErrorCode = errorCode;
        }
    }

    public readonly struct AdRewardGrantedSignal
    {
        public readonly string PlacementId;
        public readonly string RewardId;
        public readonly int RewardAmount;

        public AdRewardGrantedSignal(string placementId, string rewardId, int rewardAmount)
        {
            PlacementId = placementId ?? string.Empty;
            RewardId = rewardId ?? string.Empty;
            RewardAmount = rewardAmount < 0 ? 0 : rewardAmount;
        }
    }
}
