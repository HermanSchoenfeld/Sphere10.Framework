# Per-user form and control settings

Attach ordinary settings objects to screen registrations for integrated Options; use the existing
settings provider for independent controls and window placement. Follow
[Code Styling](../code-styling.md) and the
[Screen Options guide](../../usage/screen-options.md) for the shared catalog,
native/web bindings, detached editors and ordered accepted saves.

## Choose the owning profile

Declare screen preferences through the block's `AddScreen` callback with
`UseSettings<TSettings>()`, `UseSettings(instance)` or `UseSettings(() => new Preferences(...))`;
use `AddSettings` for application-root preferences. Screen settings need no base
class: editors edit the supplied ordinary values. Standard validation attributes
and `IValidatableObject` are supported; existing `SettingsObject.Validate()` still
applies when that base is used. Factories create settings values, never screens.
Mark a settings class `[UserEditable]` for automatic Options editing. Its public
properties remain editable unless `[ReadOnly(true)]` or `[Browsable(false)]`
restricts their presentation; those attributes do not affect persistence.
Unmarked values may still use an explicit custom editor and the same storage.
Instance defaults are copied at declaration; factory defaults are fresh for Reset.
The application enumerates unopened
and no-options registrations without constructing operational UI. Platform startup
restores profiles before default/permanent screen work. Native screens use
`GetSettings<TSettings>()` and `BindGridPreferencesAsync`; web screens use the
corresponding typed access and awaited preference callbacks.

Keep one owner per preference. Bind screen-owned grids to a named plain
`VisualGridSettings` (native) or `BlazorGridSettings` (web) section, suppressing
their independent record. The
same grid capture/apply path still serves standalone controls. Preserve window
placement through the existing main-form finalizer rather than moving it into
an unrelated screen profile. Web catalogs/stores belong to the circuit and its
authenticated subject or anonymous browser profile; never use native static
`UserSettings` as shared mutable web-user state.

Editors and screens work on detached copies. Capture on the UI/renderer owner,
serialize/persist exclusive snapshots off that owner, and publish only after a
successful save. Use the profile's ordered capture/version mechanism to prevent
stale instances or older pending writes replacing accepted values. Failed saves
remain visible and retryable. Hide, vetoed close and unchanged disposal do not
save; accepted closure and approved finalization await pending saves before the
provider is disposed.

Standard dialogs and screen helpers handle detached drafts, Reset, validation and
ordered persistence. Application code need not construct profiles or edit
sessions to declare or edit its settings.

## Standalone provider contract

- Define a public `SettingsObject` subclass with a public parameterless constructor, serializable properties, and useful defaults. Use a feature-specific name such as `EmployeeSearchSettings`; the default file provider uses the short type name, so unrelated nested classes named `FormSettings` can collide.
- Retrieve it with `UserSettings.Get<EmployeeSearchSettings>()`. An optional stable ID separates instances, for example `UserSettings.Get<EmployeeSearchSettings>("ArchivedEmployees")`.
- The returned object's instance `Save()` uses its bound provider; there is no static `UserSettings.Save()`. For asynchronous UI persistence, save a detached snapshot through the captured provider's `SaveSetting` rather than mutating/encrypting a live cached object.
- `Get<T>()` returns stored settings or a new object with defaults. The default provider does not automatically save new objects. It caches objects by type and ID, so modifying them can affect other consumers even before saving; keep provisional dialog edits in controls or a separate model until accepted.
- Use `GlobalSettings` only for values deliberately shared between system users. Window placement, personal filters, and recent selections belong in `UserSettings`.

```csharp
using Sphere10.Framework;
using Sphere10.Framework.Application;

public class EmployeeSearchSettings : SettingsObject {
	public int PageSize { get; set; } = 100;
	public string SearchText { get; set; } = string.Empty;
}
```

For standalone controls, capture the initialized provider and restore away from
the UI thread; then apply the returned values on the owner:

```csharp
var provider = UserSettings.Provider;
var settings = await Task.Run(() => provider.Get<EmployeeSearchSettings>());
_pageSize.Value = Tools.Values.ClipValue(settings.PageSize, 1, 9999);
_searchBox.Text = settings.SearchText ?? string.Empty;
```

