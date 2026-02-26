using Zenject;

namespace Vareiko.Foundation.Input
{
    public static class FoundationInputInstaller
    {
        public static void Install(DiContainer container)
        {
            if (container.HasBinding<IInputService>())
            {
                return;
            }

            if (!container.HasBinding<SignalBus>())
            {
                SignalBusInstaller.Install(container);
            }

            container.DeclareSignal<InputSchemeChangedSignal>();
            container.Bind<IInputAdapter>().To<LegacyKeyboardInputAdapter>().AsSingle();
            container.Bind<IInputAdapter>().To<NullInputAdapter>().AsSingle();
            container.Bind<IInputService>().To<InputService>().AsSingle();
        }
    }
}
