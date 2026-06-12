using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Vareiko.Foundation.Monetization
{
    public static class FoundationMonetizationInstaller
    {
        public static void Install(IContainerBuilder builder, MonetizationPolicyConfig config = null)
        {
            builder.RegisterInstance(config != null ? config : ScriptableObject.CreateInstance<MonetizationPolicyConfig>());
            builder.RegisterEntryPoint<MonetizationPolicyService>(Lifetime.Singleton).As<IMonetizationPolicyService>().AsSelf();
        }
    }
}
