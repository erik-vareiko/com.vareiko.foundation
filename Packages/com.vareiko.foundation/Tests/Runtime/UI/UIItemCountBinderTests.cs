using NUnit.Framework;
using UnityEngine;
using Vareiko.Foundation.Tests.TestDoubles;
using Vareiko.Foundation.UI;

namespace Vareiko.Foundation.Tests.UI
{
    public sealed class UIItemCountBinderTests
    {
        [Test]
        public void ReactiveValue_UpdatesCollectionCount()
        {
            TestContext context = CreateContext();
            try
            {
                context.Service.SetInt("inventory.count", 5);
                Assert.That(context.Collection.ActiveCount, Is.EqualTo(5));

                context.Service.SetInt("inventory.count", -2);
                Assert.That(context.Collection.ActiveCount, Is.EqualTo(2));
            }
            finally
            {
                context.Dispose();
            }
        }

        private static TestContext CreateContext()
        {
            GameObject root = new GameObject("CountRoot");
            root.SetActive(false);

            GameObject templateGo = new GameObject("Template");
            templateGo.transform.SetParent(root.transform, false);
            UIItemView template = templateGo.AddComponent<UIItemView>();
            ReflectionTestUtil.SetPrivateField(template, "_id", "template.item");

            UIItemCollectionBinder collection = root.AddComponent<UIItemCollectionBinder>();
            ReflectionTestUtil.SetPrivateField(collection, "_itemPrefab", template);
            ReflectionTestUtil.SetPrivateField(collection, "_container", root.transform);
            ReflectionTestUtil.SetPrivateField(collection, "_hideTemplateItem", true);
            ReflectionTestUtil.SetPrivateField(collection, "_destroyItemsWhenShrinking", false);

            UIItemCountBinder binder = root.AddComponent<UIItemCountBinder>();
            ReflectionTestUtil.SetPrivateField(binder, "_collection", collection);
            ReflectionTestUtil.SetPrivateField(binder, "_key", "inventory.count");
            ReflectionTestUtil.SetPrivateField(binder, "_minCount", 0);
            ReflectionTestUtil.SetPrivateField(binder, "_maxCount", 10);
            ReflectionTestUtil.SetPrivateField(binder, "_absoluteValue", true);

            UIValueEventService service = new UIValueEventService(null);
            binder.Construct(null, service);

            root.SetActive(true);

            return new TestContext(root, collection, service);
        }

        private sealed class TestContext
        {
            private readonly GameObject _root;
            public readonly UIItemCollectionBinder Collection;
            public readonly UIValueEventService Service;

            public TestContext(GameObject root, UIItemCollectionBinder collection, UIValueEventService service)
            {
                _root = root;
                Collection = collection;
                Service = service;
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
