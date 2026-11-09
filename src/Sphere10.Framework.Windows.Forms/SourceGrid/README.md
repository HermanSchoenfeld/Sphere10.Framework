# SourceGrid

A cell-based grid control for Windows Forms, with static and virtual grids, pluggable cell views, models, controllers and editors, selection, export and printing.

This folder holds the library's port of the SourceGrid control by Davide Icardi and Darius Damalakas. Its types use the `Sphere10.Framework.Windows.Forms.SourceGrid` namespace and the `Cells`, `Cells.Controllers`, `Cells.Editors`, `Cells.Models`, `Cells.Views`, `Cells.Virtual`, `Decorators`, `Exporter`, `Selection` and `Utils` sub-namespaces. The types in `Cells/Models/Real` belong to `Cells.Models`, and the types in `Common` and `Grids` belong to the root namespace.

`Grid` stores one static `Cells.Cell` at each position and supports spanned cells, sorting and row and column insertion. `GridVirtual` is the abstract base for grids whose cells are computed on demand. A derived grid overrides `GetCell`, `CreateRowsObject` and `CreateColumnsObject`. A cell combines four parts. A view (`Cells.Views`) draws it, models (`Cells.Models`) supply its value and extra state such as check box status or tooltip text, controllers (`Cells.Controllers`) add behaviour such as resizing, sorting or button clicks, and an editor (`Cells.Editors`) edits its value. Views and controllers expose shared `Default` instances that many cells can reuse. `CellContext` pairs a cell with its grid and position and is the argument that views, models, controllers and editors receive.

Selection behaviour comes from an `IGridSelection` such as `FreeSelection`, `RowSelection` or `ColumnSelection`. `RangeRegion` holds the selected ranges, and `Decorators` draw over them. The `Exporter` types write a grid to CSV, HTML, an image or a print document. Most applications use the data-bound [VisualGrid](../VisualGrid/README.md) instead of this grid directly. [SourceGrid.Extensions](../SourceGrid.Extensions/README.md) adds array, data-bound and planning grids built on these types. Rich text cells use the [RichText](../RichText/README.md) value type and editor.

## Contents

### Cells

| Type | Kind | Purpose |
|---|---|---|
| [`Button`](Cells/Button.cs) | class | Static cell drawn with the `Views.Button` look that invalidates on mouse events |
| [`Cell`](Cells/Cell.cs) | class | Static cell of a `Grid` that stores its own value, row and column span, view, model, controllers and editor |
| [`CellCollection`](Cells/CellCollection.cs) | class | List of the `ICellVirtual` cells of a grid row or column |
| [`CellContext`](Cells/CellContext.cs) | struct | Pairs a cell with its grid and position, and starts or ends editing and reads or writes the value at that position |
| [`CellControl`](Cells/CellControl.cs) | class | Cell that hosts a Windows Forms control as a linked control of the grid |
| [`CheckBox`](Cells/CheckBox.cs) | class | Boolean cell drawn as a check box with an optional caption |
| [`ColumnHeader`](Cells/ColumnHeader.cs) | class | Column header cell with sortable-header model, width resizing and automatic sort |
| [`Header`](Cells/Header.cs) | class | Unselectable 3D header cell that can be resized in both directions |
| [`ICell`](Cells/ICell.cs) | interface | Cell bound to one grid position, with row and column span and a value |
| [`ICellVirtual`](Cells/ICellVirtual.cs) | interface | Cell behaviour without position or value, made of a view, a model container, controllers and an editor |
| [`Image`](Cells/Image.cs) | class | Cell whose value is the image itself, edited with an `ImagePicker` |
| [`Link`](Cells/Link.cs) | class | Cell drawn as a hyperlink with a hand cursor |
| [`RichTextBox`](Cells/RichTextBox.cs) | class | Cell whose value is a `RichText`, with selection font, colour and alignment properties |
| [`RowHeader`](Cells/RowHeader.cs) | class | Unselectable row header cell that can be resized in height |

### Cells/Controllers

