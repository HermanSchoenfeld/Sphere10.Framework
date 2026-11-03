# WinForms screen, block and plugin settings

Use the existing `ApplicationControl` / `UserControlEx` model and UI lifecycle.
The screen declares its settings type; its registration supplies presentation,
identity and optional supplied settings instances. All settings derive from `SettingsObject`.
Options discovers them without constructing the screen or invoking its getters.

## Declare the screen's settings

Keep native screens concrete and nongeneric so the Windows Forms designer can
construct them. Declare the settings type with `[Settings(typeof(...))]` on the
screen class. An optional covariant `ScreenSettings` override provides typed
access. Explicitly hide that override from the designer: component-model
metadata does not reliably inherit through covariant property overrides.
Define the screen-specific settings class as a public inner type of the form or
screen it configures. A shared screen-base schema belongs inside that base
screen; reusable control, block and plugin settings retain their own owners.

```csharp
using System.ComponentModel;
using Sphere10.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Windows.Forms;

[Settings(typeof(MailScreen.MailScreenSettings))]
public partial class MailScreen : ApplicationScreen {
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public override MailScreenSettings ScreenSettings {
		get {
			return (MailScreenSettings)base.ScreenSettings;
		}
	}

	protected override void CopyModelToUI() {
		base.CopyModelToUI();
		if (HasSettings)
			_serverTextBox.Text = ScreenSettings.Server;
	}

	protected override void CopyUIToModel() {
		base.CopyUIToModel();
		if (HasSettings && ScreenSettings.Server != _serverTextBox.Text) {
			ScreenSettings.Server = _serverTextBox.Text;
			MarkSettingsChanged();
		}
	}

	#region Inner Types

	[UserEditable]
	public class MailScreenSettings : SettingsObject {
		[DefaultValue("localhost")]
		public string Server { get; set; }

		public VisualGridSettings Messages { get; set; }

		[Transient]
		public object RuntimePreview { get; set; }

		public override void RestoreDefaultValues() {
			base.RestoreDefaultValues();
			Messages = new VisualGridSettings {
				PageMode = VisualGridPageMode.Auto,
				PageSize = 50,
				SortColumnName = "Received",
				SortDirection = SortDirection.Descending,
				Columns = new[] {
					new ApplicationGridColumnPreferences { Key = "sender", Title = "Sender" },
					new ApplicationGridColumnPreferences { Key = "subject", Title = "Subject", ExpandsToFit = true },
					new ApplicationGridColumnPreferences { Key = "received", Title = "Received" }
				}
			};
		}
	}

	#endregion
}
```

Normal registration needs no repeated settings schema:

```csharp
block.AddScreen<MailScreen>("Mail");
```

The attribute is equivalent to a builder settings declaration. A supplied builder
instance retains its identity; a conflicting settings type throws while building
the definition. Existing class-level `[UseSettings(typeof(...))]` declarations
remain supported. Attribute and typed-override declarations must agree.
Neither declaration invokes operational UI. Do not introduce a generic visual
screen base, another settings descriptor, or a string-key lookup in the control.

Options omits screen definitions whose settings are null. A declared settings
type supplies defaults before the operational screen is opened, so those pages
remain available without constructing the screen.

`[UserEditable]` marks the settings class; its public properties are eligible for
the automatic Options editor. `[ReadOnly(true)]` and `[Browsable(false)]` affect
editing while retaining persistence. `[Transient]` excludes a member from the
editor and persistence, including when a derived property overrides an
annotated base property. It is appropriate for runtime references that
are not preference data. Validation combines `SettingsObject.Validate()` with
validation attributes and `IValidatableObject`.

A registration-specific editor takes precedence over `[Editor]` on the settings
class, followed by the `[UserEditable]` automatic editor. Declare a native class
editor with `[Editor(typeof(MailSettingsEditor), typeof(ICrudEntityEditor<object>))]`;
it can use a parameterless constructor or accept `ApplicationSettingsEditorContext`.
The same class editor also serves block/plugin settings.

