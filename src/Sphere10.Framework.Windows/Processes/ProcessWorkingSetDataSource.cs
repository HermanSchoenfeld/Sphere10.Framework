// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework;

namespace Sphere10.Framework.Windows;

/// <summary>Screen-owned scheduled reader; hidden screens retain capture, actual removal drains it, and history has a fixed record bound.</summary>
/// <remarks>
/// Each poll reads the native watch and publishes the retained faults as a fresh capture; the native reader is released once the final read has drained.
/// Subclasses may replace how the watch is started and read through <see cref="StartWatchAsync"/> and <see cref="ReadWatchAsync"/>.
/// </remarks>
public class ProcessWorkingSetDataSource : PollingDataSource<WorkingSetFaultRow, ProcessWorkingSetDataSource.Captures> {
	public event EventHandlerEx<WindowsWorkingSetSample> SampleAvailable;

	private readonly ProcessIdentity _process;
	private readonly BoundedHistory<WorkingSetFaultRow> _rows;
	private readonly SynchronizedObject _state = new();
	private WindowsWorkingSetWatch _watch;
	private IDisposable _registration;
	private Task _disposal;
	private bool _stopped;
	private long _sequence;
	private ulong _lost;
	private string _availability = string.Empty;

	/// <summary>A dormant reader of the process's working-set faults; <see cref="StartConfirmedAsync"/> enables the native watch.</summary>
	public ProcessWorkingSetDataSource(ProcessIdentity process, int maximumRecords)
		: base(new Captures()) {
		Guard.ArgumentInRange(maximumRecords, 100, 100000, nameof(maximumRecords));
		_process = process;
		_rows = new BoundedHistory<WorkingSetFaultRow>(maximumRecords);
		InternalDataSource.Owner = this;
	}

	public override bool Enabled {
		get => base.Enabled;
		set {
			using var scope = EnterWriteScope();
			ObjectDisposedException.ThrowIf(_disposal != null && value, this);
			base.Enabled = value;
		}
	}

	public bool IsRunning {
		get {
			using var scope = EnterReadScope();
			return !_stopped;
		}
	}

	/// <summary>Enables the native watch and starts the reader; the caller has confirmed that Windows keeps monitoring the target until it exits.</summary>
	/// <param name="lifetime">The application's data-source lifetime, when one registers polling sources for shutdown.</param>
	/// <remarks>A startup that fails or is cancelled disposes this source.</remarks>
	public async Task StartConfirmedAsync(DataSourceLifetime lifetime = null, CancellationToken cancellationToken = default) {
		cancellationToken.ThrowIfCancellationRequested();
		var accepted = false;
		await using var reject = Tools.Scope.ExecuteOnDisposeAsync(async () => {
			if (!accepted)
				await DisposeAsync().ConfigureAwait(false);
		});

		// The first refresh starts the native watch
		_registration = lifetime?.Register(this);
		await RefreshAsync(cancellationToken).ConfigureAwait(false);
		cancellationToken.ThrowIfCancellationRequested();

		// Later refreshes read it on the polling schedule
		using (EnterWriteScope()) {
			if (_disposal != null || lifetime?.IsCompleting == true)
				throw new OperationCanceledException("Working-set reader startup was stopped.");
			Enabled = true;
		}
		accepted = true;
	}

	public WorkingSetFaultRow[] ReadData(out string status) {
		using var scope = EnterReadScope();
		status = FormatStatus();
		return InternalDataSource.Current.Rows;
	}

	public override Task StopAsync() {
		using var scope = EnterWriteScope();
		if (_disposal == null) {
			_stopped = true;
			_disposal = base.StopAsync();
		}
		return _disposal;
	}

	/// <summary>Enables the native working-set watch of the process.</summary>
	protected virtual Task<WindowsWorkingSetWatch> StartWatchAsync(ProcessIdentity process, CancellationToken cancellationToken)
		=> WindowsWorkingSetWatch.StartAsync(process, cancellationToken);

	/// <summary>Reads the faults recorded by the watch since the previous read.</summary>
	protected virtual Task<WindowsWorkingSetSample> ReadWatchAsync(WindowsWorkingSetWatch watch, CancellationToken cancellationToken) => Task.FromResult(watch.Read());

