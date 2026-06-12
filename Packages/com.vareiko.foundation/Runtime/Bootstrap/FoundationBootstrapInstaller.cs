using System.Collections.Generic;
using Vareiko.Foundation.App;
using Vareiko.Foundation.Signals;
using VContainer;
using VContainer.Unity;

namespace Vareiko.Foundation.Bootstrap
{
    public static class FoundationBootstrapInstaller
    {
        public static void Install(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<BootstrapRunner>(resolver => new BootstrapRunner(
                    new List<IBootstrapTask>(resolver.Resolve<IEnumerable<IBootstrapTask>>()),
                    resolver.Resolve<IFoundationSignalBus>(),
                    resolver.Resolve<IAppStateMachine>()),
                Lifetime.Singleton)
                .AsSelf();
        }
    }
}
