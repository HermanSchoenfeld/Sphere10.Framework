---
name: user-settings
description: Persist user preferences and UI state between application sessions with SettingsObject, UserSettings, and settings.Save(). Use for remembered form bounds and monitors, page sizes, filters, recent choices, or other per-user state in Sphere10 applications.
---

# User settings

Use the application settings provider for preferences that survive application restarts. Follow [code-style](../code-style/SKILL.md) for code changes and [winforms-ui](../winforms-ui/SKILL.md) for form lifecycle integration.

## Existing contract

- Define a public `SettingsObject` subclass with a public parameterless constructor, serializable properties, and useful defaults. Use a feature-specific name such as `EmployeeSearchSettings`; the default file provider uses the short type name, so unrelated nested classes named `FormSettings` can collide.
- Retrieve it with `UserSettings.Get<EmployeeSearchSettings>()`. An optional stable ID separates instances, for example `UserSettings.Get<EmployeeSearchSettings>("ArchivedEmployees")`.
- Copy accepted UI values into the returned object, then call **`Settings.Save()`**. There is no static `UserSettings.Save()` method. A newly constructed settings object has no provider; retrieve it through `Get<T>()` before saving.
- `Get<T>()` returns stored settings or a new object with defaults. The default provider does not automatically save new objects. It caches objects by type and ID, so modifying them can affect other consumers even before saving; keep provisional dialog edits in controls or a separate model until accepted.
- Use `GlobalSettings` only for values deliberately shared between system users. Window placement, personal filters, and recent selections belong in `UserSettings`.

```csharp
using Sphere10.Framework.Application;

public class EmployeeSearchSettings : SettingsObject {
	public int PageSize { get; set; } = 100;
	public string SearchText { get; set; } = string.Empty;
}
```

Restore after controls and application settings services are initialized:

```csharp
var Settings = UserSettings.Get<EmployeeSearchSettings>();
_pageSize.Value = Tools.Values.ClipValue(Settings.PageSize, 1, 9999);
_searchBox.Text = Settings.SearchText ?? string.Empty;
```

Persist when the user applies the preference or an accepted close completes:

```csharp
var Settings = UserSettings.Get<EmployeeSearchSettings>();
Settings.PageSize = (int)_pageSize.Value;
Settings.SearchText = _searchBox.Text;
Settings.Save();
```

Use the control's actual supported range.

## Lifetime and storage

The application module registers `Local<ISettingsProvider>` as a `CachedSettingsProvider` around `DirectoryFileSettingsProvider`, then assigns it to `UserSettings.Provider` during initialization. Normal framework applications should use that setup. Do not replace the provider in individual forms or read settings before application initialization.

The default user directory is `{UserDataDir}/{ProductName}`. The file provider writes JSON to `.setting` files and can read legacy XML. Public settings properties are serialized; `SettingsObject.Provider` is excluded. Persist values and stable identifiers, not controls, services, live data sources, or an entire entity graph. Keep existing setting names and IDs stable; plan a migration when changing them.

Restore once after controls exist and before the user starts interacting. Validate persisted numbers, enums, and selections against current constraints; defaults must work on first launch and when older files lack newly added properties. For optional UI preferences, catch load/save errors at the UI boundary, log with `SystemLog.Exception`, and keep the form usable. Avoid swallowing failures for required application configuration.

Save when the preference is accepted. Persist main-window placement through a framework shutdown task: capture placement in memory after exit permissions and native closing observers approve shutdown, then call `Settings.Save()` during `IApplicationFinalizer.Finalize()` while the main window is alive and before settings providers are disposed. Completed closure remains the capture boundary for standalone persistence scopes. A cancelled close must not queue a save. Explicit Apply/Save actions may persist sooner. Move, resize, splitter, and keystroke handlers must not serialize or write preferences; keep any needed tracking lightweight and in memory.

## Main-window placement

Use the framework's `.UseMainFormSettings()` on the WinForms application builder to enable main-window persistence. Keep the opt-in at application startup. It restores the configured main form on load and registers `MainFormSettingsFinalizer` to capture approved placement before shutdown tasks and write it once during shutdown. The later window closure must not queue another save. For `BlockMainForm`, it also stores the left navigation pane's width in the same `FormWindowSettings`, including the remembered expanded width while hidden.

Remember normal bounds as well as the monitor and maximized state. Use restore bounds when maximized, never reopen minimized, and fit restored bounds into an available monitor's working area. Account for a disconnected monitor, changed resolution or DPI, and the form's minimum size. Use `Tools.WinForms.AutoPersistWindowSettings(Form, SettingsID)` in a scope for other forms, or `CaptureWindowSettings` and `RestoreWindowSettings` for explicit lifecycle integration. `FormWindowSettings.NavigationPaneWidth` stores the optional menu width in device pixels at the saved `Dpi`; restore scales it for the current display and clamps it through `BlockMainForm.NavigationPaneWidth`. `MaximumNavigationPaneWidth` is configured in logical pixels at 96 DPI and defaults to 480; current content space can impose a smaller limit. The standalone `AutoPersistWindowSettings` helper saves once on accepted closure; use the builder option for main forms so the write belongs to framework shutdown.

## Verification and source references

Test persistence with a temporary directory and a **fresh provider** for the next-session read, so a cache hit cannot mask a missing disk write. Test defaults, optional IDs, invalid/old values, and accepted versus cancelled UI actions. For main-window settings, verify zero writes during changes and approved placement capture, exactly one at finalization while the window is alive, no second save after closure, and saving before provider disposal. Isolate and restore any replaced static provider; such tests must not run in parallel with other users of it.

Inspect these current implementations before extending their behavior:

- [UserSettings](../../../src/Sphere10.Framework.Application/Settings/UserSettings.cs), [SettingsObject](../../../src/Sphere10.Framework.Application/Settings/SettingsObject.cs), and [ISettingsProvider](../../../src/Sphere10.Framework.Application/Settings/ISettingsProvider.cs).
- [Application module registration](../../../src/Sphere10.Framework.Application/ModuleConfiguration.cs).
- [DirectoryFileSettingsProvider](../../../src/Sphere10.Framework.Application/Settings/DirectoryFileSettingsProvider.cs) and [CachedSettingsProvider](../../../src/Sphere10.Framework.Application/Settings/CachedSettingsProvider.cs).
- [MainFormSettingsFinalizer](../../../src/Sphere10.Framework.Windows.Forms/Application/Components/MainFormSettingsFinalizer.cs) and [window tracking helpers](../../../src/Sphere10.Framework.Windows.Forms/WindowSettingsTool.cs).
- [WinForms usage](../../../src/Sphere10.Framework.Windows.Forms/README.md).

This follows the existing BlockchainSQL `DiagnosticForm`, `BlockFileImporterForm`, and `NetworkBlockImporterForm` load/copy/save pattern. EquipmentWatch's current `DatabaseConfigurationForm` uses the same settings abstraction through `GlobalSettings` for shared database configuration; choose the scope according to the value being persisted.
