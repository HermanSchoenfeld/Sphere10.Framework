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
using System.Xml;
using System.Xml.Linq;

namespace Sphere10.Framework.Windows.ScheduledTasks;

/// <summary>
/// The definition of a scheduled task, read from and written to the task XML that Task Scheduler itself uses. Triggers, actions,
/// the description and the enabled flag are modelled; everything else in the document, and any trigger or action that was not
/// changed, is written back exactly as it was read, so editing one thing never loses a setting this type does not know.
/// </summary>
/// <remarks>Change <see cref="Triggers"/> and <see cref="Actions"/> in place, then pass the definition to
/// <c>Tools.Windows.ScheduledTasks.RegisterTaskAsync</c>. Times are local.</remarks>
public sealed class WindowsTaskDefinition {
	private static readonly XNamespace Schema = "http://schemas.microsoft.com/windows/2004/02/mit/task";
	private static readonly string[] _weekdays = { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" };
	private static readonly string[] _months = CultureInfo.InvariantCulture.DateTimeFormat.MonthNames.Take(12).ToArray();
	private readonly XDocument _document;
	private readonly List<(WindowsTaskTrigger Trigger, XElement Element)> _readTriggers = new();
	private readonly List<(WindowsTaskAction Action, XElement Element)> _readActions = new();

	private WindowsTaskDefinition(XDocument document) {
		_document = document;
		foreach (var element in Section("Triggers")?.Elements() ?? Enumerable.Empty<XElement>()) {
			var trigger = ReadTrigger(element);
			_readTriggers.Add((trigger, element));
			Triggers.Add(trigger);
		}
		foreach (var element in Section("Actions")?.Elements() ?? Enumerable.Empty<XElement>()) {
			var action = ReadAction(element);
			_readActions.Add((action, element));
			Actions.Add(action);
		}
	}

	/// <summary>The task's triggers, in order. Replace an entry to change a trigger.</summary>
	public List<WindowsTaskTrigger> Triggers { get; } = new();

	/// <summary>The task's actions, in order.</summary>
	public List<WindowsTaskAction> Actions { get; } = new();

	public string Description {
		get => Section("RegistrationInfo")?.Element(Schema + "Description")?.Value;
		set => SetValue("RegistrationInfo", "Description", value);
	}

	public string Author {
		get => Section("RegistrationInfo")?.Element(Schema + "Author")?.Value;
		set => SetValue("RegistrationInfo", "Author", value);
	}

	/// <summary>Whether the task may run. A missing setting means enabled, as Task Scheduler reads it.</summary>
	public bool Enabled {
		get => !string.Equals(Section("Settings")?.Element(Schema + "Enabled")?.Value, "false", StringComparison.OrdinalIgnoreCase);
		set => SetValue("Settings", "Enabled", value ? "true" : "false");
	}

	/// <summary>Whether Task Scheduler hides the task in its ordinary views.</summary>
	public bool Hidden => string.Equals(Section("Settings")?.Element(Schema + "Hidden")?.Value, "true", StringComparison.OrdinalIgnoreCase);

	/// <summary>The account or group the task runs as, as the definition names it, or null.</summary>
	public string RunAs {
		get {
			var principal = Section("Principals")?.Elements(Schema + "Principal").FirstOrDefault();
			return principal?.Element(Schema + "UserId")?.Value ?? principal?.Element(Schema + "GroupId")?.Value;
		}
	}

	/// <summary>How the task signs in: InteractiveToken, Password, S4U, Group, ServiceAccount or InteractiveTokenOrPassword. Null when the definition leaves it to Windows.</summary>
	public string LogonType {
		get {
			var principal = Section("Principals")?.Elements(Schema + "Principal").FirstOrDefault();
			return principal?.Element(Schema + "LogonType")?.Value ?? (principal?.Element(Schema + "GroupId") != null ? "Group" : null);
		}
	}

	/// <summary>Whether the task runs with the highest privileges its account has.</summary>
	public bool RunsElevated => string.Equals(Section("Principals")?.Elements(Schema + "Principal").FirstOrDefault()?.Element(Schema + "RunLevel")?.Value, "HighestAvailable", StringComparison.OrdinalIgnoreCase);

	/// <summary>Reads a definition from task XML.</summary>
	public static WindowsTaskDefinition Parse(string xml) {
		Guard.ArgumentNotNullOrWhitespace(xml, nameof(xml));
		var document = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
		Guard.Argument(document.Root?.Name == Schema + "Task", nameof(xml), "The text is not a Task Scheduler task definition.");
		return new WindowsTaskDefinition(document);
	}

	/// <summary>Creates the definition of a new task that runs as the signed-in user without elevation, only while that user is signed in.</summary>
	public static WindowsTaskDefinition Create(string description = null, string author = null) {
		var definition = new WindowsTaskDefinition(new XDocument(new XElement(Schema + "Task", new XAttribute("version", "1.2"),
			new XElement(Schema + "RegistrationInfo"),
			new XElement(Schema + "Triggers"),
			new XElement(Schema + "Principals", new XElement(Schema + "Principal", new XAttribute("id", "Author"),
				new XElement(Schema + "LogonType", "InteractiveToken"), new XElement(Schema + "RunLevel", "LeastPrivilege"))),
			new XElement(Schema + "Settings",
				new XElement(Schema + "MultipleInstancesPolicy", "IgnoreNew"), new XElement(Schema + "DisallowStartIfOnBatteries", "false"),
				new XElement(Schema + "StopIfGoingOnBatteries", "false"), new XElement(Schema + "AllowHardTerminate", "true"),
				new XElement(Schema + "StartWhenAvailable", "false"), new XElement(Schema + "AllowStartOnDemand", "true"),
				new XElement(Schema + "Enabled", "true"), new XElement(Schema + "Hidden", "false"), new XElement(Schema + "ExecutionTimeLimit", "PT72H")),
			new XElement(Schema + "Actions", new XAttribute("Context", "Author")))));
		if (!string.IsNullOrEmpty(description))
			definition.Description = description;
		if (!string.IsNullOrEmpty(author))
			definition.Author = author;
		return definition;
	}

	/// <summary>An independent copy, for editing without changing the definition that was read.</summary>
	public WindowsTaskDefinition Copy() => Parse(ToXml());

	/// <summary>Reports what Task Scheduler would refuse: no action, or a trigger whose schedule cannot occur.</summary>
	public Result Validate() {
		var result = Result.Default;
		if (Actions.Count == 0)
			result.AddError("A task needs at least one action.");
		foreach (var action in Actions.Where(action => action.Kind == WindowsTaskActionKind.Execute && string.IsNullOrWhiteSpace(action.Command)))
			result.AddError("An action needs a program to start.");
		foreach (var trigger in Triggers.Where(trigger => trigger.IsClockBased)) {
			if (!trigger.StartBoundary.HasValue)
				result.AddError("A scheduled trigger needs a start date and time.");
			else
				foreach (var error in trigger.ToRecurrence().Validate().ErrorMessages)
					result.AddError(error);
		}
		return result;
	}

	/// <summary>The definition as task XML. Unchanged triggers and actions, and every unmodelled setting, are exactly as they were read.</summary>
	public string ToXml() {
		Rewrite("Triggers", Triggers, _readTriggers, WriteTrigger);
		Rewrite("Actions", Actions, _readActions, WriteAction);
		return _document.ToString(SaveOptions.DisableFormatting);
	}

	private XElement Section(string name) => _document.Root.Element(Schema + name);

	// The schema fixes the order of a task's sections, so a missing one is created in its place.
	private XElement EnsureSection(string name) {
		var section = Section(name);
		if (section != null)
			return section;
		var order = new[] { "RegistrationInfo", "Triggers", "Principals", "Settings", "Data", "Actions" };
		section = new XElement(Schema + name);
		var following = order.Skip(Array.IndexOf(order, name) + 1).Select(Section).FirstOrDefault(element => element != null);
		if (following != null)
			following.AddBeforeSelf(section);
		else
			_document.Root.Add(section);
		return section;
	}

	private void SetValue(string section, string name, string value) {
		var parent = EnsureSection(section);
		var element = parent.Element(Schema + name);
		if (value == null)
			element?.Remove();
		else if (element != null)
			element.Value = value;
		else
			parent.Add(new XElement(Schema + name, value));
	}

	private void Rewrite<T>(string section, List<T> current, List<(T Item, XElement Element)> read, Func<T, XElement> write) {
		var parent = EnsureSection(section);
		var remaining = read.ToList();
		var elements = new List<XElement>();
		foreach (var item in current) {
			// An item that still equals what was read keeps its original element, with anything this type does not model.
			var index = remaining.FindIndex(entry => Equals(entry.Item, item));
			if (index >= 0) {
				elements.Add(remaining[index].Element);
				remaining.RemoveAt(index);
			} else
				elements.Add(write(item));
		}
		parent.RemoveNodes();
		parent.Add(elements);
		read.Clear();
		read.AddRange(current.Zip(elements, (item, element) => (item, element)));
	}

	#region Triggers

	private static WindowsTaskTrigger ReadTrigger(XElement element) {
		string Text(string name) => element.Element(Schema + name)?.Value;
		var repetition = element.Element(Schema + "Repetition");
		var trigger = new WindowsTaskTrigger {
			Enabled = !string.Equals(Text("Enabled"), "false", StringComparison.OrdinalIgnoreCase),
			StartBoundary = ReadTime(Text("StartBoundary")),
			EndBoundary = ReadTime(Text("EndBoundary")),
			ExecutionTimeLimit = ReadDuration(Text("ExecutionTimeLimit")),
			RepetitionInterval = ReadDuration(repetition?.Element(Schema + "Interval")?.Value),
			RepetitionDuration = ReadDuration(repetition?.Element(Schema + "Duration")?.Value),
			StopAtDurationEnd = string.Equals(repetition?.Element(Schema + "StopAtDurationEnd")?.Value, "true", StringComparison.OrdinalIgnoreCase),
			Delay = ReadDuration(Text("Delay") ?? Text("RandomDelay")),
			UserId = Text("UserId"),
			Subscription = Text("Subscription"),
			StateChange = Text("StateChange")
		};
		switch (element.Name.LocalName) {
			case "TimeTrigger":
				return trigger with { Kind = WindowsTaskTriggerKind.Time };
			case "BootTrigger":
				return trigger with { Kind = WindowsTaskTriggerKind.Boot };
			case "LogonTrigger":
				return trigger with { Kind = WindowsTaskTriggerKind.Logon };
			case "IdleTrigger":
				return trigger with { Kind = WindowsTaskTriggerKind.Idle };
			case "EventTrigger":
				return trigger with { Kind = WindowsTaskTriggerKind.Event };
			case "RegistrationTrigger":
				return trigger with { Kind = WindowsTaskTriggerKind.Registration };
			case "SessionStateChangeTrigger":
				return trigger with { Kind = WindowsTaskTriggerKind.SessionStateChange };
			case "CalendarTrigger":
				break;
			default:
				return trigger with { Kind = WindowsTaskTriggerKind.Other, OtherName = element.Name.LocalName };
		}
		if (element.Element(Schema + "ScheduleByDay") is { } byDay)
			return trigger with { Kind = WindowsTaskTriggerKind.Daily, Interval = ReadInterval(byDay, "DaysInterval") };
		if (element.Element(Schema + "ScheduleByWeek") is { } byWeek)
			return trigger with { Kind = WindowsTaskTriggerKind.Weekly, Interval = ReadInterval(byWeek, "WeeksInterval"), DaysOfWeek = ReadDaysOfWeek(byWeek) };
		if (element.Element(Schema + "ScheduleByMonth") is { } byMonth) {
			var days = byMonth.Element(Schema + "DaysOfMonth")?.Elements(Schema + "Day").Select(day => day.Value.Trim()).ToArray() ?? Array.Empty<string>();
			return trigger with {
				Kind = WindowsTaskTriggerKind.Monthly,
				DaysOfMonth = days.Where(day => int.TryParse(day, NumberStyles.None, CultureInfo.InvariantCulture, out _)).Select(day => int.Parse(day, CultureInfo.InvariantCulture)).ToArray(),
				LastDayOfMonth = days.Contains("Last", StringComparer.OrdinalIgnoreCase),
				Months = ReadMonths(byMonth)
			};
		}
		if (element.Element(Schema + "ScheduleByMonthDayOfWeek") is { } byWeekday) {
			var weeks = CalendarWeeksOfMonth.None;
			foreach (var week in byWeekday.Element(Schema + "Weeks")?.Elements(Schema + "Week").Select(week => week.Value.Trim()) ?? Enumerable.Empty<string>())
				weeks |= week switch { "1" => CalendarWeeksOfMonth.First, "2" => CalendarWeeksOfMonth.Second, "3" => CalendarWeeksOfMonth.Third, "4" => CalendarWeeksOfMonth.Fourth, _ => CalendarWeeksOfMonth.Last };
			return trigger with { Kind = WindowsTaskTriggerKind.MonthlyDayOfWeek, WeeksOfMonth = weeks, DaysOfWeek = ReadDaysOfWeek(byWeekday), Months = ReadMonths(byWeekday) };
		}
		// A calendar trigger without a schedule fires once, as a time trigger does.
		return trigger with { Kind = WindowsTaskTriggerKind.Time };
	}

	private static XElement WriteTrigger(WindowsTaskTrigger trigger) {
		Guard.Argument(trigger.Kind != WindowsTaskTriggerKind.Other, nameof(trigger), "A trigger of an unmodelled kind can be kept or removed, not created or changed.");
		var element = new XElement(Schema + (trigger.Kind switch {
			WindowsTaskTriggerKind.Time => "TimeTrigger",
			WindowsTaskTriggerKind.Boot => "BootTrigger",
			WindowsTaskTriggerKind.Logon => "LogonTrigger",
			WindowsTaskTriggerKind.Idle => "IdleTrigger",
			WindowsTaskTriggerKind.Event => "EventTrigger",
			WindowsTaskTriggerKind.Registration => "RegistrationTrigger",
			WindowsTaskTriggerKind.SessionStateChange => "SessionStateChangeTrigger",
			_ => "CalendarTrigger"
		}));
		// The schema fixes this order: repetition, boundaries, limit, enabled, then the kind's own elements.
		if (trigger.RepetitionInterval.HasValue) {
			var repetition = new XElement(Schema + "Repetition", new XElement(Schema + "Interval", XmlConvert.ToString(trigger.RepetitionInterval.Value)));
			if (trigger.RepetitionDuration.HasValue)
				repetition.Add(new XElement(Schema + "Duration", XmlConvert.ToString(trigger.RepetitionDuration.Value)));
			repetition.Add(new XElement(Schema + "StopAtDurationEnd", trigger.StopAtDurationEnd ? "true" : "false"));
			element.Add(repetition);
		}
		if (trigger.StartBoundary.HasValue)
			element.Add(new XElement(Schema + "StartBoundary", WriteTime(trigger.StartBoundary.Value)));
		if (trigger.EndBoundary.HasValue)
			element.Add(new XElement(Schema + "EndBoundary", WriteTime(trigger.EndBoundary.Value)));
		if (trigger.ExecutionTimeLimit.HasValue)
			element.Add(new XElement(Schema + "ExecutionTimeLimit", XmlConvert.ToString(trigger.ExecutionTimeLimit.Value)));
		element.Add(new XElement(Schema + "Enabled", trigger.Enabled ? "true" : "false"));
		var delay = trigger.Delay.HasValue && trigger.Delay.Value > TimeSpan.Zero ? XmlConvert.ToString(trigger.Delay.Value) : null;
		switch (trigger.Kind) {
			case WindowsTaskTriggerKind.Time:
				AddOptional(element, "RandomDelay", delay);
				break;
			case WindowsTaskTriggerKind.Daily:
				AddOptional(element, "RandomDelay", delay);
				element.Add(new XElement(Schema + "ScheduleByDay", new XElement(Schema + "DaysInterval", Math.Max(1, trigger.Interval))));
				break;
			case WindowsTaskTriggerKind.Weekly:
				AddOptional(element, "RandomDelay", delay);
				element.Add(new XElement(Schema + "ScheduleByWeek", new XElement(Schema + "WeeksInterval", Math.Max(1, trigger.Interval)), WriteDaysOfWeek(trigger.DaysOfWeek)));
				break;
			case WindowsTaskTriggerKind.Monthly:
				AddOptional(element, "RandomDelay", delay);
				var days = (trigger.DaysOfMonth ?? Array.Empty<int>()).OrderBy(day => day).Select(day => new XElement(Schema + "Day", day)).ToList();
				if (trigger.LastDayOfMonth)
					days.Add(new XElement(Schema + "Day", "Last"));
				element.Add(new XElement(Schema + "ScheduleByMonth", new XElement(Schema + "DaysOfMonth", days), WriteMonths(trigger.Months)));
				break;
			case WindowsTaskTriggerKind.MonthlyDayOfWeek:
				AddOptional(element, "RandomDelay", delay);
				var weeks = new[] { (CalendarWeeksOfMonth.First, "1"), (CalendarWeeksOfMonth.Second, "2"), (CalendarWeeksOfMonth.Third, "3"), (CalendarWeeksOfMonth.Fourth, "4"), (CalendarWeeksOfMonth.Last, "Last") }
					.Where(week => trigger.WeeksOfMonth.HasFlag(week.Item1)).Select(week => new XElement(Schema + "Week", week.Item2));
				element.Add(new XElement(Schema + "ScheduleByMonthDayOfWeek", new XElement(Schema + "Weeks", weeks), WriteDaysOfWeek(trigger.DaysOfWeek), WriteMonths(trigger.Months)));
				break;
			case WindowsTaskTriggerKind.Logon:
				AddOptional(element, "UserId", trigger.UserId);
				AddOptional(element, "Delay", delay);
				break;
			case WindowsTaskTriggerKind.SessionStateChange:
				AddOptional(element, "UserId", trigger.UserId);
				AddOptional(element, "Delay", delay);
				AddOptional(element, "StateChange", trigger.StateChange);
				break;
			case WindowsTaskTriggerKind.Event:
				AddOptional(element, "Subscription", trigger.Subscription);
				AddOptional(element, "Delay", delay);
				break;
			case WindowsTaskTriggerKind.Boot:
			case WindowsTaskTriggerKind.Registration:
				AddOptional(element, "Delay", delay);
				break;
		}
		return element;
	}

	private static void AddOptional(XElement parent, string name, string value) {
		if (!string.IsNullOrEmpty(value))
			parent.Add(new XElement(Schema + name, value));
	}

	private static int ReadInterval(XElement schedule, string name)
		=> int.TryParse(schedule.Element(Schema + name)?.Value, NumberStyles.None, CultureInfo.InvariantCulture, out var interval) && interval > 0 ? interval : 1;

	private static CalendarDaysOfWeek ReadDaysOfWeek(XElement schedule) {
		var days = CalendarDaysOfWeek.None;
		foreach (var day in schedule.Element(Schema + "DaysOfWeek")?.Elements() ?? Enumerable.Empty<XElement>()) {
			var index = Array.FindIndex(_weekdays, name => name.Equals(day.Name.LocalName, StringComparison.OrdinalIgnoreCase));
			if (index >= 0)
				days |= (CalendarDaysOfWeek)(1 << index);
		}
		return days;
	}

	private static XElement WriteDaysOfWeek(CalendarDaysOfWeek days)
		=> new(Schema + "DaysOfWeek", Enumerable.Range(0, 7).Where(index => days.HasFlag((CalendarDaysOfWeek)(1 << index))).Select(index => new XElement(Schema + _weekdays[index])));

	// A schedule without a month list applies to every month.
	private static CalendarMonths ReadMonths(XElement schedule) {
		var list = schedule.Element(Schema + "Months");
		if (list == null)
			return CalendarMonths.EveryMonth;
		var months = CalendarMonths.None;
		foreach (var month in list.Elements()) {
			var index = Array.FindIndex(_months, name => name.Equals(month.Name.LocalName, StringComparison.OrdinalIgnoreCase));
			if (index >= 0)
				months |= (CalendarMonths)(1 << index);
		}
		return months;
	}

	private static XElement WriteMonths(CalendarMonths months)
		=> new(Schema + "Months", Enumerable.Range(0, 12).Where(index => months.HasFlag((CalendarMonths)(1 << index))).Select(index => new XElement(Schema + _months[index])));

	// Boundaries are written in local time; one read with an offset or as UTC is brought to local time.
	private static DateTime? ReadTime(string text) {
		if (string.IsNullOrWhiteSpace(text) || !DateTime.TryParse(text, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var time))
			return null;
		return time.Kind == DateTimeKind.Utc ? time.ToLocalTime() : DateTime.SpecifyKind(time, DateTimeKind.Unspecified);
	}

	private static string WriteTime(DateTime time) => time.ToString("yyyy-MM-dd'T'HH:mm:ss", CultureInfo.InvariantCulture);

	private static TimeSpan? ReadDuration(string text) {
		if (string.IsNullOrWhiteSpace(text))
			return null;
		TimeSpan? duration = null;
		Tools.Exceptions.ExecuteIgnoringException(() => duration = XmlConvert.ToTimeSpan(text.Trim()));
		return duration;
	}

	#endregion

	#region Actions

	private static WindowsTaskAction ReadAction(XElement element) => element.Name.LocalName switch {
		"Exec" => new WindowsTaskAction(WindowsTaskActionKind.Execute, element.Element(Schema + "Command")?.Value, element.Element(Schema + "Arguments")?.Value,
			element.Element(Schema + "WorkingDirectory")?.Value),
		"ComHandler" => new WindowsTaskAction(WindowsTaskActionKind.ComHandler, ClassId: element.Element(Schema + "ClassId")?.Value),
		_ => new WindowsTaskAction(WindowsTaskActionKind.Other, element.Name.LocalName)
	};

	private static XElement WriteAction(WindowsTaskAction action) {
		Guard.Argument(action.Kind == WindowsTaskActionKind.Execute, nameof(action), "Only an action that starts a program can be created or changed.");
		var element = new XElement(Schema + "Exec", new XElement(Schema + "Command", action.Command));
		AddOptional(element, "Arguments", action.Arguments);
		AddOptional(element, "WorkingDirectory", action.WorkingDirectory);
		return element;
	}

	#endregion
}
