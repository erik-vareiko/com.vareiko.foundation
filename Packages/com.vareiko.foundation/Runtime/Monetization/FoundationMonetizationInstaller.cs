using UnityEngine;
using Vareiko.Foundation.Observability;
using VContainer;
using VContainer.Unity;
using MessagePipe;

namespace Vareiko.Foundation.Monetization
{
    public static class FoundationMonetizationInstaller
    {
        public static void Install(IContainerBuilder builder, MonetizationPolicyConfig config = null)
        {
            builder.RegisterInstance(config != null ? config : ScriptableObject.CreateInstance<MonetizationPolicyConfig>());
            builder.RegisterEntryPoint<MonetizationPolicyService>(Lifetime.Singleton).As<IMonetizationPolicyService>().AsSelf();
            // Registered here (not in Observability) because the service subscribes to the
            // Ads/Iap/Push signals; the IMonetizationObservabilityService contract stays in
            // Observability so diagnostics can consume it without referencing monetization.
            builder.RegisterEntryPoint<MonetizationObservabilityService>(Lifetime.Singleton).As<IMonetizationObservabilityService>().AsSelf();
        }

        // Message brokers live in the project scope (GlobalMessagePipe provider), so the
        // project composition calls this even when the module services install in the
        // scene scope.
        public static void RegisterSignals(IContainerBuilder builder, MessagePipeOptions signalOptions)
        {
            builder.RegisterMessageBroker<MonetizationAdRecordedSignal>(signalOptions);
            builder.RegisterMessageBroker<MonetizationIapRecordedSignal>(signalOptions);
            builder.RegisterMessageBroker<MonetizationSessionResetSignal>(signalOptions);
            builder.RegisterMessageBroker<MonetizationAdBlockedSignal>(signalOptions);
            builder.RegisterMessageBroker<MonetizationIapBlockedSignal>(signalOptions);
        }
    }
}
