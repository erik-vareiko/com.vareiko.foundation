using Zenject;

namespace Vareiko.Foundation.UI
{
    public static class FoundationUIInstaller
    {
        public static void Install(DiContainer container)
        {
            if (container.HasBinding<IUIService>())
            {
                return;
            }

            if (!container.HasBinding<SignalBus>())
            {
                SignalBusInstaller.Install(container);
            }

            container.DeclareSignal<UIReadySignal>();
            container.DeclareSignal<UIElementShownSignal>();
            container.DeclareSignal<UIElementHiddenSignal>();
            container.DeclareSignal<UIScreenShownSignal>();
            container.DeclareSignal<UIScreenHiddenSignal>();
            container.DeclareSignal<UIWindowQueuedSignal>();
            container.DeclareSignal<UIWindowShownSignal>();
            container.DeclareSignal<UIWindowClosedSignal>();
            container.DeclareSignal<UIWindowQueueDrainedSignal>();
            container.DeclareSignal<UIWindowResolvedSignal>();
            container.DeclareSignal<UIIntValueChangedSignal>();
            container.DeclareSignal<UIFloatValueChangedSignal>();
            container.DeclareSignal<UIBoolValueChangedSignal>();
            container.DeclareSignal<UIStringValueChangedSignal>();

            container.DeclareSignal<UiReadySignal>();
            container.DeclareSignal<UiScreenShownSignal>();
            container.DeclareSignal<UiScreenHiddenSignal>();

            container.BindInterfacesAndSelfTo<UIService>().AsSingle().NonLazy();
            container.BindInterfacesAndSelfTo<UIWindowManager>().AsSingle().NonLazy();
            container.BindInterfacesAndSelfTo<UIConfirmDialogService>().AsSingle();
            container.BindInterfacesAndSelfTo<UIValueEventService>().AsSingle();
        }
    }
}
