using Vareiko.Foundation.Analytics;
using Vareiko.Foundation.AssetManagement;
using Vareiko.Foundation.Backend;
using Vareiko.Foundation.Connectivity;
using Vareiko.Foundation.Economy;
using Vareiko.Foundation.Features;
using Vareiko.Foundation.Observability;
using Vareiko.Foundation.Save;
using UnityEngine;
using Zenject;

namespace Vareiko.Foundation.Installers
{
    public sealed class FoundationProjectInstaller : MonoInstaller
    {
        [SerializeField] private AnalyticsConfig _analyticsConfig;
        [SerializeField] private BackendConfig _backendConfig;
        [SerializeField] private AssetServiceConfig _assetConfig;
        [SerializeField] private EconomyConfig _economyConfig;
        [SerializeField] private ConnectivityConfig _connectivityConfig;
        [SerializeField] private SaveSchemaConfig _saveSchemaConfig;
        [SerializeField] private SaveSecurityConfig _saveSecurityConfig;
        [SerializeField] private BackendReliabilityConfig _backendReliabilityConfig;
        [SerializeField] private RemoteConfigCacheConfig _remoteConfigCacheConfig;
        [SerializeField] private FeatureFlagsConfig _featureFlagsConfig;
        [SerializeField] private ObservabilityConfig _observabilityConfig;

        public override void InstallBindings()
        {
            FoundationRuntimeInstaller.InstallProjectServices(
                Container,
                _analyticsConfig,
                _backendConfig,
                _assetConfig,
                _economyConfig,
                _connectivityConfig,
                _saveSchemaConfig,
                _saveSecurityConfig,
                _backendReliabilityConfig,
                _remoteConfigCacheConfig,
                _featureFlagsConfig,
                _observabilityConfig);
        }
    }
}
