# Miscellaneous

Cross-domain Windows Forms helpers, including the `Tools.WinForms` facade, rendering deferral scopes, UI-thread disposal, placeholder text, system icon kinds and XML signatures.

`Tools.WinForms` is the object behind `Tools.WinForms`. Its `Charts`, `Logging`, `Grids`, `Windowing`, `Review` and `Controls` properties return the per-domain facades, and it adds alignment conversion and cursor creation helpers. Its partial declaration in `Application/WindowSettingsTool.cs` adds `AutoPersistWindowSettings`. Import the `Tools` namespace to reach the facade, because it is supplied as an extension member.

`ControlRenderingScope` suspends layout and native redrawing of a control tree until the outermost scope over it ends, which avoids flicker during bulk changes. Enter and dispose it on the control's UI thread. The application screen host enters one over the window for each screen change. Suppressed redraw also removes a window's native visibility (common controls such as the tab control handle it themselves and stay visible), so a suppressed control can neither be clicked nor take focus: end a scope before returning to the message loop. To keep a control's previous frame across an asynchronous update instead, skip its painting while the window stays visible, as `GridVirtual.SuspendPainting` does for grids. `ControlLayoutScope` only defers layout: changing many auto-sized controls, such as labels in nested tables, lays the tree out once when the scope ends, and only the changed controls repaint. `WinFormsSyncDisposableBase` is the base for disposable UI objects whose asynchronous disposal must run on the creating Windows Forms dispatcher, as `GlobalHotKey` in [HotKeys](../HotKeys/README.md) does.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ControlLayoutScope`](ControlLayoutScope.cs) | class | Scope that defers layout of a control tree and lays it out once when it ends, without suspending painting |
| [`ControlRenderingScope`](ControlRenderingScope.cs) | class | Scope that defers layout and native drawing of a control tree until its outermost scope ends, then repaints |
| [`PlaceHolderTextExtender`](PlaceHolderExtender.cs) | class | Designer extender that sets the native cue banner (placeholder text) of text boxes and combo boxes |
| [`SystemIconType`](SystemIconType.cs) | enum | System icon to show in a message dialog, such as information, question, warning, error or shield |
| [`Tools.WinForms`](WindowsFormsUtil.cs) | static class | The `Tools.WinForms` facade with per-domain helpers, alignment conversion and cursor creation |
| [`WinFormsSyncDisposableBase`](WinFormsSyncDisposableBase.cs) | abstract class | Synchronous disposable base that runs asynchronous cleanup on the creating Windows Forms synchronization context |
| [`XmlDigitalSignature`](XmlDigitalSignature.cs) | static class | Creates, checks, extracts and removes enveloped RSA XML signatures and generates XML-encoded key pairs |
