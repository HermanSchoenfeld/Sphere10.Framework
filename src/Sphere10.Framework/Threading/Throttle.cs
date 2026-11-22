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
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework;

public sealed class Throttle : SynchronizedObject {
	public readonly TimeSpan _interval;
	public DateTime _lastEvent;
	private bool _waitingForStampede;
	private TaskCompletionSource<bool> _execution;
	private Func<Task> _pendingExecution;
	private readonly Queue<Func<Task>> _unthrottledExecutions;
	private long _lastRequestTimestamp;
	private bool _hasRequestedExecution;
	private bool _leadingRequest;

	public Throttle(float ratePerSecond)
		: this(TimeSpan.FromSeconds(1 / ratePerSecond)) {
	}

	public Throttle(float ratePerSecond, ThrottleMode mode)
		: this(TimeSpan.FromSeconds(1 / ratePerSecond), mode) {
	}

	public Throttle(TimeSpan interval)
		: this(interval, ThrottleMode.NotifyEveryInterval) {
	}

	public Throttle(TimeSpan interval, ThrottleMode mode) {
		Guard.Argument(Enum.IsDefined(typeof(ThrottleMode), mode), nameof(mode), "Select a supported throttle mode.");
		_lastEvent = DateTime.MinValue;
		_interval = interval;
		_waitingForStampede = false;
		Mode = mode;
		if (mode == ThrottleMode.None)
			_unthrottledExecutions = new Queue<Func<Task>>();
	}

	/// <summary>The immutable policy for ExecuteAsync. Legacy wait and admission methods retain their own contracts.</summary>
	public ThrottleMode Mode { get; }


	public void Wait() {
		var now = DateTime.Now;
		var duration = now.Subtract(_lastEvent);
		if (duration < _interval) {
			Thread.Sleep(_interval.Subtract(duration).ClipTo(TimeSpan.Zero, TimeSpan.MaxValue));
		}
		_lastEvent = DateTime.Now;
	}

	public async Task WaitAsync() {
		var now = DateTime.Now;
		var duration = now.Subtract(_lastEvent);

		if (duration < _interval) {
			await Task.Delay(_interval.Subtract(duration).ClipTo(TimeSpan.Zero, TimeSpan.MaxValue)).ConfigureAwait(false);
		}

		_lastEvent = DateTime.Now;
	}

	/// <summary>Runs callbacks sequentially according to Mode. The default periodically delivers the latest pending callback, even during continuous input.</summary>
	/// <remarks>None retains every callback in FIFO order; the other modes coalesce pending requests to the latest callback.
	/// Quiet-burst modes measure their interval from the most recent request; start-and-finished also delivers the leading request without duplicating isolated inputs.
	/// Zero interval starts immediately. Callers share completion of the current drain, including its callback failures.
	/// A callback may enqueue another request, but must not await execution on this same throttle because it would await itself.
	/// Callbacks choose their own dispatcher and cancellation; drain bookkeeping does not retain the caller's context.
	/// No synchronization scope is held while callbacks run.</remarks>
	public Task ExecuteAsync(Func<Task> action) {
		Guard.ArgumentNotNull(action, nameof(action));
		TaskCompletionSource<bool> completion;
		using (EnterWriteScope()) {
			var requestedAt = Stopwatch.GetTimestamp();
			if (Mode == ThrottleMode.None)
				_unthrottledExecutions.Enqueue(action);
			else {
				_pendingExecution = action;
				if (Mode == ThrottleMode.NotifyOnEventBurstStartAndFinished
					&& (!_hasRequestedExecution || ElapsedSince(_lastRequestTimestamp, requestedAt) >= _interval))
					_leadingRequest = true;
			}
			_lastRequestTimestamp = requestedAt;
			_hasRequestedExecution = true;
			if (_execution != null)
				return _execution.Task;
			completion = _execution = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
		}
		_ = ExecutePendingAsync(completion);
		return completion.Task;
	}

	public void RegisterEvent() {
		_lastEvent = DateTime.Now;
	}


