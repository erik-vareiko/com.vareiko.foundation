using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Vareiko.Foundation.Backend;
using Vareiko.Foundation.Save;
using Vareiko.Foundation.Tests.TestDoubles;

namespace Vareiko.Foundation.Tests.Save
{
    public sealed class CloudSaveSyncServiceTests
    {
        [Test]
        public async Task PushAsync_WhenLocalExists_WritesPayloadToBackend()
        {
            InMemorySaveStorage storage = new InMemorySaveStorage();
            JsonUnitySaveSerializer serializer = new JsonUnitySaveSerializer();
            SaveService saveService = CreateSaveService(storage, serializer);
            FakeBackendService backend = new FakeBackendService(true, true);
            CloudSaveSyncService sync = new CloudSaveSyncService(saveService, serializer, new PreferLocalSaveConflictResolver(), backend, null);

            SaveModel local = new SaveModel { Value = 10 };
            await saveService.SaveAsync("slot", "state", local);

            CloudSaveSyncResult result = await sync.PushAsync("slot", "state", new SaveModel());

            Assert.That(result.Success, Is.True);
            Assert.That(result.Action, Is.EqualTo(CloudSaveSyncAction.PushedLocalToCloud));
            Assert.That(backend.Data.ContainsKey("foundation.save.slot.state"), Is.True);
        }

        [Test]
        public async Task PullAsync_WhenCloudExists_WritesLocalSave()
        {
            InMemorySaveStorage storage = new InMemorySaveStorage();
            JsonUnitySaveSerializer serializer = new JsonUnitySaveSerializer();
            SaveService saveService = CreateSaveService(storage, serializer);
            FakeBackendService backend = new FakeBackendService(true, true);
            CloudSaveSyncService sync = new CloudSaveSyncService(saveService, serializer, new PreferLocalSaveConflictResolver(), backend, null);

            backend.Data["foundation.save.slot.state"] = serializer.Serialize(new SaveModel { Value = 42 });

            CloudSaveSyncResult result = await sync.PullAsync("slot", "state", new SaveModel { Value = -1 });
            SaveModel loaded = await saveService.LoadAsync("slot", "state", new SaveModel { Value = -2 });

            Assert.That(result.Success, Is.True);
            Assert.That(result.Action, Is.EqualTo(CloudSaveSyncAction.PulledCloudToLocal));
            Assert.That(loaded.Value, Is.EqualTo(42));
        }

        [Test]
        public async Task SyncAsync_WithPreferLocal_OverwritesCloud()
        {
            InMemorySaveStorage storage = new InMemorySaveStorage();
            JsonUnitySaveSerializer serializer = new JsonUnitySaveSerializer();
            SaveService saveService = CreateSaveService(storage, serializer);
            FakeBackendService backend = new FakeBackendService(true, true);
            CloudSaveSyncService sync = new CloudSaveSyncService(saveService, serializer, new PreferLocalSaveConflictResolver(), backend, null);

            await saveService.SaveAsync("slot", "state", new SaveModel { Value = 5 });
            backend.Data["foundation.save.slot.state"] = serializer.Serialize(new SaveModel { Value = 99 });

            CloudSaveSyncResult result = await sync.SyncAsync("slot", "state", new SaveModel());

            Assert.That(result.Success, Is.True);
            Assert.That(result.Action, Is.EqualTo(CloudSaveSyncAction.ResolvedKeepLocal));

            SaveModel cloudNow;
            serializer.TryDeserialize(backend.Data["foundation.save.slot.state"], out cloudNow);
            Assert.That(cloudNow.Value, Is.EqualTo(5));
        }

