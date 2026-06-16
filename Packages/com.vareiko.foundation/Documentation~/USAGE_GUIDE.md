# Foundation Usage Guide (`3.0`)

This is a practical, end-to-end guide for using `com.vareiko.foundation` as a starter architecture in Unity.

## 1. What You Get
The package provides:
- deterministic bootstrap and service composition (`FoundationProjectInstaller`, a VContainer `LifetimeScope`);
- scene-local composition (`FoundationSceneInstaller`, a child `LifetimeScope`);
- modular runtime services (save/settings, analytics, backend, monetization, push, attribution, UI, observability);
- typed cloud command service with idempotency/retry queue contracts;
- deterministic RNG streams with snapshot/restore support;
- adapter-oriented integrations (safe null/simulated providers + bridge entry points);
- core gameplay primitives: object pooling, central tick service, `Result<T>`, generic FSM with extensible app states;
- modular assemblies — reference `Vareiko.Foundation.Core` plus the modules you need, or the `Vareiko.Foundation` umbrella for everything;
- built-in runtime tests and validation tooling.

Use it as your baseline layer, then add game-specific domain modules above it.

## 2. Minimal Integration (Fresh Project)
1. Add `FoundationProjectInstaller` to your bootstrap scene (it is the project-scope `LifetimeScope`).
2. Add `FoundationSceneInstaller` to gameplay scenes (it parents to the project scope automatically).
3. Create and assign these configs first:
- `EnvironmentConfig`
- `ObservabilityConfig`
- `SaveSecurityConfig`
- `AutosaveConfig`
- `BackendCommandConfig` (if you use `ICloudCommandService`)
- `DeterministicRngConfig` (if you use deterministic run seeds)

For the first run keep optional providers in fallback mode:
- backend: `None`
- IAP: `None` or `Simulated`
- ads: `None` or `Simulated`
- push: `None` or `Simulated`
- attribution: `None`

## 3. Installation Dependencies
Required:
- `com.cysharp.unitask`
- `jp.hadashikick.vcontainer`
- `com.cysharp.messagepipe`
- `com.cysharp.messagepipe.vcontainer`
- `com.unity.nuget.newtonsoft-json`
- `com.unity.inputsystem`

Optional production dependencies:
- Unity IAP (`UNITY_PURCHASING`)
- host push SDK + `FOUNDATION_UNITY_NOTIFICATIONS`
- PlayFab SDK + `PLAYFAB_SDK`

## 4. How Runtime Is Composed
At project startup, `FoundationRuntimeInstaller.InstallProjectServices(...)` installs modules in a strict order (time/common/rng/env/app/bootstrap/config/.../observability).  
The order is documented in `Documentation~/ARCHITECTURE.md`.

`FoundationProjectInstaller` exposes serialized config slots and forwards them to the runtime installer.

Since 3.0 the package ships as 10 assemblies (see `ARCHITECTURE.md`, "Assembly Layout"). A subset host references `Vareiko.Foundation.Core` + chosen modules and composes its own project `LifetimeScope`: `RegisterMessagePipe`, the `IFoundationSignalBus` facade, then each module's `RegisterSignals(builder, options)` + `Install(builder, ...)`. Signal brokers always belong in the project scope.

## 5. First Play Mode Checklist
1. Enter Play Mode in bootstrap scene.
2. Verify zero compile errors.
3. Verify no startup validation `Error`.
4. Verify boot transitions to expected app state.
5. Verify diagnostics snapshot is available via `IDiagnosticsService`.

## 6. Core Service Usage Patterns

### Save/Load
`FoundationSaveInstaller` binds `ISaveStorage` to `PlayerPrefsSaveStorage` by default. Each `slot/key` pair is stored as a separate PlayerPrefs string containing the current JSON save envelope; rolling backups use separate `.bakN` PlayerPrefs keys. Projects that need file-backed saves can pre-bind `ISaveStorage` to `FileSaveStorage` before calling the foundation save installer.

Since 3.0 the default payload serializer is `NewtonsoftJsonSaveSerializer` — dictionaries, nullables and polymorphic payloads serialize correctly. It writes the same `{"Value": ...}` envelope as the previous JsonUtility serializer, so older saves keep loading; `JsonUnitySaveSerializer` remains available as a dependency-free fallback.

```csharp
using Cysharp.Threading.Tasks;
using Vareiko.Foundation.Save;

public sealed class ProfileFacade
{
    private readonly ISaveService _save;

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

public sealed class AudioSettingsFacade
{
    private readonly ISettingsService _settings;

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

public sealed class SceneRouter
{
    private readonly ISceneFlowService _sceneFlow;

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

public sealed class FeatureGate
{
    private readonly IFeatureFlagService _flags;

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

### Cloud Commands
```csharp
using System;
using Cysharp.Threading.Tasks;
using Vareiko.Foundation.Backend;

public sealed class RunBackendFacade
{
    private readonly ICloudCommandService _commands;

    public RunBackendFacade(ICloudCommandService commands)
    {
        _commands = commands;
    }

    public UniTask<CloudCommandResponse> StartRunAsync(string payloadJson)
    {
        CloudCommandRequest request = new CloudCommandRequest(
            commandName: "StartRun",
            idempotencyKey: GenerateUuidV7(),
            correlationId: Guid.NewGuid().ToString(),
            requestVersion: "1",
            payloadJson: payloadJson ?? "{}",
            playerId: "player-1",
            clientUnixMs: DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        return _commands.ExecuteAsync(request);
    }

    private static string GenerateUuidV7()
    {
        return "01890f2e-5c5b-7b2f-a4ab-2f5bdcf18a22";
    }
}
```

### Deterministic RNG
```csharp
using Vareiko.Foundation.Rng;

public sealed class RunRngFacade
{
    private readonly IDeterministicRngService _rng;

