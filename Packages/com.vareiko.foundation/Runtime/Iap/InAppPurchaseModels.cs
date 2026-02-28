using System;

namespace Vareiko.Foundation.Iap
{
    public enum InAppPurchaseProductType
    {
        Consumable = 0,
        NonConsumable = 1,
        Subscription = 2
    }

    public enum InAppPurchaseErrorCode
    {
        None = 0,
        Unknown = 1,
        ConfigurationInvalid = 2,
        ValidationFailed = 3,
        ProviderUnavailable = 4,
        NotInitialized = 5,
        ProductNotFound = 6,
        AlreadyOwned = 7,
        PurchaseFailed = 8,
        RestoreFailed = 9
    }

    [Serializable]
    public readonly struct InAppPurchaseInitializeResult
    {
        public readonly bool Success;
        public readonly string Error;
        public readonly InAppPurchaseErrorCode ErrorCode;

        public InAppPurchaseInitializeResult(bool success, string error, InAppPurchaseErrorCode errorCode)
        {
            Success = success;
            Error = error ?? string.Empty;
            ErrorCode = success ? InAppPurchaseErrorCode.None : (errorCode == InAppPurchaseErrorCode.None ? InAppPurchaseErrorCode.Unknown : errorCode);
        }

        public static InAppPurchaseInitializeResult Succeed()
        {
            return new InAppPurchaseInitializeResult(true, string.Empty, InAppPurchaseErrorCode.None);
        }

        public static InAppPurchaseInitializeResult Fail(string error, InAppPurchaseErrorCode errorCode)
        {
            return new InAppPurchaseInitializeResult(false, error ?? "IAP initialization failed.", errorCode);
        }
    }

    [Serializable]
    public readonly struct InAppPurchaseProductInfo
    {
        public readonly string ProductId;
        public readonly InAppPurchaseProductType ProductType;
        public readonly string LocalizedTitle;
        public readonly string LocalizedDescription;
        public readonly double Price;
        public readonly string IsoCurrencyCode;
        public readonly string LocalizedPriceString;

        public InAppPurchaseProductInfo(
            string productId,
            InAppPurchaseProductType productType,
            string localizedTitle,
            string localizedDescription,
            double price,
            string isoCurrencyCode,
            string localizedPriceString)
        {
            ProductId = productId ?? string.Empty;
            ProductType = productType;
            LocalizedTitle = localizedTitle ?? string.Empty;
            LocalizedDescription = localizedDescription ?? string.Empty;
            Price = price < 0d ? 0d : price;
            IsoCurrencyCode = isoCurrencyCode ?? string.Empty;
            LocalizedPriceString = localizedPriceString ?? string.Empty;
        }
    }

    [Serializable]
    public readonly struct InAppPurchaseResult
    {
        public readonly bool Success;
        public readonly string ProductId;
        public readonly string TransactionId;
        public readonly string Receipt;
        public readonly bool IsRestored;
        public readonly string Error;
        public readonly InAppPurchaseErrorCode ErrorCode;

        public InAppPurchaseResult(
            bool success,
            string productId,
            string transactionId,
            string receipt,
            bool isRestored,
            string error,
            InAppPurchaseErrorCode errorCode)
        {
            Success = success;
            ProductId = productId ?? string.Empty;
            TransactionId = transactionId ?? string.Empty;
            Receipt = receipt ?? string.Empty;
            IsRestored = success && isRestored;
            Error = error ?? string.Empty;
            ErrorCode = success ? InAppPurchaseErrorCode.None : (errorCode == InAppPurchaseErrorCode.None ? InAppPurchaseErrorCode.Unknown : errorCode);
        }

        public static InAppPurchaseResult Succeed(string productId, string transactionId, string receipt, bool isRestored = false)
        {
            return new InAppPurchaseResult(
                true,
                productId,
                transactionId,
                receipt,
                isRestored,
                string.Empty,
                InAppPurchaseErrorCode.None);
        }

        public static InAppPurchaseResult Fail(string productId, string error, InAppPurchaseErrorCode errorCode)
        {
            return new InAppPurchaseResult(
                false,
                productId,
                string.Empty,
                string.Empty,
                false,
                error ?? "IAP purchase failed.",
                errorCode);
        }
    }

    [Serializable]
    public readonly struct InAppPurchaseRestoreResult
    {
        public readonly bool Success;
        public readonly int RestoredCount;
        public readonly string Error;
        public readonly InAppPurchaseErrorCode ErrorCode;

        public InAppPurchaseRestoreResult(bool success, int restoredCount, string error, InAppPurchaseErrorCode errorCode)
        {
            Success = success;
            RestoredCount = success ? Math.Max(0, restoredCount) : 0;
            Error = error ?? string.Empty;
            ErrorCode = success ? InAppPurchaseErrorCode.None : (errorCode == InAppPurchaseErrorCode.None ? InAppPurchaseErrorCode.Unknown : errorCode);
        }

        public static InAppPurchaseRestoreResult Succeed(int restoredCount)
        {
            return new InAppPurchaseRestoreResult(true, restoredCount, string.Empty, InAppPurchaseErrorCode.None);
        }

        public static InAppPurchaseRestoreResult Fail(string error, InAppPurchaseErrorCode errorCode)
        {
            return new InAppPurchaseRestoreResult(false, 0, error ?? "IAP restore failed.", errorCode);
        }
    }
}
