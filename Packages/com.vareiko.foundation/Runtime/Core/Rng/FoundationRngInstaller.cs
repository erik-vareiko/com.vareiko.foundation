using UnityEngine;
using VContainer;

namespace Vareiko.Foundation.Rng
{
    public static class FoundationRngInstaller
    {
        public static void Install(IContainerBuilder builder, DeterministicRngConfig config = null)
        {
            builder.RegisterInstance(config != null ? config : ScriptableObject.CreateInstance<DeterministicRngConfig>());
            builder.Register<DeterministicRngService>(Lifetime.Singleton).As<IDeterministicRngService>();
        }
    }
}
