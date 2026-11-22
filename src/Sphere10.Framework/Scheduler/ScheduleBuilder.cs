// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework;

public class ScheduleBuilder<T> where T : BaseJob {
	readonly JobBuilder<T> _jobBuilder;

	internal ScheduleBuilder(JobBuilder<T> jobBuilder) {
		_jobBuilder = jobBuilder;

	}
	public JobBuilder<T> OnInterval(TimeSpan interval, ReschedulePolicy reschedulePolicy = ReschedulePolicy.OnFinish, uint? totalIterations = null, DateTime? endDate = null) {
		return OnInterval(DateTime.UtcNow.Add(interval), interval, reschedulePolicy, totalIterations, endDate);
	}

	public JobBuilder<T> OnInterval(DateTime startOn, TimeSpan interval, ReschedulePolicy reschedulePolicy = ReschedulePolicy.OnFinish, uint? totalIterations = null, DateTime? endDate = null) {
		_jobBuilder.Job.AddSchedule(new OnIntervalSchedule<T>(startOn, interval, reschedulePolicy, totalIterations, endDate));
		return _jobBuilder;
	}

	public JobBuilder<T> OnDayOfWeek(DayOfWeek dayOfWeek, TimeSpan timeOfDay, uint? totalIterations = null, DateTime? endDate = null) {
		_jobBuilder.Job.AddSchedule(new OnDayOfWeekScheduler<T>(dayOfWeek, timeOfDay, totalIterations, endDate));
		return _jobBuilder;
	}

	public JobBuilder<T> OnDayOfMonth(int dayOfMonth, TimeSpan timeOfDay, uint? totalIterations = null, DateTime? endDate = null) {
		_jobBuilder.Job.AddSchedule(new OnDayOfMonthScheduler<T>(dayOfMonth, timeOfDay, totalIterations, endDate));
		return _jobBuilder;
	}

	/// <summary>Runs the job whenever a calendar rule fires, in local time. The rule carries its own start, end and repetition.</summary>
	public JobBuilder<T> OnRecurrence(CalendarRecurrence recurrence, uint? totalIterations = null) {
		_jobBuilder.Job.AddSchedule(new OnRecurrenceSchedule<T>(recurrence, totalIterations));
		return _jobBuilder;
	}

	/// <summary>Runs the job at the time of day of <paramref name="start"/>, every <paramref name="interval"/> days from that date.</summary>
	public JobBuilder<T> OnDays(DateTime start, int interval = 1, uint? totalIterations = null)
		=> OnRecurrence(CalendarRecurrence.Daily(start, interval), totalIterations);

	/// <summary>Runs the job on the chosen weekdays of every <paramref name="interval"/> weeks.</summary>
	public JobBuilder<T> OnWeekdays(DateTime start, CalendarDaysOfWeek days, int interval = 1, uint? totalIterations = null)
		=> OnRecurrence(CalendarRecurrence.Weekly(start, days, interval), totalIterations);

	/// <summary>Runs the job on the chosen days of the chosen months, and on each month's last day when asked.</summary>
	public JobBuilder<T> OnDaysOfMonth(DateTime start, int[] daysOfMonth, CalendarMonths months = CalendarMonths.EveryMonth, bool lastDayOfMonth = false, uint? totalIterations = null)
		=> OnRecurrence(CalendarRecurrence.Monthly(start, daysOfMonth, months, lastDayOfMonth), totalIterations);

	/// <summary>Runs the job on the chosen weekdays of the chosen weeks of the chosen months, such as the last Friday of each quarter.</summary>
	public JobBuilder<T> OnWeekdaysOfMonth(DateTime start, CalendarWeeksOfMonth weeks, CalendarDaysOfWeek days, CalendarMonths months = CalendarMonths.EveryMonth, uint? totalIterations = null)
		=> OnRecurrence(CalendarRecurrence.MonthlyDayOfWeek(start, weeks, days, months), totalIterations);

	public T Build() {
		return _jobBuilder.Build();
	}
}


public class ScheduleBuilder : ScheduleBuilder<BaseJob> {

	internal ScheduleBuilder(JobBuilder<BaseJob> jobBuilder) : base(jobBuilder) {

	}
}

