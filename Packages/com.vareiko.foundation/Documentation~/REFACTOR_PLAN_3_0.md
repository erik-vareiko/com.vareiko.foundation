# Foundation v3.0 — Universal Level-0 Refactor Plan

> Status: planning (created 2026-06-08). Target: `com.vareiko.foundation` 3.0.0.
> Supersedes the v1.0/v2.0 roadmap in `BACKLOG.md` for the architecture direction.

## Goal

Bring the package back to its founding intent: a **universal infrastructure
"level-0"** that any Unity project pulls in to skip weeks of startup wiring.

It covers two layers:
1. **Fundamental systems** — app lifecycle, bootstrap, UI layers, configs, save,
   settings, localization, audio, observability, RNG, core data primitives.
2. **Monetization & analytics service abstractions** — interface-first seams into
   which a host project plugs concrete SDKs (PlayFab, Unity IAP, ad networks, …).

A host project should compile **only the modules it opts into** and reach a running,
validated boot in minutes.

## Locked decisions (2026-06-08)

| Topic | Decision | Rationale |
|---|---|---|
| DI container | **Migrate Zenject/Extenject → VContainer** | Extenject is a community fork of an abandoned project. VContainer is source-gen (no runtime reflection), maintained, lower GC. Cheapest to migrate now — only `chibi_arena` depends on it. |
| Messaging | **SignalBus → MessagePipe** | SignalBus is a Zenject construct; MessagePipe is the VContainer-era equivalent (`IPublisher<T>`/`ISubscriber<T>`). |
| Scope | **Infra + basic gameplay primitives** | Add pooling / tick / generic FSM / `Result<T>` to Core, not strictly infra-only. |
| Async | Keep **UniTask** | Best-in-class, no replacement. |
| Input | Keep **Unity Input System** | Already abstracted behind `IInputService`. |
| Configs | Keep **ScriptableObject** | Standard, consistent across modules. |
| Assets | Keep **Addressables** | Already behind `IAssetService` with Resources fallback. |
| Save serializer default | Swap **JsonUtility → Newtonsoft/System.Text.Json** | JsonUtility can't handle dictionaries/polymorphism/nullable. Keep `ISaveSerializer` seam. |
| Backend provider | Keep **PlayFab** as one impl behind `IBackendService` | Abstraction is correct; PlayFab is pluggable. |

## Modules to keep as-is (repackage only)

Save, Bootstrap, Config, Observability, RNG, Consent, Connectivity, Localization,
Settings, Audio, UI v2, and all monetization/analytics abstractions
(Ads / IAP / Push / Attribution / Analytics / Backend / FeatureFlags).
These are solid; they only move into their target assemblies.

## Gap list (prioritized)

### P0 — structural (blocks the "any project" goal)
- **G1. Split monolithic asmdef.** All ~296 files are in one `Vareiko.Foundation`
  assembly; a Save+UI host still compiles PlayFab/Ads/attribution. Split into
  `Foundation.Core` + opt-in modules with `versionDefines` for optional SDKs.
- **G2. Generic object pooling.** No general pool exists (only ad-hoc reuse in
  `UIRegistry`). Every game needs it (projectiles, UI items, VFX, audio sources).
- **G3. Central tick/update service.** Each service spins its own UniTask loop;
  the `Time` module is a thin `UnityEngine.Time` wrapper. Need `ITickService`
  with ordered update/pause + timers/deferred calls (coroutine replacement).

### P1 — reusability & cleanliness
- **G4. De-hardcode `AppState`.** `AppState` is a fixed enum with baked-in
  transition rules (`AppStateMachine.CanTransition`). Different games need
  different state sets. Provide a generic FSM utility + extensible app states.
- **G5. Canonical `Result<T>` / error primitive.** Each module reinvents its own
  (`AssetLoadResult`, `SaveMigrationResult`, `CloudFunctionResult`,
  `StartupValidationResult`, …). One Core type removes the duplication.
- **G6. Core utility layer.** Disposable helpers, id/guid generation, weak-event,
  collection/extension helpers.

### P2 — maturity
- **G7. Critical-path tests.** Bootstrap, Consent (privacy!), Installers have 0
  tests today while UI has 16.
- **G8. Vertical-slice sample.** Only `BasicSetup` exists; need a dogfood slice
  proving "level-0 boots a project in minutes."
