using System;
using System.IO;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Zenject;

namespace Vareiko.Foundation.Save
{
    public sealed class SaveService : ISaveService
    {
        private readonly ISaveStorage _storage;
        private readonly ISaveSerializer _serializer;
        private readonly ISaveMigrationService _migrationService;
        private readonly SignalBus _signalBus;
        private readonly string _rootPath;
        private readonly int _schemaVersion;

        [Inject]
        public SaveService(
            ISaveStorage storage,
            ISaveSerializer serializer,
            [InjectOptional] ISaveMigrationService migrationService = null,
            [InjectOptional] SaveSchemaConfig schemaConfig = null,
            [InjectOptional] SignalBus signalBus = null,
            [InjectOptional(Id = "SaveRootPath")] string rootPath = null)
        {
            _storage = storage;
            _serializer = serializer;
            _migrationService = migrationService;
            _signalBus = signalBus;
            _rootPath = string.IsNullOrWhiteSpace(rootPath)
                ? Path.Combine(Application.persistentDataPath, "saves")
                : rootPath;
            _schemaVersion = schemaConfig != null ? schemaConfig.CurrentVersion : 1;
        }

        public async UniTask SaveAsync<T>(string slot, string key, T model, CancellationToken cancellationToken = default)
        {
            ValidateSlotAndKey(slot, key);
            string path = BuildPath(slot, key);
            string payload = _serializer.Serialize(model);
            string wrapped = WrapPayload(payload, _schemaVersion);
            await _storage.WriteTextAsync(path, wrapped, cancellationToken);
            _signalBus?.Fire(new SaveWrittenSignal(slot, key));
        }

        public async UniTask<T> LoadAsync<T>(string slot, string key, T fallback = default, CancellationToken cancellationToken = default)
        {
            ValidateSlotAndKey(slot, key);
            string path = BuildPath(slot, key);
            if (!await _storage.ExistsAsync(path, cancellationToken))
            {
                return fallback;
            }

            string raw = await _storage.ReadTextAsync(path, cancellationToken);
            if (string.IsNullOrWhiteSpace(raw))
            {
                return fallback;
            }

            string payload;
            int payloadVersion;
            TryExtractPayload(raw, out payload, out payloadVersion);
            if (payloadVersion > _schemaVersion)
            {
                _signalBus?.Fire(new SaveLoadFailedSignal(slot, key, $"Unsupported save version: {payloadVersion} > {_schemaVersion}"));
                return fallback;
            }

            if (payloadVersion < _schemaVersion)
            {
                if (_migrationService == null)
                {
                    _signalBus?.Fire(new SaveLoadFailedSignal(slot, key, "Save migration service is not configured."));
                    return fallback;
                }

                SaveMigrationResult migrated = _migrationService.Migrate(slot, key, payloadVersion, _schemaVersion, payload);
                if (!migrated.Success)
                {
                    _signalBus?.Fire(new SaveLoadFailedSignal(slot, key, migrated.Error));
                    return fallback;
                }

                payload = migrated.Payload;
                int fromVersion = payloadVersion;
                payloadVersion = migrated.Version;
                await _storage.WriteTextAsync(path, WrapPayload(payload, payloadVersion), cancellationToken);
                _signalBus?.Fire(new SaveMigratedSignal(slot, key, fromVersion, payloadVersion));
            }

            T model;
            if (!_serializer.TryDeserialize(payload, out model))
            {
                _signalBus?.Fire(new SaveLoadFailedSignal(slot, key, "Failed to deserialize save payload."));
                return fallback;
            }

            return model;
        }

        public UniTask<bool> ExistsAsync(string slot, string key, CancellationToken cancellationToken = default)
        {
            ValidateSlotAndKey(slot, key);
            string path = BuildPath(slot, key);
            return _storage.ExistsAsync(path, cancellationToken);
        }

        public async UniTask DeleteAsync(string slot, string key, CancellationToken cancellationToken = default)
        {
            ValidateSlotAndKey(slot, key);
            string path = BuildPath(slot, key);
            await _storage.DeleteAsync(path, cancellationToken);
            _signalBus?.Fire(new SaveDeletedSignal(slot, key));
        }

        private string BuildPath(string slot, string key)
        {
            string safeSlot = MakeSafeName(slot);
            string safeKey = MakeSafeName(key);
            return Path.Combine(_rootPath, safeSlot, safeKey + ".json");
        }

        private static string MakeSafeName(string source)
        {
            char[] invalid = Path.GetInvalidFileNameChars();
            string value = source;
            for (int i = 0; i < invalid.Length; i++)
            {
                value = value.Replace(invalid[i], '_');
            }

            return value.Trim();
        }

        private static void ValidateSlotAndKey(string slot, string key)
        {
            if (string.IsNullOrWhiteSpace(slot))
            {
                throw new ArgumentException("Slot is null or empty.", nameof(slot));
            }

            if (string.IsNullOrWhiteSpace(key))
            {
                throw new ArgumentException("Key is null or empty.", nameof(key));
            }
        }

        private static void TryExtractPayload(string raw, out string payload, out int version)
        {
            payload = raw;
            version = 1;

            try
            {
                VersionedSaveEnvelope envelope = JsonUtility.FromJson<VersionedSaveEnvelope>(raw);
                if (envelope == null || envelope.Version < 1 || string.IsNullOrEmpty(envelope.Payload))
                {
                    return;
                }

                payload = envelope.Payload;
                version = envelope.Version;
            }
            catch (Exception)
            {
                payload = raw;
                version = 1;
            }
        }

        private static string WrapPayload(string payload, int version)
        {
            VersionedSaveEnvelope envelope = new VersionedSaveEnvelope
            {
                Version = version < 1 ? 1 : version,
                Payload = payload ?? string.Empty
            };

            return JsonUtility.ToJson(envelope);
        }

        [Serializable]
        private sealed class VersionedSaveEnvelope
        {
            public int Version;
            public string Payload;
        }
    }
}