| Type | Kind | Purpose |
|---|---|---|
| [`BindProperty`](Cells/Controllers/BindProperty.cs) | class | Writes a cell's new value to a property of a linked object whenever the value changes |
| [`Button`](Cells/Controllers/Button.cs) | class | Raises `Executed` when the user clicks the cell or presses Space or Enter on it |
| [`CellEventDispatcher`](Cells/Controllers/CellEventDispatcher.cs) | class | Shared controller that forwards each unhandled event to the controller of the cell that raised it |
| [`CheckBox`](Cells/Controllers/CheckBox.cs) | class | Toggles a check box cell on click or Space, optionally for every selected cell, and raises `CheckedChanged` |
| [`ColumnFocus`](Cells/Controllers/ColumnFocus.cs) | class | Moves focus to the whole column when a cell of that column receives focus |
| [`ColumnSelector`](Cells/Controllers/ColumnSelector.cs) | class | Selects the cell's whole column when the cell is clicked |
| [`ControllerBase`](Cells/Controllers/ControllerBase.cs) | abstract class | Base controller whose handlers for every `IController` event do nothing |
| [`ControllerContainer`](Cells/Controllers/ControllerContainer.cs) | class | Ordered set of controllers that dispatches each cell event to all of them |
| [`CustomEvents`](Cells/Controllers/CustomEvents.cs) | class | Controller that exposes every cell event as a .NET event for ad-hoc handlers |
| [`IController`](Cells/Controllers/IController.cs) | interface | Contract for a cell behaviour with mouse, keyboard, focus, edit and value-change handlers |
| [`MouseCursor`](Cells/Controllers/MouseCursor.cs) | class | Changes the mouse cursor while the pointer is over the cell |
| [`MouseInvalidate`](Cells/Controllers/MouseInvalidate.cs) | class | Repaints the cell on mouse down, mouse up, enter and leave |
| [`MouseSelection`](Cells/Controllers/MouseSelection.cs) | class | Selects cells and ranges with mouse down, drag and double click |
| [`Resizable`](Cells/Controllers/Resizable.cs) | class | Resizes the row height or column width by dragging the cell border |
| [`RichTextBox`](Cells/Controllers/RichTextBox.cs) | class | Applies a font, colour, character offset, alignment or effect assigned to a rich text cell to its current selection |
| [`RowFocus`](Cells/Controllers/RowFocus.cs) | class | Moves focus to the whole row when a cell of that row receives focus |
| [`RowSelector`](Cells/Controllers/RowSelector.cs) | class | Selects the cell's whole row on click, or on mouse down for `SelectOnMouseDown` |
| [`SortableHeader`](Cells/Controllers/SortableHeader.cs) | class | Sorts the grid by the header's column when clicked, and supports resizing |
| [`StandardBehavior`](Cells/Controllers/StandardBehavior.cs) | class | Default cell behaviour for selection, keyboard and editing, shared by cells |
| [`ToolTipText`](Cells/Controllers/ToolTipText.cs) | class | Shows the tooltip text that the cell's `IToolTipText` model returns |
| [`Unselectable`](Cells/Controllers/Unselectable.cs) | class | Prevents the cell from receiving focus or selection |

### Cells/Editors

