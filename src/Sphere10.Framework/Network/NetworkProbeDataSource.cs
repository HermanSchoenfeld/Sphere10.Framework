// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework;

/// <summary>Owns an explicitly requested probe session and its bounded observations. Reading or paging never starts a probe.</summary>
/// <remarks>Each poll runs one round of the current command and publishes its observations as they arrive; the polling decorator repeats admitted commands on the chosen interval.</remarks>
public sealed class NetworkProbeDataSource : PollingDataSource<NetworkProbeObservation, NetworkProbeDataSource.ProbeRefreshableDataSource> {
	public event EventHandlerEx<NetworkProbeObservation> ObservationAvailable;
	public event EventHandlerEx RunStateChanged;

	private readonly SynchronizedObject _state = new();
	private readonly BoundedHistory<NetworkProbeObservation> _history = new(1000, 2 * 1024 * 1024, row => (long)(row.Detail?.Length ?? 0) + 256);
	private ProbeRun _run;
	private Task _lastRun = Task.CompletedTask;
	private Task _stop;
	private bool _stopped;
	private bool _enabled;

	public NetworkProbeDataSource(Scheduler scheduler = null)
		: base(new ProbeRefreshableDataSource(), scheduler) {
		InternalDataSource.Owner = this;
	}

	public bool IsRunning {
		get {
			using var scope = _state.EnterReadScope();
			return _run != null;
		}
	}

	public Task RunCompletion {
		get {
			using var scope = _state.EnterReadScope();
			return _lastRun;
		}
	}

	public int RetainedCount => _history.Count;

	public long Discarded => _history.Discarded;

	/// <summary>The retained observations currently served, newest first.</summary>
	public NetworkProbeObservation[] Data => InternalDataSource.Current.Rows;

	/// <summary>Admits future repetitions of the current explicit command. It never invents a command or cancels an accepted probe.</summary>
	public override bool Enabled {
		get {
			using var scope = _state.EnterReadScope();
			return _enabled;
		}
		set {
			using var scope = _state.EnterWriteScope();
			if (_stopped && !value)
				return;
			ObjectDisposedException.ThrowIf(_stopped, this);
			_enabled = value;
			base.Enabled = value && _run is { Bootstrapped: true, Repeating: true, Finishing: false };
		}
	}

	/// <summary>Captures immutable command inputs; caller cancellation ends only its wait. CancelAsync owns cancellation of the command itself.</summary>
	public Task StartAsync(string operation, string target, TimeSpan timeout, TimeSpan repeatInterval,
		Func<string, TimeSpan, Action<NetworkProbeObservation>, CancellationToken, Task> work, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNullOrWhitespace(operation, nameof(operation));
		Guard.ArgumentNotNullOrWhitespace(target, nameof(target));
		Guard.Argument(timeout > TimeSpan.Zero, nameof(timeout), "Choose a positive probe timeout.");
		Guard.Argument(repeatInterval >= TimeSpan.Zero && repeatInterval.TotalMilliseconds <= uint.MaxValue - 1d, nameof(repeatInterval), "Choose once or a supported repeat interval.");
		Guard.ArgumentNotNull(work, nameof(work));
		cancellationToken.ThrowIfCancellationRequested();

		// Admit the command
		ProbeRun run;
		using (var scope = _state.EnterWriteScope()) {
			ObjectDisposedException.ThrowIf(_stopped, this);
			Guard.Ensure(_run == null, "Wait for or cancel the current probe before starting another command.");
			run = new ProbeRun(operation, target.Trim(), timeout, repeatInterval > TimeSpan.Zero, work);
			_run = run;
			_enabled = run.Repeating;
			_lastRun = run.Completion.Task;
		}

		// Run its first round now; repetitions follow on the interval
		if (run.Repeating)
			RefreshInterval = repeatInterval;
		_ = BootstrapAsync(run);
		return run.Completion.Task.WaitAsync(cancellationToken);
	}

