using NUnit.Framework;
using UnityEngine;
using UnityEngine.UI;
using Vareiko.Foundation.UI;

namespace Vareiko.Foundation.Tests.UI
{
    public sealed class UIButtonViewTests
    {
        [Test]
        public void UguiButtonClick_InvokesClickedEvent()
        {
            GameObject root = new GameObject("UIButtonViewRoot");
            root.SetActive(false);

            try
            {
                Button button = root.AddComponent<Button>();
                UIButtonView view = root.AddComponent<UIButtonView>();
                int clickCount = 0;
                view.OnClicked.AddListener(() => clickCount++);

                root.SetActive(true);
                button.onClick.Invoke();

                Assert.That(clickCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void EnableDisable_DoesNotDuplicateUguiButtonSubscription()
        {
            GameObject root = new GameObject("UIButtonViewEnableRoot");
            root.SetActive(false);

            try
            {
                Button button = root.AddComponent<Button>();
                UIButtonView view = root.AddComponent<UIButtonView>();
                int clickCount = 0;
                view.OnClicked.AddListener(() => clickCount++);

                root.SetActive(true);
                root.SetActive(false);
                root.SetActive(true);
                button.onClick.Invoke();

                Assert.That(clickCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SetInteractable_SyncsUguiButtonInteractable()
        {
            GameObject root = new GameObject("UIButtonViewInteractableRoot");
            root.SetActive(false);

            try
            {
                Button button = root.AddComponent<Button>();
                UIButtonView view = root.AddComponent<UIButtonView>();

                root.SetActive(true);
                view.SetInteractable(false);

                Assert.That(view.Interactable, Is.False);
                Assert.That(button.interactable, Is.False);

                view.SetInteractable(true);

                Assert.That(view.Interactable, Is.True);
                Assert.That(button.interactable, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void Click_WhenUguiButtonIsNotInteractable_DoesNotInvokeClickedEvent()
        {
            GameObject root = new GameObject("UIButtonViewBlockedRoot");
            root.SetActive(false);

            try
            {
                Button button = root.AddComponent<Button>();
                UIButtonView view = root.AddComponent<UIButtonView>();
                int clickCount = 0;
                view.OnClicked.AddListener(() => clickCount++);

                root.SetActive(true);
                button.interactable = false;
                button.onClick.Invoke();

                Assert.That(clickCount, Is.EqualTo(0));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SetClickAction_ReplacesOwnedCallbackWithoutRemovingExternalListeners()
        {
            GameObject root = new GameObject("UIButtonViewOwnedActionRoot");

            try
            {
                UIButtonView view = root.AddComponent<UIButtonView>();
                int externalClickCount = 0;
                int firstOwnedClickCount = 0;
                int secondOwnedClickCount = 0;
                view.OnClicked.AddListener(() => externalClickCount++);

                view.SetClickAction(() => firstOwnedClickCount++);
                view.Click();

                view.SetClickAction(() => secondOwnedClickCount++);
                view.Click();

                Assert.That(externalClickCount, Is.EqualTo(2));
                Assert.That(firstOwnedClickCount, Is.EqualTo(1));
                Assert.That(secondOwnedClickCount, Is.EqualTo(1));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }
    }
}
