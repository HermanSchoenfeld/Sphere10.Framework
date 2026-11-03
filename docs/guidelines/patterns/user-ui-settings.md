# Per-user form and control settings

Use the existing settings library for screen, block, plugin and standalone UI
preferences. Every settings root derives from `SettingsObject`; nested data
records may remain ordinary values. See [native screen settings](winforms-screen-settings.md)
and [Screen Options](../../usage/screen-options.md) for declarations and editors.

## Owned instances and defaults

Screens declare `[Settings(typeof(...))]` with their nested `SettingsObject`
type; an optional covariant `ScreenSettings` override provides typed access.
Attribute, property and explicit builder settings declarations must agree;
conflicts throw. Blocks and plugins expose `BlockSettings` and `PluginSettings`.
A supplied instance is the actual settings object retained by that owner.
Options enumerates these owners without constructing operational screens and
omits screen registrations without settings. No catalog, profiles, edit sessions
or settings factories are required.

Declare default values with `[DefaultValue]`. `SettingsObject` already applies
attributes during activation through `ObjectWithDefaultValues`. Reset calls its
existing `RestoreDefaultValues()` method. Complex domain defaults can override
that method, call base and reset their nested values. Do not capture instances
as immutable defaults or construct hidden copies to implement Reset.

Class-level `[UserEditable]` enables the automatic editor. `[Browsable]` and
`[ReadOnly]` affect editing; `[Transient]` excludes editing and persistence.
`[Editor]` selects a dedicated settings editor. Keep screen-specific settings
classes inside their screens under `#region Inner Types`.

## Persistence and change events

Use `SettingsServices.UserSettings`, backed by the established
`Local<ISettingsProvider>` / native `UserSettings.Provider`. `IApplication.Settings`
exposes that same provider. Preserve the outer provider, record type and ID,
cache and encryption behavior. Controls do not replace the provider.

An accepted `settings.Save()` or awaited `settings.SaveAsync(token)` persists
through the attached provider, then calls internal `NotifyChanged()` to raise
public `OnChanged`. Failure or cancellation does not raise that event. Screens
listen to their own, block and plugin instances, refresh on the UI/renderer owner,
and unsubscribe on removal. Nonvisual services use those same objects and events.

Native screens use the existing `ApplicationControl` / `UserControlEx` lifecycle:
restore before work, present in `CopyModelToUI`, capture deliberate UI changes in
`CopyUIToModel`, and use `Refresh` after accepted settings changes. Scalar changes
call `MarkSettingsChanged()` when they need accepted-close persistence; bound
grid/log controls already track their edits. Hide, vetoed close, painting,
layout and unchanged disposal do not authorize saves.

Options editors borrow the actual object. Local dialog backups restore unapplied
edits on Cancel; earlier successful Applies remain saved. Reset only changes
memory. Apply validates and awaits `SaveAsync`; it needs no publication/session
coordinator. Runtime references, providers and transient members are excluded
from local backups and persistence. Local backups retain values omitted only by
persistence-specific `ShouldSerialize` policies, so Cancel and Reset remain useful.
An explicitly requested Options owner takes precedence; otherwise prefer the
active screen's settings and retain the existing owner/first-page fallback.

## Standalone settings

Give settings types feature-specific names: the default file provider uses short
type names, so unrelated nested types named `FormSettings` can collide. Supply a
stable ID to distinguish records of one type. `Get<T>()` returns stored settings
or provider-bound defaults; new objects are not saved automatically by the default
provider. Cached values can be shared by type/ID.

```csharp
using System.ComponentModel;
using Sphere10.Framework.Application;

public class EmployeeSearchSettings : SettingsObject {
	[DefaultValue(100)]
	public int PageSize { get; set; }

	[DefaultValue("")]
	public string SearchText { get; set; }
}
```

```csharp
var settings = UserSettings.Get<EmployeeSearchSettings>("ArchivedEmployees");
settings.PageSize = 50;
settings.Validate().ThrowOnFailure();
await settings.SaveAsync();
```

`LoadSetting(defaults)` and its async counterpart select the record using the
supplied object's type and ID. Set the ID before loading. Providers may return
an existing cached instance; application adapters retain a supplied owner's
identity and populate its persisted values through the existing settings serializer.
Missing values retain declared defaults. Writable collections replace defaults,
including explicitly empty collections; explicit null remains distinct from absence.

## Provider execution directions