| Type | Kind | Purpose |
|---|---|---|
| [`ComboBox`](Cells/Editors/ComboBox.cs) | class | Editor that edits a value in a `ComboBoxEx` with optional standard values |
| [`ControlsRepository`](Cells/Editors/ControlsRepository.cs) | class | Read-only dictionary of editor controls keyed by GUID and parented to the grid |
| [`CrudReference`](Cells/Editors/CrudReference.cs) | class | Read-only editor that opens a CRUD reference picker dialog and shows the bound entity's display text |
| [`DateTimePicker`](Cells/Editors/DateTimePicker.cs) | class | Editor that edits dates with a `DateTimePicker` |
| [`DropDownList`](Cells/Editors/DropDownList.cs) | class | Drop-down editor over a `ComboBoxEx` that can display a member of each item |
| [`EditorBase`](Cells/Editors/EditorBase.cs) | class | Type-converting validator that every cell editor derives from, with editable mode and edit lifecycle |
| [`EditorControlBase`](Cells/Editors/EditorControlBase.cs) | abstract class | Base editor that creates, positions and reads a Windows Forms control for editing |
| [`Factory`](Cells/Editors/Factory.cs) | static class | Creates a suitable editor for a type from its UI type editor, standard values or string conversion |
| [`ImagePicker`](Cells/Editors/ImagePicker.cs) | class | Editor that loads an image from a file chosen with a `TextBoxButton` |
| [`NumericUpDown`](Cells/Editors/NumericUpDown.cs) | class | Editor that edits numbers with a `NumericUpDown` control |
| [`RichTextBox`](Cells/Editors/RichTextBox.cs) | class | Editor that edits rich text in a `RichTextBoxEx` |
| [`TextBox`](Cells/Editors/TextBox.cs) | class | Editor that edits a typed value in a `TextBoxTyped` |
| [`TextBoxButton`](Cells/Editors/TextBoxButton.cs) | class | Editor that edits a typed value in a text box with a button |
| [`TextBoxCurrency`](Cells/Editors/TextBoxCurrency.cs) | class | Numeric text box editor for currency values |
| [`TextBoxNumeric`](Cells/Editors/TextBoxNumeric.cs) | class | Text box editor that restricts input to numeric characters |
| [`TextBoxUITypeEditor`](Cells/Editors/TextBoxUITypeEditor.cs) | class | Text box editor whose button opens the type's `UITypeEditor` |
| [`TimePicker`](Cells/Editors/TimePicker.cs) | class | Editor that edits times of day with a `DateTimePicker` |

### Cells/Models

| Type | Kind | Purpose |
|---|---|---|
| [`CheckBoxStatus`](Cells/Models/CheckBoxStatus.cs) | struct | Checked state, enabled flag and caption that a check box model reports for drawing |
| [`ICheckBox`](Cells/Models/ICheckBox.cs) | interface | Model contract that reads a check box status and sets its checked value |
| [`IImage`](Cells/Models/IImage.cs) | interface | Model contract that supplies the image drawn in a cell |
| [`IModel`](Cells/Models/IModel.cs) | interface | Marker contract for cell models |
| [`IRichTextBox`](Cells/Models/IRichTextBox.cs) | interface | Model contract for reading and changing the selection format of a rich text cell |
| [`ISortableHeader`](Cells/Models/ISortableHeader.cs) | interface | Model contract that reads a header's sort status and sets its sort style |
| [`IToolTipText`](Cells/Models/IToolTipText.cs) | interface | Model contract that supplies a cell's tooltip text |
| [`IValueModel`](Cells/Models/IValueModel.cs) | interface | Model contract that reads and writes a cell's value |
| [`ModelContainer`](Cells/Models/ModelContainer.cs) | class | Set of models attached to a cell, with the required `ValueModel` and lookup by model type |
| [`SortStatus`](Cells/Models/SortStatus.cs) | struct | Sort style of a header together with the comparer used for sorting |

### Cells/Models/Real

| Type | Kind | Purpose |
|---|---|---|
| [`CheckBox`](Cells/Models/Real/CheckBox.cs) | class | Check box model that reads and writes the checked state through the cell value and keeps a caption |
| [`Image`](Cells/Models/Real/Image.cs) | class | Image model that returns a fixed image assigned to `ImageValue` |
| [`NullValueModel`](Cells/Models/Real/NullValueModel.cs) | class | Value model that always returns null and ignores writes |
| [`RichTextBox`](Cells/Models/Real/RichTextBox.cs) | class | Rich text model that inserts text and changes selection font, colour, offset and alignment in the cell value |
| [`SortableHeader`](Cells/Models/Real/SortableHeader.cs) | class | Sortable header model that stores a fixed `SortStatus` |
| [`ToolTip`](Cells/Models/Real/ToolTip.cs) | class | Tooltip model that returns a fixed `ToolTipText` |
| [`ValueImage`](Cells/Models/Real/ValueImage.cs) | class | Image model that reads the image directly from the cell value |
| [`ValueModel`](Cells/Models/Real/ValueModel.cs) | class | Value model that stores the value inside the cell, used by static cells |

### Cells/Views

