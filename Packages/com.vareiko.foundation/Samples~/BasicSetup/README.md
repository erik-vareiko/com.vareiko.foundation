# Basic Setup Sample

Included assets:
- `Scenes/FoundationSampleScene.unity` - ready sample scene.
- `Scripts/FoundationSampleSceneBootstrap.cs` - bootstrap helper that wires:
  - `ProjectContext` + `FoundationProjectInstaller`
  - `SceneContext` + `FoundationSceneInstaller`
  - `UIRoot` + `UIRegistry`

Usage:
1. Import sample from Package Manager.
2. Open `FoundationSampleScene`.
3. Press Play.
4. Optional: call `Ensure Foundation Wiring` from component context menu to re-apply setup in edit mode.

After that you can add project-specific modules:
- bootstrap tasks implementing `IBootstrapTask`
- optional configs (`AnalyticsConfig`, `BackendConfig`, `BackendReliabilityConfig`, `RemoteConfigCacheConfig`, `AssetServiceConfig`, `ConnectivityConfig`, `SaveSchemaConfig`, `SaveSecurityConfig`, `AutosaveConfig`, `FeatureFlagsConfig`, `EnvironmentConfig`, `LocalizationConfig`, `ObservabilityConfig`, `EconomyConfig`, `IapConfig`)
- project installers derived from `FoundationDomainInstaller`
