using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Vareiko.Foundation.Tests.TestDoubles;
using Vareiko.Foundation.UI;

namespace Vareiko.Foundation.Tests.UI
{
    public sealed class UIConfirmDialogPresenterTests
    {
        [Test]
        public void Apply_UpdatesTexts_AndButtonsResolveWithPayloads()
        {
            FakeWindowResultService resultService = new FakeWindowResultService();
            GameObject root = new GameObject("ConfirmDialogPresenterRoot");
            root.SetActive(false);

            try
            {
                UIConfirmDialogPresenter presenter = root.AddComponent<UIConfirmDialogPresenter>();

                Text title = CreateText(root.transform, "Title", "Default Title");
                Text message = CreateText(root.transform, "Message", "Default Message");
                Text confirmLabel = CreateText(root.transform, "ConfirmLabel", "Yes");
                Text cancelLabel = CreateText(root.transform, "CancelLabel", "No");

                UIButtonView confirmButton = CreateButton(root.transform, "ConfirmButton");
                UIButtonView cancelButton = CreateButton(root.transform, "CancelButton");

                ReflectionTestUtil.SetPrivateField(presenter, "_titleText", title);
                ReflectionTestUtil.SetPrivateField(presenter, "_messageText", message);
                ReflectionTestUtil.SetPrivateField(presenter, "_confirmButtonLabelText", confirmLabel);
                ReflectionTestUtil.SetPrivateField(presenter, "_cancelButtonLabelText", cancelLabel);
                ReflectionTestUtil.SetPrivateField(presenter, "_confirmButton", confirmButton);
                ReflectionTestUtil.SetPrivateField(presenter, "_cancelButton", cancelButton);
                ReflectionTestUtil.SetPrivateField(presenter, "_instant", false);

                presenter.Construct(resultService);

                root.SetActive(true);

                UIConfirmDialogRequest request = new UIConfirmDialogRequest(
                    "Quit Game?",
                    "All unsaved progress will be lost.",
                    "Quit",
                    "Stay",
                    "confirm_quit",
                    "cancel_quit");

                presenter.Apply(request);

                Assert.That(title.text, Is.EqualTo("Quit Game?"));
                Assert.That(message.text, Is.EqualTo("All unsaved progress will be lost."));
                Assert.That(confirmLabel.text, Is.EqualTo("Quit"));
                Assert.That(cancelLabel.text, Is.EqualTo("Stay"));

                confirmButton.Click();
                Assert.That(resultService.ResolveCurrentCalls, Is.EqualTo(1));
                Assert.That(resultService.LastStatus, Is.EqualTo(UIWindowResultStatus.Confirmed));
                Assert.That(resultService.LastPayload, Is.EqualTo("confirm_quit"));
                Assert.That(resultService.LastInstant, Is.False);

                cancelButton.Click();
                Assert.That(resultService.ResolveCurrentCalls, Is.EqualTo(2));
                Assert.That(resultService.LastStatus, Is.EqualTo(UIWindowResultStatus.Canceled));
                Assert.That(resultService.LastPayload, Is.EqualTo("cancel_quit"));
                Assert.That(resultService.LastInstant, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static Text CreateText(Transform parent, string name, string initialValue)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            Text text = go.AddComponent<Text>();
            text.text = initialValue;
            return text;
        }

        private static UIButtonView CreateButton(Transform parent, string name)
        {
            GameObject go = new GameObject(name);
            go.transform.SetParent(parent, false);
            return go.AddComponent<UIButtonView>();
        }

        private sealed class FakeWindowResultService : IUIWindowResultService
        {
            public int ResolveCurrentCalls;
            public UIWindowResultStatus LastStatus;
            public string LastPayload = string.Empty;
            public bool LastInstant;

            public Cysharp.Threading.Tasks.UniTask<UIWindowResult> EnqueueAndWaitAsync(string windowId, bool instant = true, int priority = 0, bool allowDuplicate = false)
            {
                return Cysharp.Threading.Tasks.UniTask.FromResult(new UIWindowResult(windowId, UIWindowResultStatus.Rejected));
            }

            public bool TryResolveCurrent(UIWindowResultStatus status, string payload = "", bool instant = true)
            {
                ResolveCurrentCalls++;
                LastStatus = status;
                LastPayload = payload;
                LastInstant = instant;
                return true;
            }

            public bool TryResolve(string windowId, UIWindowResultStatus status, string payload = "", bool instant = true)
            {
                return false;
            }
        }
    }
}
