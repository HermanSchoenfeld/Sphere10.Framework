# Scheduler

Timer-driven scheduling of one-shot and recurring jobs, with fluent job builders and XML-serializable job surrogates.

A job (`IJob`, normally a `BaseJob` subclass) owns one or more schedules (`IJobSchedule`). `Scheduler<TJob, TJobSchedule>` keeps every schedule in a heap ordered by `NextStartTime`, sleeps on a timer until the earliest one is due, and runs each due job. A job with the `JobPolicy.Asyncronous` flag runs on a thread-pool thread, and the other jobs run inline in the timer callback. A scheduler only accepts jobs whose policy matches its own, so an asynchronous job cannot be added to a scheduler created with `SchedulerPolicy.ForceSyncronous`, and the reverse also throws. A schedule is re-queued when the run starts or when it finishes, as its `ReschedulePolicy` says, until its iterations or end date run out.

Build jobs with `JobBuilder.For(Action)` or `JobBuilder.For(Type)`, give them schedules through `Repeat`, and add them to a scheduler. `Scheduler.Asynchronous` and `Scheduler.Synchronous` are started shared instances that swallow job errors and remove failing jobs. A scheduler you create starts in the `Off` state, so call `Start()`, and dispose it when finished. `JobStatusChanged`, `StatusChanged` and the `OnBeforeJobBegin`, `OnAfterJobEnd` and `OnJobError` callbacks report progress. The surrogates in `Serializable` let a scheduler save its jobs with `ToSerializableSurrogate()` and restore them as `SchedulerJob` instances with `FromSerializableSurrogate()`. Restoring only works for jobs whose body is an `ISchedulerJob` type, because an `ActionJob` delegate cannot be persisted.

```csharp
var job = JobBuilder
	.For(() => Console.WriteLine("tick"))
	.Called("Tick")
	.Repeat.OnInterval(TimeSpan.FromMinutes(5))
	.Build();
Scheduler.Asynchronous.AddJob(job);
```

## Contents

| Type | Kind | Purpose |
|---|---|---|
| [`ActionJob`](ActionJob.cs) | class | `BaseJob` that runs an `Action` delegate, as created by `JobBuilder.For(Action)`. |
| [`BaseJob`](BaseJob.cs) | abstract class | Base `IJob` with name, status, policy (asynchronous by default), schedule list and logger, whose subclasses implement `Execute` and surrogate conversion. |
| [`BaseJobSchedule`](BaseJobSchedule.cs) | abstract class | Base `IJobSchedule` that tracks start and end times, iteration counts and end date, and derives `NextStartTime` from `CalculateNextRunTime()`. |
| [`BaseJobSchedule<T>`](BaseJobSchedule.cs) | abstract class | `BaseJobSchedule` whose `Job` property returns the owning job as `T`. |
| [`IJob`](IJob.cs) | interface | Job contract with name, policy, status, schedules, logger, `Execute()` and a serializable surrogate. |
| [`IJobSchedule`](IJobSchedule.cs) | interface | Schedule contract ordered by `NextStartTime`, with iteration counters, reschedule policy, end date and start and execution notifications. |
| [`ISchedulerJob`](ISchedulerJob.cs) | interface | Job body with a parameterless constructor that `SchedulerJob` instantiates and runs through `Execute(IJob)`. |
| [`JobBuilder`](JobBuilder.cs) | class | Builder entry point whose `For(Action)` starts an `ActionJob` and whose `For(Type)` starts a `SchedulerJob`. |
| [`JobBuilder<T>`](JobBuilder.cs) | class | Fluent job builder with `Called`, `RunOnce`, `RunSyncronously`, `RunAsyncronously`, `Repeat` and `Build`. |
| [`JobPolicy`](JobPolicy.cs) | enum | Job flags, where `Asyncronous` runs the job on a thread-pool thread. |
| [`JobStatus`](JobStatus.cs) | enum | Job lifecycle states, from `Uninitalized` through `Scheduled`, `Paused`, `Started`, `Completed` and `Error`. |
| [`OnDayOfMonthScheduler<T>`](OnDayOfMonthScheduler.cs) | class | Schedule that runs on a day of the month at a time of day, once or repeatedly. |
| [`OnDayOfWeekScheduler<T>`](OnDayOfWeekScheduler.cs) | class | Schedule that runs on a day of the week at a time of day, once or repeatedly. |
| [`OnIntervalSchedule<T>`](OnIntervalSchedule.cs) | class | Schedule that starts at a given time and repeats after an interval measured from the last start or finish. |
| [`ReschedulePolicy`](ReschedulePolicy.cs) | enum | Whether the next run is computed from the start (`OnStart`) or the end (`OnFinish`) of the previous run. |
| [`ScheduleBuilder`](ScheduleBuilder.cs) | class | `ScheduleBuilder<BaseJob>` used by the builders that `JobBuilder.For` returns. |
| [`ScheduleBuilder<T>`](ScheduleBuilder.cs) | class | Adds an interval, day-of-week or day-of-month schedule to the job of a `JobBuilder<T>` and returns that builder. |
| [`Scheduler`](Scheduler.cs) | class | Scheduler of `IJob` jobs with the started shared `Asynchronous` and `Synchronous` instances. |
| [`Scheduler<TJob>`](Scheduler.cs) | class | Scheduler of `TJob` jobs over `IJobSchedule` schedules. |
| [`Scheduler<TJob, TJobSchedule>`](Scheduler.cs) | class | Disposable timer-driven scheduler that orders schedules by next start time, runs due jobs, and raises job and scheduler status events. |
| [`SchedulerJob`](SchedulerJob.cs) | class | `BaseJob` that creates a new instance of an `ISchedulerJob` type for every run and can be restored from a surrogate. |
| [`SchedulerPolicy`](SchedulerPolicy.cs) | enum | Scheduler flags `ForceSyncronous`, `DontThrow`, `RemoveJobOnError` and `DisposeWhenFinished`. |
| [`SchedulerStatus`](SchedulerStatus.cs) | enum | Scheduler states `Off`, `On` (running jobs) and `Idle` (waiting for the next job). |

### Serializable

| Type | Kind | Purpose |
|---|---|---|
| [`DayOfMonthScheduleSerializableSurrogate`](Serializable/DayOfMonthScheduleSerializableSurrogate.cs) | class | XML surrogate of an `OnDayOfMonthScheduler<T>` with its day of month and time of day. |
| [`DayOfWeekScheduleSerializableSurrogate`](Serializable/DayOfWeekScheduleSerializableSurrogate.cs) | class | XML surrogate of an `OnDayOfWeekScheduler<T>` with its day of week and time of day. |
| [`IntervalScheduleSerializableSurrogate`](Serializable/IntervalScheduleSerializableSurrogate.cs) | class | XML surrogate of an `OnIntervalSchedule<T>` with its repeat interval in milliseconds. |
| [`ISchedulerSerializer`](Serializable/ISchedulerSerializer.cs) | interface | Contract for storing and loading a `SchedulerSerializableSurrogate`. |
| [`JobScheduleSerializableSurrogate`](Serializable/JobScheduleSerializableSurrogate.cs) | abstract class | XML attributes shared by all schedule surrogates, such as last start and end times, end date and iteration counts. |
| [`JobSerializableSurrogate`](Serializable/JobSerializableSurrogate.cs) | class | XML surrogate of a job with its type name, name, status, policy and schedules. |
| [`SchedulerSerializableSurrogate`](Serializable/SchedulerSerializableSurrogate.cs) | class | XML root (`Scheduler`) that holds the surrogates of all scheduled jobs. |
