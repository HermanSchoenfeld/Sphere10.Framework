// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Draws a horizontal or vertical two-tone separator.</summary>
public partial class SeparatorLine : UserControlEx {
	private System.Drawing.Drawing2D.DashStyle _dashStyle = System.Drawing.Drawing2D.DashStyle.Solid;
	private Color _firstColor = Color.FromKnownColor(KnownColor.ControlDark);
	private Color _secondColor = Color.FromKnownColor(KnownColor.ControlLightLight);
	private LineStyle _lineStyle = LineStyle.Horizontal;

	public SeparatorLine() {
		// This call is required by the Windows.Forms Form Designer.
		InitializeComponent();

		SetStyle(ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint | ControlStyles.FixedHeight | ControlStyles.FixedWidth | ControlStyles.ResizeRedraw, true);
		SetStyle(ControlStyles.Selectable, false);
		TabStop = false;
	}

	public System.Drawing.Drawing2D.DashStyle DashStyle {
		get => _dashStyle;
		set {
			_dashStyle = value;
			Invalidate();
		}
	}

	public Color FirstColor {
		get => _firstColor;
		set {
			_firstColor = value;
			Invalidate();
		}
	}

	public Color SecondColor {
		get => _secondColor;
		set {
			_secondColor = value;
			Invalidate();
		}
	}

	public LineStyle LineStyle {
		get => _lineStyle;
		set {
			_lineStyle = value;
			Size = new Size(Height, Width);
			Invalidate();
		}
	}

	/// <summary>
	/// Clean up any resources being used.
	/// </summary>
	protected override void Dispose(bool disposing) {
		if (disposing) {
			if (_components != null)
				_components.Dispose();
		}
		base.Dispose(disposing);
	}

	protected override void OnPaint(PaintEventArgs args) {
		base.OnPaint(args);

		int x1, x2, x3, x4, y1, y2, y3, y4;
		if (LineStyle == LineStyle.Horizontal) {
			x1 = 0;
			y1 = 0;
			x2 = ClientRectangle.Width;
			y2 = 0;

			x3 = 0;
			y3 = 1;
			x4 = ClientRectangle.Width;
			y4 = 1;
		} else {
			//if (LineStyle == LineStyle.Vertical)
			x1 = 0;
			y1 = 0;
			x2 = 0;
			y2 = ClientRectangle.Height;

			x3 = 1;
			y3 = 0;
			x4 = 1;
			y4 = ClientRectangle.Height;
		}

		using (var pen = new Pen(_firstColor, 1)) {
			pen.DashStyle = _dashStyle;
			args.Graphics.DrawLine(pen, x1, y1, x2, y2);
		}
		using (var pen = new Pen(_secondColor, 1)) {
			pen.DashStyle = _dashStyle;
			args.Graphics.DrawLine(pen, x3, y3, x4, y4);
		}
	}

	protected override void OnSizeChanged(EventArgs args) {
		base.OnSizeChanged(args);

		ChangeControlSize();
	}

	private void ChangeControlSize() {
		if (LineStyle == LineStyle.Horizontal)
			Height = 2;
		else if (LineStyle == LineStyle.Vertical)
			Width = 2;
	}
}
