using NUnit.Framework;
using UnityEngine;
using Vareiko.Foundation.Tests.TestDoubles;
using Vareiko.Foundation.UI;

namespace Vareiko.Foundation.Tests.UI
{
    public sealed class UIItemCollectionBinderTests
    {
        [Test]
        public void SetCount_GrowsPoolAndTogglesVisibility()
        {
            TestContext context = CreateContext(destroyItemsWhenShrinking: false);
            try
            {
                context.Binder.SetCount(3);
                Assert.That(context.Binder.ActiveCount, Is.EqualTo(3));
                Assert.That(context.Binder.PooledCount, Is.EqualTo(3));

                context.Binder.SetCount(1);
                Assert.That(context.Binder.ActiveCount, Is.EqualTo(1));
                Assert.That(context.Binder.PooledCount, Is.EqualTo(3));
            }
            finally
            {
                context.Dispose();
            }
        }

        [Test]
        public void SetCount_WhenDestroyOnShrinkEnabled_RemovesExtraItems()
        {
            TestContext context = CreateContext(destroyItemsWhenShrinking: true);
            try
            {
                context.Binder.SetCount(4);
                Assert.That(context.Binder.PooledCount, Is.EqualTo(4));

                context.Binder.SetCount(1);
                Assert.That(context.Binder.ActiveCount, Is.EqualTo(1));
                Assert.That(context.Binder.PooledCount, Is.EqualTo(1));
            }
            finally
            {
                context.Dispose();
            }
        }

        private static TestContext CreateContext(bool destroyItemsWhenShrinking)
        {
            GameObject root = new GameObject("ItemsRoot");
            GameObject containerGo = new GameObject("Container");
            containerGo.transform.SetParent(root.transform, false);

            GameObject templateGo = new GameObject("Template");
            templateGo.transform.SetParent(containerGo.transform, false);
            UIItemView template = templateGo.AddComponent<UIItemView>();
            ReflectionTestUtil.SetPrivateField(template, "_id", "template.item");

            UIItemCollectionBinder binder = root.AddComponent<UIItemCollectionBinder>();
            ReflectionTestUtil.SetPrivateField(binder, "_itemPrefab", template);
            ReflectionTestUtil.SetPrivateField(binder, "_container", containerGo.transform);
            ReflectionTestUtil.SetPrivateField(binder, "_hideTemplateItem", true);
            ReflectionTestUtil.SetPrivateField(binder, "_destroyItemsWhenShrinking", destroyItemsWhenShrinking);

            return new TestContext(root, binder);
        }

        private sealed class TestContext
        {
            private readonly GameObject _root;
            public readonly UIItemCollectionBinder Binder;

            public TestContext(GameObject root, UIItemCollectionBinder binder)
            {
                _root = root;
                Binder = binder;
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
