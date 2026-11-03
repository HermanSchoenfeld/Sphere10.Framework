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
using Sphere10.Framework.Drawing;

namespace Sphere10.Framework.Windows.Forms.Drawing.VisualElements;

[Serializable]
public class DropDownButton : DropDownButtonBase {
	/// <summary>
	/// Default constructor
	/// </summary>
	public DropDownButton() {
	}

	/// <summary>
	/// Copy constructor
	/// </summary>
	/// <param name="other"></param>
	public DropDownButton(DropDownButton other)
		: base(other) {
	}

	/// <summary>
	/// Clone
	/// </summary>
	/// <returns></returns>
	public override object Clone() {
		return new DropDownButton(this);
	}

	protected override void OnDraw(GraphicsCache graphics, System.Drawing.RectangleF area) {
		ButtonState state;
		if (Style == ButtonStyle.Disabled)
			state = ButtonState.Inactive;
		else if (Style == ButtonStyle.Pressed)
			state = ButtonState.Pushed;
		else if (Style == ButtonStyle.Hot)
			state = ButtonState.Normal;
		else
			state = ButtonState.Normal;
		ControlPaint.DrawComboButton(graphics.Graphics, Rectangle.Round(area), state);
		if (Style == ButtonStyle.NormalDefault)
			graphics.Graphics.DrawRectangle(Pens.Black, Rectangle.Round(area));

		if (Style == ButtonStyle.Focus) {
			using (var measure = new GraphicsMeasurement(graphics)) {
				ControlPaint.DrawFocusRectangle(graphics.Graphics, Rectangle.Round(area));
			}
		}
	}

	protected override SizeF OnMeasureContent(GraphicsMeasurement measure, SizeF maxSize) {
		//TODO Check to see if it is possible to get the real default size...
		return new SizeF(16, 16);
	}
	//public override System.Drawing.RectangleF GetBackgroundContentRectangle(GraphicsMeasurement measure, System.Drawing.RectangleF backGroundArea)
	//{
	//    backGroundArea = base.GetBackgroundContentRectangle(measure, backGroundArea);
	//    if (backGroundArea.Width > 4)
	//    {
	//        backGroundArea.X += 2;
	//        backGroundArea.Width -= 4;
	//    }
	//    if (backGroundArea.Height > 4)
	//    {
	//        backGroundArea.Y += 2;
	//        backGroundArea.Height -= 4;
	//    }
	//    return backGroundArea;
	//}
	//public override System.Drawing.SizeF GetBackgroundExtent(GraphicsMeasurement measure, System.Drawing.SizeF contentSize)
	//{
	//    contentSize = new SizeF(contentSize.Width + 4, contentSize.Height + 4);
	//    return base.GetBackgroundExtent(measure, contentSize);
	//}
}
