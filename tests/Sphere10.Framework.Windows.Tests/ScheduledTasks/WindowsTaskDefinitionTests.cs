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
using Sphere10.Framework.Windows.ScheduledTasks;

namespace Sphere10.Framework.Windows.Tests;

[TestFixture]
public class WindowsTaskDefinitionTests {
	private const string Sample = """
		<?xml version="1.0" encoding="UTF-16"?>
		<Task version="1.4" xmlns="http://schemas.microsoft.com/windows/2004/02/mit/task">
		  <RegistrationInfo>
		    <Author>Fixture</Author>
		    <Description>Every trigger kind</Description>
		    <URI>\Fixture\Everything</URI>
		  </RegistrationInfo>
		  <Triggers>
		    <TimeTrigger>
		      <StartBoundary>2026-11-02T03:30:00</StartBoundary>
		      <Enabled>true</Enabled>
		      <RandomDelay>PT5M</RandomDelay>
		    </TimeTrigger>
		    <CalendarTrigger>
		      <Repetition>
		        <Interval>PT15M</Interval>
		        <Duration>PT1H</Duration>
		        <StopAtDurationEnd>true</StopAtDurationEnd>
		      </Repetition>
		      <StartBoundary>2026-11-02T08:00:00</StartBoundary>
		      <EndBoundary>2027-01-01T00:00:00</EndBoundary>
		      <ExecutionTimeLimit>PT30M</ExecutionTimeLimit>
		      <Enabled>false</Enabled>
		      <ScheduleByDay>
		        <DaysInterval>3</DaysInterval>
		      </ScheduleByDay>
		    </CalendarTrigger>
		    <CalendarTrigger>
		      <StartBoundary>2026-11-02T09:00:00</StartBoundary>
		      <ScheduleByWeek>
		        <WeeksInterval>2</WeeksInterval>
		        <DaysOfWeek><Monday /><Friday /></DaysOfWeek>
		      </ScheduleByWeek>
		    </CalendarTrigger>
		    <CalendarTrigger>
		      <StartBoundary>2026-11-02T10:00:00</StartBoundary>
		      <ScheduleByMonth>
		        <DaysOfMonth><Day>1</Day><Day>15</Day><Day>Last</Day></DaysOfMonth>
		        <Months><January /><July /></Months>
		      </ScheduleByMonth>
		    </CalendarTrigger>
		    <CalendarTrigger>
		      <StartBoundary>2026-11-02T11:00:00</StartBoundary>
		      <ScheduleByMonthDayOfWeek>
		        <Weeks><Week>2</Week><Week>Last</Week></Weeks>
		        <DaysOfWeek><Tuesday /></DaysOfWeek>
		        <Months><January /><February /><March /><April /><May /><June /><July /><August /><September /><October /><November /><December /></Months>
		      </ScheduleByMonthDayOfWeek>
		    </CalendarTrigger>
		    <BootTrigger><Delay>PT2M</Delay></BootTrigger>
		    <LogonTrigger><UserId>FIXTURE\User</UserId></LogonTrigger>
		    <IdleTrigger />
		    <EventTrigger><Subscription>&lt;QueryList&gt;&lt;Query Id="0"&gt;&lt;Select Path="System"&gt;*[System[EventID=7036]]&lt;/Select&gt;&lt;/Query&gt;&lt;/QueryList&gt;</Subscription></EventTrigger>
		    <RegistrationTrigger />
		    <SessionStateChangeTrigger><StateChange>SessionUnlock</StateChange></SessionStateChangeTrigger>
		    <WnfStateChangeTrigger><StateName>7508BCA3380C960C</StateName></WnfStateChangeTrigger>
		  </Triggers>
		  <Principals>
		    <Principal id="Author">
		      <UserId>S-1-5-18</UserId>
		      <RunLevel>HighestAvailable</RunLevel>
		    </Principal>
		  </Principals>
		  <Settings>
		    <MultipleInstancesPolicy>IgnoreNew</MultipleInstancesPolicy>
		    <Enabled>true</Enabled>
		    <Hidden>true</Hidden>
		    <UseUnifiedSchedulingEngine>true</UseUnifiedSchedulingEngine>
		    <MaintenanceSettings><Period>P1D</Period></MaintenanceSettings>
		  </Settings>
		  <Actions Context="Author">
		    <Exec>
		      <Command>%windir%\system32\defrag.exe</Command>
		      <Arguments>-c -h</Arguments>
		    </Exec>
		    <ComHandler><ClassId>{11111111-2222-3333-4444-555555555555}</ClassId></ComHandler>
		  </Actions>
		</Task>
		""";

