using NUnit.Framework;
using UnityEngine;
using Vareiko.Foundation.Tests.TestDoubles;
using Vareiko.Foundation.UI;

namespace Vareiko.Foundation.Tests.UI
{
    public sealed class UIBoolButtonInteractableBinderTests
    {
        [Test]
        public void ReactiveValue_UpdatesButtonInteractable()
        {
            TestContext context = CreateContext();
            try
            {
                Assert.That(context.Button.Interactable, Is.True);

                context.Service.SetBool("shop.can_buy", false);
                Assert.That(context.Button.Interactable, Is.False);

                context.Service.SetBool("shop.can_buy", true);
                Assert.That(context.Button.Interactable, Is.True);
            }
            finally
            {
                context.Dispose();
            }
        }

        private static TestContext CreateContext()
        {
            GameObject root = new GameObject("ButtonBinderRoot");
            root.SetActive(false);

            UIButtonView button = root.AddComponent<UIButtonView>();
            UIBoolButtonInteractableBinder binder = root.AddComponent<UIBoolButtonInteractableBinder>();
            ReflectionTestUtil.SetPrivateField(binder, "_buttonView", button);
            ReflectionTestUtil.SetPrivateField(binder, "_key", "shop.can_buy");
            ReflectionTestUtil.SetPrivateField(binder, "_invert", false);

            UIValueEventService service = new UIValueEventService(null);
            binder.Construct(null, service);

            root.SetActive(true);

            return new TestContext(root, button, service);
        }

        private sealed class TestContext
        {
            private readonly GameObject _root;
            public readonly UIButtonView Button;
            public readonly UIValueEventService Service;

            public TestContext(GameObject root, UIButtonView button, UIValueEventService service)
            {
                _root = root;
                Button = button;
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
