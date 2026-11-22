# Visual Calendar

Scheduling grid control that shows appointments in named columns over a day, a week or a month, with selection, drag-and-drop rescheduling and resizing, a data source that shows `ICalendar` objects and an editor for recurrence rules.

`VisualCalendar` is a [SourceGrid](../SourceGrid/README.md) based user control. Assign an `IVisualCalendarDataSource`, usually `VisualCalendarDataSource`, and choose the period with `SetView(view, date)` or `SetTimePeriod`. The `View` property is a `VisualCalendarView`. `Day` has a column for each of the data source's columns and a row for each time slot, `Week` has a column for each day and `Month` has a column for each of the data source's columns and a row for each day. A week is shown from its first day (`FirstDayOfWeek`, the current culture's by default), whichever of its days was asked for. `SetTimePeriod` and `SetTimePeriodType` choose the row size (`TimePeriodType`), where the monthly size shows the month view and the other sizes keep the day or week in view. A `DataSourceToViewModelConverter` turns the data source's columns and `Appointment` records into an `VisualCalendarViewModel` of `ColumnViewModel`, `AppointmentViewModel` and `CellViewModel` rows. The control then paints those cells.

`RefreshFromDataSource` and `SetView` read the data source and lay the period out on the calling thread. `RefreshFromDataSourceAsync` and `SetViewAsync` read it and build the view model on a worker, then bind the result on the UI thread, so a data source that computes many appointments does not hold up the window. Call them on the UI thread. The period properties change at once and the calendar keeps showing what it shows until the new layout is ready. Only the latest refresh is shown: a later asynchronous or synchronous refresh, period change or data source supersedes one still running, and a cancelled one leaves the calendar as it was. A data source read this way must allow its reads from another thread while the UI does not change it; `CalendarViewDataSource` does, and also allows its period to change and its calendars to load meanwhile.

The control raises typed events for free-region selection, appointment selection, dragging, dropping and resizing. The `*Starting` and `ResizingFinished` events expose `Cancel` and a message so a handler can veto the change. `UnallocatedCalendar` is a single-column variant for appointments not yet assigned to a column.

In the week view the appointments of every data source column share their day's column, and those that overlap go to further columns of that day. Each day column is a `DayColumn`, which stands for a day and not for a column of the data source. The control therefore reports each appointment's own column in its events and to the data source, and an appointment dropped on a day keeps its column while its day and time change. A free region selected in a week reports the `DayColumn`, whose `Date` is the day. A week shares the control's width between its columns, so that all seven days are in sight.

The book draws with the same SourceGrid cells, views and borders as the SourceGrid `PlanningGrid`. Free time slots use the standard cell grid lines (`ViewBase.DefaultBorder`), and the selected appointment uses the grid's selection border. The header row shows the date and weekday above the time column, the first and last days of the week in the week view, or the month and year in the monthly view, and whole-hour rows read as `08:00`. Rows follow the grid font, and resource columns stretch to fill the control. While `SoftenAppointmentColors` is true, the default, each appointment colour becomes a pastel fill with bold text and an outline in darker shades of the same hue. Override `GetAppointmentFillColor`, `GetAppointmentTextColor` or `GetAppointmentBorderLine` to change that rule, or set the property to false to fill cells with the appointment colour unchanged.

`Style` chooses the look of appointments and free time slots from `VisualCalendarStyle`, and changing it redraws the calendar. Every style derives its colours from the appointment's own colour. `Pastel`, the default, is the look described above, and `SoftenAppointmentColors` applies to it only. `Solid` fills with the appointment colour, picks black or white text for contrast and draws a darker outline. `Outline` fills with the window colour under a heavy outline, with text in the appointment colour. `Minimal` uses a pastel fill without an outline, over faint dotted hour lines and no column lines. For a look of your own, also override `GetFreeSlotHorizontalLine` and `GetFreeSlotVerticalLine`.

## Calendars of recurring events

`CalendarViewDataSource` shows the occurrences of one or more [`ICalendar`](../../Sphere10.Framework/Calendar/README.md) objects, such as a person's calendars, a job schedule or an operating system's tasks, with a column for each calendar. The control's Day, Week and Month views apply as they do to any data source. Occurrences of one calendar that overlap are placed side by side in extra columns, so nothing hides anything else. Call `LoadAsync` to read the calendars, then `SetView` or `RefreshFromDataSource` on the control, or `SetViewAsync` or `RefreshFromDataSourceAsync` when the calendars hold many recurring events, since every occurrence in the period becomes a block. A layout computed while the period changed or the calendars loaded again is not kept. Set `SlotDuration` to the view's row size so that blocks start and end on rows. A moment without duration is drawn one row high so that it can be seen and dragged.

The source never changes a calendar itself. A drag or resize in the view raises `RescheduleRequested` with a `CalendarRescheduleRequest`, which names the calendar, the calendar the occurrence was dropped on, the occurrence as it stands and its real new times. The handler applies the change to the calendar, with any confirmation it wants, and loads the source again. `GetItem(appointment)` returns the `CalendarViewItem` behind a displayed block. `NewAppointment` and `DeleteAppointment` throw `NotSupportedException`, because events are added and removed through their calendar. Override `Includes` to hide events and `GetColor` to colour them. By default an event without a colour takes its calendar's, and a disabled event is drawn in gray.

```csharp
var source = new CalendarViewDataSource(personalCalendar, systemCalendar) { SlotDuration = TimeSpan.FromMinutes(30) };
source.RescheduleRequested += async request => {
	// The view only asks. Move the event in its calendar and show the calendars again.
	var calendarEvent = request.Occurrence.Event;
	await request.Calendar.UpdateAsync(calendarEvent, calendarEvent.Reschedule(request.Occurrence, request.NewStart, request.NewEnd));
	await source.LoadAsync();
	await _visualCalendar.RefreshFromDataSourceAsync();
};
await source.LoadAsync();
_visualCalendar.DataSource = source;
_visualCalendar.SetTimePeriodType(TimePeriodType.DailyHalfHourly);
await _visualCalendar.SetViewAsync(VisualCalendarView.Week, DateTime.Today);
```

## Editing a recurrence rule

`CalendarRecurrenceEditor` is a user control that edits a `CalendarRecurrence`. It covers how the rule repeats, when it starts and ends, its interval, weekdays, days of the month, weeks of the month and months, and whether each occurrence repeats within itself. Only the choices that apply to the chosen frequency are enabled, and the control shows the rule in words, or what is wrong with it, as it is edited. Choices the frequency does not use are kept while editing, so switching frequency and back loses nothing, and they are left out of the rule that is read. Set `Recurrence` to show a rule and read it back for the edited one. Reading it does not validate it, so call `ValidateRecurrence()`, which returns a `Result` with everything that keeps the rule from occurring, including days of the month that are not numbers. `RecurrenceChanged` is raised after the user changed the rule.

```csharp
_recurrenceEditor.Recurrence = CalendarRecurrence.Weekly(DateTime.Today.AddHours(9), CalendarDaysOfWeek.Weekdays);

// Later, when the user accepts
var validation = _recurrenceEditor.ValidateRecurrence();
if (validation.IsSuccess)
	changed = calendarEvent with { Recurrence = _recurrenceEditor.Recurrence };
```

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`TimePeriodType`](TimePeriodType.cs) | enum | Row granularity of the book: hourly, half-hourly, quarter-hourly or monthly |
| [`VisualCalendarStyle`](VisualCalendarStyle.cs) | enum | Look of appointments and free time slots: `Pastel`, `Solid`, `Outline` or `Minimal` |
| [`VisualCalendarView`](VisualCalendarView.cs) | enum | Period the calendar shows and how it lays it out: `Day`, `Week` or `Month` |

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
| [`VisualCalendar`](Controls/VisualCalendar.cs) | class | Appointment grid bound to an `IVisualCalendarDataSource` that shows a day, week or month (`View`, `SetView`, `SetViewAsync`, `FirstDayOfWeek`), lays it out on a worker with `RefreshFromDataSourceAsync` and raises selection, drag, drop and resize events |
| [`BaseVisualCalendar`](Controls/BaseVisualCalendar.cs) | class | Grid user control base with cell selection, dragging, raw mouse and click events by column and row, and the `Style` that draws appointments and free slots |
| [`CalendarRecurrenceEditor`](Controls/CalendarRecurrenceEditor.cs) | class | User control that edits a `CalendarRecurrence` through `Recurrence`, with `ValidateRecurrence()` and the `RecurrenceChanged` event |
| [`UnallocatedCalendar`](Controls/UnallocatedCalendar.cs) | class | Single-column visual calendar for unallocated appointments that disables resizing |

