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
using System.Threading;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>A passive horizontal reading guide, positioned in physical desktop pixels without taking focus or mouse input.</summary>
/// <remarks>The caller owns its lifetime. Pointer tracking uses the display clock only while the guide is visible and follows the pointer.</remarks>
public sealed partial class ReadingGuideForm : Form {
	private DisplayFrameClock _pointerClock;
	private Point _position;
	private Point _lastPointer;
	private bool _positioned;

	public ReadingGuideForm() {
		InitializeComponent();
		SetStyle(ControlStyles.Selectable, false);
	}

	/// <summary>Whether the guide moves with the pointer. Turning this off leaves it at the current position.</summary>
	[DefaultValue(true)]
	public bool FollowPointer {
		get => field;
		set {
			field = value;
			UpdateTracking();
		}
	} = true;

	/// <summary>The guide height in physical pixels, from 1 to 120.</summary>
	[DefaultValue(4)]
	public int Thickness {
		get => field;
		set {
			Guard.Argument(value >= 1 && value <= 120, nameof(value), "Guide thickness must be between 1 and 120 pixels.");
			field = value;
			if (_positioned)
				PositionGuide(_position);
		}
	} = 4;

	/// <summary>Uses a translucent highlight band instead of a solid line.</summary>
	[DefaultValue(false)]
	public bool Band {
		get => field;
		set {
			field = value;
			UpdateOpacity();
		}
	}

	/// <summary>The band's opacity from 0.1 to 1. Solid lines stay opaque.</summary>
	[DefaultValue(0.4)]
	public double BandOpacity {
		get => field;
		set {
			Guard.Argument(double.IsFinite(value) && value >= 0.1 && value <= 1, nameof(value), "Band opacity must be between 0.1 and 1.");
			field = value;
			UpdateOpacity();
		}
	} = 0.4;

	protected override bool ShowWithoutActivation => true;

	protected override CreateParams CreateParams {
		get {
			var parameters = base.CreateParams;
			parameters.ExStyle |= WinAPI.USER32.WS_EX_NOACTIVATE | WinAPI.USER32.WS_EX_TOOLWINDOW
				| WinAPI.USER32.WS_EX_TRANSPARENT | WinAPI.USER32.WS_EX_LAYERED | WinAPI.USER32.WS_EX_TOPMOST;
			return parameters;
		}
	}

	/// <summary>Shows the guide at the pointer initially, retaining a fixed position on subsequent calls.</summary>
	public void ShowGuide() {
		using var dpi = new ThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2);
		_lastPointer = Cursor.Position;
		PositionGuide(!_positioned || FollowPointer ? _lastPointer : _position);
		if (!Visible)
			Show();
		// Native creation can enlarge a very thin initial form; reapply physical bounds once its handle exists.
		PositionGuide(_position);
		this.ShowInactiveTopmost();
	}

	/// <summary>Moves the guide to a physical desktop position without changing the pointer.</summary>
	public void MoveTo(Point position) => PositionGuide(position);

	/// <summary>Moves vertically on the current display; the caller can disable pointer tracking to keep the new position.</summary>
	public void MoveVertical(int pixels) => PositionGuide(new Point(_position.X, _position.Y + pixels));

	protected override void OnVisibleChanged(EventArgs e) {
		base.OnVisibleChanged(e);
		UpdateTracking();
	}

	protected override void OnDpiChanged(DpiChangedEventArgs e) {
		// Bounds are supplied in physical desktop coordinates; WinForms must not scale them a second time.
		e.Cancel = true;
		base.OnDpiChanged(e);
	}

	protected override void Select(bool directed, bool forward) {
		// A reading guide never participates in keyboard selection.
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

	private void PositionGuide(Point position) {
		using var dpi = new ThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2);
		var screen = Screen.FromPoint(position);
		var bounds = Tools.Windows.Display.GetBounds(screen.DeviceName);
		var height = Math.Min(Thickness, bounds.Height);
		var top = (position.Y - height / 2).ClipTo(bounds.Top, bounds.Bottom - height);
		Bounds = new Rectangle(bounds.Left, top, bounds.Width, height);
		_position = new Point(position.X.ClipTo(bounds.Left, bounds.Right - 1), top + height / 2);
		_positioned = true;
	}

	private void UpdateOpacity() => Opacity = Band ? BandOpacity : 1.0;

	private void UpdateTracking() {
		if (IsDisposed || Disposing)
			return;
		if (!Visible || !FollowPointer) {
			_pointerClock?.Stop();
			return;
		}
		if (_pointerClock == null) {
			_pointerClock = new DisplayFrameClock(SynchronizationContext.Current ?? new WindowsFormsSynchronizationContext());
			_pointerClock.Frame += PointerClock_Frame;
		}
		_pointerClock.Start();
	}

	private void PointerClock_Frame() {
		using var dpi = new ThreadDpiAwarenessScope(DpiAwarenessContext.PerMonitorAwareV2);
		var position = Cursor.Position;
		if (position != _lastPointer) {
			_lastPointer = position;
			PositionGuide(position);
		}
	}
}
