using Vareiko.Foundation.Save;
using Zenject;

namespace Vareiko.Foundation.Settings
{
    public static class FoundationSettingsInstaller
    {
        public static void Install(DiContainer container)
        {
            if (container.HasBinding<ISettingsService>())
            {
                return;
            }

            if (!container.HasBinding<ISaveService>())
            {
                FoundationSaveInstaller.Install(container);
            }

            if (!container.HasBinding<SignalBus>())
            {
                SignalBusInstaller.Install(container);
            }

            container.DeclareSignal<SettingsLoadedSignal>();
            container.DeclareSignal<SettingsChangedSignal>();
            container.BindInterfacesAndSelfTo<SettingsService>().AsSingle().NonLazy();
        }
    }
}
