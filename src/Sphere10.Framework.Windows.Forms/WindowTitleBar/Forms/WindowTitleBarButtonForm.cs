// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Windows.Forms;
using Sphere10.Framework.Drawing;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Native nonactivating button that WindowTitleBarButtons lays over another window's title bar.</summary>
/// <remarks>
/// The window has per-pixel transparency, so only the image and a hover highlight show over whatever the title bar paints.
/// Its surface keeps a minimal alpha, because Windows gives a layered window no mouse input where it is fully transparent.
/// </remarks>
internal sealed partial class WindowTitleBarButtonForm : PerPixelAlphaForm {
	private Image _image;
	private bool _hot;

	public WindowTitleBarButtonForm() {
		InitializeComponent();
		SetStyle(ControlStyles.Selectable, false);
	}

	/// <summary>The borrowed image drawn in the button; the caller keeps it alive while it is assigned.</summary>
	public Image Image {
		get => _image;
		set {
			if (ReferenceEquals(_image, value))
				return;
			_image = value;
			Render();
		}
	}

	public string ToolTipText {
		get => _toolTip.GetToolTip(this);
		set => _toolTip.SetToolTip(this, value ?? string.Empty);
	}

	/// <summary>Whether the button is currently shown over its window. It is shown and hidden natively, which the managed Visible state does not follow.</summary>
	public bool IsPlaced { get; private set; }

	protected override bool ShowWithoutActivation => true;

	protected override CreateParams CreateParams {
		get {
			var parameters = base.CreateParams;
			if (!DesignMode && !Tools.Runtime.IsDesignMode)
				parameters.ExStyle |= WinAPI.USER32.WS_EX_NOACTIVATE | WinAPI.USER32.WS_EX_TOOLWINDOW;
			return parameters;
		}
	}

	/// <summary>Moves the button to physical screen bounds directly above the given window in z-order, without activating anything.</summary>
	public void Place(Rectangle bounds, IntPtr insertAfter) {
		var flags = WinAPI.USER32.SetWindowPosFlags.SWP_NOACTIVATE | WinAPI.USER32.SetWindowPosFlags.SWP_NOOWNERZORDER | WinAPI.USER32.SetWindowPosFlags.SWP_SHOWWINDOW;
		if (insertAfter == Handle)
			flags |= WinAPI.USER32.SetWindowPosFlags.SWP_NOZORDER;
		// The surface survives a move; it is drawn again only when the button appears or changes size.
		var redraw = !IsPlaced || Size != bounds.Size;
		// Windows can refuse a position in the z-order beside another process's window; the button then keeps the order it has.
		IsPlaced = WinAPI.USER32.SetWindowPos(Handle, insertAfter, bounds.X, bounds.Y, bounds.Width, bounds.Height, flags)
			|| WinAPI.USER32.SetWindowPos(Handle, IntPtr.Zero, bounds.X, bounds.Y, bounds.Width, bounds.Height, flags | WinAPI.USER32.SetWindowPosFlags.SWP_NOZORDER);
		if (IsPlaced && redraw)
			Render();
	}

	/// <summary>Hides the button without activating anything.</summary>
	public void Unplace() {
		if (!IsPlaced)
			return;
		IsPlaced = false;
		WinAPI.USER32.SetWindowPos(Handle, IntPtr.Zero, 0, 0, 0, 0, WinAPI.USER32.SetWindowPosFlags.SWP_HIDEWINDOW | WinAPI.USER32.SetWindowPosFlags.SWP_NOACTIVATE
			| WinAPI.USER32.SetWindowPosFlags.SWP_NOMOVE | WinAPI.USER32.SetWindowPosFlags.SWP_NOSIZE | WinAPI.USER32.SetWindowPosFlags.SWP_NOZORDER);
	}

	protected override void Select(bool directed, bool forward) {
		// The button never takes keyboard selection or activation from the window it sits on.
	}

	protected override void OnHandleCreated(EventArgs args) {
		base.OnHandleCreated(args);
		Render();
	}

	protected override void OnMouseEnter(EventArgs args) {
		base.OnMouseEnter(args);
		_hot = true;
		Render();
	}

	protected override void OnMouseLeave(EventArgs args) {
		base.OnMouseLeave(args);
		_hot = false;
		Render();
	}

	protected override void OnDpiChanged(DpiChangedEventArgs args) {
		// The owner supplies physical bounds for the destination display. Do not scale them a second time.
		args.Cancel = true;
		base.OnDpiChanged(args);
	}

	protected override void WndProc(ref Message message) {
		if (message.Msg == WinAPI.USER32.WM_MOUSEACTIVATE) {
			message.Result = new IntPtr(WinAPI.USER32.MA_NOACTIVATE);
			return;
		}
		base.WndProc(ref message);
	}

	private void Render() {
		if (!IsHandleCreated || IsDisposed || Width <= 0 || Height <= 0 || DesignMode || Tools.Runtime.IsDesignMode)
			return;
		using var surface = new Bitmap(Width, Height, PixelFormat.Format32bppArgb);
		using (var graphics = Graphics.FromImage(surface)) {
			graphics.SmoothingMode = SmoothingMode.AntiAlias;
			graphics.InterpolationMode = InterpolationMode.HighQualityBicubic;
			graphics.Clear(Color.FromArgb(1, 128, 128, 128));
			if (_hot) {
				using var highlight = new SolidBrush(Color.FromArgb(72, 128, 128, 128));
				using var shape = Tools.Drawing.CreateRoundedRectanglePath(new Rectangle(0, 0, Width - 1, Height - 1), Math.Max(2, Width / 6));
				graphics.FillPath(highlight, shape);
			}
			if (_image != null) {
				var inset = Math.Max(1, Width / 8);
				graphics.DrawImage(_image, new Rectangle(inset, inset, Width - 2 * inset, Height - 2 * inset));
			}
		}
		SetBitmap(surface);
	}
}