    public RunRngFacade(IDeterministicRngService rng)
    {
        _rng = rng;
    }

    public void InitializeRun(int rootSeed)
    {
        _rng.Initialize(rootSeed);
    }

    public int RollNodeIndex(int maxExclusive)
    {
        IDeterministicRngStream stream = _rng.CreateStream("run.nodes");
        return stream.NextInt(0, maxExclusive);
    }
}
```

## 7. Core Primitives (3.0)

### Tick service
Central ordered update loop — replaces per-service `Update()` loops and coroutines:

```csharp
using Vareiko.Foundation;
using Vareiko.Foundation.Time;

public sealed class SpawnDirector
{
    private readonly ITickService _tick;
    private readonly CompositeDisposable _subscriptions = new CompositeDisposable();

    public SpawnDirector(ITickService tick)
    {
        _tick = tick;
    }

    public void StartRun()
    {
        _subscriptions.Add(_tick.RegisterTick(OnTick, order: 0));
        _subscriptions.Add(_tick.Repeat(1.5f, SpawnWave));
        _subscriptions.Add(_tick.Delay(60f, FinishRun, useUnscaledTime: true));
    }

    public void StopRun()
    {
        _subscriptions.Clear();
    }

    private void OnTick(float deltaTime) { /* ... */ }
    private void SpawnWave() { /* ... */ }
    private void FinishRun() { /* ... */ }
}
```

Listeners run in explicit order; timers accumulate scaled or unscaled time and catch up after long frames; `IsPaused` gates everything. In EditMode tests drive `TickService.Advance(deltaTime, unscaledDeltaTime)` manually.

### Object pooling

```csharp
using Vareiko.Foundation.Pooling;

ComponentPool<Projectile> pool = new ComponentPool<Projectile>(projectilePrefab, poolRoot, maxSize: 64, prewarmCount: 16);
Projectile projectile = pool.Get();      // activated instance
pool.Release(projectile);                // deactivated, reparented, reused
```

`ObjectPool<T>` pools plain classes (factory + get/release/destroy callbacks, double-release detection); `GetScoped(out item)` returns a `using`-scope that releases automatically.

### Result

```csharp
public Result<PlayerProfile> ParseProfile(string raw)
{
    return _serializer.TryDeserialize(raw, out PlayerProfile profile)
        ? Result<PlayerProfile>.Success(profile)
        : Result<PlayerProfile>.Fail("Profile payload is corrupted.");
}
```

`Result`/`Result<T>` (namespace `Vareiko.Foundation`) is the default for new APIs. Domain results that carry error codes or retry flags (backend, cloud sync) keep their own types.

### Custom app states and generic FSM

`AppState` is a string-backed struct: the well-known states (`Boot`, `MainMenu`, `Gameplay`, `Error`, ...) are unchanged, and hosts mint their own:

```csharp
private static readonly AppState MetaShop = new AppState("MetaShop");

_appStateMachine.TryEnter(MetaShop);     // flows through the standard lifecycle rules
```

For any other state set use `StateMachine<TState>` directly (transition guard + change event):

```csharp
StateMachine<RunPhase> fsm = new StateMachine<RunPhase>(RunPhase.Warmup, (from, to) => to != RunPhase.Warmup);
fsm.StateChanged += (from, to) => Debug.Log($"{from} -> {to}");
fsm.TryEnter(RunPhase.Combat);
```

## 8. Custom Bootstrap Tasks
Add game-specific boot logic by implementing `IBootstrapTask`.

```csharp
using System.Threading;
using Cysharp.Threading.Tasks;
using Vareiko.Foundation.Bootstrap;
using Vareiko.Foundation.Save;

public sealed class LoadProfileBootstrapTask : IBootstrapTask
{
    private readonly ISaveService _save;

    public int Order => 100;
    public string Name => "Load Player Profile";

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

## 9. Monetization and Comms Stack

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

## 10. External Bridge Integrations

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

## 11. Observability and Diagnostics
Use:
- `IDiagnosticsService` for live snapshot access;
- `IDiagnosticsSnapshotExportService.ExportAsync(label)` for QA/support exports.

Monitor key signals in `SignalBus` for:
- startup validation;
- monetization decisions/failures;
- push permission/topic flow;
- attribution and ads bridge failures.

## 12. Testing Strategy
Recommended levels:
1. Runtime unit tests per module (service contracts + provider mapping).
2. Integration smoke tests for bootstrap + scene flow + save/settings.
3. PlayMode smoke for scene wiring and audio/UI critical path.

Use package test assembly `Tests/Runtime` as the default pattern for new modules.

## 13. Release Checklist
1. Run `Tools/Vareiko/Foundation/Validate Project`.
2. Ensure package version and changelog are aligned.
3. Fix all UI registry errors before release:
- duplicate non-empty UI ids
- empty `UIScreen` / `UIWindow` ids
- active scene template items in `UIItemCollectionBinder`
- non-empty ids on item templates
4. Review UI validator warnings:
- active `LayoutGroup` / `ContentSizeFitter` on runtime collection containers
- decorative `Graphic.raycastTarget` without a parent `Selectable`
5. Ensure no compile errors on clean import.
6. Ensure required runtime/editor tests pass.
7. Tag release (`vX.Y.Z`) after commit.

## 14. Common Pitfalls
- Missing provider define/sdks with production provider selected.
- Enabling consent-required modules without loaded consent state.
- Registering bridge handlers too late (after provider init).
- Skipping `FoundationSceneInstaller` in gameplay scenes.

If you follow the starter flow and keep provider-heavy modules in fallback mode first, integration is usually stable from day one.
