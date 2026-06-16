using MessagePipe;
using NUnit.Framework;
using Vareiko.Foundation.Signals;
using VContainer;
using VContainer.Unity;

namespace Vareiko.Foundation.Tests.Composition
{
    /// <summary>
    /// Phase 1a pilot. Validates the new DI/messaging stack — VContainer registration and
    /// resolution, MessagePipe brokers, the <see cref="IFoundationSignalBus"/> facade, entry
    /// points, and default-instance binding for ex-optional dependencies — in isolation,
    /// before the real modules are migrated off Zenject.
    /// </summary>
    public sealed class VContainerPilotTests
    {
        [Test]
        public void Facade_PublishSubscribe_RoundTrips()
        {
            ContainerBuilder builder = new ContainerBuilder();
            MessagePipeOptions options = builder.RegisterMessagePipe();
            builder.RegisterMessageBroker<PilotSignal>(options);
            builder.RegisterBuildCallback(c => GlobalMessagePipe.SetProvider(c.AsServiceProvider()));
            builder.Register<MessagePipeSignalBus>(Lifetime.Singleton).As<IFoundationSignalBus>();

            using (IObjectResolver container = builder.Build())
            {
                IFoundationSignalBus bus = container.Resolve<IFoundationSignalBus>();
                int received = 0;

                using (bus.Subscribe<PilotSignal>(signal => received = signal.Value))
                {
                    bus.Publish(new PilotSignal(42));
                }

                Assert.That(received, Is.EqualTo(42));
            }
        }

        [Test]
        public void EntryPoint_RegistersAsSingleton_AndInitializeIsWired()
        {
            ContainerBuilder builder = new ContainerBuilder();
            builder.Register<PilotEntryPoint>(Lifetime.Singleton).AsSelf().AsImplementedInterfaces();

            using (IObjectResolver container = builder.Build())
            {
                PilotEntryPoint first = container.Resolve<PilotEntryPoint>();
                PilotEntryPoint second = container.Resolve<PilotEntryPoint>();

                Assert.That(first, Is.Not.Null);
                Assert.That(first, Is.SameAs(second));

                // Auto-dispatch of IInitializable needs a runtime LifetimeScope (validated at
                // sample-scene boot in Phase 1c). Here we confirm the entry-point type resolves
                // and its Initialize body is callable — the resolution parity the sweep relies on.
                first.Initialize();
                Assert.That(first.Initialized, Is.True);
            }
        }

        [Test]
        public void DefaultInstance_ResolvesForExOptionalDependency()
        {
            ContainerBuilder builder = new ContainerBuilder();
            builder.RegisterInstance(new PilotConfig { Threshold = 7 });

            using (IObjectResolver container = builder.Build())
            {
                Assert.That(container.Resolve<PilotConfig>().Threshold, Is.EqualTo(7));
            }
        }

        [Test]
        public void WithParameter_SuppliesStringConstructorArgument()
        {
            ContainerBuilder builder = new ContainerBuilder();
            builder.Register<StringConsumer>(Lifetime.Singleton).WithParameter<string>("root/path");

            using (IObjectResolver container = builder.Build())
            {
                Assert.That(container.Resolve<StringConsumer>().Value, Is.EqualTo("root/path"));
            }
        }

        [Test]
        public void Decorator_ViaDelegateRegistration()
        {
            ContainerBuilder builder = new ContainerBuilder();
            builder.Register<InnerThing>(Lifetime.Singleton);
            builder.Register<IThing>(resolver => new Wrapper(resolver.Resolve<InnerThing>()), Lifetime.Singleton);

            using (IObjectResolver container = builder.Build())
            {
                IThing thing = container.Resolve<IThing>();
                Assert.That(thing, Is.TypeOf<Wrapper>());
                Assert.That(thing.Name, Is.EqualTo("wrap(inner)"));
            }
        }

        private sealed class StringConsumer
        {
            public readonly string Value;

            public StringConsumer(string value)
            {
                Value = value;
            }
        }

        private interface IThing
        {
            string Name { get; }
        }

        private sealed class InnerThing : IThing
        {
            public string Name => "inner";
        }

        private sealed class Wrapper : IThing
        {
            private readonly IThing _inner;

            public Wrapper(IThing inner)
            {
                _inner = inner;
            }

            public string Name => "wrap(" + _inner.Name + ")";
        }

        private readonly struct PilotSignal
        {
            public readonly int Value;

            public PilotSignal(int value)
            {
                Value = value;
            }
        }

        private sealed class PilotConfig
        {
            public int Threshold;
        }

        private sealed class PilotEntryPoint : IInitializable
        {
            public bool Initialized { get; private set; }

            public void Initialize()
            {
                Initialized = true;
            }
        }
    }
}
