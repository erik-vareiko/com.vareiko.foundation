using System.Collections.Generic;
using System.Text;
using System.Threading;
using Cysharp.Threading.Tasks;
using Vareiko.Foundation.Backend;
using Zenject;

namespace Vareiko.Foundation.Save
{
    public sealed class CloudSaveSyncService : ICloudSaveSyncService
    {
        private const string CloudKeyPrefix = "foundation.save.";

        private readonly ISaveService _saveService;
        private readonly ISaveSerializer _serializer;
        private readonly ISaveConflictResolver _conflictResolver;
        private readonly IBackendService _backendService;
        private readonly SignalBus _signalBus;

        [Inject]
        public CloudSaveSyncService(
            ISaveService saveService,
            ISaveSerializer serializer,
            [InjectOptional] ISaveConflictResolver conflictResolver = null,
            [InjectOptional] IBackendService backendService = null,
            [InjectOptional] SignalBus signalBus = null)
        {
            _saveService = saveService;
            _serializer = serializer;
            _conflictResolver = conflictResolver;
            _backendService = backendService;
            _signalBus = signalBus;
        }

        public async UniTask<CloudSaveSyncResult> PushAsync<T>(string slot, string key, T fallback = default, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!ValidateSlotAndKey(slot, key, out CloudSaveSyncResult validationResult))
            {
                _signalBus?.Fire(new SaveCloudSyncFailedSignal(slot, key, validationResult.Error));
                return validationResult;
            }

            CloudSaveSyncResult availability = ValidateBackendAvailability();
            if (!availability.Success)
            {
                _signalBus?.Fire(new SaveCloudSyncFailedSignal(slot, key, availability.Error));
                return availability;
            }

            bool localExists = await _saveService.ExistsAsync(slot, key, cancellationToken);
            if (!localExists)
            {
                CloudSaveSyncResult fail = CloudSaveSyncResult.Fail("Local save does not exist.", BackendErrorCode.ValidationFailed);
                _signalBus?.Fire(new SaveCloudSyncFailedSignal(slot, key, fail.Error));
                return fail;
            }

            T localModel = await _saveService.LoadAsync(slot, key, fallback, cancellationToken);
            string localPayload = _serializer.Serialize(localModel);
            if (!await PushPayloadAsync(slot, key, localPayload, cancellationToken))
            {
                CloudSaveSyncResult fail = CloudSaveSyncResult.Fail("Failed to push save payload to cloud.", BackendErrorCode.ProviderUnavailable);
                _signalBus?.Fire(new SaveCloudSyncFailedSignal(slot, key, fail.Error));
                return fail;
            }

            _signalBus?.Fire(new SaveCloudPushedSignal(slot, key));
            return CloudSaveSyncResult.Succeed(CloudSaveSyncAction.PushedLocalToCloud);
        }

        public async UniTask<CloudSaveSyncResult> PullAsync<T>(string slot, string key, T fallback = default, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!ValidateSlotAndKey(slot, key, out CloudSaveSyncResult validationResult))
            {
                _signalBus?.Fire(new SaveCloudSyncFailedSignal(slot, key, validationResult.Error));
                return validationResult;
            }

            CloudPayloadResult cloudPayloadResult = await TryGetCloudPayloadAsync(slot, key, cancellationToken);
            if (!cloudPayloadResult.Success)
            {
                _signalBus?.Fire(new SaveCloudSyncFailedSignal(slot, key, cloudPayloadResult.Error));
                return CloudSaveSyncResult.Fail(cloudPayloadResult.Error, cloudPayloadResult.BackendErrorCode);
            }

            if (!cloudPayloadResult.HasCloudPayload)
            {
                return CloudSaveSyncResult.Succeed(CloudSaveSyncAction.None);
            }

            T cloudModel;
            if (!_serializer.TryDeserialize(cloudPayloadResult.CloudPayload, out cloudModel))
            {
                CloudSaveSyncResult fail = CloudSaveSyncResult.Fail("Failed to deserialize cloud save payload.", BackendErrorCode.Unknown);
                _signalBus?.Fire(new SaveCloudSyncFailedSignal(slot, key, fail.Error));
                return fail;
            }

