# Scheduling

Use the existing framework scheduler rather than ad-hoc timers.

## Job Scheduler

The framework includes a built-in `Scheduler<TJob, TJobSchedule>` for recurring or one-shot background jobs:

- **`IJob`** — interface with `Execute()`, `Name`, `Status`, `Policy`, `Schedules`.
- **`BaseJob`** — abstract base with schedule management and serializable surrogates.
- **`ActionJob`** — wraps a plain `Action` as a job.
- **`JobBuilder<T>`** / **`JobBuilder`** — fluent builder: `JobBuilder.For(action).Called("name").Repeat.OnInterval(TimeSpan).Build()`.
- **`Scheduler<TJob, TJobSchedule>`** — manages a timeline heap, auto-reschedules based on `ReschedulePolicy`, supports sync/async via `JobPolicy`, emits `JobStatusChanged` / `StatusChanged` events.
- The scheduler accepts an `ILogger` for diagnostics. `IJob.Execute()` is synchronous; asynchronous scheduling runs it as background work and does not await an async action. Stop/disposal do not join in-flight jobs. Account for job ownership and shutdown completion in the caller.

`IJob.Execute()` is the synchronous execution primitive. `JobPolicy` selects background execution of that primitive; it does not turn an `Action` into an awaited async operation. Do not supply an async lambda to `JobBuilder.For(Action)` or start unobserved work inside `Execute()`.

Use the existing API spellings: `RunSyncronously`, `RunAsyncronously`, `JobPolicy.Asyncronous` and `SchedulerPolicy.ForceSyncronous`. Register jobs with `AddJob` and start the owned scheduler explicitly. Constructing a new instance does not start it; the process-global `Scheduler.Asynchronous` and `Scheduler.Synchronous` instances are started by static initialization.

`AddJob` requires the job's execution policy to match the scheduler's policy.
A default scheduler accepts `JobPolicy.Asyncronous` jobs; a scheduler with
`ForceSyncronous` requires jobs configured with `RunSyncronously()`. It does
not silently switch a mismatched job to the other execution mode.

`Repeat.OnInterval(interval)` starts at UTC now plus the interval and defaults
to `ReschedulePolicy.OnFinish`. Use its start-time overload for a specific first
run. `OnStart` and `OnFinish` select the timestamp anchoring the next interval.
`DontThrow` suppresses job errors after reporting them through `OnJobError` and
logging; `RemoveJobOnError` selects removal. Observe the callback rather than
assuming a `Completed` status proves success under the suppressing policy.

Own the scheduler through the appropriate [resource scope](resource-scopes.md) or [application lifetime](application-ownership.md). Coordinate running work before its dependent services are finalized or disposed; `Stop()` and disposal do not join that work. See [logging](logging.md) for diagnostics and [testing](testing.md) for verifying changed completion behavior.

Implementation references: [scheduler](../../../src/Sphere10.Framework/Scheduler/Scheduler.cs),
[job contract](../../../src/Sphere10.Framework/Scheduler/IJob.cs),
[custom job base](../../../src/Sphere10.Framework/Scheduler/BaseJob.cs),
[job builder](../../../src/Sphere10.Framework/Scheduler/JobBuilder.cs),
[schedule builder](../../../src/Sphere10.Framework/Scheduler/ScheduleBuilder.cs),
[interval scheduling](../../../src/Sphere10.Framework/Scheduler/OnIntervalSchedule.cs)
and [existing scheduler fixtures](../../../tests/Sphere10.Framework.Tests/Threading/SchedulerTest.cs).
