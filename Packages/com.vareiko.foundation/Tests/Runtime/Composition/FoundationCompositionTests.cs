using MessagePipe;
using NUnit.Framework;
using Vareiko.Foundation.Analytics;
using Vareiko.Foundation.App;
using Vareiko.Foundation.Connectivity;
using Vareiko.Foundation.Consent;
using Vareiko.Foundation.Environment;
using Vareiko.Foundation.Features;
using Vareiko.Foundation.Rng;
using Vareiko.Foundation.Save;
using Vareiko.Foundation.Signals;
using Vareiko.Foundation.Time;
using VContainer;

namespace Vareiko.Foundation.Tests.Composition
{
    /// <summary>
    /// Composition regression net for the DI migration (Phase 1). Rewritten against VContainer +
    /// MessagePipe in Phase 1c: the same core surface must keep resolving, proving parity with the
    /// Zenject composition it replaced.
    /// </summary>
    public sealed class FoundationCompositionTests
    {
        private static IObjectResolver BuildContainer()
        {
            ContainerBuilder builder = new ContainerBuilder();
            FoundationRuntimeInstaller.InstallProjectServices(builder);
            IObjectResolver container = builder.Build();
            GlobalMessagePipe.SetProvider(container.AsServiceProvider());
            return container;
        }

        [Test]
        public void InstallProjectServices_DoesNotThrow()
        {
            Assert.DoesNotThrow(() =>
            {
                using IObjectResolver container = BuildContainer();
            });
        }

        [Test]
        public void InstallProjectServices_ResolvesCoreServices()
        {
            using IObjectResolver container = BuildContainer();

            Assert.That(container.Resolve<IFoundationSignalBus>(), Is.Not.Null, "IFoundationSignalBus");
            Assert.That(container.Resolve<IAppStateMachine>(), Is.Not.Null, "IAppStateMachine");
            Assert.That(container.Resolve<ISaveService>(), Is.Not.Null, "ISaveService");
            Assert.That(container.Resolve<IConsentService>(), Is.Not.Null, "IConsentService");
            Assert.That(container.Resolve<IFoundationTimeProvider>(), Is.Not.Null, "IFoundationTimeProvider");
            Assert.That(container.Resolve<ITickService>(), Is.Not.Null, "ITickService");
            Assert.That(container.Resolve<IDeterministicRngService>(), Is.Not.Null, "IDeterministicRngService");
            Assert.That(container.Resolve<IEnvironmentService>(), Is.Not.Null, "IEnvironmentService");
            Assert.That(container.Resolve<IFeatureFlagService>(), Is.Not.Null, "IFeatureFlagService");
            Assert.That(container.Resolve<IAnalyticsService>(), Is.Not.Null, "IAnalyticsService");
            Assert.That(container.Resolve<IConnectivityService>(), Is.Not.Null, "IConnectivityService");
        }

        [Test]
        public void CoreServices_AreSingletons()
        {
            using IObjectResolver container = BuildContainer();

            IAppStateMachine first = container.Resolve<IAppStateMachine>();
            IAppStateMachine second = container.Resolve<IAppStateMachine>();

            Assert.That(first, Is.SameAs(second));
        }
    }
}
