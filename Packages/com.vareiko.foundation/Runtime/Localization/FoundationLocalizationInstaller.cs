using Zenject;

namespace Vareiko.Foundation.Localization
{
    public static class FoundationLocalizationInstaller
    {
        public static void Install(DiContainer container, LocalizationConfig config = null)
        {
            if (container.HasBinding<ILocalizationService>())
            {
                return;
            }

            if (!container.HasBinding<SignalBus>())
            {
                SignalBusInstaller.Install(container);
            }

            if (config != null)
            {
                container.BindInstance(config).IfNotBound();
            }

            container.DeclareSignal<LanguageChangedSignal>();
            container.DeclareSignal<LocalizationKeyMissingSignal>();
            container.BindInterfacesAndSelfTo<LocalizationService>().AsSingle().NonLazy();
        }
    }
}
