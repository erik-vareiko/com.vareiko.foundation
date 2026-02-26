using System.Collections.Generic;
using Zenject;

namespace Vareiko.Foundation.UI
{
    public sealed class UiService : IUiService, IInitializable
    {
        private readonly UIScreenRegistry _registry;
        private readonly SignalBus _signalBus;
        private readonly Dictionary<string, UIScreen> _screens = new Dictionary<string, UIScreen>();

        [Inject]
        public UiService([InjectOptional] UIScreenRegistry registry = null, [InjectOptional] SignalBus signalBus = null)
        {
            _registry = registry;
            _signalBus = signalBus;
        }

        public void Initialize()
        {
            _screens.Clear();

            if (_registry != null)
            {
                _registry.BuildMap();
                foreach (KeyValuePair<string, UIScreen> pair in _registry.Enumerate())
                {
                    _screens[pair.Key] = pair.Value;
                }
            }

            _signalBus?.Fire(new UiReadySignal(_screens.Count));
        }

        public bool Show(string screenId, bool instant = true)
        {
            UIScreen screen;
            if (!TryGet(screenId, out screen))
            {
                return false;
            }

            screen.Show(instant);
            _signalBus?.Fire(new UiScreenShownSignal(screenId));
            return true;
        }

        public bool Hide(string screenId, bool instant = true)
        {
            UIScreen screen;
            if (!TryGet(screenId, out screen))
            {
                return false;
            }

            screen.Hide(instant);
            _signalBus?.Fire(new UiScreenHiddenSignal(screenId));
            return true;
        }

        public bool Toggle(string screenId, bool instant = true)
        {
            UIScreen screen;
            if (!TryGet(screenId, out screen))
            {
                return false;
            }

            bool nextVisible = !screen.IsVisible;
            if (nextVisible)
            {
                return Show(screenId, instant);
            }

            return Hide(screenId, instant);
        }

        public void HideAll(bool instant = true)
        {
            foreach (KeyValuePair<string, UIScreen> pair in _screens)
            {
                pair.Value.Hide(instant);
                _signalBus?.Fire(new UiScreenHiddenSignal(pair.Key));
            }
        }

        public bool TryGet(string screenId, out UIScreen screen)
        {
            if (string.IsNullOrWhiteSpace(screenId))
            {
                screen = null;
                return false;
            }

            return _screens.TryGetValue(screenId, out screen);
        }
    }
}
