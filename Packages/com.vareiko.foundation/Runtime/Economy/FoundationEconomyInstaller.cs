using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Vareiko.Foundation.Economy
{
    public static class FoundationEconomyInstaller
    {
        public static void Install(IContainerBuilder builder, EconomyConfig config = null)
        {
            builder.RegisterInstance(config != null ? config : ScriptableObject.CreateInstance<EconomyConfig>());
            builder.RegisterEntryPoint<InMemoryEconomyService>(Lifetime.Singleton).As<IEconomyService>().AsSelf();
        }
    }
}
