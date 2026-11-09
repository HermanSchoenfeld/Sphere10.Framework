# Drawing

Measurable, cloneable visual elements for owner-drawn cells and controls, with draw-style enums, graphics measurement and layered transparent forms.

The `VisualElements` folder, in the `Sphere10.Framework.Windows.Forms.Drawing.VisualElements` namespace, derives from the SourceGrid DevAge drawing library. Every element implements `IVisualElement`. It measures itself with a `GraphicsMeasurement`, draws into a `GraphicsCache` rectangle and deep-clones through `ICloneable`. `VisualElementBase` is the base class to override. Most element families come in three forms. The `*Base` class holds the style and the plain class draws with `ControlPaint`. The `*Themed` class draws with `VisualStyleRenderer` when visual styles are enabled. [SourceGrid](../SourceGrid/README.md) cell views and [VisualGrid](../VisualGrid/README.md) compose these elements, and `DropDownButton` in [DropDown](../DropDown/README.md) draws with them.

`GraphicsMeasurement` wraps a control's graphics, a borrowed `Graphics` or a `GraphicsCache` for measuring. `WinFormsCompatibleDeepObjectCloner` deep-clones objects while sharing `Font` and `Color` values and copying bitmaps. The `Forms` folder holds layered, input-transparent forms for overlays and effects.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`BackgroundColorStyle`](Enums.cs) | enum | No background, linear gradient or solid fill for header elements |
| [`BorderStyle`](Enums.cs) | enum | No border or the system edit border for editable panels and `EditableControl` |
| [`ButtonStyle`](Enums.cs) | enum | Normal, pressed, hot, disabled, default or focused button drawing state |
| [`CheckBoxState`](Enums.cs) | enum | Undefined, checked or unchecked check box glyph |
| [`ControlDrawStyle`](Enums.cs) | enum | Normal, pressed, hot or disabled drawing state of headers and check boxes |
| [`ElementsDrawMode`](Enums.cs) | enum | Whether container elements overlap or align with each other |
| [`GraphicsMeasurement`](GraphicsMeasurement.cs) | class | Disposable measuring context over a control's own graphics or a borrowed `Graphics` or `GraphicsCache` |
| [`HeaderSortStyle`](Enums.cs) | enum | None, ascending or descending sort indicator |
| [`WinFormsCompatibleDeepObjectCloner`](WinFormsCompatibleDeepObjectCloner.cs) | class | `DeepObjectCloner` that shares `Font` and `Color` values and copies `Bitmap` instances |

### Forms

| Type | Kind | Purpose |
|---|---|---|
| [`ExpandingCircle`](Forms/ExpandingCircle.cs) | class | Click-through, non-activating form that animates a ring expanding from a centre point |
| [`PerPixelAlphaForm`](Forms/PerPixelAlphaForm.cs) | class | Layered form base that displays a 32-bit bitmap with per-pixel alpha and an overall opacity |

### VisualElements

