using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Vareiko.Foundation.Ads
{
    public sealed class NullAdsService : IAdsService
    {
        private static readonly IReadOnlyList<string> EmptyPlacements = new List<string>(0);

        public AdsProviderType Provider => AdsProviderType.None;
        public bool IsConfigured => false;
        public bool IsInitialized => false;

        public UniTask<AdsInitializeResult> InitializeAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return UniTask.FromResult(AdsInitializeResult.Fail("Ads provider is not configured.", AdsErrorCode.ConfigurationInvalid));
        }

        public UniTask<IReadOnlyList<string>> GetPlacementIdsAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return UniTask.FromResult(EmptyPlacements);
        }

        public UniTask<AdLoadResult> LoadAsync(string placementId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return UniTask.FromResult(AdLoadResult.Fail(placementId, AdPlacementType.Interstitial, "Ads provider is not configured.", AdsErrorCode.ProviderUnavailable));
        }

        public UniTask<AdShowResult> ShowAsync(string placementId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return UniTask.FromResult(AdShowResult.Fail(placementId, AdPlacementType.Interstitial, "Ads provider is not configured.", AdsErrorCode.ProviderUnavailable));
        }
    }
}
