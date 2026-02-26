using UnityEngine;

namespace Vareiko.Foundation.Backend
{
    [CreateAssetMenu(menuName = "Vareiko/Foundation/Backend Config")]
    public sealed class BackendConfig : ScriptableObject
    {
        [SerializeField] private BackendProviderType _provider = BackendProviderType.None;
        [SerializeField] private string _titleId;
        [SerializeField] private bool _enableCloudSave;
        [SerializeField] private bool _enableEconomy;
        [SerializeField] private bool _enableRemoteConfig;
        [SerializeField] private bool _enableCloudFunctions;

        public BackendProviderType Provider => _provider;
        public string TitleId => _titleId;
        public bool EnableCloudSave => _enableCloudSave;
        public bool EnableEconomy => _enableEconomy;
        public bool EnableRemoteConfig => _enableRemoteConfig;
        public bool EnableCloudFunctions => _enableCloudFunctions;
    }
}
