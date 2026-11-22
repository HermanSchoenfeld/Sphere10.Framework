// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class CalendarRecurrenceTests {
	// 2026-11-02 is a Monday.
	private static readonly DateTime Monday = new(2026, 11, 2, 3, 30, 0);

	[Test]
	public void OnceOccursOnlyAtItsStart() {
		var rule = CalendarRecurrence.Once(Monday);
		Assert.That(rule.GetOccurrences(Monday.AddDays(-1), Monday.AddDays(30)), Is.EqualTo(new[] { Monday }));
		Assert.That(rule.GetOccurrences(Monday.AddTicks(1), Monday.AddDays(30)), Is.Empty);
		Assert.That(rule.GetNextOccurrence(Monday.AddDays(-5)), Is.EqualTo(Monday));
		Assert.That(rule.GetNextOccurrence(Monday), Is.Null, "The next occurrence is strictly later.");
		Assert.That(rule.Describe(), Is.EqualTo("Once on 2026-11-02 at 03:30"));
	}

	[Test]
	public void DailyHonoursItsIntervalStartAndEnd() {
		var rule = CalendarRecurrence.Daily(Monday, 3) with { End = Monday.AddDays(7) };
		Assert.That(rule.GetOccurrences(Monday.AddDays(-10), Monday.AddDays(30)), Is.EqualTo(new[] { Monday, Monday.AddDays(3), Monday.AddDays(6) }));
		Assert.That(rule.GetNextOccurrence(Monday.AddDays(3)), Is.EqualTo(Monday.AddDays(6)));
		Assert.That(rule.GetNextOccurrence(Monday.AddDays(6)), Is.Null, "Nothing occurs after the end.");
		Assert.That(rule.Describe(), Is.EqualTo("Every 3 days at 03:30, until 2026-11-09 03:30"));
		Assert.That(CalendarRecurrence.Daily(Monday).Describe(), Is.EqualTo("Daily at 03:30"));
	}

	[Test]
	public void WeeklyOccursOnEachChosenWeekdayOfEveryIncludedWeek() {
		var rule = CalendarRecurrence.Weekly(Monday, CalendarDaysOfWeek.Monday | CalendarDaysOfWeek.Friday, 2);
		Assert.That(rule.GetOccurrences(Monday, Monday.AddDays(28)), Is.EqualTo(new[] { Monday, Monday.AddDays(4), Monday.AddDays(14), Monday.AddDays(18) }));
		Assert.That(rule.OccursOn(Monday.AddDays(7)), Is.False, "The week between is skipped.");
		Assert.That(rule.Describe(), Is.EqualTo("Weekly on Monday, Friday at 03:30, every 2 weeks"));
		// A rule that starts midweek does not occur on that week's earlier chosen days.
		var midweek = CalendarRecurrence.Weekly(Monday.AddDays(2), CalendarDaysOfWeek.Monday | CalendarDaysOfWeek.Friday);
		Assert.That(midweek.GetOccurrences(Monday.AddDays(-7), Monday.AddDays(8)), Is.EqualTo(new[] { Monday.AddDays(4), Monday.AddDays(7) }));
		Assert.That(CalendarRecurrence.Weekly(Monday, CalendarDaysOfWeek.Weekdays).Describe(), Is.EqualTo("Weekly on weekdays at 03:30"));
	}

	[Test]
	public void MonthlyOccursOnChosenDaysOfChosenMonthsAndSkipsDaysAMonthLacks() {
		var start = new DateTime(2026, 1, 1, 6, 0, 0);
		var rule = CalendarRecurrence.Monthly(start, new[] { 15, 31 }, CalendarMonths.January | CalendarMonths.February | CalendarMonths.April);
		Assert.That(rule.GetOccurrences(start, start.AddYears(1)).Select(time => time.ToString("MM-dd")), Is.EqualTo(new[] { "01-15", "01-31", "02-15", "04-15" }),
			"February and April have no 31st.");
		var lastDay = CalendarRecurrence.Monthly(start, null, lastDayOfMonth: true);
		Assert.That(lastDay.GetOccurrences(start, new DateTime(2026, 4, 1)).Select(time => time.Day), Is.EqualTo(new[] { 31, 28, 31 }));
		Assert.That(rule.Describe(), Is.EqualTo("Monthly on day 15, day 31 of January, February, April at 06:00"));
		Assert.That(lastDay.Describe(), Is.EqualTo("Monthly on the last day at 06:00"));
	}

	[Test]
	public void MonthlyDayOfWeekFindsOrdinalAndLastWeekdays() {
		var start = new DateTime(2026, 1, 1, 9, 0, 0);
		var secondTuesday = CalendarRecurrence.MonthlyDayOfWeek(start, CalendarWeeksOfMonth.Second, CalendarDaysOfWeek.Tuesday);
		Assert.That(secondTuesday.GetOccurrences(start, new DateTime(2026, 4, 1)).Select(time => time.ToString("MM-dd")), Is.EqualTo(new[] { "01-13", "02-10", "03-10" }));
		var lastFriday = CalendarRecurrence.MonthlyDayOfWeek(start, CalendarWeeksOfMonth.Last, CalendarDaysOfWeek.Friday, CalendarMonths.January | CalendarMonths.May);
		Assert.That(lastFriday.GetOccurrences(start, start.AddYears(1)).Select(time => time.ToString("MM-dd")), Is.EqualTo(new[] { "01-30", "05-29" }));
		// January 2026 has five Fridays; the fifth is the last one and never the fourth.
		var fourthFriday = CalendarRecurrence.MonthlyDayOfWeek(start, CalendarWeeksOfMonth.Fourth, CalendarDaysOfWeek.Friday, CalendarMonths.January);
		Assert.That(fourthFriday.GetOccurrences(start, start.AddMonths(1)).Select(time => time.Day), Is.EqualTo(new[] { 23 }));
		Assert.That(secondTuesday.Describe(), Is.EqualTo("Monthly on the second Tuesday at 09:00"));
	}

	[Test]
	public void ARareRuleStillFindsItsNextOccurrence() {
		var leapDay = CalendarRecurrence.Monthly(new DateTime(2026, 1, 1, 0, 0, 0), new[] { 29 }, CalendarMonths.February);
		Assert.That(leapDay.GetNextOccurrence(new DateTime(2026, 1, 1)), Is.EqualTo(new DateTime(2028, 2, 29)));
	}

	[Test]
	public void RunTimesIncludeRepetitionsUntilTheirDurationOrTheNextOccurrence() {
		var bounded = CalendarRecurrence.Daily(Monday) with { RepeatEvery = TimeSpan.FromMinutes(20), RepeatFor = TimeSpan.FromHours(1) };
		Assert.That(bounded.GetRunTimes(Monday, Monday.AddHours(3)), Is.EqualTo(new[] { Monday, Monday.AddMinutes(20), Monday.AddMinutes(40), Monday.AddMinutes(60) }));
		Assert.That(bounded.GetRepetitionEnd(Monday), Is.EqualTo(Monday.AddHours(1)));
		// A repetition without a duration keeps going until the next occurrence begins.
		var unbounded = CalendarRecurrence.Daily(Monday) with { RepeatEvery = TimeSpan.FromHours(8) };
		Assert.That(unbounded.GetRunTimes(Monday, Monday.AddDays(2)), Is.EqualTo(new[] { Monday, Monday.AddHours(8), Monday.AddHours(16), Monday.AddDays(1), Monday.AddHours(32), Monday.AddHours(40) }));
		// A period that begins inside an earlier occurrence still sees its remaining repetitions.
		Assert.That(bounded.GetRunTimes(Monday.AddMinutes(30), Monday.AddHours(2)), Is.EqualTo(new[] { Monday.AddMinutes(40), Monday.AddMinutes(60) }));
		Assert.That(bounded.Describe(), Is.EqualTo("Daily at 03:30, repeating every 20 minutes for 1 hour"));
	}

	[Test]
	public void TheNextRunIsTheNextRepetitionOrOccurrenceWhicheverIsFirst() {
		var plain = CalendarRecurrence.Daily(Monday);
		Assert.That((plain.GetNextRunTime(Monday.AddDays(-5)), plain.GetNextRunTime(Monday), plain.GetNextRunTime(Monday.AddHours(1))),
			Is.EqualTo(((DateTime?)Monday, (DateTime?)Monday.AddDays(1), (DateTime?)Monday.AddDays(1))), "The moment itself does not count.");
		var bounded = CalendarRecurrence.Daily(Monday) with { RepeatEvery = TimeSpan.FromMinutes(20), RepeatFor = TimeSpan.FromHours(1) };
		Assert.That(new[] { Monday, Monday.AddMinutes(25), Monday.AddMinutes(40), Monday.AddMinutes(60), Monday.AddHours(5) }.Select(bounded.GetNextRunTime),
			Is.EqualTo(new DateTime?[] { Monday.AddMinutes(20), Monday.AddMinutes(40), Monday.AddMinutes(60), Monday.AddDays(1), Monday.AddDays(1) }));
		// A weekly occurrence that repeats without a duration is still repeating days later, until the next one begins.
		var weekly = CalendarRecurrence.Weekly(Monday, CalendarDaysOfWeek.Monday) with { RepeatEvery = TimeSpan.FromHours(10) };
		Assert.That(weekly.GetNextRunTime(Monday.AddDays(3)), Is.EqualTo(Monday.AddHours(80)));
		Assert.That(weekly.GetRunTimes(Monday.AddDays(3), Monday.AddDays(4)), Is.EqualTo(new[] { Monday.AddHours(80), Monday.AddHours(90) }));
		Assert.That(weekly.GetNextRunTime(Monday.AddHours(160)), Is.EqualTo(Monday.AddDays(7)), "The next occurrence comes before the repetition after it.");
		Assert.That((CalendarRecurrence.Once(Monday) with { RepeatEvery = TimeSpan.FromHours(1), RepeatFor = TimeSpan.FromHours(2) }).GetNextRunTime(Monday.AddHours(2)), Is.Null);
	}

	[Test]
	public void ValidationNamesEachProblem() {
		Assert.That(CalendarRecurrence.Daily(Monday).Validate().IsSuccess, Is.True);
		Assert.That(CalendarRecurrence.Daily(Monday, 0).Validate().ErrorMessages, Has.Some.Contains("interval"));
		Assert.That(CalendarRecurrence.Weekly(Monday, CalendarDaysOfWeek.None).Validate().ErrorMessages, Has.Some.Contains("weekday"));
		Assert.That(CalendarRecurrence.Monthly(Monday, Array.Empty<int>()).Validate().ErrorMessages, Has.Some.Contains("day of the month"));
		Assert.That(CalendarRecurrence.Monthly(Monday, new[] { 32 }).Validate().ErrorMessages, Has.Some.Contains("1 to 31"));
		Assert.That(CalendarRecurrence.MonthlyDayOfWeek(Monday, CalendarWeeksOfMonth.None, CalendarDaysOfWeek.Monday).Validate().ErrorMessages, Has.Some.Contains("week of the month"));
		Assert.That((CalendarRecurrence.Daily(Monday) with { End = Monday.AddDays(-1) }).Validate().ErrorMessages, Has.Some.Contains("ends before"));
		Assert.That((CalendarRecurrence.Daily(Monday) with { RepeatFor = TimeSpan.FromHours(1) }).Validate().ErrorMessages, Has.Some.Contains("repeat interval"));
	}

	[Test]
	public void AnEventsOccurrencesSpanItsDurationAndRepetitions() {
		var meeting = new CalendarEvent { Title = "Review", Recurrence = CalendarRecurrence.Weekly(Monday, CalendarDaysOfWeek.Monday), Duration = TimeSpan.FromHours(2) };
		var occurrences = meeting.GetOccurrences(Monday.AddHours(1), Monday.AddDays(8)).ToArray();
		Assert.That(occurrences.Select(item => (item.Start, item.End)), Is.EqualTo(new[] { (Monday, Monday.AddHours(2)), (Monday.AddDays(7), Monday.AddDays(7).AddHours(2)) }),
			"An occurrence that began before the period but reaches into it is included.");
		Assert.That(occurrences[0].IsRecurring, Is.True);
		var task = new CalendarEvent { Title = "Sync", Recurrence = CalendarRecurrence.Daily(Monday) with { RepeatEvery = TimeSpan.FromMinutes(15), RepeatFor = TimeSpan.FromHours(1) } };
		var run = task.GetOccurrences(Monday, Monday.AddDays(1)).Single();
		Assert.That((run.Start, run.End), Is.EqualTo((Monday, Monday.AddHours(1))), "A repeating occurrence spans from its first to its last repetition.");
		Assert.That(new CalendarEvent { Title = "Launch", Recurrence = CalendarRecurrence.Once(Monday) }.GetOccurrences(Monday, Monday.AddDays(1)).Single().IsRecurring, Is.False);
	}

	[Test]
	public void ReschedulingOneOccurrenceMovesTheWholeEvent() {
		var weekly = new CalendarEvent { Title = "Backup", Recurrence = CalendarRecurrence.Weekly(Monday, CalendarDaysOfWeek.Monday | CalendarDaysOfWeek.Thursday), Duration = TimeSpan.FromHours(1) };
		var occurrence = weekly.GetOccurrences(Monday, Monday.AddDays(1)).Single();
		// Dragged from Monday 03:30 to Tuesday 05:00 and stretched to two hours.
		var moved = weekly.Reschedule(occurrence, Monday.AddDays(1).AddMinutes(90), Monday.AddDays(1).AddMinutes(210));
		Assert.That(moved.Recurrence.DaysOfWeek, Is.EqualTo(CalendarDaysOfWeek.Tuesday | CalendarDaysOfWeek.Friday), "Every chosen weekday turns with the one that was moved.");
		Assert.That(moved.Recurrence.Start, Is.EqualTo(Monday.AddDays(1).AddMinutes(90)));
		Assert.That(moved.Duration, Is.EqualTo(TimeSpan.FromHours(2)));
		Assert.That(moved.GetOccurrences(Monday, Monday.AddDays(7)).Select(item => item.Start), Is.EqualTo(new[] { Monday.AddDays(1).AddMinutes(90), Monday.AddDays(4).AddMinutes(90) }));

		var monthly = new CalendarEvent { Title = "Invoice", Recurrence = CalendarRecurrence.Monthly(new DateTime(2026, 11, 1, 9, 0, 0), new[] { 5, 20 }) };
		var fifth = monthly.GetOccurrences(new DateTime(2026, 11, 1), new DateTime(2026, 11, 10)).Single();
		var later = monthly.Reschedule(fifth, new DateTime(2026, 11, 8, 10, 0, 0), new DateTime(2026, 11, 8, 10, 0, 0));
		Assert.That(later.Recurrence.DaysOfMonth, Is.EqualTo(new[] { 8, 20 }), "Only the day that was moved changes.");
		Assert.That(later.Recurrence.Start.TimeOfDay, Is.EqualTo(TimeSpan.FromHours(10)));

		var secondTuesday = new CalendarEvent { Title = "Patch", Recurrence = CalendarRecurrence.MonthlyDayOfWeek(new DateTime(2026, 11, 1, 2, 0, 0), CalendarWeeksOfMonth.Second, CalendarDaysOfWeek.Tuesday) };
		var patch = secondTuesday.GetOccurrences(new DateTime(2026, 11, 1), new DateTime(2026, 12, 1)).Single();
		var thirdWednesday = secondTuesday.Reschedule(patch, patch.Start.AddDays(8), patch.Start.AddDays(8));
		Assert.That((thirdWednesday.Recurrence.WeeksOfMonth, thirdWednesday.Recurrence.DaysOfWeek), Is.EqualTo((CalendarWeeksOfMonth.Third, CalendarDaysOfWeek.Wednesday)));

		var repeating = new CalendarEvent { Title = "Sync", Recurrence = CalendarRecurrence.Daily(Monday) with { RepeatEvery = TimeSpan.FromMinutes(15), RepeatFor = TimeSpan.FromHours(1) } };
		var run = repeating.GetOccurrences(Monday, Monday.AddDays(1)).Single();
		var shifted = repeating.Reschedule(run, run.Start.AddHours(2), run.End.AddHours(4));
		Assert.That((shifted.Recurrence.Start, shifted.Duration, shifted.Recurrence.RepeatFor), Is.EqualTo((Monday.AddHours(2), TimeSpan.Zero, (TimeSpan?)TimeSpan.FromHours(1))),
			"An occurrence that repeats within itself keeps its own length.");
	}

	[Test]
	public async Task AMemoryCalendarIdentifiesAnnouncesAndGuardsItsEvents() {
		var calendar = new MemoryCalendar("Personal");
		var changes = 0;
		calendar.Changed += () => changes++;
		var added = await calendar.AddAsync(new CalendarEvent { Title = "Dentist", Recurrence = CalendarRecurrence.Once(Monday) });
		Assert.That(added.Id, Is.Not.Null.And.Not.Empty);
		var moved = await calendar.UpdateAsync(added, added with { Recurrence = CalendarRecurrence.Once(Monday.AddDays(1)) });
		Assert.That(moved.Id, Is.EqualTo(added.Id));
		Assert.That((await calendar.ReadEventsAsync()).Single().Recurrence.Start, Is.EqualTo(Monday.AddDays(1)));
		Assert.That(await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => calendar.RemoveAsync(added)), Is.Not.Null, "A stale copy must not remove the changed event.");
		Assert.That(await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => calendar.AddAsync(new CalendarEvent { Title = "Never", Recurrence = CalendarRecurrence.Weekly(Monday, CalendarDaysOfWeek.None) })),
			Is.InstanceOf<ArgumentException>(), "A rule that cannot occur is refused.");
		await calendar.RemoveAsync(moved);
		Assert.That(await calendar.ReadEventsAsync(), Is.Empty);
		Assert.That(changes, Is.EqualTo(3));
	}
}
