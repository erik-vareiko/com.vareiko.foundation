using System;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Vareiko.Foundation.Tests.TestDoubles;
using Vareiko.Foundation.UI;

namespace Vareiko.Foundation.Tests.UI
{
    public sealed class UIValueBinderLifecycleTests
    {
        [Test]
        public void ValueServicePath_DoesNotTouchSignalBusUnsubscribe()
        {
            RunAllBinderCases(useValueService: true);
        }

        [Test]
        public void SignalBusFallbackPath_UpdatesTargetsAndUnsubscribesCleanly()
        {
            RunAllBinderCases(useValueService: false);
        }

        [Test]
        public void InvalidKeys_DoNotSubscribeOrUpdateTargets()
        {
            FakeSignalBus signalBus = new FakeSignalBus();
            UIValueEventService valueService = new UIValueEventService(signalBus);
            BinderCase binderCase = CreateStringCase(" ");
            try
            {
                binderCase.Activate(signalBus, valueService);
                valueService.SetString("hud.name", "Rogue");
                signalBus.Publish(new UIStringValueChangedSignal("hud.name", "Mage"));

                Assert.That(binderCase.ReadString(), Is.Empty);
                Assert.DoesNotThrow(() => binderCase.Root.SetActive(false));
            }
            finally
            {
                binderCase.Dispose();
            }
        }

        private static void RunAllBinderCases(bool useValueService)
        {
            BinderCase[] cases =
            {
                CreateStringCase("hud.name"),
                CreateIntCase("hud.coins"),
                CreateFloatCase("hud.hp"),
                CreateBoolGameObjectCase("hud.visible"),
                CreateButtonCase("hud.can_click"),
                CreateItemCountCase("hud.item_count")
            };

            FakeSignalBus signalBus = new FakeSignalBus();
            UIValueEventService valueService = new UIValueEventService(signalBus);
            for (int i = 0; i < cases.Length; i++)
            {
                BinderCase binderCase = cases[i];
                try
                {
                    binderCase.Activate(signalBus, useValueService ? valueService : null);
                    if (useValueService)
                    {
                        binderCase.PublishValueService(valueService);
                    }
                    else
                    {
                        binderCase.PublishSignalBus(signalBus);
                    }

                    binderCase.AssertApplied();
                    Assert.DoesNotThrow(() => binderCase.Root.SetActive(false));
                }
                finally
                {
                    binderCase.Dispose();
                }
            }
        }

        private static BinderCase CreateStringCase(string key)
        {
            GameObject root = CreateInactiveRoot("StringBinder");
            Text text = root.AddComponent<Text>();
            UIStringTextBinder binder = root.AddComponent<UIStringTextBinder>();
            ReflectionTestUtil.SetPrivateField(binder, "_key", key);
            ReflectionTestUtil.SetPrivateField(binder, "_target", text);
            return new BinderCase(
                root,
                (bus, service) => binder.Construct(bus, service),
                service => service.SetString("hud.name", "Rogue"),
                bus => bus.Publish(new UIStringValueChangedSignal("hud.name", "Rogue")),
                () => Assert.That(text.text, Is.EqualTo("Rogue")),
                () => text.text);
        }

        private static BinderCase CreateIntCase(string key)
        {
            GameObject root = CreateInactiveRoot("IntBinder");
            Text text = root.AddComponent<Text>();
            UIIntTextBinder binder = root.AddComponent<UIIntTextBinder>();
            ReflectionTestUtil.SetPrivateField(binder, "_key", key);
            ReflectionTestUtil.SetPrivateField(binder, "_target", text);
            return new BinderCase(
                root,
                (bus, service) => binder.Construct(bus, service),
                service => service.SetInt("hud.coins", 42),
                bus => bus.Publish(new UIIntValueChangedSignal("hud.coins", 42)),
                () => Assert.That(text.text, Is.EqualTo("42")));
        }

        private static BinderCase CreateFloatCase(string key)
        {
            GameObject root = CreateInactiveRoot("FloatBinder");
            Text text = root.AddComponent<Text>();
            UIFloatTextBinder binder = root.AddComponent<UIFloatTextBinder>();
            ReflectionTestUtil.SetPrivateField(binder, "_key", key);
            ReflectionTestUtil.SetPrivateField(binder, "_target", text);
            return new BinderCase(
                root,
                (bus, service) => binder.Construct(bus, service),
                service => service.SetFloat("hud.hp", 12.5f),
                bus => bus.Publish(new UIFloatValueChangedSignal("hud.hp", 12.5f)),
                () => Assert.That(text.text, Is.EqualTo("12.5")));
        }

