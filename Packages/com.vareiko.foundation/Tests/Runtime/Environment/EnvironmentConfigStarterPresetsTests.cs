using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Vareiko.Foundation.Environment;
using Vareiko.Foundation.Tests.TestDoubles;

namespace Vareiko.Foundation.Tests.EnvironmentModule
{
    public sealed class EnvironmentConfigStarterPresetsTests
    {
        [Test]
        public void ApplyStarterPresets_CreatesDevStageProdProfiles()
        {
            EnvironmentConfig config = ScriptableObject.CreateInstance<EnvironmentConfig>();
            try
            {
                config.ApplyStarterPresets();

                Assert.That(config.DefaultProfileId, Is.EqualTo(EnvironmentConfig.StarterProfileDev));
                Assert.That(config.Profiles.Count, Is.EqualTo(3));
                Assert.That(config.Profiles[0].Id, Is.EqualTo(EnvironmentConfig.StarterProfileDev));
                Assert.That(config.Profiles[1].Id, Is.EqualTo(EnvironmentConfig.StarterProfileStage));
                Assert.That(config.Profiles[2].Id, Is.EqualTo(EnvironmentConfig.StarterProfileProd));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void EnvironmentService_WithStarterPresets_SwitchesProfiles()
        {
            EnvironmentConfig config = ScriptableObject.CreateInstance<EnvironmentConfig>();
            try
            {
                config.ApplyStarterPresets();
                ReflectionTestUtil.SetPrivateField(config, "_allowCommandLineOverride", false);

                EnvironmentService service = new EnvironmentService(config, null);
                service.Initialize();

                Assert.That(service.ActiveProfileId, Is.EqualTo(EnvironmentConfig.StarterProfileDev));
                Assert.That(service.TryGetString("environment.name", out string initialName), Is.True);
                Assert.That(initialName, Is.EqualTo("dev"));
                Assert.That(service.TryGetBool("feature.experimental", out bool devFeature), Is.True);
                Assert.That(devFeature, Is.True);

                Assert.That(service.TrySetActiveProfile(EnvironmentConfig.StarterProfileProd), Is.True);
                Assert.That(service.ActiveProfileId, Is.EqualTo(EnvironmentConfig.StarterProfileProd));
                Assert.That(service.TryGetString("environment.name", out string prodName), Is.True);
                Assert.That(prodName, Is.EqualTo("prod"));
                Assert.That(service.TryGetBool("feature.experimental", out bool prodFeature), Is.True);
                Assert.That(prodFeature, Is.False);

                IReadOnlyDictionary<string, string> snapshot = service.Snapshot();
                Assert.That(snapshot.ContainsKey("api.base_url"), Is.True);
                Assert.That(snapshot["logging.level"], Is.EqualTo("warning"));
            }
            finally
            {
                Object.DestroyImmediate(config);
            }
        }
    }
}
