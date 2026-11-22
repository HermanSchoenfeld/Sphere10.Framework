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

/// <summary>One toolbar command for an operation that is either stopped or running. It shows a green start triangle while stopped and a red
/// stop square while running, each with its own caption, and requests the matching action when clicked.</summary>
/// <remarks>The owner sets <see cref="Running"/> when the operation's state changes. A click only requests the change, so a declined or
/// failed start leaves the button as it was.</remarks>
[DefaultEvent(nameof(StartRequested))]
public class ToolStripStartStopButton : ToolStripButton {
	[Description("Raised when the button is clicked while the operation is stopped")]
	[Category("Action")]
	public event EventHandlerEx StartRequested;

	[Description("Raised when the button is clicked while the operation is running")]
	[Category("Action")]
	public event EventHandlerEx StopRequested;

	private const int GlyphSize = 32;
	private readonly Bitmap _startGlyph = StartStopGlyph.Create(false, GlyphSize);
	private readonly Bitmap _stopGlyph = StartStopGlyph.Create(true, GlyphSize);
	private bool _running;
	private string _startText = "Start";
	private string _stopText = "Stop";
	private string _startToolTipText = string.Empty;
	private string _stopToolTipText = string.Empty;

	public ToolStripStartStopButton() {
		DisplayStyle = ToolStripItemDisplayStyle.ImageAndText;
		ApplyState();
	}

	/// <summary>Whether the operation is running, which makes this the stop command.</summary>
	[Category("Behavior")]
	[DefaultValue(false)]
	public bool Running {
		get => _running;
		set {
			if (_running == value)
				return;
			_running = value;
			ApplyState();
		}
	}

	/// <summary>The caption shown while the operation is stopped.</summary>
	[Category("Appearance")]
	[DefaultValue("Start")]
	public string StartText {
		get => _startText;
		set { _startText = value ?? string.Empty; ApplyState(); }
	}

	/// <summary>The caption shown while the operation is running.</summary>
	[Category("Appearance")]
	[DefaultValue("Stop")]
	public string StopText {
		get => _stopText;
		set { _stopText = value ?? string.Empty; ApplyState(); }
	}

	/// <summary>The tool tip shown while the operation is stopped.</summary>
	[Category("Appearance")]
	[DefaultValue("")]
	public string StartToolTipText {
		get => _startToolTipText;
		set { _startToolTipText = value ?? string.Empty; ApplyState(); }
	}

	/// <summary>The tool tip shown while the operation is running.</summary>
	[Category("Appearance")]
	[DefaultValue("")]
	public string StopToolTipText {
		get => _stopToolTipText;
		set { _stopToolTipText = value ?? string.Empty; ApplyState(); }
	}

	protected void NotifyStartRequested() {
		OnStartRequested();
		StartRequested?.Invoke();
	}

	protected virtual void OnStartRequested() {
	}

	protected void NotifyStopRequested() {
		OnStopRequested();
		StopRequested?.Invoke();
	}

	protected virtual void OnStopRequested() {
	}

	protected override void OnClick(EventArgs args) {
		base.OnClick(args);
		if (Running)
			NotifyStopRequested();
		else
			NotifyStartRequested();
	}

	protected override void Dispose(bool disposing) {
		if (disposing) {
			Image = null;
			_startGlyph.Dispose();
			_stopGlyph.Dispose();
		}
		base.Dispose(disposing);
	}

	// The caption, glyph and tool tip always describe what the next click requests.
	private void ApplyState() {
		Text = _running ? _stopText : _startText;
		AccessibleName = Text;
		ToolTipText = _running ? _stopToolTipText : _startToolTipText;
		Image = _running ? _stopGlyph : _startGlyph;
	}
}
