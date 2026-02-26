using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Vareiko.Foundation.Tests.TestDoubles;
using Vareiko.Foundation.UI;

namespace Vareiko.Foundation.Tests.UI
{
    public sealed class UIConfirmDialogServiceTests
    {
        [Test]
        public void ShowAsync_AppliesRequest_AndReturnsFallbackConfirmPayload()
        {
            GameObject root = new GameObject("ConfirmDialogServiceRoot");
            try
            {
                UIWindow window = root.AddComponent<UIWindow>();
                ReflectionTestUtil.SetPrivateField(window, "_id", "window.confirm");

                UIConfirmDialogPresenter presenter = root.AddComponent<UIConfirmDialogPresenter>();
                Text title = root.AddComponent<Text>();
                ReflectionTestUtil.SetPrivateField(presenter, "_titleText", title);

                FakeUIService uiService = new FakeUIService(window);
                FakeWindowResultService windowResultService = new FakeWindowResultService(
                    new UIWindowResult("window.confirm", UIWindowResultStatus.Confirmed, string.Empty));
                UIConfirmDialogService service = new UIConfirmDialogService(uiService, windowResultService);

                UIConfirmDialogRequest request = new UIConfirmDialogRequest(
                    "Buy Offer?",
                    "Spend 100 gems?",
                    confirmPayload: "offer_a");

                UIWindowResult result = service.ShowAsync("window.confirm", request).GetAwaiter().GetResult();

                Assert.That(title.text, Is.EqualTo("Buy Offer?"));
                Assert.That(windowResultService.EnqueueCalls, Is.EqualTo(1));
                Assert.That(windowResultService.LastWindowId, Is.EqualTo("window.confirm"));
                Assert.That(result.Status, Is.EqualTo(UIWindowResultStatus.Confirmed));
                Assert.That(result.Payload, Is.EqualTo("offer_a"));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ShowAsync_WhenResultAlreadyHasPayload_DoesNotOverride()
        {
            GameObject root = new GameObject("ConfirmDialogServiceRoot2");
            try
            {
                UIWindow window = root.AddComponent<UIWindow>();
                ReflectionTestUtil.SetPrivateField(window, "_id", "window.confirm");
                UIConfirmDialogPresenter presenter = root.AddComponent<UIConfirmDialogPresenter>();
                ReflectionTestUtil.SetPrivateField(presenter, "_titleText", root.AddComponent<Text>());

                FakeUIService uiService = new FakeUIService(window);
                FakeWindowResultService windowResultService = new FakeWindowResultService(
                    new UIWindowResult("window.confirm", UIWindowResultStatus.Canceled, "from-ui"));
                UIConfirmDialogService service = new UIConfirmDialogService(uiService, windowResultService);

                UIConfirmDialogRequest request = new UIConfirmDialogRequest(
                    "Delete Save?",
                    "This cannot be undone.",
                    cancelPayload: "fallback_cancel");

                UIWindowResult result = service.ShowAsync("window.confirm", request).GetAwaiter().GetResult();

                Assert.That(result.Status, Is.EqualTo(UIWindowResultStatus.Canceled));
                Assert.That(result.Payload, Is.EqualTo("from-ui"));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private sealed class FakeUIService : IUIService
        {
            private readonly UIWindow _window;

            public FakeUIService(UIWindow window)
            {
                _window = window;
            }

            public bool Show(string elementId, bool instant = true)
            {
                return true;
            }

            public bool Hide(string elementId, bool instant = true)
            {
                return true;
            }

            public bool Toggle(string elementId, bool instant = true)
            {
                return true;
            }

            public void HideAll(bool instant = true)
            {
            }

            public bool TryGetElement(string elementId, out UIElement element)
            {
                element = _window;
                return true;
            }

            public bool TryGetScreen(string screenId, out UIScreen screen)
            {
                screen = null;
                return false;
            }

            public bool TryGetWindow(string windowId, out UIWindow window)
            {
                if (_window != null && string.Equals(_window.Id, windowId, System.StringComparison.Ordinal))
                {
                    window = _window;
                    return true;
                }

                window = null;
                return false;
            }
        }

        private sealed class FakeWindowResultService : IUIWindowResultService
        {
            private readonly UIWindowResult _result;
            public int EnqueueCalls;
            public string LastWindowId = string.Empty;

            public FakeWindowResultService(UIWindowResult result)
            {
                _result = result;
            }

            public Cysharp.Threading.Tasks.UniTask<UIWindowResult> EnqueueAndWaitAsync(string windowId, bool instant = true, int priority = 0, bool allowDuplicate = false)
            {
                EnqueueCalls++;
                LastWindowId = windowId;
                return Cysharp.Threading.Tasks.UniTask.FromResult(_result);
            }

            public bool TryResolveCurrent(UIWindowResultStatus status, string payload = "", bool instant = true)
            {
                return true;
            }

            public bool TryResolve(string windowId, UIWindowResultStatus status, string payload = "", bool instant = true)
            {
                return true;
            }
        }
    }
}
