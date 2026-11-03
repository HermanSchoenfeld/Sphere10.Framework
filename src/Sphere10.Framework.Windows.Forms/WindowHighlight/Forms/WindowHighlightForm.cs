// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Native nonactivating, input-transparent frame owned by WindowHighlightScope.</summary>
internal sealed partial class WindowHighlightForm : Form {
	private Region _frameRegion;
	private int _thickness = 3;

	public WindowHighlightForm() {
		InitializeComponent();
		SetStyle(ControlStyles.Selectable, false);
	}

	public int Thickness {
		get => _thickness;
		set { _thickness = value; UpdateFrameRegion(); }
	}

	protected override bool ShowWithoutActivation => true;

	protected override CreateParams CreateParams {
		get {
			var parameters = base.CreateParams;
			// Layered + transparent passes mouse input through to windows from other threads/processes too.
			parameters.ExStyle |= WinAPI.USER32.WS_EX_NOACTIVATE | WinAPI.USER32.WS_EX_TOOLWINDOW
				| WinAPI.USER32.WS_EX_TRANSPARENT | WinAPI.USER32.WS_EX_LAYERED | WinAPI.USER32.WS_EX_TOPMOST;
			return parameters;
		}
	}

	// Keep Form.TopMost false: .NET 10 Form.SetVisibleCore explicitly focuses managed TopMost forms,
	// even with ShowWithoutActivation. Its TopMost setter also omits SWP_NOACTIVATE. Native WS_EX_TOPMOST
	// above establishes z-order without either focus path. See WinForms README's scoped outline contract.
	protected override void Select(bool directed, bool forward) {
		// This input-transparent visual has no keyboard selection or activation behavior.
	}

	protected override void OnSizeChanged(EventArgs args) {
		base.OnSizeChanged(args);
		UpdateFrameRegion();
	}

	protected override void OnDpiChanged(DpiChangedEventArgs args) {
		// The caller already supplied the destination's physical rectangle. Do not scale it a second time.
		args.Cancel = true;
		base.OnDpiChanged(args);
	}

	protected override void WndProc(ref Message message) {
		if (message.Msg == WinAPI.USER32.WM_NCHITTEST) {
			message.Result = new IntPtr(WinAPI.USER32.HTTRANSPARENT);
			return;
		}
		if (message.Msg == WinAPI.USER32.WM_MOUSEACTIVATE) {
			message.Result = new IntPtr(WinAPI.USER32.MA_NOACTIVATE);
			return;
		}
		base.WndProc(ref message);
	}

	private void UpdateFrameRegion() {
		if (ClientSize.Width <= 0 || ClientSize.Height <= 0)
			return;
		var region = new Region(ClientRectangle);
		var retained = false;
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => { if (!retained) region.Dispose(); });
		var inner = Rectangle.Inflate(ClientRectangle, -_thickness, -_thickness);
		if (inner.Width > 0 && inner.Height > 0)
			region.Exclude(inner);
		var previous = _frameRegion;
		Region = region;
		_frameRegion = region;
		retained = true;
		previous?.Dispose();
	}
}
