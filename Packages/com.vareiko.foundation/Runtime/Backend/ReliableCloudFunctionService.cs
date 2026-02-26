using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using Vareiko.Foundation.Common;
using Vareiko.Foundation.Connectivity;
using UnityEngine;
using Zenject;

namespace Vareiko.Foundation.Backend
{
    public sealed class ReliableCloudFunctionService : ICloudFunctionService, IInitializable, System.IDisposable
    {
        private readonly ICloudFunctionService _inner;
        private readonly IConnectivityService _connectivityService;
        private readonly BackendReliabilityConfig _config;
        private readonly SignalBus _signalBus;
        private readonly RetryPolicy _retryPolicy;
        private readonly ICloudFunctionQueueStore _queueStore;
        private readonly Queue<CloudFunctionQueueItem> _queue = new Queue<CloudFunctionQueueItem>();
        private bool _isFlushing;

        [Inject]
        public ReliableCloudFunctionService(
            [Inject(Id = "CloudFunctionInner")] ICloudFunctionService inner,
            [InjectOptional] IConnectivityService connectivityService = null,
            [InjectOptional] BackendReliabilityConfig config = null,
            [InjectOptional] SignalBus signalBus = null,
            [InjectOptional] ICloudFunctionQueueStore queueStore = null)
        {
            _inner = inner;
            _connectivityService = connectivityService;
            _config = config;
            _signalBus = signalBus;
            _queueStore = queueStore;

            bool retryEnabled = _config == null || _config.EnableRetry;
            int attempts = _config != null ? _config.MaxAttempts : 3;
            int delay = _config != null ? _config.InitialDelayMs : 250;
            _retryPolicy = new RetryPolicy(retryEnabled, attempts, delay);
        }

        public void Initialize()
        {
            RestorePersistedQueue();

            bool autoFlushOnReconnect = _config == null || _config.AutoFlushQueueOnReconnect;
            if (autoFlushOnReconnect && _connectivityService != null && _connectivityService.IsOnline && _queue.Count > 0)
            {
                FlushQueueAsync().Forget();
            }

            if (_signalBus == null || _connectivityService == null)
            {
                return;
            }

            if (!autoFlushOnReconnect)
            {
                return;
            }

            _signalBus.Subscribe<ConnectivityChangedSignal>(OnConnectivityChanged);
        }

        public void Dispose()
        {
            PersistQueue();

            if (_signalBus == null || _connectivityService == null)
            {
                return;
            }

            if (_config != null && !_config.AutoFlushQueueOnReconnect)
            {
                return;
            }

            _signalBus.Unsubscribe<ConnectivityChangedSignal>(OnConnectivityChanged);
        }

        public async UniTask<CloudFunctionResult> ExecuteAsync(string functionName, string payloadJson = null, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (string.IsNullOrWhiteSpace(functionName))
            {
                return new CloudFunctionResult(false, string.Empty, "Function name is null or empty.");
            }

            if (ShouldQueueBecauseOffline())
            {
                Enqueue(functionName, payloadJson, "Offline");
                return new CloudFunctionResult(false, string.Empty, "Cloud function has been queued because connection is offline.");
            }

            CloudFunctionResult result = await _retryPolicy.ExecuteAsync(
                token => _inner.ExecuteAsync(functionName, payloadJson, token),
                response => response.Success,
                (attempt, maxAttempts, _) =>
                {
                    _signalBus?.Fire(new BackendOperationRetriedSignal("CloudFunction:" + functionName, attempt, maxAttempts, string.Empty));
                },
                cancellationToken);

            if (!result.Success && ShouldQueueFailedOperations())
            {
                Enqueue(functionName, payloadJson, "Failure");
            }

            return result;
        }

        private bool ShouldQueueBecauseOffline()
        {
            if (_connectivityService == null)
            {
                return false;
            }

            if (!IsQueueEnabled())
            {
                return false;
            }

            return !_connectivityService.IsOnline;
        }

