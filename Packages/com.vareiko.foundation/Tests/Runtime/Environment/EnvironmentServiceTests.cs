using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Vareiko.Foundation.Environment;
using Vareiko.Foundation.Tests.TestDoubles;

namespace Vareiko.Foundation.Tests.EnvironmentModule
{
    public sealed class EnvironmentServiceTests
    {
        [Test]
        public void Initialize_UsesDefaultProfile_AndParsesTypedValues()
        {
            EnvironmentConfig config = CreateConfig();
            try
            {
                EnvironmentService service = new EnvironmentService(config, null);
                service.Initialize();

                Assert.That(service.ActiveProfileId, Is.EqualTo("prod"));

                Assert.That(service.TryGetString("api.url", out string url), Is.True);
                Assert.That(url, Is.EqualTo("https://api.example.com"));

                Assert.That(service.TryGetInt("max_players", out int maxPlayers), Is.True);
                Assert.That(maxPlayers, Is.EqualTo(16));

                Assert.That(service.TryGetFloat("spawn_rate", out float spawnRate), Is.True);
                Assert.That(spawnRate, Is.EqualTo(1.5f).Within(0.0001f));

                Assert.That(service.TryGetBool("feature.enabled", out bool enabled), Is.True);
                Assert.That(enabled, Is.True);
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void TrySetActiveProfile_WhenProfileMissing_ReturnsFalse()
        {
            EnvironmentConfig config = CreateConfig();
            try
            {
                EnvironmentService service = new EnvironmentService(config, null);
                service.Initialize();

                Assert.That(service.TrySetActiveProfile("unknown"), Is.False);
                Assert.That(service.ActiveProfileId, Is.EqualTo("prod"));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void Snapshot_ReturnsCurrentProfileValues()
        {
            EnvironmentConfig config = CreateConfig();
            try
            {
                EnvironmentService service = new EnvironmentService(config, null);
                service.Initialize();

                IReadOnlyDictionary<string, string> snapshot = service.Snapshot();
                Assert.That(snapshot.Count, Is.EqualTo(4));
                Assert.That(snapshot["api.url"], Is.EqualTo("https://api.example.com"));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        private static EnvironmentConfig CreateConfig()
        {
            EnvironmentConfig config = ScriptableObject.CreateInstance<EnvironmentConfig>();
            ReflectionTestUtil.SetPrivateField(config, "_defaultProfileId", "prod");
            ReflectionTestUtil.SetPrivateField(config, "_allowCommandLineOverride", false);

            EnvironmentConfig.Profile profile = new EnvironmentConfig.Profile();
            ReflectionTestUtil.SetPrivateField(profile, "_id", "prod");
            ReflectionTestUtil.SetPrivateField(profile, "_values", new List<EnvironmentConfig.EnvironmentValue>
            {
                new EnvironmentConfig.EnvironmentValue { Key = "api.url", Value = "https://api.example.com" },
                new EnvironmentConfig.EnvironmentValue { Key = "max_players", Value = "16" },
                new EnvironmentConfig.EnvironmentValue { Key = "spawn_rate", Value = "1.5" },
                new EnvironmentConfig.EnvironmentValue { Key = "feature.enabled", Value = "on" }
            });

            ReflectionTestUtil.SetPrivateField(config, "_profiles", new List<EnvironmentConfig.Profile> { profile });
            return config;
        }
    }
}
