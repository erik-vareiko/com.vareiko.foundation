namespace Vareiko.Foundation.Analytics
{
    public readonly struct AnalyticsEventTrackedSignal
    {
        public readonly string EventName;

        public AnalyticsEventTrackedSignal(string eventName)
        {
            EventName = eventName;
        }
    }

    public readonly struct AnalyticsEventDroppedSignal
    {
        public readonly string EventName;
        public readonly string Reason;

        public AnalyticsEventDroppedSignal(string eventName, string reason)
        {
            EventName = eventName;
            Reason = reason;
        }
    }
}
