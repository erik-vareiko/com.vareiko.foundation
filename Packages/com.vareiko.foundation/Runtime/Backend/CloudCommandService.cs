using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Vareiko.Foundation.Common;
using Vareiko.Foundation.Connectivity;
using Vareiko.Foundation.Signals;
using UnityEngine;
using Zenject;

namespace Vareiko.Foundation.Backend
{
    public sealed class CloudCommandService : ICloudCommandService, IInitializable, IDisposable
    {
        private readonly ICloudFunctionService _transport;
        private readonly BackendReliabilityConfig _reliabilityConfig;
        private readonly BackendCommandConfig _commandConfig;
        private readonly ICloudCommandRetryClassifier _retryClassifier;
        private readonly ICloudCommandQueueStore _queueStore;
        private readonly IConnectivityService _connectivityService;
        private readonly IFoundationSignalBus _signalBus;
        private readonly RetryPolicy _retryPolicy;
        private readonly List<IDisposable> _signalSubscriptions = new List<IDisposable>();

        private readonly List<CloudCommandQueueItem> _queue = new List<CloudCommandQueueItem>();
        private bool _isFlushing;

        [Inject]
        public CloudCommandService(
            [Inject(Id = "CloudFunctionInner")] ICloudFunctionService transport,
            [InjectOptional] BackendReliabilityConfig reliabilityConfig = null,
            [InjectOptional] BackendCommandConfig commandConfig = null,
            [InjectOptional] ICloudCommandRetryClassifier retryClassifier = null,
            [InjectOptional] ICloudCommandQueueStore queueStore = null,
            [InjectOptional] IConnectivityService connectivityService = null,
            [InjectOptional] IFoundationSignalBus signalBus = null)
        {
            _transport = transport;
            _reliabilityConfig = reliabilityConfig;
            _commandConfig = commandConfig;
            _retryClassifier = retryClassifier;
            _queueStore = queueStore;
            _connectivityService = connectivityService;
            _signalBus = signalBus;

            bool retryEnabled = _reliabilityConfig == null || _reliabilityConfig.EnableRetry;
            int attempts = _reliabilityConfig != null ? _reliabilityConfig.MaxAttempts : 3;
            int delay = _reliabilityConfig != null ? _reliabilityConfig.InitialDelayMs : 250;
            _retryPolicy = new RetryPolicy(retryEnabled, attempts, delay);
        }

        public void Initialize()
        {
            RestorePersistedQueue();

            bool autoFlushOnReconnect = _reliabilityConfig == null || _reliabilityConfig.AutoFlushQueueOnReconnect;
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

            _signalSubscriptions.Add(_signalBus.Subscribe<ConnectivityChangedSignal>(OnConnectivityChanged));
        }

        public void Dispose()
        {
            PersistQueue();

            if (_signalBus == null || _connectivityService == null)
            {
                return;
            }

            if (_reliabilityConfig != null && !_reliabilityConfig.AutoFlushQueueOnReconnect)
            {
                return;
            }

            for (int i = 0; i < _signalSubscriptions.Count; i++)
            {
                _signalSubscriptions[i].Dispose();
            }
            _signalSubscriptions.Clear();
        }

        public async UniTask<CloudCommandResponse> ExecuteAsync(CloudCommandRequest request, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();

            CloudCommandRequest normalized = NormalizeRequest(request);
            CloudCommandResponse validation = Validate(normalized);
            if (!validation.Success)
            {
                return validation;
            }

            if (ShouldQueueBecauseOffline())
            {
                Enqueue(normalized, "Offline");
                return CloudCommandResponse.Fail("Queue.Offline", "Cloud command has been queued because connection is offline.", true);
            }

            CloudCommandResponse result = await ExecuteWithRetryAsync(normalized, cancellationToken);
            if (!result.Success && result.IsRetryable && ShouldQueueFailedOperations())
            {
                Enqueue(normalized, "Failure");
            }

            return result;
        }

