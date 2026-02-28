namespace Vareiko.Foundation.Iap
{
    public readonly struct IapInitializedSignal
    {
        public readonly bool Success;
        public readonly string Error;

        public IapInitializedSignal(bool success, string error)
        {
            Success = success;
            Error = error ?? string.Empty;
        }
    }

    public readonly struct IapPurchaseSucceededSignal
    {
        public readonly string ProductId;
        public readonly string TransactionId;
        public readonly bool IsRestored;

        public IapPurchaseSucceededSignal(string productId, string transactionId, bool isRestored)
        {
            ProductId = productId ?? string.Empty;
            TransactionId = transactionId ?? string.Empty;
            IsRestored = isRestored;
        }
    }

    public readonly struct IapPurchaseFailedSignal
    {
        public readonly string ProductId;
        public readonly string Error;
        public readonly InAppPurchaseErrorCode ErrorCode;

        public IapPurchaseFailedSignal(string productId, string error, InAppPurchaseErrorCode errorCode)
        {
            ProductId = productId ?? string.Empty;
            Error = error ?? string.Empty;
            ErrorCode = errorCode;
        }
    }

    public readonly struct IapRestoreCompletedSignal
    {
        public readonly int RestoredCount;

        public IapRestoreCompletedSignal(int restoredCount)
        {
            RestoredCount = restoredCount < 0 ? 0 : restoredCount;
        }
    }

    public readonly struct IapRestoreFailedSignal
    {
        public readonly string Error;
        public readonly InAppPurchaseErrorCode ErrorCode;

        public IapRestoreFailedSignal(string error, InAppPurchaseErrorCode errorCode)
        {
            Error = error ?? string.Empty;
            ErrorCode = errorCode;
        }
    }
}
