using System.Collections.Generic;
using UnityEngine;
using Vareiko.Foundation.Signals;
using VContainer;

namespace Vareiko.Foundation.AssetManagement
{
    public static class FoundationAssetInstaller
    {
        public static void Install(IContainerBuilder builder, AssetServiceConfig config = null)
        {
            builder.RegisterInstance(config != null ? config : ScriptableObject.CreateInstance<AssetServiceConfig>());
            builder.Register<ResourcesAssetProvider>(Lifetime.Singleton).As<IAssetProvider>();
            builder.Register<AddressablesAssetProvider>(Lifetime.Singleton).As<IAssetProvider>();
            builder.Register<AssetService>(resolver => new AssetService(
                    new List<IAssetProvider>(resolver.Resolve<IEnumerable<IAssetProvider>>()),
                    resolver.Resolve<AssetServiceConfig>(),
                    resolver.Resolve<IFoundationSignalBus>()),
                Lifetime.Singleton)
                .As<IAssetService>();
        }
    }
}
