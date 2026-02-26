using NUnit.Framework;
using UnityEngine;
using Vareiko.Foundation.Tests.TestDoubles;
using Vareiko.Foundation.UI;

namespace Vareiko.Foundation.Tests.UI
{
    public sealed class UIWindowButtonActionsTests
    {
        [Test]
        public void OpenAction_Click_EnqueuesWindow()
        {
            FakeWindowManager manager = new FakeWindowManager();
            GameObject root = new GameObject("OpenActionRoot");
            root.SetActive(false);

            try
            {
                UIButtonView button = root.AddComponent<UIButtonView>();
                UIWindowOpenButtonAction action = root.AddComponent<UIWindowOpenButtonAction>();

                ReflectionTestUtil.SetPrivateField(action, "_button", button);
                ReflectionTestUtil.SetPrivateField(action, "_windowId", "window.shop");
                ReflectionTestUtil.SetPrivateField(action, "_instant", false);
                ReflectionTestUtil.SetPrivateField(action, "_priority", 10);
                ReflectionTestUtil.SetPrivateField(action, "_allowDuplicate", true);

                action.Construct(manager);

                root.SetActive(true);
                button.Click();

                Assert.That(manager.EnqueueCalls, Is.EqualTo(1));
                Assert.That(manager.LastEnqueueWindowId, Is.EqualTo("window.shop"));
                Assert.That(manager.LastEnqueueInstant, Is.False);
                Assert.That(manager.LastEnqueuePriority, Is.EqualTo(10));
                Assert.That(manager.LastEnqueueAllowDuplicate, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void CloseAction_Click_ClosesCurrentByDefault()
        {
            FakeWindowManager manager = new FakeWindowManager();
            GameObject root = new GameObject("CloseActionRoot");
            root.SetActive(false);

            try
            {
                UIButtonView button = root.AddComponent<UIButtonView>();
                UIWindowCloseButtonAction action = root.AddComponent<UIWindowCloseButtonAction>();

                ReflectionTestUtil.SetPrivateField(action, "_button", button);
                ReflectionTestUtil.SetPrivateField(action, "_closeSpecificWindow", false);
                ReflectionTestUtil.SetPrivateField(action, "_instant", false);

                action.Construct(manager);

                root.SetActive(true);
                button.Click();

                Assert.That(manager.CloseCurrentCalls, Is.EqualTo(1));
                Assert.That(manager.LastCloseCurrentInstant, Is.False);
                Assert.That(manager.CloseCalls, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void CloseAction_WhenSpecificWindowEnabled_ClosesById()
        {
            FakeWindowManager manager = new FakeWindowManager();
            GameObject root = new GameObject("CloseSpecificActionRoot");
            root.SetActive(false);

            try
            {
                UIButtonView button = root.AddComponent<UIButtonView>();
                UIWindowCloseButtonAction action = root.AddComponent<UIWindowCloseButtonAction>();

                ReflectionTestUtil.SetPrivateField(action, "_button", button);
                ReflectionTestUtil.SetPrivateField(action, "_closeSpecificWindow", true);
                ReflectionTestUtil.SetPrivateField(action, "_windowId", "window.daily");
                ReflectionTestUtil.SetPrivateField(action, "_instant", true);

                action.Construct(manager);

                root.SetActive(true);
                button.Click();

                Assert.That(manager.CloseCalls, Is.EqualTo(1));
                Assert.That(manager.LastCloseWindowId, Is.EqualTo("window.daily"));
                Assert.That(manager.LastCloseInstant, Is.True);
                Assert.That(manager.CloseCurrentCalls, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private sealed class FakeWindowManager : IUIWindowManager
        {
            public int EnqueueCalls;
            public string LastEnqueueWindowId = string.Empty;
            public bool LastEnqueueInstant;
            public int LastEnqueuePriority;
            public bool LastEnqueueAllowDuplicate;

            public int CloseCurrentCalls;
            public bool LastCloseCurrentInstant;

            public int CloseCalls;
            public string LastCloseWindowId = string.Empty;
            public bool LastCloseInstant;

            public string CurrentWindowId => string.Empty;
            public int QueueCount => 0;

            public bool Enqueue(string windowId, bool instant = true, int priority = 0, bool allowDuplicate = false)
            {
                EnqueueCalls++;
                LastEnqueueWindowId = windowId;
                LastEnqueueInstant = instant;
                LastEnqueuePriority = priority;
                LastEnqueueAllowDuplicate = allowDuplicate;
                return true;
            }

            public bool TryCloseCurrent(bool instant = true)
            {
                CloseCurrentCalls++;
                LastCloseCurrentInstant = instant;
                return true;
            }

            public bool TryClose(string windowId, bool instant = true)
            {
                CloseCalls++;
                LastCloseWindowId = windowId;
                LastCloseInstant = instant;
                return true;
            }

            public void ClearQueue()
            {
            }
        }
    }
}
