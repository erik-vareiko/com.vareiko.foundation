using Zenject;

namespace Vareiko.Foundation.UI
{
    public static class FoundationUiInstaller
    {
        public static void Install(DiContainer container)
        {
            if (container.HasBindingId(typeof(IUiService), null, InjectSources.Local))
            {
                return;
            }

            if (!container.HasBinding<SignalBus>())
            {
                SignalBusInstaller.Install(container);
            }

            container.DeclareSignal<UiReadySignal>();
            container.DeclareSignal<UiScreenShownSignal>();
            container.DeclareSignal<UiScreenHiddenSignal>();

            container.BindInterfacesAndSelfTo<UiService>().AsSingle().NonLazy();
        }
    }
}
