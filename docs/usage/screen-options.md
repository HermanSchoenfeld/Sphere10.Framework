# Screen registrations and Options

Attach an ordinary settings object to the block-owned screen registration with
`UseSettings`. The application restores its values before the screen works and
provides Options even before that screen opens. Editors edit the supplied value;
the application handles persistence and acceptance.

Mark the settings class `[UserEditable]` to use the automatic editor. Public
properties are editable unless `[ReadOnly(true)]` makes them read-only or
`[Browsable(false)]` hides them. These attributes affect editing, not persistence.
An unmarked object can still load and save, and can use an explicitly supplied editor.

## Declare values and defaults

Settings can be an ordinary class with public editable values. Constructor/property
defaults, a supplied instance, or a supplied factory also provide **Reset Defaults**.
The factory creates only the settings value; it never constructs the screen.
Managed profile records restore over those authoritative defaults: missing
members retain their defaults, including members of an existing nested object.
An explicitly stored collection replaces its default collection, even when
empty; explicit null stays distinct from a missing member. Compatible legacy
type/ID records continue to use their original provider's serialization contract.

```csharp
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

[UserEditable]
public class UsersPreferences {
	[Category("Display")]
	[DisplayName("Show archived users")]
	public bool ShowArchived { get; set; }

	[Category("Display")]
	[Range(1, 10000)]
	public int PageSize { get; set; } = 100;
}
```

The platform block builder uses the same registration shape:

```csharp
block
	.AddScreen<UsersScreen>(screen => screen
		.WithTitle("Users")
		.UseSettings<UsersPreferences>(),
		out var users
	)
	.AddMenu(menu => menu
		.WithText("View")
		.AddShowScreenItem(users)
		.AddShowScreenItem<UsersScreen>()
	);
```

Use `.UseSettings(new UsersPreferences { PageSize = 50 })` to supply an instance,
or `.UseSettings(() => new UsersPreferences { PageSize = 50 })` for fresh factory
defaults. Instance defaults are copied when declared, so later edits cannot
change Reset. A supplied instance or factory can use a class whose constructor
requires arguments. No settings base class is required. Existing `SettingsObject`
classes remain supported and use their `Validate()` method. Ordinary objects can
use validation attributes and `IValidatableObject`; validate nested/grid sections
there when they need product constraints.

The type command chooses the first matching registration in that owning block.
Use the moniker to select an exact registration when a screen type is registered
several times. Defaults and programmatic type lookup still require an unambiguous
registration. See [builder migration](builder-api-migration.md) for the complete
API migration and activation rules.

Profiles use stable application/plugin/block/registration identities. Set
`WithSettingsKey` on the application and `WithStableKey` on otherwise ambiguous
same-type registrations. Changing a displayed title does not change a profile.
Several instances of one registration share accepted preferences; different
registrations of the same type have separate profiles. `AddSettings` contributes
application-root preferences without inventing a screen.

## Restore before work and save accepted snapshots

`IApplication.Settings` exposes the catalog. Platform startup restores declared
profiles before default/permanent screens and their first query. Catalog
enumeration and editor navigation never run activators. A no-options screen
still appears in the catalog with a no-options explanation.

Native screens use `GetSettings<TSettings>()` after their normal application
attachment. Bind grid roles with `BindGridPreferencesAsync<TSettings>` after
configuring columns and before the first refresh. It imports absent legacy grid
sections and subscribes the control to the registration profile. Use
`OnSettingsChanged()` for other value-only presentation changes, and
`MarkSettingsChanged()` for deliberate local scalar edits.
`BindLogOutputPreferencesAsync<TSettings>` binds a named plain logging section
without opening output during restoration. Foreign accepted changes stop/drain
existing output through its normal lifetime, apply values and explicitly honor
remembered On through the consumer's preflight. An own-save acknowledgement does
not restart a live output.