        private async UniTask<CloudCommandResponse> ExecuteWithRetryAsync(CloudCommandRequest request, CancellationToken cancellationToken)
        {
            int maxAttempts = _retryPolicy.Enabled ? _retryPolicy.MaxAttempts : 1;
            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                cancellationToken.ThrowIfCancellationRequested();
                CloudCommandResponse response = await ExecuteSingleAsync(request, cancellationToken);
                if (response.Success || !response.IsRetryable || attempt >= maxAttempts)
                {
                    return response;
                }

                int delayMs = _retryPolicy.GetDelayMs(attempt + 1);
                _signalBus?.Publish(new BackendOperationRetriedSignal("CloudCommand:" + request.CommandName, attempt, maxAttempts, response.ErrorCode));
                if (delayMs > 0)
                {
                    await UniTask.Delay(delayMs, cancellationToken: cancellationToken);
                }
            }

            return CloudCommandResponse.Fail("Unknown", "Retry loop ended unexpectedly.");
        }

        private async UniTask<CloudCommandResponse> ExecuteSingleAsync(CloudCommandRequest request, CancellationToken cancellationToken)
        {
            string payload = SerializeRequestForTransport(request);
            CloudFunctionResult transportResult = await _transport.ExecuteAsync(GetGatewayFunctionName(), payload, cancellationToken);
            if (!transportResult.Success)
            {
                CloudCommandFailureClassification classification = ClassifyFailure(string.Empty, transportResult.Error);
                if (classification.Kind == CloudCommandFailureKind.SuccessLike)
                {
                    return CloudCommandResponse.Succeed(string.Empty, request.IdempotencyKey, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                }

                return CloudCommandResponse.Fail(classification.ErrorCode, transportResult.Error, classification.Kind == CloudCommandFailureKind.Retryable);
            }

            CloudCommandTransportResponse transportResponse;
            if (!TryDeserializeTransportResponse(transportResult.ResponseJson, out transportResponse))
            {
                return CloudCommandResponse.Succeed(transportResult.ResponseJson, request.IdempotencyKey, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            }

            CloudCommandResponse mapped = new CloudCommandResponse(
                transportResponse.Success,
                transportResponse.IsRetryable,
                transportResponse.ErrorCode,
                transportResponse.ErrorMessage,
                transportResponse.ResponseJson,
                transportResponse.ProcessedIdempotencyKey,
                transportResponse.ServerUnixMs);

            if (mapped.Success)
            {
                return mapped;
            }

            CloudCommandFailureClassification mappedClassification = ClassifyFailure(mapped.ErrorCode, mapped.ErrorMessage);
            if (mappedClassification.Kind == CloudCommandFailureKind.SuccessLike)
            {
                string processedKey = string.IsNullOrWhiteSpace(mapped.ProcessedIdempotencyKey)
                    ? request.IdempotencyKey
                    : mapped.ProcessedIdempotencyKey;
                return CloudCommandResponse.Succeed(mapped.ResponseJson, processedKey, mapped.ServerUnixMs);
            }

            return new CloudCommandResponse(
                false,
                mappedClassification.Kind == CloudCommandFailureKind.Retryable,
                mappedClassification.ErrorCode,
                mapped.ErrorMessage,
                mapped.ResponseJson,
                mapped.ProcessedIdempotencyKey,
                mapped.ServerUnixMs);
        }

        private CloudCommandRequest NormalizeRequest(CloudCommandRequest request)
        {
            string requestVersion = string.IsNullOrWhiteSpace(request.RequestVersion)
                ? (_commandConfig != null ? _commandConfig.DefaultRequestVersion : "1")
                : request.RequestVersion.Trim();
            long clientUnixMs = request.ClientUnixMs > 0 ? request.ClientUnixMs : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            return new CloudCommandRequest(
                request.CommandName?.Trim(),
                request.IdempotencyKey?.Trim(),
                request.CorrelationId?.Trim(),
                requestVersion,
                request.PayloadJson,
                request.PlayerId?.Trim(),
                clientUnixMs,
                request.Meta);
        }

        private CloudCommandResponse Validate(CloudCommandRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.CommandName))
            {
                return CloudCommandResponse.Fail("Validation.CommandName", "CommandName is required.");
            }

            if (string.IsNullOrWhiteSpace(request.IdempotencyKey))
            {
                return CloudCommandResponse.Fail("Validation.IdempotencyKey", "IdempotencyKey is required.");
            }

            if (string.IsNullOrWhiteSpace(request.RequestVersion))
            {
                return CloudCommandResponse.Fail("Validation.RequestVersion", "RequestVersion is required.");
            }

            if (request.PayloadJson == null)
            {
                return CloudCommandResponse.Fail("Validation.PayloadJson", "PayloadJson must not be null.");
            }

            int maxPayloadBytes = _commandConfig != null ? _commandConfig.MaxPayloadBytes : 65536;
            int payloadBytes = Encoding.UTF8.GetByteCount(request.PayloadJson);
            if (payloadBytes > maxPayloadBytes)
            {
                return CloudCommandResponse.Fail("Validation.PayloadTooLarge", $"PayloadJson exceeds {maxPayloadBytes} bytes.");
            }

            if (!LooksLikeUuidV7(request.IdempotencyKey))
            {
                return CloudCommandResponse.Fail("Validation.IdempotencyKeyFormat", "IdempotencyKey must be a UUIDv7 string.");
            }

            return new CloudCommandResponse(true, false, string.Empty, string.Empty, string.Empty, request.IdempotencyKey, request.ClientUnixMs);
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
            return IsQueueEnabled() && (_reliabilityConfig == null || _reliabilityConfig.QueueFailedCloudFunctions);
        }

