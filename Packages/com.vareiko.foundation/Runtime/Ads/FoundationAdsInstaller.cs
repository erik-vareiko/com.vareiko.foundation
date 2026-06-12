using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Vareiko.Foundation.Ads
{
    public static class FoundationAdsInstaller
    {
        public static void Install(IContainerBuilder builder, AdsConfig config = null)
        {
            builder.RegisterInstance(config != null ? config : ScriptableObject.CreateInstance<AdsConfig>());

            if (config != null && config.Provider == AdsProviderType.Simulated)
            {
                builder.RegisterEntryPoint<SimulatedAdsService>(Lifetime.Singleton).As<IAdsService>().AsSelf();
            }
            else if (config != null && config.Provider == AdsProviderType.ExternalBridge)
            {
                builder.RegisterEntryPoint<ExternalAdsBridgeService>(Lifetime.Singleton).As<IAdsService>().AsSelf();
            }
            else
            {
                builder.Register<NullAdsService>(Lifetime.Singleton).As<IAdsService>();
            }
        }
    }
}
