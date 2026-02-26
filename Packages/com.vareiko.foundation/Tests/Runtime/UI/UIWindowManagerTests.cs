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
            UIRegistry registry = root.AddComponent<UIRegistry>();

            UIWindow windowA = CreateWindow(root.transform, "WindowA", "window.a");
            UIWindow windowB = CreateWindow(root.transform, "WindowB", "window.b");
            UIWindow windowC = CreateWindow(root.transform, "WindowC", "window.c");

            registry.BuildMap();
            UIService uiService = new UIService(registry, null, null);
            uiService.Initialize();
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
