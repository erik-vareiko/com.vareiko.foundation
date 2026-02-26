using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Vareiko.Foundation.Tests.TestDoubles;
using Vareiko.Foundation.UI;

namespace Vareiko.Foundation.Tests.UI
{
    public sealed class UIWindowResultServiceTests
    {
        [Test]
        public void EnqueueAndWaitAsync_WhenResolved_ReturnsResult()
        {
            TestContext context = CreateContext();
            try
            {
                var waitTask = context.Manager.EnqueueAndWaitAsync("window.a");
                Assert.That(context.Manager.CurrentWindowId, Is.EqualTo("window.a"));

                Assert.That(context.Manager.TryResolveCurrent(UIWindowResultStatus.Confirmed, "ok"), Is.True);

                UIWindowResult result = waitTask.GetAwaiter().GetResult();
                Assert.That(result.WindowId, Is.EqualTo("window.a"));
                Assert.That(result.Status, Is.EqualTo(UIWindowResultStatus.Confirmed));
                Assert.That(result.Payload, Is.EqualTo("ok"));
                Assert.That(result.IsConfirmed, Is.True);
            }
            finally
            {
                context.Dispose();
            }
        }

        [Test]
        public void EnqueueAndWaitAsync_WhenCurrentClosed_ReturnsClosed()
        {
            TestContext context = CreateContext();
            try
            {
                var waitTask = context.Manager.EnqueueAndWaitAsync("window.a");

                Assert.That(context.Manager.TryCloseCurrent(), Is.True);
                UIWindowResult result = waitTask.GetAwaiter().GetResult();

                Assert.That(result.Status, Is.EqualTo(UIWindowResultStatus.Closed));
                Assert.That(result.WindowId, Is.EqualTo("window.a"));
            }
            finally
            {
                context.Dispose();
            }
        }

        [Test]
        public void EnqueueAndWaitAsync_WhenRejected_ReturnsRejected()
        {
            TestContext context = CreateContext();
            try
            {
                Assert.That(context.Manager.Enqueue("window.a"), Is.True);

                UIWindowResult result = context.Manager.EnqueueAndWaitAsync("window.a").GetAwaiter().GetResult();
                Assert.That(result.Status, Is.EqualTo(UIWindowResultStatus.Rejected));
                Assert.That(result.WindowId, Is.EqualTo("window.a"));
            }
            finally
            {
                context.Dispose();
            }
        }

        [Test]
        public void TryResolve_WhenQueuedWindow_CompletesQueuedRequest()
        {
            TestContext context = CreateContext();
            try
            {
                var first = context.Manager.EnqueueAndWaitAsync("window.a");
                var second = context.Manager.EnqueueAndWaitAsync("window.b");

                Assert.That(context.Manager.CurrentWindowId, Is.EqualTo("window.a"));
                Assert.That(context.Manager.QueueCount, Is.EqualTo(1));

                Assert.That(context.Manager.TryResolve("window.b", UIWindowResultStatus.Canceled, "queued-cancel"), Is.True);
                UIWindowResult queuedResult = second.GetAwaiter().GetResult();

                Assert.That(queuedResult.WindowId, Is.EqualTo("window.b"));
                Assert.That(queuedResult.Status, Is.EqualTo(UIWindowResultStatus.Canceled));
                Assert.That(queuedResult.Payload, Is.EqualTo("queued-cancel"));
                Assert.That(context.Manager.QueueCount, Is.EqualTo(0));

                Assert.That(context.Manager.TryResolveCurrent(UIWindowResultStatus.Closed), Is.True);
                UIWindowResult firstResult = first.GetAwaiter().GetResult();
                Assert.That(firstResult.Status, Is.EqualTo(UIWindowResultStatus.Closed));
            }
            finally
            {
                context.Dispose();
            }
        }

        [Test]
        public void EnqueueAndWaitAsync_WhenQueuedWindowFailsToShow_ReturnsRejected()
        {
            TestContext context = CreateContext("window.b");
            try
            {
                var first = context.Manager.EnqueueAndWaitAsync("window.a");
                var second = context.Manager.EnqueueAndWaitAsync("window.b");

                Assert.That(context.Manager.TryResolveCurrent(UIWindowResultStatus.Closed), Is.True);

                UIWindowResult firstResult = first.GetAwaiter().GetResult();
                UIWindowResult secondResult = second.GetAwaiter().GetResult();

                Assert.That(firstResult.Status, Is.EqualTo(UIWindowResultStatus.Closed));
                Assert.That(secondResult.Status, Is.EqualTo(UIWindowResultStatus.Rejected));
                Assert.That(context.Manager.CurrentWindowId, Is.Empty);
                Assert.That(context.Manager.QueueCount, Is.EqualTo(0));
            }
            finally
            {
                context.Dispose();
            }
        }

