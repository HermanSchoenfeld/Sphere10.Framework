# Appointment Book

Scheduling grid control that shows appointments in named columns over a time period, with selection, drag-and-drop rescheduling and resizing.

`AppointmentBook` is a [SourceGrid](../SourceGrid/README.md) based user control. Assign an `IAppointmentBookDataSource`, usually `AppointmentBookDataSource`, and choose the period with `SetTimePeriod`. A `DataSourceToViewModelConverter` turns the data source's columns and `Appointment` records into an `AppointmentBookViewModel` of `ColumnViewModel`, `AppointmentViewModel` and `CellViewModel` rows. The control then paints those cells.

The control raises typed events for free-region selection, appointment selection, dragging, dropping and resizing. The `*Starting` and `ResizingFinished` events expose `Cancel` and a message so a handler can veto the change. `UnallocatedBook` is a single-column variant for appointments not yet assigned to a column.

The book draws with the same SourceGrid cells, views and borders as the SourceGrid `PlanningGrid`. Free time slots use the standard cell grid lines (`ViewBase.DefaultBorder`), and the selected appointment uses the grid's selection border. The header row shows the date and weekday above the time column, or the month and year in the monthly view, and whole-hour rows read as `08:00`. Rows follow the grid font, and resource columns stretch to fill the control. While `SoftenAppointmentColors` is true, the default, each appointment colour becomes a pastel fill with bold text and an outline in darker shades of the same hue. Override `GetAppointmentFillColor`, `GetAppointmentTextColor` or `GetAppointmentBorderLine` to change that rule, or set the property to false to fill cells with the appointment colour unchanged.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`TimePeriodType`](TimePeriodType.cs) | enum | Row granularity of the book: hourly, half-hourly, quarter-hourly or monthly |

### CellControllers

| Type | Kind | Purpose |
|---|---|---|
| [`ResizedAppointmentBorder`](CellControllers/ResizedAppointmentBorder.cs) | enum | Top or bottom edge being dragged while resizing an appointment |

### CellViews

| Type | Kind | Purpose |
|---|---|---|
| [`SuperscriptRowHeaderCellView`](CellViews/SuperscriptRowHeaderCellView.cs) | class | SourceGrid row header view with a small top-centred font for time labels. The book's own time column uses the standard row header |

### Controls

| Type | Kind | Purpose |
|---|---|---|
| [`AppointmentBook`](Controls/AppointmentBook.cs) | class | Appointment grid bound to an `IAppointmentBookDataSource` that raises selection, drag, drop and resize events |
| [`BaseAppointmentBook`](Controls/BaseAppointmentBook.cs) | class | Grid user control base with cell selection, dragging and raw mouse and click events by column and row |
| [`UnallocatedBook`](Controls/UnallocatedBook.cs) | class | Single-column appointment book for unallocated appointments that disables resizing |

### DataSources

| Type | Kind | Purpose |
|---|---|---|
| [`Appointment`](DataSources/Appointment.cs) | class | Appointment record with ID, subject, two location lines, notes, start and end times, color and user object |
| [`AppointmentBookDataSource`](DataSources/AppointmentBookDataSource.cs) | class | In-memory data source that stores columns and appointments and checks time availability |
| [`AppointmentColumn`](DataSources/AppointmentColumn.cs) | class | Named book column with an application user object |
| [`IAppointmentBookDataSource`](DataSources/IAppointmentBookDataSource.cs) | interface | Data source contract for time range, columns, appointments, create, delete, reschedule and availability |
| [`UnallocatedColumn`](DataSources/UnallocatedColumn.cs) | class | Column named "Unallocated" used by `UnallocatedBook` |

### Events

| Type | Kind | Purpose |
|---|---|---|
| [`AppointmentBookEvent`](Events/AppointmentBookEvent.cs) | class | Base event data carrying the source `AppointmentBook` |
| [`AppointmentBookFreeRegionSelected`](Events/AppointmentBookFreeRegionSelected.cs) | class | Column, start and end time of a selected empty region |
| [`AppointmentColumnEvent`](Events/AppointmentColumnEvent.cs) | class | Event data that adds the source column |
| [`AppointmentDraggedEvent`](Events/AppointmentDraggedEvent.cs) | class | Notification after an appointment drag completes |
| [`AppointmentDraggingEvent`](Events/AppointmentDraggingEvent.cs) | class | Destination column, times and compatibility while an appointment is dragged |
| [`AppointmentDragStartingEvent`](Events/AppointmentDragStartingEvent.cs) | class | Cancellable notification before an appointment drag starts |
| [`AppointmentDropEvent`](Events/AppointmentDropEvent.cs) | class | Target column, start and end time of a dropped appointment |
| [`AppointmentDropStartingEvent`](Events/AppointmentDropStartingEvent.cs) | class | Cancellable drop notification whose handler can change the target column |
| [`AppointmentEvent`](Events/AppointmentEvent.cs) | class | Event data that adds the affected `Appointment` |
| [`AppointmentResizingEvent`](Events/AppointmentResizingEvent.cs) | class | Selected start and end time while an appointment is resized |
| [`AppointmentResizingFinishedEvent`](Events/AppointmentResizingFinishedEvent.cs) | class | Cancellable notification with a message when a resize ends |

### ViewModels

| Type | Kind | Purpose |
|---|---|---|
| [`AppointmentBookViewModel`](ViewModels/AppointmentBookViewModel.cs) | class | Cell layout of columns and appointments for a time period, with a column filter |
| [`AppointmentBookViewModelFilter`](ViewModels/AppointmentBookViewModelFilter.cs) | enum | Shows all columns, excludes empty columns or shows only empty columns |
| [`AppointmentViewModel`](ViewModels/AppointmentViewModel.cs) | class | Visible span, color and rendered text lines of one appointment |
| [`BaseAppointmentBookViewModel`](ViewModels/BaseAppointmentBookViewModel.cs) | abstract class | Time period state that rebuilds cell displays when the period or granularity changes |
| [`CellTraits`](ViewModels/CellTraits.cs) | enum | Flags that mark a cell as empty, filled, edge, top, bottom or interior |
| [`CellViewModel`](ViewModels/CellViewModel.cs) | class | Text, colors, font style, alignment and traits of one grid cell |
| [`ColumnViewModel`](ViewModels/ColumnViewModel.cs) | class | Name, index and appointments of one displayed column |
| [`DataSourceToUnallocatedViewModelConverter`](ViewModels/DataSourceToUnallocatedViewModelConverter.cs) | class | Converts a data source into an `UnallocatedBookViewModel` with compact appointment text |
| [`DataSourceToViewModelConverter`](ViewModels/DataSourceToViewModelConverter.cs) | class | Converts a data source into an `AppointmentBookViewModel` and renders appointment cell text by size |
| [`IAppointmentBookViewModel`](ViewModels/IAppointmentBookViewModel.cs) | interface | Declares `SetTimePeriod` for a view model |
| [`UnallocatedBookViewModel`](ViewModels/UnallocatedBookViewModel.cs) | class | View model with one unallocated column of appointments |
