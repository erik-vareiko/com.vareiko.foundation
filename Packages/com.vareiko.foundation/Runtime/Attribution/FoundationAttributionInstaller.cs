using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Vareiko.Foundation.Attribution
{
    public static class FoundationAttributionInstaller
    {
        public static void Install(IContainerBuilder builder, AttributionConfig config = null)
        {
            builder.RegisterInstance(config != null ? config : ScriptableObject.CreateInstance<AttributionConfig>());

            if (config != null && config.Provider == AttributionProviderType.ExternalBridge)
            {
                builder.RegisterEntryPoint<ExternalAttributionBridgeService>(Lifetime.Singleton).As<IAttributionService>().AsSelf();
            }
            else
            {
                builder.Register<NullAttributionService>(Lifetime.Singleton).As<IAttributionService>();
            }
        }
    }
}
