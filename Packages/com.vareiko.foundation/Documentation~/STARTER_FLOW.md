# Starter Flow (Fresh Unity Project)

This guide is the shortest path to a clean first run with `com.vareiko.foundation`.

## Goal
- Import the package into a new Unity project.
- Wire minimum contexts/installers.
- Reach predictable boot without production SDK dependencies.

## 1) Host Project Prerequisites
1. Unity `2022.3+`.
2. OpenUPM scoped registry for:
- `com.cysharp`
- `net.bobbo`
3. Install required dependencies:
- `com.cysharp.unitask`
- `net.bobbo.extenject`
- `com.unity.inputsystem`

## 2) Scene Wiring
1. Create `Bootstrap` scene:
- add `ProjectContext`
- add `FoundationProjectInstaller`
2. Create `Gameplay` scene:
- add `SceneContext`
- add `FoundationSceneInstaller`
3. Optional but recommended:
- add `LoadingOverlayPresenter` on a UI canvas in gameplay scene

## 3) Minimum Config Assignment
Assign these to `FoundationProjectInstaller` first:
1. `EnvironmentConfig` (`ApplyStarterPresets()` for `dev/stage/prod`).
2. `ObservabilityConfig`.
3. `SaveSecurityConfig`.
4. `AutosaveConfig`.

Keep provider-backed modules in safe mode for first launch:
1. backend provider -> `None`
2. IAP provider -> `None` or `Simulated`
3. ads provider -> `None` or `Simulated`
4. push provider -> `None` or `Simulated`
5. attribution provider -> `None`

## 4) First-Run Validation Checklist
1. Enter Play Mode.
2. Confirm no compile errors and no startup validation `Error`.
3. Confirm app state transitions from bootstrap to your target initial state.
4. Confirm fallback behavior:
- intentional bootstrap failure transitions to `AppState.Error`
5. Confirm diagnostics:
- `IFoundationDiagnosticsService.GetSnapshot()` returns non-null snapshot

## 5) Move to Vertical Slice
After first clean run:
1. Enable real backend (`PlayFab`) only when host SDK + defines are ready.
2. Enable real IAP/push adapters only when corresponding SDKs/defines are present.
3. Enable attribution bridge handlers only after host attribution SDK initialization.
4. Add feature modules via scaffolder:
- menu `Tools/Vareiko/Foundation/Create Runtime Module`
5. Add runtime tests for each newly enabled provider path.

## 6) Release Gate Before Tagging
1. Run `Tools/Vareiko/Foundation/Validate Project`.
2. Ensure:
- package/changelog versions match
- dependencies and `.meta` completeness checks are green
- no merge markers in release-critical roots