## Use the existing lifecycle

Application initialization restores settings asynchronously before hosted screen
work. The inherited native sequence is `InitializeUIPrimingData()`,
`CopyModelToUI()`, then localization inside the existing update scope.
`InitializeUIPrimingData()` prepares ready models; `CopyModelToUI()` presents
them. Accepted Options changes use `Refresh()`, which repeats `CopyModelToUI()`
inside an update scope. Keep operational drafts separate from preferences.

Accepted `SaveSettingsAsync()` captures `CopyUIToModel()` on the UI thread before
awaiting persistence. Compare scalar values and call `MarkSettingsChanged()` when
they deliberately change; assigning a plain property alone does not mark its
binding dirty. The actual settings object saves through its attached provider and raises
`OnChanged` only after persistence succeeds. Screens observe that event and
refresh through the same existing model/UI lifecycle; removal unsubscribes them. Hiding, rejected closure, layout and unchanged disposal do not
authorize a save. Do not call synchronous storage from paint, resize, pointer or
keystroke events. Do not block synchronous model/UI hooks on asynchronous work.

Grid/log initialization can remain asynchronous. Configure columns, await the
existing `BindGridPreferencesAsync(grid, nameof(MailScreenSettings.Messages))`,
then bind and refresh the source through its normal awaited initialization task.
Each logical grid has its own `VisualGridSettings` value in the screen root,
with stable column keys and readable titles declared in its defaults. Options
must list those columns before the screen opens and preserve accepted width and
stretch overrides when loading an older or empty record. The grid applies that
configuration; the screen owns its editor and persistence. Native grids have no
settings button, local settings dialog or independent settings record. The screen's
`Columns` collection determines displayed column order, with width and stretch
overrides attached to stable keys. Manual width takes priority over `ExpandsToFit`.
The Auto/Single/Manual selector, manual page size, page navigation, search,
sorting and column resizing remain transient operations. Binding does not copy
them back into accepted `ScreenSettings` during capture, close or finalization.

For explicit configuration commands, drop `VisualGridConfigurationContextMenu`
onto the screen and assign it to the grid's `HeaderContextMenuStrip`, or attach
it after `InitializeComponent()`:

```csharp
_messages.HeaderContextMenuStrip = new VisualGridConfigurationContextMenu(components);
await BindGridPreferencesAsync(_messages, nameof(MailScreenSettings.Messages));
```

Right-click a column header to choose **Save configuration** or **Restore
defaults**. The reusable menu resolves the clicked grid's owning screen. Save
copies only that grid's current configuration to its existing settings section,
then persists the actual root through its provider. Restore applies the selected
grid's complete defaults from the screen settings class. The default
`DefaultsRestorePolicy` is `VisualGridDefaultsRestorePolicy.RestoreOnly`;
`RestoreAndSave` persists the restored values through the same owner. Declare its
complete column schema, order, initial page mode, manual size and sort in those defaults. A shared menu
can serve several grids; `SourceControl` identifies the clicked one.
`HeaderContextMenuStrip` also accepts an ordinary custom `ContextMenuStrip`.
Screen Options remains the configuration editor, with explicit Apply/OK.

## Block and plugin settings

Declare `SettingsObject` defaults on the existing block and plugin builders:

```csharp
var plugin = new ApplicationPluginBuilder()
	.WithName("Mail tools")
	.UseSettings(new MailPluginSettings())
	.AddBlock(block => block
		.WithName("Mail")
		.UseSettings(new MailWorkspaceSettings())
		.AddScreen<MailScreen>("Mail")
	)
	.Build();
```

`MailPluginSettings` and `MailWorkspaceSettings` derive from `SettingsObject`.
Those values populate `PluginSettings` and `BlockSettings`. Their Options nodes
use the class editor or automatic editing when enabled. Service-only plugins are
included. A screen reads accepted owner values with
`GetBlockSettings<MailWorkspaceSettings>()` and
`GetPluginSettings<MailPluginSettings>()`; it does not need settings of its own.
Matching owner changes refresh existing screens through the same model/UI path.
An unrelated block's changes do not refresh them.