`ISettingsProvider` declares paired sync and async create, contains, load, save,
delete and clear operations. `SettingsProviderBase` shares policies without
choosing IO direction. `SyncSettingsProviderBase` owns sync primitives and worker
adapters; `AsyncSettingsProviderBase` owns awaited primitives and standard sync
adapters. `NewSettingAsync` awaits optional autosave. Decorators forward both
directions. See [provider directions](sync-async.md#settings-providers).

Directory providers retain JSON `.setting` writes, legacy XML reads and existing
encryption. The user directory remains `{UserDataDir}/{ProductName}`. Keep record
types/IDs stable. Store preference values and identifiers, not services, controls,
live sources or business entity graphs. Optional preference-load failures should
be logged at the UI boundary while defaults keep the form usable.

Browser preferences use `UserSettings` with the circuit-scoped
`BlazorSettingsProvider` and current user/browser identity. Blazor Framework
initialization and circuit activities establish the facade's provider binding.
Settings-bearing runtime owners are circuit-local; never replace the process
default with one browser user's provider. Await IO after interactive
startup and before operational screen work. Loading preferences does not start
loggers, files, hardware collectors or management operations.

## Main-window placement

Use the framework's `.UseMainFormSettings()` on the WinForms application builder to enable main-window persistence. Keep the opt-in at application startup. It restores the configured main form on load and registers `MainFormSettingsFinalizer` to capture approved placement before shutdown tasks and write it once during shutdown. The later window closure must not queue another save. For `BlockMainForm`, it also stores the left navigation pane's width in the same `FormWindowSettings`, including the remembered expanded width while hidden.

Remember normal bounds as well as the monitor and maximized state. Use restore bounds when maximized, never reopen minimized, and fit restored bounds into an available monitor's working area. Account for a disconnected monitor, changed resolution or DPI, and the form's minimum size. Use `Tools.WinForms.AutoPersistWindowSettings(Form, SettingsID)` in a scope for other forms, or `CaptureWindowSettings` and `RestoreWindowSettings` for explicit lifecycle integration. `FormWindowSettings.NavigationPaneWidth` stores the optional preferred menu width in device pixels at the saved `Dpi`; restore scales it for the current display and clamps it against the configured `BlockMainForm.MaximumNavigationPaneWidth`. That maximum is configured in logical pixels at 96 DPI and defaults to 480. Main-frame resizing preserves this width and gives the remaining space to the content pane. Only physically insufficient native panel bounds can constrain its visible width without replacing the stored preference: widening the frame restores it, while an explicit divider move or width assignment selects a new preference. The standalone `AutoPersistWindowSettings` helper saves once on accepted closure; use the builder option for main forms so the write belongs to framework shutdown.

## Reusable grid preferences

[VisualGridSettings](../../../src/Sphere10.Framework.Windows.Forms/VisualGrid/VisualGridSettings.cs)
is a settings data object for one logical grid. It stores nullable `PageMode`, the
manual `PageSize`, zero-based Paged/Auto `PageNumber`, `SortColumnName` and
`SortDirection`. It contains values rather than controls, row data or a live
provider graph. [VisualGrid](../../../src/Sphere10.Framework.Windows.Forms/VisualGrid/Controls/VisualGrid.cs)
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

A stored typed screen root is authoritative. Consult old standalone records only
when that typed root is absent; do not maintain a section-version registry or
sidecar. Resetting saved values must not cause a later restart to reimport old
control records. Standalone grids retain their own existing persistence.

## Reusable logging preferences

Use the public WinForms
[LogOutputSelectorControl](../../../src/Sphere10.Framework.Windows.Forms/Logging/Controls/LogOutputSelectorControl.cs)
for optional File, Application or standard-stream output. Its
[LogOutputSelectorSettings](../../../src/Sphere10.Framework.Windows.Forms/Logging/LogOutputSelectorSettings.cs)
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
and the actual screen settings root rather than an independent record. Applying values must
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
- [MainFormSettingsFinalizer](../../../src/Sphere10.Framework.Windows.Forms/Application/MainFormSettingsFinalizer.cs) and [window tracking helpers](../../../src/Sphere10.Framework.Windows.Forms/Application/WindowSettingsTool.cs).
- [WinForms usage](../../../src/Sphere10.Framework.Windows.Forms/README.md).
- [VisualGrid preference and layout tests](../../../tests/Sphere10.Framework.Windows.Forms.Tests/VisualGridAutoPageSizeTests.cs), including fresh-provider round trips and concurrent instances sharing a settings ID.

Use the local [WinForms tester](../../../utils/Sphere10.Framework.Utils.WinFormsTester/Program.cs) for the real `.UseMainFormSettings()` registration path, and the [WinForms lifecycle guide](../../../src/Sphere10.Framework.Windows.Forms/README.md#application-lifecycle-and-splash) for its approved shutdown ordering.
