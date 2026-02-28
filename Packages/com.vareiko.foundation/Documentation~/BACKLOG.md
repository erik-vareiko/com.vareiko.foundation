# Foundation Backlog

## v0.4 Goal
Production-ready "new project starter" package:
- predictable startup and failure behavior;
- safe default data/asset lifecycle;
- testable and template-friendly architecture;
- minimum host-project wiring.

## Roadmap (`v0.4 -> v1.0`)
1. P0 - Core completion (`v0.4`):
- New Input System adapter + persistent rebind storage.
- Release docs closure (`README`, `ARCHITECTURE`, sample flow).
- Startup validation expansion for production baseline checks.
2. P0 - Backend production readiness (`v0.5` target):
- PlayFab adapter hardening (auth/session/error mapping).
- Cloud save sync with conflict strategies.
- Remote config cache invalidation + forced refresh paths.
3. P1 - Observability and operations (`v0.6` target):
- Structured logging sink abstraction (console/file/http).
- Optional crash-reporting adapter integration.
- Diagnostics snapshot export path for QA/support.
4. P1 - Developer experience and template (`v0.7` target):
- Scaffolder upgrades (module + tests + integration sample).
- Project validator release gate checks.
- Starter template profile presets (`dev/stage/prod`).
5. P2 - Monetization/comms layer (`v1.0` target):
- IAP abstraction + provider baseline.
- Ads abstraction (`rewarded/interstitial`) behind consent.
- Push notification abstraction baseline.

## Progress
1. Completed in `0.3.5` (Milestone A baseline):
- Environment profile module (`EnvironmentConfig`, `IEnvironmentService`, installer + signals).
- Global unhandled exception boundary (`GlobalExceptionHandler`, observability signal).
- Runtime fallback to `AppState.Error` on boot failure/unhandled exception.
2. Completed in `0.3.6` (Milestone B baseline):
- Save hardening: atomic writes + rolling backups + restore fallback.
- Autosave scheduler for `Settings` and `Consent` with interval/pause/quit triggers.
- Asset lifecycle baseline: reference counting, release tracking and leak diagnostics signals.
3. Completed in `0.3.7` (Milestone C baseline):
- Runtime package test assembly and deterministic test doubles.
- Smoke coverage for boot/save/loading/feature flags/assets plus scene flow contract checks.
4. Completed in `0.3.8` (Milestone D partial):
- Localization baseline module (`LocalizationConfig`, `ILocalizationService`, installer + tests).
- Editor scaffolding tool for fast runtime module generation from templates.
5. Completed in `0.3.9`:
- Unified `UI` naming across primary runtime contracts (`IUIService`, `IUINavigationService`, installers/signals).
- Added shared UI primitives (`UIElement`, `UIWindow`, `UIPanel`, `UIItemView`, `UIButtonView`) and `UIRegistry`.
- Added queue-based window orchestration layer (`IUIWindowManager` / `UIWindowManager`) with runtime tests.
6. Completed in `0.3.28` (v0.4 closure progress):
- New Input System baseline with persistent rebind storage (`NewInputSystemAdapter`, `IInputRebindService`, `IInputRebindStorage`) + runtime tests.
- Startup validation expansion baseline: built-in rules for save security, backend config and observability config.
- Remote config cache hardening: explicit cache invalidation + forced refresh path in cached remote-config service.
- PlayFab backend hardening baseline: stricter config/input validation, auth-state normalization and backend error-code mapping.
7. Completed in `0.3.29` (backend readiness increment):
- Cloud save sync baseline: push/pull/sync orchestration with resolver-based conflict handling over backend player data.
8. Completed in `0.3.30` (observability increment):
- Structured logging sink baseline: `FoundationLogEntry`, `IFoundationLogSink`, default `UnityConsoleLogSink` binding and logger sink fan-out.
9. Completed in `0.3.31` (observability increment):
- Optional crash-reporting integration baseline: `ICrashReportingService`, `FoundationCrashReport`, crash-report submission/failure signals and unhandled-exception forwarding gate.
10. Completed in `0.3.32` (observability increment):
- Diagnostics snapshot export baseline: `IDiagnosticsSnapshotExportService`, local file export path, and export success/failure signals.
11. Completed in `0.3.33` (developer experience increment):
- Scaffolder upgrade baseline: optional test stub and integration sample installer generation.
12. Completed in `0.3.34` (developer experience increment):
- Project validator release-gate baseline: version/dependency/meta/merge-marker/Unity-version checks in editor validation flow.

## v0.4 Scope
### P0 (must have)
1. Environment/Profile module (`dev`, `stage`, `prod`) with runtime access.
2. Global runtime exception handling and fallback error flow.
3. Save hardening: atomic writes + rolling backups + autosave scheduler.
4. Addressables lifecycle: reference counting and leak diagnostics baseline.
5. Smoke/integration tests for boot, save/settings, scene flow, loading, feature flags.
6. Module/template tooling for rapid feature scaffolding.

