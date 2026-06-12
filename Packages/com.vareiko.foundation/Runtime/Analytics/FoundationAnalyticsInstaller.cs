using UnityEngine;
using VContainer;
using MessagePipe;

namespace Vareiko.Foundation.Analytics
{
    public static class FoundationAnalyticsInstaller
    {
        public static void Install(IContainerBuilder builder, AnalyticsConfig config = null)
        {
            builder.RegisterInstance(config != null ? config : ScriptableObject.CreateInstance<AnalyticsConfig>());
            builder.Register<AnalyticsService>(Lifetime.Singleton).As<IAnalyticsService>();
        }

        // Message brokers live in the project scope (GlobalMessagePipe provider), so the
        // project composition calls this even when the module services install in the
        // scene scope.
        public static void RegisterSignals(IContainerBuilder builder, MessagePipeOptions signalOptions)
        {
            builder.RegisterMessageBroker<AnalyticsEventTrackedSignal>(signalOptions);
            builder.RegisterMessageBroker<AnalyticsEventDroppedSignal>(signalOptions);
        }
    }
}
