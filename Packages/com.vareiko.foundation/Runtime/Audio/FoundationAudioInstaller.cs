using VContainer;
using VContainer.Unity;

namespace Vareiko.Foundation.Audio
{
    public static class FoundationAudioInstaller
    {
        public static void Install(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<AudioService>(Lifetime.Singleton).As<IAudioService>().AsSelf();
        }
    }
}
