using Vareiko.Foundation.UI;
using Zenject;

namespace Vareiko.Foundation.UINavigation
{
    public static class FoundationUINavigationInstaller
    {
        public static void Install(DiContainer container)
        {
            if (container.HasBinding<IUINavigationService>())
            {
                return;
            }

            if (!container.HasBinding<IUIService>())
            {
                FoundationUIInstaller.Install(container);
            }

            if (!container.HasBinding<SignalBus>())
            {
                SignalBusInstaller.Install(container);
            }

            container.DeclareSignal<UINavigationChangedSignal>();
            container.DeclareSignal<UiNavigationChangedSignal>();
            container.BindInterfacesAndSelfTo<UINavigationService>().AsSingle().NonLazy();
        }
    }
}
