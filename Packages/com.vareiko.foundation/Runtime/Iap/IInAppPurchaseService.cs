using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;

namespace Vareiko.Foundation.Iap
{
    public interface IInAppPurchaseService
    {
        InAppPurchaseProviderType Provider { get; }
        bool IsConfigured { get; }
        bool IsInitialized { get; }
        UniTask<InAppPurchaseInitializeResult> InitializeAsync(CancellationToken cancellationToken = default);
        UniTask<IReadOnlyList<InAppPurchaseProductInfo>> GetCatalogAsync(CancellationToken cancellationToken = default);
        UniTask<InAppPurchaseResult> PurchaseAsync(string productId, CancellationToken cancellationToken = default);
        UniTask<InAppPurchaseRestoreResult> RestorePurchasesAsync(CancellationToken cancellationToken = default);
    }
}
