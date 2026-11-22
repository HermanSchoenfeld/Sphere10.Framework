// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using Sphere10.Framework.Windows.Forms;
using Sphere10.Framework.Windows.Forms.VisualCalendar;

namespace Sphere10.Framework.Utils.WinFormsTester;

/// <summary>Explores the calendar's day, week and month views, its styles, resource scheduling with unallocated work and complete drag/resize events,
/// and calendars of recurring events through the generic calendar model.</summary>
public partial class VisualCalendarScreen : ApplicationScreen {
	private static readonly string[] _subjects = new string[] { "Subject 1", "Subject 2", "Subject 3", "Subject 4", "Subject 5", "Subject 6", "Subject 7", "Subject 8", "Subject 9", "Subject 10" };
	private static readonly string[] _streets = new string[] { "Alpha", "Beta", "Gamma", "Delta", "Omega", "Phi", "Psi", "Andromeda", "Milky Way", "Ursur Minor" };
	private static readonly string[] _streetTypes = new string[] { "St", "Rd", "Way", "Pde", "Ct" };
	private static readonly string[] _states = new string[] { "ACT", "QLD", "NSW", "TAS", "VIC", "WA", "SA", "NT" };
	private static readonly string[] _cities = new string[] { "Vega", "Pleides", "Ceres", "Hyades", "Celestial Sphere", "Taurus", "Gemeni", "Mars", "Mercury", "Woodridge" };
	// Mid-tone category hues; the visual calendar softens each into a pastel fill with matching text.
	private static readonly Color[] _appointmentColors = new[] { Color.SteelBlue, Color.SeaGreen, Color.Goldenrod, Color.IndianRed, Color.MediumPurple, Color.Coral, Color.Teal, Color.Orchid };
	private readonly TextBoxWriter _outputWriter;
	private MyVisualCalendarDataSource _dataSource;
	private CalendarViewDataSource _calendarSource;
	private bool _synchronizing;

	public VisualCalendarScreen() {
		InitializeComponent();
		Disposables.Add(_heading.Font);
		_timeViewSelector.EnumType = typeof(TimePeriodType);
		_columnFilterComboBox.EnumType = typeof(VisualCalendarViewModelFilter);
		_viewSelector.EnumType = typeof(VisualCalendarView);
		_dataSelector.EnumType = typeof(DemoData);
		_styleSelector.EnumType = typeof(VisualCalendarStyle);
		_outputWriter = new TextBoxWriter(_outputTextBox);
	}

	/// <summary>Generates sample appointments only when the real example is displayed.</summary>
	protected override void OnLoad(EventArgs e) {
		base.OnLoad(e);
		if (!Tools.Runtime.IsDesignMode && _dataSource == null)
			GenerateData();
	}

	private void VisualCalendar_VisualCalendarFreeRegionSelected(VisualCalendarFreeRegionSelected arg)
		=> _outputWriter.WriteLine("{0}: free region selected - column '{1}' start time: '{2}' end time: '{3}'", arg.Source.Name, arg.Column.Name, arg.StartTime.ToShortTimeString(), arg.EndTime.ToShortTimeString());

	private void VisualCalendar_AppointmentDrag(AppointmentDraggedEvent arg)
		=> _outputWriter.WriteLine("{0}: appointment dragged - column '{1}' appointment '{2}'", arg.Source.Name, arg.SourceColumn.Name, arg.Appointment.Subject);

	private void VisualCalendar_AppointmentDragStarting(AppointmentDragStartingEvent arg) {
		_outputWriter.WriteLine("{0}: appointment drag starting - column '{1}' appointment '{2}'", arg.Source.Name, arg.SourceColumn.Name, arg.Appointment.Subject);
		// note you can cancel the drag here by settting
		// arg.Cancel = true;
		// arg.ErrorMessage = reason for cancel
	}

	private void VisualCalendar_AppointmentDrop(AppointmentDropEvent arg) {
		_outputWriter.WriteLine("{0}: appointment dropped - column '{1}' appointment '{2}' target column:'{3}' start time: '{4}' end time: '{5}'",
			arg.Source.Name,
			arg.SourceColumn.Name,
			arg.Appointment.Subject,
			arg.TargetColumn.Name,
			arg.StartTime.ToShortTimeString(),
			arg.EndTime.ToShortTimeString());
	}

