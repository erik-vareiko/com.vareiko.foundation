using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Zenject;

namespace Vareiko.Foundation.Iap
{
    public sealed class SimulatedInAppPurchaseService : IInAppPurchaseService, IInitializable
    {
        private readonly IapConfig _config;
        private readonly SignalBus _signalBus;
        private readonly Dictionary<string, IapConfig.ProductDefinition> _productsById = new Dictionary<string, IapConfig.ProductDefinition>(StringComparer.Ordinal);
        private readonly Dictionary<string, InAppPurchaseResult> _ownedPurchases = new Dictionary<string, InAppPurchaseResult>(StringComparer.Ordinal);
        private readonly List<InAppPurchaseProductInfo> _catalog = new List<InAppPurchaseProductInfo>(16);

        private bool _initialized;

        [Inject]
        public SimulatedInAppPurchaseService([InjectOptional] IapConfig config = null, [InjectOptional] SignalBus signalBus = null)
        {
            _config = config;
            _signalBus = signalBus;
        }

        public InAppPurchaseProviderType Provider => InAppPurchaseProviderType.Simulated;
        public bool IsConfigured => _config != null && _config.Provider == InAppPurchaseProviderType.Simulated && _config.Products != null && _config.Products.Count > 0;
        public bool IsInitialized => _initialized;

        public void Initialize()
        {
            if (_config != null && _config.AutoInitializeOnStart)
            {
                InitializeAsync().Forget();
            }
        }

        public UniTask<InAppPurchaseInitializeResult> InitializeAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (_config == null)
            {
                return UniTask.FromResult(FailInitialize("IAP config is not assigned.", InAppPurchaseErrorCode.ConfigurationInvalid));
            }

            if (_config.Provider != InAppPurchaseProviderType.Simulated)
            {
                return UniTask.FromResult(FailInitialize("IAP provider is not set to Simulated.", InAppPurchaseErrorCode.ProviderUnavailable));
            }

            _productsById.Clear();
            _catalog.Clear();

            IReadOnlyList<IapConfig.ProductDefinition> products = _config.Products;
            for (int i = 0; i < products.Count; i++)
            {
                IapConfig.ProductDefinition product = products[i];
                if (product == null || !product.Enabled)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(product.ProductId))
                {
                    return UniTask.FromResult(FailInitialize("IAP product has empty id.", InAppPurchaseErrorCode.ConfigurationInvalid));
                }

                if (_productsById.ContainsKey(product.ProductId))
                {
                    return UniTask.FromResult(FailInitialize($"Duplicate IAP product id '{product.ProductId}'.", InAppPurchaseErrorCode.ConfigurationInvalid));
                }

                _productsById[product.ProductId] = product;
                _catalog.Add(new InAppPurchaseProductInfo(
                    product.ProductId,
                    product.ProductType,
                    product.LocalizedTitle,
                    product.LocalizedDescription,
                    product.Price,
                    product.IsoCurrencyCode,
                    product.LocalizedPriceString));
            }

            if (_catalog.Count == 0)
            {
                return UniTask.FromResult(FailInitialize("IAP catalog is empty.", InAppPurchaseErrorCode.ConfigurationInvalid));
            }

            _ownedPurchases.Clear();
            _initialized = true;
            InAppPurchaseInitializeResult success = InAppPurchaseInitializeResult.Succeed();
            _signalBus?.Fire(new IapInitializedSignal(true, string.Empty));
            return UniTask.FromResult(success);
        }

        public UniTask<IReadOnlyList<InAppPurchaseProductInfo>> GetCatalogAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return UniTask.FromResult((IReadOnlyList<InAppPurchaseProductInfo>)new List<InAppPurchaseProductInfo>(_catalog));
        }

        public UniTask<InAppPurchaseResult> PurchaseAsync(string productId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_initialized)
            {
                return UniTask.FromResult(FailPurchase(productId, "IAP service is not initialized.", InAppPurchaseErrorCode.NotInitialized));
            }

            if (string.IsNullOrWhiteSpace(productId))
            {
                return UniTask.FromResult(FailPurchase(string.Empty, "IAP product id is empty.", InAppPurchaseErrorCode.ValidationFailed));
            }

            string trimmedId = productId.Trim();
            IapConfig.ProductDefinition product;
            if (!_productsById.TryGetValue(trimmedId, out product))
            {
                return UniTask.FromResult(FailPurchase(trimmedId, "IAP product not found in catalog.", InAppPurchaseErrorCode.ProductNotFound));
            }

            bool isOwnedType = product.ProductType == InAppPurchaseProductType.NonConsumable || product.ProductType == InAppPurchaseProductType.Subscription;
            if (isOwnedType && _config.SimulateAlreadyOwnedAsFailure && _ownedPurchases.ContainsKey(trimmedId))
            {
                return UniTask.FromResult(FailPurchase(trimmedId, "IAP product is already owned.", InAppPurchaseErrorCode.AlreadyOwned));
            }

            string transactionId = "SIM-" + Guid.NewGuid().ToString("N");
            string receipt = $"SIMULATED:{trimmedId}:{transactionId}";
            InAppPurchaseResult success = InAppPurchaseResult.Succeed(trimmedId, transactionId, receipt, false);

            if (isOwnedType)
            {
                _ownedPurchases[trimmedId] = success;
            }

            _signalBus?.Fire(new IapPurchaseSucceededSignal(trimmedId, transactionId, false));
            return UniTask.FromResult(success);
        }

        public UniTask<InAppPurchaseRestoreResult> RestorePurchasesAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!_initialized)
            {
                InAppPurchaseRestoreResult failed = InAppPurchaseRestoreResult.Fail("IAP service is not initialized.", InAppPurchaseErrorCode.NotInitialized);
                _signalBus?.Fire(new IapRestoreFailedSignal(failed.Error, failed.ErrorCode));
                return UniTask.FromResult(failed);
            }

            int restored = 0;
            foreach (KeyValuePair<string, InAppPurchaseResult> pair in _ownedPurchases)
            {
                InAppPurchaseResult owned = pair.Value;
                _signalBus?.Fire(new IapPurchaseSucceededSignal(owned.ProductId, owned.TransactionId, true));
                restored++;
            }

            InAppPurchaseRestoreResult result = InAppPurchaseRestoreResult.Succeed(restored);
            _signalBus?.Fire(new IapRestoreCompletedSignal(restored));
            return UniTask.FromResult(result);
        }

        private InAppPurchaseInitializeResult FailInitialize(string error, InAppPurchaseErrorCode errorCode)
        {
            _initialized = false;
            InAppPurchaseInitializeResult result = InAppPurchaseInitializeResult.Fail(error, errorCode);
            _signalBus?.Fire(new IapInitializedSignal(false, result.Error));
            return result;
        }

        private InAppPurchaseResult FailPurchase(string productId, string error, InAppPurchaseErrorCode errorCode)
        {
            InAppPurchaseResult result = InAppPurchaseResult.Fail(productId, error, errorCode);
            _signalBus?.Fire(new IapPurchaseFailedSignal(result.ProductId, result.Error, result.ErrorCode));
            return result;
        }
    }
}