	[Test]
	public void EveryTriggerKindIsRead() {
		var definition = WindowsTaskDefinition.Parse(Sample);
		Assert.That(definition.Triggers.Select(trigger => trigger.Kind), Is.EqualTo(new[] {
			WindowsTaskTriggerKind.Time, WindowsTaskTriggerKind.Daily, WindowsTaskTriggerKind.Weekly, WindowsTaskTriggerKind.Monthly, WindowsTaskTriggerKind.MonthlyDayOfWeek,
			WindowsTaskTriggerKind.Boot, WindowsTaskTriggerKind.Logon, WindowsTaskTriggerKind.Idle, WindowsTaskTriggerKind.Event, WindowsTaskTriggerKind.Registration,
			WindowsTaskTriggerKind.SessionStateChange, WindowsTaskTriggerKind.Other }));
		var once = definition.Triggers[0];
		Assert.That((once.StartBoundary, once.Delay, once.Enabled), Is.EqualTo(((DateTime?)new DateTime(2026, 11, 2, 3, 30, 0), (TimeSpan?)TimeSpan.FromMinutes(5), true)));
		var daily = definition.Triggers[1];
		Assert.That((daily.Interval, daily.Enabled, daily.EndBoundary, daily.ExecutionTimeLimit), Is.EqualTo((3, false, (DateTime?)new DateTime(2027, 1, 1), (TimeSpan?)TimeSpan.FromMinutes(30))));
		Assert.That((daily.RepetitionInterval, daily.RepetitionDuration, daily.StopAtDurationEnd), Is.EqualTo(((TimeSpan?)TimeSpan.FromMinutes(15), (TimeSpan?)TimeSpan.FromHours(1), true)));
		var weekly = definition.Triggers[2];
		Assert.That((weekly.Interval, weekly.DaysOfWeek), Is.EqualTo((2, CalendarDaysOfWeek.Monday | CalendarDaysOfWeek.Friday)));
		var monthly = definition.Triggers[3];
		Assert.That(monthly.DaysOfMonth, Is.EqualTo(new[] { 1, 15 }));
		Assert.That((monthly.LastDayOfMonth, monthly.Months), Is.EqualTo((true, CalendarMonths.January | CalendarMonths.July)));
		var weekday = definition.Triggers[4];
		Assert.That((weekday.WeeksOfMonth, weekday.DaysOfWeek, weekday.Months), Is.EqualTo((CalendarWeeksOfMonth.Second | CalendarWeeksOfMonth.Last, CalendarDaysOfWeek.Tuesday, CalendarMonths.EveryMonth)));
		Assert.That(definition.Triggers[5].Delay, Is.EqualTo(TimeSpan.FromMinutes(2)));
		Assert.That(definition.Triggers[6].UserId, Is.EqualTo(@"FIXTURE\User"));
		Assert.That(definition.Triggers[8].Subscription, Does.Contain("EventID=7036"));
		Assert.That(definition.Triggers[10].StateChange, Is.EqualTo("SessionUnlock"));
		Assert.That(definition.Triggers[11].OtherName, Is.EqualTo("WnfStateChangeTrigger"));
		Assert.That(definition.Triggers.Count(trigger => trigger.IsClockBased), Is.EqualTo(5));
		Assert.That((definition.Description, definition.Author, definition.Enabled, definition.Hidden, definition.RunAs, definition.RunsElevated), Is.EqualTo(("Every trigger kind", "Fixture", true, true, "S-1-5-18", true)));
		Assert.That(definition.Actions.Select(action => action.Describe()), Is.EqualTo(new[] { @"%windir%\system32\defrag.exe -c -h", "COM Handler {11111111-2222-3333-4444-555555555555}" }));
	}

