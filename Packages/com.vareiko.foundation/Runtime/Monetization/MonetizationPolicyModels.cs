using System;
using Vareiko.Foundation.Ads;

namespace Vareiko.Foundation.Monetization
{
    public enum MonetizationPolicyBlockReason
    {
        None = 0,
        InvalidInput = 1,
        PlacementNotConfigured = 2,
        ProductNotConfigured = 3,
        CooldownActive = 4,
        SessionCapReached = 5
    }

    [Serializable]
    public readonly struct MonetizationAdDecision
    {
        public readonly bool Allowed;
        public readonly string PlacementId;
        public readonly AdPlacementType PlacementType;
        public readonly MonetizationPolicyBlockReason BlockReason;
        public readonly string Message;
        public readonly float RetryAfterSeconds;

        public MonetizationAdDecision(
            bool allowed,
            string placementId,
            AdPlacementType placementType,
            MonetizationPolicyBlockReason blockReason,
            string message,
            float retryAfterSeconds)
        {
            Allowed = allowed;
            PlacementId = placementId ?? string.Empty;
            PlacementType = placementType;
            BlockReason = allowed ? MonetizationPolicyBlockReason.None : blockReason;
            Message = message ?? string.Empty;
            RetryAfterSeconds = allowed ? 0f : Math.Max(0f, retryAfterSeconds);
        }

        public static MonetizationAdDecision Allow(string placementId, AdPlacementType placementType)
        {
            return new MonetizationAdDecision(true, placementId, placementType, MonetizationPolicyBlockReason.None, string.Empty, 0f);
        }

        public static MonetizationAdDecision Block(
            string placementId,
            AdPlacementType placementType,
            MonetizationPolicyBlockReason blockReason,
            string message,
            float retryAfterSeconds = 0f)
        {
            return new MonetizationAdDecision(false, placementId, placementType, blockReason, message, retryAfterSeconds);
        }
    }

    [Serializable]
    public readonly struct MonetizationIapDecision
    {
        public readonly bool Allowed;
        public readonly string ProductId;
        public readonly MonetizationPolicyBlockReason BlockReason;
        public readonly string Message;
        public readonly float RetryAfterSeconds;

        public MonetizationIapDecision(
            bool allowed,
            string productId,
            MonetizationPolicyBlockReason blockReason,
            string message,
            float retryAfterSeconds)
        {
            Allowed = allowed;
            ProductId = productId ?? string.Empty;
            BlockReason = allowed ? MonetizationPolicyBlockReason.None : blockReason;
            Message = message ?? string.Empty;
            RetryAfterSeconds = allowed ? 0f : Math.Max(0f, retryAfterSeconds);
        }

        public static MonetizationIapDecision Allow(string productId)
        {
            return new MonetizationIapDecision(true, productId, MonetizationPolicyBlockReason.None, string.Empty, 0f);
        }

        public static MonetizationIapDecision Block(
            string productId,
            MonetizationPolicyBlockReason blockReason,
            string message,
            float retryAfterSeconds = 0f)
        {
            return new MonetizationIapDecision(false, productId, blockReason, message, retryAfterSeconds);
        }
    }
}
