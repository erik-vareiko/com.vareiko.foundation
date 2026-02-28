using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Vareiko.Foundation.Iap
{
    public sealed class NullInAppPurchaseService : IInAppPurchaseService
    {
        private static readonly IReadOnlyList<InAppPurchaseProductInfo> EmptyCatalog = new List<InAppPurchaseProductInfo>(0);

        public InAppPurchaseProviderType Provider => InAppPurchaseProviderType.None;
        public bool IsConfigured => false;
        public bool IsInitialized => false;

        public UniTask<InAppPurchaseInitializeResult> InitializeAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return UniTask.FromResult(
                InAppPurchaseInitializeResult.Fail("IAP provider is not configured.", InAppPurchaseErrorCode.ConfigurationInvalid));
        }

        public UniTask<IReadOnlyList<InAppPurchaseProductInfo>> GetCatalogAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return UniTask.FromResult(EmptyCatalog);
        }

        public UniTask<InAppPurchaseResult> PurchaseAsync(string productId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return UniTask.FromResult(
                InAppPurchaseResult.Fail(productId, "IAP provider is not configured.", InAppPurchaseErrorCode.ProviderUnavailable));
        }

        public UniTask<InAppPurchaseRestoreResult> RestorePurchasesAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return UniTask.FromResult(
                InAppPurchaseRestoreResult.Fail("IAP provider is not configured.", InAppPurchaseErrorCode.ProviderUnavailable));
        }
    }
}