        private static BinderCase CreateBoolGameObjectCase(string key)
        {
            GameObject root = CreateInactiveRoot("BoolGameObjectBinder");
            GameObject target = new GameObject("Target");
            target.transform.SetParent(root.transform, false);
            target.SetActive(false);
            UIBoolGameObjectBinder binder = root.AddComponent<UIBoolGameObjectBinder>();
            ReflectionTestUtil.SetPrivateField(binder, "_key", key);
            ReflectionTestUtil.SetPrivateField(binder, "_target", target);
            return new BinderCase(
                root,
                (bus, service) => binder.Construct(bus, service),
                service => service.SetBool("hud.visible", true),
                bus => bus.Publish(new UIBoolValueChangedSignal("hud.visible", true)),
                () => Assert.That(target.activeSelf, Is.True));
        }

        private static BinderCase CreateButtonCase(string key)
        {
            GameObject root = CreateInactiveRoot("ButtonBinder");
            UIButtonView button = root.AddComponent<UIButtonView>();
            UIBoolButtonInteractableBinder binder = root.AddComponent<UIBoolButtonInteractableBinder>();
            ReflectionTestUtil.SetPrivateField(binder, "_key", key);
            ReflectionTestUtil.SetPrivateField(binder, "_buttonView", button);
            return new BinderCase(
                root,
                (bus, service) => binder.Construct(bus, service),
                service => service.SetBool("hud.can_click", false),
                bus => bus.Publish(new UIBoolValueChangedSignal("hud.can_click", false)),
                () => Assert.That(button.Interactable, Is.False));
        }

        private static BinderCase CreateItemCountCase(string key)
        {
            GameObject root = CreateInactiveRoot("ItemCountBinder");
            UIItemCollectionBinder collection = CreateCollection(root.transform);
            UIItemCountBinder binder = root.AddComponent<UIItemCountBinder>();
            ReflectionTestUtil.SetPrivateField(binder, "_key", key);
            ReflectionTestUtil.SetPrivateField(binder, "_collection", collection);
            return new BinderCase(
                root,
                (bus, service) => binder.Construct(bus, service),
                service => service.SetInt("hud.item_count", 2),
                bus => bus.Publish(new UIIntValueChangedSignal("hud.item_count", 2)),
                () => Assert.That(collection.ActiveCount, Is.EqualTo(2)));
        }

        private static UIItemCollectionBinder CreateCollection(Transform parent)
        {
            GameObject container = new GameObject("Container");
            container.transform.SetParent(parent, false);
            GameObject templateGo = new GameObject("Template");
            templateGo.transform.SetParent(container.transform, false);
            UIItemView template = templateGo.AddComponent<UIItemView>();

            UIItemCollectionBinder collection = parent.gameObject.AddComponent<UIItemCollectionBinder>();
            ReflectionTestUtil.SetPrivateField(collection, "_itemPrefab", template);
            ReflectionTestUtil.SetPrivateField(collection, "_container", container.transform);
            ReflectionTestUtil.SetPrivateField(collection, "_hideTemplateItem", true);
            return collection;
        }

        private static GameObject CreateInactiveRoot(string name)
        {
            GameObject root = new GameObject(name);
            root.SetActive(false);
            return root;
        }

        private sealed class BinderCase : IDisposable
        {
            private readonly Action<FakeSignalBus, IUIValueEventService> _construct;
            private readonly Func<string> _readString;

            public readonly GameObject Root;
            public readonly Action<UIValueEventService> PublishValueService;
            public readonly Action<FakeSignalBus> PublishSignalBus;
            public readonly Action AssertApplied;

            public BinderCase(
                GameObject root,
                Action<FakeSignalBus, IUIValueEventService> construct,
                Action<UIValueEventService> publishValueService,
                Action<FakeSignalBus> publishSignalBus,
                Action assertApplied,
                Func<string> readString = null)
            {
                Root = root;
                _construct = construct;
                PublishValueService = publishValueService;
                PublishSignalBus = publishSignalBus;
                AssertApplied = assertApplied;
                _readString = readString;
            }

            public void Activate(FakeSignalBus signalBus, IUIValueEventService valueService)
            {
                _construct(signalBus, valueService);
                Root.SetActive(true);
            }

            public string ReadString()
            {
                return _readString != null ? _readString() : string.Empty;
            }

            public void Dispose()
            {
                if (Root != null)
                {
                    UnityEngine.Object.DestroyImmediate(Root);
                }
            }
        }
    }
}
