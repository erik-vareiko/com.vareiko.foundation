# Vareiko Foundation

Reusable Zenject-first runtime architecture package for Unity projects.

## Included Modules
- Bootstrap pipeline (`IBootstrapTask`, `BootstrapRunner`, lifecycle signals).
- Common runtime helpers (`RetryPolicy`, health checks).
- Composition helpers (`FoundationDomainInstaller`).
- App state machine (`IAppStateMachine`).
- Asset management (`IAssetService`, Resources/Addressables providers, reference tracking).
- Config registry (`IConfigService`, `ConfigRegistry` bootstrap task).
- Runtime environments (`IEnvironmentService`, profile-based key/value access).
- Connectivity monitoring (`IConnectivityService`).
- Feature flags (`IFeatureFlagService`) with remote-config fallback and local overrides.
- Localization baseline (`ILocalizationService`, language switching and fallback lookups).
- Input service (`IInputService`, adapter-based architecture).
- Scene flow (`ISceneFlowService`).
- Loading state orchestration (`ILoadingService`) with scene-signal integration.
- UI loading presenter (`LoadingOverlayPresenter`).
- Save system (`ISaveService`, atomic file storage, JSON serializer, rolling backups, autosave scheduler).
- Save schema versioning/migration contracts (`ISaveMigration`) and security layer (`SaveSecurityConfig` + `SecureSaveSerializer`).
- Privacy and consent (`IConsentService`).
- Settings system (`ISettingsService`).
- Economy service (`IEconomyService`, in-memory baseline).
- Audio service (`IAudioService`).
- Observability (`IFoundationLogger`, diagnostics snapshot service, diagnostics overlay view, global exception boundary, asset/save diagnostics signals).
- Startup validation (`IStartupValidationRule`, `StartupValidationRunner`).
- UI base and navigation (`UIElement`, `UIScreen`, `UIWindow`, `UIPanel`, `UIItemView`, `UIButtonView`, `IUIService`, `IUINavigationService`, `IUIWindowManager`).
- UI button actions (`UIWindowOpenButtonAction`, `UIWindowCloseButtonAction`) and button-state binding (`UIBoolButtonInteractableBinder`).
- Analytics abstraction (`IAnalyticsService`).
- Backend abstraction (`IBackendService`, `IRemoteConfigService`, `ICloudFunctionService`) with PlayFab entry adapter, retry and cloud-function queue.
- Editor tooling: module scaffolder (`Tools/Vareiko/Foundation/Create Runtime Module`).

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
- `AutosaveConfig`
- `RemoteConfigCacheConfig`
- `FeatureFlagsConfig`
- `EnvironmentConfig`
- `LocalizationConfig`
- `ObservabilityConfig`
- `UIRegistry` (or legacy `UIScreenRegistry`)
- `ConfigRegistry[]` (optional, auto-registered as bootstrap tasks)
- `IBootstrapTask` MonoBehaviours

## Dependencies
- Zenject (`Zenject` asmdef, OpenUPM package `net.bobbo.extenject`)
- UniTask (`UniTask` asmdef, OpenUPM package `com.cysharp.unitask`)
- Optional: Addressables (`FOUNDATION_ADDRESSABLES` define to enable provider)

`com.vareiko.foundation` declares Zenject/UniTask as package dependencies.
Host project must have OpenUPM scoped registry configured.

OpenUPM commands:

```bash
openupm add com.cysharp.unitask
openupm add net.bobbo.extenject
```

Example `Packages/manifest.json`:

```json
{
  "scopedRegistries": [
    {
      "name": "OpenUPM",
      "url": "https://package.openupm.com",
      "scopes": [
        "com.cysharp",
        "net.bobbo"
      ]
    }
  ],
  "dependencies": {
    "com.cysharp.unitask": "2.5.10",
    "net.bobbo.extenject": "9.3.2"
  }
}
```

## Notes
- `PlayFabBackendService` is an integration point and expects `PLAYFAB_SDK` define when real PlayFab SDK is installed.
- `PlayFabCloudFunctionService` is an integration point and expects `PLAYFAB_SDK` define when real PlayFab SDK is installed.
- `PlayFabRemoteConfigService` is an integration point and expects `PLAYFAB_SDK` define when real PlayFab SDK is installed.
- By default backend services run in safe null mode (`NullBackendService`, `NullRemoteConfigService`, `NullCloudFunctionService`).
- Analytics is privacy-first by default: events are blocked until consent is explicitly collected and granted.
- Runtime smoke tests are included in `Tests/Runtime` (`Vareiko.Foundation.Tests` assembly).
- Runtime module scaffolder generates `IService/Service/Config/Signals/Installer` templates into your `Assets` folder.
- Project validator menu: `Tools/Vareiko/Foundation/Validate Project`.

## Event-Driven UI Template
- Publish values from domain/services through `IUIValueEventService`:
  - `SetInt("hud.coins", value)`
  - `SetFloat("hud.hp", value)`
  - `SetBool("hud.alive", value)`
  - `SetString("hud.player_name", value)`
- Bind values on scene objects with ready components:
  - `UIIntTextBinder`
  - `UIFloatTextBinder`
  - `UIStringTextBinder`
  - `UIBoolGameObjectBinder`
  - `UIBoolButtonInteractableBinder`
  - `UIItemCountBinder` (binds item collection size from int key)
- All binder components consume `SignalBus` + key matching, so UI updates are push-based without per-frame polling.

### Button Actions
- `UIWindowOpenButtonAction`:
  - subscribes to `UIButtonView.OnClicked`
  - enqueues target window in `IUIWindowManager` with configured priority/duplicate policy
- `UIWindowCloseButtonAction`:
  - closes current window by default
  - can close a specific window id when configured

### Collection Binding
- `UIItemCollectionBinder` manages pooled `UIItemView` instances and supports:
  - grow/shrink by count
  - hide extra items or destroy on shrink
  - retrieving active item views for data rendering
- Typical flow:
  - `UIItemCountBinder` listens to `IUIValueEventService` key (for example `inventory.count`)
  - updates collection size reactively
  - presenter fills visible items with domain data

### Reactive Streams (Built-in)
- `IUIValueEventService` also exposes reactive streams:
  - `ObserveInt(key)`
  - `ObserveFloat(key)`
  - `ObserveBool(key)`
  - `ObserveString(key)`
- Example:
```csharp
private IDisposable _coinsSubscription;

void Bind(IUIValueEventService values)
{
    _coinsSubscription = values.ObserveInt("hud.coins")
        .Subscribe(v => coinsText.text = v.ToString(), true);
}

void Unbind()
{
    _coinsSubscription?.Dispose();
    _coinsSubscription = null;
}
```

- Compose derived streams without extra dependencies:
```csharp
var hpPercent = values.ObserveFloat("hud.hp")
    .Combine(values.ObserveFloat("hud.hp_max"), (hp, max) => max <= 0f ? 0f : hp / max);

var hpLabel = hpPercent.Map(p => $"{p * 100f:0}%");
```
