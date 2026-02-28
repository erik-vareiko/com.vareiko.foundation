namespace Vareiko.Foundation.Observability
{
    public sealed class MonetizationObservabilitySnapshot
    {
        public int IapPurchaseSuccessCount;
        public int IapPurchaseFailureCount;
        public float IapPurchaseLastLatencyMs;
        public float IapPurchaseAvgLatencyMs;

        public int AdShowSuccessCount;
        public int AdShowFailureCount;
        public float AdShowLastLatencyMs;
        public float AdShowAvgLatencyMs;

        public int PushPermissionGrantedCount;
        public int PushPermissionDeniedCount;
        public float PushPermissionLastLatencyMs;
        public float PushPermissionAvgLatencyMs;

        public int PushTopicSubscribeSuccessCount;
        public int PushTopicSubscribeFailureCount;
    }
}