- **G9. Save serializer default swap** (see decisions table).

## Phased execution

Sequencing rationale: the DI migration touches everything, so it needs a test
safety net first; the asmdef split lands after DI so install logic is rewritten
once, not twice.

### Phase 0 — Safety net (before any DI change)
Add tests to the untested critical path so the migration has a regression detector.
- Cover: `BootstrapRunner` (already had tests), `AppStateMachine`, `ConsentService`,
  composition/installer resolution.
- Closes G7.
- **Done when:** boot/consent/installer behaviour is asserted by tests that pass
  on the current Zenject build.

Status (2026-06-08): tests authored, pending a green run in Unity Test Runner.
- `Tests/Runtime/App/AppStateMachineTests.cs` — transition rules (no bus) + signal
  emission / boot-failed→Error / dispose (real `SignalBus`).
- `Tests/Runtime/Consent/ConsentServiceTests.cs` — state + save/load round-trip
  (fake `ISaveService`) + signal emission (real `SignalBus`).
- `Tests/Runtime/Composition/FoundationCompositionTests.cs` — **the DI parity spec**:
  `InstallProjectServices` installs all 28 modules without throwing and resolves the
  core service surface as singletons. Rewritten against VContainer in Phase 1; must
  stay green.
- Signal-emission tests use Zenject `SignalBus` and are the ones rewritten for
  MessagePipe; the transition/state tests are DI-agnostic and survive the migration.

### Phase 1 — DI migration (Zenject → VContainer, SignalBus → MessagePipe)
Done on the current monolith — a sweeping change is simpler in one assembly.
- `MonoInstaller` / `InstallBindings` → `LifetimeScope` / `Configure(IContainerBuilder)`.
- `[Inject]` constructors → VContainer constructor injection (most classes unchanged).
- `IInitializable` / `IDisposable` → VContainer entry points.
- `SignalBus.Fire/Subscribe` → MessagePipe `IPublisher<T>` / `ISubscriber<T>`
  (~42 services fire signals — introduce a thin adapter to avoid hand-editing each).
- `[InjectOptional]` → VContainer resolve-with-fallback pattern.
- **Done when:** Phase 0 tests pass on VContainer; sample scene boots; no Zenject
  references remain in Runtime/Editor asmdefs.

### Phase 2 — Asmdef modularization
- Create `Foundation.Core` + opt-in module assemblies
  (`Foundation.Save`, `Foundation.UI`, `Foundation.Monetization`,
  `Foundation.Backend.PlayFab`, …) with `versionDefines` for optional SDKs.
- Closes G1.
- **Done when:** a host can reference Core + a subset and compile without the rest.

Status (2026-06-12): **landed** as 10 runtime assemblies (decisions confirmed with
the owner: grouped granularity, Observability via direct refs):
- `Vareiko.Foundation.Core` — Signals facade, Time, Common, App+Bootstrap (cyclic
  pair), Config, Connectivity, Environment, Input, Loading, Rng, SceneFlow,
  Composition, Validation framework.
- `Vareiko.Foundation.Persistence` — Save+Settings+Consent (mutually cyclic by
  design: settings/consent persist through saves).
- `Vareiko.Foundation.Audio` (→ Persistence), `…UI` (incl. UINavigation),
  `…Assets` (versionDefines: `FOUNDATION_ADDRESSABLES` auto-set from
  `com.unity.addressables`; refs `Unity.Addressables`/`Unity.ResourceManager`,
  ignored when absent), `…Backend` (incl. CloudSaveSync + PlayFab stubs),
  `…Features` (→ Backend), `…Monetization` (Ads/Iap/Push/Attribution/Analytics/
  Economy/policy, → Persistence + Observability), `…Observability`
  (→ Assets + Backend).
- `Vareiko.Foundation` (umbrella) — `FoundationRuntimeInstaller`, the
  Project/Scene installers, cross-module startup rules; refs all modules.
  Hosts wanting a subset compose their own `LifetimeScope` from module
  installers (`RegisterSignals` + `Install`) instead of `InstallProjectServices`.
- Deviation from the sketch: no `Foundation.Backend.PlayFab` — the PlayFab
  "impl" is a stub behind `#if PLAYFAB_SDK` with no SDK reference today; split
  it out when a real SDK integration lands.
