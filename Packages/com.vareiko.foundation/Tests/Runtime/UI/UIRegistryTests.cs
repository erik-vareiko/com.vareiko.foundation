using System;
using NUnit.Framework;
using UnityEngine;
using Vareiko.Foundation.Tests.TestDoubles;
using Vareiko.Foundation.UI;

namespace Vareiko.Foundation.Tests.UI
{
    public sealed class UIRegistryTests
    {
        [Test]
        public void BuildMap_WithDuplicateIds_Throws()
        {
            GameObject root = new GameObject("UIRoot");
            try
            {
                root.AddComponent<UIRegistry>();
                CreateElement<UIElement>(root.transform, "First", "screen.main");
                CreateElement<UIElement>(root.transform, "Second", "screen.main");

                UIRegistry registry = root.GetComponent<UIRegistry>();

                Assert.Throws<InvalidOperationException>(() => registry.BuildMap());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void BuildMap_WithEmptyScreenOrWindowId_Throws()
        {
            GameObject root = new GameObject("UIRoot");
            try
            {
                root.AddComponent<UIRegistry>();
                CreateElement<UIScreen>(root.transform, "MainScreen", string.Empty);

                UIRegistry registry = root.GetComponent<UIRegistry>();

                Assert.Throws<InvalidOperationException>(() => registry.BuildMap());
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void BuildMap_IgnoresEmptyAuxiliaryElementIds()
        {
            GameObject root = new GameObject("UIRoot");
            try
            {
                root.AddComponent<UIRegistry>();
                CreateElement<UIElement>(root.transform, "Decor", string.Empty);
                CreateElement<UIScreen>(root.transform, "MainScreen", "screen.main");

                UIRegistry registry = root.GetComponent<UIRegistry>();
                registry.BuildMap();

                Assert.That(registry.Count, Is.EqualTo(1));
                Assert.That(registry.TryGetScreen("screen.main", out _), Is.True);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static T CreateElement<T>(Transform parent, string name, string id)
            where T : UIElement
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            T element = gameObject.AddComponent<T>();
            ReflectionTestUtil.SetPrivateField(element, "_id", id);
            return element;
        }
    }
}
