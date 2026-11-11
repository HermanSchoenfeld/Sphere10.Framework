// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>One desktop-window selection using the existing input hooks and verified red outline. Native queries run on workers; presentation belongs to the supplied UI control.</summary>
public sealed class WindowPicker : WinFormsSyncDisposableBase {
	private readonly Control _dispatcher;
	private readonly int _ownerThread = Environment.CurrentManagedThreadId;
	private readonly IMouseHook _mouse;
	private readonly IKeyboardHook _keyboard;
	private readonly Func<Point?, CancellationToken, Task<WindowsWindowTarget>> _read;
	private readonly CancellationTokenSource _queries = new();
	private WindowHighlightScope _highlight;
	private TaskCompletionSource<WindowsWindowTarget> _completion;
	private Task _query = Task.CompletedTask;
	private Point? _requestedPoint;
	private bool _requested;
	private bool _select;
	private bool _started;
	private bool _running;
	private bool _released;
	private bool _hooksReleased;
	private bool _leftDown;
	private bool _cancelPending;
	private IScope _hidden;

	/// <summary>Creates a caller-owned one-use picker. The dispatcher must retain its native handle while selection is active.</summary>
	public WindowPicker(Control dispatcher)
		: this(dispatcher, Tools.Windows.Hooks.CreateMouseHook(new NoOpActiveApplicationMonitor()), Tools.Windows.Hooks.CreateKeyboardHook(new NoOpActiveApplicationMonitor()),
			(point, token) => point.HasValue ? Tools.Windows.Windowing.ReadAtPhysicalPointAsync(point.Value, token) : Tools.Windows.Windowing.ReadAtPointerAsync(token)) {
	}

	/// <summary>Creates a picker owning supplied input hooks and using a caller-supplied worker query. The picker disposes both hooks; the UI dispatcher remains borrowed.</summary>
	public WindowPicker(Control dispatcher, IMouseHook mouse, IKeyboardHook keyboard, Func<Point?, CancellationToken, Task<WindowsWindowTarget>> read) {
		Guard.ArgumentNotNull(dispatcher, nameof(dispatcher));
		Guard.ArgumentNotNull(mouse, nameof(mouse));
		Guard.ArgumentNotNull(keyboard, nameof(keyboard));
		Guard.ArgumentNotNull(read, nameof(read));
		Guard.Ensure(!dispatcher.InvokeRequired && dispatcher.IsHandleCreated, "Create a desktop picker on its dispatcher's native UI thread.");
		_dispatcher = dispatcher;
		_mouse = mouse;
		_keyboard = keyboard;
		_read = read;
	}

	/// <summary>Highlights the window under the pointer; left-button release accepts, right-click or Escape cancels. A supplied main form alone is hidden and restored.</summary>
	/// <remarks>The selecting primary press/release are consumed by the existing input hook, so choosing a window does not click its controls. No additional click or target code is dispatched.</remarks>
	public async Task<WindowsWindowTarget> PickAsync(Form hideForm = null, CancellationToken cancellationToken = default) {
		VerifyThread();
		Guard.Ensure(!_started && !_released, "A desktop picker owns one selection lifetime.");
		_started = true;
		_running = true;
		_completion = new TaskCompletionSource<WindowsWindowTarget>(TaskCreationOptions.RunContinuationsAsynchronously);
		_mouse.Activity += MouseActivity;
		_mouse.Click += MouseClick;
		_mouse.ShouldIntercept = InterceptMouse;
		_keyboard.KeyDown += KeyDown;
		_keyboard.ShouldIntercept = (key, _) => key == Key.Escape;
		_dispatcher.Disposed += DispatcherDisposed;
		if (hideForm != null)
			hideForm.FormClosing += HiddenFormClosing;
		using var cancellation = cancellationToken.Register(() => Post(Cancel));
		try {
			cancellationToken.ThrowIfCancellationRequested();
			_hidden = HideMainForm(hideForm);
			_mouse.StartHook();
			_keyboard.StartHook();
			Request(null, false);
			return await _completion.Task;
		} finally {
			_running = false;
			_queries.Cancel();
			ReleaseHooks();
			_highlight?.Dispose();
			_highlight = null;
			_hidden?.Dispose();
			_hidden = null;
			if (hideForm != null)
				hideForm.FormClosing -= HiddenFormClosing;
			await _query;
		}
	}

	/// <summary>Cancels selection on the UI owner without changing the target.</summary>
	public void Cancel() {
		VerifyThread();
		_hidden?.Dispose();
		_hidden = null;
		_highlight?.Dispose();
		_highlight = null;
		_cancelPending = true;
		_queries.Cancel();
		if (!_leftDown)
			_completion?.TrySetResult(null);
	}

	private void MouseActivity(object sender, MouseEvent args) {
		var point = new Point(args.X, args.Y);
		Post(() => Request(point, false));
	}

