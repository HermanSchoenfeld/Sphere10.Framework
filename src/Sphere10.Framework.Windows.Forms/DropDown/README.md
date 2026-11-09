# Drop Down

Popup and drop-down hosting for custom editor content, with working-area placement, resize grips and a themed drop-down button.

`PopupControl` owns a `PopupDropDown` and borrows the control shown inside it. `Show` positions the popup and can enable resizing on chosen edges through `PopupResizeMode`. `PopupDropDown` is the `ToolStripDropDown` that keeps the popup inside the working area and draws resize grips with `GripRenderer`. A control that owns a popup implements `IPopupControlHost`, as `CustomComboBox` in [ComboBoxes](../ComboBoxes/README.md) does.

`DropDownForm` is the form-based alternative used by `TextBoxUITypeEditor` in [Controls](../Controls/README.md). It hosts a borrowed editor control below or above its parent control within the monitor work area. It closes when it loses activation or on the keys selected by `DropDownFlags`. `DropDownButton` draws the drop-down arrow with hover and pressed feedback.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`DropDownFlags`](DropDownFlags.cs) | enum | Flags that let Escape or Enter close a `DropDownForm` |
| [`GripAlignMode`](GripAlignMode.cs) | enum | Corner in which a popup resize grip is drawn |
| [`GripRenderer`](GripRenderer.cs) | class | Static helpers that draw the native size grip in a requested corner |
| [`IPopupControlHost`](IPopupControlHost.cs) | interface | `ShowDropDown` and `HideDropDown` operations of a control that owns a popup |
| [`PopupResizeMode`](PopupResizeMode.cs) | enum | Flags for the popup edges and corners that can be dragged to resize |

### Controls

| Type | Kind | Purpose |
|---|---|---|
| [`DropDownButton`](Controls/DropDownButton.cs) | class | Themed drop-down arrow control with hover, pressed and keyboard feedback |
| [`PopupControl`](Controls/PopupControl.cs) | class | Disposable owner of a resizable popup that shows a borrowed control at a location and size |
| [`PopupDropDown`](Controls/PopupDropDown.cs) | class | Tool strip drop-down that hosts popup content within the working area and processes resize grips |

### Forms

| Type | Kind | Purpose |
|---|---|---|
| [`DropDownForm`](Forms/DropDownForm.cs) | class | Borderless form that shows a borrowed editor control next to its parent control, fitted to the work area, with open and closed events |
