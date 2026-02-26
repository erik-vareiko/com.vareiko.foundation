using Cysharp.Threading.Tasks;

namespace Vareiko.Foundation.UI
{
    public interface IUIConfirmDialogService
    {
        UniTask<UIWindowResult> ShowAsync(
            string windowId,
            UIConfirmDialogRequest request,
            bool instant = true,
            int priority = 0,
            bool allowDuplicate = false);
    }
}
