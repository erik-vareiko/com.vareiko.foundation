using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Vareiko.Foundation.Iap
{
    public static class FoundationIapInstaller
    {
        public static void Install(IContainerBuilder builder, IapConfig config = null)
        {
            builder.RegisterInstance(config != null ? config : ScriptableObject.CreateInstance<IapConfig>());

            if (config != null && config.Provider == InAppPurchaseProviderType.Simulated)
            {
                builder.RegisterEntryPoint<SimulatedInAppPurchaseService>(Lifetime.Singleton).As<IInAppPurchaseService>().AsSelf();
            }
            else if (config != null && config.Provider == InAppPurchaseProviderType.UnityIap)
            {
                builder.RegisterEntryPoint<UnityInAppPurchaseService>(Lifetime.Singleton).As<IInAppPurchaseService>().AsSelf();
            }
            else
            {
                builder.Register<NullInAppPurchaseService>(Lifetime.Singleton).As<IInAppPurchaseService>();
            }
        }
    }
}
