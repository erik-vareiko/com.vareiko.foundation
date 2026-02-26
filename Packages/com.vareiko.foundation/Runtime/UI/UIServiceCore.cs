using System;
using System.Collections.Generic;
using Zenject;

namespace Vareiko.Foundation.UI
{
    public class UIService : IUIService, IUiService, IInitializable
    {
        private readonly UIRegistry _registry;
        private readonly UIScreenRegistry _legacyRegistry;
        private readonly SignalBus _signalBus;
        private readonly Dictionary<string, UIElement> _elements = new Dictionary<string, UIElement>(StringComparer.Ordinal);

        [Inject]
        public UIService(
            [InjectOptional] UIRegistry registry = null,
            [InjectOptional] UIScreenRegistry legacyRegistry = null,
            [InjectOptional] SignalBus signalBus = null)
        {
            _registry = registry;
            _legacyRegistry = legacyRegistry;
            _signalBus = signalBus;
        }

        public void Initialize()
        {
            _elements.Clear();

            UIRegistry activeRegistry = _registry ?? _legacyRegistry;
            if (activeRegistry != null)
            {
                activeRegistry.BuildMap();
                foreach (KeyValuePair<string, UIElement> pair in activeRegistry.EnumerateElements())
                {
                    _elements[pair.Key] = pair.Value;
                }
            }

            int screenCount = 0;
            int windowCount = 0;
            foreach (KeyValuePair<string, UIElement> pair in _elements)
            {
                if (pair.Value is UIScreen)
                {
                    screenCount++;
                }

                if (pair.Value is UIWindow)
                {
                    windowCount++;
                }
            }

            _signalBus?.Fire(new UIReadySignal(_elements.Count, screenCount, windowCount));
            _signalBus?.Fire(new UiReadySignal(screenCount));
        }

        public bool Show(string elementId, bool instant = true)
        {
            UIElement element;
            if (!TryGetElement(elementId, out element))
            {
                return false;
            }

            element.Show(instant);
            FireShownSignals(elementId, element);
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
            FireHiddenSignals(elementId, element);
            return true;
        }

        public bool Toggle(string elementId, bool instant = true)
        {
            UIElement element;
            if (!TryGetElement(elementId, out element))
            {
                return false;
            }

            return element.IsVisible ? Hide(elementId, instant) : Show(elementId, instant);
        }

        public void HideAll(bool instant = true)
        {
            foreach (KeyValuePair<string, UIElement> pair in _elements)
            {
                pair.Value.Hide(instant);
                FireHiddenSignals(pair.Key, pair.Value);
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

        bool IUiService.TryGet(string screenId, out UIScreen screen)
        {
            return TryGetScreen(screenId, out screen);
        }

        private void FireShownSignals(string elementId, UIElement element)
        {
            _signalBus?.Fire(new UIElementShownSignal(elementId, element.GetType().Name));

            UIScreen screen = element as UIScreen;
            if (screen != null)
            {
                _signalBus?.Fire(new UIScreenShownSignal(elementId));
                _signalBus?.Fire(new UiScreenShownSignal(elementId));
            }
        }

        private void FireHiddenSignals(string elementId, UIElement element)
        {
            _signalBus?.Fire(new UIElementHiddenSignal(elementId, element.GetType().Name));

            UIScreen screen = element as UIScreen;
            if (screen != null)
            {
                _signalBus?.Fire(new UIScreenHiddenSignal(elementId));
                _signalBus?.Fire(new UiScreenHiddenSignal(elementId));
            }
        }
    }
}
