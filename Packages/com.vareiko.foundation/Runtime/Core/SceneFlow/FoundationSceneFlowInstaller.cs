using VContainer;
using MessagePipe;

namespace Vareiko.Foundation.SceneFlow
{
    public static class FoundationSceneFlowInstaller
    {
        public static void Install(IContainerBuilder builder)
        {
            builder.Register<SceneFlowService>(Lifetime.Singleton).As<ISceneFlowService>();
        }

        // Message brokers live in the project scope (GlobalMessagePipe provider), so the
        // project composition calls this even when the module services install in the
        // scene scope.
        public static void RegisterSignals(IContainerBuilder builder, MessagePipeOptions signalOptions)
        {
            builder.RegisterMessageBroker<SceneLoadStartedSignal>(signalOptions);
            builder.RegisterMessageBroker<SceneLoadProgressSignal>(signalOptions);
            builder.RegisterMessageBroker<SceneLoadCompletedSignal>(signalOptions);
            builder.RegisterMessageBroker<SceneUnloadStartedSignal>(signalOptions);
            builder.RegisterMessageBroker<SceneUnloadCompletedSignal>(signalOptions);
        }
    }
}
