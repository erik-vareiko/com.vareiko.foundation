namespace Vareiko.Foundation.UI
{
    public readonly struct UIWindowQueuedSignal
    {
        public readonly string WindowId;
        public readonly int QueueCount;
        public readonly int Priority;

        public UIWindowQueuedSignal(string windowId, int queueCount, int priority)
        {
            WindowId = windowId ?? string.Empty;
            QueueCount = queueCount;
            Priority = priority;
        }
    }

    public readonly struct UIWindowShownSignal
    {
        public readonly string WindowId;

        public UIWindowShownSignal(string windowId)
        {
            WindowId = windowId ?? string.Empty;
        }
    }

    public readonly struct UIWindowClosedSignal
    {
        public readonly string WindowId;
        public readonly bool HasNext;

        public UIWindowClosedSignal(string windowId, bool hasNext)
        {
            WindowId = windowId ?? string.Empty;
            HasNext = hasNext;
        }
    }

    public readonly struct UIWindowQueueDrainedSignal
    {
    }
}
