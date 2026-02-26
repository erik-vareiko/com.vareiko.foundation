using UnityEngine;

namespace Vareiko.Foundation.AssetManagement
{
    [CreateAssetMenu(menuName = "Vareiko/Foundation/Asset Service Config")]
    public sealed class AssetServiceConfig : ScriptableObject
    {
        [SerializeField] private AssetProviderType _provider = AssetProviderType.Resources;

        public AssetProviderType Provider => _provider;
    }
}