Capture a detached snapshot when the preference is accepted. Serialize/save it
away from the UI thread through the same captured provider; order repeated saves
and keep provisional edits out of its cached object:

```csharp
var snapshot = new EmployeeSearchSettings {
	PageSize = (int)_pageSize.Value,
	SearchText = _searchBox.Text
};
snapshot.Validate().ThrowOnFailure();
await Task.Run(() => provider.SaveSetting(snapshot));
```

Use the control's actual supported range.

## Lifetime and storage

The application module registers `Local<ISettingsProvider>` as a `CachedSettingsProvider` around `DirectoryFileSettingsProvider`, then assigns it to `UserSettings.Provider` during framework module initialization. Normal framework applications should use that setup. Do not replace the provider in individual forms or read settings before that assignment. `StartWinFormsApplication` starts the framework before the root's application tasks and main-window display, so those consumers use the established provider.

The default user directory is `{UserDataDir}/{ProductName}`. The file provider writes JSON to `.setting` files and can read legacy XML. Public settings properties are serialized; `SettingsObject.Provider` is excluded. Persist values and stable identifiers, not controls, services, live data sources, or an entire entity graph. Keep existing setting names and IDs stable; plan a migration when changing them.

Restore once after controls exist and before the user starts interacting. Validate persisted numbers, enums, and selections against current constraints; defaults must work on first launch and when older files lack newly added properties. For optional UI preferences, catch load/save errors at the UI boundary, log through the owning `IApplication.Logger` when available (or `SystemLog.Exception` for a standalone form), and keep the form usable. Avoid swallowing failures for required application configuration.

Save when the preference is accepted. Persist main-window placement through the existing framework shutdown task: capture placement in memory after exit permissions and native closing observers approve shutdown, then call the settings instance's `Save()` during `MainFormSettingsFinalizer.Finalize()` while the main window is alive and before service-provider disposal. Completed closure remains the capture boundary for standalone persistence scopes. A cancelled close must not queue a save. Explicit Apply/Save actions may persist sooner. Move, resize, splitter, and keystroke handlers must not serialize or write preferences; keep any needed tracking lightweight and in memory. Custom lifecycle tasks use the existing sync/async directional bases; see [the sync/async guide](../../../docs/guidelines/patterns/sync-async.md#synchronous-and-asynchronous-implementation-hierarchies).

## Main-window placement

Use the framework's `.UseMainFormSettings()` on the WinForms application builder to enable main-window persistence. Keep the opt-in at application startup. It restores the configured main form on load and registers `MainFormSettingsFinalizer` to capture approved placement before shutdown tasks and write it once during shutdown. The later window closure must not queue another save. For `BlockMainForm`, it also stores the left navigation pane's width in the same `FormWindowSettings`, including the remembered expanded width while hidden.

Remember normal bounds as well as the monitor and maximized state. Use restore bounds when maximized, never reopen minimized, and fit restored bounds into an available monitor's working area. Account for a disconnected monitor, changed resolution or DPI, and the form's minimum size. Use `Tools.WinForms.AutoPersistWindowSettings(Form, SettingsID)` in a scope for other forms, or `CaptureWindowSettings` and `RestoreWindowSettings` for explicit lifecycle integration. `FormWindowSettings.NavigationPaneWidth` stores the optional preferred menu width in device pixels at the saved `Dpi`; restore scales it for the current display and clamps it against the configured `BlockMainForm.MaximumNavigationPaneWidth`. That maximum is configured in logical pixels at 96 DPI and defaults to 480. Main-frame resizing preserves this width and gives the remaining space to the content pane. Only physically insufficient native panel bounds can constrain its visible width without replacing the stored preference: widening the frame restores it, while an explicit divider move or width assignment selects a new preference. The standalone `AutoPersistWindowSettings` helper saves once on accepted closure; use the builder option for main forms so the write belongs to framework shutdown.

## Reusable grid preferences

