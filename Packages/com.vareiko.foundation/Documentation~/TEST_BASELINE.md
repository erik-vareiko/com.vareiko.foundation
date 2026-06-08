# Test Baseline (frozen 2026-06-08)

Snapshot of the EditMode test suite **before** the Phase 1 DI migration, so the
migration can be judged against a known state ("same 27 failing, no new ones"),
not an imagined all-green suite.

Run: `dash-survival` host project, Unity 6.3 LTS (6000.3.10f1), EditMode Run All.
Result: **212 passed, 27 failed.**

These 27 failures are **pre-existing** and unrelated to the migration. Proof:
the migration is purely additive (new files + 2 asmdef references); UniTask stays
`2.5.10`; Zenject `SignalBus`-based tests (incl. the new App/Consent signal tests)
pass; every failure's root cause is an EditMode/environment/assertion issue, not
DI/messaging.

## Failures by root cause

### A. Validator needs an active scene (5)
`Invalid SceneManagerSetup: No loaded scene found.` Unity 6 EditMode leaves 0 active
scenes after `EditorSceneManager.NewScene` in SetUp.
- `EditorTests.FoundationProjectValidatorTests.*` (all 5)

### B. Exact exception-type mismatch (3)
Assert `OperationCanceledException` but UniTask throws `TaskCanceledException` (a
subclass); the matcher is exact-type. Fix = `Throws.InstanceOf` / `Assert.CatchAsync`.
- `AssetManagement.AddressablesAssetProviderTests.ReleaseAsync_WithCanceledToken_Throws`
- `Backend.PlayFabServicesSmokeTests.PlayFabServices_WithCanceledToken_Throw`
- `Config.ConfigRegistryTests.ExecuteAsync_WithCanceledToken_Throws`

### C. Unhandled-log strictness (3)
Code logs an exception/error by design; NUnit fails without `LogAssert.Expect`.
Behaviour is correct; the test just doesn't declare the expected log.
- `Boot.BootstrapRunnerTests.Initialize_WhenTaskThrows_TransitionsToErrorAndStopsPipeline`
- `Common.HealthCheckRunnerTests.RunAsync_ProcessesChecks_AndEmitsSignals`
- `Validation.StartupValidationRunnerTests.Initialize_EmitsCompletedSummary_WithWarningAndErrorCounts`

### D. Runtime-only Unity API in EditMode (2)
`DontDestroyOnLoad` / `Destroy` are play-mode only.
- `Audio.AudioServiceTests.Setters_ClampValues_AndEmitSignals`
- `UI.UIItemCollectionBinderTests.SetCount_WhenDestroyOnShrinkEnabled_RemovesExtraItems`

### E. UGUI events / reactive bindings need PlayMode (14)
"Expected 1 But was 0" — UGUI click/event and value propagation don't run in EditMode.
- `Audio.AudioServiceTests.Initialize_UsesSettingsVolumes_AndEmitsInitialSignal`
- `Input.InputRebindServiceTests.ApplyImportReset_PersistsThroughStorage`
- `UI.UIBoolButtonInteractableBinderTests.ReactiveValue_UpdatesButtonInteractable`
- `UI.UIButtonViewTests.EnableDisable_DoesNotDuplicateUguiButtonSubscription`
- `UI.UIButtonViewTests.UguiButtonClick_InvokesClickedEvent`
- `UI.UIConfirmDialogPresenterTests.Apply_UpdatesTexts_AndButtonsResolveWithPayloads`
- `UI.UIItemCountBinderTests.ReactiveValue_UpdatesCollectionCount`
- `UI.UIValueBinderLifecycleTests.SignalBusFallbackPath_UpdatesTargetsAndUnsubscribesCleanly`
- `UI.UIValueBinderLifecycleTests.ValueServicePath_DoesNotTouchSignalBusUnsubscribe`
- `UI.UIWindowButtonActionsTests.CloseAction_Click_ClosesCurrentByDefault`
- `UI.UIWindowButtonActionsTests.CloseAction_WhenSpecificWindowEnabled_ClosesById`
- `UI.UIWindowButtonActionsTests.OpenAction_Click_EnqueuesWindow`
- `UI.UIWindowResolveButtonActionTests.Click_DefaultConfig_ResolvesCurrent`
- `UI.UIWindowResolveButtonActionTests.Click_SpecificWindow_ResolvesById`

## Migration gate
After Phase 1b/1c, EditMode Run All must show **the same 27 failures (same names) or
fewer** — any *new* failure is a migration regression. The green subset, especially
`FoundationCompositionTests` (rewritten for VContainer), is the parity net.

Categories B and C are cheap to fix and tracked separately from the migration.
