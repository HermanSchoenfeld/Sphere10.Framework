// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;

namespace Sphere10.Framework.Windows.ScheduledTasks;

/// <summary>
/// One trigger of a scheduled task: what starts it and, for the triggers that follow the clock, exactly when and how often. Every
/// frequency Task Scheduler offers is modelled. A clock trigger converts to and from a <see cref="CalendarRecurrence"/>, so tasks
/// can be shown in a calendar and rescheduled there.
/// </summary>
public sealed record WindowsTaskTrigger {
	public WindowsTaskTriggerKind Kind { get; init; }

	/// <summary>Whether the trigger can start the task.</summary>
	public bool Enabled { get; init; } = true;

	/// <summary>When the trigger becomes active, in local time. For a clock trigger its time of day is the time the task starts.</summary>
	public DateTime? StartBoundary { get; init; }

	/// <summary>When the trigger stops being active, in local time, or null.</summary>
	public DateTime? EndBoundary { get; init; }

	/// <summary>Every how many days (Daily) or weeks (Weekly) the trigger fires.</summary>
	public int Interval { get; init; } = 1;

	/// <summary>The weekdays of a weekly or monthly-by-weekday trigger.</summary>
	public CalendarDaysOfWeek DaysOfWeek { get; init; }

	/// <summary>The days (1 to 31) of a monthly trigger.</summary>
	public int[] DaysOfMonth { get; init; } = Array.Empty<int>();

	/// <summary>Whether a monthly trigger also fires on the last day of each chosen month.</summary>
	public bool LastDayOfMonth { get; init; }

	/// <summary>The months of a monthly or monthly-by-weekday trigger.</summary>
	public CalendarMonths Months { get; init; } = CalendarMonths.EveryMonth;

	/// <summary>The weeks of the month of a monthly-by-weekday trigger.</summary>
	public CalendarWeeksOfMonth WeeksOfMonth { get; init; }

	/// <summary>How often the task is started again after the trigger fired, or null.</summary>
	public TimeSpan? RepetitionInterval { get; init; }

	/// <summary>For how long the task keeps being started again, or null for indefinitely.</summary>
	public TimeSpan? RepetitionDuration { get; init; }

	/// <summary>Whether a running instance is stopped when the repetition ends.</summary>
	public bool StopAtDurationEnd { get; init; }

	/// <summary>The longest the task may run when this trigger starts it, or null for the task's own limit.</summary>
	public TimeSpan? ExecutionTimeLimit { get; init; }

	/// <summary>The wait after the triggering moment: a random delay up to this for a clock trigger, a fixed delay for the others. Null is none.</summary>
	public TimeSpan? Delay { get; init; }

	/// <summary>The account whose sign-in or session a Logon or SessionStateChange trigger follows, or null for any user.</summary>
	public string UserId { get; init; }

	/// <summary>The event query of an Event trigger.</summary>
	public string Subscription { get; init; }

	/// <summary>The session change of a SessionStateChange trigger, such as SessionLock or RemoteConnect.</summary>
	public string StateChange { get; init; }

	/// <summary>The element name Windows uses for an <see cref="WindowsTaskTriggerKind.Other"/> trigger.</summary>
	public string OtherName { get; init; }

	/// <summary>Whether the trigger follows the clock, so that it has a place in a calendar.</summary>
	public bool IsClockBased => Kind <= WindowsTaskTriggerKind.MonthlyDayOfWeek;

	/// <summary>The trigger as a recurrence rule, or null for a trigger that does not follow the clock or has no start.</summary>
	public CalendarRecurrence ToRecurrence() {
		if (!IsClockBased || !StartBoundary.HasValue)
			return null;
		return new CalendarRecurrence {
			Frequency = Kind switch {
				WindowsTaskTriggerKind.Daily => CalendarFrequency.Daily,
				WindowsTaskTriggerKind.Weekly => CalendarFrequency.Weekly,
				WindowsTaskTriggerKind.Monthly => CalendarFrequency.Monthly,
				WindowsTaskTriggerKind.MonthlyDayOfWeek => CalendarFrequency.MonthlyDayOfWeek,
				_ => CalendarFrequency.Once
			},
			Start = StartBoundary.Value,
			End = EndBoundary,
			Interval = Math.Max(1, Interval),
			DaysOfWeek = DaysOfWeek,
			DaysOfMonth = DaysOfMonth ?? Array.Empty<int>(),
			LastDayOfMonth = LastDayOfMonth,
			Months = Months,
			WeeksOfMonth = WeeksOfMonth,
			RepeatEvery = RepetitionInterval,
			RepeatFor = RepetitionDuration
		};
	}

