using Vareiko.Foundation.Ads;

namespace Vareiko.Foundation.Monetization
{
    public readonly struct MonetizationAdBlockedSignal
    {
        public readonly string PlacementId;
        public readonly AdPlacementType PlacementType;
        public readonly MonetizationPolicyBlockReason Reason;
        public readonly string Message;
        public readonly float RetryAfterSeconds;

        public MonetizationAdBlockedSignal(string placementId, AdPlacementType placementType, MonetizationPolicyBlockReason reason, string message, float retryAfterSeconds)
        {
            PlacementId = placementId ?? string.Empty;
            PlacementType = placementType;
            Reason = reason;
            Message = message ?? string.Empty;
            RetryAfterSeconds = retryAfterSeconds < 0f ? 0f : retryAfterSeconds;
        }
    }

    public readonly struct MonetizationAdRecordedSignal
    {
        public readonly string PlacementId;
        public readonly AdPlacementType PlacementType;
        public readonly int SessionCount;

        public MonetizationAdRecordedSignal(string placementId, AdPlacementType placementType, int sessionCount)
        {
            PlacementId = placementId ?? string.Empty;
            PlacementType = placementType;
            SessionCount = sessionCount < 0 ? 0 : sessionCount;
        }
    }

    public readonly struct MonetizationIapBlockedSignal
    {
        public readonly string ProductId;
        public readonly MonetizationPolicyBlockReason Reason;
        public readonly string Message;
        public readonly float RetryAfterSeconds;

        public MonetizationIapBlockedSignal(string productId, MonetizationPolicyBlockReason reason, string message, float retryAfterSeconds)
        {
            ProductId = productId ?? string.Empty;
            Reason = reason;
            Message = message ?? string.Empty;
            RetryAfterSeconds = retryAfterSeconds < 0f ? 0f : retryAfterSeconds;
        }
    }

    public readonly struct MonetizationIapRecordedSignal
    {
        public readonly string ProductId;
        public readonly int SessionCount;

        public MonetizationIapRecordedSignal(string productId, int sessionCount)
        {
            ProductId = productId ?? string.Empty;
            SessionCount = sessionCount < 0 ? 0 : sessionCount;
        }
    }

    public readonly struct MonetizationSessionResetSignal
    {
    }
}
