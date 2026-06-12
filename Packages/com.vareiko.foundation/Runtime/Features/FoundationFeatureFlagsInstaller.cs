using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Vareiko.Foundation.Features
{
    public static class FoundationFeatureFlagsInstaller
    {
        public static void Install(IContainerBuilder builder, FeatureFlagsConfig config = null)
        {
            builder.RegisterInstance(config != null ? config : ScriptableObject.CreateInstance<FeatureFlagsConfig>());
            builder.RegisterEntryPoint<FeatureFlagService>(Lifetime.Singleton).As<IFeatureFlagService>().AsSelf();
        }
    }
}
