namespace Vareiko.Foundation.Attribution
{
    public readonly struct AttributionInitializedSignal
    {
        public readonly bool Success;
        public readonly string Error;

        public AttributionInitializedSignal(bool success, string error)
        {
            Success = success;
            Error = error ?? string.Empty;
        }
    }

    public readonly struct AttributionEventTrackedSignal
    {
        public readonly string EventName;

        public AttributionEventTrackedSignal(string eventName)
        {
            EventName = eventName ?? string.Empty;
        }
    }

    public readonly struct AttributionEventTrackFailedSignal
    {
        public readonly string EventName;
        public readonly string Error;
        public readonly AttributionErrorCode ErrorCode;

        public AttributionEventTrackFailedSignal(string eventName, string error, AttributionErrorCode errorCode)
        {
            EventName = eventName ?? string.Empty;
            Error = error ?? string.Empty;
            ErrorCode = errorCode;
        }
    }

    public readonly struct AttributionRevenueTrackedSignal
    {
        public readonly string ProductId;
        public readonly string Currency;
        public readonly double Amount;
        public readonly string TransactionId;

        public AttributionRevenueTrackedSignal(string productId, string currency, double amount, string transactionId)
        {
            ProductId = productId ?? string.Empty;
            Currency = currency ?? string.Empty;
            Amount = amount < 0d ? 0d : amount;
            TransactionId = transactionId ?? string.Empty;
        }
    }

    public readonly struct AttributionRevenueTrackFailedSignal
    {
        public readonly string ProductId;
        public readonly string Error;
        public readonly AttributionErrorCode ErrorCode;

        public AttributionRevenueTrackFailedSignal(string productId, string error, AttributionErrorCode errorCode)
        {
            ProductId = productId ?? string.Empty;
            Error = error ?? string.Empty;
            ErrorCode = errorCode;
        }
    }
}
