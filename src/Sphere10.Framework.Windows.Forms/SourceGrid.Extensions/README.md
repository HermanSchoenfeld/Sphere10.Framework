# SourceGrid Extensions

Ready-made grids built on SourceGrid for two-dimensional arrays, `IBoundList` data binding, read-only property sources, list editing and appointment planning.

Each grid derives from `GridVirtual` in [SourceGrid](../SourceGrid/README.md) and supplies its own rows, columns, value models and cell controller. `ArrayGrid` edits a two-dimensional `System.Array` in place. `DataGrid` binds an `IBoundList`, such as a `BoundDataView` from the Data project, with one `DataGridColumn` per property, row editing, delete-key handling and `Conditions` that swap cells or views per row. `ConditionBuilder.AlternateView` builds the common alternating row colour condition. `PingGrid`, in `PingGrids`, reads values on demand from an `IPingData` source by row index and property name and sorts through the source. It is read-only, and its value model throws on `SetValue`. `ListPingSource<T>` adapts a list to it.

`ListEditor` is a user control that edits an `IList` through reflected property columns with add, remove, refresh and move commands. `PlanningGrid` lays `IAppointment` items across day columns and time-slot rows and raises `AppointmentClick` and `AppointmentDoubleClick`. The planning types use the `SourceGrid.Planning` namespace. For the data-source-driven grid that most screens use, see [VisualGrid](../VisualGrid/README.md). For a column-based scheduling control, see [AppointmentBook](../AppointmentBook/README.md).

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`AppointmentBase`](AppointmentBase.cs) | class | Basic `IAppointment` with title, start, end and an optional cell view. |
| [`AppointmentCollection`](AppointmentCollection.cs) | class | `List<IAppointment>` holding the appointments shown by a `PlanningGrid`. |
| [`AppointmentEventArgs`](AppointmentEventArgs.cs) | class | Event data with the clicked time range and the appointment, if any. |
| [`ArrayColumnHeader`](ArrayColumnHeader.cs) | class | Virtual column header cell for an `ArrayGrid`. |
| [`ArrayColumnHeaderModel`](ArrayColumnHeaderModel.cs) | class | Value model that shows the zero-based array column index as the header caption. |
| [`ArrayColumns`](ArrayColumns.cs) | class | Column collection of an `ArrayGrid`. |
| [`ArrayGrid`](ArrayGrid.cs) | class | Virtual grid that displays and edits a two-dimensional `System.Array`. |
| [`ArrayHeader`](ArrayHeader.cs) | class | Virtual top-left header cell for an `ArrayGrid`. |
| [`ArrayRowHeader`](ArrayRowHeader.cs) | class | Virtual row header cell for an `ArrayGrid`. |
| [`ArrayRowHeaderModel`](ArrayRowHeaderModel.cs) | class | Value model that shows the zero-based array row index as the header caption. |
| [`ArrayRows`](ArrayRows.cs) | class | Row collection whose count follows the first dimension of the bound array. |
| [`ArrayValueModel`](ArrayValueModel.cs) | class | Value model that reads and writes array elements and raises the grid's value-change notifications. |
| [`CellAppointment`](CellAppointment.cs) | class | Planning grid cell that shows one appointment. |
| [`CellEmpty`](CellEmpty.cs) | class | Planning grid cell for an empty time slot with its start and end. |
| [`DataGrid`](DataGrid.cs) | class | Virtual grid bound to an `IBoundList` with row editing, deletion, selection and conditional cells. |
| [`DataGridCellController`](DataGridCellController.cs) | class | Cell controller that starts and tracks row edits on the bound list. |
| [`DataGridColumn`](DataGridColumn.cs) | class | `DataGrid` column with its property, header cell, data cell and conditions. |
| [`DataGridColumns`](DataGridColumns.cs) | class | `DataGrid` column collection that maps grid columns to bound properties and adds columns by property. |
| [`DataGridRowHeaderModel`](DataGridRowHeaderModel.cs) | class | Value model for `DataGrid` row headers that shows `*` on the new row when the list allows adding. |
| [`DataGridRows`](DataGridRows.cs) | class | `DataGrid` row collection whose count follows the bound list and maps grid rows to source indexes. |
| [`DataGridValueModel`](DataGridValueModel.cs) | class | Value model that reads and writes a bound property of a `DataGrid` row. |
| [`EndEditingException`](EndEditingException.cs) | class | `SourceGridException` reported through `OnUserException` when ending a row edit fails. |
| [`Header00`](Header00.cs) | class | Top-left header cell of a `PlanningGrid`. |
| [`HeaderDay1`](HeaderDay1.cs) | class | `PlanningGrid` column header cell for the date. |
| [`HeaderDay2`](HeaderDay2.cs) | class | `PlanningGrid` column header cell for the day name. |
| [`HeaderHour1`](HeaderHour1.cs) | class | `PlanningGrid` row header cell for the hour. |
| [`HeaderHour2`](HeaderHour2.cs) | class | `PlanningGrid` row header cell for minutes within the hour. |
| [`IAppointment`](IAppointment.cs) | interface | Appointment contract with title, start, end and the cell view used to draw it. |
| [`ListEditor`](ListEditor.cs) | class | User control that edits an `IList` through reflected property columns with add, remove, refresh and move commands. |
| [`PlanningGrid`](PlanningGrid.cs) | class | User control that shows appointments across day columns and configurable time slots. |

