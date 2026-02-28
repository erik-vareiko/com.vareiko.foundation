using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Vareiko.Foundation.Attribution
{
    public sealed class NullAttributionService : IAttributionService
    {
        public AttributionProviderType Provider => AttributionProviderType.None;
        public bool IsConfigured => false;
        public bool IsInitialized => false;

        public UniTask<AttributionInitializeResult> InitializeAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return UniTask.FromResult(
                AttributionInitializeResult.Fail("Attribution provider is not configured.", AttributionErrorCode.ConfigurationInvalid));
        }

        public void SetUserId(string userId)
        {
        }

        public UniTask<AttributionTrackResult> TrackEventAsync(
            string eventName,
            IReadOnlyDictionary<string, string> properties = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return UniTask.FromResult(
                AttributionTrackResult.Fail(eventName, "Attribution provider is not configured.", AttributionErrorCode.ProviderUnavailable));
        }

        public UniTask<AttributionRevenueTrackResult> TrackRevenueAsync(
            AttributionRevenueData revenueData,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return UniTask.FromResult(
                AttributionRevenueTrackResult.Fail(
                    revenueData.ProductId,
                    revenueData.Currency,
                    revenueData.Amount,
                    "Attribution provider is not configured.",
                    AttributionErrorCode.ProviderUnavailable));
        }
    }
}
