using System;
using System.IO;
using System.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;
using Vareiko.Foundation.Save;
using Vareiko.Foundation.Tests.TestDoubles;

namespace Vareiko.Foundation.Tests.Save
{
    public sealed class SaveServiceTests
    {
        [Test]
        public async Task SaveAndLoad_RoundTrip_ReturnsStoredModel()
        {
            InMemorySaveStorage storage = new InMemorySaveStorage();
            JsonUnitySaveSerializer serializer = new JsonUnitySaveSerializer();
            SaveService service = new SaveService(storage, serializer, null, null, ScriptableObject.CreateInstance<SaveSecurityConfig>(), null, "root");

            SaveModel expected = new SaveModel { Value = 42 };
            await service.SaveAsync("global", "player", expected);
            SaveModel loaded = await service.LoadAsync("global", "player", new SaveModel { Value = -1 });

            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.Value, Is.EqualTo(42));
        }

        [Test]
        public async Task Load_WhenPrimaryCorrupted_RestoresFromLatestBackup()
        {
            InMemorySaveStorage storage = new InMemorySaveStorage();
            JsonUnitySaveSerializer serializer = new JsonUnitySaveSerializer();
            SaveSecurityConfig securityConfig = ScriptableObject.CreateInstance<SaveSecurityConfig>();
            ReflectionTestUtil.SetPrivateField(securityConfig, "_enableRollingBackups", true);
            ReflectionTestUtil.SetPrivateField(securityConfig, "_restoreFromBackupOnLoadFailure", true);
            ReflectionTestUtil.SetPrivateField(securityConfig, "_maxBackupFiles", 2);

            SaveService service = new SaveService(storage, serializer, null, null, securityConfig, null, "root");
            await service.SaveAsync("global", "state", new SaveModel { Value = 10 });
            await service.SaveAsync("global", "state", new SaveModel { Value = 20 });

            string path = Path.Combine("root", "global", "state.json");
            await storage.WriteTextAsync(path, "{broken");

            SaveModel restored = await service.LoadAsync("global", "state", new SaveModel { Value = -1 });
            SaveModel loadedAgain = await service.LoadAsync("global", "state", new SaveModel { Value = -2 });

            Assert.That(restored.Value, Is.EqualTo(10));
            Assert.That(loadedAgain.Value, Is.EqualTo(10));
        }

        [Test]
        public async Task Load_WhenBackupRestoreDisabled_ReturnsFallbackOnCorruption()
        {
            InMemorySaveStorage storage = new InMemorySaveStorage();
            JsonUnitySaveSerializer serializer = new JsonUnitySaveSerializer();
            SaveSecurityConfig securityConfig = ScriptableObject.CreateInstance<SaveSecurityConfig>();
            ReflectionTestUtil.SetPrivateField(securityConfig, "_enableRollingBackups", true);
            ReflectionTestUtil.SetPrivateField(securityConfig, "_restoreFromBackupOnLoadFailure", false);

            SaveService service = new SaveService(storage, serializer, null, null, securityConfig, null, "root");
            await service.SaveAsync("global", "state", new SaveModel { Value = 1 });

            string path = Path.Combine("root", "global", "state.json");
            await storage.WriteTextAsync(path, "{broken");

            SaveModel loaded = await service.LoadAsync("global", "state", new SaveModel { Value = -100 });
            Assert.That(loaded.Value, Is.EqualTo(-100));
        }

        [Serializable]
        private sealed class SaveModel
        {
            public int Value;
        }
    }
}