| Type | Kind | Purpose |
|---|---|---|
| [`Button`](Cells/Views/Button.cs) | class | View that draws the cell as a themed push button |
| [`Cell`](Cells/Views/Cell.cs) | class | Default shareable view that draws a cell's background, border, text element and image element |
| [`CheckBox`](Cells/Views/CheckBox.cs) | class | View that draws a check box with optional caption |
| [`ColumnHeader`](Cells/Views/ColumnHeader.cs) | class | View that draws a column header with sort indicator, without theme support |
| [`ComboBox`](Cells/Views/ComboBox.cs) | class | View that draws the cell text with a drop-down button element |
| [`Header`](Cells/Views/Header.cs) | class | View that draws a 3D header background |
| [`IView`](Cells/Views/IView.cs) | interface | Contract for drawing and measuring a cell, with its colours, font, border and alignment |
| [`Link`](Cells/Views/Link.cs) | class | View that draws the cell text as a hyperlink |
| [`MultiImages`](Cells/Views/MultiImages.cs) | class | View that draws several sub-images in the cell besides its content |
| [`RichTextBox`](Cells/Views/RichTextBox.cs) | class | View that draws the cell value as rich text |
| [`RowHeader`](Cells/Views/RowHeader.cs) | class | View that draws a row header |
| [`ViewBase`](Cells/Views/ViewBase.cs) | abstract class | Base view built from visual elements, shared between cells |

### Cells/Virtual

| Type | Kind | Purpose |
|---|---|---|
| [`Button`](Cells/Virtual/Button.cs) | abstract class | Virtual button cell whose value comes from the grid's data source |
| [`CellVirtual`](Cells/Virtual/CellVirtual.cs) | class | Base virtual cell that holds view, model, controllers and editor but reads its value from the grid |
| [`CheckBox`](Cells/Virtual/CheckBox.cs) | class | Virtual check box cell whose value comes from the grid's data source |
| [`ColumnHeader`](Cells/Virtual/ColumnHeader.cs) | class | Virtual column header cell with sorting and resizing |
| [`Header`](Cells/Virtual/Header.cs) | abstract class | Virtual 3D header cell |
| [`Image`](Cells/Virtual/Image.cs) | class | Virtual image cell whose value is the image |
| [`Link`](Cells/Virtual/Link.cs) | abstract class | Virtual hyperlink cell |
| [`RichTextBox`](Cells/Virtual/RichTextBox.cs) | class | Virtual rich text cell |
| [`RowHeader`](Cells/Virtual/RowHeader.cs) | class | Virtual row header cell with height resizing |

### Common

