using UnityEngine;
using VContainer;
using VContainer.Unity;
using MessagePipe;

namespace Vareiko.Foundation.Iap
{
    public static class FoundationIapInstaller
    {
        public static void Install(IContainerBuilder builder, IapConfig config = null)
        {
            builder.RegisterInstance(config != null ? config : ScriptableObject.CreateInstance<IapConfig>());

            if (config != null && config.Provider == InAppPurchaseProviderType.Simulated)
            {
                builder.RegisterEntryPoint<SimulatedInAppPurchaseService>(Lifetime.Singleton).As<IInAppPurchaseService>().AsSelf();
            }
            else if (config != null && config.Provider == InAppPurchaseProviderType.UnityIap)
            {
                builder.RegisterEntryPoint<UnityInAppPurchaseService>(Lifetime.Singleton).As<IInAppPurchaseService>().AsSelf();
            }
            else
            {
                builder.Register<NullInAppPurchaseService>(Lifetime.Singleton).As<IInAppPurchaseService>();
            }
        }

        // Message brokers live in the project scope (GlobalMessagePipe provider), so the
        // project composition calls this even when the module services install in the
        // scene scope.
        public static void RegisterSignals(IContainerBuilder builder, MessagePipeOptions signalOptions)
        {
            builder.RegisterMessageBroker<IapInitializedSignal>(signalOptions);
            builder.RegisterMessageBroker<IapPurchaseSucceededSignal>(signalOptions);
            builder.RegisterMessageBroker<IapPurchaseFailedSignal>(signalOptions);
            builder.RegisterMessageBroker<IapRestoreCompletedSignal>(signalOptions);
            builder.RegisterMessageBroker<IapRestoreFailedSignal>(signalOptions);
            builder.RegisterMessageBroker<IapOperationTelemetrySignal>(signalOptions);
        }
    }
}
