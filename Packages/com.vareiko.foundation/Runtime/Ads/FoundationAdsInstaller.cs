using UnityEngine;
using VContainer;
using VContainer.Unity;
using MessagePipe;

namespace Vareiko.Foundation.Ads
{
    public static class FoundationAdsInstaller
    {
        public static void Install(IContainerBuilder builder, AdsConfig config = null)
        {
            builder.RegisterInstance(config != null ? config : ScriptableObject.CreateInstance<AdsConfig>());

            if (config != null && config.Provider == AdsProviderType.Simulated)
            {
                builder.RegisterEntryPoint<SimulatedAdsService>(Lifetime.Singleton).As<IAdsService>().AsSelf();
            }
            else if (config != null && config.Provider == AdsProviderType.ExternalBridge)
            {
                builder.RegisterEntryPoint<ExternalAdsBridgeService>(Lifetime.Singleton).As<IAdsService>().AsSelf();
            }
            else
            {
                builder.Register<NullAdsService>(Lifetime.Singleton).As<IAdsService>();
            }
        }

        // Message brokers live in the project scope (GlobalMessagePipe provider), so the
        // project composition calls this even when the module services install in the
        // scene scope.
        public static void RegisterSignals(IContainerBuilder builder, MessagePipeOptions signalOptions)
        {
            builder.RegisterMessageBroker<AdsInitializedSignal>(signalOptions);
            builder.RegisterMessageBroker<AdLoadedSignal>(signalOptions);
            builder.RegisterMessageBroker<AdLoadFailedSignal>(signalOptions);
            builder.RegisterMessageBroker<AdShownSignal>(signalOptions);
            builder.RegisterMessageBroker<AdShowFailedSignal>(signalOptions);
            builder.RegisterMessageBroker<AdRewardGrantedSignal>(signalOptions);
            builder.RegisterMessageBroker<AdsOperationTelemetrySignal>(signalOptions);
        }
    }
}