        private void Enqueue(CloudCommandRequest request, string reason)
        {
            if (string.IsNullOrWhiteSpace(request.CommandName))
            {
                return;
            }

            int maxSize = _reliabilityConfig != null ? _reliabilityConfig.MaxQueuedCloudFunctions : 32;
            while (_queue.Count >= maxSize)
            {
                _queue.RemoveAt(0);
            }

            string payload = SerializeRequestForTransport(request);
            string requestJson = SerializeRequestForQueue(request);
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            _queue.Add(CloudCommandQueueItem.Create(GetGatewayFunctionName(), payload, requestJson, request.IdempotencyKey, now));
            PersistQueue();
            _signalBus?.Publish(new CloudFunctionQueuedSignal(request.CommandName, _queue.Count, reason));
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
                    long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    CloudCommandQueueItem head = _queue[0];
                    if (IsExpired(head, now))
                    {
                        _queue.RemoveAt(0);
                        continue;
                    }

                    CloudCommandQueueItem attempted = head.RegisterAttempt(now);
                    _queue[0] = attempted;

                    CloudCommandResponse result = await ExecuteQueuedItemAsync(attempted);
                    if (result.Success || !result.IsRetryable)
                    {
                        flushed++;
                        _queue.RemoveAt(0);
                        continue;
                    }

                    break;
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                _isFlushing = false;
            }

