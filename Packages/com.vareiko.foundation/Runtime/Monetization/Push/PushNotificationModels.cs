using System;

namespace Vareiko.Foundation.Push
{
    public enum PushNotificationPermissionStatus
    {
        Unknown = 0,
        Granted = 1,
        Denied = 2
    }

    public enum PushNotificationErrorCode
    {
        None = 0,
        Unknown = 1,
        ConfigurationInvalid = 2,
        ProviderUnavailable = 3,
        ConsentDenied = 4,
        NotInitialized = 5,
        PermissionDenied = 6,
        TopicInvalid = 7,
        TopicNotSubscribed = 8,
        OperationFailed = 9
    }

    [Serializable]
    public readonly struct PushInitializeResult
    {
        public readonly bool Success;
        public readonly string Error;
        public readonly PushNotificationErrorCode ErrorCode;

        public PushInitializeResult(bool success, string error, PushNotificationErrorCode errorCode)
        {
            Success = success;
            Error = error ?? string.Empty;
            ErrorCode = success ? PushNotificationErrorCode.None : (errorCode == PushNotificationErrorCode.None ? PushNotificationErrorCode.Unknown : errorCode);
        }

        public static PushInitializeResult Succeed()
        {
            return new PushInitializeResult(true, string.Empty, PushNotificationErrorCode.None);
        }

        public static PushInitializeResult Fail(string error, PushNotificationErrorCode errorCode)
        {
            return new PushInitializeResult(false, error ?? "Push initialization failed.", errorCode);
        }
    }

    [Serializable]
    public readonly struct PushPermissionResult
    {
        public readonly bool Success;
        public readonly PushNotificationPermissionStatus Status;
        public readonly string Error;
        public readonly PushNotificationErrorCode ErrorCode;

        public PushPermissionResult(bool success, PushNotificationPermissionStatus status, string error, PushNotificationErrorCode errorCode)
        {
            Success = success;
            Status = success ? PushNotificationPermissionStatus.Granted : status;
            Error = error ?? string.Empty;
            ErrorCode = success ? PushNotificationErrorCode.None : (errorCode == PushNotificationErrorCode.None ? PushNotificationErrorCode.Unknown : errorCode);
        }

        public static PushPermissionResult Succeed()
        {
            return new PushPermissionResult(true, PushNotificationPermissionStatus.Granted, string.Empty, PushNotificationErrorCode.None);
        }

        public static PushPermissionResult Fail(PushNotificationPermissionStatus status, string error, PushNotificationErrorCode errorCode)
        {
            return new PushPermissionResult(false, status, error ?? "Push permission request failed.", errorCode);
        }
    }

    [Serializable]
    public readonly struct PushDeviceTokenResult
    {
        public readonly bool Success;
        public readonly string DeviceToken;
        public readonly string Error;
        public readonly PushNotificationErrorCode ErrorCode;

        public PushDeviceTokenResult(bool success, string deviceToken, string error, PushNotificationErrorCode errorCode)
        {
            Success = success;
            DeviceToken = success ? (deviceToken ?? string.Empty) : string.Empty;
            Error = error ?? string.Empty;
            ErrorCode = success ? PushNotificationErrorCode.None : (errorCode == PushNotificationErrorCode.None ? PushNotificationErrorCode.Unknown : errorCode);
        }

        public static PushDeviceTokenResult Succeed(string deviceToken)
        {
            return new PushDeviceTokenResult(true, deviceToken, string.Empty, PushNotificationErrorCode.None);
        }

        public static PushDeviceTokenResult Fail(string error, PushNotificationErrorCode errorCode)
        {
            return new PushDeviceTokenResult(false, string.Empty, error ?? "Push device token request failed.", errorCode);
        }
    }

    [Serializable]
    public readonly struct PushTopicResult
    {
        public readonly bool Success;
        public readonly string Topic;
        public readonly bool IsSubscribed;
        public readonly string Error;
        public readonly PushNotificationErrorCode ErrorCode;

        public PushTopicResult(bool success, string topic, bool isSubscribed, string error, PushNotificationErrorCode errorCode)
        {
            Success = success;
            Topic = topic ?? string.Empty;
            IsSubscribed = success && isSubscribed;
            Error = error ?? string.Empty;
            ErrorCode = success ? PushNotificationErrorCode.None : (errorCode == PushNotificationErrorCode.None ? PushNotificationErrorCode.Unknown : errorCode);
        }

        public static PushTopicResult Succeed(string topic, bool isSubscribed)
        {
            return new PushTopicResult(true, topic, isSubscribed, string.Empty, PushNotificationErrorCode.None);
        }

        public static PushTopicResult Fail(string topic, string error, PushNotificationErrorCode errorCode)
        {
            return new PushTopicResult(false, topic, false, error ?? "Push topic operation failed.", errorCode);
        }
    }
}
