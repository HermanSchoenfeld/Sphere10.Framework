// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows.Forms.VisualCalendar;

/// <summary>
/// Shows the occurrences of one or more <see cref="ICalendar"/> objects in a <see cref="VisualCalendar"/>, with a column for each
/// calendar. The control's own Day, Week and Month views apply as they do to any data source. Occurrences of one calendar that
/// overlap are placed side by side in extra columns, so nothing hides anything else. Use it for a person's calendars, a job
/// schedule or an operating system's tasks.
/// </summary>
/// <remarks>
/// Call <see cref="LoadAsync"/> to read the calendars, then <c>VisualCalendar.SetView</c> or <c>RefreshFromDataSource</c>, or their
/// asynchronous forms, which compute the occurrences and their layout on a worker. The source may be read from another thread while the
/// UI changes its period or reloads it; a layout computed for a period that has changed meanwhile is not kept. The
/// source never changes a calendar itself. A drag or resize in the view raises <see cref="RescheduleRequested"/> with the real new
/// times; the owner applies the change to the calendar, with any confirmation it wants, and loads again. A moment without duration
/// is drawn one row high so that it can be seen and dragged.
/// </remarks>
public class CalendarViewDataSource : IVisualCalendarDataSource {
	/// <summary>Raised when the user moved or resized an occurrence in the view. Nothing has been changed yet.</summary>
	public event EventHandlerEx<CalendarRescheduleRequest> RescheduleRequested;

	private static readonly Color[] _palette = {
		Color.SteelBlue, Color.SeaGreen, Color.DarkOrange, Color.MediumPurple, Color.IndianRed, Color.Teal, Color.Goldenrod, Color.SlateGray
	};
	private readonly ICalendar[] _calendars;
	private readonly Dictionary<ICalendar, CalendarEvent[]> _events = new();
	private readonly object _layoutLock = new();
	private TimeSpan _slotDuration = TimeSpan.FromHours(1);
	private DateTime _periodStart = DateTime.Today;
	private DateTime _periodEnd = DateTime.Today.AddDays(1);
	private int _layoutVersion;
	private Layout _layout;

	public CalendarViewDataSource(params ICalendar[] calendars) {
		Guard.ArgumentNotNull(calendars, nameof(calendars));
		Guard.Argument(calendars.Length > 0 && calendars.All(calendar => calendar != null), nameof(calendars), "Show at least one calendar.");
		_calendars = (ICalendar[])calendars.Clone();
		foreach (var calendar in _calendars)
			_events[calendar] = Array.Empty<CalendarEvent>();
	}

	/// <summary>The calendars shown, in column order.</summary>
	public ICalendar[] Calendars => (ICalendar[])_calendars.Clone();

	/// <summary>The height of one row in the time-of-day views. Set it to the view's row size, so that blocks start and end on rows.</summary>
	public TimeSpan SlotDuration {
		get => _slotDuration;
		set {
			Guard.Argument(value > TimeSpan.Zero && value <= TimeSpan.FromDays(1), nameof(value), "A row lasts between a moment and a day.");
			lock (_layoutLock) {
				_slotDuration = value;
				Invalidate();
			}
		}
	}

	/// <summary>The first moment of the period shown.</summary>
	public DateTime PeriodStart {
		get {
			lock (_layoutLock)
				return _periodStart;
		}
	}

	/// <summary>The moment the period shown ends, which is not part of it.</summary>
	public DateTime PeriodEnd {
		get {
			lock (_layoutLock)
				return _periodEnd;
		}
	}

	/// <summary>Reads every calendar's events again. The view shows them at its next refresh.</summary>
	public async Task LoadAsync(CancellationToken cancellationToken = default) {
		var loaded = new Dictionary<ICalendar, CalendarEvent[]>();
		foreach (var calendar in _calendars)
			loaded[calendar] = await calendar.ReadEventsAsync(cancellationToken).ConfigureAwait(false) ?? Array.Empty<CalendarEvent>();
		lock (_events) {
			foreach (var pair in loaded)
				_events[pair.Key] = pair.Value;
			Invalidate();
		}
	}

	/// <summary>The calendar and occurrence a displayed block stands for, or null for a block from another source.</summary>
	public CalendarViewItem GetItem(Appointment appointment) => appointment?.UserObject as CalendarViewItem;

	#region IVisualCalendarDataSource

