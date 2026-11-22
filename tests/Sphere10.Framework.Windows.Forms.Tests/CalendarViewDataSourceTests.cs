// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Utils.WinFormsTester;
using Sphere10.Framework.Windows.Forms.VisualCalendar;
using Calendar = Sphere10.Framework.Windows.Forms.VisualCalendar.VisualCalendar;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>The calendar's day, week and month views over a resource schedule and over the generic calendar model.</summary>
[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class CalendarViewDataSourceTests {
	// 2026-11-02 is a Monday.
	private static readonly DateTime Monday = new(2026, 11, 2);

	[Test]
	public void AWeekOfAResourceScheduleHasAColumnForEachDayWithOverlapsSideBySide() {
		var source = new VisualCalendarDataSource();
		var first = source.NewColumn("Resource 1", null);
		var second = source.NewColumn("Resource 2", null);
		var inspection = source.NewAppointment(first, "a", Monday.AddHours(3), Monday.AddHours(5), "Inspection", null, null, null, Color.SteelBlue, null);
		source.NewAppointment(second, "b", Monday.AddHours(4), Monday.AddHours(6), "Servicing", null, null, null, Color.SeaGreen, null);
		source.NewAppointment(first, "c", Monday.AddDays(2).AddHours(9), Monday.AddDays(2).AddHours(10), "Call", null, null, null, Color.IndianRed, null);
		source.NewAppointment(second, "d", Monday.AddDays(8).AddHours(9), Monday.AddDays(8).AddHours(10), "Next Week", null, null, null, Color.Gray, null);
		var week = new DataSourceToViewModelConverter().ConvertWeek(source, Monday, TimePeriodType.DailyHourly, VisualCalendarViewModelFilter.All);
		var columns = week.Columns;
		Assert.That(columns.Select(column => column.ColumnDataObject.Date), Is.EqualTo(new DateTime?[] { Monday, Monday, Monday.AddDays(1), Monday.AddDays(2), Monday.AddDays(3),
			Monday.AddDays(4), Monday.AddDays(5), Monday.AddDays(6) }), "Monday needs a second column because two resources' appointments overlap.");
		Assert.That(columns.All(column => column.ColumnDataObject is DayColumn), Is.True);
		Assert.That(columns.Select(column => column.Name).Take(3), Is.EqualTo(new[] { Monday.ToString("ddd d MMM"), string.Empty, Monday.AddDays(1).ToString("ddd d MMM") }));
		var shown = columns[0].Appointments.Single();
		Assert.That(shown.AppointmentDataObject, Is.SameAs(inspection));
		Assert.That(shown.SourceColumn, Is.SameAs(first), "A displayed appointment remembers the data source's column it belongs to.");
		Assert.That(shown.Lines, Has.Length.EqualTo(2), "Two hours fill two hourly rows.");
		Assert.That(columns[1].Appointments.Single().SourceColumn, Is.SameAs(second));
		var wednesday = columns[3].Appointments.Single();
		Assert.That((wednesday.VisibleStartTime, wednesday.Lines.Length), Is.EqualTo((Monday.AddDays(2).AddHours(9), 1)), "A later day's appointment is laid out on that day's rows.");
		Assert.That(columns.SelectMany(column => column.Appointments).Select(item => item.AppointmentDataObject.Subject), Has.None.EqualTo("Next Week"));
	}

	[Test]
	public void TheControlSwitchesBetweenDayWeekAndMonthAndReportsAnAppointmentsOwnColumn() {
		var source = new VisualCalendarDataSource();
		var resource = source.NewColumn("Resource 1", null);
		var saturday = source.NewAppointment(resource, "a", Monday.AddDays(5).AddHours(3), Monday.AddDays(5).AddHours(5), "Backup", null, null, null, Color.SteelBlue, null);
		using var form = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000), ClientSize = new Size(1000, 700) };
		using var calendar = new Calendar { Dock = DockStyle.Fill, FirstDayOfWeek = DayOfWeek.Monday };
		form.Controls.Add(calendar);
		form.Show();
		calendar.DataSource = source;
		Assert.That(calendar.View, Is.EqualTo(VisualCalendarView.Day), "The established day view stays the default.");

		calendar.SetView(VisualCalendarView.Week, Monday.AddDays(3).AddHours(15));
		Assert.That((calendar.View, calendar.TimePeriodStart, calendar.TimePeriodType), Is.EqualTo((VisualCalendarView.Week, Monday, TimePeriodType.DailyHourly)),
			"Any moment of a week shows that week from its first day.");
		Assert.That(calendar.ViewModel.Columns, Has.Length.EqualTo(7));
		Assert.That((source.StartTime, source.EndTime.Date), Is.EqualTo((Monday, Monday.AddDays(6))), "The data source is asked for the whole week.");
		var selected = new List<AppointmentEvent>();
		calendar.AppointmentSelected += selected.Add;
		calendar.SelectAppointment(saturday);
		Assert.That(calendar.SelectedAppointment, Is.SameAs(saturday), "An appointment in a later day's column can be selected.");
		Assert.That(selected.Single().SourceColumn, Is.SameAs(resource), "Events name the data source's column, never the day column.");

		calendar.SetTimePeriodType(TimePeriodType.DailyHalfHourly);
		Assert.That((calendar.View, calendar.TimePeriodType), Is.EqualTo((VisualCalendarView.Week, TimePeriodType.DailyHalfHourly)), "The row size changes within the week.");
		Assert.That(calendar.ViewModel.Columns.Single(column => column.Appointments.Length > 0).Appointments.Single().Lines, Has.Length.EqualTo(4));

		calendar.View = VisualCalendarView.Month;
		Assert.That((calendar.View, calendar.TimePeriodType, calendar.CanResize), Is.EqualTo((VisualCalendarView.Month, TimePeriodType.Monthly, false)));
		Assert.That(calendar.ViewModel.Columns.Select(column => column.ColumnDataObject), Is.EqualTo(new[] { resource }), "A month has the data source's own columns.");

		calendar.View = VisualCalendarView.Day;
		Assert.That((calendar.View, calendar.TimePeriodType), Is.EqualTo((VisualCalendarView.Day, TimePeriodType.DailyHalfHourly)), "Leaving the month restores the row size.");
		calendar.SetTimePeriod(TimePeriodType.Monthly, Monday);
		Assert.That(calendar.View, Is.EqualTo(VisualCalendarView.Month), "The monthly row size is the month view.");
		calendar.SetTimePeriodType(TimePeriodType.DailyHourly);
		Assert.That(calendar.View, Is.EqualTo(VisualCalendarView.Day));
		form.Close();
	}

	[Test]
	public async Task ACalendarSourceHasAColumnForEachCalendarOverTheRequestedPeriod() {
		var personal = new MemoryCalendar("Personal", new CalendarEvent { Id = "gym", Title = "Gym", Recurrence = CalendarRecurrence.Daily(Monday.AddHours(18)), Duration = TimeSpan.FromMinutes(45) });
		var system = new MemoryCalendar("System Tasks",
			new CalendarEvent { Id = "backup", Title = "Backup", Recurrence = CalendarRecurrence.Daily(Monday.AddHours(3)), Duration = TimeSpan.FromHours(2) },
			new CalendarEvent { Id = "scan", Title = "Scan", Recurrence = CalendarRecurrence.Weekly(Monday.AddHours(3).AddMinutes(10), CalendarDaysOfWeek.Monday | CalendarDaysOfWeek.Thursday) },
			new CalendarEvent { Id = "sync", Title = "Sync", Recurrence = CalendarRecurrence.Daily(Monday.AddHours(8)) with { RepeatEvery = TimeSpan.FromHours(4), RepeatFor = TimeSpan.FromHours(8) } },
			new CalendarEvent { Id = "report", Title = "Report", Recurrence = CalendarRecurrence.Once(Monday.AddDays(2).AddHours(22)), Duration = TimeSpan.FromHours(30), Enabled = false });
		var source = new CalendarViewDataSource(personal, system) { SlotDuration = TimeSpan.FromMinutes(30) };
		await source.LoadAsync();

		// One day.
		source.SetTimeRange(Monday, Monday.AddDays(1).AddTicks(-1));
		Assert.That((source.PeriodStart, source.PeriodEnd), Is.EqualTo((Monday, Monday.AddDays(1))));
		var day = source.GetColumns().ToArray();
		Assert.That(day.Select(column => (column.Name, column.UserObject)), Is.EqualTo(new (string, object)[] { ("Personal", personal), ("System Tasks", system), (string.Empty, system) }),
			"A calendar gets a further column only where its own occurrences overlap.");
		var gym = source.GetAppointments(day[0]).Single();
		Assert.That((gym.Subject, gym.StartTime, gym.EndTime), Is.EqualTo(("18:00 Gym", Monday.AddHours(18), Monday.AddHours(19))), "A block ends on the row boundary after its occurrence.");
		var first = source.GetAppointments(day[1]).ToArray();
		Assert.That(first.Select(block => (block.Subject, block.StartTime, block.EndTime)), Is.EqualTo(new[] {
			("03:00 Backup", Monday.AddHours(3), Monday.AddHours(5)), ("08:00 Sync", Monday.AddHours(8), Monday.AddHours(16)) }),
			"An occurrence that repeats within itself is one block from its first to its last repetition.");
		var scan = source.GetAppointments(day[2]).Single();
		Assert.That((scan.Subject, scan.StartTime, scan.EndTime), Is.EqualTo(("03:10 Scan", Monday.AddHours(3), Monday.AddHours(3).AddMinutes(30))), "A moment is drawn on the whole row that contains it.");
		Assert.That(first[0].Notes, Is.EqualTo("Daily at 03:00"));
		Assert.That(source.GetItem(scan).Occurrence.Start, Is.EqualTo(Monday.AddHours(3).AddMinutes(10)), "The block keeps its occurrence's real time.");

		// One week: every day's blocks, which the control's week view then arranges by day.
		source.SetTimeRange(Monday, Monday.AddDays(6).AddHours(23));
		var week = source.GetColumns().SelectMany(source.GetAppointments).ToArray();
		Assert.That(week.Count(block => block.Subject.EndsWith("Backup")), Is.EqualTo(7));
		Assert.That(week.Where(block => block.Subject.EndsWith("Report")).Select(block => (block.StartTime, block.EndTime, block.Color)), Is.EqualTo(new[] {
			(Monday.AddDays(2).AddHours(22), Monday.AddDays(3), Color.Gainsboro), (Monday.AddDays(3), Monday.AddDays(4), Color.Gainsboro), (Monday.AddDays(4), Monday.AddDays(4).AddHours(4), Color.Gainsboro) }),
			"A long occurrence has a block on each day it touches, and a disabled event is gray.");

		// One month: a block for each day an event touches.
		source.SetTimeRange(new DateTime(2026, 11, 1), new DateTime(2026, 11, 30, 23, 59, 59));
		Assert.That((source.PeriodStart, source.PeriodEnd), Is.EqualTo((new DateTime(2026, 11, 1), new DateTime(2026, 12, 1))));
		var month = source.GetColumns().ToArray();
		Assert.That(source.GetAppointments(month[0]).Count(), Is.EqualTo(29), "The daily event starts on the 2nd.");
		var report = month.SelectMany(source.GetAppointments).Where(block => block.Subject.EndsWith("Report")).OrderBy(block => block.StartTime).ToArray();
		Assert.That(report.Select(block => (block.Subject, block.StartTime)), Is.EqualTo(new[] { ("22:00 Report", Monday.AddDays(2)), ("Report", Monday.AddDays(3)), ("Report", Monday.AddDays(4)) }));
		Assert.That(source.IsTimeAvailable(month[0], report[0], Monday, Monday.AddDays(1)), Is.True);
	}

	[Test]
	public async Task AMoveOrResizeIsReportedWithTheOccurrencesRealNewTimesAndChangesNothing() {
		var system = new MemoryCalendar("System Tasks", new CalendarEvent { Id = "scan", Title = "Scan", Recurrence = CalendarRecurrence.Once(Monday.AddHours(3).AddMinutes(10)) });
		var source = new CalendarViewDataSource(system);
		await source.LoadAsync();
		source.SetTimeRange(Monday, Monday.AddDays(6).AddHours(23));
		var requests = new List<CalendarRescheduleRequest>();
		source.RescheduleRequested += requests.Add;
		var column = source.GetColumns().Single();
		var block = source.GetAppointments(column).Single();
		// Dropped two days later, five rows down, as the control's week view reports it: in the block's own column.
		source.Reschedule(block, column, column, Monday.AddDays(2).AddHours(8), Monday.AddDays(2).AddHours(9));
		var request = requests.Single();
		Assert.That((request.NewStart, request.NewEnd), Is.EqualTo((Monday.AddDays(2).AddHours(8).AddMinutes(10), Monday.AddDays(2).AddHours(8).AddMinutes(10))),
			"The occurrence keeps its minutes and its length.");
		Assert.That((request.Calendar, request.TargetCalendar, request.Occurrence.Event.Id), Is.EqualTo(((ICalendar)system, (ICalendar)system, "scan")));
		Assert.That((await system.ReadEventsAsync()).Single().Recurrence.Start, Is.EqualTo(Monday.AddHours(3).AddMinutes(10)), "The source never changes a calendar itself.");
		Assert.That(() => source.DeleteAppointment(column, block), Throws.TypeOf<NotSupportedException>());
	}

	[Test]
	public async Task TheControlShowsACalendarSourceByWeekWithEachOccurrenceOnItsDay() {
		var system = new MemoryCalendar("System Tasks",
			new CalendarEvent { Id = "backup", Title = "Backup", Recurrence = CalendarRecurrence.Weekly(Monday.AddHours(3), CalendarDaysOfWeek.Tuesday | CalendarDaysOfWeek.Saturday), Duration = TimeSpan.FromHours(2) });
		var source = new CalendarViewDataSource(system);
		await source.LoadAsync();
		using var form = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000), ClientSize = new Size(1000, 700) };
		using var calendar = new Calendar { Dock = DockStyle.Fill, FirstDayOfWeek = DayOfWeek.Monday };
		form.Controls.Add(calendar);
		form.Show();
		calendar.DataSource = source;
		calendar.SetView(VisualCalendarView.Week, Monday);
		var days = calendar.ViewModel.Columns;
		Assert.That(days.Select((column, index) => (index, column.Appointments.Length)).Where(entry => entry.Length > 0).Select(entry => entry.index), Is.EqualTo(new[] { 1, 5 }),
			"Tuesday and Saturday are the second and sixth columns.");
		var saturday = days[5].Appointments.Single();
		Assert.That(source.GetItem(saturday.AppointmentDataObject).Occurrence.Start, Is.EqualTo(Monday.AddDays(5).AddHours(3)));
		Assert.That((saturday.SourceColumn.UserObject, saturday.Lines.Length), Is.EqualTo(((object)system, 2)));
		form.Close();
	}

	[Test]
	public async Task ChangingViewAwayFromAWeekWithOverlappingEventsRebuildsTheGrid() {
		// Overlapping events give Tuesday three lanes under one spanned day header; narrower views then remove those columns.
		var system = new MemoryCalendar("System",
			new CalendarEvent { Id = "a", Title = "A", Recurrence = CalendarRecurrence.Once(Monday.AddDays(1).AddHours(3)), Duration = TimeSpan.FromHours(3) },
			new CalendarEvent { Id = "b", Title = "B", Recurrence = CalendarRecurrence.Once(Monday.AddDays(1).AddHours(4)), Duration = TimeSpan.FromHours(3) },
			new CalendarEvent { Id = "c", Title = "C", Recurrence = CalendarRecurrence.Once(Monday.AddDays(1).AddHours(5)), Duration = TimeSpan.FromHours(3) });
		var source = new CalendarViewDataSource(system);
		await source.LoadAsync();
		using var form = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000), ClientSize = new Size(1000, 700) };
		using var calendar = new Calendar { Dock = DockStyle.Fill, FirstDayOfWeek = DayOfWeek.Monday };
		form.Controls.Add(calendar);
		form.Show();
		calendar.DataSource = source;
		foreach (var view in new[] { VisualCalendarView.Week, VisualCalendarView.Month, VisualCalendarView.Week, VisualCalendarView.Day, VisualCalendarView.Week }) {
			calendar.SetView(view, Monday.AddDays(1));
			Assert.That(calendar.View, Is.EqualTo(view));
			Assert.That(calendar.ViewModel.Columns.SelectMany(column => column.Appointments).Select(appointment => source.GetItem(appointment.AppointmentDataObject).Occurrence.Event.Id).Distinct().Count(), Is.EqualTo(3), $"{view} shows the three events.");
		}
		Assert.That(calendar.ViewModel.Columns.Count(column => column.ColumnDataObject.Date == Monday.AddDays(1)), Is.EqualTo(3), "Tuesday has a lane for each overlapping event.");
		form.Close();
	}

	[Test]
	public async Task AnAsynchronousRefreshLaysThePeriodOutOnAWorkerAndShowsOnlyTheLatestRequest() {
		var system = new MemoryCalendar("System",
			new CalendarEvent { Id = "backup", Title = "Backup", Recurrence = CalendarRecurrence.Daily(Monday.AddHours(3)), Duration = TimeSpan.FromHours(2) },
			new CalendarEvent { Id = "sync", Title = "Sync", Recurrence = CalendarRecurrence.Daily(Monday.AddHours(8)) with { RepeatEvery = TimeSpan.FromMinutes(15), RepeatFor = TimeSpan.FromHours(12) } });
		var source = new ThreadRecordingDataSource(system);
		await source.LoadAsync();
		using var form = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000), ClientSize = new Size(1000, 700) };
		using var calendar = new Calendar { Dock = DockStyle.Fill, FirstDayOfWeek = DayOfWeek.Monday };
		form.Controls.Add(calendar);
		form.Show();
		var uiThread = Environment.CurrentManagedThreadId;
		calendar.DataSource = source;
		source.Threads.Clear();

		await calendar.SetViewAsync(VisualCalendarView.Week, Monday);
		Assert.That(Environment.CurrentManagedThreadId, Is.EqualTo(uiThread), "The refresh returns to the UI thread.");
		Assert.That(source.Threads, Is.Not.Empty.And.All.Not.EqualTo(uiThread), "The occurrences are computed on a worker.");
		Assert.That(calendar.ViewModel.Columns.Select(column => column.ColumnDataObject.Date).Distinct().Count(), Is.EqualTo(7));
		Assert.That(calendar.ViewModel.Columns.SelectMany(column => column.Appointments).Count(block => block.AppointmentDataObject.Subject.EndsWith("Backup")), Is.EqualTo(7));

		// The month is asked for and then the day before the month is ready: the day wins, laid out for its own period.
		var month = calendar.SetViewAsync(VisualCalendarView.Month, Monday);
		var day = calendar.SetViewAsync(VisualCalendarView.Day, Monday.AddDays(2));
		await Task.WhenAll(month, day);
		Assert.That((calendar.View, calendar.ViewModel.TimeView, calendar.ViewModel.TimePeriodStart), Is.EqualTo((VisualCalendarView.Day, TimePeriodType.DailyHourly, Monday.AddDays(2))));
		var backup = calendar.ViewModel.Columns.SelectMany(column => column.Appointments).Single(block => block.AppointmentDataObject.Subject.EndsWith("Backup"));
		Assert.That((backup.AppointmentDataObject.StartTime, backup.Lines.Length), Is.EqualTo((Monday.AddDays(2).AddHours(3), 2)), "The day's block is two hourly rows, not a day of the month.");

		// The synchronous refresh supersedes one still running.
		var superseded = calendar.SetViewAsync(VisualCalendarView.Month, Monday);
		calendar.SetView(VisualCalendarView.Week, Monday);
		await superseded;
		Assert.That((calendar.View, calendar.ViewModel.Columns.Select(column => column.ColumnDataObject.Date).Distinct().Count()), Is.EqualTo((VisualCalendarView.Week, 7)));

		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		Assert.That(await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => calendar.SetViewAsync(VisualCalendarView.Month, Monday, cancellation.Token)), Is.InstanceOf<OperationCanceledException>());
		Assert.That(calendar.ViewModel.TimeView, Is.EqualTo(TimePeriodType.DailyHourly), "A cancelled refresh leaves the shown layout as it was.");
		form.Close();
	}

	[Test]
	public async Task TheTesterScreenShowsBothItsSchedulesByDayWeekAndMonth() {
		using var form = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000), ClientSize = new Size(1200, 800) };
		using var screen = new VisualCalendarScreen { Dock = DockStyle.Fill };
		form.Controls.Add(screen);
		form.Show();
		var calendar = screen.Controls.Find("VisualCalendar", true).OfType<Calendar>().Single();
		var view = screen.Controls.Find("_viewSelector", true).OfType<EnumComboBox>().Single();
		var data = screen.Controls.Find("_dataSelector", true).OfType<EnumComboBox>().Single();
		var rows = screen.Controls.Find("_timeViewSelector", true).OfType<EnumComboBox>().Single();
		Assert.That(calendar.DataSource, Is.Not.InstanceOf<CalendarViewDataSource>(), "The screen opens on its resource schedule.");
		Assert.That(calendar.View, Is.EqualTo(VisualCalendarView.Day));
		foreach (var calendars in new[] { false, true }) {
			data.SelectedEnum = calendars ? VisualCalendarScreen.DemoData.Calendars : VisualCalendarScreen.DemoData.Resources;
			await WaitAsync(() => calendar.DataSource is CalendarViewDataSource == calendars);
			foreach (var (choice, period) in new[] { (VisualCalendarView.Week, TimePeriodType.DailyHourly), (VisualCalendarView.Month, TimePeriodType.Monthly), (VisualCalendarView.Day, TimePeriodType.DailyHourly) }) {
				view.SelectedEnum = choice;
				// The calendars load before the screen brings its selectors in step, so the wait covers both.
				await WaitAsync(() => calendar.View == choice && (TimePeriodType)rows.SelectedEnum == period);
				Assert.That((calendar.View, calendar.TimePeriodType), Is.EqualTo((choice, period)), $"{(calendars ? "Calendars" : "Resources")} by {choice}.");
				Assert.That((TimePeriodType)rows.SelectedEnum, Is.EqualTo(period), "The row size selector follows the view.");
				Assert.That(calendar.ViewModel.Columns.SelectMany(column => column.Appointments).Any(), Is.True, "The view shows appointments.");
				if (choice == VisualCalendarView.Week)
					Assert.That(calendar.ViewModel.Columns.Select(column => column.ColumnDataObject.Date).Distinct().Count(), Is.EqualTo(7));
				// A capture of each view for review, as the appearance fixture keeps for the day view.
				var directory = System.IO.Path.Combine(TestContext.CurrentContext.WorkDirectory, "visual-calendar-captures");
				System.IO.Directory.CreateDirectory(directory);
				using var capture = new Bitmap(screen.Width, screen.Height, System.Drawing.Imaging.PixelFormat.Format32bppRgb);
				screen.DrawToBitmap(capture, new Rectangle(Point.Empty, screen.Size));
				capture.Save(System.IO.Path.Combine(directory, $"visual-calendar-{(calendars ? "calendars" : "resources")}-{choice.ToString().ToLowerInvariant()}.png"));
			}
		}
		Assert.That(((CalendarViewDataSource)calendar.DataSource).Calendars.Select(item => item.Name), Is.EqualTo(new[] { "Personal", "System Tasks" }));
		form.Close();
	}

	private static async Task WaitAsync(Func<bool> ready) {
		var deadline = DateTime.UtcNow.AddSeconds(5);
		while (!ready() && DateTime.UtcNow < deadline) {
			await Task.Delay(10);
			System.Windows.Forms.Application.DoEvents();
		}
		System.Windows.Forms.Application.DoEvents();
		Assert.That(ready(), Is.True, "The screen applied the selection.");
	}

	#region Inner Types

	// Records the threads that decide which events are shown, which is part of every layout.
	private sealed class ThreadRecordingDataSource : CalendarViewDataSource {
		public ThreadRecordingDataSource(params ICalendar[] calendars)
			: base(calendars) {
		}

		public ConcurrentBag<int> Threads { get; } = new();

		protected override bool Includes(ICalendar calendar, CalendarEvent calendarEvent) {
			Threads.Add(Environment.CurrentManagedThreadId);
			return true;
		}
	}

	#endregion
}
