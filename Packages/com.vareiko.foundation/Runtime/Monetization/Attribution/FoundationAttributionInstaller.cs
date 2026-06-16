using UnityEngine;
using VContainer;
using VContainer.Unity;
using MessagePipe;

namespace Vareiko.Foundation.Attribution
{
    public static class FoundationAttributionInstaller
    {
        public static void Install(IContainerBuilder builder, AttributionConfig config = null)
        {
            builder.RegisterInstance(config != null ? config : ScriptableObject.CreateInstance<AttributionConfig>());

            if (config != null && config.Provider == AttributionProviderType.ExternalBridge)
            {
                builder.RegisterEntryPoint<ExternalAttributionBridgeService>(Lifetime.Singleton).As<IAttributionService>().AsSelf();
            }
            else
            {
                builder.Register<NullAttributionService>(Lifetime.Singleton).As<IAttributionService>();
            }
        }

        // Message brokers live in the project scope (GlobalMessagePipe provider), so the
        // project composition calls this even when the module services install in the
        // scene scope.
        public static void RegisterSignals(IContainerBuilder builder, MessagePipeOptions signalOptions)
        {
            builder.RegisterMessageBroker<AttributionInitializedSignal>(signalOptions);
            builder.RegisterMessageBroker<AttributionEventTrackedSignal>(signalOptions);
            builder.RegisterMessageBroker<AttributionEventTrackFailedSignal>(signalOptions);
            builder.RegisterMessageBroker<AttributionRevenueTrackedSignal>(signalOptions);
            builder.RegisterMessageBroker<AttributionRevenueTrackFailedSignal>(signalOptions);
        }
    }
}
