# Basic Setup Sample

1. Create a scene with `ProjectContext` and add `FoundationProjectInstaller`.
2. Create gameplay scene with `SceneContext` and add `FoundationSceneInstaller`.
3. Add `UIScreenRegistry` and a few `UIScreen` instances.
4. Add optional bootstrap tasks implementing `IBootstrapTask`.
5. Assign optional configs (`AnalyticsConfig`, `BackendConfig`, `BackendReliabilityConfig`, `RemoteConfigCacheConfig`, `AssetServiceConfig`, `ConnectivityConfig`, `SaveSchemaConfig`, `SaveSecurityConfig`, `AutosaveConfig`, `FeatureFlagsConfig`, `EnvironmentConfig`, `ObservabilityConfig`, `EconomyConfig`).
6. Wire a consent UI to `IConsentService` before tracking analytics events in production.
7. Optional: add `LoadingOverlayPresenter` to scene UI and inject it through `SceneContext`.
8. Use `FoundationDomainInstaller` as a base for project-specific gameplay installers.
9. Optional: add `RefreshFeatureFlagsBootstrapTask` as a scene bootstrap task for explicit early refresh.

This sample folder is intentionally lightweight and code-only.
