# Controls

Standalone general-purpose WinForms controls for text, numeric, color, image, button, group box and panel input and presentation.

These controls have no dedicated support classes, so they share one folder. The validated editors `TextBoxEx`, `MaskedTextBoxEx` and `ValidatedTextBoxButton` accept a borrowed `IValidator` and expose a typed `Value`. `NumericBoxBase<T>` derives from `TextBoxEx`, and `IntBox`, `DecimalBox` and `MoneyBox` specialize it. `EditableControl` draws a themed edit border around composed inputs such as `ColorPicker` and `ValidatedTextBoxButton`.

`WindowsFormsControlUtil`, reached as `Tools.Windows.Forms.Controls`, creates the shared `SearchTextBox` and the toolbar choice selector from [ToolStrips](../ToolStrips/README.md). Controls with their own support types have their own domains, such as [ComboBoxes](../ComboBoxes/README.md), [DropDown](../DropDown/README.md) and [Display](../Display/README.md).

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`BitmapButton`](BitmapButton.cs) | class | Button that draws up, down, focused, mouse-over and disabled states from horizontally tiled frames in its `Image` |
| [`ButtonTextBox`](ButtonTextBox.cs) | class | Text box with an embedded right-hand button and a `ButtonClick` event |
| [`CheckedGroupBox`](CheckedGroupBox.cs) | class | Group box with a check box in its caption that can disable its children while unchecked |
| [`CheckedListBoxEx`](CheckedListBoxEx.cs) | class | Checked list box with an after-change `ItemChecked` event and `CheckItems` by item or predicate |
| [`ColorPicker`](ColorPicker.cs) | class | Shows the selected color and opens the native color dialog to change it |
| [`DecimalBox`](DecimalBox.cs) | class | `NumericBoxBase<decimal>` editor for decimal values |
| [`EditableControl`](EditableControl.cs) | class | User control base that draws a themed edit border and lays out content beside an optional button |
| [`FlagsCheckedListBox`](FlagsCheckedListBox.cs) | class | Checked list box bound to a flags `EnumType` whose checks combine into `SelectedEnum` |
| [`GradientPanel`](GradientPanel.cs) | class | Panel that paints a linear gradient from `FromColor` to `ToColor` at an `Angle` with an optional `Blend` |
| [`HeaderGroupBox`](HeaderGroupBox.cs) | class | Group box drawn as heading text, an optional image and a trailing rule |
| [`ImageAttachmentSelectorControl`](ImageAttachmentSelectorControl.cs) | class | Lets the user attach an image from a file or the clipboard and returns it through `TryGetSelectedImage` |
| [`ImageLinkLabel`](ImageLinkLabel.cs) | class | Link label with normal, mouse-over and disabled images, alignment options and an optional rounded border |
| [`ImageNavigator`](ImageNavigator.cs) | class | Shows borrowed images one at a time with previous and next navigation and a status line |
| [`IntBox`](IntBox.cs) | class | `NumericBoxBase<int>` editor for integer values |
| [`MaskedTextBoxEx`](MaskedTextBoxEx.cs) | class | Masked text box with `IValidator`-based typed `Value` validation and formatting |
| [`MoneyBox`](MoneyBox.cs) | class | `NumericBoxBase<decimal>` editor with a currency format string |
| [`NumericBoxBase<T>`](NumericTextBoxBase.cs) | abstract class | Nullable numeric text editor with parsing hooks, `NullText`, `FormatString` and `ValueChanged` |
| [`NumericUpDownEx`](NumericUpDownEx.cs) | class | Numeric up-down that commits typed input when native validation completes |
| [`PadLockButton`](PadLockButton.cs) | class | Button that toggles a locked or unlocked padlock image and raises `PadLockStateChanged` |
| [`PanelEx`](PanelEx.cs) | class | Empty `UserControlEx` panel that adds the update-scope support of its base |
| [`PictureBoxEx`](PictureBoxEx.cs) | class | Picture box that can show a DPI-scaled `SystemIconType` icon |
| [`PropertyGridEx`](PropertyGridEx.cs) | class | Property grid with optional automatic label column sizing bounded by a width and ratio |
| [`RadioButtonPanel`](RadioButtonPanel.cs) | class | Panel that keeps one selection across mixed `RadioButton` and `RadioGroupBox` children |
| [`RadioGroupBox`](RadioGroupBox.cs) | class | Group box with a radio button in its caption that can disable its children while unchecked |
| [`SearchTextBox`](SearchTextBox.cs) | class | Search editor that shows a magnifier when empty and a clear action while text is present |
| [`SidebarToggleButton`](SidebarToggleButton.cs) | class | Compact tool strip button drawn as a sidebar outline whose `Checked` state means the sidebar is visible |
| [`SplitButton`](SplitButton.cs) | class | Button with a split arrow that opens `SplitMenuStrip` |
| [`SquareButton`](SquareButton.cs) | class | Gradient button with configurable normal, hover and selected colors and a `ButtonStateChanged` event |
| [`TextBoxEx`](TextBoxEx.cs) | class | Text box with placeholder text and `IValidator`-based typed `Value` validation |
| [`TextBoxUITypeEditor`](TextBoxUITypeEditor.cs) | class | Text box with a button that hosts the `UITypeEditor` of the configured value type as a dialog or drop-down |
| [`TransparentPanel`](TransparentPanel.cs) | class | Panel with a transparent background that raises `OnBackgroundPaint` for custom background drawing |
| [`ValidatedTextBoxButton`](ValidatedTextBoxButton.cs) | class | Validated text box with a right-hand button that opens a dialog through `ShowDialog` |
