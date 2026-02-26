using Vareiko.Foundation.Settings;
using Zenject;

namespace Vareiko.Foundation.Audio
{
    public static class FoundationAudioInstaller
    {
        public static void Install(DiContainer container)
        {
            if (container.HasBinding<IAudioService>())
            {
                return;
            }

            if (!container.HasBinding<SignalBus>())
            {
                SignalBusInstaller.Install(container);
            }

            if (!container.HasBinding<ISettingsService>())
            {
                FoundationSettingsInstaller.Install(container);
            }

            container.DeclareSignal<AudioVolumesChangedSignal>();
            container.BindInterfacesAndSelfTo<AudioService>().AsSingle().NonLazy();
        }
    }
}