        private static TestContext CreateContext(params string[] failShowWindowIds)
        {
            GameObject root = new GameObject("UI Result Root");

            UIWindow windowA = CreateWindow(root.transform, "WindowA", "window.a");
            UIWindow windowB = CreateWindow(root.transform, "WindowB", "window.b");
            UIWindow windowC = CreateWindow(root.transform, "WindowC", "window.c");

            FakeUIService uiService = new FakeUIService(
                new[] { windowA, windowB, windowC },
                failShowWindowIds);
            UIWindowManager manager = new UIWindowManager(uiService, null);

            return new TestContext(root, manager);
        }

        private static UIWindow CreateWindow(Transform parent, string name, string id)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            UIWindow window = gameObject.AddComponent<UIWindow>();
            ReflectionTestUtil.SetPrivateField(window, "_id", id);
            return window;
        }

        private sealed class FakeUIService : IUIService
        {
            private readonly Dictionary<string, UIElement> _elements = new Dictionary<string, UIElement>();
            private readonly HashSet<string> _showFailIds = new HashSet<string>();

            public FakeUIService(IReadOnlyList<UIElement> elements, IReadOnlyList<string> showFailIds)
            {
                for (int i = 0; i < elements.Count; i++)
                {
                    UIElement element = elements[i];
                    if (element == null || string.IsNullOrWhiteSpace(element.Id))
                    {
                        continue;
                    }

                    _elements[element.Id] = element;
                }

                if (showFailIds == null)
                {
                    return;
                }

                for (int i = 0; i < showFailIds.Count; i++)
                {
                    if (string.IsNullOrWhiteSpace(showFailIds[i]))
                    {
                        continue;
                    }

                    _showFailIds.Add(showFailIds[i].Trim());
                }
            }

            public bool Show(string elementId, bool instant = true)
            {
                if (_showFailIds.Contains(elementId))
                {
                    return false;
                }

                UIElement element;
                if (!TryGetElement(elementId, out element))
                {
                    return false;
                }

                element.Show(instant);
                return true;
            }

            public bool Hide(string elementId, bool instant = true)
            {
                UIElement element;
                if (!TryGetElement(elementId, out element))
                {
                    return false;
                }

                element.Hide(instant);
                return true;
            }

            public bool Toggle(string elementId, bool instant = true)
            {
                UIElement element;
                if (!TryGetElement(elementId, out element))
                {
                    return false;
                }

                if (element.IsVisible)
                {
                    element.Hide(instant);
                }
                else
                {
                    element.Show(instant);
                }

                return true;
            }

            public void HideAll(bool instant = true)
            {
                foreach (KeyValuePair<string, UIElement> pair in _elements)
                {
                    pair.Value.Hide(instant);
                }
            }

            public bool TryGetElement(string elementId, out UIElement element)
            {
                if (string.IsNullOrWhiteSpace(elementId))
                {
                    element = null;
                    return false;
                }

                return _elements.TryGetValue(elementId, out element);
            }

            public bool TryGetScreen(string screenId, out UIScreen screen)
            {
                screen = null;
                UIElement element;
                if (!TryGetElement(screenId, out element))
                {
                    return false;
                }

                screen = element as UIScreen;
                return screen != null;
            }

            public bool TryGetWindow(string windowId, out UIWindow window)
            {
                window = null;
                UIElement element;
                if (!TryGetElement(windowId, out element))
                {
                    return false;
                }

                window = element as UIWindow;
                return window != null;
            }
        }

        private sealed class TestContext
        {
            private readonly GameObject _root;
            public readonly UIWindowManager Manager;

            public TestContext(GameObject root, UIWindowManager manager)
            {
                _root = root;
                Manager = manager;
            }

            public void Dispose()
            {
                if (_root != null)
                {
                    Object.DestroyImmediate(_root);
                }
            }
        }
    }
}
