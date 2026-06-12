using System;

namespace Vareiko.Foundation.Attribution
{
    public enum AttributionErrorCode
    {
        None = 0,
        Unknown = 1,
        ConfigurationInvalid = 2,
        ProviderUnavailable = 3,
        ConsentDenied = 4,
        NotInitialized = 5,
        InvalidPayload = 6,
        TrackFailed = 7
    }

    [Serializable]
    public readonly struct AttributionInitializeResult
    {
        public readonly bool Success;
        public readonly string Error;
        public readonly AttributionErrorCode ErrorCode;

        public AttributionInitializeResult(bool success, string error, AttributionErrorCode errorCode)
        {
            Success = success;
            Error = error ?? string.Empty;
            ErrorCode = success ? AttributionErrorCode.None : (errorCode == AttributionErrorCode.None ? AttributionErrorCode.Unknown : errorCode);
        }

        public static AttributionInitializeResult Succeed()
        {
            return new AttributionInitializeResult(true, string.Empty, AttributionErrorCode.None);
        }

        public static AttributionInitializeResult Fail(string error, AttributionErrorCode errorCode)
        {
            return new AttributionInitializeResult(false, error ?? "Attribution initialization failed.", errorCode);
        }
    }

    [Serializable]
    public readonly struct AttributionTrackResult
    {
        public readonly bool Success;
        public readonly string EventName;
        public readonly string Error;
        public readonly AttributionErrorCode ErrorCode;

        public AttributionTrackResult(bool success, string eventName, string error, AttributionErrorCode errorCode)
        {
            Success = success;
            EventName = eventName ?? string.Empty;
            Error = error ?? string.Empty;
            ErrorCode = success ? AttributionErrorCode.None : (errorCode == AttributionErrorCode.None ? AttributionErrorCode.Unknown : errorCode);
        }

        public static AttributionTrackResult Succeed(string eventName)
        {
            return new AttributionTrackResult(true, eventName, string.Empty, AttributionErrorCode.None);
        }

        public static AttributionTrackResult Fail(string eventName, string error, AttributionErrorCode errorCode)
        {
            return new AttributionTrackResult(false, eventName, error ?? "Attribution event tracking failed.", errorCode);
        }
    }

    [Serializable]
    public readonly struct AttributionRevenueData
    {
        public readonly string ProductId;
        public readonly string Currency;
        public readonly double Amount;
        public readonly string TransactionId;

        public AttributionRevenueData(string productId, string currency, double amount, string transactionId)
        {
            ProductId = productId ?? string.Empty;
            Currency = currency ?? string.Empty;
            Amount = amount < 0d ? 0d : amount;
            TransactionId = transactionId ?? string.Empty;
        }

        public bool IsValid =>
            !string.IsNullOrWhiteSpace(ProductId) &&
            !string.IsNullOrWhiteSpace(Currency) &&
            Amount > 0d;
    }

    [Serializable]
    public readonly struct AttributionRevenueTrackResult
    {
        public readonly bool Success;
        public readonly string ProductId;
        public readonly string Currency;
        public readonly double Amount;
        public readonly string Error;
        public readonly AttributionErrorCode ErrorCode;

        public AttributionRevenueTrackResult(
            bool success,
            string productId,
            string currency,
            double amount,
            string error,
            AttributionErrorCode errorCode)
        {
            Success = success;
            ProductId = productId ?? string.Empty;
            Currency = currency ?? string.Empty;
            Amount = amount < 0d ? 0d : amount;
            Error = error ?? string.Empty;
            ErrorCode = success ? AttributionErrorCode.None : (errorCode == AttributionErrorCode.None ? AttributionErrorCode.Unknown : errorCode);
        }

        public static AttributionRevenueTrackResult Succeed(string productId, string currency, double amount)
        {
            return new AttributionRevenueTrackResult(true, productId, currency, amount, string.Empty, AttributionErrorCode.None);
        }

        public static AttributionRevenueTrackResult Fail(
            string productId,
            string currency,
            double amount,
            string error,
            AttributionErrorCode errorCode)
        {
            return new AttributionRevenueTrackResult(
                false,
                productId,
                currency,
                amount,
                error ?? "Attribution revenue tracking failed.",
                errorCode);
        }
    }
}
