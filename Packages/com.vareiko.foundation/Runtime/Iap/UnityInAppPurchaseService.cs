using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

#if UNITY_PURCHASING
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
#endif

namespace Vareiko.Foundation.Iap
{
    public sealed class UnityInAppPurchaseService : IInAppPurchaseService, IInitializable
#if UNITY_PURCHASING
        , IStoreListener
#endif
    {
        private readonly IapConfig _config;
        private readonly SignalBus _signalBus;
        private readonly Dictionary<string, IapConfig.ProductDefinition> _productsById = new Dictionary<string, IapConfig.ProductDefinition>(StringComparer.Ordinal);
        private readonly List<InAppPurchaseProductInfo> _catalog = new List<InAppPurchaseProductInfo>(16);

        private bool _initialized;
        private bool _initializationInProgress;
        private UniTaskCompletionSource<InAppPurchaseInitializeResult> _initializeCompletion;
        private UniTaskCompletionSource<InAppPurchaseResult> _purchaseCompletion;
        private UniTaskCompletionSource<InAppPurchaseRestoreResult> _restoreCompletion;
        private string _activePurchaseProductId = string.Empty;
        private bool _restoreInProgress;
        private int _restoredTransactionsCount;

#if UNITY_PURCHASING
        private IStoreController _storeController;
        private IExtensionProvider _extensionProvider;
#endif

        [Inject]
        public UnityInAppPurchaseService([InjectOptional] IapConfig config = null, [InjectOptional] SignalBus signalBus = null)
        {
            _config = config;
            _signalBus = signalBus;
        }

        public InAppPurchaseProviderType Provider => InAppPurchaseProviderType.UnityIap;
        public bool IsConfigured =>
            _config != null &&
            _config.Provider == InAppPurchaseProviderType.UnityIap &&
            _config.Products != null &&
            _config.Products.Count > 0;
        public bool IsInitialized => _initialized;

        public void Initialize()
        {
            if (_config != null && _config.AutoInitializeOnStart)
            {
                InitializeAsync().Forget();
            }
        }

        public async UniTask<InAppPurchaseInitializeResult> InitializeAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            float startedAt = Time.realtimeSinceStartup;

            InAppPurchaseInitializeResult FinalizeResult(InAppPurchaseInitializeResult result)
            {
                EmitTelemetry("initialize", result.Success, result.ErrorCode, startedAt);
                return result;
            }

            if (_initialized)
            {
                return FinalizeResult(InAppPurchaseInitializeResult.Succeed());
            }

            if (_config == null)
            {
                return FinalizeResult(FailInitialize("IAP config is not assigned.", InAppPurchaseErrorCode.ConfigurationInvalid));
            }

            if (_config.Provider != InAppPurchaseProviderType.UnityIap)
            {
                return FinalizeResult(FailInitialize("IAP provider is not set to UnityIap.", InAppPurchaseErrorCode.ProviderUnavailable));
            }

            if (!BuildCatalogFromConfig(out InAppPurchaseInitializeResult validationFailure))
            {
                return FinalizeResult(validationFailure);
            }

#if UNITY_PURCHASING
            if (_initializationInProgress && _initializeCompletion != null)
            {
                try
                {
                    InAppPurchaseInitializeResult pendingResult = await _initializeCompletion.Task.AttachExternalCancellation(cancellationToken);
                    return FinalizeResult(pendingResult);
                }
                catch (OperationCanceledException)
                {
                    throw;
                }
            }

            _initializationInProgress = true;
            _initializeCompletion = new UniTaskCompletionSource<InAppPurchaseInitializeResult>();

            try
            {
                ConfigurationBuilder builder = ConfigurationBuilder.Instance(StandardPurchasingModule.Instance());
                foreach (KeyValuePair<string, IapConfig.ProductDefinition> pair in _productsById)
                {
                    builder.AddProduct(pair.Key, MapProductType(pair.Value.ProductType));
                }

                UnityPurchasing.Initialize(this, builder);

                InAppPurchaseInitializeResult result = await _initializeCompletion.Task.AttachExternalCancellation(cancellationToken);
                return FinalizeResult(result);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return FinalizeResult(FailInitialize("Unity IAP initialization failed unexpectedly.", InAppPurchaseErrorCode.ProviderUnavailable));
            }
            finally
            {
                _initializationInProgress = false;
            }
#else
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            return FinalizeResult(FailInitialize("UNITY_PURCHASING is not enabled. Install Unity IAP package.", InAppPurchaseErrorCode.ProviderUnavailable));
#endif
        }