[VisualGridSettings](../../../src/Sphere10.Framework.Windows.Forms/VisualGrid/VisualGridSettings.cs)
is a settings data object for one logical grid. It stores nullable `PageMode`, the
manual `PageSize`, zero-based Paged/Auto `PageNumber`, `SortColumnName` and
`SortDirection`. It contains values rather than controls, row data or a live
provider graph. [VisualGrid](../../../src/Sphere10.Framework.Windows.Forms/VisualGrid/VisualGrid.cs)
uses the same `Get<T>(id)` / instance `Save()` pattern as form preferences.

`VisualGrid.PersistSettings` defaults to true. `SettingsKey` supplies an explicit
stable ID when needed. Otherwise the control uses the nearest concrete hosted
screen or user-control type plus its named child path, with a form type/name
fallback. The ID must remain stable across docking, captions, localization and
application sessions; different logical grids need different IDs. Dynamic or
unnamed owners should supply an explicit key. Transient reference pickers set
`PersistSettings = false`.

Restore after source/column bindings and the user provider are available,
before querying the first page. If there is no saved file, retain the actual
control/consumer defaults rather than overriding them with an unrelated
settings default. Clamp old page sizes and page numbers and validate sort names
against the current column `SortName` values. Renamed/removed columns fall back
to the configured source ordering.

Save accepted page-size, page-navigation, sort and page-mode changes.
The footer's compact grid-icon menu selects **Single**, **Auto** or **Manual**,
with a checkmark on the current mode. Manual retains the `Paged` API value and shows the
manual size and navigator; Auto hides the manual size and retains the navigator;
Single shows all matching rows on page zero and hides both. The remembered manual
value is restored on returning to Manual and is stored in `VisualGridSettings.PageSize`.
Navigation in Auto is an accepted preference; the source clamps the restored page
when its data or fitted capacity changes. A null or invalid saved `PageMode` falls
back to the retained legacy `AutoPageSize` flag without writing during restore.
Automatic fitted row counts depend on the current viewport and must not replace
the remembered manual size. Resize/font/DPI changes, source refreshes and
normalizing old values do not authorize preference writes. Because a cached
settings object can be shared by instances with the same ID, unchanged disposal
must not overwrite another instance's accepted preferences. Track accepted dirty
state and retry failed writes through the original provider-bound object.

Consumers with a versioned preference migration can override
`VisualGrid.UpgradeGridSettings(VisualGridSettings)`. It receives a detached value
copy for either stored preferences or fresh control defaults, before the first
page query. Use `ConsumerVersion` for the consumer's migration version. The hook
must not perform IO. Its upgraded values and version are saved together with the
next accepted preference change; restoration and unchanged disposal do not write
or mutate the provider's cached payload. This allows a one-time default migration
while respecting subsequent deliberate user choices.

For a registered screen, bind its existing `VisualGridSettings` (native) or
`BlazorGridSettings` (web) root section
to the grid's non-null `Preferences` property. That explicitly replaces its
standalone persistence owner; do not leave two records competing. Declare column
keys/titles in value-only defaults, with a distinct role for each grid. Deliberate
manual widths use stable `PreferenceKey` identities and logical 96-DPI units;
web widths use CSS pixels. Columns without an override retain natural/stretch
sizing. Restore before first sizing/query and preserve a live cell draft when
accepted Options changes alter layout or defer query settings.

Import old paging/sorting only when the destination section is absent.
`ApplicationSettingsProfile.TryInitializeSection` records presence separately
from resettable values, persisted alongside the next accepted root snapshot.
Explicit Options Apply owns the root's public value members and declared grid
paths even before they open. A root `LogOutput` property needs no separate section
declaration. Reset/restart must not reimport a retired record merely
because the accepted width collection is intentionally empty. Standalone grids
retain their built-in persistence using the same value capture/apply mechanism.

## Reusable logging preferences

Use the public WinForms
[LogOutputSelectorControl](../../../src/Sphere10.Framework.Windows.Forms/Controls/LogOutputSelectorControl.cs)
for optional File, Application or standard-stream output. Its
[LogOutputSelectorSettings](../../../src/Sphere10.Framework.Windows.Forms/Controls/Logging/LogOutputSelectorSettings.cs)
payload stores the accepted `LogOutputConfiguration` and enabled choice; no
logger, writer, service or control is serialized. **Remember After Close** is an
explicit wizard option, off by default.

