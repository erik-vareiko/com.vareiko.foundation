using UnityEngine;
using VContainer;
using VContainer.Unity;
using MessagePipe;

namespace Vareiko.Foundation.Environment
{
    public static class FoundationEnvironmentInstaller
    {
        public static void Install(IContainerBuilder builder, EnvironmentConfig config = null)
        {
            builder.RegisterInstance(config != null ? config : ScriptableObject.CreateInstance<EnvironmentConfig>());
            builder.RegisterEntryPoint<EnvironmentService>(Lifetime.Singleton).As<IEnvironmentService>().AsSelf();
        }

        // Message brokers live in the project scope (GlobalMessagePipe provider), so the
        // project composition calls this even when the module services install in the
        // scene scope.
        public static void RegisterSignals(IContainerBuilder builder, MessagePipeOptions signalOptions)
        {
            builder.RegisterMessageBroker<EnvironmentProfileChangedSignal>(signalOptions);
            builder.RegisterMessageBroker<EnvironmentValueMissingSignal>(signalOptions);
        }
    }
}