        public UniTask<IReadOnlyList<InAppPurchaseProductInfo>> GetCatalogAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return UniTask.FromResult((IReadOnlyList<InAppPurchaseProductInfo>)new List<InAppPurchaseProductInfo>(_catalog));
        }

        public async UniTask<InAppPurchaseResult> PurchaseAsync(string productId, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            float startedAt = Time.realtimeSinceStartup;

            InAppPurchaseResult FinalizeResult(InAppPurchaseResult result)
            {
                EmitTelemetry("purchase", result.Success, result.ErrorCode, startedAt);
                return result;
            }

            if (!_initialized)
            {
                return FinalizeResult(FailPurchase(productId, "IAP service is not initialized.", InAppPurchaseErrorCode.NotInitialized));
            }

            if (string.IsNullOrWhiteSpace(productId))
            {
                return FinalizeResult(FailPurchase(string.Empty, "IAP product id is empty.", InAppPurchaseErrorCode.ValidationFailed));
            }

            string trimmedId = productId.Trim();
            if (!_productsById.ContainsKey(trimmedId))
            {
                return FinalizeResult(FailPurchase(trimmedId, "IAP product not found in catalog.", InAppPurchaseErrorCode.ProductNotFound));
            }

#if UNITY_PURCHASING
            if (_storeController == null)
            {
                return FinalizeResult(FailPurchase(trimmedId, "Unity IAP store controller is not ready.", InAppPurchaseErrorCode.ProviderUnavailable));
            }

            if (_purchaseCompletion != null)
            {
                return FinalizeResult(FailPurchase(trimmedId, "IAP purchase flow is already in progress.", InAppPurchaseErrorCode.PurchaseFailed));
            }

            _activePurchaseProductId = trimmedId;
            _purchaseCompletion = new UniTaskCompletionSource<InAppPurchaseResult>();

            try
            {
                _storeController.InitiatePurchase(trimmedId);
                InAppPurchaseResult result = await _purchaseCompletion.Task.AttachExternalCancellation(cancellationToken);
                return FinalizeResult(result);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                _purchaseCompletion = null;
                _activePurchaseProductId = string.Empty;
                return FinalizeResult(FailPurchase(trimmedId, "Unity IAP purchase failed unexpectedly.", InAppPurchaseErrorCode.PurchaseFailed));
            }
#else
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            return FinalizeResult(FailPurchase(trimmedId, "UNITY_PURCHASING is not enabled. Install Unity IAP package.", InAppPurchaseErrorCode.ProviderUnavailable));
#endif
        }

        public async UniTask<InAppPurchaseRestoreResult> RestorePurchasesAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            float startedAt = Time.realtimeSinceStartup;

            InAppPurchaseRestoreResult FinalizeResult(InAppPurchaseRestoreResult result)
            {
                EmitTelemetry("restore", result.Success, result.ErrorCode, startedAt);
                return result;
            }

            if (!_initialized)
            {
                return FinalizeResult(FailRestore("IAP service is not initialized.", InAppPurchaseErrorCode.NotInitialized));
            }

#if UNITY_PURCHASING
            if (_restoreCompletion != null)
            {
                return FinalizeResult(FailRestore("IAP restore flow is already in progress.", InAppPurchaseErrorCode.RestoreFailed));
            }

#if UNITY_IOS || UNITY_TVOS || UNITY_STANDALONE_OSX
            if (_extensionProvider == null)
            {
                return FinalizeResult(FailRestore("Unity IAP extension provider is not ready.", InAppPurchaseErrorCode.ProviderUnavailable));
            }

            IAppleExtensions appleExtensions = _extensionProvider.GetExtension<IAppleExtensions>();
            if (appleExtensions == null)
            {
                return FinalizeResult(FailRestore("Unity IAP Apple restore extension is unavailable.", InAppPurchaseErrorCode.ProviderUnavailable));
            }

            _restoreInProgress = true;
            _restoredTransactionsCount = 0;
            _restoreCompletion = new UniTaskCompletionSource<InAppPurchaseRestoreResult>();

            try
            {
                appleExtensions.RestoreTransactions(OnAppleRestoreFinished);
                InAppPurchaseRestoreResult result = await _restoreCompletion.Task.AttachExternalCancellation(cancellationToken);
                return FinalizeResult(result);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                _restoreCompletion = null;
                _restoreInProgress = false;
                return FinalizeResult(FailRestore("Unity IAP restore failed unexpectedly.", InAppPurchaseErrorCode.RestoreFailed));
            }