### P1 (should have)
1. Input System adapter with persistent rebind storage.
2. Structured logging sink abstraction (file/http adapters optional).

### Out of scope for v0.4
1. PlayFab full production adapter.
2. IAP/Ads/Push/Attribution.
3. A/B experimentation platform and analytics governance toolkit.

## Execution Order
1. Milestone A: startup reliability and environments.
2. Milestone B: save safety and asset lifecycle.
3. Milestone C: tests and diagnostics closure.
4. Milestone D: scaffolding/tooling and docs release.

## Milestone A (P0): Startup Reliability
1. Add environment module.
File targets:
`Runtime/Environment/IEnvironmentService.cs`
`Runtime/Environment/EnvironmentService.cs`
`Runtime/Environment/EnvironmentConfig.cs`
`Runtime/Environment/FoundationEnvironmentInstaller.cs`
`Runtime/Environment/EnvironmentSignals.cs`
`Runtime/FoundationRuntimeInstaller.cs` (install order)
`Runtime/Installers/FoundationProjectInstaller.cs` (config injection)
2. Add global exception boundary.
File targets:
`Runtime/Observability/GlobalExceptionHandler.cs`
`Runtime/Observability/ObservabilitySignals.cs` (new exception signal)
`Runtime/Observability/FoundationObservabilityInstaller.cs`
3. Add fallback boot failure strategy.
File targets:
`Runtime/Bootstrap/BootstrapRunner.cs`
`Runtime/App/AppStateMachine.cs` (error transition policy)

## Milestone B (P0): Save + Assets Hardening
1. Atomic save writes and backups.
File targets:
`Runtime/Save/FileSaveStorage.cs` (write temp + replace)
`Runtime/Save/SaveService.cs` (backup generation/restore path)
`Runtime/Save/SaveSecurityConfig.cs` (backup/security options)
2. Autosave orchestration.
File targets:
`Runtime/Save/AutosaveService.cs` (new)
`Runtime/Save/AutosaveConfig.cs` (new)
`Runtime/Save/FoundationSaveInstaller.cs`
3. Addressables reference tracking.
File targets:
`Runtime/AssetManagement/AddressablesAssetProvider.cs`
`Runtime/AssetManagement/AssetService.cs`
`Runtime/AssetManagement/AssetSignals.cs` (refcount/leak signals)
`Runtime/Observability/DiagnosticsSnapshot.cs` (asset counters)

## Milestone C (P0): Test Baseline
1. Add package tests structure.
File targets:
`Tests/Runtime/Vareiko.Foundation.Tests.asmdef` (new)
`Tests/Runtime/Boot/BootstrapRunnerTests.cs` (new)
`Tests/Runtime/Save/SaveServiceTests.cs` (new)
`Tests/Runtime/SceneFlow/SceneFlowServiceTests.cs` (new)
`Tests/Runtime/Loading/LoadingServiceTests.cs` (new)
`Tests/Runtime/Features/FeatureFlagServiceTests.cs` (new)
2. Add deterministic test doubles for time/connectivity/storage.
File targets:
`Tests/Runtime/TestDoubles/FakeTimeProvider.cs` (new)
`Tests/Runtime/TestDoubles/InMemorySaveStorage.cs` (new)
`Tests/Runtime/TestDoubles/FakeConnectivityService.cs` (new)

## Milestone D (P0/P1): Tooling + Docs
1. Feature module scaffolding tool. [DONE in 0.3.8]
File targets:
`Editor/Scaffolding/FoundationModuleScaffolder.cs` (new)
`Editor/Scaffolding/Templates/*` (new)
2. Localization baseline (P1). [DONE in 0.3.8]
File targets:
`Runtime/Localization/ILocalizationService.cs` (new)
`Runtime/Localization/LocalizationService.cs` (new)
`Runtime/Localization/LocalizationConfig.cs` (new)
`Runtime/Localization/FoundationLocalizationInstaller.cs` (new)
3. Input System adapter (P1). [DONE in 0.3.28]
File targets:
`Runtime/Input/NewInputSystemAdapter.cs` (new)
`Runtime/Input/InputRebindStorage.cs` (new)
`Runtime/Input/FoundationInputInstaller.cs`
4. Release docs update. [IN PROGRESS]
File targets:
`README.md`
`Documentation~/ARCHITECTURE.md`
`Documentation~/BACKLOG.md`
`CHANGELOG.md`

## Definition of Done for v0.4
1. Clean import in fresh project with only OpenUPM dependencies configured.
2. All P0 tests pass in Unity Test Runner (EditMode + Runtime-focused smoke tests).
3. Boot failure path is observable and transitions to error state predictably.
4. Save data survives interruption scenarios (atomic write + backup restore).
5. Template tool can generate a new runtime module skeleton in under 1 minute.
