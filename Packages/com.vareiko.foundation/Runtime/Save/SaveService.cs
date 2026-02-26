using System;
using System.Globalization;
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
        private readonly SaveSecurityConfig _securityConfig;
        private readonly SignalBus _signalBus;
        private readonly string _rootPath;
        private readonly int _schemaVersion;

        [Inject]
        public SaveService(
            ISaveStorage storage,
            ISaveSerializer serializer,
            [InjectOptional] ISaveMigrationService migrationService = null,
            [InjectOptional] SaveSchemaConfig schemaConfig = null,
            [InjectOptional] SaveSecurityConfig securityConfig = null,
            [InjectOptional] SignalBus signalBus = null,
            [InjectOptional(Id = "SaveRootPath")] string rootPath = null)
        {
            _storage = storage;
            _serializer = serializer;
            _migrationService = migrationService;
            _securityConfig = securityConfig;
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
            await CreateBackupIfExistsAsync(slot, key, path, cancellationToken);
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
            LoadAttempt<T> attempt = await TryLoadModelAsync<T>(slot, key, path, raw, cancellationToken);
            if (attempt.Success)
            {
                return attempt.Model;
            }

            (bool Restored, int BackupIndex, string Raw) restored = IsBackupRestoreEnabled()
                ? await TryRestoreFromBackupAsync(path, cancellationToken)
                : (false, 0, string.Empty);
            if (restored.Restored)
            {
                await _storage.WriteTextAsync(path, restored.Raw, cancellationToken);
                _signalBus?.Fire(new SaveRestoredFromBackupSignal(slot, key, restored.BackupIndex));
                LoadAttempt<T> restoredAttempt = await TryLoadModelAsync<T>(slot, key, path, restored.Raw, cancellationToken);
                if (restoredAttempt.Success)
                {
                    return restoredAttempt.Model;
                }

                attempt = restoredAttempt;
            }

            _signalBus?.Fire(new SaveLoadFailedSignal(slot, key, attempt.Error));
            return fallback;
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
            int backupCount = GetMaxBackupFiles();
            for (int i = 1; i <= backupCount; i++)
            {
                await _storage.DeleteAsync(BuildBackupPath(path, i), cancellationToken);
            }

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

        private async UniTask CreateBackupIfExistsAsync(string slot, string key, string primaryPath, CancellationToken cancellationToken)
        {
            if (!IsBackupsEnabled())
            {
                return;
            }

            if (!await _storage.ExistsAsync(primaryPath, cancellationToken))
            {
                return;
            }

            int maxBackups = GetMaxBackupFiles();
            for (int i = maxBackups; i >= 2; i--)
            {
                string previousPath = BuildBackupPath(primaryPath, i - 1);
                string nextPath = BuildBackupPath(primaryPath, i);

                if (await _storage.ExistsAsync(previousPath, cancellationToken))
                {
                    string previousPayload = await _storage.ReadTextAsync(previousPath, cancellationToken);
                    await _storage.WriteTextAsync(nextPath, previousPayload ?? string.Empty, cancellationToken);
                }
                else
                {
                    await _storage.DeleteAsync(nextPath, cancellationToken);
                }
            }

            string currentPayload = await _storage.ReadTextAsync(primaryPath, cancellationToken);
            if (!string.IsNullOrEmpty(currentPayload))
            {
                await _storage.WriteTextAsync(BuildBackupPath(primaryPath, 1), currentPayload, cancellationToken);
                _signalBus?.Fire(new SaveBackupWrittenSignal(slot, key, 1));
            }
        }

        private async UniTask<(bool Restored, int BackupIndex, string Raw)> TryRestoreFromBackupAsync(string primaryPath, CancellationToken cancellationToken)
        {
            int maxBackups = GetMaxBackupFiles();
            for (int i = 1; i <= maxBackups; i++)
            {
                string backupPath = BuildBackupPath(primaryPath, i);
                if (!await _storage.ExistsAsync(backupPath, cancellationToken))
                {
                    continue;
                }

                string raw = await _storage.ReadTextAsync(backupPath, cancellationToken);
                if (string.IsNullOrWhiteSpace(raw))
                {
                    continue;
                }

                return (true, i, raw);
            }

            return (false, 0, string.Empty);
        }

        private async UniTask<LoadAttempt<T>> TryLoadModelAsync<T>(
            string slot,
            string key,
            string primaryPath,
            string raw,
            CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return LoadAttempt<T>.Fail("Save payload is empty.");
            }

            string payload;
            int payloadVersion;
            TryExtractPayload(raw, out payload, out payloadVersion);
            if (payloadVersion > _schemaVersion)
            {
                return LoadAttempt<T>.Fail($"Unsupported save version: {payloadVersion} > {_schemaVersion}");
            }

            if (payloadVersion < _schemaVersion)
            {
                if (_migrationService == null)
                {
                    return LoadAttempt<T>.Fail("Save migration service is not configured.");
                }

                SaveMigrationResult migrated = _migrationService.Migrate(slot, key, payloadVersion, _schemaVersion, payload);
                if (!migrated.Success)
                {
                    return LoadAttempt<T>.Fail(migrated.Error);
                }

                payload = migrated.Payload;
                int fromVersion = payloadVersion;
                payloadVersion = migrated.Version;
                await _storage.WriteTextAsync(primaryPath, WrapPayload(payload, payloadVersion), cancellationToken);
                _signalBus?.Fire(new SaveMigratedSignal(slot, key, fromVersion, payloadVersion));
            }

            T model;
            if (!_serializer.TryDeserialize(payload, out model))
            {
                return LoadAttempt<T>.Fail("Failed to deserialize save payload.");
            }

            return LoadAttempt<T>.Succeed(model);
        }

        private bool IsBackupsEnabled()
        {
            return _securityConfig != null && _securityConfig.EnableRollingBackups;
        }

        private bool IsBackupRestoreEnabled()
        {
            return IsBackupsEnabled() && _securityConfig.RestoreFromBackupOnLoadFailure;
        }

        private int GetMaxBackupFiles()
        {
            return _securityConfig != null ? _securityConfig.MaxBackupFiles : 1;
        }

        private static string BuildBackupPath(string primaryPath, int index)
        {
            return primaryPath + ".bak" + index.ToString(CultureInfo.InvariantCulture);
        }

        private readonly struct LoadAttempt<T>
        {
            public readonly bool Success;
            public readonly T Model;
            public readonly string Error;

            private LoadAttempt(bool success, T model, string error)
            {
                Success = success;
                Model = model;
                Error = error ?? string.Empty;
            }

            public static LoadAttempt<T> Succeed(T model)
            {
                return new LoadAttempt<T>(true, model, string.Empty);
            }

            public static LoadAttempt<T> Fail(string error)
            {
                return new LoadAttempt<T>(false, default, error);
            }
        }
    }
}
