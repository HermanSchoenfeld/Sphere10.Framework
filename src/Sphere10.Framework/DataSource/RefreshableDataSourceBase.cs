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

/// <summary>Refreshes a data source by replacing it: a fresh source is created and loaded while the current one stays readable, then swapped in and the old one disposed.</summary>
/// <remarks>
/// A template: subclasses create each fresh source in <see cref="CreateDataSource"/>; clients which compose a source without deriving one use
/// <see cref="ActionRefreshableDataSource{TItem, TConcrete}"/>. The decorator works over a synchronous or an asynchronous source alike:
/// <see cref="Refresh"/> loads the fresh source with <see cref="ILoadable.Load"/> on the calling thread and <see cref="RefreshAsync"/> with
/// <see cref="ILoadable.LoadAsync"/> on a worker, overlapping callers sharing one refresh. Reads load an unpublished source first. An explicit
/// <see cref="Publish"/> wins over a refresh that began earlier; <see cref="CanPublish"/> lets a subclass reject and repeat a fetch.
/// </remarks>
public abstract class RefreshableDataSourceBase<TItem, TConcrete> : DataSourceDecorator<TItem, TConcrete>, IRefreshableDataSource<TItem>
	where TConcrete : IDataSource<TItem> {
	public override event EventHandlerEx Changed { add => _changed += value; remove => _changed -= value; }
	public event EventHandlerEx<object> Loading;
	public event EventHandlerEx<object> Loaded;

	private readonly SynchronizedObject _state = new();
	private readonly CancellationTokenSource _lifetime = new();
	private readonly EventHandlerEx _relay;
	private EventHandlerEx _changed;
	private Task _pending = Task.CompletedTask;
	private Task _dispose;
	private volatile bool _published;

	/// <summary>Serves the initial source as a placeholder: the first load refreshes through <see cref="CreateDataSource"/>.</summary>
	protected RefreshableDataSourceBase(TConcrete initial)
		: this(initial, false) {
	}

	/// <summary>Serves the initial source; when <paramref name="published"/> it is real data (an empty inventory before an explicit bootstrap) and reads serve it without refreshing.</summary>
	protected RefreshableDataSourceBase(TConcrete initial, bool published)
		: base(initial) {
		_relay = () => NotifyChanged(_changed);
		_published = published;
		Attach(initial);
	}

	/// <summary>The source currently served.</summary>
	public TConcrete Current => InternalDataSource;

	/// <summary>True until a refresh or publication has served real data, or while the served source still requires a load; reads then refresh first.</summary>
	public bool RequiresLoad => !_published || InternalDataSource is ILoadable { RequiresLoad: true };

	public Task Completion {
		get {
			using var scope = _state.EnterReadScope();
			return _pending;
		}
	}

	public bool IsDisposed {
		get {
			using var scope = _state.EnterReadScope();
			return _dispose != null;
		}
	}

	public override long Count {
		get {
			if (RequiresLoad)
				Load();
			return base.Count;
		}
	}

	public override Task<long> CountAsync => ReadCountAsync();

	/// <summary>Cancelled by disposal; the asynchronous load of a fresh source receives it.</summary>
	protected CancellationToken Lifetime => _lifetime.Token;

	/// <summary>Creates, loads and publishes a fresh source on the calling thread.</summary>
	public void Refresh() {
		ThrowIfDisposed();
		while (true) {
			// Create and load the fresh source while the current one stays readable
			var started = InternalDataSource;
			var latest = CreateDataSource();
			Guard.Ensure(latest != null, "A fresh data source must be created.");
			var outcome = SwapOutcome.Rejected;
			try {
				if (latest is ILoadable { RequiresLoad: true } loadable)
					loadable.Load();

				// Publish it unless superseded or rejected
				outcome = TrySwap(started, latest, out var old);
				if (outcome == SwapOutcome.Published) {
					Release(old);
					NotifyChanged(_changed);
				}
			} finally {
				if (outcome != SwapOutcome.Published)
					Release(latest);
			}
			if (outcome != SwapOutcome.Rejected)
				return;
		}
	}

	/// <summary>Creates, loads and publishes a fresh source on a worker. Overlapping callers share one refresh; cancellation ends only this caller's wait.</summary>
	public Task RefreshAsync(CancellationToken cancellationToken = default) {
		cancellationToken.ThrowIfCancellationRequested();
		Task pending;
		using (_state.EnterWriteScope()) {
			ThrowIfDisposed();
			if (_pending.IsCompleted) {
				var refresh = Task.Run(RefreshCoreAsync);
				_ = Tools.Exceptions.ExecuteCapturingExceptionAsync(() => refresh);
				_pending = refresh;
			}
			pending = _pending;
		}
		return pending.WaitAsync(cancellationToken);
	}

	public void Load() {
		if (!RequiresLoad)
			return;
		NotifyLoading();
		Refresh();
		NotifyLoaded();
	}

	public async Task LoadAsync(CancellationToken cancellationToken = default) {
		if (!RequiresLoad)
			return;
		NotifyLoading();
		await RefreshAsync(cancellationToken).ConfigureAwait(false);
		NotifyLoaded();
	}

	public override DataSourceItems<TItem> ReadRange(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection) {
		if (RequiresLoad)
			Load();
		return base.ReadRange(searchTerm, pageLength, page, sortProperty, sortDirection);
	}

	public override async Task<DataSourceItems<TItem>> ReadRangeAsync(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection) {
		if (RequiresLoad)
			await LoadAsync().ConfigureAwait(false);
		return await base.ReadRangeAsync(searchTerm, pageLength, page, sortProperty, sortDirection).ConfigureAwait(false);
	}

	/// <summary>Requests observed, non-blocking disposal. Owners awaiting completion use <see cref="DisposeAsync"/>.</summary>
	public void Dispose() => _ = Tools.Exceptions.ExecuteCapturingExceptionAsync(DisposeCoreAsync);

	/// <summary>Cancels the lifetime, drains the asynchronous refresh in progress, disposes the served source and then releases owned dependencies.</summary>
	public ValueTask DisposeAsync() => new ValueTask(DisposeCoreAsync());

	/// <summary>Creates the fresh source of a refresh, unloaded (the refresh loads it in its own direction) or already holding its rows.</summary>
	protected abstract TConcrete CreateDataSource();

	/// <summary>Evaluated under the publication lock with the loaded fresh source. A false result discards it and refreshes again.</summary>
	protected virtual bool CanPublish(TConcrete latest) => true;

	/// <summary>Releases owned dependencies once the final refresh has drained and the served source is disposed.</summary>
	protected virtual Task ReleaseAsync() => Task.CompletedTask;

	/// <summary>Serves the given source now, loading it first if required. Wins over a refresh that began earlier.</summary>
	protected void Publish(TConcrete source) {
		Guard.ArgumentNotNull(source, nameof(source));
		if (source is ILoadable { RequiresLoad: true } loadable)
			loadable.Load();
		TConcrete old;
		using (_state.EnterWriteScope()) {
			ThrowIfDisposed();
			old = Replace(source);
			_published = true;
		}
		Release(old);
		NotifyChanged(_changed);
	}

	private async Task RefreshCoreAsync() {
		while (true) {
			// Create and load the fresh source while the current one stays readable
			var started = InternalDataSource;
			var latest = CreateDataSource();
			Guard.Ensure(latest != null, "A fresh data source must be created.");
			var outcome = SwapOutcome.Rejected;
			try {
				if (latest is ILoadable { RequiresLoad: true } loadable)
					await loadable.LoadAsync(_lifetime.Token).ConfigureAwait(false);
				_lifetime.Token.ThrowIfCancellationRequested();

				// Publish it unless superseded or rejected
				outcome = TrySwap(started, latest, out var old);
				if (outcome == SwapOutcome.Published) {
					Release(old);
					NotifyChanged(_changed);
				}
			} finally {
				if (outcome != SwapOutcome.Published)
					Release(latest);
			}
			if (outcome != SwapOutcome.Rejected)
				return;
		}
	}

	/// <summary>Publishes the loaded source unless something else was published since the refresh began or the subclass rejects it.</summary>
	private SwapOutcome TrySwap(TConcrete started, TConcrete latest, out TConcrete old) {
		old = default;
		using var scope = _state.EnterWriteScope();
		ThrowIfDisposed();
		if (!ReferenceEquals(InternalDataSource, started))
			return SwapOutcome.Superseded;
		if (!CanPublish(latest))
			return SwapOutcome.Rejected;
		old = Replace(latest);
		_published = true;
		return SwapOutcome.Published;
	}

	/// <summary>Runs under the state lock.</summary>
	private TConcrete Replace(TConcrete source) {
		var old = InternalDataSource;
		Detach(old);
		InternalDataSource = source;
		Attach(source);
		return old;
	}

	private void Attach(TConcrete source) {
		if (source.Capabilities.HasFlag(DataSourceCapabilities.CanNotify))
			source.Changed += _relay;
	}

	private void Detach(TConcrete source) {
		if (source.Capabilities.HasFlag(DataSourceCapabilities.CanNotify))
			source.Changed -= _relay;
	}

	private static void Release(TConcrete source) {
		if (source is IDisposable disposable)
			Tools.Exceptions.ExecuteIgnoringException(disposable.Dispose);
	}

	private void ThrowIfDisposed() => ObjectDisposedException.ThrowIf(_dispose != null, this);

	private Task DisposeCoreAsync() {
		Task pending;
		TaskCompletionSource disposed;
		using (_state.EnterWriteScope()) {
			if (_dispose != null)
				return _dispose;
			disposed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			_dispose = disposed.Task;
			pending = _pending;
		}
		_ = DisposeRunAsync(pending, disposed);
		return disposed.Task;
	}

	private async Task DisposeRunAsync(Task pending, TaskCompletionSource disposed) {
		// Cancel and drain: a refresh which finishes after disposal is observed here; its publication was rejected under the lock
		Tools.Exceptions.ExecuteCapturingException(_lifetime.Cancel, out var cancellationFailure);
		await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => pending).ConfigureAwait(false);

		// Dispose the served source, then release owned dependencies
		TConcrete served;
		using (_state.EnterWriteScope()) {
			served = InternalDataSource;
			Detach(served);
		}
		Release(served);
		var releaseFailure = await Tools.Exceptions.ExecuteCapturingExceptionAsync(ReleaseAsync).ConfigureAwait(false);
		_lifetime.Dispose();

		// Report the first failure
		var failure = cancellationFailure ?? releaseFailure;
		if (failure != null)
			disposed.TrySetException(failure);
		else
			disposed.TrySetResult();
	}

	private async Task<long> ReadCountAsync() {
		if (RequiresLoad)
			await LoadAsync().ConfigureAwait(false);
		return await base.CountAsync.ConfigureAwait(false);
	}

	private void NotifyLoading() => Tools.Exceptions.Execute(() => Loading?.Invoke(this), ReportSubscriberFailure);

	private void NotifyLoaded() => Tools.Exceptions.Execute(() => Loaded?.Invoke(this), ReportSubscriberFailure);

	private static void ReportSubscriberFailure(Exception error)
		=> Tools.Exceptions.ExecuteIgnoringException(() => SystemLog.Logger.Exception(error, "Data source load notification failed."));

	#region Inner Types

	private enum SwapOutcome {
		Published,
		Superseded,
		Rejected
	}

	#endregion
}
