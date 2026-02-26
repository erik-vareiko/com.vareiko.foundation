using Cysharp.Threading.Tasks;

namespace Vareiko.Foundation.UI
{
    public interface IUIWindowResultService
    {
        UniTask<UIWindowResult> EnqueueAndWaitAsync(string windowId, bool instant = true, int priority = 0, bool allowDuplicate = false);
        bool TryResolveCurrent(UIWindowResultStatus status, string payload = "", bool instant = true);
        bool TryResolve(string windowId, UIWindowResultStatus status, string payload = "", bool instant = true);
    }
}
