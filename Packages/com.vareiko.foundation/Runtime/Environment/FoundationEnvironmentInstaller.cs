using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Vareiko.Foundation.Environment
{
    public static class FoundationEnvironmentInstaller
    {
        public static void Install(IContainerBuilder builder, EnvironmentConfig config = null)
        {
            builder.RegisterInstance(config != null ? config : ScriptableObject.CreateInstance<EnvironmentConfig>());
            builder.RegisterEntryPoint<EnvironmentService>(Lifetime.Singleton).As<IEnvironmentService>().AsSelf();
        }
    }
}