| Type | Kind | Purpose |
|---|---|---|
| [`BackgroundBase`](VisualElements/BackgroundBase.cs) | abstract class | Background element base that reports its content rectangle and the extent needed around content |
| [`BackgroundLinearGradient`](VisualElements/BackgroundLinearGradient.cs) | class | Linear gradient fill between two colors at an angle, solid when both colors match |
| [`BackgroundSolid`](VisualElements/BackgroundSolid.cs) | class | Solid color fill |
| [`Button`](VisualElements/Button.cs) | class | Button background drawn with standard control painting |
| [`ButtonBase`](VisualElements/ButtonBase.cs) | abstract class | Button background base with a `ButtonStyle` |
| [`ButtonThemed`](VisualElements/ButtonThemed.cs) | class | Button background drawn with the Windows visual style |
| [`CheckBox`](VisualElements/CheckBox.cs) | class | Check box glyph drawn with standard control painting |
| [`CheckBoxBase`](VisualElements/CheckBoxBase.cs) | abstract class | Check box glyph base with a draw style and `CheckBoxState` |
| [`CheckBoxThemed`](VisualElements/CheckBoxThemed.cs) | class | Check box glyph drawn with the Windows visual style |
| [`ColumnHeader`](VisualElements/ColumnHeader.cs) | class | Column header background with back color, color style and border |
| [`ColumnHeaderBase`](VisualElements/ColumnHeaderBase.cs) | abstract class | Column header base over `HeaderBase` |
| [`ColumnHeaderThemed`](VisualElements/ColumnHeaderThemed.cs) | class | Column header drawn with the Windows visual style |
| [`Container`](VisualElements/Container.cs) | class | Container with public border, padding, background and draw mode properties |
| [`ContainerBase`](VisualElements/ContainerBase.cs) | abstract class | Base that measures and draws child elements from `GetElements()` inside a border, padding and background |
| [`DropDownButton`](VisualElements/DropDownButton.cs) | class | Drop-down arrow button drawn with standard control painting |
| [`DropDownButtonBase`](VisualElements/DropDownButtonBase.cs) | abstract class | Drop-down arrow base with a `ButtonStyle` |
| [`DropDownButtonThemed`](VisualElements/DropDownButtonThemed.cs) | class | Drop-down arrow drawn with the Windows visual style |
| [`EditablePanel`](VisualElements/EditablePanel.cs) | class | Edit-field border drawn with standard control painting |
| [`EditablePanelBase`](VisualElements/EditablePanelBase.cs) | abstract class | Edit-field border base with a `BorderStyle`, content rectangle and extent |
| [`EditablePanelThemed`](VisualElements/EditablePanelThemed.cs) | class | Edit-field border drawn with the Windows visual style |
| [`Header`](VisualElements/Header.cs) | class | Header background with back color, border and gradient angle |
| [`HeaderBase`](VisualElements/HeaderBase.cs) | abstract class | Header background base with a `ControlDrawStyle` |
| [`HeaderThemed`](VisualElements/HeaderThemed.cs) | class | Header background drawn with the Windows visual style |
| [`IBackground`](VisualElements/IBackground.cs) | interface | Element that reports its content rectangle and the extent required around content |
| [`IButton`](VisualElements/IButton.cs) | interface | Background element with a `ButtonStyle` |
| [`ICheckBox`](VisualElements/ICheckBox.cs) | interface | Element with a draw style and `CheckBoxState` |
| [`IColumnHeader`](VisualElements/IColumnHeader.cs) | interface | Marker for column header elements |
| [`Icon`](VisualElements/Icon.cs) | class | Draws a `System.Drawing.Icon` |
| [`IContainer`](VisualElements/IContainer.cs) | interface | Element that reports a content rectangle and extent for child elements |
| [`IDropDownButton`](VisualElements/IDropDownButton.cs) | interface | Element with a `ButtonStyle` for drop-down arrows |
| [`IEditablePanel`](VisualElements/IEditablePanel.cs) | interface | Border element with a `BorderStyle` |
| [`IHeader`](VisualElements/IHeader.cs) | interface | Background element with a `ControlDrawStyle` |
| [`IIcon`](VisualElements/IIcon.cs) | interface | Element whose value is a `System.Drawing.Icon` |
| [`IImage`](VisualElements/IImage.cs) | interface | Element whose value is a `System.Drawing.Image` |
| [`Image`](VisualElements/Image.cs) | class | Draws a `System.Drawing.Image`, optionally disabled and resized with a `ResizeMethod` |
| [`IRichText`](VisualElements/IRichText.cs) | interface | Element whose value is a `RichText` with color, alignment, font and rotation |
| [`IRowHeader`](VisualElements/IRowHeader.cs) | interface | Marker for row header elements |
| [`ISortIndicator`](VisualElements/ISortIndicator.cs) | interface | Element with a `HeaderSortStyle` |
| [`IText`](VisualElements/IText.cs) | interface | Element whose value is a string with color and font |
| [`IVisualElement`](VisualElements/IVisualElement.cs) | interface | Cloneable element contract to measure, draw, anchor and hit-test within a rectangle |
| [`RichText`](VisualElements/RichText.cs) | class | Draws a `RichText` value with color, alignment and font |
| [`RichTextGDI`](VisualElements/RichTextGDI.cs) | class | Draws rich text through a native rich-edit surface created for each render |
| [`RowHeader`](VisualElements/RowHeader.cs) | class | Row header background with back color, color style and border |
| [`RowHeaderBase`](VisualElements/RowHeaderBase.cs) | abstract class | Row header base over `HeaderBase` |
| [`RowHeaderThemed`](VisualElements/RowHeaderThemed.cs) | class | Row header drawn with the Windows visual style |
| [`SortIndicator`](VisualElements/SortIndicator.cs) | class | Ascending or descending arrow that can reserve its space while unsorted |
| [`SortIndicatorThemed`](VisualElements/SortIndicatorThemed.cs) | class | Sort arrow drawn with the Windows visual style |
| [`Text`](VisualElements/Text.cs) | class | Draws a string with `Graphics.DrawString` |
| [`TextGDI`](VisualElements/TextGDI.cs) | class | Draws a string with a configurable `StringFormat` and alignment |
| [`TextRenderer`](VisualElements/TextRenderer.cs) | class | Draws and measures a string with WinForms `TextRenderer` and `TextFormatFlags` |
| [`VisualElementBase`](VisualElements/VisualElementBase.cs) | abstract class | Element base with anchoring, measurement and drawing that requires `OnDraw`, `OnMeasureContent` and `Clone` overrides |
| [`VisualElementList`](VisualElements/VisualElementList.cs) | class | Cloneable list of `IVisualElement` instances |
