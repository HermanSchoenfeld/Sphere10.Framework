// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Xml.Serialization;
using NUnit.Framework;

namespace Sphere10.Framework.Tests;

/// <summary>A job schedule that follows a calendar rule: its next start is the rule's next run, in UTC, counted from the last start.</summary>
[TestFixture]
[NonParallelizable]
public class RecurrenceScheduleTests {
	private static readonly DateTime LastMonth = DateTime.Today.AddDays(-30);

	[Test]
	public void TheNextStartIsTheRulesNextRunInUniversalTime() {
		var rule = CalendarRecurrence.Weekly(LastMonth.AddHours(3), CalendarDaysOfWeek.Monday | CalendarDaysOfWeek.Thursday, 2);
		var before = DateTime.Now;
		var schedule = new OnRecurrenceSchedule<BaseJob>(rule);
		var expected = new[] { before, DateTime.Now }.Select(moment => rule.GetNextRunTime(moment).Value.ToUniversalTime()).Distinct().ToArray();
		Assert.That(expected, Does.Contain(schedule.NextStartTime));
		Assert.That(schedule.NextStartTime, Is.GreaterThan(DateTime.UtcNow));
		Assert.That(schedule.ReschedulePolicy, Is.EqualTo(ReschedulePolicy.OnStart));
	}

	[Test]
	public void AfterARunTheNextStartIsTheRunThatFollowsIt() {
		var rule = CalendarRecurrence.Monthly(LastMonth.AddHours(22), new[] { 1, 15 }, lastDayOfMonth: true);
		var schedule = new OnRecurrenceSchedule<BaseJob>(rule);
		var first = schedule.NextStartTime;
		schedule.NotifyStart(first);
		schedule.NotifyExecution(first, first.AddSeconds(1));
		var second = schedule.NextStartTime;
		Assert.That(second, Is.EqualTo(rule.GetNextRunTime(first.ToLocalTime()).Value.ToUniversalTime()));
		Assert.That(second, Is.GreaterThan(first));
		Assert.That(schedule.IterationsExecuted, Is.EqualTo(1));
	}

	[Test]
	public void ARepeatingOccurrenceAlreadyUnderWayGivesItsNextRepetition() {
		// Started an hour ago today and repeats every twenty minutes for a day, so the next run is within twenty minutes.
		var rule = CalendarRecurrence.Daily(DateTime.Now.AddDays(-3).AddHours(-1)) with { RepeatEvery = TimeSpan.FromMinutes(20), RepeatFor = TimeSpan.FromHours(23) };
		var schedule = new OnRecurrenceSchedule<BaseJob>(rule);
		Assert.That(schedule.NextStartTime - DateTime.UtcNow, Is.GreaterThan(TimeSpan.Zero).And.LessThanOrEqualTo(TimeSpan.FromMinutes(20)));
	}

	[Test]
	public void TheIterationLimitAndTheRulesEndStopTheSchedule() {
		var limited = new OnRecurrenceSchedule<BaseJob>(CalendarRecurrence.Daily(LastMonth.AddHours(4)), 1);
		Assert.That(limited.IterationsRemaining, Is.EqualTo(1));
		limited.NotifyStart(limited.NextStartTime);
		Assert.That(limited.NextStartTime, Is.EqualTo(DateTime.MaxValue), "It ran its one time.");

		var ended = new OnRecurrenceSchedule<BaseJob>(CalendarRecurrence.Daily(LastMonth.AddHours(4)) with { End = DateTime.Now.AddMinutes(-1) });
		Assert.That(ended.NextStartTime, Is.EqualTo(DateTime.MaxValue), "The rule ended a minute ago.");

		var past = new OnRecurrenceSchedule<BaseJob>(CalendarRecurrence.Once(LastMonth));
		Assert.That(past.NextStartTime, Is.EqualTo(DateTime.MaxValue), "A single time that has passed never runs.");
	}

	[Test]
	public void AnInvalidRuleIsRejected() {
		Assert.That(() => new OnRecurrenceSchedule<BaseJob>(null), Throws.InstanceOf<ArgumentException>());
		Assert.That(() => new OnRecurrenceSchedule<BaseJob>(CalendarRecurrence.Weekly(LastMonth, CalendarDaysOfWeek.None)), Throws.InstanceOf<ArgumentException>().With.Message.Contains("weekday"));
	}