| Type | Kind | Purpose |
|---|---|---|
| [`AutoSizeMode`](Common/AutoSizeMode.cs) | enum | Flags that control whether a row or column auto-sizes, stretches or keeps a minimum size |
| [`CellCancelEventArgs`](Common/CellCancelEventArgs.cs) | class | Cell event arguments with a `Cancel` flag |
| [`CellCancelEventHandler`](Common/CellCancelEventHandler.cs) | delegate | Handler for cancellable cell events |
| [`CellContextEventArgs`](Common/CellContextEventArgs.cs) | class | Event arguments that carry a `CellContext` |
| [`CellContextEventHandler`](Common/CellContextEventHandler.cs) | delegate | Handler for cell events that carry a `CellContext` |
| [`CellOptimizeMode`](Common/CellOptimizeMode.cs) | enum | Whether a `Grid` stores its cells optimised for row or for column operations |
| [`CellPositionType`](Common/CellPositionType.cs) | enum | Whether a position is empty, in a fixed area or in the scrollable area |
| [`CellRange`](Common/CellRange.cs) | struct | Immutable rectangle of cells from a top-left to a bottom-right position |
| [`CellResizeMode`](Common/CellResizeMode.cs) | enum | Flags that allow resizing a cell's height, width or both |
| [`ChangeActivePositionEventArgs`](Common/ChangeActivePositionEventArgs.cs) | class | Cancellable event arguments with the old and new focused positions |
| [`ChangeActivePositionEventHandler`](Common/ChangeActivePositionEventHandler.cs) | delegate | Handler for focus changes raised by a selection |
| [`ClipboardMode`](Common/ClipboardMode.cs) | enum | Flags that enable copy, cut, paste and delete through the clipboard |
| [`ColumnCancelEventArgs`](Common/ColumnCancelEventArgs.cs) | class | Column event arguments with a `Cancel` flag, used when focus leaves a column |
| [`ColumnCancelEventHandler`](Common/ColumnCancelEventHandler.cs) | delegate | Handler for cancellable column focus events |
| [`ColumnEventArgs`](Common/ColumnEventArgs.cs) | class | Event arguments that carry a column index, used when focus enters a column |
| [`ColumnEventHandler`](Common/ColumnEventHandler.cs) | delegate | Handler for column focus events |
| [`ColumnInfo`](Common/ColumnInfo.cs) | class | Width limits, visibility, auto-size mode and tag of one grid column |
| [`ColumnInfoCollection`](Common/ColumnInfoCollection.cs) | class | Columns of a grid stored as `ColumnInfo` objects |
| [`ColumnInfoEventArgs`](Common/ColumnInfoEventArgs.cs) | class | Event arguments that carry a `ColumnInfo` |
| [`ColumnInfoEventHandler`](Common/ColumnInfoEventHandler.cs) | delegate | Handler for column information events |
| [`ColumnsBase`](Common/ColumnsBase.cs) | abstract class | Base for column width, position and visibility management of a grid |
| [`ColumnsSimpleBase`](Common/ColumnsSimpleBase.cs) | abstract class | Column manager that gives every column the same width |
| [`ContextMenuStyle`](Common/ContextMenuStyle.cs) | enum | Flags that choose which built-in context menu items the grid shows |
| [`CustomScrollControl`](Common/CustomScrollControl.cs) | abstract class | Panel with its own horizontal and vertical scroll bars and scroll position events |
| [`CutMode`](Common/CutMode.cs) | enum | Whether a cut never removes its source cells, removes them on paste or removes them immediately |
| [`DisplayStringCellComparer`](Common/DisplayStringCellComparer.cs) | class | Compares static cells by their display text |
| [`EditableMode`](Common/EditableMode.cs) | enum | Flags that choose which user actions start editing a cell |
| [`EditingCellException`](Common/EditingCellException.cs) | class | Exception that wraps an error raised while editing a cell |
| [`ExceptionEventArgs`](Common/ExceptionEventArgs.cs) | class | Event arguments that carry an exception and a `Handled` flag |
| [`ExceptionEventHandler`](Common/ExceptionEventHandler.cs) | delegate | Handler for exception events |
| [`FocusStyle`](Common/FocusStyle.cs) | enum | Flags that control focus and selection behaviour when the grid gains or loses focus |
| [`GridDragEventHandler`](Common/GridDragEventHandler.cs) | delegate | Handler for grid drag-and-drop events |
| [`GridEventHandler`](Common/GridEventHandler.cs) | delegate | Handler for plain grid events |
| [`GridGiveFeedbackEventHandler`](Common/GridGiveFeedbackEventHandler.cs) | delegate | Handler for grid drag feedback events |
| [`GridKeyEventHandler`](Common/GridKeyEventHandler.cs) | delegate | Handler for grid key down and key up events |
| [`GridKeyPressEventHandler`](Common/GridKeyPressEventHandler.cs) | delegate | Handler for grid key press events |
| [`GridMouseEventHandler`](Common/GridMouseEventHandler.cs) | delegate | Handler for grid mouse events |
| [`GridSelectionMode`](Common/GridSelectionMode.cs) | enum | Whether the grid selects cells, rows, columns or nothing |
| [`GridSpecialKeys`](Common/GridSpecialKeys.cs) | enum | Flags for the navigation and editing keys the grid handles itself |
| [`IHiddenRowCoordinator`](Common/IHiddenRowCoordinator.cs) | interface | Contract that maps scroll bar values to visible rows when rows are hidden |
| [`IndexRangeEventArgs`](Common/IndexRangeEventArgs.cs) | class | Event arguments with a start index and a count |
| [`IndexRangeEventHandler`](Common/IndexRangeEventHandler.cs) | delegate | Handler for events about a range of row or column indexes |
| [`IRangeLoader`](Common/IRangeLoader.cs) | interface | Contract that returns a range of a grid, such as the whole grid or its fixed rows |
| [`IRows`](Common/IRows.cs) | interface | Contract for showing, hiding and testing the visibility of rows |
| [`LinkedControlScrollMode`](Common/LinkedControlScrollMode.cs) | enum | How a control linked to a cell scrolls with the grid |
| [`LinkedControlsList`](Common/LinkedControlsList.cs) | class | Controls linked to grid positions, kept with their parent control |
| [`LinkedControlValue`](Common/LinkedControlValue.cs) | class | A control linked to a grid position, with its scroll mode and border option |
| [`MultiColumnsComparer`](Common/MultiColumnsComparer.cs) | class | Compares rows by several columns in turn |
| [`Position`](Common/Position.cs) | struct | Immutable row and column position of a cell |
| [`PositionCollection`](Common/PositionCollection.cs) | class | List of cell positions |
| [`RangeCancelEventArgs`](Common/RangeCancelEventArgs.cs) | class | Range event arguments with a `Cancel` flag |
| [`RangeCancelEventHandler`](Common/RangeCancelEventHandler.cs) | delegate | Handler for cancellable cell range events |
| [`RangeCollection`](Common/RangeCollection.cs) | class | List of cell ranges |
| [`RangeData`](Common/RangeData.cs) | class | Values of a cell range for clipboard and drag-and-drop transfer, with cut handling |
| [`RangeEventArgs`](Common/RangeEventArgs.cs) | class | Event arguments that carry a `CellRange` |
| [`RangeEventHandler`](Common/RangeEventHandler.cs) | delegate | Handler for cell range events |
| [`RangeFixedCols`](Common/RangeFixedCols.cs) | class | Range loader that returns the fixed columns of a grid |
| [`RangeFixedRows`](Common/RangeFixedRows.cs) | class | Range loader that returns the fixed rows of a grid |
| [`RangeFullGrid`](Common/RangeFullGrid.cs) | class | Range loader that returns the whole grid |
| [`RangeFullGridNoFixedCols`](Common/RangeFullGridNoFixedCols.cs) | class | Range loader that returns the whole grid without its fixed columns |
| [`RangeFullGridNoFixedRows`](Common/RangeFullGridNoFixedRows.cs) | class | Range loader that returns the whole grid without its fixed rows |
| [`RangeLoader`](Common/RangeLoader.cs) | class | Range loader that returns a fixed custom range |
| [`RangePaintEventArgs`](Common/RangePaintEventArgs.cs) | class | Paint arguments with the grid, graphics cache and range being drawn |
| [`RangePaintEventHandler`](Common/RangePaintEventHandler.cs) | delegate | Handler for range paint events |
| [`RangeRegion`](Common/RangeRegion.cs) | class | Set of non-overlapping cell ranges, used for selections |
| [`RangeRegionCancelEventArgs`](Common/RangeRegionCancelEventArgs.cs) | class | Range region event arguments with a `Cancel` flag |
| [`RangeRegionCancelEventHandler`](Common/RangeRegionCancelEventHandler.cs) | delegate | Handler for cancellable range region events |
| [`RangeRegionChangedEventArgs`](Common/RangeRegionChangedEventArgs.cs) | class | Arguments with the ranges added to and removed from a range region |
| [`RangeRegionChangedEventHandler`](Common/RangeRegionChangedEventHandler.cs) | delegate | Handler for range region change events such as selection changes |
| [`RangeRegionChangingEventArgs`](Common/RangeRegionChangingEventArgs.cs) | class | Arguments with the current region and the regions about to be included and excluded |
| [`RangeRegionChangingEventHandler`](Common/RangeRegionChangingEventHandler.cs) | delegate | Handler for range region changing events |
| [`RangeRegionEventArgs`](Common/RangeRegionEventArgs.cs) | class | Event arguments that carry a `RangeRegion` |
| [`RangeRegionEventHandler`](Common/RangeRegionEventHandler.cs) | delegate | Handler for range region events |
| [`RowCancelEventArgs`](Common/RowCancelEventArgs.cs) | class | Row event arguments with a `Cancel` flag, used when focus leaves a row |
| [`RowCancelEventHandler`](Common/RowCancelEventHandler.cs) | delegate | Handler for cancellable row focus events |
| [`RowEventArgs`](Common/RowEventArgs.cs) | class | Event arguments that carry a row index, used when focus enters a row |
| [`RowEventHandler`](Common/RowEventHandler.cs) | delegate | Handler for row focus events |
| [`RowInfo`](Common/RowInfo.cs) | class | Height, visibility, auto-size mode and tag of one grid row |
| [`RowInfoCollection`](Common/RowInfoCollection.cs) | abstract class | Rows of a grid stored as `RowInfo` objects |
| [`RowInfoCollectoinHiddenRowCoordinator`](Common/RowInfoCollectoinHiddenRowCoordinator.cs) | class | Hidden row coordinator that tracks row visibility changes of a `RowInfoCollection` |
| [`RowInfoEventArgs`](Common/RowInfoEventArgs.cs) | class | Event arguments that carry a `RowInfo` |
| [`RowInfoEventHandler`](Common/RowInfoEventHandler.cs) | delegate | Handler for row information events |
| [`RowsBase`](Common/RowsBase.cs) | abstract class | Base for row height, position and visibility management of a grid |
| [`RowsSimpleBase`](Common/RowsSimpleBase.cs) | abstract class | Row manager that gives every row the same height |
| [`RowVisibilityChangedHandler`](Common/RowVisibilityChangedHandler.cs) | delegate | Handler called with a row index when that row is shown or hidden |
| [`ScrollPositionChangedEventArgs`](Common/ScrollPositionChangedEventArgs.cs) | class | Old value, new value and delta of a scroll bar change |
| [`ScrollPositionChangedEventHandler`](Common/ScrollPositionChangedEventHandler.cs) | delegate | Handler for scroll position changes |
| [`SelectionBorderMode`](Common/SelectionBorderMode.cs) | enum | Which part of the selection gets a border, such as the focus range, the focus cell or a single range |
| [`SelectionChangeEventArgs`](Common/SelectionChangeEventArgs.cs) | class | Event arguments with a selection change type and its range |
| [`SelectionChangeEventHandler`](Common/SelectionChangeEventHandler.cs) | delegate | Handler for selection change events |
| [`SelectionChangeEventType`](Common/SelectionChangeEventType.cs) | enum | Whether a selection change adds, removes or clears ranges |
| [`SelectionMaskStyle`](Common/SelectionMaskStyle.cs) | enum | Flags that control how the selection mask is drawn over cells |
| [`SortRangeRowsEventArgs`](Common/SortRangeRowsEventArgs.cs) | class | Arguments that request sorting the rows of a range by a key column and comparer |
| [`SortRangeRowsEventHandler`](Common/SortRangeRowsEventHandler.cs) | delegate | Handler for range row sort events |
| [`SourceGridException`](Common/SourceGridException.cs) | class | Base exception of the grid |
| [`StandardHiddenRowCoordinator`](Common/StandardHiddenRowCoordinator.cs) | class | Default hidden row coordinator that skips hidden row ranges while scrolling |
| [`TrimmingMode`](Common/TrimmingMode.cs) | enum | Whether cell text is trimmed by character, by word or not at all |
| [`ValidatingCellEventArgs`](Common/ValidatingCellEventArgs.cs) | class | Cancellable cell event arguments with the proposed new value |
| [`ValidatingCellEventHandler`](Common/ValidatingCellEventHandler.cs) | delegate | Handler for cell validation events |
| [`ValueCellComparer`](Common/ValueCellComparer.cs) | class | Compares static cells by their values |
| [`ValueChangeEventArgs`](Common/ValueChangeEventArgs.cs) | class | Event arguments with the old and new value of a cell |
| [`ValueChangeEventHandler`](Common/ValueChangeEventHandler.cs) | delegate | Handler for cell value change events |