	private void MouseClick(object sender, MouseClickEvent args) {
		if (args.ButtonState != MouseButtonState.Up)
			return;
		if (args.Buttons == MouseButton.Right)
			Post(Cancel);
		else if (args.Buttons == MouseButton.Left)
			Post(() => Request(new Point(args.X, args.Y), true));
	}

	// Native hook callbacks run on the installing UI thread. Retain a consumed DOWN until its UP, even if Escape requested cancellation meanwhile.
	private bool InterceptMouse(MouseButton button, MouseButtonState state) {
		if (button != MouseButton.Left || !_running)
			return false;
		if (state == MouseButtonState.Down) {
			if (_cancelPending)
				return false;
			_leftDown = true;
			return true;
		}
		if (state != MouseButtonState.Up || !_leftDown)
			return false;
		_leftDown = false;
		if (_cancelPending)
			_completion.TrySetResult(null);
		return true;
	}

	private void KeyDown(object sender, KeyEvent args) {
		if (args.Key == Key.Escape)
			Post(Cancel);
	}

	private void DispatcherDisposed(object sender, EventArgs args) => Cancel();

	private void HiddenFormClosing(object sender, FormClosingEventArgs args) => Cancel();

	// Coalesce pointer motion behind one read. Once a click is queued, later motion must not retarget that click.
	private void Request(Point? point, bool select) {
		if (!_running || _cancelPending || _completion.Task.IsCompleted || _select)
			return;
		_requestedPoint = point;
		_select = select;
		_requested = true;
		if (_query.IsCompleted)
			_query = ReadRequestedAsync();
	}

	private async Task ReadRequestedAsync() {
		while (_running && _requested && !_completion.Task.IsCompleted) {
			_requested = false;
			var point = _requestedPoint;
			var select = _select;
			WindowsWindowTarget target = null;
			var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync(async () => {
				target = await Task.Run(() => _read(point, _queries.Token), _queries.Token);
			});
			if (!_running || _queries.IsCancellationRequested || _completion.Task.IsCompleted)
				return;
			if (error != null && (select || error is not (Win32Exception or InvalidOperationException or ArgumentException))) {
				_completion.TrySetException(error);
				return;
			}
			if (!select && _requested)
				continue;
			Tools.Exceptions.ExecuteCapturingException(() => {
				if (target?.Bounds is { Width: > 0, Height: > 0 } bounds) {
					if (_highlight == null)
						_highlight = new WindowHighlightScope(bounds, Color.Red, 6);
					else if (_highlight.Bounds != bounds)
						_highlight.Bounds = bounds;
				} else {
					_highlight?.Dispose();
					_highlight = null;
				}
			}, out var presentationError);
			if (presentationError != null) {
				_completion.TrySetException(presentationError);
				return;
			}
			if (select)
				_completion.TrySetResult(target);
		}
	}

	private void Post(Action action) {
		if (!_running || _dispatcher.IsDisposed || !_dispatcher.IsHandleCreated)
			return;
		Tools.Exceptions.ExecuteCapturingException<InvalidOperationException>(() => _dispatcher.BeginInvoke((Action)(() => {
			if (_running && !_dispatcher.IsDisposed)
				action();
		})), out _, _ => _dispatcher.IsDisposed || !_dispatcher.IsHandleCreated);
	}

	private IScope HideMainForm(Form form) {
		if (form == null || form.IsDisposed)
			return Tools.Scope.ExecuteOnDispose(() => { });
		var visible = form.Visible;
		// Owned detached windows would otherwise follow the hidden owner. Borrow their ownership only for this scope.
		var owned = form.OwnedForms.Where(window => !window.IsDisposed).ToArray();
		foreach (var window in owned)
			window.Owner = null;
		if (visible)
			form.Hide();
		return Tools.Scope.ExecuteOnDispose(() => {
			if (form.IsDisposed || form.Disposing)
				return;
			if (visible)
				form.Show();
			foreach (var window in owned)
				if (!window.IsDisposed && window.Owner == null)
					window.Owner = form;
		});
	}

	private void ReleaseHooks() {
		if (_hooksReleased)
			return;
		_hooksReleased = true;
		_mouse.Activity -= MouseActivity;
		_mouse.Click -= MouseClick;
		_mouse.ShouldIntercept = null;
		_keyboard.KeyDown -= KeyDown;
		_keyboard.ShouldIntercept = null;
		_dispatcher.Disposed -= DispatcherDisposed;
		_mouse.Dispose();
		_keyboard.Dispose();
	}

	protected override void DisposeInternal() {
		VerifyThread();
		if (_released)
			return;
		_released = true;
		Cancel();
		_completion?.TrySetResult(null);
		_running = false;
		_queries.Cancel();
		ReleaseHooks();
		_highlight?.Dispose();
		_highlight = null;
		_hidden?.Dispose();
		_hidden = null;
		_queries.Dispose();
	}

	private void VerifyThread()
		=> Guard.Ensure(Environment.CurrentManagedThreadId == _ownerThread, "A desktop picker's native lifetime belongs to its creating UI thread.");
}
