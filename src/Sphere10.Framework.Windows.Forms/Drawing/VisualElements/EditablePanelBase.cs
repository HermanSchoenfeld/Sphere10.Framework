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
public abstract class EditablePanelBase : IEditablePanel {
	private BorderStyle _borderStyle = BorderStyle.System;

	/// <summary>
	/// Default constructor
	/// </summary>
	public EditablePanelBase() {
	}

	/// <summary>
	/// Copy constructor
	/// </summary>
	/// <param name="other"></param>
	public EditablePanelBase(EditablePanelBase other) {
		BorderStyle = other.BorderStyle;
	}

	public virtual BorderStyle BorderStyle {
		get {
			return _borderStyle;
		}

		set {
			_borderStyle = value;
		}
	}

	public abstract RectangleF GetContentRectangle(RectangleF backGroundArea);

	public abstract SizeF GetExtent(SizeF contentSize);

	/// <summary>
	/// Draw the current VisualElement in the specified Graphics object.
	/// </summary>
	/// <param name="graphics"></param>
	/// <param name="area"></param>
	public abstract void Draw(GraphicsCache graphics, System.Drawing.RectangleF area);

	/// <summary>
	///
	/// </summary>
	/// <param name="area"></param>
	/// <param name="point"></param>
	/// <param name="distanceFromBorder">Returns the distance of the specified point from the border rectangle. -1 if is not inside the border. Returns a positive value or 0 if inside the border. Consider always the distance from the outer border.</param>
	/// <returns></returns>
	public abstract RectanglePartType GetPointPartType(System.Drawing.RectangleF area, System.Drawing.PointF point, out float distanceFromBorder);

	public abstract object Clone();

	protected virtual bool ShouldSerializeBorderStyle() {
		return BorderStyle != BorderStyle.System;
	}
}
