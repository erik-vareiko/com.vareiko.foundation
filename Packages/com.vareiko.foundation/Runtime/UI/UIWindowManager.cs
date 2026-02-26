using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Zenject;

namespace Vareiko.Foundation.UI
{
    public sealed class UIWindowManager : IUIWindowManager, IUIWindowResultService
    {
        private readonly IUIService _uiService;
        private readonly SignalBus _signalBus;
        private readonly List<WindowRequest> _queue = new List<WindowRequest>();

        private long _requestSequence;
        private string _currentWindowId = string.Empty;
        private WindowRequest _currentRequest;

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
            return TryEnqueueInternal(windowId, instant, priority, allowDuplicate, null);
        }

        public UniTask<UIWindowResult> EnqueueAndWaitAsync(string windowId, bool instant = true, int priority = 0, bool allowDuplicate = false)
        {
            UniTaskCompletionSource<UIWindowResult> completion = new UniTaskCompletionSource<UIWindowResult>();
            if (!TryEnqueueInternal(windowId, instant, priority, allowDuplicate, completion))
            {
                completion.TrySetResult(new UIWindowResult(windowId, UIWindowResultStatus.Rejected));
            }

            return completion.Task;
        }

        public bool TryResolveCurrent(UIWindowResultStatus status, string payload = "", bool instant = true)
        {
            if (string.IsNullOrEmpty(_currentWindowId) || _currentRequest == null)
            {
                return false;
            }

            return ResolveAndCloseCurrent(_currentRequest, status, payload, instant);
        }

        public bool TryResolve(string windowId, UIWindowResultStatus status, string payload = "", bool instant = true)
        {
            if (string.IsNullOrWhiteSpace(windowId))
            {
                return false;
            }

            windowId = windowId.Trim();
            if (_currentRequest != null && string.Equals(_currentRequest.WindowId, windowId, StringComparison.Ordinal))
            {
                return ResolveAndCloseCurrent(_currentRequest, status, payload, instant);
            }

            bool removedAny = false;
            for (int i = _queue.Count - 1; i >= 0; i--)
            {
                WindowRequest request = _queue[i];
                if (!string.Equals(request.WindowId, windowId, StringComparison.Ordinal))
                {
                    continue;
                }

                _queue.RemoveAt(i);
                CompleteRequest(request, status, payload);
                removedAny = true;
            }

            return removedAny;
        }

        public bool TryCloseCurrent(bool instant = true)
        {
            if (string.IsNullOrEmpty(_currentWindowId) || _currentRequest == null)
            {
                return false;
            }

            return ResolveAndCloseCurrent(_currentRequest, UIWindowResultStatus.Closed, string.Empty, instant);
        }

        public bool TryClose(string windowId, bool instant = true)
        {
            if (string.IsNullOrWhiteSpace(windowId))
            {
                return false;
            }

            windowId = windowId.Trim();
            if (string.Equals(_currentWindowId, windowId, StringComparison.Ordinal))
            {
                return TryCloseCurrent(instant);
            }

            bool removedAny = false;
            for (int i = _queue.Count - 1; i >= 0; i--)
            {
                WindowRequest request = _queue[i];
                if (!string.Equals(request.WindowId, windowId, StringComparison.Ordinal))
                {
                    continue;
                }

                _queue.RemoveAt(i);
                CompleteRequest(request, UIWindowResultStatus.Closed, string.Empty);
                removedAny = true;
            }

            return removedAny;
        }

        public void ClearQueue()
        {
            for (int i = 0; i < _queue.Count; i++)
            {
                CompleteRequest(_queue[i], UIWindowResultStatus.Closed, string.Empty);
            }

            _queue.Clear();
            if (string.IsNullOrEmpty(_currentWindowId))
            {
                _signalBus?.Fire(new UIWindowQueueDrainedSignal());
            }
        }

        private bool TryEnqueueInternal(string windowId, bool instant, int priority, bool allowDuplicate, UniTaskCompletionSource<UIWindowResult> completion)
        {
            if (string.IsNullOrWhiteSpace(windowId))
            {
                return false;
            }

            windowId = windowId.Trim();
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
            WindowRequest request = new WindowRequest(windowId, instant, effectivePriority, _requestSequence++, completion);
            if (string.IsNullOrEmpty(_currentWindowId))
            {
                return ShowWindow(request);
            }

            _queue.Add(request);
            _queue.Sort(WindowRequestComparer.Instance);
            _signalBus?.Fire(new UIWindowQueuedSignal(request.WindowId, _queue.Count, request.Priority));
            return true;
        }

        private bool ShowWindow(WindowRequest request)
        {
            if (!_uiService.Show(request.WindowId, request.Instant))
            {
                return false;
            }

            _currentRequest = request;
            _currentWindowId = request.WindowId;
            _signalBus?.Fire(new UIWindowShownSignal(request.WindowId));
            return true;
        }

        private bool ResolveAndCloseCurrent(WindowRequest request, UIWindowResultStatus status, string payload, bool instant)
        {
            string closingId = request.WindowId;
            _uiService.Hide(closingId, instant);

            _currentWindowId = string.Empty;
            _currentRequest = null;
            bool hasNext = _queue.Count > 0;
            _signalBus?.Fire(new UIWindowClosedSignal(closingId, hasNext));
            CompleteRequest(request, status, payload);

            TryShowNextQueued();
            return true;
        }

        private void CompleteRequest(WindowRequest request, UIWindowResultStatus status, string payload)
        {
            if (request == null)
            {
                return;
            }

            _signalBus?.Fire(new UIWindowResolvedSignal(request.WindowId, status, payload));
            request.Completion?.TrySetResult(new UIWindowResult(request.WindowId, status, payload));
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

                CompleteRequest(request, UIWindowResultStatus.Rejected, "Failed to show window.");
            }

            _signalBus?.Fire(new UIWindowQueueDrainedSignal());
        }

        private sealed class WindowRequest
        {
            public readonly string WindowId;
            public readonly bool Instant;
            public readonly int Priority;
            public readonly long Sequence;
            public readonly UniTaskCompletionSource<UIWindowResult> Completion;

            public WindowRequest(string windowId, bool instant, int priority, long sequence, UniTaskCompletionSource<UIWindowResult> completion)
            {
                WindowId = windowId;
                Instant = instant;
                Priority = priority;
                Sequence = sequence;
                Completion = completion;
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
