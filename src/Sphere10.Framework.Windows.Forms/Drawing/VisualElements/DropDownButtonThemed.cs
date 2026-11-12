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
public class DropDownButtonThemed : DropDownButtonBase {
	/// <summary>
	/// Standard button used when the XP style are disabled.
	/// </summary>
	private DropDownButton _standardButton = new DropDownButton();

	#region Constuctor

	/// <summary>
	/// Default constructor
	/// </summary>
	public DropDownButtonThemed() {
	}

	/// <summary>
	/// Copy constructor
	/// </summary>
	/// <param name="other"></param>
	public DropDownButtonThemed(DropDownButtonThemed other)
		: base(other) {
	}

	#endregion

	public override ButtonStyle Style {
		get => base.Style;

		set {
			base.Style = value;
			_standardButton.Style = value;
		}
	}

	/// <summary>
	/// Clone
	/// </summary>
	/// <returns></returns>
	public override object Clone() => new DropDownButtonThemed(this);

	protected override void OnDraw(GraphicsCache graphics, RectangleF area) {
		if (System.Windows.Forms.Application.RenderWithVisualStyles && VisualStyleRenderer.IsElementDefined(GetBackgroundElement())) {
			GetRenderer(GetBackgroundElement()).DrawBackground(graphics.Graphics, Rectangle.Round(area));
			if (Style == ButtonStyle.Focus) {
				using (var measure = new GraphicsMeasurement(graphics)) {
					ControlPaint.DrawFocusRectangle(graphics.Graphics, Rectangle.Round(area));
				}
			}
		} else
			_standardButton.Draw(graphics, area);
	}

	protected override SizeF OnMeasureContent(GraphicsMeasurement measure, SizeF maxSize) {
		if (System.Windows.Forms.Application.RenderWithVisualStyles && VisualStyleRenderer.IsElementDefined(GetBackgroundElement())) {
			var size = GetRenderer(GetBackgroundElement()).GetPartSize(measure.Graphics, ThemeSizeType.True);
			// in Win-7 machines size is returned as 7
			// Increase it to 16, so it matches the standard drop-down width
			if (size.Width < 16)
				size.Width = 16;
			return size;
		}
		return _standardButton.Measure(measure, Size.Empty, maxSize);
	}

	protected VisualStyleElement GetBackgroundElement() {
		if (Style == ButtonStyle.Hot)
			return VisualStyleElement.ComboBox.DropDownButton.Hot;
		if (Style == ButtonStyle.Pressed)
			return VisualStyleElement.ComboBox.DropDownButton.Pressed;
		if (Style == ButtonStyle.Disabled)
			return VisualStyleElement.ComboBox.DropDownButton.Disabled;
		return VisualStyleElement.ComboBox.DropDownButton.Normal;
	}

	/// <summary>
	/// Gets the System.Windows.Forms.VisualStyles.VisualStyleRenderer to draw the specified element.
	/// </summary>
	/// <param name="element"></param>
	/// <returns></returns>
	protected System.Windows.Forms.VisualStyles.VisualStyleRenderer GetRenderer(VisualStyleElement element)
		=> new System.Windows.Forms.VisualStyles.VisualStyleRenderer(element);
	//public override RectangleF GetBackgroundContentRectangle(GraphicsMeasurement measure, RectangleF backGroundArea)
	//{
	//    backGroundArea = base.GetBackgroundContentRectangle(measure, backGroundArea);
	//    if (System.Windows.Forms.Application.RenderWithVisualStyles && VisualStyleRenderer.IsElementDefined(GetBackgroundElement()))
	//        return GetRenderer(GetBackgroundElement()).GetBackgroundContentRectangle(measure.Graphics, Rectangle.Round(backGroundArea));
	//    else
	//        return _standardButton.GetBackgroundContentRectangle(measure, backGroundArea);
	//}
	//public override SizeF GetBackgroundExtent(GraphicsMeasurement measure, SizeF contentSize)
	//{
	//    if (System.Windows.Forms.Application.RenderWithVisualStyles && VisualStyleRenderer.IsElementDefined(GetBackgroundElement()))
	//    {
	//        Rectangle content = new Rectangle(new Point(0, 0), Size.Ceiling(contentSize));
	//        contentSize = GetRenderer(GetBackgroundElement()).GetBackgroundExtent(measure.Graphics, content).Size;
	//    }
	//    else
	//        contentSize = _standardButton.GetBackgroundExtent(measure, contentSize);
	//    return base.GetBackgroundExtent(measure, contentSize);
	//}
}
