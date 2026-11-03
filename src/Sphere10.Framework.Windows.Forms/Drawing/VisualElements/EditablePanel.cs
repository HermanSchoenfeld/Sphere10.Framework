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
public class EditablePanel : EditablePanelBase {
	private RectangleBorder _equivalentPadding = new RectangleBorder(new BorderLine(Color.Empty, 2));

	/// <summary>
	/// Default constructor
	/// </summary>
	public EditablePanel() {
	}

	/// <summary>
	/// Copy constructor
	/// </summary>
	/// <param name="other"></param>
	public EditablePanel(EditablePanel other)
		: base(other) {
	}

	/// <summary>
	/// Clone
	/// </summary>
	/// <returns></returns>
	public override object Clone() {
		return new EditablePanel(this);
	}

	public override void Draw(GraphicsCache graphics, RectangleF area) {
		if (BorderStyle == BorderStyle.System)
			ControlPaint.DrawBorder3D(graphics.Graphics, Rectangle.Round(area), Border3DStyle.Flat, Border3DSide.All);
	}

	public override System.Drawing.RectangleF GetContentRectangle(System.Drawing.RectangleF backGroundArea) {
		if (BorderStyle == BorderStyle.System)
			return _equivalentPadding.GetContentRectangle(backGroundArea);
		return backGroundArea;
	}

	public override System.Drawing.SizeF GetExtent(System.Drawing.SizeF contentSize) {
		if (BorderStyle == BorderStyle.System)
			return _equivalentPadding.GetExtent(contentSize);
		return contentSize;
	}

	public override RectanglePartType GetPointPartType(RectangleF area, PointF point, out float distanceFromBorder) {
		if (BorderStyle == BorderStyle.System)
			return _equivalentPadding.GetPointPartType(area, point, out distanceFromBorder);
		distanceFromBorder = 0;
		return RectanglePartType.ContentArea;
	}
}
