// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework.Application;

/// <summary>
/// Asks the application's <see cref="ProductUpdaterBase"/> for a newer version in the background and raises
/// <see cref="UpdateAvailable"/> when one is found. <see cref="Start"/> schedules the first check after
/// <see cref="ProductUpdateCheckOptions.InitialDelay"/> and one more every <see cref="ProductUpdateCheckOptions.Interval"/> after the
/// previous check ends. <see cref="StopAsync"/> ends the schedule, cancels the check in flight and completes when it has finished.
/// </summary>
/// <remarks>
/// The checker owns one repeating job on its own scheduler and never runs two checks at once. A failed check is logged and the
/// schedule continues. <see cref="UpdateAvailable"/> is raised on a worker thread, once for each version in the checker's lifetime,
/// and a handler returns promptly. It hands presentation to its UI thread and never waits for <see cref="StopAsync"/>. An observer
/// that could not present an update calls <see cref="OfferAgain"/>, and the next check then announces that version again.
/// </remarks>
public class ProductUpdateChecker : AsyncDisposable {

	public event EventHandlerEx<ProductUpdate> UpdateAvailable;

	private readonly SynchronizedObject _state = new();
	private readonly ProductUpdaterBase _updater;
	private readonly CancellationTokenSource _stopping = new();
	private Scheduler<BaseJob> _scheduler;
	private BaseJob _job;
	private Task<ProductUpdateCheckResult> _check;
	private TaskCompletionSource _stopped;
	private ApplicationVersion _notifiedVersion;

	public ProductUpdateChecker(ProductUpdaterBase updater, ProductUpdateCheckOptions options = null, ILogger logger = null) {
		Guard.ArgumentNotNull(updater, nameof(updater));
		options ??= new ProductUpdateCheckOptions();
		Guard.Argument(options.InitialDelay >= TimeSpan.Zero, nameof(options), "The initial delay cannot be negative.");
		Guard.Argument(options.Interval > TimeSpan.Zero, nameof(options), "The interval must be positive.");
		_updater = updater;
		Options = options;
		Logger = logger ?? new NoOpLogger();
	}

	public ProductUpdateCheckOptions Options { get; }

	/// <summary>Whether the schedule has been started and not yet stopped.</summary>
	public bool IsRunning {
		get {
			using (_state.EnterReadScope())
				return _scheduler != null;
		}
	}

	/// <summary>The update the most recent completed check found, or null when it found none or no check has completed.</summary>
	public ProductUpdate AvailableUpdate { get; private set; }

	protected ILogger Logger { get; }

	/// <summary>Schedules the checks and returns at once. A second call does nothing.</summary>
	public void Start() {
		using (_state.EnterWriteScope()) {
			ObjectDisposedException.ThrowIf(_stopped != null, this);
			if (_scheduler != null)
				return;
			var scheduler = CreateScheduler();
			var builder = JobBuilder
				.For(RunScheduledCheck)
				.Called("Product update check")
				.Repeat.OnInterval(DateTime.UtcNow.Add(Options.InitialDelay), Options.Interval);
			if (scheduler.Policy.HasFlag(SchedulerPolicy.ForceSyncronous))
				builder.RunSyncronously();
			_job = builder.Build();
			_scheduler = scheduler;
			scheduler.AddJob(_job);
			scheduler.Start();
		}
	}

	/// <summary>
	/// Checks now and completes with the updater's answer, raising <see cref="UpdateAvailable"/> as a scheduled check does. A request
	/// made while a check is running joins that check. The token ends only this caller's wait.
	/// </summary>
	public Task<ProductUpdateCheckResult> CheckAsync(CancellationToken cancellationToken = default) {
		Task<ProductUpdateCheckResult> check;
		using (_state.EnterWriteScope()) {
			ObjectDisposedException.ThrowIf(_stopped != null, this);
			// The check runs on a worker, outside this scope and off the caller's thread.
			if (_check == null || _check.IsCompleted)
				_check = Task.Run(RunCheckAsync);
			check = _check;
		}
		return check.WaitAsync(cancellationToken);
	}