Blazor screens consume the supplied typed `values` in
`OnSettingsChangedAsync()` before their first data load. That snapshot also carries
working edits preserved by a later section initialization. Use
`GetSettings<TSettings>()` when explicitly reading a separate accepted profile
snapshot. Bind each grid's
`Preferences` and awaited `PreferencesChanged` callback to a named root section.
For accepted direct-control changes, update the working values, call
`MarkSettingsChanged()`, and await `SaveSettingsChangesAsync()`. Override
`CaptureSettings()` to copy UI-owned values at an accepted save boundary.
Do not replace an unsaved business draft when preferences change.

Capture values on their UI/renderer owner. Stores serialize and perform IO away
from that owner using detached snapshots; they never encrypt or serialize a live
editor or shared cached root. Optional restore failures expose `LastLoadError`
and use validated defaults. Failed saves expose `LastSaveError`, retain dirty
values and allow retry. Hide, a rejected close, refresh and unchanged disposal
do not authorize a save. Accepted close and approved application finalization
capture remaining changes and await persistence. Screen captures wait for pending
preference applications; a section initialized during capture is rebased before
an obsolete snapshot is retried.

## Native and web Options

Applications with declared options add **Options → Screen Options...**. Existing
Options commands and headings remain in place. The web shell places a newly
needed heading before Help while preserving the product's menu order.

Both dialogs group application/plugin/block/registration pages, show no-options
screens, hide persistence IDs/providers, and support scalar, enum, nested and
collection values. Ordinary properties named `ID` or `Provider` remain ordinary
values; only the persistence members of `SettingsObject` are hidden. The native default editor reuses
`DefaultCrudEntityEditor<object>` and its PropertyGrid mechanisms. The web
default editor uses the same value metadata with renderer-owned controls.

**Apply**, **Reset Defaults** and **OK** operate on the selected page. Apply
validates and awaits storage before clearing that page's dirty state; a failure
keeps the draft and error visible. OK applies the selected page and closes only
when no other page has unapplied edits. **Cancel** discards all unapplied drafts,
including nested/collection edits, while preserving earlier successful Applies.
Opening Options and displaying/resetting defaults do not write preferences.

For dedicated native UI, use `WithSettingsEditor` on the screen builder. Its
`WinFormsApplicationSettingsEditorContext` supplies the detached `Settings` object,
real services and owner. Application-root definitions can use
`WinFormsApplicationSettingsDefinition`. Custom editors implement the existing
entity-editor contract; the dialog owns validation and persistence.

For dedicated web UI, use `UseSettings<TSettings, TEditor>()` with a component
implementing `IBlazorApplicationSettingsEditor`. It receives the ordinary `Value` and
an awaited `Changed` callback. `ValidateAsync` validates pending editor text;
the dialog still invokes object validation and owns persistence. A screen can also
use `UseSettings(instance).WithSettingsEditor<TEditor>()`; application-root
definitions accept `settingsEditor: typeof(TEditor)`.
The selected page remains fixed while asynchronous validation or storage runs.
Editor factories/components are runtime metadata and are never serialized.

## One persistence owner per control

Use the existing native `VisualGridSettings` or web `BlazorGridSettings` value per
logical grid, with distinct
named roles for screens containing several grids. Declare readable column
schemas in defaults so unopened screens have useful Options pages. Stable
`PreferenceKey` values identify computed/icon columns; captions and indexes do
not identify saved widths.

A non-null grid `Preferences` selects screen-owned mode and suppresses the
independent standalone record. The same capture/apply mechanism serves standalone
grids. Native widths are logical 96-DPI units; web widths use CSS pixels.

The grid's top settings button edits that same `VisualGridSettings` or
`BlazorGridSettings` value through a detached draft. It appears when settings
exist; `AllowsSettingsChange` controls whether the button is enabled. Apply/OK
uses the existing grid binding, while Cancel discards the provisional edits.
The compact page-mode menu offers Single, Auto and Manual; Manual retains the
existing `Paged` enum value.
Only deliberate manual overrides persist. Natural and stretch sizing remain
active for other columns. Apply before first sizing, keep overrides through
refresh/paging/rebinding, and defer query changes while a cell draft is active.
Completed drags update values without file IO on every movement.

