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
using Sphere10.Framework.Drawing;

namespace Sphere10.Framework.Windows.Forms.Drawing.VisualElements;

[Serializable]
public class Header : HeaderBase {
	private Color _backColor;
	private RectangleBorder _border;
	private float _gradientAngle = 45;
	private BackgroundColorStyle _backgroundColorStyle = BackgroundColorStyle.Linear;
	private BackgroundLinearGradient _background;

	/// <summary>
	/// Default constructor
	/// </summary>
	public Header()
		: this(45) {
	}

	public Header(float gradientAngle) {
		GradientAngle = gradientAngle;
		_background = new BackgroundLinearGradient(Color.Empty, Color.Empty, GradientAngle);
		BackColor = Color.FromKnownColor(KnownColor.Control);
		var darkControlColor = Tools.Drawing.CalculateLightDarkColor(BackColor, -0.2f);
		var darkBorderLine = new BorderLine(darkControlColor, 1);
		_border = new RectangleBorder(darkBorderLine, darkBorderLine);
	}

	/// <summary>
	/// Copy constructor
	/// </summary>
	/// <param name="other"></param>
	public Header(Header other)
		: base(other) {
		BackColor = other.BackColor;
		Border = other.Border;
		GradientAngle = other.GradientAngle;
	}

	/// <summary>
	/// Back Color
	/// </summary>
	public Color BackColor {
		get => _backColor;
		set => _backColor = value;
	}

	/// <summary>
	/// Border
	/// </summary>
	public RectangleBorder Border {
		get => _border;
		set => _border = value;
	}

	/// <summary>
	/// Gradient angle used for linear gradient.
	/// </summary>
	public float GradientAngle {
		get => _gradientAngle;
		set => _gradientAngle = value;
	}

	/// <summary>
	/// Draw mode for the header. Default is Linear.
	/// </summary>
	public BackgroundColorStyle BackgroundColorStyle {
		get => _backgroundColorStyle;
		set => _backgroundColorStyle = value;
	}

	/// <summary>
	/// Clone
	/// </summary>
	/// <returns></returns>
	public override object Clone() {
		return new Header(this);
	}

	/// <summary>
	/// Calculate the client area where the content can be drawed, usually removing the area used by the background, for example removing a border.
	/// </summary>
	/// <returns></returns>
	public override RectangleF GetBackgroundContentRectangle(GraphicsMeasurement measure, RectangleF backGroundArea) {
		backGroundArea = _border.GetContentRectangle(backGroundArea);
		return base.GetBackgroundContentRectangle(measure, backGroundArea);
	}

	/// <summary>
	/// Calculate the total area used by the backgound and the content, adding the background area to the content area.
	/// </summary>
	/// <returns></returns>
	public override SizeF GetBackgroundExtent(GraphicsMeasurement measure, SizeF contentSize) {
		var extend = base.GetBackgroundExtent(measure, contentSize);
		extend = _border.GetExtent(contentSize);
		return extend;
	}

	protected override void OnDraw(GraphicsCache graphics, RectangleF area) {
		//The background is drawed on the background (back)
		OnDrawBackground(graphics, area);
		//The border is drawed on the background
		OnDrawBorder(graphics, area);
	}

	protected virtual void OnDrawBorder(GraphicsCache graphics, RectangleF area) {
		_border.Draw(graphics, area);
	}

	protected virtual void OnDrawBackground(GraphicsCache graphics, RectangleF area) {
		var darkControlColor = Tools.Drawing.CalculateLightDarkColor(BackColor, -0.2f);
		//Color darkControl = Tools.DrawingTool.CalculateLightDarkColor(BackColor, -0.1f);
		var lightControl = Tools.Drawing.CalculateLightDarkColor(BackColor, 0.5f);
		var hotLightControl = Tools.Drawing.CalculateMiddleColor(Color.FromKnownColor(KnownColor.Highlight), lightControl);
		if (Style == ControlDrawStyle.Hot) {
			_background.FirstColor = hotLightControl;
			_background.SecondColor = hotLightControl;
		} else if (Style == ControlDrawStyle.Pressed) {
			_background.FirstColor = darkControlColor;
			_background.SecondColor = lightControl;
		} else { //Normal or Disabled
			if (BackgroundColorStyle == BackgroundColorStyle.Linear) {
				_background.FirstColor = lightControl;
				_background.SecondColor = darkControlColor;
			} else if (BackgroundColorStyle == BackgroundColorStyle.Solid) {
				_background.FirstColor = BackColor;
				_background.SecondColor = BackColor;
			} else {
				_background.FirstColor = Color.Empty;
				_background.SecondColor = Color.Empty;
			}
		}

		_background.Angle = GradientAngle;
		_background.Draw(graphics, area);
	}
}