	private void VisualCalendar_AppointmentDropStarting(AppointmentDropStartingEvent arg) {
		_outputWriter.WriteLine("{0}: appointment drop starting - column '{1}' appointment '{2}' target column:'{3}' start time: '{4}' end time: '{5}'",
			arg.Source.Name,
			arg.SourceColumn.Name,
			arg.Appointment.Subject,
			arg.TargetColumn.Name,
			arg.StartTime.ToShortTimeString(),
			arg.EndTime.ToShortTimeString());
		// note you can cancel the drag here by settting
		// arg.Cancel = true;
		// arg.ErrorMessage = reason for cancel
	}

	private void VisualCalendar_AppointmentResizing(AppointmentResizingEvent arg) {
		_outputWriter.WriteLine("{0}: appointment resizing - column '{1}' appointment '{2}' start time: '{3}' end time: '{4}'",
			arg.Source.Name,
			arg.SourceColumn.Name,
			arg.Appointment.Subject,
			arg.SelectedStartTime.ToShortTimeString(),
			arg.SelectedEndTime.ToShortTimeString());
	}

	private void VisualCalendar_AppointmentResizingFinished(AppointmentResizingFinishedEvent arg) {
		_outputWriter.WriteLine("{0}: appointment resized - column '{1}' appointment '{2}' start time: '{3}' end time: '{4}'",
			arg.Source.Name,
			arg.SourceColumn.Name,
			arg.Appointment.Subject,
			arg.SelectedStartTime.ToShortTimeString(),
			arg.SelectedEndTime.ToShortTimeString());
	}

	private void VisualCalendar_AppointmentResizingStarted(AppointmentEvent arg)
		=> _outputWriter.WriteLine("{0}: appointment resizing started - column '{1}' appointment '{2}'", arg.Source.Name, arg.SourceColumn.Name, arg.Appointment.Subject);

	private void VisualCalendar_AppointmentDoubleClicked(AppointmentEvent arg)
		=> _outputWriter.WriteLine("{0}: appointment double clicked - column '{1}' appointment '{2}'", arg.Source.Name, arg.SourceColumn.Name, arg.Appointment.Subject);

	private void VisualCalendar_AppointmentSelected(AppointmentEvent arg)
		=> _outputWriter.WriteLine("{0}: appointment selected - column '{1}' appointment '{2}'", arg.Source.Name, arg.SourceColumn.Name, arg.Appointment.Subject);

	private void VisualCalendar_AppointmentDeselected(AppointmentEvent arg)
		=> _outputWriter.WriteLine("{0}: appointment deselected - column '{1}' appointment '{2}'", arg.Source.Name, arg.SourceColumn.Name, arg.Appointment.Subject);

	private void VisualCalendar_AppointmentDragging(AppointmentDraggingEvent arg)
		=> _outputWriter.WriteLine("{0}: appointment dragging - appointment '{1}' column '{2}' start: '{3}' finish: '{4}' ", arg.Source.Name, arg.Appointment.Subject, arg.DestinationColumn.Name, arg.DestinationStartTime, arg.DestinationEndTime);

	private void UnallocatedCalendar_AppointmentDrag(AppointmentDraggedEvent arg)
		=> _outputWriter.WriteLine("{0}: appointment dragged - column '{1}' appointment '{2}'", arg.Source.Name, arg.SourceColumn.Name, arg.Appointment.Subject);

	private void UnallocatedCalendar_AppointmentDragStarting(AppointmentDragStartingEvent arg) {
		_outputWriter.WriteLine("{0}: appointment drag starting - column '{1}' appointment '{2}'", arg.Source.Name, arg.SourceColumn.Name, arg.Appointment.Subject);
		// note you can cancel the drag here by settting
		// arg.Cancel = true;
		// arg.ErrorMessage = reason for cancel
	}

	private void UnallocatedCalendar_AppointmentDrop(AppointmentDropEvent arg) {
		_outputWriter.WriteLine("{0}: appointment dropped - column '{1}' appointment '{2}' target column:'{3}' start time: '{4}' end time: '{5}'",
			arg.Source.Name,
			arg.SourceColumn.Name,
			arg.Appointment.Subject,
			arg.TargetColumn.Name,
			arg.StartTime.ToShortTimeString(),
			arg.EndTime.ToShortTimeString());
	}