        [Test]
        public async Task SyncAsync_WithUseCloudResolver_WritesCloudToLocal()
        {
            InMemorySaveStorage storage = new InMemorySaveStorage();
            JsonUnitySaveSerializer serializer = new JsonUnitySaveSerializer();
            SaveService saveService = CreateSaveService(storage, serializer);
            FakeBackendService backend = new FakeBackendService(true, true);
            CloudSaveSyncService sync = new CloudSaveSyncService(saveService, serializer, new PreferCloudResolver(), backend, null);

            await saveService.SaveAsync("slot", "state", new SaveModel { Value = 1 });
            backend.Data["foundation.save.slot.state"] = serializer.Serialize(new SaveModel { Value = 7 });

            CloudSaveSyncResult result = await sync.SyncAsync("slot", "state", new SaveModel());
            SaveModel local = await saveService.LoadAsync("slot", "state", new SaveModel { Value = -1 });

            Assert.That(result.Success, Is.True);
            Assert.That(result.Action, Is.EqualTo(CloudSaveSyncAction.ResolvedUseCloud));
            Assert.That(local.Value, Is.EqualTo(7));
        }

        [Test]
        public async Task SyncAsync_WhenBackendUnauthenticated_ReturnsAuthFailure()
        {
            InMemorySaveStorage storage = new InMemorySaveStorage();
            JsonUnitySaveSerializer serializer = new JsonUnitySaveSerializer();
            SaveService saveService = CreateSaveService(storage, serializer);
            FakeBackendService backend = new FakeBackendService(true, false);
            CloudSaveSyncService sync = new CloudSaveSyncService(saveService, serializer, new PreferLocalSaveConflictResolver(), backend, null);

            CloudSaveSyncResult result = await sync.SyncAsync("slot", "state", new SaveModel());

            Assert.That(result.Success, Is.False);
            Assert.That(result.BackendErrorCode, Is.EqualTo(BackendErrorCode.AuthenticationRequired));
        }

        private static SaveService CreateSaveService(InMemorySaveStorage storage, JsonUnitySaveSerializer serializer)
        {
            SaveSecurityConfig security = ScriptableObject.CreateInstance<SaveSecurityConfig>();
            ReflectionTestUtil.SetPrivateField(security, "_enableRollingBackups", false);
            return new SaveService(storage, serializer, null, null, security, null, "root");
        }

        [System.Serializable]
        private sealed class SaveModel
        {
            public int Value;
        }

        private sealed class PreferCloudResolver : ISaveConflictResolver
        {
            public SaveConflictResolution Resolve(string slot, string key, string localPayload, string cloudPayload)
            {
                return SaveConflictResolution.Cloud(cloudPayload);
            }
        }

        private sealed class FakeBackendService : IBackendService
        {
            public readonly Dictionary<string, string> Data = new Dictionary<string, string>(System.StringComparer.Ordinal);

            public FakeBackendService(bool isConfigured, bool isAuthenticated)
            {
                IsConfigured = isConfigured;
                IsAuthenticated = isAuthenticated;
            }

            public BackendProviderType Provider => BackendProviderType.PlayFab;
            public bool IsConfigured { get; }
            public bool IsAuthenticated { get; }

            public UniTask<BackendAuthResult> LoginAnonymousAsync(string customId, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                return UniTask.FromResult(BackendAuthResult.Succeed(customId));
            }

            public UniTask<BackendPlayerDataResult> GetPlayerDataAsync(CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!IsConfigured)
                {
                    return UniTask.FromResult(BackendPlayerDataResult.Fail("Not configured", BackendErrorCode.ConfigurationInvalid));
                }

                if (!IsAuthenticated)
                {
                    return UniTask.FromResult(BackendPlayerDataResult.Fail("Not authenticated", BackendErrorCode.AuthenticationRequired));
                }

                return UniTask.FromResult(BackendPlayerDataResult.Succeed(new Dictionary<string, string>(Data)));
            }

            public UniTask<bool> SetPlayerDataAsync(IReadOnlyDictionary<string, string> data, CancellationToken cancellationToken = default)
            {
                cancellationToken.ThrowIfCancellationRequested();
                if (!IsConfigured || !IsAuthenticated || data == null)
                {
                    return UniTask.FromResult(false);
                }

                foreach (KeyValuePair<string, string> pair in data)
                {
                    Data[pair.Key] = pair.Value ?? string.Empty;
                }

                return UniTask.FromResult(true);
            }
        }
    }
}