### Decorators

| Type | Kind | Purpose |
|---|---|---|
| [`DecoratorBase`](Decorators/DecoratorBase.cs) | abstract class | Base for a drawing layer that paints over the cell ranges it intersects |
| [`DecoratorHighlight`](Decorators/DecoratorHighlight.cs) | class | Decorator that holds a highlight range and does not draw anything yet |
| [`DecoratorList`](Decorators/DecoratorList.cs) | class | Ordered list of decorators a grid draws after its cells |
| [`DecoratorSelection`](Decorators/DecoratorSelection.cs) | class | Decorator that draws a selection's mask and border |

### Exporter

| Type | Kind | Purpose |
|---|---|---|
| [`CSV`](Exporter/CSV.cs) | class | Writes a grid's display text as CSV with configurable field and line separators |
| [`ExportHTMLMode`](Exporter/ExportHTMLMode.cs) | enum | Flags that control which features an HTML export writes, such as embedded images |
| [`GridPrintDocument`](Exporter/GridPrintDocument.cs) | class | Print document that pages a grid range with title, header, footer and repeated fixed rows |
| [`HTML`](Exporter/HTML.cs) | class | Writes a grid as an HTML table, optionally exporting cell images to files |
| [`Image`](Exporter/Image.cs) | class | Draws a grid range to a bitmap or to a supplied `Graphics` |

