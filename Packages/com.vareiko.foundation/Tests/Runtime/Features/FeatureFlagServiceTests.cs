using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;
using Vareiko.Foundation.Features;
using Vareiko.Foundation.Tests.TestDoubles;

namespace Vareiko.Foundation.Tests.Features
{
    public sealed class FeatureFlagServiceTests
    {
        [Test]
        public void IsEnabled_UsesPriority_LocalOverrideThenRemoteThenDefault()
        {
            FeatureFlagsConfig config = ScriptableObject.CreateInstance<FeatureFlagsConfig>();
            FeatureFlagsConfig.BoolFeature[] defaults = new FeatureFlagsConfig.BoolFeature[1];
            defaults[0] = new FeatureFlagsConfig.BoolFeature
            {
                Key = "feature.new_ui",
                DefaultValue = false
            };

            ReflectionTestUtil.SetPrivateField(config, "_refreshOnInitialize", false);
            ReflectionTestUtil.SetPrivateField(config, "_boolFeatures", defaults);

            FakeRemoteConfigService remoteConfig = new FakeRemoteConfigService(
                new Dictionary<string, string>(System.StringComparer.Ordinal)
                {
                    { "feature.new_ui", "true" },
                    { "feature.max_count", "7" },
                    { "feature.scale", "0.75" }
                });

            FeatureFlagService service = new FeatureFlagService(remoteConfig, config, null);

            Assert.That(service.IsEnabled("feature.new_ui"), Is.True);
            Assert.That(service.GetInt("feature.max_count"), Is.EqualTo(7));
            Assert.That(service.GetFloat("feature.scale"), Is.EqualTo(0.75f).Within(0.0001f));

            service.SetLocalOverride("feature.new_ui", false);
            Assert.That(service.IsEnabled("feature.new_ui"), Is.False);

            service.ClearLocalOverrides();
            Assert.That(service.IsEnabled("feature.new_ui"), Is.True);
        }
    }
}
