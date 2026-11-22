// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Sphere10.Framework;

/// <summary>
/// When something happens and how it repeats: once, every few days, on weekdays of every few weeks, on days of chosen months, or on
/// weekdays of chosen weeks of chosen months. It answers which occurrences fall in a period and what the next one is. The rule is
/// independent of any calendar system or scheduler, so a person's appointments, a job schedule and an operating system's tasks can
/// share it.
/// </summary>
/// <remarks>
/// Times are plain <see cref="DateTime"/> values in one time zone of the caller's choosing; the rule never converts them. An
/// occurrence takes the time of day of <see cref="Start"/>. <see cref="RepeatEvery"/> describes repetition inside each occurrence
/// (every ten minutes for an hour, say) and is reported by <see cref="GetRunTimes"/>.
/// </remarks>
public sealed record CalendarRecurrence {
	// A rule can skip years (29 February, the fifth Monday of a month), so a search for the next occurrence looks this far ahead.
	private const int SearchYears = 12;

	public CalendarFrequency Frequency { get; init; }

	/// <summary>The date and time of day of the first possible occurrence. No occurrence is earlier.</summary>
	public DateTime Start { get; init; }

	/// <summary>The last moment an occurrence may start, or null when the rule never ends.</summary>
	public DateTime? End { get; init; }

	/// <summary>Every how many days (<see cref="CalendarFrequency.Daily"/>) or weeks (<see cref="CalendarFrequency.Weekly"/>) the rule applies.</summary>
	public int Interval { get; init; } = 1;

	/// <summary>The weekdays of a weekly or monthly-by-weekday rule.</summary>
	public CalendarDaysOfWeek DaysOfWeek { get; init; }

	/// <summary>The days (1 to 31) of a monthly rule. A day a month does not have is skipped in that month.</summary>
	public int[] DaysOfMonth { get; init; } = Array.Empty<int>();

	/// <summary>Whether a monthly rule also falls on the last day of each chosen month.</summary>
	public bool LastDayOfMonth { get; init; }

	/// <summary>The months of a monthly or monthly-by-weekday rule.</summary>
	public CalendarMonths Months { get; init; } = CalendarMonths.EveryMonth;

	/// <summary>The weeks of the month of a monthly-by-weekday rule.</summary>
	public CalendarWeeksOfMonth WeeksOfMonth { get; init; }

	/// <summary>How often each occurrence repeats after it starts, or null when it happens once.</summary>
	public TimeSpan? RepeatEvery { get; init; }

	/// <summary>For how long each occurrence keeps repeating, or null to repeat until the next occurrence.</summary>
	public TimeSpan? RepeatFor { get; init; }

	public static CalendarRecurrence Once(DateTime start) => new() { Frequency = CalendarFrequency.Once, Start = start };

	public static CalendarRecurrence Daily(DateTime start, int interval = 1) => new() { Frequency = CalendarFrequency.Daily, Start = start, Interval = interval };

	public static CalendarRecurrence Weekly(DateTime start, CalendarDaysOfWeek days, int interval = 1)
		=> new() { Frequency = CalendarFrequency.Weekly, Start = start, DaysOfWeek = days, Interval = interval };

	public static CalendarRecurrence Monthly(DateTime start, int[] daysOfMonth, CalendarMonths months = CalendarMonths.EveryMonth, bool lastDayOfMonth = false)
		=> new() { Frequency = CalendarFrequency.Monthly, Start = start, DaysOfMonth = daysOfMonth ?? Array.Empty<int>(), Months = months, LastDayOfMonth = lastDayOfMonth };

	public static CalendarRecurrence MonthlyDayOfWeek(DateTime start, CalendarWeeksOfMonth weeks, CalendarDaysOfWeek days, CalendarMonths months = CalendarMonths.EveryMonth)
		=> new() { Frequency = CalendarFrequency.MonthlyDayOfWeek, Start = start, WeeksOfMonth = weeks, DaysOfWeek = days, Months = months };

