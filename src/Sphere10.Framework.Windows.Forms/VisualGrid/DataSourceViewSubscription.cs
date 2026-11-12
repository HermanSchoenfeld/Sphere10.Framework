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
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Coalesces a borrowed data source's notifications into presentation on a control's dispatcher.</summary>
/// <remarks>The callback reads or projects published data. It must not initiate acquisition. Hiding retains one pending update; disposal detaches without disposing the source.</remarks>
public sealed class DataSourceViewSubscription<T> : IDisposable {
	private readonly Control _owner;
	private readonly IDataSource<T> _source;
	private readonly Func<Task> _present;
	private readonly Action<Exception> _reportFailure;
	private readonly Func<bool> _invalidate;
	private readonly Func<bool> _canPresent;
	private readonly List<Control> _ancestors = new();
	private ApplicationScreen _screen;
	private IApplicationScreenHost _host;
	private bool _requestedActive = true;
	private int _queued;
	private int _pending;
	private volatile bool _active;
	private volatile bool _disposed;

	public DataSourceViewSubscription(Control owner, IDataSource<T> source, Func<Task> refresh, Action<Exception> reportFailure = null)
		: this(owner, source, refresh, reportFailure, null, null) {
	}

	/// <summary>Defers presentation while the owner has a draft or another temporary admission veto, retaining the pending change.</summary>
	public DataSourceViewSubscription(Control owner, IDataSource<T> source, Func<Task> refresh, Action<Exception> reportFailure, Func<bool> canPresent)
		: this(owner, source, refresh, reportFailure, null, canPresent) {
	}

	internal DataSourceViewSubscription(Control owner, IDataSource<T> source, Func<Task> refresh, Action<Exception> reportFailure,
		Func<bool> invalidate, Func<bool> canPresent
	) {
		Guard.ArgumentNotNull(owner, nameof(owner));
		Guard.ArgumentNotNull(source, nameof(source));
		Guard.ArgumentNotNull(refresh, nameof(refresh));
		_owner = owner;
		_source = source;
		_present = refresh;
		_reportFailure = reportFailure ?? (error => SystemLog.Logger.Exception(error, "Could not present changed data source values."));
		_invalidate = invalidate;
		_canPresent = canPresent;
		_source.Changed += SourceChanged;
		_owner.VisibleChanged += OwnerVisibleChanged;
		_owner.HandleCreated += OwnerReady;
		_owner.Disposed += OwnerDisposed;
		TrackPresentation();
	}

	/// <summary>Controls presentation demand without changing the borrowed source's acquisition policy.</summary>
	public void SetActive(bool active) {
		_requestedActive = active;
		UpdateActiveState();
	}

	public void Dispose() {
		if (_disposed)
			return;
		_disposed = true;
		_active = false;
		_source.Changed -= SourceChanged;
		_owner.VisibleChanged -= OwnerVisibleChanged;
		_owner.HandleCreated -= OwnerReady;
		_owner.Disposed -= OwnerDisposed;
		foreach (var ancestor in _ancestors)
			ancestor.ParentChanged -= OwnerReady;
		_ancestors.Clear();
		ReleaseScreen();
		if (_host != null)
			_host.ActiveScreenChanged -= ActiveScreenChanged;
		_host = null;
	}

	internal void ConsumeChange() => Interlocked.Exchange(ref _pending, 0);

	internal void DeferChange() => Interlocked.Exchange(ref _pending, 1);

	internal void TryPresent() => QueueRefresh();

	private void SourceChanged() {
		if (_disposed || !_source.Capabilities.HasFlag(DataSourceCapabilities.CanNotify))
			return;
		if (_invalidate?.Invoke() == false)
			return;
		Interlocked.Exchange(ref _pending, 1);
		QueueRefresh();
	}

	private void OwnerReady(object sender, EventArgs args) => TrackPresentation();

	private void ActiveScreenChanged(ApplicationScreen screen) => UpdateActiveState();

	private void OwnerVisibleChanged(object sender, EventArgs args) => UpdateActiveState();

	private void TrackPresentation() {
		if (_disposed)
			return;
		foreach (var ancestor in _ancestors)
			ancestor.ParentChanged -= OwnerReady;
		_ancestors.Clear();
		ApplicationScreen screen = null;
		for (var control = _owner; control != null; control = control.Parent) {
			_ancestors.Add(control);
			control.ParentChanged += OwnerReady;
			if (screen == null && control is ApplicationScreen candidate)
				screen = candidate;
		}
		if (!ReferenceEquals(screen, _screen)) {
			ReleaseScreen();
			_screen = screen;
			if (_screen != null) {
				_screen.ScreenDisplayed += OwnerReady;
				_screen.ScreenDestroyed += OwnerDisposed;
			}
		}
		if (!ReferenceEquals(_host, _screen?.Host)) {
			if (_host != null)
				_host.ActiveScreenChanged -= ActiveScreenChanged;
			_host = _screen?.Host;
			if (_host != null)
				_host.ActiveScreenChanged += ActiveScreenChanged;
		}
		UpdateActiveState();
	}

	private void UpdateActiveState() {
		if (_disposed)
			return;
		_active = _requestedActive && _owner.Visible && (_host == null || ReferenceEquals(_host.ActiveScreen, _screen));
		if (_active)
			QueueRefresh();
	}

	private void ReleaseScreen() {
		if (_screen == null)
			return;
		_screen.ScreenDisplayed -= OwnerReady;
		_screen.ScreenDestroyed -= OwnerDisposed;
		_screen = null;
	}

	private void OwnerDisposed(object sender, EventArgs args) => Dispose();

	private void QueueRefresh() {
		if (_disposed || !_active || Volatile.Read(ref _pending) == 0 || _owner.IsDisposed || !_owner.IsHandleCreated
			|| Interlocked.CompareExchange(ref _queued, 1, 0) != 0)
			return;
		Tools.Exceptions.ExecuteCapturingException<InvalidOperationException>(() => _owner.BeginInvoke((Action)(async () => {
			using var completed = Tools.Scope.ExecuteOnDispose(() => {
				Interlocked.Exchange(ref _queued, 0);
				if (!_disposed && _active && !_owner.IsDisposed && _canPresent?.Invoke() != false)
					QueueRefresh();
			});
			while (!_disposed && _active && !_owner.IsDisposed && _canPresent?.Invoke() != false && Interlocked.Exchange(ref _pending, 0) != 0) {
				var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(_present);
				if (error is OperationCanceledException && (_disposed || _owner.IsDisposed))
					return;
				if (error != null)
					Tools.Exceptions.ExecuteIgnoringException(() => _reportFailure(error));
			}
		})), out var error);
		if (error != null)
			Interlocked.Exchange(ref _queued, 0);
	}
}
