using System;
using NUnit.Framework;
using UnityEngine;
using Vareiko.Foundation.Config;
using Zenject;

namespace Vareiko.Foundation.Tests.Config
{
    public sealed class ConfigServiceTests
    {
        [Test]
        public void Register_AndTryGet_ReturnsRegisteredConfig_AndFiresSignal()
        {
            SignalBus signalBus = CreateSignalBus();
            int registeredSignals = 0;
            signalBus.Subscribe<ConfigRegisteredSignal>(_ => registeredSignals++);

            TestConfig config = ScriptableObject.CreateInstance<TestConfig>();
            try
            {
                ConfigService service = new ConfigService(signalBus);
                service.Register(config, "main");

                bool found = service.TryGet(out TestConfig resolved, "main");

                Assert.That(found, Is.True);
                Assert.That(resolved, Is.SameAs(config));
                Assert.That(registeredSignals, Is.EqualTo(1));
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        [Test]
        public void MissingConfig_FiresMissingSignal_AndGetRequiredThrows()
        {
            SignalBus signalBus = CreateSignalBus();
            ConfigMissingSignal missing = default;
            int missingSignals = 0;
            signalBus.Subscribe<ConfigMissingSignal>(signal =>
            {
                missing = signal;
                missingSignals++;
            });

            ConfigService service = new ConfigService(signalBus);

            bool found = service.TryGet(out TestConfig _, "missing");
            Assert.That(found, Is.False);

            InvalidOperationException exception = Assert.Throws<InvalidOperationException>(() => service.GetRequired<TestConfig>("missing"));
            Assert.That(exception.Message, Does.Contain("Config not found"));
            Assert.That(missingSignals, Is.EqualTo(2));
            Assert.That(missing.Id, Is.EqualTo("missing"));
            Assert.That(missing.TypeName, Is.EqualTo(nameof(TestConfig)));
        }

        [Test]
        public void Unregister_RemovesRegisteredConfig()
        {
            ConfigService service = new ConfigService();
            TestConfig config = ScriptableObject.CreateInstance<TestConfig>();
            try
            {
                service.Register(config);
                Assert.That(service.TryGet(out TestConfig _, "default"), Is.True);

                service.Unregister<TestConfig>();

                Assert.That(service.TryGet(out TestConfig _, "default"), Is.False);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(config);
            }
        }

        private static SignalBus CreateSignalBus()
        {
            DiContainer container = new DiContainer();
            SignalBusInstaller.Install(container);
            container.DeclareSignal<ConfigRegisteredSignal>();
            container.DeclareSignal<ConfigMissingSignal>();
            return container.Resolve<SignalBus>();
        }

        private sealed class TestConfig : ScriptableObject
        {
        }
    }
}
