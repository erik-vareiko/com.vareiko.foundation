using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Vareiko.Foundation.Localization
{
    public static class FoundationLocalizationInstaller
    {
        public static void Install(IContainerBuilder builder, LocalizationConfig config = null)
        {
            builder.RegisterInstance(config != null ? config : ScriptableObject.CreateInstance<LocalizationConfig>());
            builder.RegisterEntryPoint<LocalizationService>(Lifetime.Singleton).As<ILocalizationService>().AsSelf();
        }
    }
}
