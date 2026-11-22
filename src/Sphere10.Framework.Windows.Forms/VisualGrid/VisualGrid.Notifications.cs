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

// Observes the borrowed source and retains changes while presentation is inactive or editing.
// Throttle coalesces requests; this UI owner controls dispatch, admission and subscription lifetime.
public partial class VisualGrid {
	private readonly Throttle _sourceNotificationThrottle = new(TimeSpan.Zero);
	private readonly List<Control> _sourcePresentationAncestors = new();
	private IDataSource<object> _observedSource;
	private EventHandlerEx _sourceChanged;
	private ApplicationScreen _sourcePresentationScreen;
	private IApplicationScreenHost _sourcePresentationHost;
	private volatile bool _sourcePresentationActive;
	private int _pendingSourceNotification;
	private int _sourceNotificationEdits;
	private bool _sourceNotificationRefreshing;

	private bool CanPresentSourceNotification
		=> _sourcePresentationActive && !IsDisposed && !Disposing && IsHandleCreated && State == VisualState.Normal
			&& _crudCapabilities.HasFlag(DataSourceCapabilities.CanRead) && _sourceNotificationEdits == 0 && !IsPreferenceCellEditing();

	private void ObserveDataSourceNotifications(IDataSource<object> source, DataSourceCapabilities available) {
		if (!available.HasFlag(DataSourceCapabilities.CanNotify))
			return;
		_observedSource = source;
		_sourceChanged = () => {
			if (!ReferenceEquals(source, Volatile.Read(ref _observedSource)) || !source.Capabilities.HasFlag(DataSourceCapabilities.CanNotify))
				return;
			NotifyDataSourceChanged();
			Interlocked.Exchange(ref _pendingSourceNotification, 1);
			QueueDataSourceNotification();
		};
		source.Changed += _sourceChanged;
		VisibleChanged += SourcePresentationVisibleChanged;
		HandleCreated += SourcePresentationReady;
		Disposed += SourcePresentationDisposed;
		TrackSourcePresentation();
	}

	private void ReleaseDataSourceNotifications() {
		var source = _observedSource;
		_observedSource = null;
		_sourcePresentationActive = false;
		Interlocked.Exchange(ref _pendingSourceNotification, 0);
		if (source != null)
			source.Changed -= _sourceChanged;
		_sourceChanged = null;
		VisibleChanged -= SourcePresentationVisibleChanged;
		HandleCreated -= SourcePresentationReady;
		Disposed -= SourcePresentationDisposed;
		foreach (var ancestor in _sourcePresentationAncestors)
			ancestor.ParentChanged -= SourcePresentationReady;
		_sourcePresentationAncestors.Clear();
		ReleaseSourcePresentationScreen();
		if (_sourcePresentationHost != null)
			_sourcePresentationHost.ActiveScreenChanged -= SourcePresentationActiveScreenChanged;
		_sourcePresentationHost = null;
	}

	// Keep activity on the UI owner; source events may be raised by a collector thread.
	private void SourcePresentationReady(object sender, EventArgs args) => TrackSourcePresentation();

	private void SourcePresentationActiveScreenChanged(ApplicationScreen screen) => UpdateSourcePresentation();

	private void SourcePresentationVisibleChanged(object sender, EventArgs args) => UpdateSourcePresentation();

	private void SourcePresentationDisposed(object sender, EventArgs args) => ReleaseDataSourceNotifications();

	private void TrackSourcePresentation() {
		if (_observedSource == null)
			return;
		foreach (var ancestor in _sourcePresentationAncestors)
			ancestor.ParentChanged -= SourcePresentationReady;
		_sourcePresentationAncestors.Clear();
		ApplicationScreen screen = null;
		for (Control control = this; control != null; control = control.Parent) {
			_sourcePresentationAncestors.Add(control);
			control.ParentChanged += SourcePresentationReady;
			if (screen == null && control is ApplicationScreen candidate)
				screen = candidate;
		}
		if (!ReferenceEquals(screen, _sourcePresentationScreen)) {
			ReleaseSourcePresentationScreen();
			_sourcePresentationScreen = screen;
			if (screen != null) {
				screen.ScreenDisplayed += SourcePresentationReady;
				screen.ScreenDestroyed += SourcePresentationDisposed;
			}
		}
		if (!ReferenceEquals(_sourcePresentationHost, screen?.Host)) {
			if (_sourcePresentationHost != null)
				_sourcePresentationHost.ActiveScreenChanged -= SourcePresentationActiveScreenChanged;
			_sourcePresentationHost = screen?.Host;
			if (_sourcePresentationHost != null)
				_sourcePresentationHost.ActiveScreenChanged += SourcePresentationActiveScreenChanged;
		}
		UpdateSourcePresentation();
	}

	private void UpdateSourcePresentation() {
		_sourcePresentationActive = _observedSource != null && Visible
			&& (_sourcePresentationHost == null || ReferenceEquals(_sourcePresentationHost.ActiveScreen, _sourcePresentationScreen));
		if (_sourcePresentationActive)
			QueueDataSourceNotification();
	}

	private void ReleaseSourcePresentationScreen() {
		if (_sourcePresentationScreen == null)
			return;
		_sourcePresentationScreen.ScreenDisplayed -= SourcePresentationReady;
		_sourcePresentationScreen.ScreenDestroyed -= SourcePresentationDisposed;
		_sourcePresentationScreen = null;
	}

	// An explicit refresh may join a quiet read; only notification-only work defers while inactive.
	private bool DeferInactiveSourceRefresh() {
		if (!_sourceNotificationOnlyRefresh || _observedSource == null || _sourcePresentationActive)
			return false;
		Interlocked.Exchange(ref _pendingSourceNotification, 1);
		return true;
	}

	private void QueueDataSourceNotification() {
		var source = Volatile.Read(ref _observedSource);
		if (source == null || !_sourcePresentationActive || Volatile.Read(ref _pendingSourceNotification) == 0
			|| IsDisposed || Disposing || !IsHandleCreated)
			return;
		// The callback captures its failures; source events do not each retain a waiter for a continuously active drain.
		_ = _sourceNotificationThrottle.ExecuteAsync(() => PresentSourceNotificationAsync(source));
	}

	private async Task PresentSourceNotificationAsync(IDataSource<object> source) {
		var dispatchError = await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => this.InvokeAsyncEx(async _ => {
			if (!ReferenceEquals(source, _observedSource) || !CanPresentSourceNotification || Interlocked.Exchange(ref _pendingSourceNotification, 0) == 0)
				return;
			var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => RequestGridRefresh(true));
			if (error is OperationCanceledException && (IsDisposed || Disposing || !ReferenceEquals(source, _observedSource)))
				return;
			if (error != null)
				ReportSourcePresentationFailure(error, ResolveSettingsLogger());
		})).ConfigureAwait(false);
		// Handle destruction can race a queued notification. Keep the dirty state for HandleCreated replay.
		if (dispatchError != null && dispatchError is not InvalidOperationException)
			ReportSourcePresentationFailure(dispatchError, null);
	}

	private static void ReportSourcePresentationFailure(Exception error, ILogger logger)
		=> Tools.Exceptions.ExecuteIgnoringException(() => (logger ?? SystemLog.Logger).Exception(error, "Could not present changed grid data."));

	private ActionScope EnterSourceNotificationEdit() {
		_sourceNotificationEdits++;
		return Tools.Scope.ExecuteOnDispose(() => {
			_sourceNotificationEdits--;
			QueueDataSourceNotification();
		});
	}
}