	/// <summary>Sets the period shown, from the day of <paramref name="startTime"/> through the day of <paramref name="endTime"/>.</summary>
	public void SetTimeRange(DateTime startTime, DateTime endTime) {
		lock (_layoutLock) {
			_periodStart = startTime.Date;
			_periodEnd = (endTime > startTime ? endTime.Date : startTime.Date).AddDays(1);
			Invalidate();
		}
	}

	public IEnumerable<AppointmentColumn> GetColumns() => EnsureLayout().Columns;

	public IEnumerable<Appointment> GetAppointments(AppointmentColumn column) {
		Guard.ArgumentNotNull(column, nameof(column));
		return EnsureLayout().Appointments.TryGetValue(column, out var appointments) ? appointments : Enumerable.Empty<Appointment>();
	}

	/// <summary>Reports the move or resize as a request with the occurrence's real new times; the calendars are left as they are.</summary>
	public void Reschedule(Appointment appointment, AppointmentColumn sourceColumn, AppointmentColumn destColumn, DateTime newStartTime, DateTime newEndTime) {
		var item = GetItem(appointment);
		Guard.Argument(item != null, nameof(appointment), "The appointment does not come from this source.");
		// The block was drawn on whole rows, so the request carries how far it moved, applied to the occurrence's own times.
		var start = item.Occurrence.Start + (newStartTime - appointment.StartTime);
		var end = item.Occurrence.End + (newEndTime - appointment.EndTime);
		var target = destColumn?.UserObject as ICalendar ?? item.Calendar;
		RescheduleRequested?.Invoke(new CalendarRescheduleRequest(item.Calendar, target, item.Occurrence, start, end < start ? start : end));
	}

	public Appointment NewAppointment(AppointmentColumn column, string id, DateTime startTime, DateTime endTime, string subject, string locationPart1, string locationPart2, string notes, Color color, object userObject)
		=> throw new NotSupportedException("Add events through their calendar, then load this source again.");

	public void DeleteAppointment(AppointmentColumn column, Appointment appointment)
		=> throw new NotSupportedException("Remove events through their calendar, then load this source again.");

	/// <summary>Always true: occurrences that share a time are shown side by side.</summary>
	public bool IsTimeAvailable(AppointmentColumn column, Appointment appointment, DateTime startTime, DateTime endTime) => true;

	#endregion

	/// <summary>Whether an event is shown. Override to hide events, for example disabled ones or those outside a search.</summary>
	protected virtual bool Includes(ICalendar calendar, CalendarEvent calendarEvent) => true;

	/// <summary>The color of an event that does not name one: its calendar's, or gray while it is disabled.</summary>
	protected virtual Color GetColor(ICalendar calendar, CalendarEvent calendarEvent)
		=> !calendarEvent.Enabled ? Color.Gainsboro : calendarEvent.Color ?? _palette[Array.IndexOf(_calendars, calendar) % _palette.Length];

	/// <summary>Drops the computed layout after the events, period or mode changed.</summary>
	protected void Invalidate() {
		lock (_layoutLock) {
			_layoutVersion++;
			_layout = null;
		}
	}

	// The layout of the period, computed outside the lock so that a period change on the UI thread never waits for it. A layout computed
	// while the period or the events changed is returned to its reader, whose request is stale, but not kept.
	private Layout EnsureLayout() {
		int version;
		DateTime periodStart, periodEnd;
		TimeSpan slotDuration;
		lock (_layoutLock) {
			if (_layout != null)
				return _layout;
			(version, periodStart, periodEnd, slotDuration) = (_layoutVersion, _periodStart, _periodEnd, _slotDuration);
		}
		var columns = new List<AppointmentColumn>();
		var appointments = new Dictionary<AppointmentColumn, List<Appointment>>();
		var items = ReadItems(periodStart, periodEnd);
		var days = Enumerable.Range(0, (int)(periodEnd - periodStart).TotalDays).Select(day => periodStart.AddDays(day)).ToArray();
		// A period longer than a week is a month, whose rows are days; shorter periods have rows for the time of day.
		var showsDays = (periodEnd - periodStart).TotalDays > 7;
		foreach (var calendar in _calendars) {
			var own = items.Where(item => ReferenceEquals(item.Calendar, calendar));
			AddGroup(columns, appointments, calendar.Name, calendar, showsDays ? own.SelectMany(item => CreateDayBlocks(item, periodStart, periodEnd))
				: own.SelectMany(item => days.SelectMany(day => CreateTimeBlocks(item, day, slotDuration))));
		}
		var layout = new Layout(columns.ToArray(), appointments);
		lock (_layoutLock) {
			if (version == _layoutVersion)
				_layout = layout;
		}
		return layout;
	}