	/// <summary>The weekday set that contains only this day.</summary>
	public static CalendarDaysOfWeek ToDays(DayOfWeek day) => (CalendarDaysOfWeek)(1 << (int)day);

	/// <summary>The month set that contains only this month (1 to 12).</summary>
	public static CalendarMonths ToMonths(int month) {
		Guard.ArgumentInRange(month, 1, 12, nameof(month));
		return (CalendarMonths)(1 << (month - 1));
	}

	/// <summary>Reports every way the rule cannot produce an occurrence or is out of range.</summary>
	public Result Validate() {
		var result = Result.Default;
		if (!Enum.IsDefined(Frequency))
			result.AddError("Choose how the rule repeats.");
		if (Interval < 1)
			result.AddError("The interval must be at least 1.");
		if (End.HasValue && End.Value < Start)
			result.AddError("The rule ends before it starts.");
		if (Frequency is CalendarFrequency.Weekly or CalendarFrequency.MonthlyDayOfWeek && (DaysOfWeek & CalendarDaysOfWeek.EveryDay) == CalendarDaysOfWeek.None)
			result.AddError("Choose at least one weekday.");
		if (Frequency == CalendarFrequency.Monthly && !LastDayOfMonth && (DaysOfMonth == null || DaysOfMonth.Length == 0))
			result.AddError("Choose at least one day of the month.");
		if (DaysOfMonth != null && DaysOfMonth.Any(day => day is < 1 or > 31))
			result.AddError("Days of the month run from 1 to 31.");
		if (Frequency is CalendarFrequency.Monthly or CalendarFrequency.MonthlyDayOfWeek && (Months & CalendarMonths.EveryMonth) == CalendarMonths.None)
			result.AddError("Choose at least one month.");
		if (Frequency == CalendarFrequency.MonthlyDayOfWeek && WeeksOfMonth == CalendarWeeksOfMonth.None)
			result.AddError("Choose at least one week of the month.");
		if (RepeatEvery.HasValue && RepeatEvery.Value <= TimeSpan.Zero)
			result.AddError("The repeat interval must be positive.");
		if (RepeatFor.HasValue && (!RepeatEvery.HasValue || RepeatFor.Value < RepeatEvery.Value))
			result.AddError("Repeating for a time needs a repeat interval no longer than that time.");
		return result;
	}

	/// <summary>Whether the rule has an occurrence on this date, at the time of day of <see cref="Start"/>.</summary>
	public bool OccursOn(DateTime date) {
		var day = date.Date;
		var occurrence = day + Start.TimeOfDay;
		if (occurrence < Start || End.HasValue && occurrence > End.Value)
			return false;
		switch (Frequency) {
			case CalendarFrequency.Once:
				return day == Start.Date;
			case CalendarFrequency.Daily:
				return (day - Start.Date).Days % Math.Max(1, Interval) == 0;
			case CalendarFrequency.Weekly:
				// Weeks are counted from the Sunday of the starting week, so every chosen weekday of an included week occurs.
				var firstWeek = Start.Date.AddDays(-(int)Start.DayOfWeek);
				return DaysOfWeek.HasFlag(ToDays(day.DayOfWeek)) && (day - firstWeek).Days / 7 % Math.Max(1, Interval) == 0;
			case CalendarFrequency.Monthly:
				return Months.HasFlag(ToMonths(day.Month))
					&& (DaysOfMonth != null && DaysOfMonth.Contains(day.Day) || LastDayOfMonth && day.Day == DateTime.DaysInMonth(day.Year, day.Month));
			case CalendarFrequency.MonthlyDayOfWeek:
				if (!Months.HasFlag(ToMonths(day.Month)) || !DaysOfWeek.HasFlag(ToDays(day.DayOfWeek)))
					return false;
				var week = (CalendarWeeksOfMonth)(1 << Math.Min(3, (day.Day - 1) / 7));
				// The fifth occurrence of a weekday has no ordinal of its own; it is only ever the last.
				return day.Day <= 28 && WeeksOfMonth.HasFlag(week) || WeeksOfMonth.HasFlag(CalendarWeeksOfMonth.Last) && day.Day + 7 > DateTime.DaysInMonth(day.Year, day.Month);
			default:
				return false;
		}
	}

