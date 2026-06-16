using System;

namespace Vareiko.Foundation.Ads
{
    public enum AdPlacementType
    {
        Rewarded = 0,
        Interstitial = 1
    }

    public enum AdsErrorCode
    {
        None = 0,
        Unknown = 1,
        ConfigurationInvalid = 2,
        ProviderUnavailable = 3,
        ConsentDenied = 4,
        NotInitialized = 5,
        PlacementNotFound = 6,
        PlacementNotLoaded = 7,
        LoadFailed = 8,
        ShowFailed = 9
    }

    [Serializable]
    public readonly struct AdsInitializeResult
    {
        public readonly bool Success;
        public readonly string Error;
        public readonly AdsErrorCode ErrorCode;

        public AdsInitializeResult(bool success, string error, AdsErrorCode errorCode)
        {
            Success = success;
            Error = error ?? string.Empty;
            ErrorCode = success ? AdsErrorCode.None : (errorCode == AdsErrorCode.None ? AdsErrorCode.Unknown : errorCode);
        }

        public static AdsInitializeResult Succeed()
        {
            return new AdsInitializeResult(true, string.Empty, AdsErrorCode.None);
        }

        public static AdsInitializeResult Fail(string error, AdsErrorCode errorCode)
        {
            return new AdsInitializeResult(false, error ?? "Ads initialization failed.", errorCode);
        }
    }

    [Serializable]
    public readonly struct AdLoadResult
    {
        public readonly bool Success;
        public readonly string PlacementId;
        public readonly AdPlacementType PlacementType;
        public readonly string Error;
        public readonly AdsErrorCode ErrorCode;

        public AdLoadResult(bool success, string placementId, AdPlacementType placementType, string error, AdsErrorCode errorCode)
        {
            Success = success;
            PlacementId = placementId ?? string.Empty;
            PlacementType = placementType;
            Error = error ?? string.Empty;
            ErrorCode = success ? AdsErrorCode.None : (errorCode == AdsErrorCode.None ? AdsErrorCode.Unknown : errorCode);
        }

        public static AdLoadResult Succeed(string placementId, AdPlacementType placementType)
        {
            return new AdLoadResult(true, placementId, placementType, string.Empty, AdsErrorCode.None);
        }

        public static AdLoadResult Fail(string placementId, AdPlacementType placementType, string error, AdsErrorCode errorCode)
        {
            return new AdLoadResult(false, placementId, placementType, error ?? "Ad load failed.", errorCode);
        }
    }

    [Serializable]
    public readonly struct AdShowResult
    {
        public readonly bool Success;
        public readonly string PlacementId;
        public readonly AdPlacementType PlacementType;
        public readonly bool RewardGranted;
        public readonly string RewardId;
        public readonly int RewardAmount;
        public readonly string Error;
        public readonly AdsErrorCode ErrorCode;

        public AdShowResult(
            bool success,
            string placementId,
            AdPlacementType placementType,
            bool rewardGranted,
            string rewardId,
            int rewardAmount,
            string error,
            AdsErrorCode errorCode)
        {
            Success = success;
            PlacementId = placementId ?? string.Empty;
            PlacementType = placementType;
            RewardGranted = success && rewardGranted;
            RewardId = rewardId ?? string.Empty;
            RewardAmount = success ? Math.Max(0, rewardAmount) : 0;
            Error = error ?? string.Empty;
            ErrorCode = success ? AdsErrorCode.None : (errorCode == AdsErrorCode.None ? AdsErrorCode.Unknown : errorCode);
        }

        public static AdShowResult Succeed(string placementId, AdPlacementType placementType, bool rewardGranted, string rewardId, int rewardAmount)
        {
            return new AdShowResult(
                true,
                placementId,
                placementType,
                rewardGranted,
                rewardId,
                rewardAmount,
                string.Empty,
                AdsErrorCode.None);
        }

        public static AdShowResult Fail(string placementId, AdPlacementType placementType, string error, AdsErrorCode errorCode)
        {
            return new AdShowResult(
                false,
                placementId,
                placementType,
                false,
                string.Empty,
                0,
                error ?? "Ad show failed.",
                errorCode);
        }
    }
}
