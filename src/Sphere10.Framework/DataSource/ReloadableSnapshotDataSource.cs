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

/// <summary>A lazily loaded snapshot data source whose subsequent queries page the published inventory without recollecting it.</summary>
public class ReloadableSnapshotDataSource<T> : SnapshotDataSource<T> {
	private readonly Func<CancellationToken, Task<T[]>> _capture;
	private readonly AsyncSynchronizedGate _reloadGate = new();
	private readonly SynchronizedObject _publication = new();
	private long _version;
	private int _loaded;

	public ReloadableSnapshotDataSource(Func<CancellationToken, Task<T[]>> capture, Func<object, string> formatValue = null)
		: base(formatValue) {
		Guard.ArgumentNotNull(capture, nameof(capture));
		_capture = capture;
	}

	public bool IsLoaded => Volatile.Read(ref _loaded) != 0;

	public override T[] Snapshot {
		get {
			EnsureLoaded();
			return base.Snapshot;
		}
	}

	public override long Count {
		get {
			EnsureLoaded();
			return base.Count;
		}
	}

	public override Task<long> CountAsync => GetCountAsync();

	/// <summary>Publishes explicit membership and prevents an already running capture from overwriting it.</summary>
	public override void ReplaceSnapshot(T[] snapshot) {
		Guard.ArgumentNotNull(snapshot, nameof(snapshot));
		using var scope = _publication.EnterWriteScope();
		base.ReplaceSnapshot(snapshot);
		_version++;
		Volatile.Write(ref _loaded, 1);
	}

	public override DataSourceItems<T> ReadRange(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection) {
		Guard.ArgumentGT(pageLength, 0, nameof(pageLength));
		Guard.ArgumentGTE(page, 0, nameof(page));
		EnsureLoaded();
		return base.ReadRange(searchTerm, pageLength, page, sortProperty, sortDirection);
	}

	public override async Task<DataSourceItems<T>> ReadRangeAsync(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection) {
		Guard.ArgumentGT(pageLength, 0, nameof(pageLength));
		Guard.ArgumentGTE(page, 0, nameof(page));
		await EnsureLoadedAsync(CancellationToken.None).ConfigureAwait(false);
		return await base.ReadRangeAsync(searchTerm, pageLength, page, sortProperty, sortDirection).ConfigureAwait(false);
	}

	/// <summary>Loads on first access; the returned array owns only its copied collection membership.</summary>
	public async Task<T[]> GetSnapshotAsync(CancellationToken cancellationToken = default) {
		await EnsureLoadedAsync(cancellationToken).ConfigureAwait(false);
		return base.Snapshot;
	}

	public void Reload() {
		DataSourceThreadDiagnostics.Default.AssertBackgroundExecution(this);
		Task.Run(() => ReloadAsync()).GetAwaiter().GetResult();
	}

	/// <summary>Serializes native captures and atomically publishes each completed result. Failure/cancellation preserves the previous inventory.</summary>
	public async Task ReloadAsync(CancellationToken cancellationToken = default) {
		using var scope = await _reloadGate.EnterScopeAsync(cancellationToken).ConfigureAwait(false);
		await CaptureAndPublishAsync(cancellationToken).ConfigureAwait(false);
	}

	private void EnsureLoaded() {
		if (!IsLoaded) {
			DataSourceThreadDiagnostics.Default.AssertBackgroundExecution(this);
			Task.Run(() => EnsureLoadedAsync(CancellationToken.None)).GetAwaiter().GetResult();
		}
	}

	private async Task EnsureLoadedAsync(CancellationToken cancellationToken) {
		cancellationToken.ThrowIfCancellationRequested();
		if (IsLoaded)
			return;
		using var scope = await _reloadGate.EnterScopeAsync(cancellationToken).ConfigureAwait(false);
		if (!IsLoaded)
			await CaptureAndPublishAsync(cancellationToken).ConfigureAwait(false);
	}

	private async Task<long> GetCountAsync() {
		await EnsureLoadedAsync(CancellationToken.None).ConfigureAwait(false);
		return base.Count;
	}

	private async Task CaptureAndPublishAsync(CancellationToken cancellationToken) {
		long version;
		using (_publication.EnterReadScope())
			version = _version;
		// Even a loader with synchronous setup executes away from the asynchronous consumer's dispatcher.
		var rows = await Task.Run(() => {
			DataSourceThreadDiagnostics.Default.AssertBackgroundExecution(this);
			return _capture(cancellationToken);
		}, cancellationToken).ConfigureAwait(false);
		Guard.Ensure(rows != null, "A snapshot capture must return its complete inventory.");
		cancellationToken.ThrowIfCancellationRequested();
		using var scope = _publication.EnterWriteScope();
		if (_version != version)
			return;
		base.ReplaceSnapshot(rows);
		_version++;
		Volatile.Write(ref _loaded, 1);
	}
}
