// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static Sphere10.Framework.Windows.WinAPI;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Defers layout and native drawing for a control tree until its outermost rendering scope ends.</summary>
public class ControlRenderingScope : WinFormsSyncDisposableBase, IScope {
	// Native callbacks share this registration identity while controls are added, moved or recreated.
	private const int SetRedrawMessage = 0x000B;
	private static readonly ConditionalWeakTable<Control, RenderingState> _states = new();
	private static readonly PropertyDescriptor _visibleProperty = TypeDescriptor.GetProperties(typeof(Control))[nameof(Control.Visible)];
	private readonly int _threadId;
	private readonly bool _repaintImmediately;
	private readonly List<RenderingState> _participants = new();
	private bool _ended;

	public ControlRenderingScope(Control control)
		: this(control, true) {
	}

	/// <param name="repaintImmediately">Whether to paint the final invalidated controls immediately rather than in the next native paint cycle.</param>
	public ControlRenderingScope(Control control, bool repaintImmediately) {
		Guard.ArgumentNotNull(control, nameof(control));
		Guard.Argument(!control.IsDisposed && !control.Disposing, nameof(control), "Cannot defer rendering of a disposed control.");
		Guard.Ensure(!control.InvokeRequired, "Rendering scopes must be entered on the control's UI thread.");
		_repaintImmediately = repaintImmediately;
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

	// Native controls remain on their creating UI thread, including asynchronous disposal.
	protected override void DisposeInternal() {
		OnScopeEnd();
	}

	// Release rendering suppression and complete the control tree's deferred layout.
	protected virtual void OnScopeEnd() {
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
		foreach (var participant in releasing)
			Tools.Exceptions.ExecuteCapturingException(participant.SynchronizeNativeLayout, errors);
		if (errors.Count > 0)
			ExceptionDispatchInfo.Capture(errors[0]).Throw();

		// Reparenting can move a released subtree outside the original root (for example, an undocked screen).
		// Paint its current root once, while controls still owned by another rendering scope remain suppressed.
		var repainting = new HashSet<Control>(releasing.Select(participant => participant.Control)
			.Where(control => !control.IsDisposed && !control.Disposing && !IsSuppressed(control)), ReferenceEqualityComparer.Instance);
		foreach (var control in repainting) {
			var ancestor = control.Parent;
			while (ancestor != null && !repainting.Contains(ancestor))
				ancestor = ancestor.Parent;
			if (ancestor != null)
				continue;
			control.Invalidate(true);
			if (_repaintImmediately)
				control.Update();
		}
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

	#region Inner Types

	// Track the scopes sharing one control's native redraw and layout suppression.
	private sealed class RenderingState {
		private readonly HashSet<ControlRenderingScope> _scopes = new(ReferenceEqualityComparer.Instance);
		private IntPtr _suppressedHandle;
		private bool _layoutResumed;

		public RenderingState(Control control) {
			Control = control;
		}

		public Control Control { get; }

		public bool IsActive => _scopes.Count > 0;

		public bool Enter(ControlRenderingScope scope) {
			if (!_scopes.Add(scope))
				return false;
			if (_scopes.Count > 1)
				return true;
			_layoutResumed = false;
			Control.SuspendLayout();
			Control.HandleCreated += HandleCreated;
			Control.ControlAdded += ControlAdded;
			Control.VisibleChanged += VisibleChanged;
			SuppressRedraw();
			return true;
		}

		public bool Leave(ControlRenderingScope scope) {
			if (!_scopes.Remove(scope) || IsActive)
				return false;
			Control.HandleCreated -= HandleCreated;
			Control.ControlAdded -= ControlAdded;
			Control.VisibleChanged -= VisibleChanged;
			return true;
		}

		public void ResumeLayout() {
			if (Control.IsDisposed || Control.Disposing)
				return;
			Control.ResumeLayout(true);
			_layoutResumed = true;
		}

		public void Restore() {
			if (Control.IsDisposed || Control.Disposing)
				return;
			using var redraw = Tools.Scope.ExecuteOnDispose(RestoreRedraw);
			if (!_layoutResumed) {
				_layoutResumed = true;
				Control.ResumeLayout(false);
			}
		}

		public void SynchronizeNativeLayout() {
			if (Control is not TabControl { IsDisposed: false, Disposing: false, IsHandleCreated: true, Alignment: TabAlignment.Top, SelectedTab: { } page } tabs)
				return;
			if (!tabs.Visible || IsSuppressed(tabs) || _visibleProperty.ShouldSerializeValue(page))
				return;
			var native = new RECT { Left = 0, Top = 0, Right = tabs.ClientSize.Width, Bottom = tabs.ClientSize.Height };
			USER32.SendMessage(new HandleRef(tabs, tabs.Handle), COMCTL32.TCM_ADJUSTRECT, IntPtr.Zero, ref native);
			if (tabs.DisplayRectangle == Rectangle.FromLTRB(native.Left, native.Top, native.Right, native.Bottom))
				return;
			// WinForms can cache DisplayRectangle in OnResize before comctl32 handles WM_SIZE. Reapply the existing
			// nonvirtual image metadata to refresh that parent cache, preserving the key/index mode and custom Text setters.
			if (!string.IsNullOrEmpty(page.ImageKey))
				page.ImageKey = page.ImageKey;
			else
				page.ImageIndex = page.ImageIndex;
		}

		private void RestoreRedraw() {
			if (Control.IsDisposed || Control.Disposing)
				return;
			if (_suppressedHandle != IntPtr.Zero && Control.IsHandleCreated && Control.Handle == _suppressedHandle) {
				// The base designer descriptor observes the control's own visibility request, including under a hidden parent.
				var visible = !_visibleProperty.ShouldSerializeValue(Control);
				Tools.Windows.Win32.SendMessage(new HandleRef(Control, _suppressedHandle), SetRedrawMessage, 1, 0);
				// WM_SETREDRAW enables WS_VISIBLE. Preserve an explicit Hide without hiding children of a retained tab.
				if (!visible)
					USER32.ShowWindow(_suppressedHandle, USER32.ShowWindowCommands.Hide);
			}
			_suppressedHandle = IntPtr.Zero;
		}

		private void SuppressRedraw() {
			if (!Control.IsHandleCreated || !Control.Visible)
				return;
			if (_suppressedHandle == Control.Handle)
				return;
			_suppressedHandle = Control.Handle;
			Tools.Windows.Win32.SendMessage(new HandleRef(Control, _suppressedHandle), SetRedrawMessage, 0, 0);
		}

		private void HandleCreated(object sender, EventArgs args) {
			SuppressRedraw();
		}

		private void VisibleChanged(object sender, EventArgs args) {
			SuppressRedraw();
		}

		private void ControlAdded(object sender, ControlEventArgs args) {
			foreach (var scope in _scopes.ToArray())
				scope.Include(args.Control);
		}
	}

	#endregion
}
