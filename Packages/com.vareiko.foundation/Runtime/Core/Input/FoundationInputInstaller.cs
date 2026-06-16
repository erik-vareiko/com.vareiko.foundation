using System.Collections.Generic;
using Vareiko.Foundation.Signals;
using VContainer;
using MessagePipe;

namespace Vareiko.Foundation.Input
{
    public static class FoundationInputInstaller
    {
        public static void Install(IContainerBuilder builder)
        {
#if ENABLE_INPUT_SYSTEM
            // The storage key is an optional ctor param with a baked default; VContainer ignores C#
            // default values and cannot resolve a bare string, so construct it via a factory to keep
            // the default key (parity with the old Zenject [InjectOptional(Id=...)] behaviour).
            builder.Register<IInputRebindStorage>(_ => new PlayerPrefsInputRebindStorage(), Lifetime.Singleton);
            builder.Register<NewInputSystemAdapter>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
#endif

            // NewInputSystemAdapter is only registered when the Input System is enabled; resolve it
            // leniently so the rebind service composes either way.
            builder.Register<InputRebindService>(resolver =>
                {
                    resolver.TryResolve<NewInputSystemAdapter>(out NewInputSystemAdapter adapter);
                    return new InputRebindService(adapter);
                }, Lifetime.Singleton)
                .As<IInputRebindService>();
            builder.Register<LegacyKeyboardInputAdapter>(Lifetime.Singleton).As<IInputAdapter>();
            builder.Register<NullInputAdapter>(Lifetime.Singleton).As<IInputAdapter>();
            builder.Register<InputService>(resolver => new InputService(
                    new List<IInputAdapter>(resolver.Resolve<IEnumerable<IInputAdapter>>()),
                    resolver.Resolve<IFoundationSignalBus>()),
                Lifetime.Singleton)
                .As<IInputService>();
        }

        // Message brokers live in the project scope (GlobalMessagePipe provider), so the
        // project composition calls this even when the module services install in the
        // scene scope.
        public static void RegisterSignals(IContainerBuilder builder, MessagePipeOptions signalOptions)
        {
            builder.RegisterMessageBroker<InputSchemeChangedSignal>(signalOptions);
        }
    }
}
