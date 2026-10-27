// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Threading;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Owns a hollow, click-through, nonactivating frame over caller-supplied physical desktop bounds. Create, update and dispose on its STA UI thread.</summary>
/// <remarks>The caller owns target verification, color, thickness and lifetime. No drawing is performed in another window's device context.</remarks>
public sealed class WindowHighlightScope : WinFormsSyncDisposableBase, IScope {
	// The frame, its mutable bounds and UI-thread lifetime keep one shared owner.
	private readonly WindowHighlightForm _frame;
	private readonly int _threadId;

	public WindowHighlightScope(Rectangle bounds, Color color, int thickness = 3) {
		Guard.Argument(bounds.Width > 0 && bounds.Height > 0, nameof(bounds), "Supply nonempty screen-coordinate bounds.");
		Guard.Argument(thickness > 0 && thickness <= 64, nameof(thickness), "Frame thickness must be 1 to 64 pixels.");
		Guard.Ensure(Thread.CurrentThread.GetApartmentState() == ApartmentState.STA, "Highlight frames require an STA UI thread.");
		_threadId = Environment.CurrentManagedThreadId;
		using var dpi = new ThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2);
		_frame = new WindowHighlightForm();
		var retained = false;
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => { if (!retained) _frame.Dispose(); });
		_frame.BackColor = color;
		_frame.Thickness = thickness;
		_frame.Bounds = bounds;
		_frame.Show();
		retained = true;
	}

	public Rectangle Bounds {
		get {
			VerifyThread();
			using var dpi = new ThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2);
			return _frame.Bounds;
		}
		set {
			VerifyThread();
			Guard.Argument(value.Width > 0 && value.Height > 0, nameof(value), "Supply nonempty screen-coordinate bounds.");
			using var dpi = new ThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2);
			if (!_frame.IsDisposed)
				_frame.Bounds = value;
		}
	}

	protected override void FreeManagedResources() {
		VerifyThread();
		using var dpi = new ThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2);
		_frame.Dispose();
	}

	private void VerifyThread() => Guard.Ensure(Environment.CurrentManagedThreadId == _threadId, "Highlight frames belong to their creating UI thread.");
}