### DataSources

| Type | Kind | Purpose |
|---|---|---|
| [`Appointment`](DataSources/Appointment.cs) | class | Appointment record with ID, subject, two location lines, notes, start and end times, color and user object |
| [`VisualCalendarDataSource`](DataSources/VisualCalendarDataSource.cs) | class | In-memory data source that stores columns and appointments and checks time availability |
| [`AppointmentColumn`](DataSources/AppointmentColumn.cs) | class | Named book column with an application user object and an optional `Date` for a column that shows its own day |
| [`CalendarRescheduleRequest`](DataSources/CalendarRescheduleRequest.cs) | record | The user's request, made by dragging or resizing, to give an occurrence new times, with its calendar and the calendar it was dropped on |
| [`CalendarViewDataSource`](DataSources/CalendarViewDataSource.cs) | class | Data source that shows the occurrences of `ICalendar` objects, one column per calendar with overlapping occurrences side by side, and raises `RescheduleRequested` |
| [`CalendarViewItem`](DataSources/CalendarViewItem.cs) | record | One displayed occurrence and the calendar it came from |
| [`DayColumn`](DataSources/DayColumn.cs) | class | Column of the week view that stands for a day instead of a column of the data source |
| [`IVisualCalendarDataSource`](DataSources/IVisualCalendarDataSource.cs) | interface | Data source contract for time range, columns, appointments, create, delete, reschedule and availability |
| [`UnallocatedColumn`](DataSources/UnallocatedColumn.cs) | class | Column named "Unallocated" used by `UnallocatedCalendar` |

