using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Vareiko.Foundation.Ads
{
    public interface IAdsService
    {
        AdsProviderType Provider { get; }
        bool IsConfigured { get; }
        bool IsInitialized { get; }
        UniTask<AdsInitializeResult> InitializeAsync(CancellationToken cancellationToken = default);
        UniTask<IReadOnlyList<string>> GetPlacementIdsAsync(CancellationToken cancellationToken = default);
        UniTask<AdLoadResult> LoadAsync(string placementId, CancellationToken cancellationToken = default);
        UniTask<AdShowResult> ShowAsync(string placementId, CancellationToken cancellationToken = default);
    }
}
