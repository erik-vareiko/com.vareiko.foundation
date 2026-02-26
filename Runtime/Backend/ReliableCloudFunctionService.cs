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
        private readonly Queue<QueuedCloudFunction> _queue = new Queue<QueuedCloudFunction>();
        private bool _isFlushing;

        [Inject]
        public ReliableCloudFunctionService(
            [Inject(Id = "CloudFunctionInner")] ICloudFunctionService inner,
            [InjectOptional] IConnectivityService connectivityService = null,
            [InjectOptional] BackendReliabilityConfig config = null,
            [InjectOptional] SignalBus signalBus = null)
        {
            _inner = inner;
            _connectivityService = connectivityService;
            _config = config;
            _signalBus = signalBus;

            bool retryEnabled = _config == null || _config.EnableRetry;
            int attempts = _config != null ? _config.MaxAttempts : 3;
            int delay = _config != null ? _config.InitialDelayMs : 250;
            _retryPolicy = new RetryPolicy(retryEnabled, attempts, delay);
        }

        public void Initialize()
        {
            if (_signalBus == null || _connectivityService == null)
            {
                return;
            }

            if (_config != null && !_config.AutoFlushQueueOnReconnect)
            {
                return;
            }

            _signalBus.Subscribe<ConnectivityChangedSignal>(OnConnectivityChanged);
        }

        public void Dispose()
        {
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

            if (_config != null && !_config.EnableCloudFunctionQueue)
            {
                return false;
            }

            return !_connectivityService.IsOnline;
        }

        private bool ShouldQueueFailedOperations()
        {
            if (_config == null)
            {
                return true;
            }

            return _config.EnableCloudFunctionQueue && _config.QueueFailedCloudFunctions;
        }

        private void Enqueue(string functionName, string payloadJson, string reason)
        {
            int maxSize = _config != null ? _config.MaxQueuedCloudFunctions : 32;
            while (_queue.Count >= maxSize)
            {
                _queue.Dequeue();
            }

            _queue.Enqueue(new QueuedCloudFunction(functionName, payloadJson));
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
                    QueuedCloudFunction queued = _queue.Peek();
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

            _signalBus?.Fire(new CloudFunctionQueueFlushedSignal(startCount, flushed, _queue.Count));
        }

        private readonly struct QueuedCloudFunction
        {
            public readonly string FunctionName;
            public readonly string PayloadJson;

            public QueuedCloudFunction(string functionName, string payloadJson)
            {
                FunctionName = functionName;
                PayloadJson = payloadJson;
            }
        }
    }
}
