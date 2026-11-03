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
using System.Windows.Forms.VisualStyles;
using Sphere10.Framework.Drawing;

namespace Sphere10.Framework.Windows.Forms.Drawing.VisualElements;

[Serializable]
public class ButtonThemed : ButtonBase {
	/// <summary>
	/// Standard button used when the XP style are disabled.
	/// </summary>
	private Button _standardButton = new Button();

	/// <summary>
	/// Default constructor
	/// </summary>
	public ButtonThemed() {
	}

	/// <summary>
	/// Copy constructor
	/// </summary>
	/// <param name="other"></param>
	public ButtonThemed(ButtonThemed other)
		: base(other) {
	}

	public override ButtonStyle Style {
		get {
			return base.Style;
		}
		set {
			base.Style = value;
			_standardButton.Style = value;
		}
	}

	/// <summary>
	/// Clone
	/// </summary>
	/// <returns></returns>
	public override object Clone() {
		return new ButtonThemed(this);
	}

	public override SizeF GetBackgroundExtent(GraphicsMeasurement measure, SizeF contentSize) {
		if (System.Windows.Forms.Application.RenderWithVisualStyles && VisualStyleRenderer.IsElementDefined(GetBackgroundElement())) {
			var content = new Rectangle(new Point(0, 0), Size.Ceiling(contentSize));
			contentSize = GetRenderer(GetBackgroundElement()).GetBackgroundExtent(measure.Graphics, content).Size;
		} else
			contentSize = _standardButton.GetBackgroundExtent(measure, contentSize);
		return base.GetBackgroundExtent(measure, contentSize);
	}

	protected override void OnDraw(GraphicsCache graphics, RectangleF area) {
		base.OnDraw(graphics, area);
		if (System.Windows.Forms.Application.RenderWithVisualStyles && VisualStyleRenderer.IsElementDefined(GetBackgroundElement())) {
			GetRenderer(GetBackgroundElement()).DrawBackground(graphics.Graphics, Rectangle.Round(area));
			if (Style == ButtonStyle.Focus) {
				using (var measure = new GraphicsMeasurement(graphics)) {
					ControlPaint.DrawFocusRectangle(graphics.Graphics, Rectangle.Round(GetBackgroundContentRectangle(measure, area)));
				}
			}
		} else
			_standardButton.Draw(graphics, area);
	}

	public override RectangleF GetBackgroundContentRectangle(GraphicsMeasurement measure, RectangleF backGroundArea) {
		backGroundArea = base.GetBackgroundContentRectangle(measure, backGroundArea);
		if (System.Windows.Forms.Application.RenderWithVisualStyles && VisualStyleRenderer.IsElementDefined(GetBackgroundElement()))
			return GetRenderer(GetBackgroundElement()).GetBackgroundContentRectangle(measure.Graphics, Rectangle.Round(backGroundArea));
		return _standardButton.GetBackgroundContentRectangle(measure, backGroundArea);
	}

	protected VisualStyleElement GetBackgroundElement() {
		if (Style == ButtonStyle.Hot)
			return VisualStyleElement.Button.PushButton.Hot;
		if (Style == ButtonStyle.Pressed)
			return VisualStyleElement.Button.PushButton.Pressed;
		if (Style == ButtonStyle.Disabled)
			return VisualStyleElement.Button.PushButton.Disabled;
		if (Style == ButtonStyle.NormalDefault)
			return VisualStyleElement.Button.PushButton.Default;
		return VisualStyleElement.Button.PushButton.Normal;
	}

	/// <summary>
	/// Gets the System.Windows.Forms.VisualStyles.VisualStyleRenderer to draw the specified element.
	/// </summary>
	/// <param name="element"></param>
	/// <returns></returns>
	protected System.Windows.Forms.VisualStyles.VisualStyleRenderer GetRenderer(VisualStyleElement element) {
		return new System.Windows.Forms.VisualStyles.VisualStyleRenderer(element);
	}
}
