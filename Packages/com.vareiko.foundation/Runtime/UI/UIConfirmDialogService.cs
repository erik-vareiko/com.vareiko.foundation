using Cysharp.Threading.Tasks;
using Zenject;

namespace Vareiko.Foundation.UI
{
    public sealed class UIConfirmDialogService : IUIConfirmDialogService
    {
        private readonly IUIService _uiService;
        private readonly IUIWindowResultService _windowResultService;

        [Inject]
        public UIConfirmDialogService(IUIService uiService, IUIWindowResultService windowResultService)
        {
            _uiService = uiService;
            _windowResultService = windowResultService;
        }

        public async UniTask<UIWindowResult> ShowAsync(
            string windowId,
            UIConfirmDialogRequest request,
            bool instant = true,
            int priority = 0,
            bool allowDuplicate = false)
        {
            if (string.IsNullOrWhiteSpace(windowId))
            {
                return new UIWindowResult(string.Empty, UIWindowResultStatus.Rejected);
            }

            string normalizedWindowId = windowId.Trim();
            if (_uiService.TryGetWindow(normalizedWindowId, out UIWindow window))
            {
                UIConfirmDialogPresenter presenter = window.GetComponent<UIConfirmDialogPresenter>();
                presenter?.Apply(request);
            }

            UIWindowResult result = await _windowResultService.EnqueueAndWaitAsync(
                normalizedWindowId,
                instant,
                priority,
                allowDuplicate);

            if (!string.IsNullOrEmpty(result.Payload))
            {
                return result;
            }

            if (result.Status == UIWindowResultStatus.Confirmed && !string.IsNullOrEmpty(request.ConfirmPayload))
            {
                return new UIWindowResult(result.WindowId, result.Status, request.ConfirmPayload);
            }

            if (result.Status == UIWindowResultStatus.Canceled && !string.IsNullOrEmpty(request.CancelPayload))
            {
                return new UIWindowResult(result.WindowId, result.Status, request.CancelPayload);
            }

            return result;
        }
    }
}
