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
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;
using static Sphere10.Framework.Windows.WinAPI;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Defers layout and native drawing for a control tree until its outermost rendering scope ends.</summary>
public class ControlRenderingScope : ScopeBase {
	private const int SetRedrawMessage = 0x000B;
	private static readonly ConditionalWeakTable<Control, RenderingState> _states = new();
	private readonly Control _control;
	private readonly int _threadId;
	private readonly List<RenderingState> _participants = new();
	private bool _ended;

	public ControlRenderingScope(Control control) {
		Guard.ArgumentNotNull(control, nameof(control));
		Guard.Argument(!control.IsDisposed && !control.Disposing, nameof(control), "Cannot defer rendering of a disposed control.");
		Guard.Ensure(!control.InvokeRequired, "Rendering scopes must be entered on the control's UI thread.");
		_control = control;
		_threadId = Environment.CurrentManagedThreadId;
		var entered = false;
		using var rollback = Tools.Scope.ExecuteOnDispose(() => {
			if (entered)
				return;
			_ended = true;
			foreach (var participant in _participants)
				Tools.Exceptions.ExecuteIgnoringException(() => {
					if (participant.Leave(this))
						participant.Restore();
				});
		});
		Include(control);
		entered = true;
	}

	protected override void OnScopeEnd() {
		Guard.Ensure(Environment.CurrentManagedThreadId == _threadId, "Rendering scopes must be disposed on their creating UI thread.");
		if (_ended)
			return;
		_ended = true;
		var releasing = _participants.Where(participant => participant.Leave(this)).ToArray();
		var errors = new List<Exception>();
		// Resume parents before their children, and restore every participant even when a custom control fails.
		foreach (var participant in releasing)
			Tools.Exceptions.ExecuteCapturingException(participant.ResumeLayout, errors);
		foreach (var participant in releasing)
			Tools.Exceptions.ExecuteCapturingException(participant.Restore, errors);
		if (errors.Count > 0)
			ExceptionDispatchInfo.Capture(errors[0]).Throw();
		if (_control.IsDisposed || _control.Disposing || IsSuppressed(_control))
			return;
		_control.Invalidate(true);
		_control.Update();
	}

	protected override ValueTask OnScopeEndAsync() {
		// Native controls must remain on the caller's UI thread, including asynchronous disposal.
		OnScopeEnd();
		return ValueTask.CompletedTask;
	}

	protected override ValueTask FreeManagedResourcesAsync() {
		// ScopeBase's general async completion dispatches ScopeEnd on a worker; native rendering remains on the UI thread.
		FreeManagedResources();
		return ValueTask.CompletedTask;
	}

	private void Include(Control control) {
		if (control.IsDisposed || control.Disposing)
			return;
		var state = _states.GetValue(control, item => new RenderingState(item));
		if (_participants.Contains(state))
			return;
		_participants.Add(state);
		state.Enter(this);
		foreach (Control child in control.Controls)
			Include(child);
	}

	private static bool IsSuppressed(Control control) {
		for (var current = control; current != null; current = current.Parent)
			if (_states.TryGetValue(current, out var state) && state.IsActive)
				return true;
		return false;
	}

	private sealed class RenderingState {
		private readonly Control _control;
		private readonly HashSet<ControlRenderingScope> _scopes = new(ReferenceEqualityComparer.Instance);
		private IntPtr _suppressedHandle;
		private bool _layoutResumed;

		public RenderingState(Control control) => _control = control;

		public bool IsActive => _scopes.Count > 0;

		public bool Enter(ControlRenderingScope scope) {
			if (!_scopes.Add(scope))
				return false;
			if (_scopes.Count > 1)
				return true;
			_layoutResumed = false;
			_control.SuspendLayout();
			_control.HandleCreated += HandleCreated;
			_control.ControlAdded += ControlAdded;
			_control.VisibleChanged += VisibleChanged;
			SuppressRedraw();
			return true;
		}

		public bool Leave(ControlRenderingScope scope) {
			if (!_scopes.Remove(scope) || IsActive)
				return false;
			_control.HandleCreated -= HandleCreated;
			_control.ControlAdded -= ControlAdded;
			_control.VisibleChanged -= VisibleChanged;
			return true;
		}

		public void ResumeLayout() {
			if (_control.IsDisposed || _control.Disposing)
				return;
			_control.ResumeLayout(true);
			_layoutResumed = true;
		}

		public void Restore() {
			if (_control.IsDisposed || _control.Disposing)
				return;
			using var redraw = Tools.Scope.ExecuteOnDispose(RestoreRedraw);
			if (!_layoutResumed) {
				_layoutResumed = true;
				_control.ResumeLayout(false);
			}
		}

		private void RestoreRedraw() {
			if (_control.IsDisposed || _control.Disposing)
				return;
			if (_suppressedHandle != IntPtr.Zero && _control.IsHandleCreated && _control.Handle == _suppressedHandle) {
				var visible = _control.Visible;
				Tools.Windows.Win32.SendMessage(new HandleRef(_control, _suppressedHandle), SetRedrawMessage, 1, 0);
				// WM_SETREDRAW enables WS_VISIBLE. Preserve controls intentionally hidden during the transition.
				if (!visible)
					USER32.ShowWindow(_suppressedHandle, USER32.ShowWindowCommands.Hide);
			}
			_suppressedHandle = IntPtr.Zero;
		}

		private void SuppressRedraw() {
			if (!_control.IsHandleCreated || !_control.Visible)
				return;
			if (_suppressedHandle == _control.Handle)
				return;
			_suppressedHandle = _control.Handle;
			Tools.Windows.Win32.SendMessage(new HandleRef(_control, _suppressedHandle), SetRedrawMessage, 0, 0);
		}

		private void HandleCreated(object sender, EventArgs args) => SuppressRedraw();

		private void VisibleChanged(object sender, EventArgs args) => SuppressRedraw();

		private void ControlAdded(object sender, ControlEventArgs args) {
			foreach (var scope in _scopes.ToArray())
				scope.Include(args.Control);
		}
	}
}