        private bool ShouldQueueFailedOperations()
        {
            return IsQueueEnabled() && (_config == null || _config.QueueFailedCloudFunctions);
        }

        private void Enqueue(string functionName, string payloadJson, string reason)
        {
            if (string.IsNullOrWhiteSpace(functionName))
            {
                return;
            }

            int maxSize = _config != null ? _config.MaxQueuedCloudFunctions : 32;
            while (_queue.Count >= maxSize)
            {
                _queue.Dequeue();
            }

            _queue.Enqueue(new CloudFunctionQueueItem(functionName.Trim(), payloadJson ?? string.Empty));
            PersistQueue();
            _signalBus?.Fire(new CloudFunctionQueuedSignal(functionName, _queue.Count, reason));
        }

        private void OnConnectivityChanged(ConnectivityChangedSignal signal)
        {
            if (!signal.IsOnline || _queue.Count == 0 || _isFlushing)
            {
                return;
            }

            FlushQueueAsync().Forget();
        }

        private async UniTaskVoid FlushQueueAsync()
        {
            _isFlushing = true;
            int flushed = 0;
            int startCount = _queue.Count;
            try
            {
                while (_queue.Count > 0)
                {
                    CloudFunctionQueueItem queued = _queue.Peek();
                    CloudFunctionResult result = await _inner.ExecuteAsync(queued.FunctionName, queued.PayloadJson);
                    if (!result.Success)
                    {
                        break;
                    }

                    flushed++;
                    _queue.Dequeue();
                }
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                _isFlushing = false;
            }

            PersistQueue();
            _signalBus?.Fire(new CloudFunctionQueueFlushedSignal(startCount, flushed, _queue.Count));
        }

        private void RestorePersistedQueue()
        {
            if (!ShouldPersistQueue())
            {
                return;
            }

            IReadOnlyList<CloudFunctionQueueItem> saved;
            try
            {
                saved = _queueStore.Load();
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
                return;
            }

            if (saved == null || saved.Count == 0)
            {
                return;
            }

            int maxSize = _config != null ? _config.MaxQueuedCloudFunctions : 32;
            int start = saved.Count > maxSize ? saved.Count - maxSize : 0;
            int restored = 0;

            for (int i = start; i < saved.Count; i++)
            {
                CloudFunctionQueueItem item = saved[i];
                if (string.IsNullOrWhiteSpace(item.FunctionName))
                {
                    continue;
                }

                _queue.Enqueue(new CloudFunctionQueueItem(item.FunctionName.Trim(), item.PayloadJson ?? string.Empty));
                restored++;
            }

            if (restored <= 0)
            {
                try
                {
                    _queueStore.Clear();
                }
                catch (System.Exception exception)
                {
                    Debug.LogException(exception);
                }
                return;
            }

            PersistQueue();
            _signalBus?.Fire(new CloudFunctionQueueRestoredSignal(restored));
        }

        private void PersistQueue()
        {
            if (!ShouldPersistQueue())
            {
                return;
            }

            if (_queue.Count == 0)
            {
                try
                {
                    _queueStore.Clear();
                }
                catch (System.Exception exception)
                {
                    Debug.LogException(exception);
                }
                return;
            }

            List<CloudFunctionQueueItem> snapshot = new List<CloudFunctionQueueItem>(_queue.Count);
            foreach (CloudFunctionQueueItem item in _queue)
            {
                snapshot.Add(item);
            }

            try
            {
                _queueStore.Save(snapshot);
            }
            catch (System.Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private bool IsQueueEnabled()
        {
            return _config == null || _config.EnableCloudFunctionQueue;
        }

        private bool ShouldPersistQueue()
        {
            return IsQueueEnabled() &&
                   (_config == null || _config.EnablePersistentCloudFunctionQueue) &&
                   _queueStore != null;
        }
    }
}