            await _saveService.SaveAsync(slot, key, cloudModel, cancellationToken);
            _signalBus?.Fire(new SaveCloudPulledSignal(slot, key));
            return CloudSaveSyncResult.Succeed(CloudSaveSyncAction.PulledCloudToLocal);
        }

        public async UniTask<CloudSaveSyncResult> SyncAsync<T>(string slot, string key, T fallback = default, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!ValidateSlotAndKey(slot, key, out CloudSaveSyncResult validationResult))
            {
                _signalBus?.Fire(new SaveCloudSyncFailedSignal(slot, key, validationResult.Error));
                return validationResult;
            }

            CloudPayloadResult cloudPayloadResult = await TryGetCloudPayloadAsync(slot, key, cancellationToken);
            if (!cloudPayloadResult.Success)
            {
                _signalBus?.Fire(new SaveCloudSyncFailedSignal(slot, key, cloudPayloadResult.Error));
                return CloudSaveSyncResult.Fail(cloudPayloadResult.Error, cloudPayloadResult.BackendErrorCode);
            }

            bool localExists = await _saveService.ExistsAsync(slot, key, cancellationToken);
            string localPayload = string.Empty;
            if (localExists)
            {
                T localModel = await _saveService.LoadAsync(slot, key, fallback, cancellationToken);
                localPayload = _serializer.Serialize(localModel);
            }

            bool cloudExists = cloudPayloadResult.HasCloudPayload;
            if (!localExists && !cloudExists)
            {
                return CloudSaveSyncResult.Succeed(CloudSaveSyncAction.None);
            }

            if (localExists && !cloudExists)
            {
                if (!await PushPayloadAsync(slot, key, localPayload, cancellationToken))
                {
                    CloudSaveSyncResult fail = CloudSaveSyncResult.Fail("Failed to push local save to cloud.", BackendErrorCode.ProviderUnavailable);
                    _signalBus?.Fire(new SaveCloudSyncFailedSignal(slot, key, fail.Error));
                    return fail;
                }

                _signalBus?.Fire(new SaveCloudPushedSignal(slot, key));
                return CloudSaveSyncResult.Succeed(CloudSaveSyncAction.PushedLocalToCloud);
            }

            if (!localExists && cloudExists)
            {
                T cloudOnlyModel;
                if (!_serializer.TryDeserialize(cloudPayloadResult.CloudPayload, out cloudOnlyModel))
                {
                    CloudSaveSyncResult fail = CloudSaveSyncResult.Fail("Failed to deserialize cloud save payload.", BackendErrorCode.Unknown);
                    _signalBus?.Fire(new SaveCloudSyncFailedSignal(slot, key, fail.Error));
                    return fail;
                }

                await _saveService.SaveAsync(slot, key, cloudOnlyModel, cancellationToken);
                _signalBus?.Fire(new SaveCloudPulledSignal(slot, key));
                return CloudSaveSyncResult.Succeed(CloudSaveSyncAction.PulledCloudToLocal);
            }

            string cloudPayload = cloudPayloadResult.CloudPayload;
            if (string.Equals(localPayload, cloudPayload, System.StringComparison.Ordinal))
            {
                return CloudSaveSyncResult.Succeed(CloudSaveSyncAction.None);
            }

            SaveConflictResolution resolution = _conflictResolver != null
                ? _conflictResolver.Resolve(slot, key, localPayload, cloudPayload)
                : SaveConflictResolution.Local(localPayload);

            if (!ApplyConflictChoiceIsValid(resolution))
            {
                CloudSaveSyncResult fail = CloudSaveSyncResult.Fail("Invalid save conflict resolution payload.", BackendErrorCode.ValidationFailed);
                _signalBus?.Fire(new SaveCloudSyncFailedSignal(slot, key, fail.Error));
                return fail;
            }

            switch (resolution.Choice)
            {
                case SaveConflictChoice.KeepLocal:
                {
                    string chosenPayload = string.IsNullOrWhiteSpace(resolution.Payload) ? localPayload : resolution.Payload;
                    if (!await PushPayloadAsync(slot, key, chosenPayload, cancellationToken))
                    {
                        CloudSaveSyncResult fail = CloudSaveSyncResult.Fail("Failed to push local conflict result to cloud.", BackendErrorCode.ProviderUnavailable);
                        _signalBus?.Fire(new SaveCloudSyncFailedSignal(slot, key, fail.Error));
                        return fail;
                    }

                    _signalBus?.Fire(new SaveCloudConflictResolvedSignal(slot, key, SaveConflictChoice.KeepLocal));
                    return CloudSaveSyncResult.Succeed(CloudSaveSyncAction.ResolvedKeepLocal, SaveConflictChoice.KeepLocal);
                }

                case SaveConflictChoice.UseCloud:
                {
                    string chosenPayload = string.IsNullOrWhiteSpace(resolution.Payload) ? cloudPayload : resolution.Payload;
                    T chosenModel;
                    if (!_serializer.TryDeserialize(chosenPayload, out chosenModel))
                    {
                        CloudSaveSyncResult fail = CloudSaveSyncResult.Fail("Failed to deserialize cloud conflict payload.", BackendErrorCode.ValidationFailed);
                        _signalBus?.Fire(new SaveCloudSyncFailedSignal(slot, key, fail.Error));
                        return fail;
                    }

                    await _saveService.SaveAsync(slot, key, chosenModel, cancellationToken);
                    _signalBus?.Fire(new SaveCloudConflictResolvedSignal(slot, key, SaveConflictChoice.UseCloud));
                    _signalBus?.Fire(new SaveCloudPulledSignal(slot, key));
                    return CloudSaveSyncResult.Succeed(CloudSaveSyncAction.ResolvedUseCloud, SaveConflictChoice.UseCloud);
                }

                case SaveConflictChoice.Merge:
                {
                    string chosenPayload = resolution.Payload;
                    T mergedModel;
                    if (!_serializer.TryDeserialize(chosenPayload, out mergedModel))
                    {
                        CloudSaveSyncResult fail = CloudSaveSyncResult.Fail("Failed to deserialize merged conflict payload.", BackendErrorCode.ValidationFailed);
                        _signalBus?.Fire(new SaveCloudSyncFailedSignal(slot, key, fail.Error));
                        return fail;
                    }

                    await _saveService.SaveAsync(slot, key, mergedModel, cancellationToken);
                    if (!await PushPayloadAsync(slot, key, chosenPayload, cancellationToken))
                    {
                        CloudSaveSyncResult fail = CloudSaveSyncResult.Fail("Failed to push merged conflict payload to cloud.", BackendErrorCode.ProviderUnavailable);
                        _signalBus?.Fire(new SaveCloudSyncFailedSignal(slot, key, fail.Error));
                        return fail;
                    }

                    _signalBus?.Fire(new SaveCloudConflictResolvedSignal(slot, key, SaveConflictChoice.Merge));
                    return CloudSaveSyncResult.Succeed(CloudSaveSyncAction.ResolvedMerge, SaveConflictChoice.Merge);
                }
            }

            CloudSaveSyncResult unknown = CloudSaveSyncResult.Fail("Unsupported save conflict choice.", BackendErrorCode.ValidationFailed);
            _signalBus?.Fire(new SaveCloudSyncFailedSignal(slot, key, unknown.Error));
            return unknown;
        }

        private CloudSaveSyncResult ValidateBackendAvailability()
        {
            if (_backendService == null)
            {
                return CloudSaveSyncResult.Fail("Backend service is not available.", BackendErrorCode.ProviderUnavailable);
            }

            if (!_backendService.IsConfigured)
            {
                return CloudSaveSyncResult.Fail("Backend service is not configured.", BackendErrorCode.ConfigurationInvalid);
            }

            if (!_backendService.IsAuthenticated)
            {
                return CloudSaveSyncResult.Fail("Backend service is not authenticated.", BackendErrorCode.AuthenticationRequired);
            }

            return CloudSaveSyncResult.Succeed(CloudSaveSyncAction.None);
        }

        private async UniTask<CloudPayloadResult> TryGetCloudPayloadAsync(string slot, string key, CancellationToken cancellationToken)
        {
            CloudSaveSyncResult availability = ValidateBackendAvailability();
            if (!availability.Success)
            {
                return CloudPayloadResult.Fail(availability.Error, availability.BackendErrorCode);
            }

            BackendPlayerDataResult result = await _backendService.GetPlayerDataAsync(cancellationToken);
            if (!result.Success)
            {
                return CloudPayloadResult.Fail(result.Error, result.ErrorCode);
            }

            IReadOnlyDictionary<string, string> data = result.Data;
            if (data == null || data.Count == 0)
            {
                return CloudPayloadResult.NotFound();
            }

            string payload;
            if (!data.TryGetValue(BuildCloudKey(slot, key), out payload) || string.IsNullOrWhiteSpace(payload))
            {
                return CloudPayloadResult.NotFound();
            }

            return CloudPayloadResult.Found(payload);
        }

        private async UniTask<bool> PushPayloadAsync(string slot, string key, string payload, CancellationToken cancellationToken)
        {
            if (_backendService == null)
            {
                return false;
            }

            Dictionary<string, string> update = new Dictionary<string, string>(1, System.StringComparer.Ordinal)
            {
                { BuildCloudKey(slot, key), payload ?? string.Empty }
            };

            return await _backendService.SetPlayerDataAsync(update, cancellationToken);
        }

        private static bool ApplyConflictChoiceIsValid(SaveConflictResolution resolution)
        {
            if (!string.IsNullOrWhiteSpace(resolution.Error))
            {
                return false;
            }

            if (resolution.Choice == SaveConflictChoice.Merge)
            {
                return !string.IsNullOrWhiteSpace(resolution.Payload);
            }

            return true;
        }

        private static string BuildCloudKey(string slot, string key)
        {
            StringBuilder builder = new StringBuilder(CloudKeyPrefix.Length + slot.Length + key.Length + 2);
            builder.Append(CloudKeyPrefix);
            builder.Append(NormalizePart(slot));
            builder.Append('.');
            builder.Append(NormalizePart(key));
            return builder.ToString();
        }

        private static string NormalizePart(string source)
        {
            if (string.IsNullOrWhiteSpace(source))
            {
                return "empty";
            }

            string value = source.Trim();
            return value.Replace(' ', '_');
        }

        private static bool ValidateSlotAndKey(string slot, string key, out CloudSaveSyncResult result)
        {
            if (string.IsNullOrWhiteSpace(slot))
            {
                result = CloudSaveSyncResult.Fail("Save slot is null or empty.", BackendErrorCode.ValidationFailed);
                return false;
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                result = CloudSaveSyncResult.Fail("Save key is null or empty.", BackendErrorCode.ValidationFailed);
                return false;
            }

            result = CloudSaveSyncResult.Succeed(CloudSaveSyncAction.None);
            return true;
        }

        private readonly struct CloudPayloadResult
        {
            public readonly bool Success;
            public readonly bool HasCloudPayload;
            public readonly string CloudPayload;
            public readonly string Error;
            public readonly BackendErrorCode BackendErrorCode;

            private CloudPayloadResult(bool success, bool hasCloudPayload, string cloudPayload, string error, BackendErrorCode backendErrorCode)
            {
                Success = success;
                HasCloudPayload = hasCloudPayload;
                CloudPayload = cloudPayload ?? string.Empty;
                Error = error ?? string.Empty;
                BackendErrorCode = backendErrorCode;
            }

            public static CloudPayloadResult Found(string cloudPayload)
            {
                return new CloudPayloadResult(true, true, cloudPayload, string.Empty, BackendErrorCode.None);
            }

            public static CloudPayloadResult NotFound()
            {
                return new CloudPayloadResult(true, false, string.Empty, string.Empty, BackendErrorCode.None);
            }

            public static CloudPayloadResult Fail(string error, BackendErrorCode backendErrorCode)
            {
                return new CloudPayloadResult(false, false, string.Empty, error, backendErrorCode);
            }
        }
    }
}