	/// <summary>The occurrences that start at or after <paramref name="from"/> and before <paramref name="to"/>, in order.</summary>
	public IEnumerable<DateTime> GetOccurrences(DateTime from, DateTime to) {
		var last = End.HasValue && End.Value < to ? End.Value : to;
		for (var day = (from > Start ? from : Start).Date; day <= last.Date; day = day.AddDays(1)) {
			var occurrence = day + Start.TimeOfDay;
			if (occurrence >= from && occurrence < to && OccursOn(day))
				yield return occurrence;
			if (Frequency == CalendarFrequency.Once && day >= Start.Date)
				yield break;
		}
	}

	/// <summary>The first occurrence after <paramref name="after"/>, or null when the rule has ended or has none within twelve years.</summary>
	public DateTime? GetNextOccurrence(DateTime after) {
		var from = after < Start ? Start : after.AddTicks(1);
		foreach (var occurrence in GetOccurrences(from, from.AddYears(SearchYears)))
			return occurrence;
		return null;
	}

	/// <summary>Every moment the rule fires in a period: each occurrence and, when it repeats, each repetition after it.</summary>
	/// <remarks>Repetitions of an occurrence that started before <paramref name="from"/> are included when they fall in the period.</remarks>
	public IEnumerable<DateTime> GetRunTimes(DateTime from, DateTime to) {
		if (!RepeatEvery.HasValue || RepeatEvery.Value <= TimeSpan.Zero) {
			foreach (var occurrence in GetOccurrences(from, to))
				yield return occurrence;
			yield break;
		}
		// An occurrence may still be repeating when the period begins.
		foreach (var occurrence in GetOccurrencesUnderWay(from).Concat(GetOccurrences(from, to))) {
			var until = GetRepetitionEnd(occurrence);
			for (var run = occurrence; run <= until && run < to; run += RepeatEvery.Value)
				if (run >= from)
					yield return run;
		}
	}

	/// <summary>The first moment the rule fires after <paramref name="after"/>, whether an occurrence or a repetition of one, or
	/// null when the rule has ended or has none within twelve years.</summary>
	public DateTime? GetNextRunTime(DateTime after) {
		var next = GetNextOccurrence(after);
		if (!RepeatEvery.HasValue || RepeatEvery.Value <= TimeSpan.Zero)
			return next;
		// A repetition of an occurrence already under way comes before the next occurrence.
		foreach (var occurrence in GetOccurrencesUnderWay(after.AddTicks(1))) {
			var run = occurrence + TimeSpan.FromTicks(((after - occurrence).Ticks / RepeatEvery.Value.Ticks + 1) * RepeatEvery.Value.Ticks);
			if (run <= GetRepetitionEnd(occurrence) && (!next.HasValue || run < next.Value))
				next = run;
		}
		return next;
	}

	/// <summary>When the repetitions of an occurrence stop: after <see cref="RepeatFor"/>, or just before the next occurrence.</summary>
	public DateTime GetRepetitionEnd(DateTime occurrence) {
		if (!RepeatEvery.HasValue)
			return occurrence;
		if (RepeatFor.HasValue)
			return occurrence + RepeatFor.Value;
		var next = GetNextOccurrence(occurrence);
		var limit = next ?? occurrence.AddDays(1);
		return End.HasValue && End.Value < limit ? End.Value : limit.AddTicks(-1);
	}

