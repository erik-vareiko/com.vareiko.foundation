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

#if ENABLE_INPUT_SYSTEM
            container.Bind<IInputRebindStorage>().To<PlayerPrefsInputRebindStorage>().AsSingle().IfNotBound();
            container.BindInterfacesAndSelfTo<NewInputSystemAdapter>().AsSingle();
#endif

            container.Bind<IInputRebindService>().To<InputRebindService>().AsSingle().IfNotBound();
            container.Bind<IInputAdapter>().To<LegacyKeyboardInputAdapter>().AsSingle();
            container.Bind<IInputAdapter>().To<NullInputAdapter>().AsSingle();
            container.Bind<IInputService>().To<InputService>().AsSingle();
        }
    }
}
