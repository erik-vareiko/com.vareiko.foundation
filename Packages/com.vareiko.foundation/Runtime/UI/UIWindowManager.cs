using System;
using System.Collections.Generic;
using Zenject;

namespace Vareiko.Foundation.UI
{
    public sealed class UIWindowManager : IUIWindowManager
    {
        private readonly IUIService _uiService;
        private readonly SignalBus _signalBus;
        private readonly List<WindowRequest> _queue = new List<WindowRequest>();

        private long _requestSequence;
        private string _currentWindowId = string.Empty;

        [Inject]
        public UIWindowManager(IUIService uiService, [InjectOptional] SignalBus signalBus = null)
        {
            _uiService = uiService;
            _signalBus = signalBus;
        }

        public string CurrentWindowId => _currentWindowId;
        public int QueueCount => _queue.Count;

        public bool Enqueue(string windowId, bool instant = true, int priority = 0, bool allowDuplicate = false)
        {
            UIWindow window;
            if (!_uiService.TryGetWindow(windowId, out window))
            {
                return false;
            }

            if (!allowDuplicate)
            {
                if (string.Equals(_currentWindowId, windowId, StringComparison.Ordinal))
                {
                    return false;
                }

                for (int i = 0; i < _queue.Count; i++)
                {
                    if (string.Equals(_queue[i].WindowId, windowId, StringComparison.Ordinal))
                    {
                        return false;
                    }
                }
            }

            int effectivePriority = priority != 0 ? priority : window.DefaultPriority;
            WindowRequest request = new WindowRequest(windowId, instant, effectivePriority, _requestSequence++);
            if (string.IsNullOrEmpty(_currentWindowId))
            {
                return ShowWindow(request);
            }

            _queue.Add(request);
            _queue.Sort(WindowRequestComparer.Instance);
            _signalBus?.Fire(new UIWindowQueuedSignal(request.WindowId, _queue.Count, request.Priority));
            return true;
        }

        public bool TryCloseCurrent(bool instant = true)
        {
            if (string.IsNullOrEmpty(_currentWindowId))
            {
                return false;
            }

            string closingId = _currentWindowId;
            _uiService.Hide(closingId, instant);
            _currentWindowId = string.Empty;
            bool hasNext = _queue.Count > 0;
            _signalBus?.Fire(new UIWindowClosedSignal(closingId, hasNext));

            TryShowNextQueued();
            return true;
        }

        public bool TryClose(string windowId, bool instant = true)
        {
            if (string.IsNullOrWhiteSpace(windowId))
            {
                return false;
            }

            if (string.Equals(_currentWindowId, windowId, StringComparison.Ordinal))
            {
                return TryCloseCurrent(instant);
            }

            int removed = _queue.RemoveAll(x => string.Equals(x.WindowId, windowId, StringComparison.Ordinal));
            return removed > 0;
        }

        public void ClearQueue()
        {
            _queue.Clear();
            if (string.IsNullOrEmpty(_currentWindowId))
            {
                _signalBus?.Fire(new UIWindowQueueDrainedSignal());
            }
        }

        private bool ShowWindow(WindowRequest request)
        {
            if (!_uiService.Show(request.WindowId, request.Instant))
            {
                return false;
            }

            _currentWindowId = request.WindowId;
            _signalBus?.Fire(new UIWindowShownSignal(request.WindowId));
            return true;
        }

        private void TryShowNextQueued()
        {
            while (_queue.Count > 0)
            {
                WindowRequest request = _queue[0];
                _queue.RemoveAt(0);
                if (ShowWindow(request))
                {
                    return;
                }
            }

            _signalBus?.Fire(new UIWindowQueueDrainedSignal());
        }

        private readonly struct WindowRequest
        {
            public readonly string WindowId;
            public readonly bool Instant;
            public readonly int Priority;
            public readonly long Sequence;

            public WindowRequest(string windowId, bool instant, int priority, long sequence)
            {
                WindowId = windowId;
                Instant = instant;
                Priority = priority;
                Sequence = sequence;
            }
        }

        private sealed class WindowRequestComparer : IComparer<WindowRequest>
        {
            public static readonly WindowRequestComparer Instance = new WindowRequestComparer();

            public int Compare(WindowRequest x, WindowRequest y)
            {
                int byPriority = y.Priority.CompareTo(x.Priority);
                if (byPriority != 0)
                {
                    return byPriority;
                }

                return x.Sequence.CompareTo(y.Sequence);
            }
        }
    }
}