	[Test]
	public void TheBuilderOffersEachKindOfRule() {
		var start = new DateTime(2030, 1, 7, 6, 30, 0);
		var job = JobBuilder.For(() => { })
			.Repeat.OnDays(start, 3)
			.Repeat.OnWeekdays(start, CalendarDaysOfWeek.Weekdays, 2)
			.Repeat.OnDaysOfMonth(start, new[] { 10 }, CalendarMonths.January | CalendarMonths.July, lastDayOfMonth: true)
			.Repeat.OnWeekdaysOfMonth(start, CalendarWeeksOfMonth.Last, CalendarDaysOfWeek.Friday)
			.Repeat.OnRecurrence(CalendarRecurrence.Daily(start) with { RepeatEvery = TimeSpan.FromMinutes(10), RepeatFor = TimeSpan.FromHours(1) })
			.Build();
		Assert.That(job.Schedules.Cast<OnRecurrenceSchedule<BaseJob>>().Select(schedule => schedule.Recurrence.Describe()), Is.EqualTo(new[] {
			"Every 3 days at 06:30",
			"Weekly on weekdays at 06:30, every 2 weeks",
			"Monthly on day 10, the last day of January, July at 06:30",
			"Monthly on the last Friday at 06:30",
			"Daily at 06:30, repeating every 10 minutes for 1 hour"
		}));
		Assert.That(job.Schedules.Select(schedule => schedule.NextStartTime), Is.All.EqualTo(start.ToUniversalTime()).Or.GreaterThan(start.ToUniversalTime()));
	}

	[Test]
	public void AStoredJobRestoresItsRuleAndItsPlaceInIt() {
		var rule = CalendarRecurrence.MonthlyDayOfWeek(LastMonth.AddHours(2), CalendarWeeksOfMonth.First | CalendarWeeksOfMonth.Last, CalendarDaysOfWeek.Monday | CalendarDaysOfWeek.Friday, CalendarMonths.EveryMonth)
			with { RepeatEvery = TimeSpan.FromMinutes(30), RepeatFor = TimeSpan.FromHours(2), End = DateTime.Today.AddYears(2) };
		var job = JobBuilder.For(typeof(SchedulerTestNopJob)).Called("Stored")
			.Repeat.OnRecurrence(rule, 5)
			.Repeat.OnDaysOfMonth(LastMonth.AddHours(1), new[] { 1, 15, 31 }, CalendarMonths.March | CalendarMonths.September)
			.Build();
		var original = job.Schedules.Cast<OnRecurrenceSchedule<BaseJob>>().ToArray();
		original[0].NotifyStart(original[0].NextStartTime);

		var serializer = new XmlSerializer(typeof(JobSerializableSurrogate));
		using var stored = new StringWriter();
		serializer.Serialize(stored, job.ToSerializableSurrogate());
		Assert.That(stored.ToString(), Does.Contain("<Recurrence ").And.Contain("Frequency=\"MonthlyDayOfWeek\"").And.Contain("DaysOfMonth=\"1,15,31\""));
		var restoredJob = new SchedulerJob();
		restoredJob.FromSerializableSurrogate((JobSerializableSurrogate)serializer.Deserialize(new StringReader(stored.ToString())));

		var restored = restoredJob.Schedules.Cast<OnRecurrenceSchedule<SchedulerJob>>().ToArray();
		Assert.That(restored.Select(schedule => schedule.Recurrence.Describe()), Is.EqualTo(original.Select(schedule => schedule.Recurrence.Describe())));
		Assert.That(restored.Select(schedule => (schedule.NextStartTime, schedule.IterationsRemaining)), Is.EqualTo(original.Select(schedule => (TruncateToSeconds(schedule.NextStartTime), schedule.IterationsRemaining))));
		Assert.That(restored[0].IterationsRemaining, Is.EqualTo(4));
	}

	[Test]
	public void AJobRunsWhenItsRuleFires() {
		Assume.That(!Tools.NUnit.IsGitHubAction, "Timing tests are disabled on GitHub Actions");
		using var scheduler = new Scheduler<BaseJob>(SchedulerPolicy.DontThrow);
		using var ran = new ManualResetEventSlim();
		var due = DateTime.Now.AddMilliseconds(400);
		var job = JobBuilder.For(() => ran.Set()).Repeat.OnRecurrence(CalendarRecurrence.Once(due)).Build();
		scheduler.AddJob(job);
		scheduler.Start();
		Assert.That(ran.Wait(TimeSpan.FromSeconds(5)), Is.True, "The job ran at its one time.");
		Assert.That(DateTime.Now, Is.GreaterThanOrEqualTo(due));
		Assert.That(job.Schedules.Single().NextStartTime, Is.EqualTo(DateTime.MaxValue).After(2000, 20), "A single time does not come again.");
	}

	// Stored times keep whole seconds.
	private static DateTime TruncateToSeconds(DateTime time) => time == DateTime.MaxValue ? time : new DateTime(time.Ticks - time.Ticks % TimeSpan.TicksPerSecond, time.Kind);
}
