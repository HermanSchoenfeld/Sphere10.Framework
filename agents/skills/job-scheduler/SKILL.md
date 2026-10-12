---
name: job-scheduler
description: Recurring and one-shot background jobs with JobBuilder and Scheduler. Trigger when scheduling background work.
---

# Job Scheduler Skill

Use the built-in scheduler (`src/Sphere10.Framework/Scheduler/`) instead of ad-hoc timers.

## Creating jobs
```csharp
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
- Custom jobs: derive `BaseJob` (implements `IJob`: `Execute()`, `Name`, `Status`, `Policy`, `Schedules`).

## Scheduling
- `Scheduler<TJob, TJobSchedule>` manages a timeline heap, auto-reschedules per `ReschedulePolicy`, honors `JobPolicy` (sync/async), and emits `JobStatusChanged` / `StatusChanged`.
- Pass an `ILogger` for diagnostics (see [logging](../logging/SKILL.md)).
