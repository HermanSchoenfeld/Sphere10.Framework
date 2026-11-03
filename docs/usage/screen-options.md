# Screen registrations and Options

Screens, blocks and plugins own actual `SettingsObject` instances. A screen
declares its settings with `[Settings(typeof(...))]`; blocks and plugins expose
`BlockSettings` and `PluginSettings`. Options enumerates those existing owners,
including unopened screen definitions with settings and service-only plugins.
Screens with no settings are omitted. It does not
construct operational screens to discover preferences.

The navigation hierarchy is application → plugin → block → screen. The application
caption comes from the configured `IProductInformationProvider.ProductInformation.ProductName`,
not the application's CLR class name. Select assembly attributes with
`.UseAssemblyInfo()` (or supply an assembly), or configure `ProductInformation`
through `.WithProductInformation(...)` on the native or Blazor application builder.
The same selected provider supplies existing product-information consumers. Caption
changes do not change settings IDs. Plugin and block nodes retain their owners' names.

## Declare settings and their defaults

Every settings root derives from `SettingsObject`. Keep each screen-specific
schema as a public inner type inside its screen, at the bottom under
`#region Inner Types`. Native typed properties explicitly hide themselves from
the designer; covariance does not reliably inherit that metadata.

```csharp
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Sphere10.Framework.Application;
using Sphere10.Framework.Windows.Forms;

[Settings(typeof(UsersScreen.UsersPreferences))]
public partial class UsersScreen : WinFormsApplicationScreen {

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public override UsersPreferences ScreenSettings {
		get {
			return (UsersPreferences)base.ScreenSettings;
		}
	}

	#region Inner Types

	[UserEditable]
	public class UsersPreferences : SettingsObject {

		[Category("Display")]
		[DefaultValue(false)]
		public bool ShowArchived { get; set; }

		[Category("Display")]
		[DefaultValue(100)]
		[Range(1, 10000)]
		public int PageSize { get; set; }
	}

	#endregion
}
```

`SettingsObject` activation applies `[DefaultValue]` attributes through its
existing `ObjectWithDefaultValues` base. Reset calls the actual object's
`RestoreDefaultValues()`. For complex nested values, override that existing
method, call base, and restore the domain's own values there. Activation and
Reset then use the same defaults. There is no settings factory, captured-default
snapshot or constructor-cloning mechanism in a builder or editor.

Normal registration only needs `AddScreen<UsersScreen>("Users")`. The attribute
is equivalent to declaring that settings type through the builder. A covariant
`ScreenSettings` override is optional and provides convenient typed access.
Existing `[UseSettings(typeof(...))]` declarations remain supported. Attribute,
typed property and explicit builder settings must agree; conflicting schemas
throw while the screen definition is built. An explicit
`UseSettings(instance)` retains that actual supplied instance. It is a live
owner value, not an immutable Reset template.

Class-level `[UserEditable]` enables automatic editing of public properties.
`[ReadOnly(true)]` makes a property read-only; `[Browsable(false)]` hides it while
retaining persistence. `[Transient]` excludes editing and persistence. Hide the
inherited provider and ID. Validation combines `SettingsObject.Validate()`,
data annotations and `IValidatableObject`.

## Ownership, identity and loading

Several instances of one registration share its settings object. Distinct
registrations can use separate supplied instances and stable keys. Type menu
commands select the first matching registration in the owning block; use a
registration moniker for an exact choice. See [builder migration](builder-api-migration.md).

Screen storage IDs derive from application/plugin/block/registration identity,
not display captions. Use `WithSettingsKey` on the application and
`WithStableKey` for otherwise ambiguous same-type registrations. Explicit
settings IDs retain their existing record identity. Shared preferences belong
on their actual block or plugin; no application-root settings definition is needed.