For a nonvisual service, read the actual `PluginSettings` or `BlockSettings`,
subscribe to its `OnChanged`, and await that object's `SaveAsync(cancellationToken)`
for accepted edits. `IApplication.Settings` is the existing provider. Options
enumerates owners directly; no catalog, profile, factory or edit-session layer
is needed. Use the canonical plugin reference from `LoadedPlugins` / `Block.Source`.

Plugin objects are startup declarations and may be shared by Blazor circuits.
Accepted user values belong to the running application, not those declarations.
Never mutate startup plugin defaults into one user's live settings.

## Defaults, identity and providers

Several instances of one registration share the actual settings object. Distinct
registrations can supply separate `UseSettings(instance)` values and stable keys.
The supplied object is retained; it is not a captured defaults template.

Defaults are declared by `[DefaultValue]` on the settings class's members.
`SettingsObject` activation already applies them. Options Reset calls the actual
object's `RestoreDefaultValues()`. The native dialog confirms the selected
branch's impact and defaults to resetting that owner plus descendants, including
settings without editors. Defaults stay provisional; Apply normally saves the
pending reset scope across navigation, and OK always saves all pending resets.
See the [native reset policies](../../usage/screen-options.md#native-reset-policies)
for the reusable dialog's `Policy` property and `ApplicationSettingsScreenPolicy`
flags. For nested values that attributes cannot express, override that existing
method, call base and restore the domain's nested
defaults there. This keeps activation and Reset consistent. Do not add factories
or constructor cloning to registrations or editors.

Use the existing native `SettingsServices.UserSettings` provider, which is backed
by the configured `Local<ISettingsProvider>` / `UserSettings.Provider`. Controls
do not replace it or own another provider. Save the actual typed `SettingsObject`
through `settings.SaveAsync()`, which awaits that provider and then raises the
actual object's `OnChanged`. Native Options borrows the actual object; any Cancel
backups belong only to the dialog. Do not introduce another store, carrier or token-backed record. Preserve
existing encryption, cache behavior,
record type/ID and existing legacy-import behavior. An existing owner settings object's
non-null `ID` retains its legacy record identity. Keep legacy nested record types
when their full names identify stored records.

Do not add application-root settings definitions to declare screen preferences.
Put shared preferences on their actual block or plugin. Window placement remains
with `.UseMainFormSettings()` and its approved shutdown finalizer.

## Verify the pattern

Exercise unopened Options without constructing screens; independent same-type
registration defaults; real Apply/Cancel/Reset and failure/retry; typed owner
access with and without screen settings; first-query restoration; and accepted
versus rejected closure. Include a scalar-only edit, accepted close and fresh-provider
reopen so a grid edit cannot mask a missing dirty notification. Verify transient
grid changes do not enter that save, explicit Save captures only the selected grid,
and the default RestoreOnly policy stays temporary until saved. Verify cascade
Reset includes noneditable settings, Cancel restores unapplied descendants, and
Apply/OK honor their configured scope. Verify
`ScreenSettings` is non-browsable and hidden from
designer serialization on each concrete screen. Stop queued owner refreshes
after removal. `[Transient]` members must not appear in either native or
web editors or serialized values.

Examples: [EmailTestScreen](../../../utils/Sphere10.Framework.Utils.WinFormsTester/Screens/EmailTestScreen.cs),
[VisualGridTestScreen](../../../utils/Sphere10.Framework.Utils.WinFormsTester/Screens/VisualGridTestScreen.cs),
and the CPU/GPU registrations in
[WinFormsDemoPlugin](../../../utils/Sphere10.Framework.Utils.WinFormsTester/WinFormsDemoPlugin.cs).
See [Screen Options](../../usage/screen-options.md) for browser settings and
custom editors, and [per-user settings](user-ui-settings.md) for standalone records.
