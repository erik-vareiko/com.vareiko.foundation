using System;
using System.Collections.Generic;

namespace Vareiko.Foundation.Backend
{
    public enum BackendErrorCode
    {
        None = 0,
        Unknown = 1,
        ConfigurationInvalid = 2,
        DependencyMissing = 3,
        ValidationFailed = 4,
        AuthenticationRequired = 5,
        ProviderUnavailable = 6
    }

    [Serializable]
    public readonly struct BackendAuthResult
    {
        public readonly bool Success;
        public readonly string PlayerId;
        public readonly string Error;
        public readonly BackendErrorCode ErrorCode;
        public readonly bool IsRetryable;

        public BackendAuthResult(bool success, string playerId, string error)
            : this(
                success,
                playerId,
                error,
                success ? BackendErrorCode.None : BackendErrorCode.Unknown,
                false)
        {
        }

        public BackendAuthResult(bool success, string playerId, string error, BackendErrorCode errorCode, bool isRetryable = false)
        {
            Success = success;
            PlayerId = playerId ?? string.Empty;
            Error = error ?? string.Empty;
            ErrorCode = success ? BackendErrorCode.None : (errorCode == BackendErrorCode.None ? BackendErrorCode.Unknown : errorCode);
            IsRetryable = !success && isRetryable;
        }

        public static BackendAuthResult Succeed(string playerId)
        {
            return new BackendAuthResult(true, playerId ?? string.Empty, string.Empty, BackendErrorCode.None, false);
        }

        public static BackendAuthResult Fail(string error, BackendErrorCode errorCode, bool isRetryable = false)
        {
            return new BackendAuthResult(false, string.Empty, error ?? string.Empty, errorCode, isRetryable);
        }
    }

    [Serializable]
    public readonly struct BackendPlayerDataResult
    {
        public readonly bool Success;
        public readonly IReadOnlyDictionary<string, string> Data;
        public readonly string Error;
        public readonly BackendErrorCode ErrorCode;
        public readonly bool IsRetryable;

        public BackendPlayerDataResult(bool success, IReadOnlyDictionary<string, string> data, string error)
            : this(
                success,
                data,
                error,
                success ? BackendErrorCode.None : BackendErrorCode.Unknown,
                false)
        {
        }

        public BackendPlayerDataResult(
            bool success,
            IReadOnlyDictionary<string, string> data,
            string error,
            BackendErrorCode errorCode,
            bool isRetryable = false)
        {
            Success = success;
            Data = data;
            Error = error ?? string.Empty;
            ErrorCode = success ? BackendErrorCode.None : (errorCode == BackendErrorCode.None ? BackendErrorCode.Unknown : errorCode);
            IsRetryable = !success && isRetryable;
        }

        public static BackendPlayerDataResult Succeed(IReadOnlyDictionary<string, string> data)
        {
            return new BackendPlayerDataResult(true, data, string.Empty, BackendErrorCode.None, false);
        }

        public static BackendPlayerDataResult Fail(string error, BackendErrorCode errorCode, bool isRetryable = false)
        {
            return new BackendPlayerDataResult(false, null, error ?? string.Empty, errorCode, isRetryable);
        }
    }
}