	public Task ClearAsync(CancellationToken cancellationToken = default) {
		return Task.Run(() => {
			using (var scope = _state.EnterWriteScope()) {
				ObjectDisposedException.ThrowIf(_stopped, this);
				cancellationToken.ThrowIfCancellationRequested();
				Guard.Ensure(_run == null, "Cancel the current probe before clearing its retained observations.");
				_history.Clear();
			}
			InternalDataSource.PublishRows(Array.Empty<NetworkProbeObservation>());
		}, cancellationToken);
	}

	/// <summary>Cancels and drains only the active command. Its source can accept another command afterwards.</summary>
	public Task CancelAsync() {
		using var scope = _state.EnterWriteScope();
		_enabled = false;
		base.Enabled = false;
		return _run == null ? Task.CompletedTask : FinishRunAsync(_run);
	}

	public override Task StopAsync() {
		using var scope = _state.EnterWriteScope();
		if (_stop != null)
			return _stop;
		_stopped = true;
		_enabled = false;
		var pending = _run == null ? Task.CompletedTask : FinishRunAsync(_run);
		_stop = Task.Run(async () => {
			// Drain the command, clear what it retained, then stop polling
			var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => pending).ConfigureAwait(false);
			using (var state = _state.EnterWriteScope())
				_history.Clear(resetCounters: false);
			Tools.Exceptions.ExecuteCapturingException(() => InternalDataSource.PublishRows(Array.Empty<NetworkProbeObservation>()), out var publishFailure);
			error ??= publishFailure;
			error ??= await Tools.Exceptions.ExecuteCapturingExceptionAsync(base.StopAsync).ConfigureAwait(false);
			if (error != null)
				ExceptionDispatchInfo.Capture(error).Throw();
		});
		return _stop;
	}

	private async Task BootstrapAsync(ProbeRun run) {
		// The first round reports through the command's completion, not the automatic failure event.
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => InternalDataSource.RefreshAsync()).ConfigureAwait(false);
		bool reportFailure;
		using (var scope = _state.EnterWriteScope()) {
			if (error == null && ReferenceEquals(_run, run) && !_stopped && run.Repeating && !run.Finishing) {
				run.Bootstrapped = true;
				base.Enabled = _enabled;
				return;
			}
			reportFailure = !run.Finishing && !_stopped;
		}
		if (reportFailure && error != null && error is not OperationCanceledException)
			NotifyRefreshFailed(error);

		// The command's completion and StopAsync retain cleanup failures for their actual callers.
		await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => FinishRunAsync(run)).ConfigureAwait(false);
	}

	/// <summary>One round of the current command; without a command the retained observations are served unchanged.</summary>
	private async Task<NetworkProbeObservation[]> CollectAsync(CancellationToken lifetime) {
		ProbeRun run;
		using (_state.EnterReadScope())
			run = _run is { Finishing: false } current ? current : null;
		if (run == null)
			return Retained();

		// Run the command's work, publishing each observation as it arrives
		Debug.AssertNotMainThread();
		using var linked = CancellationTokenSource.CreateLinkedTokenSource(lifetime, run.Cancellation.Token);
		var token = linked.Token;
		Raise(RunStateChanged);
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => run.Work(run.Target, run.Timeout, row => Publish(run, row), token)).ConfigureAwait(false);
		if (error != null)
			Publish(run, new NetworkProbeObservation(DateTime.UtcNow, run.Operation, run.Target, string.Empty, null,
				error is OperationCanceledException ? token.IsCancellationRequested ? "Cancelled" : "Timed Out" : "Failed", null, null, error.Message));
		return Retained();
	}

	private NetworkProbeObservation[] Retained() {
		using var scope = _state.EnterReadScope();
		return _history.Snapshot().Reverse().ToArray();
	}

	private void Publish(ProbeRun run, NetworkProbeObservation row) {
		Guard.ArgumentNotNull(row, nameof(row));
		NetworkProbeObservation[] rows;
		using (var scope = _state.EnterWriteScope()) {
			if (_stopped || !ReferenceEquals(_run, run) || !_history.TryAdd(row))
				return;
			rows = _history.Snapshot().Reverse().ToArray();
		}

		// Each observation is served as soon as it arrives; the round's own publication is then superseded.
		InternalDataSource.PublishRows(rows);
		var subscribers = ObservationAvailable;
		if (subscribers != null)
			foreach (EventHandlerEx<NetworkProbeObservation> subscriber in subscribers.GetInvocationList())
				Tools.Exceptions.Execute(() => subscriber(row), NotifyRefreshFailed);
	}

	private Task FinishRunAsync(ProbeRun run) {
		using var scope = _state.EnterWriteScope();
		if (run.Finishing)
			return run.Completion.Task;
		run.Finishing = true;
		base.Enabled = false;

		// The command owns cancellation; the round in progress drains before the next command can be admitted. A failing owned
		// cancellation callback is the command's cleanup failure: its completion and StopAsync report it to their actual callers.
		Tools.Exceptions.ExecuteCapturingException(run.Cancellation.Cancel, out var cancellationFailure);
		var drain = Completion;
		_ = Task.Run(async () => {
			var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => drain).ConfigureAwait(false);
			using (var state = _state.EnterWriteScope()) {
				if (ReferenceEquals(_run, run)) {
					_run = null;
					_enabled = false;
				}
			}
			run.Cancellation.Dispose();
			if (error != null)
				NotifyRefreshFailed(error);
			Raise(RunStateChanged);
			var failure = error ?? cancellationFailure;
			if (failure == null)
				run.Completion.TrySetResult();
			else
				run.Completion.TrySetException(failure);
		});
		return run.Completion.Task;
	}

	private void Raise(EventHandlerEx subscribers) {
		if (subscribers != null)
			foreach (EventHandlerEx subscriber in subscribers.GetInvocationList())
				Tools.Exceptions.Execute(subscriber.Invoke, NotifyRefreshFailed);
	}

	#region Inner Types

	/// <summary>Refreshes the served observations by running one probe round of its owner; the owner also publishes rows as they arrive.</summary>
	public sealed class ProbeRefreshableDataSource : RefreshableDataSourceBase<NetworkProbeObservation, ProbeRound> {
		internal ProbeRefreshableDataSource()
			: base(new ProbeRound(Array.Empty<NetworkProbeObservation>()), true) {
		}

		internal NetworkProbeDataSource Owner { get; set; }

		/// <summary>Serves the given rows now; a round that began earlier is discarded.</summary>
		internal void PublishRows(NetworkProbeObservation[] rows) {
			Guard.ArgumentNotNull(rows, nameof(rows));
			Publish(new ProbeRound(rows));
		}

		protected override ProbeRound CreateDataSource() {
			Guard.Ensure(Owner != null, "The probe source owns this refreshable source.");
			return new ProbeRound(Owner);
		}
	}

	/// <summary>The observations of one probe round, fetched by running the round, or rows its owner already holds.</summary>
	public sealed class ProbeRound : AsyncListDataSource<NetworkProbeObservation> {
		private readonly NetworkProbeDataSource _owner;

		internal ProbeRound(NetworkProbeDataSource owner) => _owner = owner;

		internal ProbeRound(IEnumerable<NetworkProbeObservation> rows)
			: base(rows) {
		}

		public NetworkProbeObservation[] Rows => List.ToArray();

		public override DataSourceCapabilities Capabilities => DataSourceCapabilities.ReadOnly | DataSourceCapabilities.CanNotify;

		protected override async Task<IEnumerable<NetworkProbeObservation>> FetchAsync(CancellationToken cancellationToken)
			=> await _owner.CollectAsync(cancellationToken).ConfigureAwait(false);

		protected override string FormatValue(object value) => DataSourceFormatting.Value(value);
	}

	private sealed class ProbeRun(string operation, string target, TimeSpan timeout, bool repeating,
		Func<string, TimeSpan, Action<NetworkProbeObservation>, CancellationToken, Task> work) {
		public string Operation { get; } = operation;

		public string Target { get; } = target;

		public TimeSpan Timeout { get; } = timeout;

		public bool Repeating { get; } = repeating;

		public Func<string, TimeSpan, Action<NetworkProbeObservation>, CancellationToken, Task> Work { get; } = work;

		public TaskCompletionSource Completion { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public CancellationTokenSource Cancellation { get; } = new();

		public bool Bootstrapped { get; set; }

		public bool Finishing { get; set; }
	}

	#endregion
}
