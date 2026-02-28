# Foundation Usage Guide (`1.0+`)

This is a practical, end-to-end guide for using `com.vareiko.foundation` as a starter architecture in Unity.

## 1. What You Get
The package provides:
- deterministic bootstrap and service composition (`ProjectContext` + `FoundationProjectInstaller`);
- scene-local composition (`SceneContext` + `FoundationSceneInstaller`);
- modular runtime services (save/settings, analytics, backend, monetization, push, attribution, UI, observability);
- adapter-oriented integrations (safe null/simulated providers + bridge entry points);
- built-in runtime tests and validation tooling.

Use it as your baseline layer, then add game-specific domain modules above it.

## 2. Minimal Integration (Fresh Project)
1. Add `ProjectContext` to your bootstrap scene.
2. Attach `FoundationProjectInstaller`.
3. Add `SceneContext` to gameplay scenes.
4. Attach `FoundationSceneInstaller`.
5. Create and assign these configs first:
- `EnvironmentConfig`
- `ObservabilityConfig`
- `SaveSecurityConfig`
- `AutosaveConfig`

For the first run keep optional providers in fallback mode:
- backend: `None`
- IAP: `None` or `Simulated`
- ads: `None` or `Simulated`
- push: `None` or `Simulated`
- attribution: `None`

## 3. Installation Dependencies
Required:
- `com.cysharp.unitask`
- `net.bobbo.extenject`
- `com.unity.inputsystem`

Optional production dependencies:
- Unity IAP (`UNITY_PURCHASING`)
- host push SDK + `FOUNDATION_UNITY_NOTIFICATIONS`
- PlayFab SDK + `PLAYFAB_SDK`

## 4. How Runtime Is Composed
At project startup, `FoundationRuntimeInstaller.InstallProjectServices(...)` installs modules in a strict order (time/common/env/app/bootstrap/config/.../observability).  
The order is documented in `Documentation~/ARCHITECTURE.md`.

`FoundationProjectInstaller` exposes serialized config slots and forwards them to the runtime installer.

## 5. First Play Mode Checklist
1. Enter Play Mode in bootstrap scene.
2. Verify zero compile errors.
3. Verify no startup validation `Error`.
4. Verify boot transitions to expected app state.
5. Verify diagnostics snapshot is available via `IDiagnosticsService`.

## 6. Core Service Usage Patterns

### Save/Load
```csharp
using Cysharp.Threading.Tasks;
using Vareiko.Foundation.Save;
using Zenject;

public sealed class ProfileFacade
{
    private readonly ISaveService _save;

    [Inject]
    public ProfileFacade(ISaveService save)
    {
        _save = save;
    }

    public UniTask SaveProfileAsync(PlayerProfile profile)
    {
        return _save.SaveAsync("main", "profile", profile);
    }

    public UniTask<PlayerProfile> LoadProfileAsync()
    {
        return _save.LoadAsync("main", "profile", new PlayerProfile());
    }
}
```

### Settings
```csharp
using Vareiko.Foundation.Settings;
using Zenject;

public sealed class AudioSettingsFacade
{
    private readonly ISettingsService _settings;

    [Inject]
    public AudioSettingsFacade(ISettingsService settings)
    {
        _settings = settings;
    }

    public void SetMusicVolume(float volume01)
    {
        var next = _settings.Current;
        next.MusicVolume = volume01;
        _settings.Apply(next, saveImmediately: true);
    }
}
```

### Scene Flow
```csharp
using Cysharp.Threading.Tasks;
using UnityEngine.SceneManagement;
using Vareiko.Foundation.SceneFlow;
using Zenject;

public sealed class SceneRouter
{
    private readonly ISceneFlowService _sceneFlow;

    [Inject]
    public SceneRouter(ISceneFlowService sceneFlow)
    {
        _sceneFlow = sceneFlow;
    }

    public UniTask OpenGameplayAsync()
    {
        return _sceneFlow.LoadSceneAsync("Gameplay", LoadSceneMode.Single, setActive: true);
    }
}
```

### Feature Flags
```csharp
using Cysharp.Threading.Tasks;
using Vareiko.Foundation.Features;
using Zenject;

public sealed class FeatureGate
{
    private readonly IFeatureFlagService _flags;

    [Inject]
    public FeatureGate(IFeatureFlagService flags)
    {
        _flags = flags;
    }

    public async UniTask<bool> IsNewEconomyEnabledAsync()
    {
        await _flags.RefreshAsync();
        return _flags.IsEnabled("economy.v2", false);
    }
}
```

## 7. Custom Bootstrap Tasks
Add game-specific boot logic by implementing `IBootstrapTask`.