            PersistQueue();
            _signalBus?.Publish(new CloudFunctionQueueFlushedSignal(startCount, flushed, _queue.Count));
        }

        private async UniTask<CloudCommandResponse> ExecuteQueuedItemAsync(CloudCommandQueueItem item)
        {
            CloudCommandRequest queuedRequest;
            if (TryDeserializeRequestFromQueue(item.RequestJson, out queuedRequest))
            {
                CloudCommandResponse validation = Validate(queuedRequest);
                if (!validation.Success)
                {
                    return validation;
                }

                return await ExecuteSingleAsync(queuedRequest, CancellationToken.None);
            }

            CloudFunctionResult transportResult = await _transport.ExecuteAsync(item.FunctionName, item.PayloadJson, CancellationToken.None);
            if (transportResult.Success)
            {
                return CloudCommandResponse.Succeed(transportResult.ResponseJson, item.IdempotencyKey, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            }

            CloudCommandFailureClassification classification = ClassifyFailure(string.Empty, transportResult.Error);
            if (classification.Kind == CloudCommandFailureKind.SuccessLike)
            {
                return CloudCommandResponse.Succeed(string.Empty, item.IdempotencyKey, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            }

            return CloudCommandResponse.Fail(classification.ErrorCode, transportResult.Error, classification.Kind == CloudCommandFailureKind.Retryable);
        }

        private void RestorePersistedQueue()
        {
            if (!ShouldPersistQueue())
            {
                return;
            }

            IReadOnlyList<CloudCommandQueueItem> saved;
            try
            {
                saved = _queueStore.Load();
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
                return;
            }

            if (saved == null || saved.Count == 0)
            {
                return;
            }

            int maxSize = _reliabilityConfig != null ? _reliabilityConfig.MaxQueuedCloudFunctions : 32;
            int start = saved.Count > maxSize ? saved.Count - maxSize : 0;
            int restored = 0;
            long now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            for (int i = start; i < saved.Count; i++)
            {
                CloudCommandQueueItem item = saved[i];
                if (string.IsNullOrWhiteSpace(item.FunctionName))
                {
                    Debug.LogWarning("CloudCommandService: skipping queue item with empty function name.");
                    continue;
                }

                if (IsExpired(item, now))
                {
                    continue;
                }

                _queue.Add(item);
                restored++;
            }

            if (restored <= 0)
            {
                try
                {
                    _queueStore.Clear();
                }
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }

                return;
            }

            PersistQueue();
            _signalBus?.Publish(new CloudFunctionQueueRestoredSignal(restored));
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
                catch (Exception exception)
                {
                    Debug.LogException(exception);
                }

                return;
            }

            try
            {
                _queueStore.Save(_queue);
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private bool IsQueueEnabled()
        {
            return _reliabilityConfig == null || _reliabilityConfig.EnableCloudFunctionQueue;
        }

        private bool ShouldPersistQueue()
        {
            return IsQueueEnabled() &&
                   (_reliabilityConfig == null || _reliabilityConfig.EnablePersistentCloudFunctionQueue) &&
                   _queueStore != null;
        }

        private string GetGatewayFunctionName()
        {
            return _commandConfig != null ? _commandConfig.GatewayFunctionName : "CommandGateway";
        }

        private bool IsExpired(CloudCommandQueueItem item, long nowUnixMs)
        {
            int ttlHours = _commandConfig != null ? _commandConfig.QueueTtlHours : 24;
            long ttlMs = TimeSpan.FromHours(ttlHours).Ticks / TimeSpan.TicksPerMillisecond;
            if (ttlMs <= 0)
            {
                return false;
            }

            if (item.FirstQueuedUnixMs <= 0)
            {
                return false;
            }

            return nowUnixMs - item.FirstQueuedUnixMs > ttlMs;
        }

        private CloudCommandFailureClassification ClassifyFailure(string errorCode, string errorMessage)
        {
            if (_retryClassifier == null)
            {
                return new CloudCommandFailureClassification(CloudCommandFailureKind.NonRetryable, string.IsNullOrWhiteSpace(errorCode) ? "Unknown" : errorCode);
            }

            return _retryClassifier.Classify(errorCode, errorMessage);
        }

        private static bool LooksLikeUuidV7(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            string trimmed = value.Trim();
            if (trimmed.Length != 36)
            {
                return false;
            }

            if (!Guid.TryParse(trimmed, out _))
            {
                return false;
            }

            return char.ToLowerInvariant(trimmed[14]) == '7';
        }

        private static string SerializeRequestForTransport(CloudCommandRequest request)
        {
            CloudCommandTransportRequest transport = new CloudCommandTransportRequest
            {
                CommandName = request.CommandName ?? string.Empty,
                IdempotencyKey = request.IdempotencyKey ?? string.Empty,
                CorrelationId = request.CorrelationId ?? string.Empty,
                RequestVersion = request.RequestVersion ?? string.Empty,
                PayloadJson = request.PayloadJson ?? string.Empty,
                PlayerId = request.PlayerId ?? string.Empty,
                ClientUnixMs = request.ClientUnixMs,
                Meta = ToMetaEntries(request.Meta)
            };

            return JsonUtility.ToJson(transport);
        }

        private static string SerializeRequestForQueue(CloudCommandRequest request)
        {
            CloudCommandStoredRequest stored = new CloudCommandStoredRequest
            {
                CommandName = request.CommandName ?? string.Empty,
                IdempotencyKey = request.IdempotencyKey ?? string.Empty,
                CorrelationId = request.CorrelationId ?? string.Empty,
                RequestVersion = request.RequestVersion ?? string.Empty,
                PayloadJson = request.PayloadJson ?? string.Empty,
                PlayerId = request.PlayerId ?? string.Empty,
                ClientUnixMs = request.ClientUnixMs,
                Meta = ToMetaEntries(request.Meta)
            };

            return JsonUtility.ToJson(stored);
        }

        private static bool TryDeserializeRequestFromQueue(string json, out CloudCommandRequest request)
        {
            request = default;
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            CloudCommandStoredRequest stored;
            try
            {
                stored = JsonUtility.FromJson<CloudCommandStoredRequest>(json);
            }
            catch
            {
                return false;
            }

            if (stored == null)
            {
                return false;
            }

            request = new CloudCommandRequest(
                stored.CommandName,
                stored.IdempotencyKey,
                stored.CorrelationId,
                stored.RequestVersion,
                stored.PayloadJson,
                stored.PlayerId,
                stored.ClientUnixMs,
                ToDictionary(stored.Meta));
            return true;
        }

        private static bool TryDeserializeTransportResponse(string json, out CloudCommandTransportResponse response)
        {
            response = null;
            if (string.IsNullOrWhiteSpace(json))
            {
                return false;
            }

            try
            {
                response = JsonUtility.FromJson<CloudCommandTransportResponse>(json);
            }
            catch
            {
                response = null;
            }

            return response != null;
        }

        private static List<CloudCommandMetaEntry> ToMetaEntries(IReadOnlyDictionary<string, string> meta)
        {
            if (meta == null || meta.Count == 0)
            {
                return new List<CloudCommandMetaEntry>();
            }

            List<CloudCommandMetaEntry> entries = new List<CloudCommandMetaEntry>(meta.Count);
            foreach (KeyValuePair<string, string> pair in meta)
            {
                if (string.IsNullOrWhiteSpace(pair.Key))
                {
                    continue;
                }

                entries.Add(new CloudCommandMetaEntry
                {
                    Key = pair.Key.Trim(),
                    Value = pair.Value ?? string.Empty
                });
            }

            return entries;
        }

        private static IReadOnlyDictionary<string, string> ToDictionary(List<CloudCommandMetaEntry> entries)
        {
            if (entries == null || entries.Count == 0)
            {
                return null;
            }

            Dictionary<string, string> result = new Dictionary<string, string>(entries.Count, StringComparer.Ordinal);
            for (int i = 0; i < entries.Count; i++)
            {
                CloudCommandMetaEntry entry = entries[i];
                if (entry == null || string.IsNullOrWhiteSpace(entry.Key))
                {
                    continue;
                }

                result[entry.Key.Trim()] = entry.Value ?? string.Empty;
            }

            return result;
        }

        [Serializable]
        private sealed class CloudCommandMetaEntry
        {
            public string Key;
            public string Value;
        }

        [Serializable]
        private sealed class CloudCommandStoredRequest
        {
            public string CommandName;
            public string IdempotencyKey;
            public string CorrelationId;
            public string RequestVersion;
            public string PayloadJson;
            public string PlayerId;
            public long ClientUnixMs;
            public List<CloudCommandMetaEntry> Meta = new List<CloudCommandMetaEntry>();
        }

        [Serializable]
        private sealed class CloudCommandTransportRequest
        {
            public string CommandName;
            public string IdempotencyKey;
            public string CorrelationId;
            public string RequestVersion;
            public string PayloadJson;
            public string PlayerId;
            public long ClientUnixMs;
            public List<CloudCommandMetaEntry> Meta = new List<CloudCommandMetaEntry>();
        }

        [Serializable]
        private sealed class CloudCommandTransportResponse
        {
            public bool Success;
            public bool IsRetryable;
            public string ErrorCode;
            public string ErrorMessage;
            public string ResponseJson;
            public string ProcessedIdempotencyKey;
            public long ServerUnixMs;
        }
    }
}