	private void UnallocatedCalendar_AppointmentDropStarting(AppointmentDropStartingEvent arg) {
		_outputWriter.WriteLine("{0}: appointment drop starting - column '{1}' appointment '{2}' target column:'{3}' start time: '{4}' end time: '{5}'",
			arg.Source.Name,
			arg.SourceColumn.Name,
			arg.Appointment.Subject,
			arg.TargetColumn.Name,
			arg.StartTime.ToShortTimeString(),
			arg.EndTime.ToShortTimeString());
		// note you can cancel the drag here by settting
		// arg.Cancel = true;
		// arg.ErrorMessage = reason for cancel
	}

	private void UnallocatedCalendar_AppointmentDoubleClicked(AppointmentEvent arg)
		=> _outputWriter.WriteLine("{0}: appointment double clicked - column '{1}' appointment '{2}'", arg.Source.Name, arg.SourceColumn.Name, arg.Appointment.Subject);

	private void UnallocatedCalendar_AppointmentSelected(AppointmentEvent arg)
		=> _outputWriter.WriteLine("{0}: appointment selected - column '{1}' appointment '{2}'", arg.Source.Name, arg.SourceColumn.Name, arg.Appointment.Subject);

	private void UnallocatedCalendar_AppointmentDeselected(AppointmentEvent arg)
		=> _outputWriter.WriteLine("{0}: appointment deselected - column '{1}' appointment '{2}'", arg.Source.Name, arg.SourceColumn.Name, arg.Appointment.Subject);

	private void UnallocatedCalendar_AppointmentDragging(AppointmentDraggingEvent arg)
		=> _outputWriter.WriteLine("{0}: appointment dragging - appointment '{1}' column '{2}' start: '{3}' finish: '{4}' ", arg.Source.Name, arg.Appointment.Subject, arg.DestinationColumn.Name, arg.DestinationStartTime, arg.DestinationEndTime);

	// Form Event Handlers
	private void PopulateButton_Click(object sender, EventArgs args) => GenerateData();

	// Data Generation
	private void GenerateData() {
		const int numResources = 10;
		const int maxAppointmentsPerDay = 4;

		var year = DateTime.Now.Year;
		var month = DateTime.Now.Month;
		var startDay = 1;
		var endDay = DateTime.Now.ToEndOfMonth().Day;

		_dataSource = new MyVisualCalendarDataSource();

		// Create unallocated appointments
		for (var day = startDay; day <= endDay; day++) {
			var numAppointments = Tools.Maths.RNG.Next(maxAppointmentsPerDay - 1) + 1;
			var appointmentBoundaries = GenerateRandomBoundaries(maxAppointmentsPerDay, 0, 24).ToArray();
			for (var appointmentNo = 0; appointmentNo < numAppointments; appointmentNo++)
				_dataSource.NewAppointment(
					new UnallocatedColumn(),
					Guid.NewGuid().ToStrictAlphaString(),
					new DateTime(year, month, day, appointmentBoundaries[appointmentNo].Item1, 0, 0),
					new DateTime(year, month, day, appointmentBoundaries[appointmentNo].Item2, 0, 0),
					RandomSubject(),
					RandomLocation1(),
					RandomLocation2(),
					RandomNotes(),
					RandomAppointmentColor(),
					null
				);
		}

		// Create appointments
		for (var resource = 0; resource < numResources; resource++) {
			var column = _dataSource.NewColumn(string.Format("Resource {0}", resource + 1), null);
			for (var day = startDay; day <= endDay; day++) {
				var numAppointments = Tools.Maths.RNG.Next(maxAppointmentsPerDay + 1);
				var appointmentBoundaries = GenerateRandomBoundaries(maxAppointmentsPerDay, 0, 24).ToArray();
				for (var appointmentNo = 0; appointmentNo < numAppointments; appointmentNo++)
					_dataSource.NewAppointment(
						column,
						Guid.NewGuid().ToStrictAlphaString(),
						new DateTime(year, month, day, appointmentBoundaries[appointmentNo].Item1, 0, 0),
						new DateTime(year, month, day, appointmentBoundaries[appointmentNo].Item2, 0, 0),
						RandomSubject(),
						RandomLocation1(),
						RandomLocation2(),
						RandomNotes(),
						RandomAppointmentColor(),
						null
					);
			}
		}
		_unallocatedCalendar.DataSource = _dataSource;
		if ((DemoData)_dataSelector.SelectedEnum == DemoData.Resources) {
			_visualCalendar.DataSource = _dataSource;
			_visualCalendar.SetView(_visualCalendar.View, DateTime.Today);
		}
	}

