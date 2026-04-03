using Zenject;

namespace Vareiko.Foundation.Rng
{
    public static class FoundationRngInstaller
    {
        public static void Install(DiContainer container, DeterministicRngConfig config = null)
        {
            if (container.HasBinding<IDeterministicRngService>())
            {
                return;
            }

            if (config != null)
            {
                container.BindInstance(config).IfNotBound();
            }

            container.Bind<IDeterministicRngService>().To<DeterministicRngService>().AsSingle();
        }
    }
}
