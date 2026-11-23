// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;

namespace Sphere10.Framework;

/// <summary>
/// Something that happens at set times: a meeting in a person's calendar, a job in a schedule, a task the operating system runs.
/// It has a title, a rule for when it occurs and how long each occurrence lasts. <see cref="Tag"/> carries the calendar's own record.
/// </summary>
/// <typeparam name="TLocation">Where an event takes place or what it acts on, as the calendar records it, such as a room name or a program.</typeparam>
/// <typeparam name="TDecoration">How a view may present an event, such as a color, as the calendar defines it.</typeparam>
/// <typeparam name="TTag">The calendar's own record of an event, such as an appointment row or a scheduled task.</typeparam>
public sealed record CalendarEvent<TLocation, TDecoration, TTag> {
	/// <summary>The identity of the event within its calendar, used to find it again when it is changed or removed.</summary>
	public string Id { get; init; }

	public string Title { get; init; }

	public string Description { get; init; }

	/// <summary>Where the event takes place or what it acts on, such as a room or a program.</summary>
	public TLocation Location { get; init; }

	/// <summary>When the event occurs.</summary>
	public CalendarRecurrence Recurrence { get; init; }

	/// <summary>How long each occurrence lasts. Zero is a moment, such as the start of a task.</summary>
	public TimeSpan Duration { get; init; }

	/// <summary>Whether the event takes place. A disabled event keeps its place in the calendar but is not carried out.</summary>
	public bool Enabled { get; init; } = true;

	/// <summary>How a view may present the event, such as its color. A view uses its own choice when this is the default.</summary>
	public TDecoration Decoration { get; init; }

	/// <summary>The calendar's own record of the event, such as an appointment row or a scheduled task.</summary>
	public TTag Tag { get; init; }

	/// <summary>Returns this event as an event of another calendar type: the same identity, title, description, rule, duration and state,
	/// with that calendar's location, decoration and tag.</summary>
	public CalendarEvent<TOtherLocation, TOtherDecoration, TOtherTag> Project<TOtherLocation, TOtherDecoration, TOtherTag>(TOtherLocation location, TOtherDecoration decoration, TOtherTag tag)
		=> new() {
			Id = Id,
			Title = Title,
			Description = Description,
			Location = location,
			Recurrence = Recurrence,
			Duration = Duration,
			Enabled = Enabled,
			Decoration = decoration,
			Tag = tag
		};

	/// <summary>Returns this event with one of its occurrences given new times, as when a user drags it in a calendar view. A recurring
	/// event moves as a whole, so every occurrence shifts with the one that was moved.</summary>
	/// <remarks>The rule's start moves by the same amount. When the move crosses days, a weekly or monthly-by-weekday rule turns its
	/// weekdays by that many days, and a monthly rule replaces the moved day of the month with the new one. A new length applies to an
	/// event that does not repeat within its occurrences.</remarks>
	public CalendarEvent<TLocation, TDecoration, TTag> Reschedule(CalendarOccurrence<TLocation, TDecoration, TTag> occurrence, DateTime newStart, DateTime newEnd) {
		Guard.ArgumentNotNull(occurrence, nameof(occurrence));
		Guard.Argument(newEnd >= newStart, nameof(newEnd), "An occurrence cannot end before it starts.");
		Guard.Ensure(Recurrence != null, "A calendar event needs a recurrence to be rescheduled.");
		var days = (newStart.Date - occurrence.Start.Date).Days;
		var rule = Recurrence with { Start = Recurrence.Start + (newStart - occurrence.Start) };
		if (days != 0) {
			if (rule.Frequency is CalendarFrequency.Weekly or CalendarFrequency.MonthlyDayOfWeek)
				rule = rule with { DaysOfWeek = Turn(rule.DaysOfWeek, days) };
			if (rule.Frequency == CalendarFrequency.Monthly && rule.DaysOfMonth != null && Array.IndexOf(rule.DaysOfMonth, occurrence.Start.Day) >= 0)
				rule = rule with { DaysOfMonth = Array.ConvertAll(rule.DaysOfMonth, day => day == occurrence.Start.Day ? newStart.Day : day) };
			if (rule.Frequency == CalendarFrequency.MonthlyDayOfWeek && rule.WeeksOfMonth != CalendarWeeksOfMonth.Last) {
				var from = (CalendarWeeksOfMonth)(1 << Math.Min(3, (occurrence.Start.Day - 1) / 7));
				var to = (CalendarWeeksOfMonth)(1 << Math.Min(3, (newStart.Day - 1) / 7));
				if (from != to && rule.WeeksOfMonth.HasFlag(from))
					rule = rule with { WeeksOfMonth = rule.WeeksOfMonth & ~from | to };
			}
		}
		return this with { Recurrence = rule, Duration = Recurrence.RepeatEvery.HasValue ? Duration : newEnd - newStart };
	}

	// Moves every chosen weekday on by a number of days, wrapping through the week.
	private static CalendarDaysOfWeek Turn(CalendarDaysOfWeek days, int by) {
		var turned = CalendarDaysOfWeek.None;
		for (var day = 0; day < 7; day++)
			if (days.HasFlag((CalendarDaysOfWeek)(1 << day)))
				turned |= (CalendarDaysOfWeek)(1 << ((day + by % 7 + 7) % 7));
		return turned;
	}

	/// <summary>The occurrences that overlap the period from <paramref name="from"/> up to <paramref name="to"/>, in order. An occurrence
	/// that repeats within itself spans from its first to its last repetition.</summary>
	public IEnumerable<CalendarOccurrence<TLocation, TDecoration, TTag>> GetOccurrences(DateTime from, DateTime to) {
		Guard.Ensure(Recurrence != null, "A calendar event needs a recurrence to occur.");
		// An occurrence that began before the period can still reach into it.
		var reach = Duration + (Recurrence.RepeatEvery.HasValue ? Recurrence.RepeatFor ?? TimeSpan.FromDays(1) : TimeSpan.Zero);
		foreach (var start in Recurrence.GetOccurrences(from - reach, to)) {
			var end = (Recurrence.RepeatEvery.HasValue ? Recurrence.GetRepetitionEnd(start) : start) + Duration;
			if (end >= from)
				yield return new CalendarOccurrence<TLocation, TDecoration, TTag>(this, start, end);
		}
	}
}