	private IEnumerable<Tuple<int, int>> GenerateRandomBoundaries(int numberToGenerate, int startValueInclusive, int endValueExclusive) {
		if (endValueExclusive - startValueInclusive - 1 < numberToGenerate / 2)
			throw new SoftwareException("Insufficient number of options to generate random boundaries. Generate less or increase boundary");

		var values = new HashSet<int>();

		for (var index = 0; index < numberToGenerate * 2; index++) {
			var generatedValue = 0;
			do {
				generatedValue = Tools.Maths.RNG.Next(startValueInclusive, endValueExclusive);
			} while (values.Contains(generatedValue));
			values.Add(generatedValue);
		}

		var sortedValues = values.ToList();
		sortedValues.Sort();

		for (var i = 0; i < sortedValues.Count; i += 2) {
			yield return Tuple.Create(sortedValues[i], sortedValues[i + 1]);
		}
	}

	private Color RandomAppointmentColor() => _appointmentColors[Tools.Maths.RNG.Next(_appointmentColors.Length)];

	private string RandomSubject() => _subjects.Randomize().First();

	private string RandomLocation1() => string.Format("{0} {1} {2}", RandomStreetNumber(), RandomStreetName(), RandomStreetType());

	private string RandomStreetName() => _streets.Randomize().First();

	private string RandomStreetNumber() => Tools.Maths.RNG.Next(1, 100).ToString();

	private string RandomStreetType() => _streetTypes.Randomize().First();

	private string RandomLocation2() => string.Format("{0}, {1} {2}", RandomCity(), RandomState(), RandomPostCode());

	private string RandomCity() => _cities.Randomize().First();

	private string RandomState() => _states.Randomize().First();

	private string RandomPostCode() => Tools.Maths.RNG.Next(1001, 10000).ToString();

	private string RandomNotes() {
		if (Tools.Maths.Gamble(0.2))
			return Tools.Text.GenerateRandomString(Tools.Maths.RNG.Next(5, 100));

		return string.Empty;
	}

	private async void TimeViewSelector_SelectedIndexChanged(object sender, EventArgs args) {
		if (_synchronizing || Tools.Runtime.IsDesignMode || _dataSource == null)
			return;
		var rows = (TimePeriodType)_timeViewSelector.SelectedEnum;
		_unallocatedCalendar.SetTimePeriodType(rows);
		// The monthly row size is the month view; any other keeps the day or week in view.
		_visualCalendar.SetTimePeriodType(rows);
		await ShowCalendarAsync();
	}

	private async void ViewSelector_SelectedIndexChanged(object sender, EventArgs args) {
		if (_synchronizing || Tools.Runtime.IsDesignMode || _dataSource == null)
			return;
		_visualCalendar.SetView((VisualCalendarView)_viewSelector.SelectedEnum, DateTime.Today);
		await ShowCalendarAsync();
	}

	// Both calendars take the chosen look, so the tray matches the schedule beside it.
	private void StyleSelector_SelectedIndexChanged(object sender, EventArgs args)
		=> _visualCalendar.Style = _unallocatedCalendar.Style = (VisualCalendarStyle)_styleSelector.SelectedEnum;

	private async void DataSelector_SelectedIndexChanged(object sender, EventArgs args) {
		if (!Tools.Runtime.IsDesignMode && _dataSource != null)
			await ShowCalendarAsync();
	}

	/// <summary>Shows the chosen sample data in the calendar's current view and brings the selectors in step with it.</summary>
	private async Task ShowCalendarAsync() {
		var calendars = (DemoData)_dataSelector.SelectedEnum == DemoData.Calendars;
		_columnFilterComboBox.Enabled = _unallocatedCalendar.Enabled = !calendars;
		if (calendars) {
			_calendarSource ??= CreateCalendarSource();
			// The calendar source draws each occurrence on whole rows, so it is told the row size.
			_calendarSource.SlotDuration = _visualCalendar.TimePeriodType switch {
				TimePeriodType.DailyHalfHourly => TimeSpan.FromMinutes(30),
				TimePeriodType.DailyQuaterHourly => TimeSpan.FromMinutes(15),
				_ => TimeSpan.FromHours(1)
			};
			await _calendarSource.LoadAsync();
		}
		if (IsDisposed)
			return;
		var source = calendars ? (IVisualCalendarDataSource)_calendarSource : _dataSource;
		if (!ReferenceEquals(_visualCalendar.DataSource, source))
			_visualCalendar.DataSource = source;
		_visualCalendar.SetView(_visualCalendar.View, DateTime.Today);
		_synchronizing = true;
		using (Tools.Scope.ExecuteOnDispose(() => _synchronizing = false)) {
			_viewSelector.SelectedEnum = _visualCalendar.View;
			_timeViewSelector.SelectedEnum = _visualCalendar.TimePeriodType;
		}
	}

