using UnityEngine;
using VContainer;
using VContainer.Unity;
using MessagePipe;

namespace Vareiko.Foundation.Localization
{
    public static class FoundationLocalizationInstaller
    {
        public static void Install(IContainerBuilder builder, LocalizationConfig config = null)
        {
            builder.RegisterInstance(config != null ? config : ScriptableObject.CreateInstance<LocalizationConfig>());
            builder.RegisterEntryPoint<LocalizationService>(Lifetime.Singleton).As<ILocalizationService>().AsSelf();
        }

        // Message brokers live in the project scope (GlobalMessagePipe provider), so the
        // project composition calls this even when the module services install in the
        // scene scope.
        public static void RegisterSignals(IContainerBuilder builder, MessagePipeOptions signalOptions)
        {
            builder.RegisterMessageBroker<LanguageChangedSignal>(signalOptions);
            builder.RegisterMessageBroker<LocalizationKeyMissingSignal>(signalOptions);
        }
    }
}