	/// <summary>Runs once the final read has drained: the registration ends and the native reader is released.</summary>
	protected override async Task ReleaseAsync() {
		_registration?.Dispose();
		_registration = null;
		var watch = _watch;
		_watch = null;
		if (watch != null)
			await Task.Run(watch.Dispose).ConfigureAwait(false);
	}

	private async Task<WorkingSetFaultRow[]> FetchDataAsync(CancellationToken token) {
		token.ThrowIfCancellationRequested();

		// The first read starts the watch
		if (_watch == null) {
			var watch = await StartWatchAsync(_process, token).ConfigureAwait(false);
			Guard.Ensure(watch != null, "Working-set initialization must return its native reader owner.");
			// Retain even a late startup result so StopAsync can release its now-enabled native reader after acquisition drains.
			_watch = watch;
			token.ThrowIfCancellationRequested();
			return Array.Empty<WorkingSetFaultRow>();
		}

		// Later reads retain the new faults
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(async () => {
			// The data source owns the worker and finalization joins it before releasing the native reader.
			var sample = await ReadWatchAsync(_watch, token).ConfigureAwait(false);
			if (token.IsCancellationRequested)
				return;
			using (EnterWriteScope()) {
				if (token.IsCancellationRequested)
					return;
				_lost += sample.LostRecords;
				_availability = sample.Availability;
				foreach (var fault in sample.Faults)
					_rows.TryAdd(new WorkingSetFaultRow(++_sequence, sample.ObservedAtUtc, fault.InstructionPointer, fault.VirtualAddress, fault.ThreadId, fault.Flags));
			}
			if (!token.IsCancellationRequested)
				SampleAvailable?.Invoke(sample);
		}).ConfigureAwait(false);
		if (error != null && !(error is OperationCanceledException && token.IsCancellationRequested)) {
			using var failureScope = EnterWriteScope();
			_availability = error.Message;
			_stopped = true;
			base.Enabled = false;
		}
		token.ThrowIfCancellationRequested();
		using var scope = EnterReadScope();
		return _rows.Snapshot().Reverse().ToArray();
	}

	private string FormatStatus() {
		return $"{(_stopped ? "Reader Stopped" : "Reader Active")} • {_rows.Count:N0} Retained • {_rows.Discarded:N0} Old Records Discarded • {_lost:N0} Native Records Lost"
			+ (string.IsNullOrEmpty(_availability) ? string.Empty : " • " + _availability);
	}

	private ReadScope EnterReadScope() => _state.EnterReadScope();

	private WriteScope EnterWriteScope() => _state.EnterWriteScope();

	#region Inner Types

	/// <summary>Refreshes the served faults by reading its owner's watch.</summary>
	public sealed class Captures : RefreshableDataSourceBase<WorkingSetFaultRow, Capture> {
		internal Captures()
			: base(new Capture(Array.Empty<WorkingSetFaultRow>()), true) {
		}

		internal ProcessWorkingSetDataSource Owner { get; set; }

		protected override Capture CreateDataSource() {
			Guard.Ensure(Owner != null, "The working-set source owns this refreshable source.");
			return new Capture(Owner);
		}
	}

	/// <summary>The retained faults of one read of the watch.</summary>
	public sealed class Capture : AsyncListDataSource<WorkingSetFaultRow> {
		private readonly ProcessWorkingSetDataSource _owner;

		internal Capture(ProcessWorkingSetDataSource owner) => _owner = owner;

		internal Capture(IEnumerable<WorkingSetFaultRow> rows)
			: base(rows) {
		}

		public WorkingSetFaultRow[] Rows => List.ToArray();

		public override DataSourceCapabilities Capabilities => DataSourceCapabilities.ReadOnly | DataSourceCapabilities.CanNotify;

		protected override async Task<IEnumerable<WorkingSetFaultRow>> FetchAsync(CancellationToken cancellationToken)
			=> await _owner.FetchDataAsync(cancellationToken).ConfigureAwait(false);

		protected override string FormatValue(object value) => DataSourceFormatting.Value(value);
	}

	#endregion
}
