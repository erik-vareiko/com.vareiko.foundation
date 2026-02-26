using NUnit.Framework;
using UnityEngine;
using Vareiko.Foundation.Tests.TestDoubles;
using Vareiko.Foundation.UI;

namespace Vareiko.Foundation.Tests.UI
{
    public sealed class UIWindowResolveButtonActionTests
    {
        [Test]
        public void Click_DefaultConfig_ResolvesCurrent()
        {
            FakeWindowResultService service = new FakeWindowResultService();
            GameObject root = new GameObject("ResolveActionRoot");
            root.SetActive(false);

            try
            {
                UIButtonView button = root.AddComponent<UIButtonView>();
                UIWindowResolveButtonAction action = root.AddComponent<UIWindowResolveButtonAction>();

                ReflectionTestUtil.SetPrivateField(action, "_button", button);
                ReflectionTestUtil.SetPrivateField(action, "_resolveSpecificWindow", false);
                ReflectionTestUtil.SetPrivateField(action, "_status", UIWindowResultStatus.Canceled);
                ReflectionTestUtil.SetPrivateField(action, "_payload", "from-button");
                ReflectionTestUtil.SetPrivateField(action, "_instant", false);

                action.Construct(service);

                root.SetActive(true);
                button.Click();

                Assert.That(service.ResolveCurrentCalls, Is.EqualTo(1));
                Assert.That(service.LastCurrentStatus, Is.EqualTo(UIWindowResultStatus.Canceled));
                Assert.That(service.LastCurrentPayload, Is.EqualTo("from-button"));
                Assert.That(service.LastCurrentInstant, Is.False);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Click_SpecificWindow_ResolvesById()
        {
            FakeWindowResultService service = new FakeWindowResultService();
            GameObject root = new GameObject("ResolveSpecificRoot");
            root.SetActive(false);

            try
            {
                UIButtonView button = root.AddComponent<UIButtonView>();
                UIWindowResolveButtonAction action = root.AddComponent<UIWindowResolveButtonAction>();

                ReflectionTestUtil.SetPrivateField(action, "_button", button);
                ReflectionTestUtil.SetPrivateField(action, "_resolveSpecificWindow", true);
                ReflectionTestUtil.SetPrivateField(action, "_windowId", "window.daily");
                ReflectionTestUtil.SetPrivateField(action, "_status", UIWindowResultStatus.Confirmed);
                ReflectionTestUtil.SetPrivateField(action, "_payload", "reward");
                ReflectionTestUtil.SetPrivateField(action, "_instant", true);

                action.Construct(service);

                root.SetActive(true);
                button.Click();

                Assert.That(service.ResolveCalls, Is.EqualTo(1));
                Assert.That(service.LastResolveWindowId, Is.EqualTo("window.daily"));
                Assert.That(service.LastResolveStatus, Is.EqualTo(UIWindowResultStatus.Confirmed));
                Assert.That(service.LastResolvePayload, Is.EqualTo("reward"));
                Assert.That(service.LastResolveInstant, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private sealed class FakeWindowResultService : IUIWindowResultService
        {
            public int ResolveCurrentCalls;
            public UIWindowResultStatus LastCurrentStatus;
            public string LastCurrentPayload = string.Empty;
            public bool LastCurrentInstant;

            public int ResolveCalls;
            public string LastResolveWindowId = string.Empty;
            public UIWindowResultStatus LastResolveStatus;
            public string LastResolvePayload = string.Empty;
            public bool LastResolveInstant;

            public Cysharp.Threading.Tasks.UniTask<UIWindowResult> EnqueueAndWaitAsync(string windowId, bool instant = true, int priority = 0, bool allowDuplicate = false)
            {
                return Cysharp.Threading.Tasks.UniTask.FromResult(new UIWindowResult(windowId, UIWindowResultStatus.Rejected));
            }

            public bool TryResolveCurrent(UIWindowResultStatus status, string payload = "", bool instant = true)
            {
                ResolveCurrentCalls++;
                LastCurrentStatus = status;
                LastCurrentPayload = payload;
                LastCurrentInstant = instant;
                return true;
            }

            public bool TryResolve(string windowId, UIWindowResultStatus status, string payload = "", bool instant = true)
            {
                ResolveCalls++;
                LastResolveWindowId = windowId;
                LastResolveStatus = status;
                LastResolvePayload = payload;
                LastResolveInstant = instant;
                return true;
            }
        }
    }
}