	// Two calendars of recurring events: one a person's, one a system's scheduled work. Dragging an occurrence moves its whole event.
	private CalendarViewDataSource CreateCalendarSource() {
		var today = DateTime.Today;
		var personal = new MemoryCalendar("Personal",
			new CalendarEvent { Title = "Stand-Up", Location = "Room 2", Recurrence = CalendarRecurrence.Weekly(today.AddDays(-30).AddHours(9), CalendarDaysOfWeek.Weekdays), Duration = TimeSpan.FromMinutes(30) },
			new CalendarEvent { Title = "Gym", Recurrence = CalendarRecurrence.Weekly(today.AddDays(-30).AddHours(18), CalendarDaysOfWeek.Monday | CalendarDaysOfWeek.Wednesday | CalendarDaysOfWeek.Friday), Duration = TimeSpan.FromHours(1) },
			new CalendarEvent { Title = "Dentist", Description = "Check-up", Recurrence = CalendarRecurrence.Once(today.AddDays(2).AddHours(14)), Duration = TimeSpan.FromHours(1) });
		var system = new MemoryCalendar("System Tasks",
			new CalendarEvent { Title = "Backup", Recurrence = CalendarRecurrence.Daily(today.AddDays(-30).AddHours(3)), Duration = TimeSpan.FromHours(1) },
			new CalendarEvent { Title = "Disk Check", Recurrence = CalendarRecurrence.Weekly(today.AddDays(-30).AddHours(2), CalendarDaysOfWeek.Sunday), Duration = TimeSpan.FromHours(2) },
			new CalendarEvent { Title = "Patch Install", Recurrence = CalendarRecurrence.MonthlyDayOfWeek(today.AddDays(-60).AddHours(3), CalendarWeeksOfMonth.Second, CalendarDaysOfWeek.Tuesday), Duration = TimeSpan.FromHours(2) },
			new CalendarEvent { Title = "Month-End Report", Recurrence = CalendarRecurrence.Monthly(today.AddDays(-60).AddHours(23), null, lastDayOfMonth: true) },
			new CalendarEvent { Title = "Sync", Recurrence = CalendarRecurrence.Daily(today.AddDays(-30).AddHours(8)) with { RepeatEvery = TimeSpan.FromHours(2), RepeatFor = TimeSpan.FromHours(8) } },
			new CalendarEvent { Title = "Old Cleanup", Recurrence = CalendarRecurrence.Daily(today.AddDays(-30).AddHours(3).AddMinutes(30)), Enabled = false });
		var source = new CalendarViewDataSource(personal, system);
		source.RescheduleRequested += CalendarRescheduleRequested;
		return source;
	}

	// The view only asks; the screen moves the event in its calendar and shows the calendars again.
	private async void CalendarRescheduleRequested(CalendarRescheduleRequest request) {
		var calendarEvent = request.Occurrence.Event;
		var changed = calendarEvent.Reschedule(request.Occurrence, request.NewStart, request.NewEnd);
		_outputWriter.WriteLine("{0}: '{1}' moved from {2:g} to {3:g} - {4}", request.Calendar.Name, calendarEvent.Title, request.Occurrence.Start, request.NewStart, changed.Recurrence.Describe());
		await request.Calendar.UpdateAsync(calendarEvent, changed);
		await _calendarSource.LoadAsync();
		if (!IsDisposed)
			_visualCalendar.RefreshFromDataSource();
	}

	private void ColumnFilterComboBox_SelectedIndexChanged(object sender, EventArgs args)
		=> _visualCalendar.ColumnFilter = (VisualCalendarViewModelFilter)_columnFilterComboBox.SelectedEnum;

	#region Inner Types

	/// <summary>The sample data the calendar shows.</summary>
	public enum DemoData {
		[Description("Resource Schedule")] Resources,

		[Description("Recurring Calendars")] Calendars
	}

	#endregion
}