	/// <summary>
	/// This throttle method returns true when interval from last call is at least the requested interval, false everything else.
	/// Useful when dealing with events that fire too often, and you only want to be notified once every period of time. Every 
	/// other notification be be scrapped.
	/// </summary>
	/// <returns><c>true</c> if this instance can pass; otherwise, <c>false</c>.</returns>
	public bool CanPass() {
		DateTime now = DateTime.Now;

		var canPass = true;
		using (EnterReadScope()) {
			canPass = now.Subtract(_lastEvent) >= _interval;
		}

		if (canPass) {
			using (EnterWriteScope()) {
				canPass = now.Subtract(_lastEvent) >= _interval;
				if (canPass) {
					_lastEvent = now;
					return true;
				}
			}
		}
		return false;
	}


	/// <summary>
	/// Waits the until a stampede of calls are finished. Two calls are considered in a "stampede" if they 
	/// occur within the interval provided in the constructor. This is useful if you are listening to a 
	/// collections remove event, but you only want to do something when all the removes have completed rather
	/// than doing something on every remove. The first caller in the stampede is kept waiting the other callers
	/// are dismissed.
	/// </summary>
	/// <returns>True - when the stampede has finished (first callers gets this), False - when the stampede is going on</returns>
	public async Task<bool> IsCallerFirstInStampede() {
		DateTime now = DateTime.Now;
		_lastEvent = now; // no need to lock for stampeding calls, faster this way, should be ok!

		using (EnterReadScope()) {
			if (_waitingForStampede)
				return false;
		}

		using (EnterWriteScope()) {
			if (_waitingForStampede)
				return false;
			_waitingForStampede = true;
		}

		while (_waitingForStampede) {
			await Task.Delay((int)_interval.TotalMilliseconds).ConfigureAwait(false);

			using (EnterWriteScope()) {
				_waitingForStampede = DateTime.Now.Subtract(_lastEvent) < _interval;
			}
		}

		using (EnterWriteScope()) {
			_waitingForStampede = false;
		}
		return true;
	}

	private async Task ExecutePendingAsync(TaskCompletionSource<bool> completion) {
		List<Exception> errors = null;
		while (true) {
			Func<Task> action;
			bool leading;
			using (EnterWriteScope()) {
				action = Mode == ThrottleMode.None
					? _unthrottledExecutions.Count > 0 ? _unthrottledExecutions.Dequeue() : null
					: _pendingExecution;
				_pendingExecution = null;
				leading = _leadingRequest;
				_leadingRequest = false;
				if (action == null)
					_execution = null;
			}
			if (action == null)
				break;
			try {
				if (Mode == ThrottleMode.NotifyEveryInterval) {
					// Preserve existing periodic timing and RegisterEvent behavior; requests never postpone this wait.
					await WaitAsync().ConfigureAwait(false);
					using (EnterWriteScope()) {
						if (_pendingExecution != null) {
							action = _pendingExecution;
							_pendingExecution = null;
						}
					}
				} else if (Mode != ThrottleMode.None && !leading)
					action = await WaitForQuietExecutionAsync(action).ConfigureAwait(false);
				await action().ConfigureAwait(false);
			} catch (Exception error) {
				(errors ??= new List<Exception>()).Add(error);
			}
		}
		if (errors == null)
			completion.SetResult(true);
		else
			completion.SetException(errors);
	}

	private async Task<Func<Task>> WaitForQuietExecutionAsync(Func<Task> action) {
		while (true) {
			TimeSpan remaining;
			using (EnterWriteScope()) {
				remaining = _interval - ElapsedSince(_lastRequestTimestamp, Stopwatch.GetTimestamp());
				if (_leadingRequest || remaining <= TimeSpan.Zero) {
					// Admit against the same request timestamp used for the quiet-period check.
					// A later request then remains pending for a separate delivery.
					if (_pendingExecution != null) {
						action = _pendingExecution;
						_pendingExecution = null;
					}
					_leadingRequest = false;
					return action;
				}
			}
			await Task.Delay(remaining).ConfigureAwait(false);
		}
	}

	private static TimeSpan ElapsedSince(long started, long now) => TimeSpan.FromSeconds((now - started) / (double)Stopwatch.Frequency);

}