### Grids

| Type | Kind | Purpose |
|---|---|---|
| [`Grid`](Grids/Grid.cs) | class | Grid control that stores a static cell at each position, with spanned cells and sorting |
| [`GridColumn`](Grids/GridColumn.cs) | class | Column of a `Grid` that indexes its cells by row |
| [`GridColumns`](Grids/GridColumns.cs) | class | Column collection of a `Grid` that inserts, removes and counts columns |
| [`GridRow`](Grids/GridRow.cs) | class | Row of a `Grid` that indexes its cells by column |
| [`GridRows`](Grids/GridRows.cs) | class | Row collection of a `Grid` that inserts, removes, swaps and counts rows |
| [`GridVirtual`](Grids/GridVirtual.cs) | abstract class | Abstract grid control over virtual data, with selection, editing, scrolling, decorators and clipboard support |
| [`ISpannedCellRangesController`](Grids/ISpannedCellRangesController.cs) | interface | Contract that keeps spanned cell ranges consistent as rows and columns move |
| [`ISpannedRangesCollection`](Grids/ISpannedRangesCollection.cs) | interface | Contract for storing and querying the ranges of spanned cells |
| [`OverlappingCellException`](Grids/OverlappingCellException.cs) | class | Raised when a spanned cell would overlap another spanned cell |
| [`QuadTreeRangesList`](Grids/QuadTreeRangesList.cs) | class | Spanned range collection indexed by a quad tree for fast lookup |
| [`RangeNotFoundException`](Grids/RangeNotFoundException.cs) | class | Raised when a spanned range is not found |
| [`SpannedCellRangesController`](Grids/SpannedCellRangesController.cs) | class | Default controller that updates, moves and removes spanned ranges of a `Grid` |
| [`SpannedRangesList`](Grids/SpannedRangesList.cs) | class | Spanned range collection that scans a simple list |