	[Test]
	public void ClockTriggersAreRecurrencesAndTheOthersAreDescribed() {
		var definition = WindowsTaskDefinition.Parse(Sample);
		Assert.That(definition.Triggers.Take(5).Select(trigger => trigger.ToRecurrence().Describe()), Is.EqualTo(new[] {
			"Once on 2026-11-02 at 03:30",
			"Every 3 days at 08:00, repeating every 15 minutes for 1 hour, until 2027-01-01 00:00",
			"Weekly on Monday, Friday at 09:00, every 2 weeks",
			"Monthly on day 1, day 15, the last day of January, July at 10:00",
			"Monthly on the second, last Tuesday at 11:00" }));
		Assert.That(definition.Triggers.Skip(5).Select(trigger => trigger.ToRecurrence()), Is.All.Null, "A trigger that follows something happening has no place in a calendar.");
		Assert.That(definition.Triggers[1].Describe(), Does.EndWith("(disabled)"));
		Assert.That(definition.Triggers.Skip(5).Select(trigger => trigger.Describe()), Is.EqualTo(new[] {
			"At system startup, after 00:02:00", @"At sign-in of FIXTURE\User", "When the computer is idle",
			"On an event: <QueryList><Query Id=\"0\"><Select Path=\"System\">*[System[EventID=7036]]</Select></Query></QueryList>",
			"When the task is created or changed", "On SessionUnlock of any user", "On WnfStateChangeTrigger" }));
		// The second Tuesday of November 2026 is the 10th and the last is the 24th.
		Assert.That(definition.Triggers[4].ToRecurrence().GetOccurrences(new DateTime(2026, 11, 1), new DateTime(2026, 12, 1)).Select(time => time.Day), Is.EqualTo(new[] { 10, 24 }));
	}

	[Test]
	public void AnUnchangedDefinitionIsWrittenBackWithEverythingItDidNotModel() {
		var definition = WindowsTaskDefinition.Parse(Sample);
		var written = definition.ToXml();
		Assert.That(written, Does.Contain("<UseUnifiedSchedulingEngine>true</UseUnifiedSchedulingEngine>").And.Contain("<MaintenanceSettings><Period>P1D</Period></MaintenanceSettings>"));
		Assert.That(written, Does.Contain("<WnfStateChangeTrigger><StateName>7508BCA3380C960C</StateName></WnfStateChangeTrigger>"), "An unmodelled trigger is kept exactly.");
		Assert.That(written, Does.Contain("<ComHandler><ClassId>{11111111-2222-3333-4444-555555555555}</ClassId></ComHandler>"));
		var again = WindowsTaskDefinition.Parse(written);
		Assert.That(again.Triggers, Is.EqualTo(definition.Triggers));
		Assert.That(again.Actions, Is.EqualTo(definition.Actions));
		Assert.That(again.ToXml(), Is.EqualTo(written), "Reading and writing settle at once.");
	}

	[Test]
	public void ChangingOneTriggerRewritesOnlyThatTrigger() {
		var definition = WindowsTaskDefinition.Parse(Sample);
		var weekly = definition.Triggers[2];
		var moved = weekly.WithRecurrence(CalendarRecurrence.Weekly(new DateTime(2026, 11, 3, 21, 15, 0), CalendarDaysOfWeek.Tuesday | CalendarDaysOfWeek.Saturday, 2));
		definition.Triggers[2] = moved;
		definition.Triggers.RemoveAt(7);
		definition.Triggers.Add(WindowsTaskTrigger.FromRecurrence(CalendarRecurrence.MonthlyDayOfWeek(new DateTime(2026, 11, 1, 2, 0, 0), CalendarWeeksOfMonth.First, CalendarDaysOfWeek.Sunday, CalendarMonths.March)));
		definition.Enabled = false;
		definition.Description = "Changed";
		var again = WindowsTaskDefinition.Parse(definition.ToXml());
		Assert.That(again.Triggers.Select(trigger => trigger.Kind), Has.None.EqualTo(WindowsTaskTriggerKind.Idle));
		Assert.That(again.Triggers, Has.Count.EqualTo(12));
		Assert.That((again.Triggers[2].StartBoundary, again.Triggers[2].DaysOfWeek, again.Triggers[2].Interval), Is.EqualTo(((DateTime?)new DateTime(2026, 11, 3, 21, 15, 0), CalendarDaysOfWeek.Tuesday | CalendarDaysOfWeek.Saturday, 2)));
		var added = again.Triggers[11];
		Assert.That((added.Kind, added.WeeksOfMonth, added.DaysOfWeek, added.Months), Is.EqualTo((WindowsTaskTriggerKind.MonthlyDayOfWeek, CalendarWeeksOfMonth.First, CalendarDaysOfWeek.Sunday, CalendarMonths.March)));
		Assert.That((again.Enabled, again.Description), Is.EqualTo((false, "Changed")));
		Assert.That(again.Triggers[1], Is.EqualTo(definition.Triggers[1]), "The untouched daily trigger keeps its repetition, limit and end.");
		Assert.That(again.ToXml(), Does.Contain("<WnfStateChangeTrigger><StateName>7508BCA3380C960C</StateName></WnfStateChangeTrigger>"));
		Assert.That(() => definition.Triggers.Add(new WindowsTaskTrigger { Kind = WindowsTaskTriggerKind.Other }), Throws.Nothing);
		Assert.That(() => definition.ToXml(), Throws.TypeOf<ArgumentException>(), "An unmodelled trigger cannot be created.");
	}

