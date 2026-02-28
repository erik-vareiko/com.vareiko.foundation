using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Vareiko.Foundation.Attribution
{
    public interface IAttributionService
    {
        AttributionProviderType Provider { get; }
        bool IsConfigured { get; }
        bool IsInitialized { get; }
        UniTask<AttributionInitializeResult> InitializeAsync(CancellationToken cancellationToken = default);
        void SetUserId(string userId);
        UniTask<AttributionTrackResult> TrackEventAsync(
            string eventName,
            IReadOnlyDictionary<string, string> properties = null,
            CancellationToken cancellationToken = default);
        UniTask<AttributionRevenueTrackResult> TrackRevenueAsync(
            AttributionRevenueData revenueData,
            CancellationToken cancellationToken = default);
    }
}
