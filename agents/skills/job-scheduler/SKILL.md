---
name: job-scheduler
description: Recurring and one-shot background jobs with JobBuilder and Scheduler. Trigger when scheduling background work.
---

# Job Scheduler Skill

Read [scheduling guidance](../../../docs/guidelines/patterns/scheduling.md) for job execution, completion and lifetime rules.

Use the built-in scheduler (`src/Sphere10.Framework/Scheduler/`) instead of ad-hoc timers.

## Creating jobs

```csharp
using System;
using Sphere10.Framework;

var job =
	JobBuilder
		.For(() => DoWork())
		.Called("Nightly cleanup")
		.Repeat.OnInterval(TimeSpan.FromHours(24))
		.RunAsyncronously()
		.Build();
```

- `JobBuilder.For(action)` / `For(jobType)` / `JobBuilder<T>.For(jobInstance)`.
- `Called(name)`, `RunOnce(start)`, `RunSyncronously()` / `RunAsyncronously()`, `Repeat` → `ScheduleBuilder<T>`.
- Custom jobs: derive the existing `BaseJob` (implements `IJob`: `Execute()`, `Name`, `Status`, `Policy`, `Schedules`, `Log`, and serialization). Supply `ToSerializableSurrogate()` and `FromSerializableSurrogate(...)` as well as `Execute()`; do not rename the framework's existing base to match a new scaffold name.
- The spellings `RunSyncronously`, `RunAsyncronously`, `JobPolicy.Asyncronous` and `SchedulerPolicy.ForceSyncronous` are the current API. Use them exactly when calling existing code.
- `IJob.Execute()` is synchronous. Asynchronous scheduling runs that synchronous primitive on background work; it does not await a `Task` returned by an action. Never supply an `async` lambda to `JobBuilder.For(Action)` or introduce a fire-and-forget task inside `Execute()`.
- `Repeat.OnInterval(interval)` first schedules for UTC now plus the interval and defaults to `ReschedulePolicy.OnFinish`. Use the start-time overload for an explicit first run. `OnStart` and `OnFinish` choose which timestamp anchors the next interval; they do not change the execution primitive.

## Scheduling
- `Scheduler<TJob, TJobSchedule>` manages a timeline heap, auto-reschedules per `ReschedulePolicy`, honors `JobPolicy` (sync/async), and emits `JobStatusChanged` / `StatusChanged`.
- Pass an `ILogger` for diagnostics (see [logging](../logging/SKILL.md)).
- Own a scheduler with a disposable scope or the application's `Disposables` bag. Register jobs with `AddJob` and start it explicitly; constructing a scheduler does not start it. Avoid process-global `Scheduler.Asynchronous` / `Synchronous` when the lifetime belongs to an application.
- Match job and scheduler policy: a default scheduler accepts `JobPolicy.Asyncronous`; `SchedulerPolicy.ForceSyncronous` requires `RunSyncronously()` jobs. `AddJob` rejects either mismatch. The process-global singleton schedulers are started by static initialization; a newly constructed instance still needs `Start()`.
- `Stop()` stops scheduling and pauses queued jobs; disposal clears scheduler state. Neither joins already running background jobs. Coordinate completion/cancellation of application-owned work before its dependent services are finalized or disposed. Observe `OnJobError` and select error/removal policies deliberately.
- For changed scheduling behavior, verify first-run time, iteration/end limits, reschedule anchoring and error handling through the existing scheduler fixtures. Do not infer task completion from a timer callback or `Stop()` returning.

Current source references: `src/Sphere10.Framework/Scheduler/JobBuilder.cs`, `ScheduleBuilder.cs`, `BaseJob.cs`, `Scheduler.cs`, and `tests/Sphere10.Framework.Tests/Threading/SchedulerTest.cs`.
