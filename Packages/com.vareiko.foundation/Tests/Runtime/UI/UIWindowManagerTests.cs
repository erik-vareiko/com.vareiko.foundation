using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Vareiko.Foundation.Tests.TestDoubles;
using Vareiko.Foundation.UI;

namespace Vareiko.Foundation.Tests.UI
{
    public sealed class UIWindowManagerTests
    {
        [Test]
        public void Enqueue_WhenNoActiveWindow_ShowsImmediately()
        {
            TestContext context = CreateContext();
            try
            {
                Assert.That(context.Manager.Enqueue("window.a"), Is.True);
                Assert.That(context.Manager.CurrentWindowId, Is.EqualTo("window.a"));
                Assert.That(context.Manager.QueueCount, Is.EqualTo(0));
                Assert.That(context.WindowA.gameObject.activeSelf, Is.True);
            }
            finally
            {
                context.Dispose();
            }
        }

        [Test]
        public void Enqueue_RespectsPriorityAndSequence()
        {
            TestContext context = CreateContext();
            try
            {
                Assert.That(context.Manager.Enqueue("window.a"), Is.True);
                Assert.That(context.Manager.Enqueue("window.b"), Is.True);
                Assert.That(context.Manager.Enqueue("window.c", priority: 10), Is.True);

                Assert.That(context.Manager.CurrentWindowId, Is.EqualTo("window.a"));
                Assert.That(context.Manager.QueueCount, Is.EqualTo(2));

                Assert.That(context.Manager.TryCloseCurrent(), Is.True);
                Assert.That(context.Manager.CurrentWindowId, Is.EqualTo("window.c"));

                Assert.That(context.Manager.TryCloseCurrent(), Is.True);
                Assert.That(context.Manager.CurrentWindowId, Is.EqualTo("window.b"));

                Assert.That(context.Manager.TryCloseCurrent(), Is.True);
                Assert.That(context.Manager.CurrentWindowId, Is.Empty);
                Assert.That(context.Manager.QueueCount, Is.EqualTo(0));
            }
            finally
            {
                context.Dispose();
            }
        }

        [Test]
        public void Enqueue_DoesNotAllowDuplicatesByDefault()
        {
            TestContext context = CreateContext();
            try
            {
                Assert.That(context.Manager.Enqueue("window.a"), Is.True);
                Assert.That(context.Manager.Enqueue("window.a"), Is.False);

                Assert.That(context.Manager.Enqueue("window.b"), Is.True);
                Assert.That(context.Manager.Enqueue("window.b"), Is.False);
                Assert.That(context.Manager.QueueCount, Is.EqualTo(1));
            }
            finally
            {
                context.Dispose();
            }
        }

        private static TestContext CreateContext()
        {
            GameObject root = new GameObject("UI Root");

            UIWindow windowA = CreateWindow(root.transform, "WindowA", "window.a");
            UIWindow windowB = CreateWindow(root.transform, "WindowB", "window.b");
            UIWindow windowC = CreateWindow(root.transform, "WindowC", "window.c");

            FakeUIService uiService = new FakeUIService(windowA, windowB, windowC);
            UIWindowManager manager = new UIWindowManager(uiService, null);

            return new TestContext(root, windowA, windowB, windowC, manager);
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

            public FakeUIService(params UIElement[] elements)
            {
                for (int i = 0; i < elements.Length; i++)
                {
                    UIElement element = elements[i];
                    if (element == null || string.IsNullOrWhiteSpace(element.Id))
                    {
                        continue;
                    }

                    _elements[element.Id] = element;
                }
            }

            public bool Show(string elementId, bool instant = true)
            {
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

            public readonly UIWindow WindowA;
            public readonly UIWindow WindowB;
            public readonly UIWindow WindowC;
            public readonly UIWindowManager Manager;

            public TestContext(GameObject root, UIWindow windowA, UIWindow windowB, UIWindow windowC, UIWindowManager manager)
            {
                _root = root;
                WindowA = windowA;
                WindowB = windowB;
                WindowC = windowC;
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
