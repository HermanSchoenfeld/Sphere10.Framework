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
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>A compact sidebar outline button. Checked indicates that the sidebar is visible.</summary>
public class SidebarToggleButton : ToolStripButton {
	private const int LogicalButtonWidth = 24;
	private const int LogicalButtonHeight = 22;
	private const float LogicalIconWidth = 17;
	private const float LogicalIconHeight = 15;
	private const float LogicalCornerDiameter = 5;
	private const float LogicalDividerOffset = 6;
	private const float LogicalStrokeWidth = 1.5f;

	public SidebarToggleButton() {
		DisplayStyle = ToolStripItemDisplayStyle.None;
		CheckOnClick = true;
		AccessibleRole = AccessibleRole.CheckButton;
		Checked = true;
	}

	public override Size GetPreferredSize(Size ConstrainingSize) {
		var DpiScale = (Owner?.DeviceDpi ?? 96) / 96.0f;
		return new Size((int)Math.Round(LogicalButtonWidth * DpiScale) + Padding.Horizontal, (int)Math.Round(LogicalButtonHeight * DpiScale) + Padding.Vertical);
	}

	protected override void OnCheckedChanged(EventArgs Args) {
		Text = Checked ? "Hide sidebar" : "Show sidebar";
		AccessibleName = Text;
		base.OnCheckedChanged(Args);
	}

	protected override void OnPaint(PaintEventArgs Args) {
		base.OnPaint(Args);
		var DpiScale = (Owner?.DeviceDpi ?? 96) / 96.0f;
		var IconWidth = LogicalIconWidth * DpiScale;
		var IconHeight = LogicalIconHeight * DpiScale;
		var Bounds = new RectangleF((Width - IconWidth) / 2, (Height - IconHeight) / 2, IconWidth, IconHeight);
		using var Outline = Bounds.GetRoundPath(LogicalCornerDiameter * DpiScale);
		using var Stroke = new Pen(Enabled ? ForeColor : SystemColors.GrayText, LogicalStrokeWidth * DpiScale);
		var State = Args.Graphics.Save();
		using var Restore = Tools.Scope.ExecuteOnDispose(() => Args.Graphics.Restore(State));
		Args.Graphics.SmoothingMode = SmoothingMode.AntiAlias;
		Args.Graphics.DrawPath(Stroke, Outline);
		var Divider = Bounds.Left + LogicalDividerOffset * DpiScale;
		Args.Graphics.DrawLine(Stroke, Divider, Bounds.Top, Divider, Bounds.Bottom);
	}
}
