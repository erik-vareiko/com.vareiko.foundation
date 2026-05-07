# Migration Guide 2.0

`com.vareiko.foundation` 2.0 removes legacy UI bridge APIs and makes UI validation stricter. This is a breaking release.

## API Mapping

| Removed API | Replacement |
|---|---|
| `UIScreenRegistry` | `UIRegistry` |
| `IUiService` | `IUIService` |
| `UiService` | `UIService` |
| `IUiNavigationService` | `IUINavigationService` |
| `UiNavigationService` | `UINavigationService` |
| `UiReadySignal` | `UIReadySignal` |
| `UiScreenShownSignal` | `UIScreenShownSignal` |
| `UiScreenHiddenSignal` | `UIScreenHiddenSignal` |
| `UiNavigationChangedSignal` | `UINavigationChangedSignal` |
| `FoundationUiInstaller` | `FoundationUIInstaller` |
| `FoundationUiNavigationInstaller` | `FoundationUINavigationInstaller` |

## Required Project Changes

1. Replace every `UIScreenRegistry` component with `UIRegistry`.
2. Replace lowercase `Ui*` service and signal references with uppercase `UI*` / `UINavigation*` equivalents.
3. Ensure every `UIScreen` and `UIWindow` has a non-empty id.
4. Ensure all non-empty UI ids are unique inside a scene registry.
5. Remove ids from `UIItemCollectionBinder` scene template items.
6. Hide scene template items before entering Play Mode.
7. Disable runtime `LayoutGroup` / `ContentSizeFitter` on hot collection containers after layout setup.
8. Disable `Graphic.raycastTarget` on decorative graphics that are not under a `Selectable`.

## Validation

Run `Tools/Vareiko/Foundation/Validate Project` after migration. Errors must be fixed before release. Warnings should be reviewed and either fixed or intentionally accepted by the host project.