	/// <summary>The rule in words, such as "Weekly on Monday, Wednesday at 03:00, every 2 weeks".</summary>
	public string Describe() {
		var time = Start.ToString("HH:mm", CultureInfo.InvariantCulture);
		var text = Frequency switch {
			CalendarFrequency.Once => $"Once on {Start:yyyy-MM-dd} at {time}",
			CalendarFrequency.Daily => Interval > 1 ? $"Every {Interval} days at {time}" : $"Daily at {time}",
			CalendarFrequency.Weekly => $"Weekly on {Describe(DaysOfWeek)} at {time}" + (Interval > 1 ? $", every {Interval} weeks" : string.Empty),
			CalendarFrequency.Monthly => $"Monthly on {DescribeDaysOfMonth()}{DescribeMonths()} at {time}",
			CalendarFrequency.MonthlyDayOfWeek => $"Monthly on the {Describe(WeeksOfMonth)} {Describe(DaysOfWeek)}{DescribeMonths()} at {time}",
			_ => Frequency.ToString()
		};
		if (RepeatEvery.HasValue)
			text += $", repeating every {Describe(RepeatEvery.Value)}" + (RepeatFor.HasValue ? $" for {Describe(RepeatFor.Value)}" : string.Empty);
		if (End.HasValue)
			text += $", until {End.Value:yyyy-MM-dd HH:mm}";
		return text;
	}

	// The occurrences before a moment whose repetitions can still be under way at it: those within the repetition time, or the
	// latest one when it repeats until the next.
	private IEnumerable<DateTime> GetOccurrencesUnderWay(DateTime at) {
		if (RepeatFor.HasValue)
			return GetOccurrences(at - RepeatFor.Value, at);
		var latest = End.HasValue && End.Value < at ? End.Value : at;
		for (var day = latest.Date; day >= Start.Date && day > latest.Date.AddYears(-SearchYears); day = day.AddDays(-1)) {
			var occurrence = day + Start.TimeOfDay;
			if (occurrence < at && OccursOn(day))
				return new[] { occurrence };
		}
		return Array.Empty<DateTime>();
	}

	private string DescribeDaysOfMonth() {
		var days = (DaysOfMonth ?? Array.Empty<int>()).OrderBy(day => day).Select(day => "day " + day.ToString(CultureInfo.InvariantCulture)).ToList();
		if (LastDayOfMonth)
			days.Add("the last day");
		return days.Count == 0 ? "no day" : string.Join(", ", days);
	}

	private string DescribeMonths()
		=> (Months & CalendarMonths.EveryMonth) == CalendarMonths.EveryMonth ? string.Empty
			: " of " + string.Join(", ", Enumerable.Range(1, 12).Where(month => Months.HasFlag(ToMonths(month))).Select(month => CultureInfo.InvariantCulture.DateTimeFormat.GetMonthName(month)));

	private static string Describe(CalendarDaysOfWeek days) => (days & CalendarDaysOfWeek.EveryDay) switch {
		CalendarDaysOfWeek.EveryDay => "every day",
		CalendarDaysOfWeek.Weekdays => "weekdays",
		CalendarDaysOfWeek.None => "no day",
		_ => string.Join(", ", Enum.GetValues<DayOfWeek>().Where(day => days.HasFlag(ToDays(day))).Select(day => day.ToString()))
	};

	private static string Describe(CalendarWeeksOfMonth weeks) {
		var names = new[] { CalendarWeeksOfMonth.First, CalendarWeeksOfMonth.Second, CalendarWeeksOfMonth.Third, CalendarWeeksOfMonth.Fourth, CalendarWeeksOfMonth.Last }
			.Where(week => weeks.HasFlag(week)).Select(week => week.ToString().ToLowerInvariant()).ToArray();
		return names.Length == 0 ? "no" : string.Join(", ", names);
	}

	private static string Describe(TimeSpan span)
		=> span.TotalDays >= 1 && span.TotalDays % 1 == 0 ? Plural((int)span.TotalDays, "day") : span.TotalHours >= 1 && span.TotalHours % 1 == 0 ? Plural((int)span.TotalHours, "hour")
			: span.TotalMinutes >= 1 && span.TotalMinutes % 1 == 0 ? Plural((int)span.TotalMinutes, "minute") : Plural((int)span.TotalSeconds, "second");

	private static string Plural(int count, string unit) => count == 1 ? "1 " + unit : $"{count} {unit}s";
}
