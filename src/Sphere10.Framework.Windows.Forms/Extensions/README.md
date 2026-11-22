# Extensions

Extension methods for WinForms controls, forms and tool strip items, service registration helpers for native applications, and the `Tools.WinForms` and scope entry points.

`ControlExtensions` holds the most used helpers. `InvokeEx`, `BeginInvokeEx` and `InvokeAsyncEx` dispatch to the control's UI thread. A control without a window handle falls back to the most recent open form, so a handle-free control needs a live form. `EnterRenderingScope` returns a `ControlRenderingScope` from [Misc](../Misc/README.md) that suspends layout and drawing until disposed, and `EnterLayoutScope` returns a `ControlLayoutScope` that suspends only layout. `KeepHeightOf` holds a control, such as a button beside a text box or combo box, at the reference control's height through every rescaling; a single-line box takes its height from its font while a button keeps its designer height, so no fixed size matches at every display scale.

`IoCExtensions` registers the native application. `AddMainForm<TMainForm>` registers the main form as `IMainForm` and `IUserInterfaceServices`. It also registers the screen host interfaces for a `MainForm` and `IApplication` for a `BlockMainForm`. `AddWinFormsApplicationPlugin` builds and registers an `ApplicationPlugin`, and `AddControlStateEventProvider` registers state observers for [StateEvent](../StateEvent/README.md). The application types live in [Application](../Application/README.md).

`WinFormsScopeExtensions` is declared in the `Tools` namespace with a C# extension block. It adds the static members `Tools.Scope.EnterRenderingScope`, `EnterHighlightScope` and `EnterUpdateScope`. The static `Tools.WinForms` utilities are declared in their own domain folders; `Charts` and `Controls` are documented in [Charts](../Charts/README.md) and [Controls](../Controls/README.md).

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ComboBoxExtensions`](ComboBoxExtensions.cs) | static class | `UseSelectionOnlyAppearance`, `TrySelectByText` and `TrySelectOrSet` for `ComboBox` |
| [`ControlExtensions`](ControlExtensions.cs) | static class | UI-thread dispatch, rendering scopes, parent form lookup, control search and paths, child enabling, transparent background painting, dragging and row height matching (`KeepHeightOf`) for `Control` |
| [`FormExtensions`](FormExtensions.cs) | static class | `ShowInactiveTopmost` shows a `Form` topmost without activating it |
| [`IoCExtensions`](IoCExtensions.cs) | static class | Registers the main form, application blocks and WinForms plugins, and registers and resolves control state event providers |
| [`LabelExtensions`](LabelExtensions.cs) | static class | `CanTextFit` and `FitSize` measure whether and how text fits a `Label` |
| [`ListBoxExtensions`](ListBoxExtensions.cs) | static class | `RemoveSelectedItems` for `ListBox` |
| [`NotifyIconExtensions`](NotifyIconExtensions.cs) | static class | `EnableContextMenuOnLeftClick` opens a `NotifyIcon` context menu on left click |
| [`NumericUpDownExtensions`](NumericUpDownExtensions.cs) | static class | `GetValueDouble` and `GetValueInt` read a `NumericUpDown` value |
| [`TextBoxExtensions`](TextBoxExtensions.cs) | static class | `FocusAtEnd`, `AppendText` and `AppendLine` for `TextBox` |
| [`ButtonBaseExtensions`](ButtonBaseExtensions.cs) | static class | `ScaleImageToDeviceDpi` replaces a button's 96-DPI image with one scaled to the button's DPI, as the list merger's arrows and the grid's page buttons do; buttons do not scale their images |
| [`ToolStripExtensions`](ToolStripExtensions.cs) | static class | `ScaleFixedItemSizes` scales the sizes a designer fixed on a tool strip's items and drop-down items (items with `AutoSize` off, text and combo box widths) from 96-DPI pixels to the tool strip's DPI, which Windows Forms does not do |
| [`ToolStripItemExtensions`](ToolStripItemExtensions.cs) | static class | `ReplaceImage` sets a caller-owned image or a pixel-sized icon rendering and disposes the replaced image |
| [`WinFormsScopeExtensions`](WinFormsScopeExtensions.cs) | static class | Adds `Tools.Scope` factories for control rendering, window highlight and update scopes |
