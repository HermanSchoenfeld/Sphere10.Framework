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
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>A passive, topmost status popup that confirms a state change near the bottom of a screen without taking focus.</summary>
/// <remarks>Each call to <see cref="ShowMessage(string)"/> replaces the text and restarts the dismissal timer. Use it on the UI thread.</remarks>
public partial class OnScreenNotificationForm : Form {

	public OnScreenNotificationForm() {
		InitializeComponent();
	}

	/// <summary>How long the message stays visible after its latest update.</summary>
	[Category("Behavior")]
	[DefaultValue(1500)]
	public int DisplayDurationMilliseconds {
		get => _dismissTimer.Interval;
		set => _dismissTimer.Interval = Math.Max(1, value);
	}

	/// <summary>The gap between the popup and the bottom of the working area, in logical pixels.</summary>
	[Category("Layout")]
	[DefaultValue(48)]
	public int BottomMargin { get; set; } = 48;

	protected override bool ShowWithoutActivation => true;

	protected override CreateParams CreateParams {
		get {
			var parameters = base.CreateParams;
			// WS_EX_NOACTIVATE, WS_EX_TRANSPARENT, WS_EX_TOOLWINDOW and WS_EX_TOPMOST keep the popup passive and above ordinary windows.
			parameters.ExStyle |= WinAPI.USER32.WS_EX_NOACTIVATE | WinAPI.USER32.WS_EX_TRANSPARENT | WinAPI.USER32.WS_EX_TOOLWINDOW | WinAPI.USER32.WS_EX_TOPMOST;
			return parameters;
		}
	}

	/// <summary>Shows the message on the screen that currently contains the pointer.</summary>
	public void ShowMessage(string message) {
		ShowMessage(message, Screen.FromPoint(Cursor.Position));
	}

	/// <summary>Shows the message centered near the bottom of the given screen's working area.</summary>
	public void ShowMessage(string message, Screen screen) {
		Guard.ArgumentNotNull(screen, nameof(screen));
		if (Tools.Runtime.IsDesignMode || IsDisposed || Disposing)
			return;
		_dismissTimer.Stop();
		_messageLabel.Text = message ?? string.Empty;
		PerformLayout();
		PositionWithinWorkingArea(screen);
		// Form.TopMost asks WinForms to activate during Show(); place the window topmost natively without activation instead.
		Show();
		PositionWithinWorkingArea(screen);
		this.ShowInactiveTopmost();
		_dismissTimer.Start();
	}

	protected override void OnVisibleChanged(EventArgs e) {
		base.OnVisibleChanged(e);
		if (!Visible)
			_dismissTimer.Stop();
	}

	private void PositionWithinWorkingArea(Screen screen) {
		var workingArea = screen.WorkingArea;
		var width = Math.Min(Width, workingArea.Width);
		var height = Math.Min(Height, workingArea.Height);
		var bottomMargin = (int)Math.Round(BottomMargin * DeviceDpi / 96.0);
		var left = workingArea.Left + (workingArea.Width - width) / 2;
		var top = Tools.Values.ClipValue(workingArea.Bottom - height - bottomMargin, workingArea.Top, workingArea.Bottom - height);
		Bounds = new Rectangle(left, top, width, height);
	}

	private void _dismissTimer_Tick(object sender, EventArgs e) {
		_dismissTimer.Stop();
		Hide();
	}
}
