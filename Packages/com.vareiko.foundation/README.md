# Vareiko Foundation

Reusable Zenject-first runtime architecture package for Unity projects.

## Included Modules
- Bootstrap pipeline (`IBootstrapTask`, `BootstrapRunner`, lifecycle signals).
- Common runtime helpers (`RetryPolicy`, health checks).
- Composition helpers (`FoundationDomainInstaller`).
- App state machine (`IAppStateMachine`).
- Asset management (`IAssetService`, Resources/Addressables providers).
- Config registry (`IConfigService`, `ConfigRegistry` bootstrap task).
- Connectivity monitoring (`IConnectivityService`).
- Feature flags (`IFeatureFlagService`) with remote-config fallback and local overrides.
- Input service (`IInputService`, adapter-based architecture).
- Scene flow (`ISceneFlowService`).
- Loading state orchestration (`ILoadingService`) with scene-signal integration.
- UI loading presenter (`LoadingOverlayPresenter`).
- Save system (`ISaveService`, file storage, JSON serializer).
- Save schema versioning/migration contracts (`ISaveMigration`) and security layer (`SaveSecurityConfig` + `SecureSaveSerializer`).
- Privacy and consent (`IConsentService`).
- Settings system (`ISettingsService`).
- Economy service (`IEconomyService`, in-memory baseline).
- Audio service (`IAudioService`).
- Observability (`IFoundationLogger`, diagnostics snapshot service, diagnostics overlay view).
- Startup validation (`IStartupValidationRule`, `StartupValidationRunner`).
- UI base and navigation (`UIScreen`, `IUiService`, `IUiNavigationService`).
- Analytics abstraction (`IAnalyticsService`).
- Backend abstraction (`IBackendService`, `IRemoteConfigService`, `ICloudFunctionService`) with PlayFab entry adapter, retry and cloud-function queue.

## Installation
1. Add a `ProjectContext` in your bootstrap scene.
2. Attach `FoundationProjectInstaller` to `ProjectContext`.
3. Add a `SceneContext` in gameplay scenes.
4. Attach `FoundationSceneInstaller` to `SceneContext`.
5. Optionally assign:
- `AnalyticsConfig`
- `BackendConfig`
- `BackendReliabilityConfig`
- `AssetServiceConfig`
- `EconomyConfig`
- `ConnectivityConfig`
- `SaveSchemaConfig`
- `SaveSecurityConfig`
- `RemoteConfigCacheConfig`
- `FeatureFlagsConfig`
- `ObservabilityConfig`
- `UIScreenRegistry`
- `ConfigRegistry[]` (optional, auto-registered as bootstrap tasks)
- `IBootstrapTask` MonoBehaviours

## Dependencies
- Zenject (`Zenject` asmdef)
- UniTask (`UniTask` asmdef)
- Optional: Addressables (`FOUNDATION_ADDRESSABLES` define to enable provider)

If your project does not resolve Zenject/UniTask from the default Unity registry, add OpenUPM scoped registry in `Packages/manifest.json`:

```json
{
  "scopedRegistries": [
    {
      "name": "OpenUPM",
      "url": "https://package.openupm.com",
      "scopes": [
        "com.cysharp",
        "com.svermeulen"
      ]
    }
  ],
  "dependencies": {
    "com.cysharp.unitask": "2.5.10",
    "com.svermeulen.extenject": "9.2.0-stcf3"
  }
}
```

## Notes
- `PlayFabBackendService` is an integration point and expects `PLAYFAB_SDK` define when real PlayFab SDK is installed.
- `PlayFabCloudFunctionService` is an integration point and expects `PLAYFAB_SDK` define when real PlayFab SDK is installed.
- `PlayFabRemoteConfigService` is an integration point and expects `PLAYFAB_SDK` define when real PlayFab SDK is installed.
- By default backend services run in safe null mode (`NullBackendService`, `NullRemoteConfigService`, `NullCloudFunctionService`).
- Analytics is privacy-first by default: events are blocked until consent is explicitly collected and granted.
