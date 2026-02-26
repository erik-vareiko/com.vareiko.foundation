using Vareiko.Foundation.UI;
using Zenject;

namespace Vareiko.Foundation.UINavigation
{
    public static class FoundationUiNavigationInstaller
    {
        public static void Install(DiContainer container)
        {
            if (container.HasBinding<IUiNavigationService>())
            {
                return;
            }

            if (!container.HasBinding<IUiService>())
            {
                FoundationUiInstaller.Install(container);
            }

            if (!container.HasBinding<SignalBus>())
            {
                SignalBusInstaller.Install(container);
            }

            container.DeclareSignal<UiNavigationChangedSignal>();
            container.Bind<IUiNavigationService>().To<UiNavigationService>().AsSingle().NonLazy();
        }
    }
}
