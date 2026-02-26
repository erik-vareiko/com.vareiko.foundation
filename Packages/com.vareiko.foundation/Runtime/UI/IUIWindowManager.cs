namespace Vareiko.Foundation.UI
{
    public interface IUIWindowManager
    {
        string CurrentWindowId { get; }
        int QueueCount { get; }
        bool Enqueue(string windowId, bool instant = true, int priority = 0, bool allowDuplicate = false);
        bool TryCloseCurrent(bool instant = true);
        bool TryClose(string windowId, bool instant = true);
        void ClearQueue();
    }
}
