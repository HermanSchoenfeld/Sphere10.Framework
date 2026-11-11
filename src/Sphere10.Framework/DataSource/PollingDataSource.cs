// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework;

/// <summary>Decorates a refreshable data source with automatic refresh on a configurable schedule, and owns its lifecycle.</summary>
/// <remarks>
/// Automatic refresh runs while both <see cref="Enabled"/> (lifecycle admission) and <see cref="AutoRefresh"/> (user policy) are true, as one repeating
/// Framework <see cref="Scheduler"/> job which starts a refresh every <see cref="RefreshInterval"/>; overlapping ticks join the refresh in progress.
/// Changing either policy or the interval replaces the job without cancelling an admitted refresh. <see cref="StopAsync"/> is permanent and disposes the
/// decorated source. Every member forwards to the decorated source in its own direction; nothing here adapts sync to async or back.
/// </remarks>
public class PollingDataSource<TItem, TConcrete> : DataSourceDecorator<TItem, TConcrete>, IRefreshableDataSource<TItem>, IRefreshingDataSource
	where TConcrete : IRefreshableDataSource<TItem> {
	public event EventHandlerEx<Exception> RefreshFailed;
	public event EventHandlerEx<object> Loading { add => InternalDataSource.Loading += value; remove => InternalDataSource.Loading -= value; }
	public event EventHandlerEx<object> Loaded { add => InternalDataSource.Loaded += value; remove => InternalDataSource.Loaded -= value; }

	private readonly Scheduler _scheduler;
	private readonly SynchronizedObject _state = new();
	private BaseJob _job;
	private Task _observed;
	private Task _stop;
	private bool _enabled;
	private bool _autoRefresh = true;
	private TimeSpan _refreshInterval = TimeSpan.FromSeconds(1);

	/// <param name="source">The owned refreshable source.</param>
	/// <param name="scheduler">The started scheduler that times automatic refresh; the process-wide asynchronous scheduler by default.</param>
	public PollingDataSource(TConcrete source, Scheduler scheduler = null)
		: base(source) {
		_scheduler = scheduler ?? Scheduler.Asynchronous;
	}

	/// <summary>Lifecycle admission for automatic refresh; false until an owner enables the source.</summary>
	public virtual bool Enabled {
		get {
			using var scope = _state.EnterReadScope();
			return _enabled;
		}
		set => Configure(() => _enabled = value, allowAfterStop: !value);
	}

	/// <summary>The user's automatic-refresh policy, independent of <see cref="Enabled"/>.</summary>
	public virtual bool AutoRefresh {
		get {
			using var scope = _state.EnterReadScope();
			return _autoRefresh;
		}
		set => Configure(() => _autoRefresh = value, allowAfterStop: !value);
	}

	/// <summary>A positive interval within the scheduler's timer range, or <see cref="Timeout.InfiniteTimeSpan"/> when the owner supplies its own clock.</summary>
	public virtual TimeSpan RefreshInterval {
		get {
			using var scope = _state.EnterReadScope();
			return _refreshInterval;
		}
		set {
			Guard.Argument(value == Timeout.InfiniteTimeSpan || (value > TimeSpan.Zero && value.TotalMilliseconds <= int.MaxValue), nameof(value),
				"Choose a positive refresh interval within the scheduler's timer range, or an infinite interval for an externally clocked source.");
			Configure(() => _refreshInterval = value, allowAfterStop: false);
		}
	}

	public bool IsStopped {
		get {
			using var scope = _state.EnterReadScope();
			return _stop != null;
		}
	}

	public bool IsDisposed => IsStopped;

	public bool RequiresLoad => InternalDataSource.RequiresLoad;

	public Task Completion => InternalDataSource.Completion;

	/// <summary>Runs under the state lock.</summary>
	private bool ShouldSchedule => _enabled && _autoRefresh && _refreshInterval != Timeout.InfiniteTimeSpan;

	public void Load() {
		ThrowIfStopped();
		InternalDataSource.Load();
	}

	public Task LoadAsync(CancellationToken cancellationToken = default) {
		ThrowIfStopped();
		return InternalDataSource.LoadAsync(cancellationToken);
	}

	public void Refresh() {
		ThrowIfStopped();
		try {
			InternalDataSource.Refresh();
		} catch (Exception error) when (ShouldReport(error)) {
			NotifyRefreshFailed(error);
			throw;
		}
	}

	/// <summary>Refreshes now, whether or not automatic refresh is admitted. Overlapping callers share one refresh; cancellation ends only this caller's wait.</summary>
	public Task RefreshAsync(CancellationToken cancellationToken = default) {
		cancellationToken.ThrowIfCancellationRequested();
		ThrowIfStopped();
		return Admit().WaitAsync(cancellationToken);
	}

	/// <summary>Permanently disables automatic refresh, removes the job and disposes the decorated source, draining its refresh. Repeated calls return the same task.</summary>
	public virtual Task StopAsync() {
		BaseJob job;
		TaskCompletionSource stopped;
		using (_state.EnterWriteScope()) {
			if (_stop != null)
				return _stop;
			_enabled = false;
			job = _job;
			_job = null;
			stopped = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			_stop = stopped.Task;
		}
		if (job != null)
			_scheduler.RemoveJob(job);
		_ = StopCoreAsync(stopped);
		return stopped.Task;
	}

	/// <summary>Requests observed, non-blocking shutdown. Owners awaiting completion use <see cref="StopAsync"/> or <see cref="DisposeAsync"/>.</summary>
	public virtual void Dispose() => _ = Tools.Exceptions.ExecuteCapturingExceptionAsync(StopAsync);

	public virtual ValueTask DisposeAsync() => new ValueTask(StopAsync());

	protected virtual void OnRefreshFailed(Exception error) {
	}

	protected void NotifyRefreshFailed(Exception error) {
		Tools.Exceptions.Execute(() => OnRefreshFailed(error), ReportSubscriberFailure);
		var subscribers = RefreshFailed;
		if (subscribers == null)
			return;
		foreach (EventHandlerEx<Exception> subscriber in subscribers.GetInvocationList())
			Tools.Exceptions.Execute(() => subscriber(error), ReportSubscriberFailure);
	}

	/// <summary>Releases owned dependencies after the decorated source has been disposed.</summary>
	protected virtual Task ReleaseAsync() => Task.CompletedTask;

	private void Configure(Action change, bool allowAfterStop) {
		BaseJob former;
		BaseJob next;
		using (_state.EnterWriteScope()) {
			if (_stop != null) {
				ObjectDisposedException.ThrowIf(!allowAfterStop, this);
				return;
			}
			var before = (ShouldSchedule, _refreshInterval);
			change();
			if (before == (ShouldSchedule, _refreshInterval))
				return;
			former = _job;
			_job = ShouldSchedule ? CreateJob() : null;
			next = _job;
		}
		// Replacing the job restarts the wait with the new policy; the admitted refresh itself is never cancelled by a policy change.
		if (former != null)
			_scheduler.RemoveJob(former);
		if (next != null)
			_scheduler.AddJob(next);
	}

	/// <summary>Runs under the state lock.</summary>
	private BaseJob CreateJob() {
		BaseJob job = null;
		var builder = JobBuilder.For(() => Tick(job)).Called("Data source polling").Repeat.OnInterval(_refreshInterval);
		if (_scheduler.Policy.HasFlag(SchedulerPolicy.ForceSyncronous))
			builder.RunSyncronously();
		job = builder.Build();
		return job;
	}

	/// <summary>Starts a refresh and returns; the scheduler thread never waits for acquisition.</summary>
	private void Tick(BaseJob job) {
		using (_state.EnterReadScope()) {
			// A schedule taken from the timeline before its job was replaced still runs once; it must not refresh for a superseded policy.
			if (_stop != null || !ReferenceEquals(job, _job))
				return;
		}
		_ = Tools.Exceptions.ExecuteCapturingExceptionAsync(() => Admit());
	}

	/// <summary>Starts or joins the decorated source's refresh and reports its failure once.</summary>
	private Task Admit() {
		var refresh = InternalDataSource.RefreshAsync();
		using (_state.EnterWriteScope()) {
			if (ReferenceEquals(_observed, refresh))
				return refresh;
			_observed = refresh;
		}
		_ = ReportAsync(refresh);
		return refresh;
	}

	private async Task ReportAsync(Task refresh) {
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => refresh).ConfigureAwait(false);
		if (error != null && ShouldReport(error))
			NotifyRefreshFailed(error);
	}

	private bool ShouldReport(Exception error) => error is not OperationCanceledException || !IsStopped;

	private async Task StopCoreAsync(TaskCompletionSource stopped) {
		var failure = await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => InternalDataSource.DisposeAsync().AsTask()).ConfigureAwait(false);
		failure ??= await Tools.Exceptions.ExecuteCapturingExceptionAsync(ReleaseAsync).ConfigureAwait(false);
		if (failure != null)
			stopped.TrySetException(failure);
		else
			stopped.TrySetResult();
	}

	private void ThrowIfStopped() => ObjectDisposedException.ThrowIf(IsStopped, this);

	private static void ReportSubscriberFailure(Exception error)
		=> Tools.Exceptions.ExecuteIgnoringException(() => SystemLog.Logger.Exception(error, "Data source refresh failure subscriber failed."));
}


/// <summary>Interface-typed convenience form of <see cref="PollingDataSource{TItem, TConcrete}"/>.</summary>
public class PollingDataSource<TItem> : PollingDataSource<TItem, IRefreshableDataSource<TItem>> {
	public PollingDataSource(IRefreshableDataSource<TItem> source, Scheduler scheduler = null)
		: base(source, scheduler) {
	}
}
