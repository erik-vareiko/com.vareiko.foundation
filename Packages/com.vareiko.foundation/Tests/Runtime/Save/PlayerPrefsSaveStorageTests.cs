using System;
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Vareiko.Foundation.Save;
using Vareiko.Foundation.Tests.TestDoubles;

namespace Vareiko.Foundation.Tests.Save
{
    public sealed class PlayerPrefsSaveStorageTests
    {
        private const string Prefix = "Vareiko.Foundation.Tests.Save.";

        private readonly List<string> _keysToDelete = new List<string>();

        [TearDown]
        public void TearDown()
        {
            for (int i = 0; i < _keysToDelete.Count; i++)
            {
                PlayerPrefs.DeleteKey(_keysToDelete[i]);
            }

            PlayerPrefs.Save();
            _keysToDelete.Clear();
        }

        [Test]
        public async System.Threading.Tasks.Task SaveReadExistsDelete_Works()
        {
            const string key = Prefix + "main/player_profile.json";
            Track(key);

            PlayerPrefsSaveStorage storage = new PlayerPrefsSaveStorage("root/saves", Prefix);
            string path = "root/saves/main/player_profile.json";

            Assert.That(await storage.ExistsAsync(path), Is.False);

            await storage.WriteTextAsync(path, "{\"value\":1}");

            Assert.That(await storage.ExistsAsync(path), Is.True);
            Assert.That(await storage.ReadTextAsync(path), Is.EqualTo("{\"value\":1}"));

            await storage.DeleteAsync(path);

            Assert.That(await storage.ExistsAsync(path), Is.False);
            Assert.That(await storage.ReadTextAsync(path), Is.Null);
        }

        [Test]
        public async System.Threading.Tasks.Task SaveRead_NormalizesRootPath_WithoutAbsolutePersistentDataPath()
        {
            const string key = Prefix + "main/player_profile.json";
            Track(key);

            PlayerPrefsSaveStorage storage = new PlayerPrefsSaveStorage("/tmp/chibi/saves", Prefix);
            await storage.WriteTextAsync("/tmp/chibi/saves/main/player_profile.json", "payload");

            Assert.That(PlayerPrefs.HasKey(key), Is.True);
            Assert.That(PlayerPrefs.GetString(key), Is.EqualTo("payload"));
        }

        [Test]
        public async System.Threading.Tasks.Task SaveService_WithPlayerPrefsStorage_RoundTrip_ReturnsStoredModel()
        {
            Track(Prefix + "global/player.json");

            PlayerPrefsSaveStorage storage = new PlayerPrefsSaveStorage("root", Prefix);
            JsonUnitySaveSerializer serializer = new JsonUnitySaveSerializer();
            SaveService service = new SaveService(storage, serializer, null, null, ScriptableObject.CreateInstance<SaveSecurityConfig>(), null, "root");

            SaveModel expected = new SaveModel { Value = 42 };
            await service.SaveAsync("global", "player", expected);
            SaveModel loaded = await service.LoadAsync("global", "player", new SaveModel { Value = -1 });

            Assert.That(loaded, Is.Not.Null);
            Assert.That(loaded.Value, Is.EqualTo(42));
        }

        [Test]
        public async System.Threading.Tasks.Task SaveService_WithPlayerPrefsStorage_RestoresBackup_WhenPrimaryCorrupted()
        {
            const string primaryKey = Prefix + "global/state.json";
            const string backupKey = Prefix + "global/state.json.bak1";
            Track(primaryKey);
            Track(backupKey);

            PlayerPrefsSaveStorage storage = new PlayerPrefsSaveStorage("root", Prefix);
            JsonUnitySaveSerializer serializer = new JsonUnitySaveSerializer();
            SaveSecurityConfig securityConfig = ScriptableObject.CreateInstance<SaveSecurityConfig>();
            ReflectionTestUtil.SetPrivateField(securityConfig, "_enableRollingBackups", true);
            ReflectionTestUtil.SetPrivateField(securityConfig, "_restoreFromBackupOnLoadFailure", true);
            ReflectionTestUtil.SetPrivateField(securityConfig, "_maxBackupFiles", 2);

            SaveService service = new SaveService(storage, serializer, null, null, securityConfig, null, "root");
            await service.SaveAsync("global", "state", new SaveModel { Value = 10 });
            await service.SaveAsync("global", "state", new SaveModel { Value = 20 });

            PlayerPrefs.SetString(primaryKey, "{broken");
            PlayerPrefs.Save();

            SaveModel restored = await service.LoadAsync("global", "state", new SaveModel { Value = -1 });
            SaveModel loadedAgain = await service.LoadAsync("global", "state", new SaveModel { Value = -2 });

            Assert.That(restored.Value, Is.EqualTo(10));
            Assert.That(loadedAgain.Value, Is.EqualTo(10));
        }

        private void Track(string key)
        {
            if (string.IsNullOrWhiteSpace(key) || _keysToDelete.Contains(key))
            {
                return;
            }

            _keysToDelete.Add(key);
            PlayerPrefs.DeleteKey(key);
            PlayerPrefs.Save();
        }

        [Serializable]
        private sealed class SaveModel
        {
            public int Value;
        }
    }
}