	/// <summary>Returns this trigger with the schedule of a recurrence rule, keeping its other settings such as limits and delays.</summary>
	public WindowsTaskTrigger WithRecurrence(CalendarRecurrence recurrence) {
		Guard.ArgumentNotNull(recurrence, nameof(recurrence));
		return this with {
			Kind = recurrence.Frequency switch {
				CalendarFrequency.Daily => WindowsTaskTriggerKind.Daily,
				CalendarFrequency.Weekly => WindowsTaskTriggerKind.Weekly,
				CalendarFrequency.Monthly => WindowsTaskTriggerKind.Monthly,
				CalendarFrequency.MonthlyDayOfWeek => WindowsTaskTriggerKind.MonthlyDayOfWeek,
				_ => WindowsTaskTriggerKind.Time
			},
			StartBoundary = recurrence.Start,
			EndBoundary = recurrence.End,
			Interval = Math.Max(1, recurrence.Interval),
			DaysOfWeek = recurrence.DaysOfWeek,
			DaysOfMonth = recurrence.DaysOfMonth ?? Array.Empty<int>(),
			LastDayOfMonth = recurrence.LastDayOfMonth,
			Months = recurrence.Months,
			WeeksOfMonth = recurrence.WeeksOfMonth,
			RepetitionInterval = recurrence.RepeatEvery,
			RepetitionDuration = recurrence.RepeatFor
		};
	}

	/// <summary>A new clock trigger for a recurrence rule.</summary>
	public static WindowsTaskTrigger FromRecurrence(CalendarRecurrence recurrence) => new WindowsTaskTrigger().WithRecurrence(recurrence);

	/// <summary>The trigger in words.</summary>
	public string Describe() {
		var text = Kind switch {
			WindowsTaskTriggerKind.Boot => "At system startup",
			WindowsTaskTriggerKind.Logon => string.IsNullOrEmpty(UserId) ? "At sign-in of any user" : "At sign-in of " + UserId,
			WindowsTaskTriggerKind.Idle => "When the computer is idle",
			WindowsTaskTriggerKind.Event => "On an event" + (string.IsNullOrEmpty(Subscription) ? string.Empty : ": " + new string(Subscription.Where(character => !char.IsControl(character)).ToArray()).Trim()),
			WindowsTaskTriggerKind.Registration => "When the task is created or changed",
			WindowsTaskTriggerKind.SessionStateChange => "On " + (StateChange ?? "a session change") + (string.IsNullOrEmpty(UserId) ? " of any user" : " of " + UserId),
			WindowsTaskTriggerKind.Other => "On " + (OtherName ?? "a custom trigger"),
			_ => ToRecurrence()?.Describe() ?? "At no set time"
		};
		if (!IsClockBased && RepetitionInterval.HasValue)
			text += $", then every {RepetitionInterval.Value}";
		if (Delay.HasValue && Delay.Value > TimeSpan.Zero)
			text += IsClockBased ? $", delayed at random by up to {Delay.Value}" : $", after {Delay.Value}";
		return Enabled ? text : text + " (disabled)";
	}

	// The day lists are compared by content, so a trigger read twice equals itself.
	public bool Equals(WindowsTaskTrigger other)
		=> other != null && Kind == other.Kind && Enabled == other.Enabled && StartBoundary == other.StartBoundary && EndBoundary == other.EndBoundary && Interval == other.Interval
			&& DaysOfWeek == other.DaysOfWeek && (DaysOfMonth ?? Array.Empty<int>()).SequenceEqual(other.DaysOfMonth ?? Array.Empty<int>()) && LastDayOfMonth == other.LastDayOfMonth
			&& Months == other.Months && WeeksOfMonth == other.WeeksOfMonth && RepetitionInterval == other.RepetitionInterval && RepetitionDuration == other.RepetitionDuration
			&& StopAtDurationEnd == other.StopAtDurationEnd && ExecutionTimeLimit == other.ExecutionTimeLimit && Delay == other.Delay && UserId == other.UserId
			&& Subscription == other.Subscription && StateChange == other.StateChange && OtherName == other.OtherName;

	public override int GetHashCode() => HashCode.Combine(Kind, StartBoundary, Interval, DaysOfWeek, Months, WeeksOfMonth, UserId, Subscription);
}
