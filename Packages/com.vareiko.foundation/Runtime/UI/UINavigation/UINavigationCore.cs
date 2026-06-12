using System.Collections.Generic;
using Vareiko.Foundation.UI;
using Vareiko.Foundation.Signals;

namespace Vareiko.Foundation.UINavigation
{
    public class UINavigationService : IUINavigationService
    {
        private readonly IUIService _uiService;
        private readonly IFoundationSignalBus _signalBus;
        private readonly Stack<string> _stack = new Stack<string>();

        public UINavigationService(IUIService uiService, IFoundationSignalBus signalBus = null)
        {
            _uiService = uiService;
            _signalBus = signalBus;
        }

        public string Current => _stack.Count > 0 ? _stack.Peek() : string.Empty;

        public bool Push(string screenId)
        {
            if (string.IsNullOrWhiteSpace(screenId))
            {
                return false;
            }

            string current = Current;
            if (!string.IsNullOrEmpty(current))
            {
                _uiService.Hide(current);
            }

            UIScreen screen;
            if (!_uiService.TryGetScreen(screenId, out screen) || !_uiService.Show(screenId))
            {
                if (!string.IsNullOrEmpty(current))
                {
                    _uiService.Show(current);
                }

                return false;
            }

            _stack.Push(screenId);
            FireChanged();
            return true;
        }

        public bool Replace(string screenId)
        {
            if (_stack.Count > 0)
            {
                string current = _stack.Pop();
                _uiService.Hide(current);
            }

            return Push(screenId);
        }

        public bool Pop()
        {
            if (_stack.Count == 0)
            {
                return false;
            }

            string current = _stack.Pop();
            _uiService.Hide(current);

            if (_stack.Count > 0)
            {
                _uiService.Show(_stack.Peek());
            }

            FireChanged();
            return true;
        }

        public void Clear()
        {
            while (_stack.Count > 0)
            {
                _uiService.Hide(_stack.Pop());
            }

            FireChanged();
        }

        private void FireChanged()
        {
            _signalBus?.Publish(new UINavigationChangedSignal(Current, _stack.Count));
        }
    }
}
