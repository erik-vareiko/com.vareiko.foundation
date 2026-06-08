using NUnit.Framework;
using UnityEngine;
using Vareiko.Foundation.Tests.TestDoubles;
using Vareiko.Foundation.UI;

namespace Vareiko.Foundation.Tests.UI
{
    public sealed class UIServiceTests
    {
        [Test]
        public void ShowHide_EmitSignalsOnlyWhenVisibilityChanges()
        {
            GameObject root = new GameObject("UIRoot");
            try
            {
                UIRegistry registry = root.AddComponent<UIRegistry>();
                UIScreen screen = CreateScreen(root.transform, "MainScreen", "screen.main");
                screen.Hide(true);

                CountingSignalBus signalBus = new CountingSignalBus();
                UIService service = new UIService(registry, signalBus.Bus);
                service.Initialize();

                Assert.That(service.Show("screen.main"), Is.True);
                Assert.That(service.Show("screen.main"), Is.True);
                Assert.That(signalBus.ScreenShownCount, Is.EqualTo(1));

                Assert.That(service.Hide("screen.main"), Is.True);
                Assert.That(service.Hide("screen.main"), Is.True);
                Assert.That(signalBus.ScreenHiddenCount, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static UIScreen CreateScreen(Transform parent, string name, string id)
        {
            GameObject gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent, false);
            UIScreen screen = gameObject.AddComponent<UIScreen>();
            ReflectionTestUtil.SetPrivateField(screen, "_id", id);
            return screen;
        }

        private sealed class CountingSignalBus
        {
            public readonly FakeSignalBus Bus = new FakeSignalBus();
            public int ScreenShownCount;
            public int ScreenHiddenCount;

            public CountingSignalBus()
            {
                Bus.Subscribe<UIScreenShownSignal>(_ => ScreenShownCount++);
                Bus.Subscribe<UIScreenHiddenSignal>(_ => ScreenHiddenCount++);
            }
        }
    }
}
