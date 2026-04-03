using NUnit.Framework;
using UnityEngine;
using Vareiko.Foundation.Backend;
using Vareiko.Foundation.Tests.TestDoubles;

namespace Vareiko.Foundation.Tests.Backend
{
    public sealed class PlayerPrefsCloudCommandQueueStoreTests
    {
        private const string NewKey = "vareiko.foundation.backend.cloud_command_queue";
        private const string LegacyKey = "test.foundation.backend.legacy_queue";

        [SetUp]
        public void SetUp()
        {
            PlayerPrefs.DeleteKey(NewKey);
            PlayerPrefs.DeleteKey(LegacyKey);
            PlayerPrefs.Save();
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteKey(NewKey);
            PlayerPrefs.DeleteKey(LegacyKey);
            PlayerPrefs.Save();
        }

        [Test]
        public void Load_WhenLegacyExists_MigratesAndReturnsQueue()
        {
            PlayerPrefs.SetString(LegacyKey, "{\"Items\":[{\"FunctionName\":\"fn.restore\",\"PayloadJson\":\"{}\"},{\"FunctionName\":\"\",\"PayloadJson\":\"{}\"}]}");
            PlayerPrefs.Save();

            PlayerPrefsCloudCommandQueueStore store = new PlayerPrefsCloudCommandQueueStore(CreateReliabilityConfigWithLegacyKey());

            var loaded = store.Load();

            Assert.That(loaded.Count, Is.EqualTo(1));
            Assert.That(loaded[0].FunctionName, Is.EqualTo("fn.restore"));
            Assert.That(PlayerPrefs.HasKey(LegacyKey), Is.False);
            Assert.That(PlayerPrefs.HasKey(NewKey), Is.True);
        }

        [Test]
        public void Load_WhenLegacyInvalid_ReturnsEmptyWithoutThrow()
        {
            PlayerPrefs.SetString(LegacyKey, "{\"Items\":[{\"FunctionName\":");
            PlayerPrefs.Save();

            PlayerPrefsCloudCommandQueueStore store = new PlayerPrefsCloudCommandQueueStore(CreateReliabilityConfigWithLegacyKey());

            var loaded = store.Load();

            Assert.That(loaded.Count, Is.EqualTo(0));
        }

        private static BackendReliabilityConfig CreateReliabilityConfigWithLegacyKey()
        {
            BackendReliabilityConfig config = ScriptableObject.CreateInstance<BackendReliabilityConfig>();
            ReflectionTestUtil.SetPrivateField(config, "_cloudFunctionQueueStorageKey", LegacyKey);
            return config;
        }
    }
}