Bind the owning `IApplication.Logger` and all `LoggingStarting`/failure handlers,
then call `RestoreSettings()` from the screen's initialized load/activation path.
The control must not restore in its constructor or merely because a handle was
created: remembered On can open a file and must pass the real consumer's preflight.
The method is idempotent once a stable key/provider is available; a false return
allows retry after initialization. The tester's
[LoggingTestScreen](../../../utils/Sphere10.Framework.Utils.WinFormsTester/Screens/LoggingTestScreen.cs)
demonstrates that ordering. A restored output that fails preflight/opening stays
off and exposes `LastError` without rewriting the user's remembered choice.

Use a stable `SettingsKey` before restoring/accepting output, or the control's
screen/control type and named child path. Captions and docking do not form the ID.
Accepted configuration and explicit On/Off changes save when Remember is enabled.
Disposal closes a live sink without changing the saved On choice to Off.
Unchanged instances do not overwrite another instance's accepted preferences;
failed accepted writes retry through their original provider at disposal.
Restoration, layout and provisional/cancelled wizard edits do not save.
Optional preference errors appear in `LastSettingsError` while output remains usable.

Turning Remember off deletes only this selector's record. Delete through its
captured owning provider so `CachedSettingsProvider` also evicts the cache entry:
the inner settings object's `Provider` can be the underlying file provider, whose
`Delete()` alone would leave the decorator's cached copy discoverable in this
session. Keep accepted models separate from the shared cached object until save.
Verify next-session behavior with a fresh provider, including remembered On/Off,
consumer preflight, invalid options, cache eviction and unchanged-instance disposal.

Screen-owned logging preferences use the selector's plain-value ownership seam
and the root profile rather than its independent record. Applying values must
not acquire a logger or open a file. The screen stops/drains existing output
through its normal lifetime, applies accepted configuration and honors remembered
On only after its preflight is ready. Disabling Remember excludes transient
configuration/enabled state from persisted preferences. Specialized editor and
product validation remain in that product's existing guarded path.

## Verification and source references

Test persistence with a temporary directory and a **fresh provider** for the next-session read, so a cache hit cannot mask a missing disk write. Test defaults, optional IDs, invalid/old values, and accepted versus cancelled UI actions. For main-window settings, verify zero writes during changes and approved placement capture, exactly one at finalization while the window is alive, no second save after closure, and saving before provider disposal. Isolate and restore any replaced static provider; such tests must not run in parallel with other users of it.

Inspect these current implementations before extending their behavior:

- [UserSettings](../../../src/Sphere10.Framework.Application/Settings/UserSettings.cs), [SettingsObject](../../../src/Sphere10.Framework.Application/Settings/SettingsObject.cs), and [ISettingsProvider](../../../src/Sphere10.Framework.Application/Settings/ISettingsProvider.cs).
- [Application module registration](../../../src/Sphere10.Framework.Application/ModuleConfiguration.cs).
- [DirectoryFileSettingsProvider](../../../src/Sphere10.Framework.Application/Settings/DirectoryFileSettingsProvider.cs) and [CachedSettingsProvider](../../../src/Sphere10.Framework.Application/Settings/CachedSettingsProvider.cs).
- [MainFormSettingsFinalizer](../../../src/Sphere10.Framework.Windows.Forms/Application/Components/MainFormSettingsFinalizer.cs) and [window tracking helpers](../../../src/Sphere10.Framework.Windows.Forms/WindowSettingsTool.cs).
- [WinForms usage](../../../src/Sphere10.Framework.Windows.Forms/README.md).
- [VisualGrid preference and layout tests](../../../tests/Sphere10.Framework.Windows.Forms.Tests/VisualGridAutoPageSizeTests.cs), including fresh-provider round trips and concurrent instances sharing a settings ID.

Use the local [WinForms tester](../../../utils/Sphere10.Framework.Utils.WinFormsTester/Program.cs) for the real `.UseMainFormSettings()` registration path, and the [WinForms lifecycle guide](../../../src/Sphere10.Framework.Windows.Forms/README.md#application-lifecycle-and-splash) for its approved shutdown ordering.