#else
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            InAppPurchaseRestoreResult result = InAppPurchaseRestoreResult.Succeed(0);
            _signalBus?.Fire(new IapRestoreCompletedSignal(result.RestoredCount));
            return FinalizeResult(result);
#endif
#else
            await UniTask.Yield(PlayerLoopTiming.Update, cancellationToken);
            return FinalizeResult(FailRestore("UNITY_PURCHASING is not enabled. Install Unity IAP package.", InAppPurchaseErrorCode.ProviderUnavailable));
#endif
        }

#if UNITY_PURCHASING
        public void OnInitialized(IStoreController controller, IExtensionProvider extensions)
        {
            _storeController = controller;
            _extensionProvider = extensions;
            _initialized = true;

            RefreshCatalogFromStore();

            InAppPurchaseInitializeResult result = InAppPurchaseInitializeResult.Succeed();
            _signalBus?.Fire(new IapInitializedSignal(true, string.Empty));
            _initializeCompletion?.TrySetResult(result);
            _initializeCompletion = null;
        }

        public void OnInitializeFailed(InitializationFailureReason error)
        {
            OnInitializeFailed(error, error.ToString());
        }

        public void OnInitializeFailed(InitializationFailureReason error, string message)
        {
            string details = string.IsNullOrWhiteSpace(message) ? error.ToString() : message;
            InAppPurchaseInitializeResult failed = FailInitialize($"Unity IAP initialization failed: {details}", InAppPurchaseErrorCode.ProviderUnavailable);
            _initializeCompletion?.TrySetResult(failed);
            _initializeCompletion = null;
        }

        public PurchaseProcessingResult ProcessPurchase(PurchaseEventArgs args)
        {
            if (args == null || args.purchasedProduct == null)
            {
                CompletePurchaseFailure("", "Unity IAP purchase callback received empty product.", InAppPurchaseErrorCode.PurchaseFailed);
                return PurchaseProcessingResult.Complete;
            }

            Product product = args.purchasedProduct;
            string productId = product.definition != null ? product.definition.id : string.Empty;
            string transactionId = product.transactionID ?? string.Empty;
            string receipt = product.receipt ?? string.Empty;

            if (_restoreInProgress)
            {
                _restoredTransactionsCount++;
                _signalBus?.Fire(new IapPurchaseSucceededSignal(productId, transactionId, true));
                return PurchaseProcessingResult.Complete;
            }

            InAppPurchaseResult success = InAppPurchaseResult.Succeed(productId, transactionId, receipt, false);
            _signalBus?.Fire(new IapPurchaseSucceededSignal(productId, transactionId, false));

            _purchaseCompletion?.TrySetResult(success);
            _purchaseCompletion = null;
            _activePurchaseProductId = string.Empty;
            return PurchaseProcessingResult.Complete;
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureReason failureReason)
        {
            string productId = product != null && product.definition != null ? product.definition.id : _activePurchaseProductId;
            CompletePurchaseFailure(productId, $"Unity IAP purchase failed: {failureReason}", InAppPurchaseErrorCode.PurchaseFailed);
        }

        public void OnPurchaseFailed(Product product, PurchaseFailureDescription failureDescription)
        {
            string productId = product != null && product.definition != null ? product.definition.id : _activePurchaseProductId;
            string details = failureDescription != null ? failureDescription.message : string.Empty;
            if (string.IsNullOrWhiteSpace(details) && failureDescription != null)
            {
                details = failureDescription.reason.ToString();
            }

            CompletePurchaseFailure(productId, $"Unity IAP purchase failed: {details}", InAppPurchaseErrorCode.PurchaseFailed);
        }

        private void OnAppleRestoreFinished(bool success)
        {
            if (_restoreCompletion == null)
            {
                _restoreInProgress = false;
                return;
            }

            if (success)
            {
                InAppPurchaseRestoreResult result = InAppPurchaseRestoreResult.Succeed(_restoredTransactionsCount);
                _signalBus?.Fire(new IapRestoreCompletedSignal(result.RestoredCount));
                _restoreCompletion.TrySetResult(result);
            }
            else
            {
                InAppPurchaseRestoreResult failed = FailRestore("Unity IAP restore failed.", InAppPurchaseErrorCode.RestoreFailed);
                _restoreCompletion.TrySetResult(failed);
            }

            _restoreCompletion = null;
            _restoreInProgress = false;
            _restoredTransactionsCount = 0;
        }

        private void RefreshCatalogFromStore()
        {
            if (_storeController == null || _storeController.products == null)
            {
                return;
            }

            ProductCollection products = _storeController.products;
            for (int i = 0; i < _catalog.Count; i++)
            {
                InAppPurchaseProductInfo existing = _catalog[i];
                Product product = products.WithID(existing.ProductId);
                if (product == null || product.metadata == null)
                {
                    continue;
                }

                string title = string.IsNullOrWhiteSpace(product.metadata.localizedTitle) ? existing.LocalizedTitle : product.metadata.localizedTitle;
                string description = string.IsNullOrWhiteSpace(product.metadata.localizedDescription) ? existing.LocalizedDescription : product.metadata.localizedDescription;
                string isoCurrency = string.IsNullOrWhiteSpace(product.metadata.isoCurrencyCode) ? existing.IsoCurrencyCode : product.metadata.isoCurrencyCode;
                string localizedPrice = string.IsNullOrWhiteSpace(product.metadata.localizedPriceString) ? existing.LocalizedPriceString : product.metadata.localizedPriceString;
                double price = (double)product.metadata.localizedPrice;

                _catalog[i] = new InAppPurchaseProductInfo(
                    existing.ProductId,
                    existing.ProductType,
                    title,
                    description,
                    price,
                    isoCurrency,
                    localizedPrice);
            }
        }

        private static ProductType MapProductType(InAppPurchaseProductType productType)
        {
            switch (productType)
            {
                case InAppPurchaseProductType.NonConsumable:
                    return ProductType.NonConsumable;
                case InAppPurchaseProductType.Subscription:
                    return ProductType.Subscription;
                default:
                    return ProductType.Consumable;
            }
        }

        private void CompletePurchaseFailure(string productId, string error, InAppPurchaseErrorCode errorCode)
        {
            InAppPurchaseResult failed = FailPurchase(productId, error, errorCode);
            _purchaseCompletion?.TrySetResult(failed);
            _purchaseCompletion = null;
            _activePurchaseProductId = string.Empty;
        }
