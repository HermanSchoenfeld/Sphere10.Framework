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

/// <summary>
/// Interface for all the VisualElements classes. Inherits from IClonable.
/// Support a deep clone.
/// </summary>
public interface IVisualElement : ICloneable {
	/// <summary>
	/// Gets or sets the area where the content must be drawed.
	/// You can set to align the content to the left, right, top or bottom using the relative properties (Left, Right, Top, Bottom).
	/// You can also set more than one properties to allign the content to more than one side.
	/// </summary>
	AnchorArea AnchorArea {
		get; set;
	}

	/// <summary>
	/// Gets the area where the visual element will be drawed.
	/// </summary>
	/// <param name="measure"></param>
	/// <param name="area"></param>
	/// <returns></returns>
	RectangleF GetDrawingArea(GraphicsMeasurement measure, System.Drawing.RectangleF area);

	/// <summary>
	/// Measure the current VisualElement using the specified settings.
	/// </summary>
	/// <param name="measure"></param>
	/// <param name="minSize">If Empty is not used.</param>
	/// <param name="maxSize">If Empty is not used.</param>
	/// <returns></returns>
	System.Drawing.SizeF Measure(GraphicsMeasurement measure, System.Drawing.SizeF minSize, System.Drawing.SizeF maxSize);

	/// <summary>
	/// Draw the current VisualElement in the specified Graphics object.
	/// </summary>
	/// <param name="graphics"></param>
	/// <param name="area"></param>
	void Draw(GraphicsCache graphics, System.Drawing.RectangleF area);

	/// <summary>
	/// Draw the current VisualElement in the specified Graphics object.
	/// </summary>
	/// <param name="graphics"></param>
	/// <param name="area"></param>
	/// <param name="drawingArea"></param>
	void Draw(GraphicsCache graphics, System.Drawing.RectangleF area, out System.Drawing.RectangleF drawingArea);

	/// <summary>
	/// Get the element at the specified point. Usually this methods simply return the current element, but an element can return inner elements drawed inside the main elements.
	/// Returns a list of elements, where the last element is the upper element and the first element is the background element.
	/// </summary>
	/// <param name="measure"></param>
	/// <param name="area"></param>
	/// <param name="point"></param>
	/// <returns></returns>
	VisualElementList GetElementsAtPoint(GraphicsMeasurement measure, System.Drawing.RectangleF area, PointF point);
}
