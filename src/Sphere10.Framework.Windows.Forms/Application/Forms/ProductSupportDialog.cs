// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>
/// Base of the feature-request, comment and bug-report dialogs. It lays the dialog out in code at the current DPI: the banner across
/// the top, the description group, then each section group sized to its stacked controls, and the loading circle, Send and Cancel on
/// one row beneath. Designer anchors do not survive a DPI change (a dialog created on one monitor and shown on another of another
/// DPI lost its layout), so no anchor or designer coordinate decides the final layout.
/// </summary>
public class ProductSupportDialog : ApplicationForm {
	private const int LogicalClientWidth = 552;
	private const int LogicalDescriptionHeight = 147;
	private Control _banner;
	private GroupBox _description;
	private GroupBox[] _sections = [];
	private Button _sendButton;
	private Button _cancelButton;
	private LoadingCircle _loadingCircle;
	private bool _layoutInitialized;
	private bool _arranging;
	private bool _shown;

	/// <summary>Names the controls the layout arranges; a derived dialog calls it after <c>InitializeComponent</c>.</summary>
	protected void InitializeSupportLayout(Control banner, GroupBox description, GroupBox[] sections, Button sendButton, Button cancelButton, LoadingCircle loadingCircle) {
		Guard.ArgumentNotNull(banner, nameof(banner));
		Guard.ArgumentNotNull(description, nameof(description));
		Guard.ArgumentNotNull(sections, nameof(sections));
		Guard.ArgumentNotNull(sendButton, nameof(sendButton));
		Guard.ArgumentNotNull(cancelButton, nameof(cancelButton));
		Guard.ArgumentNotNull(loadingCircle, nameof(loadingCircle));
		_banner = banner;
		_description = description;
		_sections = sections;
		_sendButton = sendButton;
		_cancelButton = cancelButton;
		_loadingCircle = loadingCircle;
		_banner.SizeChanged += BannerSizeChanged;
	}

	// Laid out before Form.OnLoad centres the dialog on its owner.
	protected override void OnLoad(EventArgs e) {
		if (_banner != null && !DesignMode && !Tools.Runtime.IsDesignMode) {
			_layoutInitialized = true;
			ArrangeContent();
		}
		base.OnLoad(e);
	}

	protected override void OnShown(EventArgs e) {
		_shown = true;
		base.OnShown(e);
	}

	// WinForms rescales the designer geometry when the DPI changes; lay the content out again at the new DPI.
	protected override void OnLayout(LayoutEventArgs levent) {
		base.OnLayout(levent);
		if (_layoutInitialized && !_arranging && !Disposing && !IsDisposed)
			ArrangeContent();
	}

	private void ArrangeContent() {
		if (_arranging || Disposing || IsDisposed)
			return;
		_arranging = true;
		SuspendLayout();
		using var layoutState = Tools.Scope.ExecuteOnDispose(() => {
			ResumeLayout(true);
			_arranging = false;
		});
		var margin = LogicalToDeviceUnits(12);
		var gap = LogicalToDeviceUnits(6);
		var inset = LogicalToDeviceUnits(8);
		var buttonGap = LogicalToDeviceUnits(6);
		// The width follows the DPI alone, so a dialog created on one monitor and moved to another matches one created there.
		var width = LogicalToDeviceUnits(LogicalClientWidth);
		var groupWidth = width - margin * 2;
		_banner.Bounds = new Rectangle(0, 0, width, _banner.Height);

		_description.Bounds = new Rectangle(margin, _banner.Bottom + gap, groupWidth, LogicalToDeviceUnits(LogicalDescriptionHeight));
		var text = _description.DisplayRectangle;
		foreach (Control child in _description.Controls)
			child.Bounds = new Rectangle(text.Left + inset / 2, text.Top, text.Width - inset, text.Height - inset / 2);

		var top = _description.Bottom + gap;
		foreach (var section in _sections) {
			section.Bounds = new Rectangle(margin, top, groupWidth, section.Height);
			var content = section.DisplayRectangle;
			var y = content.Top;
			foreach (var child in section.Controls.Cast<Control>().OrderBy(child => child.Top).ToArray()) {
				child.Bounds = new Rectangle(content.Left + inset, y, content.Width - inset * 2, child.Height);
				y = child.Bottom + gap;
			}
			// The caption and padding around the content stay as the group box computes them.
			section.Height = y - gap + (section.Height - content.Bottom) + gap;
			top = section.Bottom + gap;
		}

		var rowTop = top + gap;
		var rowHeight = Math.Max(_sendButton.Height, _cancelButton.Height);
		_cancelButton.Location = new Point(width - margin - _cancelButton.Width, rowTop);
		_sendButton.Location = new Point(_cancelButton.Left - buttonGap - _sendButton.Width, rowTop + (rowHeight - _sendButton.Height) / 2);
		_loadingCircle.Location = new Point(_sendButton.Left - buttonGap - _loadingCircle.Width, rowTop + (rowHeight - _loadingCircle.Height) / 2);
		ClientSize = new Size(width, rowTop + rowHeight + margin);
		// A form takes its monitor's DPI only when WM_DPICHANGED arrives, after Form.OnLoad centred it at the system DPI's size.
		// Until the dialog is shown, keep it centred as its size settles.
		if (!_shown && IsHandleCreated) {
			if (StartPosition == FormStartPosition.CenterParent)
				CenterToParent();
			else if (StartPosition == FormStartPosition.CenterScreen)
				CenterToScreen();
		}
	}

	// A late WinForms rescale can resize the banner after the content was arranged; keep it spanning the dialog.
	private void BannerSizeChanged(object sender, EventArgs e) {
		if (_layoutInitialized && !_arranging && _banner.Width != ClientSize.Width)
			ArrangeContent();
	}
}
