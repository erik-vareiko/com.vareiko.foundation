using NUnit.Framework;
using Vareiko.Foundation;
using Vareiko.Foundation.Analytics;
using Vareiko.Foundation.App;
using Vareiko.Foundation.Connectivity;
using Vareiko.Foundation.Consent;
using Vareiko.Foundation.Environment;
using Vareiko.Foundation.Features;
using Vareiko.Foundation.Rng;
using Vareiko.Foundation.Save;
using Vareiko.Foundation.Time;
using Zenject;

namespace Vareiko.Foundation.Tests.Composition
{
    /// <summary>
    /// Composition regression net for the DI migration (Phase 1).
    /// When the container is swapped to VContainer this file is rewritten against
    /// the new API and must keep resolving the same core surface — proving parity.
    /// </summary>
    public sealed class FoundationCompositionTests
    {
        [Test]
        public void InstallProjectServices_DoesNotThrow()
        {
            DiContainer container = new DiContainer();

            Assert.DoesNotThrow(() => FoundationRuntimeInstaller.InstallProjectServices(container));
        }

        [Test]
        public void InstallProjectServices_ResolvesCoreServices()
        {
            DiContainer container = new DiContainer();
            FoundationRuntimeInstaller.InstallProjectServices(container);

            Assert.That(container.Resolve<SignalBus>(), Is.Not.Null, "SignalBus");
            Assert.That(container.Resolve<IAppStateMachine>(), Is.Not.Null, "IAppStateMachine");
            Assert.That(container.Resolve<ISaveService>(), Is.Not.Null, "ISaveService");
            Assert.That(container.Resolve<IConsentService>(), Is.Not.Null, "IConsentService");
            Assert.That(container.Resolve<IFoundationTimeProvider>(), Is.Not.Null, "IFoundationTimeProvider");
            Assert.That(container.Resolve<IDeterministicRngService>(), Is.Not.Null, "IDeterministicRngService");
            Assert.That(container.Resolve<IEnvironmentService>(), Is.Not.Null, "IEnvironmentService");
            Assert.That(container.Resolve<IFeatureFlagService>(), Is.Not.Null, "IFeatureFlagService");
            Assert.That(container.Resolve<IAnalyticsService>(), Is.Not.Null, "IAnalyticsService");
            Assert.That(container.Resolve<IConnectivityService>(), Is.Not.Null, "IConnectivityService");
        }

        [Test]
        public void CoreServices_AreSingletons()
        {
            DiContainer container = new DiContainer();
            FoundationRuntimeInstaller.InstallProjectServices(container);

            IAppStateMachine first = container.Resolve<IAppStateMachine>();
            IAppStateMachine second = container.Resolve<IAppStateMachine>();

            Assert.That(first, Is.SameAs(second));
        }
    }
}
