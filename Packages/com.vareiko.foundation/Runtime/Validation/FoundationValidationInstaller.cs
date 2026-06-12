using System.Collections.Generic;
using Vareiko.Foundation.Signals;
using VContainer;
using VContainer.Unity;

namespace Vareiko.Foundation.Validation
{
    public static class FoundationValidationInstaller
    {
        public static void Install(IContainerBuilder builder)
        {
            builder.Register<SaveSecurityStartupValidationRule>(Lifetime.Singleton).As<IStartupValidationRule>();
            builder.Register<BackendStartupValidationRule>(Lifetime.Singleton).As<IStartupValidationRule>();
            builder.Register<ObservabilityStartupValidationRule>(Lifetime.Singleton).As<IStartupValidationRule>();
            builder.RegisterEntryPoint<StartupValidationRunner>(resolver => new StartupValidationRunner(
                    new List<IStartupValidationRule>(resolver.Resolve<IEnumerable<IStartupValidationRule>>()),
                    resolver.Resolve<IFoundationSignalBus>()),
                Lifetime.Singleton)
                .AsSelf();
        }
    }
}
