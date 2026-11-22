# Hot Keys

System-wide keyboard shortcuts registered through `RegisterHotKey`, with user-assignable commands and editor controls.

`GlobalHotKey` registers one shortcut on a private message-only window and raises `Pressed` when Windows reports it. It installs no keyboard hook. `TryRegister` keeps the previous registration when the new shortcut fails, rejects modifier-only keys and F12, and returns the reason as text. `Keys.None` unregisters the shortcut. `GlobalHotKey.WindowsKey` is the modifier flag for the Windows key, which `Keys` lacks. Create, use and dispose a hot key on the same UI thread.

`GlobalHotKeySet` keeps a changing set of shortcuts registered. Applications describe their bindable operations as `HotKeyCommand` records and store the user's choices as `HotKeyAssignment` values, which keep only shortcuts that differ from a command's default. `GlobalHotKeySet.Apply(commands, assignments)` registers the resolved shortcuts and reports failures through `Errors` and `ErrorsByKey`. `HotKeyCommandEditor` lists the commands by group for an options screen, and `HotKeyEditor` edits a single shortcut.

```csharp
using var hotKey = new GlobalHotKey();
hotKey.Pressed += () => ToggleOverlay();
if (!hotKey.TryRegister(GlobalHotKey.WindowsKey | Keys.Alt | Keys.L, out var error))
	MessageBox.Show(error);
```

## Contents

Held actions can subscribe to the `EventHandlerEx` `Released` event, or set
`HotKeyCommand.Release` / `GlobalHotKeyBinding.Release`. A lazy caller-owned
Framework scheduler checks key state every 16 ms only during a held shortcut with
a release subscriber. Releasing the regular key or any required modifier ends
the hold. Rebinding, unregistering and disposal balance an active hold as well;
a failed rebind leaves it intact. The release callback runs on the owner UI thread.
Ordinary shortcuts create no scheduler or input hook. A set retains the release
action associated with the press so replacing or removing a binding can finish it.

| Type | Kind | Purpose |
|---|---|---|
| [`GlobalHotKey`](GlobalHotKey.cs) | class | Registers one global shortcut on a message-only window and raises `Pressed`, without a keyboard hook |
| [`GlobalHotKeyBinding`](GlobalHotKeyBinding.cs) | record | Key, caption, shortcut and action of one binding that a `GlobalHotKeySet` keeps registered |
| [`GlobalHotKeySet`](GlobalHotKeySet.cs) | class | Registers, re-registers and releases a set of bindings, keeping a failed binding's previous registration |
| [`HotKeyAssignment`](HotKeyAssignment.cs) | class | A user's shortcut for one command in application settings, with `Resolve`, `Assign` and `FindConflicts` helpers |
| [`HotKeyCommand`](HotKeyCommand.cs) | record | Name, caption, default shortcut, action and display group of a bindable operation |
| [`IGlobalHotKey`](IGlobalHotKey.cs) | interface | Contract for a UI-thread global shortcut, which a `GlobalHotKeySet` creates through an overridable factory |

### Controls

| Type | Kind | Purpose |
|---|---|---|
| [`HotKeyCommandEditor`](Controls/HotKeyCommandEditor.cs) | class | Lists commands by group with their shortcuts and edits the selected command, showing registration errors |
| [`HotKeyEditor`](Controls/HotKeyEditor.cs) | class | Edits a shortcut as a regular key plus Win, Ctrl, Alt and Shift modifiers, and formats shortcuts as text |