```csharp
using System.Threading;
using Cysharp.Threading.Tasks;
using Vareiko.Foundation.Bootstrap;
using Vareiko.Foundation.Save;
using Zenject;

public sealed class LoadProfileBootstrapTask : IBootstrapTask
{
    private readonly ISaveService _save;

    public int Order => 100;
    public string Name => "Load Player Profile";

    [Inject]
    public LoadProfileBootstrapTask(ISaveService save)
    {
        _save = save;
    }

    public async UniTask ExecuteAsync(CancellationToken cancellationToken)
    {
        _ = await _save.LoadAsync("main", "profile", new PlayerProfile(), cancellationToken);
    }
}
```

Bind it in your project installer or a domain installer.

## 8. Monetization and Comms Stack

### IAP
Main contract: `IInAppPurchaseService`
- `InitializeAsync()`
- `GetCatalogAsync()`
- `PurchaseAsync(productId)`
- `RestorePurchasesAsync()`

### Ads
Main contract: `IAdsService`
- `InitializeAsync()`
- `LoadAsync(placementId)`
- `ShowAsync(placementId)`

Use `IMonetizationPolicyService` to gate ad/purchase actions before calling providers.

### Push
Main contract: `IPushNotificationService`
- `InitializeAsync()`
- `RequestPermissionAsync()`
- `GetDeviceTokenAsync()`
- `SubscribeAsync(topic)`
- `UnsubscribeAsync(topic)`

## 9. External Bridge Integrations

### External Ads Bridge
For non-Unity mediation SDKs:

```csharp
using Cysharp.Threading.Tasks;
using Vareiko.Foundation.Ads;

public static class AdsSdkBridgeInstaller
{
    public static void Install()
    {
        ExternalAdsBridge.SetInitializeHandler(_ => UniTask.FromResult(AdsInitializeResult.Succeed()));
        ExternalAdsBridge.SetLoadHandler((placementId, _) => UniTask.FromResult(AdLoadResult.Succeed(placementId, AdPlacementType.Rewarded)));
        ExternalAdsBridge.SetShowHandler((placementId, _) => UniTask.FromResult(AdShowResult.Succeed(placementId, AdPlacementType.Rewarded, true, "reward.default", 1)));
    }
}
```

### External Attribution Bridge
For AppsFlyer/Adjust/other attribution SDKs:

```csharp
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using Vareiko.Foundation.Attribution;

public static class AttributionSdkBridgeInstaller
{
    public static void Install()
    {
        ExternalAttributionBridge.SetTrackEventHandler((eventName, properties, _) =>
        {
            IReadOnlyDictionary<string, string> payload = properties;
            // Forward payload to your attribution SDK here.
            return UniTask.FromResult(AttributionTrackResult.Succeed(eventName));
        });

        ExternalAttributionBridge.SetTrackRevenueHandler((data, _) =>
        {
            // Forward revenue to your attribution SDK here.
            return UniTask.FromResult(AttributionRevenueTrackResult.Succeed(data.ProductId, data.Currency, data.Amount));
        });
    }
}
```

At shutdown/domain reload call:
- `ExternalAdsBridge.ClearHandlers()`
- `ExternalAttributionBridge.ClearHandlers()`

## 10. Observability and Diagnostics
Use:
- `IDiagnosticsService` for live snapshot access;
- `IDiagnosticsSnapshotExportService.ExportAsync(label)` for QA/support exports.

Monitor key signals in `SignalBus` for:
- startup validation;
- monetization decisions/failures;
- push permission/topic flow;
- attribution and ads bridge failures.

## 11. Testing Strategy
Recommended levels:
1. Runtime unit tests per module (service contracts + provider mapping).
2. Integration smoke tests for bootstrap + scene flow + save/settings.
3. PlayMode smoke for scene wiring and audio/UI critical path.

Use package test assembly `Tests/Runtime` as the default pattern for new modules.

## 12. Release Checklist
1. Run `Tools/Vareiko/Foundation/Validate Project`.
2. Ensure package version and changelog are aligned.
3. Ensure no compile errors on clean import.
4. Ensure required runtime tests pass.
5. Tag release (`vX.Y.Z`) after commit.

## 13. Common Pitfalls
- Missing provider define/sdks with production provider selected.
- Enabling consent-required modules without loaded consent state.
- Registering bridge handlers too late (after provider init).
- Skipping `SceneContext`/`FoundationSceneInstaller` in gameplay scenes.

If you follow the starter flow and keep provider-heavy modules in fallback mode first, integration is usually stable from day one.
