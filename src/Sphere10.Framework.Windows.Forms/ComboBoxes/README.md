# Combo Boxes

Combo box controls with placeholders and validation, custom drop-down content, enum binding and image tile choices.

`ComboBoxEx` is the common base. It adds placeholder text, an optional `IValidator` and a typed `Value`. `CustomComboBox` replaces the native list with any `DropDownControl`, shown in a resizable popup from [DropDown](../DropDown/README.md). `EnumComboBox` fills itself from an enum type, with an optional empty option, filter and ignore list. `ImageComboBox` builds on `CustomComboBox` and shows `ImageComboBoxItem` choices as a tile picker.

Plain `ComboBox` helpers such as `TrySelectByText` live in `ComboBoxExtensions` in [Extensions](../Extensions/README.md).

`EnumComboBox` keeps enum-valued items while enumerating their underlying scalar values, so a runtime-selected enum does not require reflection to construct a concrete enum array. Trimmed callers must preserve the public enum fields used for names and descriptions. The control preserves the item properties used by its string-based display binding; application publishing must also retain the required WinForms binding support.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ImageComboBoxItem`](ImageComboBoxItem.cs) | class | One `ImageComboBox` choice with text, borrowed image, value, tooltip and optional group |
| [`OldNewEventArgs<T>`](OldNewEvent.cs) | class | Event data carrying the previous and new value of a change |
| [`OldNewEventHandler<T>`](OldNewEvent.cs) | delegate | Handler for events that report an `OldNewEventArgs<T>` change |

### Controls

| Type | Kind | Purpose |
|---|---|---|
| [`ComboBoxEx`](Controls/ComboBoxEx.cs) | class | Combo box with placeholder text, optional value formatting and `IValidator`-based typed `Value` validation |
| [`CustomComboBox`](Controls/CustomComboBox.cs) | class | Combo box that shows an arbitrary `DropDownControl` in a resizable popup and reports old and new selected values |
| [`EnumComboBox`](Controls/EnumComboBox.cs) | class | Combo box bound to an `EnumType` with `SelectedEnum`, an optional empty option, a filter and ignored values |
| [`ImageComboBox`](Controls/ImageComboBox.cs) | class | Drop-down list of image choices that shows the selection's image and text and opens a tile picker |