`IApplication.Settings` exposes the established `ISettingsProvider`. Native
startup uses `SettingsServices.UserSettings` / `UserSettings.Provider`, preserving
its existing encryption and cache behavior. Adapters restore actual owner values
before default/permanent screens and their first query. When a provider returns
a separate cached object, the adapter populates the supplied owner's values
without replacing its identity. Missing members retain declared defaults;
explicitly stored collections replace defaults, including empty collections.
Explicit null is distinct from absence.

Loaded values must pass the settings object's validation and data annotations.
An invalid or partially read optional record restores the supplied defaults,
retaining the actual root, ID and provider without writing. Cancellation restores
those values before propagating. Encrypted writes use a shallow storage copy of
root string members so the live object stays in cleartext during IO.

## Saving and screen refresh

Call the actual settings object's `Save()` or await `SaveAsync(token)`. It writes
through its attached provider and only then calls internal `NotifyChanged()`,
which raises public parameterless `OnChanged`. Failed or cancelled persistence
does not publish a change. Provider decorators must not duplicate that event.
An edit made while a save is pending remains dirty for the next accepted save.

Screens observe their own, block and plugin settings instances. Native screens
dispatch accepted changes to the UI thread and reuse `Refresh()` /
`CopyModelToUI`. Capture deliberate edits in `CopyUIToModel` and call
`MarkSettingsChanged()` when scalar changes require accepted-close persistence.
Use the existing `SaveSettingsAsync()` boundary; do not add a second settings
lifecycle or block synchronous model/UI hooks on IO.

Blazor screens use their typed `ScreenSettings` and renderer-owned
`OnSettingsChangedAsync` path. Mark deliberate edits, then await
`SaveSettingsChangesAsync`. Retain operational drafts when preferences change.
Both adapters unsubscribe when the screen is removed. Nonvisual services read,
observe and save the same actual plugin/block objects.

Hide, a vetoed close, paint, layout and unchanged disposal do not save settings.
Accepted close and approved finalization await the existing save boundary while
the provider is available. Native grids are excluded from automatic runtime
capture: their current page, sort and layout do not replace accepted screen
configuration at this boundary. Failures remain visible and retryable.

## Options editors

Applications with declared preferences offer **Options → Screen Options...**
while preserving existing menu commands. Both dialogs enumerate plugins, blocks
and registrations directly; there is no catalog, profile or edit-session layer.

Options initially selects the active screen's settings, even when another
block is selected for navigation. An explicitly requested owner takes precedence.
If the active screen has no settings, its block settings are preferred; otherwise
the dialog falls back to an available settings page.

Editors receive the actual owner object. The dialog may keep local serialized
backups for Cancel, excluding runtime/transient members but retaining values
omitted only by persistence-specific `ShouldSerialize` rules. Apply validates
and awaits `SaveAsync`; success updates that page's backup and clears its dirty
state. Failure retains the edits and error for retry. Reset calls
`RestoreDefaultValues` in memory and does not write. Cancel restores unapplied
edits, including nested values and collections, while retaining earlier
successful Applies. Opening and navigating Options does not save.

Editor selection is an explicit registration editor, then `[Editor]` on the
settings class (including inherited attributes), then `[UserEditable]`.
For native class editors use
`[Editor(typeof(MySettingsEditor), typeof(ICrudEntityEditor<object>))]`; the editor
can accept `WinFormsApplicationSettingsEditorContext` or have a parameterless
constructor. `WithSettingsEditor` supplies a registration-specific override.
The editor context's `Settings` is the actual object; the dialog owns validation
and persistence.

Web class editors use
`[Editor(typeof(MySettingsEditor), typeof(IBlazorApplicationSettingsEditor))]`.
`UseSettings<TSettings, TEditor>()` or `WithSettingsEditor<TEditor>()` selects a
component explicitly. It receives `Value` and an awaited `Changed` callback.
`ValidateAsync` checks pending editor input before object validation and save.
The selected page stays fixed while asynchronous validation or storage runs.
Editor metadata is not serialized.

## Grids and logging

