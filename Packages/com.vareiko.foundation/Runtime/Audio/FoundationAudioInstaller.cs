using VContainer;
using VContainer.Unity;
using MessagePipe;

namespace Vareiko.Foundation.Audio
{
    public static class FoundationAudioInstaller
    {
        public static void Install(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<AudioService>(Lifetime.Singleton).As<IAudioService>().AsSelf();
        }

        // Message brokers live in the project scope (GlobalMessagePipe provider), so the
        // project composition calls this even when the module services install in the
        // scene scope.
        public static void RegisterSignals(IContainerBuilder builder, MessagePipeOptions signalOptions)
        {
            builder.RegisterMessageBroker<AudioVolumesChangedSignal>(signalOptions);
        }
    }
}
