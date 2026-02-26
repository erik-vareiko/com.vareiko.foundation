using Zenject;

namespace Vareiko.Foundation.Composition
{
    public abstract class FoundationDomainInstaller : MonoInstaller
    {
        public sealed override void InstallBindings()
        {
            InstallDomainBindings();
            InstallDomainSystems();
            InstallDomainPresentation();
        }

        protected virtual void InstallDomainBindings()
        {
        }

        protected virtual void InstallDomainSystems()
        {
        }

        protected virtual void InstallDomainPresentation()
        {
        }
    }
}