Import legacy values only when the destination section is absent. Presence lives
outside resettable values and persists with the next accepted snapshot. Explicit
Options Apply owns the root's public value properties and declared grid paths
even if their controls have never opened. A `LogOutput` property therefore needs
no separate declaration to prevent a later legacy import. Reset must not cause
a future restart to reimport an intentionally retired record. Do not delete
unrelated or standalone settings as a side effect.

Native `WinFormsApplicationSettingsStore` captures the application's provider,
keeps existing encryption rules, and supports `ApplicationSettingsDefinition.WithStorageId`
for a compatible legacy `SettingsObject` type/ID, including explicit null. Those
legacy records retain their value-only format. Plain main forms can register the same
catalog with `AddApplicationSettings`.

The default scoped `BlazorApplicationSettingsStore` uses origin-local browser
storage. Keys include application/profile identity plus authenticated
NameIdentifier/sub/name, or a persisted anonymous browser identity. A new circuit
is required after the authenticated subject changes. Values survive reload in
the same browser profile; this default does **not** synchronize across devices.
For server-backed per-user persistence supply `IApplicationSettingsStore` in the
product. Do not use process-static native `UserSettings` for web users, or store
credentials/business entity graphs in browser preferences. Browser interop begins
only in the interactive circuit, before operational screen work.

Loading values never starts loggers, hardware access or management operations.
Products apply such choices through their existing guarded lifecycle/preflight;
the settings catalog and editors remain resource-free.

## Adapter coordination

`IApplication.Settings` exposes the application-owned catalog used by the standard
dialogs and screen helpers. Application code using those helpers does not need to
create profiles or edit sessions. Internally, a profile holds accepted values;
an Options edit session holds a detached provisional copy. Apply validates and
awaits persistence before publishing, and Cancel discards only unapplied edits.

Custom owners can use `GetValues<T>(out revision)`, `Capture` and
`SaveCaptureAsync` to preserve the same ordering. Producer identity, local version
and applied revision prevent old instances or delayed acknowledgements from
replacing newer accepted/local values. `FlushAsync` joins accepted writes before
provider disposal. Stores receive exclusive ordinary objects: `LoadAsync` hydrates
the supplied default value, and `SaveAsync` persists its detached snapshot.

For late-bound controls, `TryInitializeSection` initializes one absent section
and takes a section-copy callback. Consumers receive isolated copies of that
frozen section, preserving unrelated dirty values, save errors and local versions.
Initialization does not save. `WaitForPendingWritesAsync` joins queued writes
without rethrowing a previous optional save error; `FlushAsync` still reports it.

## Verification and examples

Use a fresh provider/circuit to prove persistence, independent profiles,
import → Reset → restart, and first-query/first-sizing restoration. Exercise
invalid input, storage failure/retry, ordered delayed saves, accepted versus
vetoed closure, stale-instance disposal and actual UI button/keyboard visibility.
Use the [startup measurement procedure](../guidelines/patterns/testing.md#startup-memory-regressions)
for comparable before/after readiness and memory evidence.

The [WinForms tester](../../utils/Sphere10.Framework.Utils.WinFormsTester/README.md)
and [Blazor tester](../../utils/Sphere10.Framework.Utils.BlazorTester/README.md)
demonstrate independent registrations and Options. See the platform
[native grid](../../src/Sphere10.Framework.Windows.Forms/README.md#crudgrid) and
[web grid](../../src/Sphere10.Framework.Web.AspNetCore.Blazor/README.md#grid-configuration-and-behavior)
guides for control bindings, and
[per-user UI settings](../guidelines/patterns/user-ui-settings.md) for standalone
providers and window placement.