Each logical grid uses its own existing `VisualGridSettings` or
`BlazorGridSettings` value inside the screen root. Bind before first sizing/query,
with stable column keys and readable default titles. Native grids apply the
screen-owned configuration, while their ordinary page, sort and resize operations
remain transient. Their configuration editor is Screen Options; they have no
settings button, local settings dialog or independent settings record. A bound
browser grid continues to suppress its separate standalone record.

Declare every column in the screen's settings defaults, so the Options page is
complete even before that screen opens. Merge older or empty column records with
those declarations while preserving accepted overrides by key. Column settings
expose **Manual width** and **Stretch to fit**. Null restores natural width or
the column definition's stretch choice; a manual width takes precedence.
For native grids, arrange the `Columns` collection in the desired displayed
order; width and stretch choices remain attached to stable column keys. Use
the standard WinForms array editor's **Up** and **Down** buttons to move columns.
Accepted changes apply through the screen's existing settings notification and
model/UI lifecycle. Native widths use logical 96-DPI pixels; browser widths use
CSS pixels. Preserve layout across refresh/paging/rebinding and protect active
cell drafts.

The footer's compact page-mode button offers Single, Auto and Manual; Manual
retains the `Paged` enum value. Automatic fitted capacity does not replace the
remembered manual size. Resize and refresh do not authorize writes.

A screen may attach the reusable `VisualGridConfigurationContextMenu` to a
native grid's generic `HeaderContextMenuStrip`. Right-clicking a column header
then offers **Save configuration** and **Restore defaults**. Save captures the
clicked grid's current values into its existing section and saves the actual
screen settings root. Restore applies that grid's complete defaults from the
screen settings class only to the current view by default. The menu's
`DefaultsRestorePolicy` is `VisualGridDefaultsRestorePolicy.RestoreOnly` by
default; choose `RestoreAndSave` to persist the restored configuration through
the same screen owner as part of that command.
Other grids' transient choices are not captured by that command. Closing the
screen or application does not implicitly save grid choices. Screen Options
continues to edit accepted configuration through Apply and OK.

Typed screen records are authoritative. Optional legacy standalone imports are
used only before that record exists; Reset must not reimport retired records
after restart. Bound logging likewise uses the actual screen root. Loading or
editing values does not start loggers, files, hardware or management operations;
the product applies those choices through its existing guarded lifecycle.

## Provider direction and browser scope

`ISettingsProvider` declares sync and async APIs. `SettingsProviderBase` shares
policies; `SyncSettingsProviderBase` owns sync primitives;
`AsyncSettingsProviderBase` owns awaited primitives. Their opposite-direction
adapters follow the existing [directional pattern](../guidelines/patterns/sync-async.md#settings-providers).
Normal UI/browser callers await `UserSettings.GetAsync<T>()`,
`HasAsync<T>()` and the returned settings object's `SaveAsync()`. No extra settings
store or token-backed record is involved.

Browser storage uses the circuit-scoped `BlazorSettingsProvider`, application
identity and authenticated subject or anonymous browser identity. Settings-bearing
runtime owners are circuit-local; startup declarations retain startup ownership.
Start a new circuit after an authenticated subject change. Preferences survive
reload in that browser profile; this storage does not synchronize devices. A
product may supply a server-backed provider through the same contract. Never use
a process-global browser provider for all users. The same `UserSettings` facade
uses the provider bound during Blazor Framework/circuit initialization and each
inbound activity. Restore after interactive startup,
before operational screen work.

Verify unopened discovery, actual instance identity, attribute activation/Reset,
Apply/Cancel/failure/retry, notification timing, owner-to-screen updates,
first-query restoration, fresh-provider persistence and browser isolation. The
[WinForms tester](../../utils/Sphere10.Framework.Utils.WinFormsTester/README.md)
and [Blazor tester](../../utils/Sphere10.Framework.Utils.BlazorTester/README.md)
demonstrate these paths. See [per-user settings](../guidelines/patterns/user-ui-settings.md)
for standalone records and window placement.