#endif

        private bool BuildCatalogFromConfig(out InAppPurchaseInitializeResult failure)
        {
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
                    failure = FailInitialize("IAP product has empty id.", InAppPurchaseErrorCode.ConfigurationInvalid);
                    return false;
                }

                if (_productsById.ContainsKey(product.ProductId))
                {
                    failure = FailInitialize($"Duplicate IAP product id '{product.ProductId}'.", InAppPurchaseErrorCode.ConfigurationInvalid);
                    return false;
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
                failure = FailInitialize("IAP catalog is empty.", InAppPurchaseErrorCode.ConfigurationInvalid);
                return false;
            }

            failure = default;
            return true;
        }

        private InAppPurchaseInitializeResult FailInitialize(string error, InAppPurchaseErrorCode errorCode)
        {
            _initialized = false;
#if UNITY_PURCHASING
            _storeController = null;
            _extensionProvider = null;
#endif
            InAppPurchaseInitializeResult failed = InAppPurchaseInitializeResult.Fail(error, errorCode);
            _signalBus?.Fire(new IapInitializedSignal(false, failed.Error));
            return failed;
        }

        private InAppPurchaseResult FailPurchase(string productId, string error, InAppPurchaseErrorCode errorCode)
        {
            InAppPurchaseResult failed = InAppPurchaseResult.Fail(productId, error, errorCode);
            _signalBus?.Fire(new IapPurchaseFailedSignal(failed.ProductId, failed.Error, failed.ErrorCode));
            return failed;
        }

        private InAppPurchaseRestoreResult FailRestore(string error, InAppPurchaseErrorCode errorCode)
        {
            InAppPurchaseRestoreResult failed = InAppPurchaseRestoreResult.Fail(error, errorCode);
            _signalBus?.Fire(new IapRestoreFailedSignal(failed.Error, failed.ErrorCode));
            return failed;
        }

        private void EmitTelemetry(string operation, bool success, InAppPurchaseErrorCode errorCode, float startedAt)
        {
            float elapsedMs = Mathf.Max(0f, (Time.realtimeSinceStartup - startedAt) * 1000f);
            _signalBus?.Fire(new IapOperationTelemetrySignal(operation, success, elapsedMs, errorCode));
        }
    }
}
