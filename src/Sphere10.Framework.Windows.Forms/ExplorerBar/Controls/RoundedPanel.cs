// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Windows.Forms;
using System.Drawing;
using System.Drawing.Drawing2D;


namespace Sphere10.Framework.Windows.Forms;

public partial class RoundedPanel : Panel {
	bool _drawBorder = true;
	bool _drawShadow = true;
	int _borderWidth = 1;
	int _shadowOffSet = 5;
	int _roundCornerRadius = 4;
	Point _imageLocation = new Point(4, 4);

	[Browsable(true), Category("Appearance")]
	[DefaultValue(1)]
	public int BorderWidth {
		get {
			return _borderWidth;
		}
		set {
			_borderWidth = value;
			Invalidate();
		}
	}

	[Browsable(true), Category("Appearance")]
	[DefaultValue(true)]
	public bool DrawBorder {
		get => _drawBorder;
		set => _drawBorder = value;
	}

	[Browsable(true), Category("Appearance")]
	[DefaultValue(true)]
	public bool DrawShadow {
		get => _drawShadow;
		set => _drawShadow = value;
	}

	[Browsable(true), Category("Appearance")]
	[DefaultValue(5)]
	public int ShadowOffSet {
		get {
			return _shadowOffSet;
		}
		set {
			_shadowOffSet = Math.Abs(value);
			Invalidate();
		}
	}

	[Browsable(true), Category("Appearance")]
	[DefaultValue(4)]
	public int RoundCornerRadius {
		get {
			return _roundCornerRadius;
		}
		set {
			_roundCornerRadius = Math.Abs(value);
			Invalidate();
		}
	}

	[Browsable(true), Category("Appearance")]
	public Image Image {
		get {
			return field;
		}
		set {
			field = value;
			Invalidate();
		}
	}

	[Browsable(true), Category("Appearance")]
	[DefaultValue("4,4")]
	public Point ImageLocation {
		get {
			return _imageLocation;
		}
		set {
			_imageLocation = value;
			Invalidate();
		}
	}

	[Browsable(true), Category("Appearance")]
	[DefaultValue("Color.Gray")]
	public Color BorderColor {
		get {
			return field;
		}
		set {
			field = value;
			Invalidate();
		}
	} = Color.Gray;

	[Browsable(true), Category("Appearance")]
	[DefaultValue("Color.White")]
	public Color GradientStartColor {
		get {
			return field;
		}
		set {
			field = value;
			Invalidate();
		}
	} = Color.White;

	[Browsable(true), Category("Appearance")]
	[DefaultValue("Color.Gray")]
	public Color GradientEndColor {
		get {
			return field;
		}
		set {
			field = value;
			Invalidate();
		}
	} = Color.Gray;

	public RoundedPanel() {
		this.SetStyle(ControlStyles.DoubleBuffer, true);
		this.SetStyle(ControlStyles.AllPaintingInWmPaint, true);
		this.SetStyle(ControlStyles.ResizeRedraw, true);
		this.SetStyle(ControlStyles.UserPaint, true);
		this.SetStyle(ControlStyles.SupportsTransparentBackColor, true);
		InitializeComponent();
	}

	protected override void OnPaintBackground(PaintEventArgs e) {
		base.OnPaintBackground(e);

		int tmpShadowOffSet = Math.Min(Math.Min(_shadowOffSet, this.Width - 2), this.Height - 2);
		int tmpSoundCornerRadius = Math.Min(Math.Min(_roundCornerRadius, this.Width - 2), this.Height - 2);
		if (this.Width > 1 && this.Height > 1) {
			e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;

			Rectangle rect = new Rectangle(0, 0, this.Width - tmpShadowOffSet - 1, this.Height - tmpShadowOffSet - 1);
			Rectangle rectShadow = new Rectangle(tmpShadowOffSet, tmpShadowOffSet, this.Width - tmpShadowOffSet - 1, this.Height - tmpShadowOffSet - 1);

			GraphicsPath graphPathShadow = rectShadow.GetRoundPath(tmpSoundCornerRadius);
			GraphicsPath graphPath = rect.GetRoundPath(tmpSoundCornerRadius);

			if (DrawShadow) {
				if (tmpSoundCornerRadius > 0) {
					using (PathGradientBrush gBrush = new PathGradientBrush(graphPathShadow)) {
						gBrush.WrapMode = WrapMode.Clamp;
						ColorBlend colorBlend = new ColorBlend(3);
						colorBlend.Colors = new Color[] {
							Color.Transparent,
							Color.FromArgb(180, Color.DimGray),
							Color.FromArgb(180, Color.DimGray)
						};

						colorBlend.Positions = new float[] { 0f, .1f, 1f };

						gBrush.InterpolationColors = colorBlend;
						e.Graphics.FillPath(gBrush, graphPathShadow);
					}
				}
			}

			// Draw backgroup
			LinearGradientBrush brush = new LinearGradientBrush(
				rect,
				GradientStartColor,
				GradientEndColor,
				LinearGradientMode.BackwardDiagonal
			);
			e.Graphics.FillPath(brush, graphPath);

			if (DrawBorder) {
				Color actualBorderColor = BorderColor.GetShade(-0.5f); //Color.FromArgb(180, this._borderColor);
				if (actualBorderColor.A > BorderColor.A) {
					actualBorderColor = BorderColor;
				}
				e.Graphics.DrawPath(new Pen(actualBorderColor, _borderWidth), graphPath);
			}

			// Draw Image
			if (Image != null) {
				e.Graphics.DrawImageUnscaled(Image, _imageLocation);
			}
		}
	}
}

