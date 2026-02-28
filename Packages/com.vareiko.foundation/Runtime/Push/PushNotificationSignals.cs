namespace Vareiko.Foundation.Push
{
    public readonly struct PushInitializedSignal
    {
        public readonly bool Success;
        public readonly string Error;

        public PushInitializedSignal(bool success, string error)
        {
            Success = success;
            Error = error ?? string.Empty;
        }
    }

    public readonly struct PushPermissionChangedSignal
    {
        public readonly PushNotificationPermissionStatus Status;

        public PushPermissionChangedSignal(PushNotificationPermissionStatus status)
        {
            Status = status;
        }
    }

    public readonly struct PushTokenUpdatedSignal
    {
        public readonly string DeviceToken;

        public PushTokenUpdatedSignal(string deviceToken)
        {
            DeviceToken = deviceToken ?? string.Empty;
        }
    }

    public readonly struct PushTopicSubscribedSignal
    {
        public readonly string Topic;

        public PushTopicSubscribedSignal(string topic)
        {
            Topic = topic ?? string.Empty;
        }
    }

    public readonly struct PushTopicSubscriptionFailedSignal
    {
        public readonly string Topic;
        public readonly string Error;
        public readonly PushNotificationErrorCode ErrorCode;

        public PushTopicSubscriptionFailedSignal(string topic, string error, PushNotificationErrorCode errorCode)
        {
            Topic = topic ?? string.Empty;
            Error = error ?? string.Empty;
            ErrorCode = errorCode;
        }
    }

    public readonly struct PushTopicUnsubscribedSignal
    {
        public readonly string Topic;

        public PushTopicUnsubscribedSignal(string topic)
        {
            Topic = topic ?? string.Empty;
        }
    }

    public readonly struct PushTopicUnsubscriptionFailedSignal
    {
        public readonly string Topic;
        public readonly string Error;
        public readonly PushNotificationErrorCode ErrorCode;

        public PushTopicUnsubscriptionFailedSignal(string topic, string error, PushNotificationErrorCode errorCode)
        {
            Topic = topic ?? string.Empty;
            Error = error ?? string.Empty;
            ErrorCode = errorCode;
        }
    }
}
