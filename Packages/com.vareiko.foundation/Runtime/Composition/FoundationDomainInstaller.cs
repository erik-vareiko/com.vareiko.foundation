using VContainer;
using VContainer.Unity;

namespace Vareiko.Foundation.Composition
{
    public abstract class FoundationDomainInstaller : LifetimeScope
    {
        protected sealed override void Configure(IContainerBuilder builder)
        {
            InstallDomainBindings(builder);
            InstallDomainSystems(builder);
            InstallDomainPresentation(builder);
        }

        protected virtual void InstallDomainBindings(IContainerBuilder builder)
        {
        }

        protected virtual void InstallDomainSystems(IContainerBuilder builder)
        {
        }

        protected virtual void InstallDomainPresentation(IContainerBuilder builder)
        {
        }
    }
}
