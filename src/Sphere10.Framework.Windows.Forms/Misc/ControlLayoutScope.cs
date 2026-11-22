// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Defers layout of a control tree until the scope ends, then lays it out once. Painting is not suspended, so only the controls
/// whose content changed repaint. Use it for frequent updates of many auto-sized controls (such as labels in nested tables), where each
/// change would otherwise lay out every ancestor; use <see cref="ControlRenderingScope"/> when intermediate frames must not be seen.</summary>
public sealed class ControlLayoutScope : WinFormsSyncDisposableBase, IScope {
	private readonly int _threadId;
	private readonly List<Control> _suspended = new();

	public ControlLayoutScope(Control control) {
		Guard.ArgumentNotNull(control, nameof(control));
		Guard.Ensure(!control.InvokeRequired, "Layout scopes must be entered on the control's UI thread.");
		_threadId = Environment.CurrentManagedThreadId;
		Suspend(control);
	}

	// Children resume before their parents: each child lays out while its parent still defers, and the parent then lays out once with its
	// children's final sizes.
	protected override void DisposeInternal() {
		Guard.Ensure(Environment.CurrentManagedThreadId == _threadId, "Layout scopes must be disposed on their creating UI thread.");
		for (var index = _suspended.Count - 1; index >= 0; index--) {
			var control = _suspended[index];
			if (!control.IsDisposed && !control.Disposing)
				control.ResumeLayout(true);
		}
		_suspended.Clear();
	}

	private void Suspend(Control control) {
		if (control.IsDisposed || control.Disposing)
			return;
		control.SuspendLayout();
		_suspended.Add(control);
		foreach (Control child in control.Controls)
			Suspend(child);
	}
}
