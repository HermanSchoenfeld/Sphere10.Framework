// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Globalization;
using System.Linq;

namespace Sphere10.Framework;

/// <summary>
/// Runs a job whenever a <see cref="CalendarRecurrence"/> fires: once, every few days, on weekdays of every few weeks, on days of
/// chosen months, or on weekdays of chosen weeks of chosen months, each optionally repeating for a while after it starts. These are
/// the time-based schedules of Windows Task Scheduler.
/// </summary>
/// <remarks>
/// The rule's times are local times, as a calendar's are; the day-of-week and day-of-month schedules take a UTC time of day. A run
/// that came due while the scheduler was off happens once when it starts, and the schedule continues from that moment.
/// </remarks>
public class OnRecurrenceSchedule<T> : BaseJobSchedule<T> where T : class, IJob {
	private const string TimeFormat = "yyyy-MM-dd HH:mm:ss";
	// The moment the next run was computed for, and that run. One reference, so a reader on another thread sees a matching pair.
	private Tuple<DateTime, DateTime> _next;
	private DateTime _createdOn;

	public OnRecurrenceSchedule() {
	}

	public OnRecurrenceSchedule(CalendarRecurrence recurrence, uint? totalTimesToRun = null)
		: base(null, totalTimesToRun, recurrence?.End) {
		Guard.ArgumentNotNull(recurrence, nameof(recurrence));
		var validation = recurrence.Validate();
		Guard.Argument(validation.IsSuccess, nameof(recurrence), string.Join(" ", validation.ErrorMessages));
		Recurrence = recurrence;
		_createdOn = DateTime.Now;
	}

	/// <summary>The rule this schedule follows.</summary>
	public CalendarRecurrence Recurrence { get; private set; }

	public override ReschedulePolicy ReschedulePolicy { get; protected set; } = ReschedulePolicy.OnStart;

	public override JobScheduleSerializableSurrogate ToSerializableSurrogate() => new RecurrenceScheduleSerializableSurrogate {
		TotalIterations = TotalIterations?.ToString(),
		InitialStartTime = InitialStartTime?.ToString(TimeFormat),
		IterationsExecuted = IterationsExecuted,
		IterationsRemaining = IterationsRemaining,
		LastEndTime = LastEndTime?.ToString(TimeFormat),
		LastStartTime = LastStartTime?.ToString(TimeFormat),
		EndDate = EndDate?.ToString(TimeFormat),
		ReschedulePolicy = ReschedulePolicy,
		Frequency = Recurrence.Frequency,
		Start = Recurrence.Start.ToString(TimeFormat),
		Interval = Recurrence.Interval,
		DaysOfWeek = (int)Recurrence.DaysOfWeek,
		DaysOfMonth = string.Join(",", (Recurrence.DaysOfMonth ?? Array.Empty<int>()).Select(day => day.ToString(CultureInfo.InvariantCulture))),
		LastDayOfMonth = Recurrence.LastDayOfMonth,
		Months = (int)Recurrence.Months,
		WeeksOfMonth = (int)Recurrence.WeeksOfMonth,
		RepeatEveryMilliseconds = (long)(Recurrence.RepeatEvery?.TotalMilliseconds ?? 0),
		RepeatForMilliseconds = (long)(Recurrence.RepeatFor?.TotalMilliseconds ?? 0),
		CreatedOn = _createdOn.ToString(TimeFormat)
	};

	public override void FromSerializableSurrogate(JobScheduleSerializableSurrogate scheduleSurrogate) {
		var surrogate = (RecurrenceScheduleSerializableSurrogate)scheduleSurrogate;
		TotalIterations = surrogate.TotalIterations.ToUIntOrDefault();
		InitialStartTime = surrogate.InitialStartTime.ToDateTimeOrDefault();
		IterationsExecuted = surrogate.IterationsExecuted;
		IterationsRemaining = surrogate.IterationsRemaining;
		LastEndTime = surrogate.LastEndTime.ToDateTimeOrDefault();
		LastStartTime = surrogate.LastStartTime.ToDateTimeOrDefault();
		EndDate = surrogate.EndDate.ToDateTimeOrDefault();
		ReschedulePolicy = surrogate.ReschedulePolicy;
		Recurrence = new CalendarRecurrence {
			Frequency = surrogate.Frequency,
			Start = DateTime.ParseExact(surrogate.Start, TimeFormat, CultureInfo.InvariantCulture),
			End = EndDate,
			Interval = surrogate.Interval,
			DaysOfWeek = (CalendarDaysOfWeek)surrogate.DaysOfWeek,
			DaysOfMonth = string.IsNullOrEmpty(surrogate.DaysOfMonth) ? Array.Empty<int>() : surrogate.DaysOfMonth.Split(',').Select(day => int.Parse(day, CultureInfo.InvariantCulture)).ToArray(),
			LastDayOfMonth = surrogate.LastDayOfMonth,
			Months = (CalendarMonths)surrogate.Months,
			WeeksOfMonth = (CalendarWeeksOfMonth)surrogate.WeeksOfMonth,
			RepeatEvery = surrogate.RepeatEveryMilliseconds > 0 ? TimeSpan.FromMilliseconds(surrogate.RepeatEveryMilliseconds) : null,
			RepeatFor = surrogate.RepeatForMilliseconds > 0 ? TimeSpan.FromMilliseconds(surrogate.RepeatForMilliseconds) : null
		};
		_createdOn = DateTime.ParseExact(surrogate.CreatedOn, TimeFormat, CultureInfo.InvariantCulture);
		_next = null;
	}

	// The first run after the last one started, or after the schedule was created when it has not run. Stored run times are UTC.
	protected override DateTime CalculateNextRunTime() {
		var after = LastStartTime.HasValue ? DateTime.SpecifyKind(LastStartTime.Value, DateTimeKind.Utc).ToLocalTime() : _createdOn;
		var next = _next;
		if (next == null || next.Item1 != after)
			_next = next = Tuple.Create(after, Recurrence.GetNextRunTime(after)?.ToUniversalTime() ?? DateTime.MaxValue);
		return next.Item2;
	}
}