### Cells/DataGrid

| Type | Kind | Purpose |
|---|---|---|
| [`Cell`](Cells/DataGrid/Cell.cs) | class | Default `DataGrid` data cell with a `Create` factory that returns a check box for `bool` and an editor-backed cell otherwise. |
| [`CheckBox`](Cells/DataGrid/CheckBox.cs) | class | `DataGrid` check box cell bound through `DataGridValueModel`. |
| [`ColumnHeader`](Cells/DataGrid/ColumnHeader.cs) | class | `DataGrid` column header cell with a fixed caption. |
| [`Header`](Cells/DataGrid/Header.cs) | class | `DataGrid` top-left header cell. |
| [`Image`](Cells/DataGrid/Image.cs) | class | `DataGrid` image cell bound through `DataGridValueModel`. |
| [`Link`](Cells/DataGrid/Link.cs) | class | `DataGrid` link cell bound through `DataGridValueModel`. |
| [`RowHeader`](Cells/DataGrid/RowHeader.cs) | class | `DataGrid` row selector cell. |

### Conditions

| Type | Kind | Purpose |
|---|---|---|
| [`ConditionBuilder`](Conditions/ConditionBuilder.cs) | static class | Builds an alternating-row condition that applies a cloned view with other colours to odd rows. |
| [`ConditionCell`](Conditions/ConditionCell.cs) | class | Condition that replaces a column's cell when its evaluate function returns `true`. |
| [`ConditionView`](Conditions/ConditionView.cs) | class | Condition that replaces a cell's view when its evaluate function returns `true`. |
| [`ICondition`](Conditions/ICondition.cs) | interface | Per-row rule that evaluates a `DataGrid` row and returns the cell to use. |

### PingGrids

| Type | Kind | Purpose |
|---|---|---|
| [`EmptyPingSource`](PingGrids/EmptyPingSource.cs) | class | Empty `IPingData` used before a `PingGrid` receives a source. |
| [`IPingData`](PingGrids/IPingData.cs) | interface | Read-only row source for `PingGrid` with count, value lookup by index and property, and sorting. |
| [`IPropertyResolver`](PingGrids/IPropertyResolver.cs) | interface | Extension point for reading a property path from an item. |
| [`ListPingSource<T>`](PingGrids/ListPingSource.cs) | class | `List<T>` that implements `IPingData` with reflected property reads and property sorting. |
| [`PingGrid`](PingGrids/PingGrid.cs) | class | Read-only virtual grid that pulls cell values from an `IPingData` source on demand. |
| [`PingGridCellController`](PingGrids/PingGridCellController.cs) | class | Cell controller for `PingGrid` cells that passes edit and value-change events to the base controller. |
| [`PingGridColumn`](PingGrids/PingGridColumn.cs) | class | `PingGrid` column with its property name, header cell and data cell. |
| [`PingGridColumns`](PingGrids/PingGridColumns.cs) | class | `PingGrid` column collection that adds columns by property. |
| [`PingGridRowHeaderModel`](PingGrids/PingGridRowHeaderModel.cs) | class | Row header value model in the `PingGrids` namespace whose `GetValue` casts the grid to `DataGrid`. |
| [`PingGridRows`](PingGrids/PingGridRows.cs) | class | `PingGrid` row collection whose count follows the source and maps grid rows to source indexes. |
| [`PingGridValueModel`](PingGrids/PingGridValueModel.cs) | class | Value model that reads a source value by row index and property name and rejects writes. |
| [`ReflectionPropertyResolver`](PingGrids/ReflectionPropertyResolver.cs) | class | `IPropertyResolver` that reads public properties by reflection, with a shared instance. |

### PingGrids/Cells

| Type | Kind | Purpose |
|---|---|---|
| [`Cell`](PingGrids/Cells/Cell.cs) | class | Default `PingGrid` data cell with a `Create` factory that returns a check box for `bool` and an editor-backed cell otherwise. |
| [`CheckBox`](PingGrids/Cells/CheckBox.cs) | class | `PingGrid` check box cell bound through `PingGridValueModel`. |
| [`ColumnHeader`](PingGrids/Cells/ColumnHeader.cs) | class | `PingGrid` column header cell with a fixed caption. |
| [`Header`](PingGrids/Cells/Header.cs) | class | `PingGrid` top-left header cell. |
| [`Image`](PingGrids/Cells/Image.cs) | class | `PingGrid` image cell bound through `PingGridValueModel`. |
| [`Link`](PingGrids/Cells/Link.cs) | class | `PingGrid` link cell bound through `PingGridValueModel`. |
| [`RowHeader`](PingGrids/Cells/RowHeader.cs) | class | `PingGrid` row selector cell, which uses `DataGridRowHeaderModel` and therefore expects a `DataGrid`. |