	[Test]
	public void ANewDefinitionIsValidOnceItHasAnActionAndASoundSchedule() {
		var definition = WindowsTaskDefinition.Create("Nightly copy", "Fixture");
		Assert.That(definition.Validate().ErrorMessages, Has.Some.Contains("at least one action"));
		definition.Actions.Add(new WindowsTaskAction(WindowsTaskActionKind.Execute, @"C:\Windows\System32\robocopy.exe", @"C:\A C:\B", @"C:\"));
		definition.Triggers.Add(WindowsTaskTrigger.FromRecurrence(CalendarRecurrence.Weekly(new DateTime(2026, 11, 2, 1, 0, 0), CalendarDaysOfWeek.None)));
		Assert.That(definition.Validate().ErrorMessages, Has.Some.Contains("weekday"));
		definition.Triggers[0] = WindowsTaskTrigger.FromRecurrence(CalendarRecurrence.Daily(new DateTime(2026, 11, 2, 1, 0, 0)) with { RepeatEvery = TimeSpan.FromHours(6) });
		Assert.That(definition.Validate().IsSuccess, Is.True);
		var read = WindowsTaskDefinition.Parse(definition.ToXml());
		Assert.That((read.Description, read.Author, read.Enabled, read.LogonType, read.RunsElevated), Is.EqualTo(("Nightly copy", "Fixture", true, "InteractiveToken", false)));
		Assert.That(read.Triggers.Single().ToRecurrence().Describe(), Is.EqualTo("Daily at 01:00, repeating every 6 hours"));
		Assert.That(read.Actions.Single(), Is.EqualTo(definition.Actions.Single()));
		Assert.That(read.Copy().ToXml(), Is.EqualTo(read.ToXml()));
	}

	[Test]
	[Category("Integration")]
	public async Task ATaskThisTestOwnsIsRegisteredReadChangedDisabledAndDeleted() {
		var path = @"\Sphere10 Framework Test " + Guid.NewGuid().ToString("N");
		var definition = WindowsTaskDefinition.Create("Created and removed by a Sphere10 Framework test. It never runs.", "Sphere10 Framework Tests");
		definition.Actions.Add(new WindowsTaskAction(WindowsTaskActionKind.Execute, @"%windir%\System32\cmd.exe", "/c exit 0"));
		// A weekly schedule far in the future: the task exists, and nothing ever starts it.
		definition.Triggers.Add(WindowsTaskTrigger.FromRecurrence(CalendarRecurrence.Weekly(new DateTime(2099, 1, 5, 3, 0, 0), CalendarDaysOfWeek.Monday | CalendarDaysOfWeek.Thursday, 2)));
		var created = await Tools.Windows.ScheduledTasks.RegisterTaskAsync(path, definition);
		await using var remove = Tools.Scope.ExecuteOnDisposeAsync(async () => {
			var remaining = await Tools.Windows.ScheduledTasks.ReadTaskAsync(path);
			if (remaining != null)
				await Tools.Windows.ScheduledTasks.DeleteTaskAsync(remaining);
		});
		Assert.That((created.Path, created.Folder, created.Enabled, created.DefinitionError), Is.EqualTo((path, @"\", true, (string)null)));
		Assert.That(created.Definition.Triggers.Single().ToRecurrence().Describe(), Is.EqualTo("Weekly on Monday, Thursday at 03:00, every 2 weeks"));
		Assert.That(created.NextRunTime, Is.EqualTo(new DateTime(2099, 1, 5, 3, 0, 0)), "Task Scheduler computes the same first occurrence.");
		Assert.That(await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => Tools.Windows.ScheduledTasks.RegisterTaskAsync(path, definition)), Is.Not.Null, "Creating does not replace an existing task.");

		var listed = (await Tools.Windows.ScheduledTasks.ReadTasksAsync(@"\", recursive: false)).Single(task => task.Path == path);
		Assert.That(listed.Xml, Is.EqualTo(created.Xml));

		// Move the schedule and register the change against the task as it was read.
		var changed = listed.Definition.Copy();
		changed.Triggers[0] = changed.Triggers[0].WithRecurrence(CalendarRecurrence.Monthly(new DateTime(2099, 1, 1, 22, 30, 0), new[] { 10 }, lastDayOfMonth: true));
		var updated = await Tools.Windows.ScheduledTasks.RegisterTaskAsync(path, changed, listed);
		Assert.That(updated.Definition.Triggers.Single().ToRecurrence().Describe(), Is.EqualTo("Monthly on day 10, the last day at 22:30"));
		Assert.That(updated.NextRunTime, Is.EqualTo(new DateTime(2099, 1, 10, 22, 30, 0)));
		Assert.That(await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => Tools.Windows.ScheduledTasks.SetEnabledAsync(listed, false)), Is.InstanceOf<InvalidOperationException>(),
			"A stale copy of the task changes nothing.");

		var disabled = await Tools.Windows.ScheduledTasks.SetEnabledAsync(updated, false);
		Assert.That((disabled.Enabled, disabled.State, disabled.Definition.Enabled), Is.EqualTo((false, WindowsTaskState.Disabled, false)));
		// The returned task is the changed one, so it is accepted as the basis of the next change.
		var enabled = await Tools.Windows.ScheduledTasks.SetEnabledAsync(disabled, true);
		Assert.That((enabled.Enabled, enabled.State, enabled.Definition.Enabled), Is.EqualTo((true, WindowsTaskState.Ready, true)));
		await Tools.Windows.ScheduledTasks.DeleteTaskAsync(enabled);
		Assert.That(await Tools.Windows.ScheduledTasks.ReadTaskAsync(path), Is.Null);
	}

	[Test]
	[Category("Integration")]
	public async Task TheComputersOwnTasksAreListedWithReadableDefinitions() {
		var tasks = await Tools.Windows.ScheduledTasks.ReadTasksAsync();
		Assert.That(tasks, Is.Not.Empty, "Windows ships with scheduled tasks.");
		Assert.That(tasks.Select(task => task.Path), Is.Unique);
		var readable = tasks.Where(task => task.Definition != null).ToArray();
		Assert.That(readable, Is.Not.Empty);
		foreach (var task in readable) {
			Assert.That(task.Path, Does.StartWith(@"\").And.EndWith(@"\" + task.Name));
			Assert.That(() => task.Definition.Triggers.Select(trigger => trigger.Describe()).ToArray(), Throws.Nothing, task.Path);
			Assert.That(WindowsTaskDefinition.Parse(task.Definition.ToXml()).Triggers, Is.EqualTo(task.Definition.Triggers), task.Path);
		}
		TestContext.WriteLine($"{tasks.Length} tasks; {readable.Length} with a readable definition; {readable.Sum(task => task.Definition.Triggers.Count(trigger => trigger.IsClockBased))} clock triggers; "
			+ $"{readable.SelectMany(task => task.Definition.Triggers).Count(trigger => trigger.Kind == WindowsTaskTriggerKind.Other)} unmodelled triggers.");
	}
}
