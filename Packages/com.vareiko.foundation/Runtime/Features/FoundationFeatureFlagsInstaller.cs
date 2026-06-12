using UnityEngine;
using VContainer;
using VContainer.Unity;
using MessagePipe;

namespace Vareiko.Foundation.Features
{
    public static class FoundationFeatureFlagsInstaller
    {
        public static void Install(IContainerBuilder builder, FeatureFlagsConfig config = null)
        {
            builder.RegisterInstance(config != null ? config : ScriptableObject.CreateInstance<FeatureFlagsConfig>());
            builder.RegisterEntryPoint<FeatureFlagService>(Lifetime.Singleton).As<IFeatureFlagService>().AsSelf();
        }

        // Message brokers live in the project scope (GlobalMessagePipe provider), so the
        // project composition calls this even when the module services install in the
        // scene scope.
        public static void RegisterSignals(IContainerBuilder builder, MessagePipeOptions signalOptions)
        {
            builder.RegisterMessageBroker<FeatureFlagsRefreshedSignal>(signalOptions);
            builder.RegisterMessageBroker<FeatureFlagOverriddenSignal>(signalOptions);
        }
    }
}
