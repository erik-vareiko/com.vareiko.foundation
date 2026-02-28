using System.Threading;
using Cysharp.Threading.Tasks;
using Vareiko.Foundation.Ads;

namespace Vareiko.Foundation.Monetization
{
    public interface IMonetizationPolicyService
    {
        UniTask<MonetizationAdDecision> CanShowAdAsync(string placementId, AdPlacementType placementType, CancellationToken cancellationToken = default);
        UniTask RecordAdShownAsync(string placementId, AdPlacementType placementType, CancellationToken cancellationToken = default);
        UniTask<MonetizationIapDecision> CanStartPurchaseAsync(string productId, CancellationToken cancellationToken = default);
        UniTask RecordPurchaseAsync(string productId, CancellationToken cancellationToken = default);
        UniTask ResetSessionAsync(CancellationToken cancellationToken = default);
    }
}
