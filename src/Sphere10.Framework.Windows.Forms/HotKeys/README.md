# Hot Keys

System-wide keyboard shortcuts registered through `RegisterHotKey`, with user-assignable commands and editor controls.

`GlobalHotKey` registers one shortcut on a private message-only window and raises `Pressed` when Windows reports it. It installs no keyboard hook. `TryRegister` keeps the previous registration when the new shortcut fails, rejects modifier-only keys and F12, and returns the reason as text. `Keys.None` unregisters the shortcut. `GlobalHotKey.WindowsKey` is the modifier flag for the Windows key, which `Keys` lacks. Create, use and dispose a hot key on the same UI thread.

`GlobalHotKeySet` keeps a changing set of shortcuts registered. Applications describe their bindable operations as `HotKeyCommand` records and store the user's choices as `HotKeyAssignment` values, which keep only shortcuts that differ from a command's default. `GlobalHotKeySet.Apply(commands, assignments)` registers the resolved shortcuts and reports failures through `Errors` and `ErrorsByKey`. `HotKeyCommandEditor` lists the commands by group for an options screen, and `HotKeyEditor` edits a single shortcut.

```csharp
using var hotKey = new GlobalHotKey();
hotKey.Pressed += (_, _) => ToggleOverlay();
if (!hotKey.TryRegister(GlobalHotKey.WindowsKey | Keys.Alt | Keys.L, out var error))
	MessageBox.Show(error);
```

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`GlobalHotKey`](GlobalHotKey.cs) | class | Registers one global shortcut on a message-only window and raises `Pressed`, without a keyboard hook |
| [`IGlobalHotKey`](IGlobalHotKey.cs) | interface | Contract for a UI-thread global shortcut, which a `GlobalHotKeySet` creates through an overridable factory |

### Controls

| Type | Kind | Purpose |
|---|---|---|
| [`HotKeyEditor`](Controls/HotKeyEditor.cs) | class | Edits a shortcut as a regular key plus Win, Ctrl, Alt and Shift modifiers, and formats shortcuts as text |
