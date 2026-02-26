using Vareiko.Foundation.Save;
using Zenject;

namespace Vareiko.Foundation.Consent
{
    public static class FoundationConsentInstaller
    {
        public static void Install(DiContainer container)
        {
            if (container.HasBinding<IConsentService>())
            {
                return;
            }

            if (!container.HasBinding<SignalBus>())
            {
                SignalBusInstaller.Install(container);
            }

            FoundationSaveInstaller.Install(container);
            container.DeclareSignal<ConsentLoadedSignal>();
            container.DeclareSignal<ConsentChangedSignal>();
            container.BindInterfacesAndSelfTo<ConsentService>().AsSingle().NonLazy();
        }
    }
}