### Events

| Type | Kind | Purpose |
|---|---|---|
| [`VisualCalendarEvent`](Events/VisualCalendarEvent.cs) | class | Base event data carrying the source `VisualCalendar` |
| [`VisualCalendarFreeRegionSelected`](Events/VisualCalendarFreeRegionSelected.cs) | class | Column, start and end time of a selected empty region |
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
| [`VisualCalendarViewModel`](ViewModels/VisualCalendarViewModel.cs) | class | Cell layout of columns and appointments for a time period, with a column filter |
| [`VisualCalendarViewModelFilter`](ViewModels/VisualCalendarViewModelFilter.cs) | enum | Shows all columns, excludes empty columns or shows only empty columns |
| [`AppointmentViewModel`](ViewModels/AppointmentViewModel.cs) | class | Visible span, color and rendered text lines of one appointment |
| [`BaseVisualCalendarViewModel`](ViewModels/BaseVisualCalendarViewModel.cs) | abstract class | Time period state that rebuilds cell displays when the period or granularity changes |
| [`CellTraits`](ViewModels/CellTraits.cs) | enum | Flags that mark a cell as empty, filled, edge, top, bottom or interior |
| [`CellViewModel`](ViewModels/CellViewModel.cs) | class | Text, colors, font style, alignment and traits of one grid cell |
| [`ColumnViewModel`](ViewModels/ColumnViewModel.cs) | class | Name, index and appointments of one displayed column |
| [`DataSourceToUnallocatedViewModelConverter`](ViewModels/DataSourceToUnallocatedViewModelConverter.cs) | class | Converts a data source into an `UnallocatedCalendarViewModel` with compact appointment text |
| [`DataSourceToViewModelConverter`](ViewModels/DataSourceToViewModelConverter.cs) | class | Converts a data source into an `VisualCalendarViewModel` (`Convert`, or `ConvertWeek` for a column per day) and renders appointment cell text by size |
| [`IVisualCalendarViewModel`](ViewModels/IVisualCalendarViewModel.cs) | interface | Declares `SetTimePeriod` for a view model |
| [`UnallocatedCalendarViewModel`](ViewModels/UnallocatedCalendarViewModel.cs) | class | View model with one unallocated column of appointments |
