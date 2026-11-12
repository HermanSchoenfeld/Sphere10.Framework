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
public class CheckBox : CheckBoxBase {
	#region Constuctor

	/// <summary>
	/// Default constructor
	/// </summary>
	public CheckBox() {
	}

	/// <summary>
	/// Copy constructor
	/// </summary>
	/// <param name="other"></param>
	public CheckBox(CheckBox other)
		: base(other) {
	}

	#endregion

	/// <summary>
	/// Clone
	/// </summary>
	/// <returns></returns>
	public override object Clone() => new CheckBox(this);

	protected override void OnDraw(GraphicsCache graphics, RectangleF area) {
		ButtonState state;
		if (Style == ControlDrawStyle.Disabled)
			state = ButtonState.Inactive;
		else if (Style == ControlDrawStyle.Pressed)
			state = ButtonState.Pushed;
		else if (Style == ControlDrawStyle.Hot)
			state = ButtonState.Normal;
		else
			state = ButtonState.Normal;
		if (CheckBoxState == CheckBoxState.Checked)
			state |= ButtonState.Checked;
		ControlPaint.DrawCheckBox(graphics.Graphics, Rectangle.Round(area), state);
	}

	protected override SizeF OnMeasureContent(GraphicsMeasurement measure, SizeF maxSize) {
		//TODO Check to see if it is possible to get the real default size...
		return new SizeF(16, 16);
	}
}