### Selection

| Type | Kind | Purpose |
|---|---|---|
| [`ColumnSelection`](Selection/ColumnSelection.cs) | class | Selection that always selects whole columns |
| [`DisabledSelection`](Selection/DisabledSelection.cs) | class | Selection that never selects anything |
| [`FreeSelection`](Selection/FreeSelection.cs) | class | Selection of arbitrary cell ranges |
| [`IGridSelection`](Selection/IGridSelection.cs) | interface | Contract for a grid selection with its active position, focus movement, selection region and focus events |
| [`RangeComparerByRows`](Selection/RangeComparerByRows.cs) | class | Orders cell ranges by their start row |
| [`RangeMergerByCells`](Selection/RangeMergerByCells.cs) | class | Merges adjacent ranges that differ only by rows or columns |
| [`RangeMergerByRows`](Selection/RangeMergerByRows.cs) | class | Keeps a list of row ranges and merges them when they touch |
| [`RowSelection`](Selection/RowSelection.cs) | class | Selection that always selects whole rows |
| [`SelectionBase`](Selection/SelectionBase.cs) | abstract class | Base selection that tracks the active position, focus events and the selected region |

### Utils

| Type | Kind | Purpose |
|---|---|---|
| [`IPerformanceCounter`](Utils/IPerformanceCounter.cs) | interface | Disposable stopwatch contract that reports elapsed seconds and milliseconds |
| [`PerformanceCounter`](Utils/PerformanceCounter.cs) | class | `IPerformanceCounter` that measures time elapsed since construction with `DateTime.Now` |
