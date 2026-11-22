# Calendar

A recurrence rule, events with occurrences, and a calendar contract that a person's calendar, a job schedule or an operating system's tasks can each implement.

`CalendarRecurrence` says when something happens and how it repeats. Its `Frequency` is `Once`, `Daily` (every `Interval` days), `Weekly` (chosen weekdays of every `Interval` weeks), `Monthly` (chosen days of chosen months, and the last day when `LastDayOfMonth` is set) or `MonthlyDayOfWeek` (chosen weekdays of chosen weeks of chosen months, such as the second Tuesday). Build a rule with the `Once`, `Daily`, `Weekly`, `Monthly` and `MonthlyDayOfWeek` factory methods and adjust it with a `with` expression. Every occurrence takes the time of day of `Start`, no occurrence is earlier than `Start`, and none starts after `End`. `RepeatEvery` makes each occurrence fire again at that interval, for `RepeatFor` or until the next occurrence.

A rule answers questions about time without any calendar or scheduler. `GetOccurrences(from, to)` returns the occurrences that start in a period, `GetNextOccurrence(after)` the first one after a moment, and `OccursOn(date)` whether a date has one. `GetRunTimes(from, to)` and `GetNextRunTime(after)` also count the repetitions inside each occurrence, so they report every moment the rule fires. `Describe()` gives the rule in words and `Validate()` returns a `Result` with every reason the rule cannot produce an occurrence. Times are plain `DateTime` values in one time zone of the caller's choosing, and the rule never converts them. A search for the next occurrence looks twelve years ahead and returns null when it finds none.

```csharp
var rule = CalendarRecurrence.MonthlyDayOfWeek(new DateTime(2026, 1, 1, 17, 0, 0), CalendarWeeksOfMonth.Last, CalendarDaysOfWeek.Friday) with {
	RepeatEvery = TimeSpan.FromMinutes(10),
	RepeatFor = TimeSpan.FromHours(1)
};
Console.WriteLine(rule.Describe());   // Monthly on the last Friday at 17:00, repeating every 10 minutes for 1 hour
var next = rule.GetNextRunTime(DateTime.Now);
```

A `CalendarEvent` is something that happens at set times. It has an `Id`, a `Title`, a `Recurrence`, a `Duration` for each occurrence, an `Enabled` flag, an optional `Color` and a `Tag` that carries the calendar's own record. `GetOccurrences(from, to)` returns a `CalendarOccurrence` for every occurrence that overlaps a period, where an occurrence that repeats within itself spans from its first to its last repetition. `Reschedule(occurrence, newStart, newEnd)` returns the event with one occurrence given new times, as when a user drags it in a calendar view. A recurring event moves as a whole. The rule's start shifts by the same amount, and when the move crosses days a weekly or monthly-by-weekday rule turns its weekdays and a monthly rule replaces the moved day of the month with the new one.

`ICalendar` is a named collection of events with `ReadEventsAsync`, `AddAsync`, `UpdateAsync`, `RemoveAsync`, an `IsReadOnly` flag and a `Changed` event. `UpdateAsync` and `RemoveAsync` take the event as the caller read it and fail when the calendar no longer holds it in that state, so a change made meanwhile is never overwritten. Derive from `CalendarBase` to put a store behind the contract. It checks arguments, validates the event's rule, refuses changes to a read-only calendar and raises `Changed`, and the subclass implements the `*CoreAsync` hooks. `MemoryCalendar` keeps its events in memory and gives an added event an identity when it has none.

```csharp
var calendar = new MemoryCalendar("Team", new CalendarEvent {
	Title = "Stand-up",
	Recurrence = CalendarRecurrence.Weekly(new DateTime(2026, 1, 5, 9, 30, 0), CalendarDaysOfWeek.Weekdays),
	Duration = TimeSpan.FromMinutes(15)
});

var standUp = (await calendar.ReadEventsAsync())[0];
foreach (var occurrence in standUp.GetOccurrences(new DateTime(2026, 1, 5), new DateTime(2026, 1, 12)))
	Console.WriteLine($"{occurrence.Start:ddd HH:mm} to {occurrence.End:HH:mm} {standUp.Title}");

// Move the whole series an hour later
var first = standUp.GetOccurrences(new DateTime(2026, 1, 5), new DateTime(2026, 1, 6)).First();
await calendar.UpdateAsync(standUp, standUp.Reschedule(first, first.Start.AddHours(1), first.End.AddHours(1)));
```

The same rule drives other parts of the Framework. [`OnRecurrenceSchedule<T>`](../Scheduler/README.md) runs a scheduler job whenever a rule fires, [Windows scheduled tasks](../../Sphere10.Framework.Windows/ScheduledTasks/README.md) convert their clock triggers to and from a rule, and the WinForms [VisualCalendar](../../Sphere10.Framework.Windows.Forms/VisualCalendar/README.md) shows `ICalendar` objects and edits a rule.

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`CalendarBase`](CalendarBase.cs) | abstract class | Template `ICalendar` that checks arguments, validates the event's rule, refuses changes to a read-only calendar and raises `Changed`, with `*CoreAsync` hooks for the store. |
| [`CalendarDaysOfWeek`](CalendarDaysOfWeek.cs) | enum | Flag set of weekdays from `Sunday` (1) to `Saturday` (64), with `Weekdays`, `Weekend` and `EveryDay`. |
| [`CalendarEvent`](CalendarEvent.cs) | record | Event with identity, title, description, location, recurrence, duration, enabled flag, color and tag, with `GetOccurrences` and `Reschedule`. |
| [`CalendarFrequency`](CalendarFrequency.cs) | enum | How a rule repeats: `Once`, `Daily`, `Weekly`, `Monthly` or `MonthlyDayOfWeek`. |
| [`CalendarMonths`](CalendarMonths.cs) | enum | Flag set of months from `January` (1) to `December` (2048), with `EveryMonth`. |
| [`CalendarOccurrence`](CalendarOccurrence.cs) | record | One time an event takes place, with the event, the start and end of the occurrence and `IsRecurring`. |
| [`CalendarRecurrence`](CalendarRecurrence.cs) | record | Recurrence rule with factory methods, `Validate`, `OccursOn`, `GetOccurrences`, `GetNextOccurrence`, `GetRunTimes`, `GetNextRunTime`, `GetRepetitionEnd` and `Describe`. |
| [`CalendarWeeksOfMonth`](CalendarWeeksOfMonth.cs) | enum | Flag set of weeks within a month, `First` to `Fourth` counted from its first day and `Last` for its final seven days. |
| [`ICalendar`](ICalendar.cs) | interface | Named collection of `CalendarEvent` records that can be read and, unless read-only, added to, updated and removed from, with a `Changed` event. |
| [`MemoryCalendar`](MemoryCalendar.cs) | class | `CalendarBase` that keeps its events in memory, for a user's own entries, samples and tests. |