	private CalendarViewItem[] ReadItems(DateTime periodStart, DateTime periodEnd) {
		lock (_events)
			return _calendars.SelectMany(calendar => _events[calendar].Where(item => item?.Recurrence != null && Includes(calendar, item))
				.SelectMany(item => item.GetOccurrences(periodStart, periodEnd).Where(occurrence => occurrence.Start < periodEnd)
					.Select(occurrence => new CalendarViewItem(calendar, occurrence)))).ToArray();
	}

	// One block for the part of an occurrence that falls on a day, drawn on whole rows.
	private IEnumerable<Appointment> CreateTimeBlocks(CalendarViewItem item, DateTime day, TimeSpan slotDuration) {
		var occurrence = item.Occurrence;
		var nextDay = day.AddDays(1);
		if (occurrence.Start >= nextDay || occurrence.End < day || occurrence.End == day && occurrence.Start < day)
			yield break;
		var slot = slotDuration.Ticks;
		var start = occurrence.Start < day ? day : new DateTime(occurrence.Start.Ticks / slot * slot);
		var end = occurrence.End > nextDay ? nextDay : new DateTime((occurrence.End.Ticks + slot - 1) / slot * slot);
		if (end <= start)
			end = start.AddTicks(slot);
		if (end > nextDay)
			end = nextDay;
		yield return CreateAppointment(item, start, end, occurrence.Start.ToString("HH:mm", CultureInfo.CurrentCulture) + " " + item.Occurrence.Event.Title);
	}

	// One block for each day of the month an occurrence touches; an event that occurs several times in a day is listed once for it.
	private IEnumerable<Appointment> CreateDayBlocks(CalendarViewItem item, DateTime periodStart, DateTime periodEnd) {
		var occurrence = item.Occurrence;
		var last = occurrence.End > occurrence.Start ? occurrence.End.AddTicks(-1).Date : occurrence.Start.Date;
		for (var day = occurrence.Start.Date < periodStart ? periodStart : occurrence.Start.Date; day <= last && day < periodEnd; day = day.AddDays(1))
			yield return CreateAppointment(item, day, day.AddHours(12), (day == occurrence.Start.Date ? occurrence.Start.ToString("HH:mm", CultureInfo.CurrentCulture) + " " : string.Empty) + occurrence.Event.Title);
	}

	private Appointment CreateAppointment(CalendarViewItem item, DateTime start, DateTime end, string subject) {
		var calendarEvent = item.Occurrence.Event;
		return new Appointment {
			ID = $"{Array.IndexOf(_calendars, item.Calendar)}|{calendarEvent.Id}|{item.Occurrence.Start.Ticks}|{start.Ticks}",
			Subject = subject,
			LocationPart1 = calendarEvent.Location,
			Notes = item.Occurrence.IsRecurring || calendarEvent.Recurrence.RepeatEvery.HasValue ? calendarEvent.Recurrence.Describe() : calendarEvent.Description,
			StartTime = start,
			EndTime = end,
			Color = GetColor(item.Calendar, calendarEvent),
			UserObject = item
		};
	}

	// Blocks that overlap go to further columns of the same group, each filled from the left.
	private static void AddGroup(List<AppointmentColumn> columns, Dictionary<AppointmentColumn, List<Appointment>> appointments, string title, object key, IEnumerable<Appointment> blocks) {
		var lanes = new List<List<Appointment>> { new() };
		foreach (var block in blocks.GroupBy(block => block.ID).Select(group => group.First()).OrderBy(block => block.StartTime).ThenBy(block => block.Subject, StringComparer.CurrentCultureIgnoreCase)) {
			var lane = lanes.FirstOrDefault(candidate => candidate.Count == 0 || candidate[^1].EndTime <= block.StartTime);
			if (lane == null)
				lanes.Add(lane = new List<Appointment>());
			lane.Add(block);
		}
		for (var index = 0; index < lanes.Count; index++) {
			var column = new AppointmentColumn { Name = index == 0 ? title : string.Empty, UserObject = key };
			columns.Add(column);
			appointments[column] = lanes[index];
		}
	}

	#region Inner Types

	// The columns of one period and the blocks in each, replaced as a whole so that a reader on another thread sees one period's layout.
	private sealed record Layout(AppointmentColumn[] Columns, Dictionary<AppointmentColumn, List<Appointment>> Appointments);

	#endregion
}