- Enablers landed earlier the same day: per-module `RegisterSignals` (the
  central broker registry would have made Core depend on every module) and the
  cross-module ownership moves (CloudSaveSync→Backend,
  MonetizationObservability→Monetization, startup rules→composition root).

### Phase 3 — Core primitives (extended scope)
- ~~`Result<T>` / error primitive; migrate ad-hoc result types onto it. (G5)~~ DONE
  2026-06-12: `Result`/`Result<T>` in `Vareiko.Foundation` (Core/Primitives) — the
  default for new APIs. Migration policy decided while landing: pure value+error
  results migrate (`DiagnosticsSnapshotExportResult` → `Result<string>` as the
  exemplar); domain results carrying codes/retry flags (Backend, CloudSaveSync,
  Push/Ads models) intentionally keep their shapes — forcing them into `Result<T>`
  would lose the domain fields or spawn a parallel error model.
- ~~Generic object pool (`IPool<T>`, prefab pool, auto-return). (G2)~~ DONE 2026-06-12:
  `Vareiko.Foundation.Pooling` in Core — `IObjectPool<T>`, `ObjectPool<T>` (callbacks,
  maxSize, prewarm, double-release detection), `ComponentPool<T>` (prefab lifecycle,
  overflow destruction), `GetScoped` auto-return. Covered by EditMode tests.
- ~~`ITickService` — ordered update/pause + timers/deferred calls; upgrade `Time`. (G3)~~
  DONE 2026-06-12: `TickService` in `Vareiko.Foundation.Time` — ordered listeners,
  `Delay`/`Repeat`/`NextFrame` (scaled/unscaled), pause, per-listener exception
  isolation; container player-loop entry point in play mode, manual `Advance` in
  tests. Registered in `FoundationTimeInstaller`; part of the composition spec.
- ~~Generic FSM + extensible app states replacing the hardcoded `AppState` enum. (G4)~~
  DONE 2026-06-12: `StateMachine<TState>` in Core/Primitives (guard + event +
  comparer); `AppState` converted enum → string-backed struct with the same
  well-known names (call sites compile unchanged — verified no `switch`/casts
  existed), so hosts mint custom states with `new AppState("X")`;
  `AppStateMachine` rebuilt on the generic FSM with the original lifecycle rules.
- ~~Core utility layer. (G6)~~ PARTIAL 2026-06-12: `DisposableAction` +
  `CompositeDisposable` landed (the highest-value pieces — the subscription-bag
  pattern every service hand-rolls). Id/guid helpers, weak events, collection
  extensions deferred until a concrete consumer needs them (avoid speculative API).

### Phase 4 — Maturity
- ~~Vertical-slice sample (dogfood). (G8)~~ DONE 2026-06-12:
  `Samples~/VerticalSlice` — one component in an empty scene boots the full
  composition: bootstrap task → custom `AppState("Run")` → `ITickService` +
  `ComponentPool` gameplay → dictionary-bearing profile autosaved through the
  Newtonsoft serializer. Compile-verified against the package (samples are not
  CI-compiled — checked by temporarily copying into `Assets/`). Both samples now
  declared in `package.json` `samples`.
- ~~Swap `JsonUtility` default in `ISaveSerializer`. (G9)~~ DONE 2026-06-12:
  `NewtonsoftJsonSaveSerializer` (com.unity.nuget.newtonsoft-json 3.2.1) is the
  default behind `SecureSaveSerializer`; same `{"Value": ...}` envelope as the
  JsonUtility serializer so pre-3.0 saves keep loading (cross-compat covered by
  tests both directions). `JsonUnitySaveSerializer` remains as fallback;
  `SecureSaveSerializer` now composes over `ISaveSerializer`.
- ~~Backfill tests for new Core modules.~~ Covered as the primitives landed
  (Phase 3 shipped with 50 tests: pooling, tick, result, FSM, disposables).

## Out of scope (explicitly)
- Gameplay frameworks (entity/component models, gameplay loops) beyond the generic
  primitives above.
- Tween/juice/feedback layers.
- Replacing UniTask, Input System, Addressables, or ScriptableObject configs.

## Tracking
Update this file's phase "Done when" checkboxes as work lands. Reflect shipped
phases in `CHANGELOG.md` and bump `package.json` toward `3.0.0`.