	/// <summary>
	/// Withdraws the announcement of an update, so the next check that finds its version raises <see cref="UpdateAvailable"/> for it
	/// again. An observer calls it when it could not present the update it was given, for example because another notice was open
	/// or the application was about to exit.
	/// </summary>
	public void OfferAgain(ProductUpdate update) {
		Guard.ArgumentNotNull(update, nameof(update));
		using (_state.EnterWriteScope()) {
			if (_notifiedVersion == update.Version)
				_notifiedVersion = null;
		}
	}

	/// <summary>
	/// Ends the schedule, cancels the check in flight and completes when that check has finished. Later calls return the same task,
	/// and the checker cannot be started again.
	/// </summary>
	public Task StopAsync() {
		TaskCompletionSource stopped;
		Scheduler<BaseJob> scheduler;
		BaseJob job;
		Task check;
		using (_state.EnterWriteScope()) {
			if (_stopped != null)
				return _stopped.Task;
			_stopped = stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			scheduler = _scheduler;
			job = _job;
			check = _check;
			_scheduler = null;
			_job = null;
		}
		_ = StopCoreAsync(stopped, scheduler, job, check);
		return stopped.Task;
	}

	/// <summary>Creates the scheduler that runs the repeating check. The checker owns and disposes it.</summary>
	protected virtual Scheduler<BaseJob> CreateScheduler() => new(SchedulerPolicy.DontThrow);

	protected void NotifyUpdateAvailable(ProductUpdate update) {
		OnUpdateAvailable(update);
		UpdateAvailable?.Invoke(update);
	}

	protected virtual void OnUpdateAvailable(ProductUpdate update) {
	}

	protected override async ValueTask DisposeInternalAsync() => await StopAsync().ConfigureAwait(false);

	// The scheduler runs the job on a worker. Joining the check there makes the next interval start when this check has ended.
	private void RunScheduledCheck() => Task.ExecuteSyncOverAsync(RunScheduledCheckAsync);

	private async Task RunScheduledCheckAsync() {
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => CheckAsync()).ConfigureAwait(false);
		// A check that shutdown cancelled or refused is not a failure.
		if (error != null && !IsStopped())
			Logger.Exception(error, "The product update check failed.");
	}

	private async Task<ProductUpdateCheckResult> RunCheckAsync() {
		var stopping = _stopping.Token;
		var result = await _updater.CheckForUpdateAsync(stopping).ConfigureAwait(false);
		// An answer that arrives during shutdown is not presented.
		stopping.ThrowIfCancellationRequested();
		bool notify;
		using (_state.EnterWriteScope()) {
			AvailableUpdate = result.Update;
			notify = result.IsUpdateAvailable && result.Update.Version != _notifiedVersion;
			// Recorded before the handlers run, so an observer that offers the update again is not overruled afterwards.
			if (notify)
				_notifiedVersion = result.Update.Version;
		}
		if (!notify)
			return result;
		// Raised outside the state scope, since a handler may call back into the checker. A notification that fails was not
		// delivered, so it is withdrawn for the next check to repeat.
		try {
			NotifyUpdateAvailable(result.Update);
		} catch {
			OfferAgain(result.Update);
			throw;
		}
		return result;
	}

	private async Task StopCoreAsync(TaskCompletionSource stopped, Scheduler<BaseJob> scheduler, BaseJob job, Task check) {
		var errors = new List<Exception>();
		// Scheduling ends first so no further check is admitted, then the check in flight is cancelled and awaited.
		if (scheduler != null) {
			Tools.Exceptions.ExecuteCapturingException(() => scheduler.RemoveJob(job), errors);
			Tools.Exceptions.ExecuteCapturingException(scheduler.Dispose, errors);
		}
		Tools.Exceptions.ExecuteCapturingException(_stopping.Cancel, errors);
		// The check's own outcome belongs to its caller; stopping only waits for it to end.
		if (check != null)
			await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => check).ConfigureAwait(false);
		Tools.Exceptions.ExecuteCapturingException(_stopping.Dispose, errors);
		if (errors.Count == 0)
			stopped.TrySetResult();
		else
			stopped.TrySetException(errors);
	}

	private bool IsStopped() {
		using (_state.EnterReadScope())
			return _stopped != null;
	}
}
