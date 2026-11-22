# Scheduled Tasks

Reads and changes this computer's Windows scheduled tasks through Task Scheduler, with a task definition that round-trips the task XML and triggers that convert to and from calendar recurrence rules.

`Tools.Windows.ScheduledTasks` lists tasks with `ReadTasks(folder, recursive)`, which reads the root folder `\` and every folder beneath it by default, and reads one task by its full path with `ReadTask`, which returns null when no such task exists. Each task is a `WindowsScheduledTask` record with its path, name, folder, state, enabled flag, last and next run times, last result, missed runs, the task XML exactly as Task Scheduler returned it and the parsed `Definition`. When the definition cannot be read, `Definition` is null and `DefinitionError` says why. A task the caller's account may not read is left out of a listing.

Every change names the task as it was read. `RegisterTask(path, definition, expected)`, `DeleteTask`, `SetEnabled` and `RunTask` take the `WindowsScheduledTask` the caller read and throw `InvalidOperationException` when the task's XML has changed since, so nothing is overwritten unseen. `RegisterTask` without `expected` creates a task and fails when one already exists at that path, and with `expected` it replaces that task. It returns the task as Task Scheduler now holds it, and `SetEnabled` likewise returns the task re-read after the change. `StopTask` asks Task Scheduler to stop every running instance of a task. A change the caller's account may not make fails with Windows' refusal. A task that signs in with a stored password cannot be re-registered, because Windows requires the password again.

The synchronous methods must run on a worker thread, and each has an `Async` counterpart that does so. Each operation connects to Task Scheduler on the calling thread and releases it before returning. The library calls Task Scheduler's automation interface by member name through the internal [`TaskSchedulerComObject`](TaskSchedulerComObjects.cs) wrappers, which derive from `DispatchComObject` in the [Objects](../Objects/README.md) domain, so the same code runs under Native AOT.

`WindowsTaskDefinition` is read from and written to the task XML that Task Scheduler itself uses. `Parse` reads a definition, `Create` starts a new one that runs as the signed-in user without elevation, `Copy` gives an independent copy for editing, `Validate` reports what Task Scheduler would refuse and `ToXml` writes the document. `Triggers`, `Actions`, `Description`, `Author` and `Enabled` can be changed, and `Hidden`, `RunAs`, `LogonType` and `RunsElevated` are read. Everything else in the document, and any trigger or action that was not changed, is written back exactly as it was read, so editing one thing never loses a setting the type does not model. Only an action that starts a program can be created or changed. A trigger or action of the `Other` kind can be kept or removed.

`WindowsTaskTrigger` models every trigger kind Task Scheduler offers. `Time`, `Daily`, `Weekly`, `Monthly` and `MonthlyDayOfWeek` follow the clock, and `Boot`, `Logon`, `Idle`, `Event`, `Registration` and `SessionStateChange` follow something that happens. A clock trigger (`IsClockBased`) converts to a [`CalendarRecurrence`](../../Sphere10.Framework/Calendar/README.md) with `ToRecurrence()`, and `WithRecurrence(rule)` returns the trigger with a rule's schedule while keeping its other settings, such as limits and delays. `FromRecurrence(rule)` creates a new clock trigger. Tasks can therefore be shown in a calendar and rescheduled there. `Describe()` gives a trigger in words. Trigger times are local.

```csharp
// Create a task that runs a program at 02:00 every day
var definition = WindowsTaskDefinition.Create("Clears the cache every night");
definition.Triggers.Add(WindowsTaskTrigger.FromRecurrence(CalendarRecurrence.Daily(DateTime.Today.AddHours(2))));
definition.Actions.Add(new WindowsTaskAction(WindowsTaskActionKind.Execute, @"C:\Tools\ClearCache.exe", "--all"));
var task = await Tools.Windows.ScheduledTasks.RegisterTaskAsync(@"\Clear Cache", definition);

// Move it to 03:00 on weekdays, keeping the trigger's other settings
var changed = task.Definition.Copy();
changed.Triggers[0] = changed.Triggers[0].WithRecurrence(CalendarRecurrence.Weekly(DateTime.Today.AddHours(3), CalendarDaysOfWeek.Weekdays));
task = await Tools.Windows.ScheduledTasks.RegisterTaskAsync(task.Path, changed, task);

// Disable it, then delete it. Each call takes the task as last read.
task = await Tools.Windows.ScheduledTasks.SetEnabledAsync(task, false);
await Tools.Windows.ScheduledTasks.DeleteTaskAsync(task);
```

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`Tools.Windows.ScheduledTasks`](ScheduledTaskUtil.cs) | static class | Lists tasks with their definitions, registers a new or changed definition, and enables, disables, runs, stops and deletes a task, refusing a change to a task that has changed since it was read |
| [`WindowsScheduledTask`](WindowsScheduledTask.cs) | record | One task as Task Scheduler held it when read, with its path, state, run history, XML and parsed definition |
| [`WindowsTaskAction`](WindowsTaskAction.cs) | record | One action of a task with its kind, command, arguments, working directory or COM class identifier, and `Describe()` |
| [`WindowsTaskActionKind`](WindowsTaskActionKind.cs) | enum | What a task does when it runs: `Execute`, `ComHandler` or `Other` |
| [`WindowsTaskDefinition`](WindowsTaskDefinition.cs) | class | Task definition read from and written to task XML, with editable triggers, actions, description and enabled flag, which preserves everything it does not model |
| [`WindowsTaskState`](WindowsTaskState.cs) | enum | State Task Scheduler reports for a task: `Unknown`, `Disabled`, `Queued`, `Ready` or `Running` |
| [`WindowsTaskTrigger`](WindowsTaskTrigger.cs) | record | One trigger of a task with its kind, boundaries, schedule, repetition, limit and delay, and conversion to and from `CalendarRecurrence` |
| [`WindowsTaskTriggerKind`](WindowsTaskTriggerKind.cs) | enum | What starts a task: the five clock kinds, `Boot`, `Logon`, `Idle`, `Event`, `Registration`, `SessionStateChange` or `Other` |
