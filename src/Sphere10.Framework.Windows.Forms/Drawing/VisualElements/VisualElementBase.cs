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
/// VisualElement abstract base class.
/// You must override the OnDraw, OnMeasureContent and Clone.
/// </summary>
[Serializable]
public abstract class VisualElementBase : IVisualElement {
	private AnchorArea _anchorArea = null;

	/// <summary>
	/// Default constructor
	/// </summary>
	protected VisualElementBase() {
	}

	/// <summary>
	/// Copy constructor
	/// </summary>
	/// <param name="other"></param>
	public VisualElementBase(VisualElementBase other) {
		if (other.AnchorArea != null)
			AnchorArea = (AnchorArea)other.AnchorArea.Clone();
		else
			AnchorArea = null;
	}

	/// <summary>
	/// Gets or sets the area where the content must be drawed.
	/// You can set to align the content to the left, right, top or bottom using the relative properties (Left, Right, Top, Bottom).
	/// You can also set more than one properties to allign the content to more than one side.
	/// </summary>
	public virtual AnchorArea AnchorArea {
		get => _anchorArea;
		set => _anchorArea = value;
	}

	/// <summary>
	/// Draw the current VisualElement in the specified Graphics object.
	/// </summary>
	/// <param name="graphics"></param>
	/// <param name="area"></param>
	/// <param name="drawingArea"></param>
	public void Draw(GraphicsCache graphics, System.Drawing.RectangleF area, out System.Drawing.RectangleF drawingArea) {
		using (var measure = new GraphicsMeasurement(graphics)) {
			drawingArea = GetDrawingArea(measure, area);
			OnDraw(graphics, drawingArea);
		}
	}

	/// <summary>
	/// Draw the current VisualElement in the specified Graphics object.
	/// Usually derived class don't override this method but the OnDraw method that automatically check the area to draw.
	/// </summary>
	/// <param name="graphics"></param>
	/// <param name="area"></param>
	public void Draw(GraphicsCache graphics, System.Drawing.RectangleF area) {
		RectangleF drawArea;
		Draw(graphics, area, out drawArea);
	}

	/// <summary>
	/// Get the element at the specified point. Usually this methods simply return the current element, but an element can return inner elements drawed inside the main elements.
	/// Returns a list of elements, where the last element is the upper element and the first element is the background element.
	/// </summary>
	/// <param name="measure"></param>
	/// <param name="area"></param>
	/// <param name="point"></param>
	/// <returns></returns>
	public virtual VisualElementList GetElementsAtPoint(GraphicsMeasurement measure, System.Drawing.RectangleF area, PointF point) {
		var list = new VisualElementList();
		if (GetDrawingArea(measure, area).Contains(point))
			list.Add(this);
		return list;
	}

	/// <summary>
	/// Gets the area where the visual element will be drawed.
	/// </summary>
	/// <param name="measure"></param>
	/// <param name="area"></param>
	/// <returns></returns>
	public RectangleF GetDrawingArea(GraphicsMeasurement measure, System.Drawing.RectangleF area) {
		var destContentArea = area;
		if (AnchorArea != null && !AnchorArea.IsEmpty) {
			var measureSize = Measure(measure, SizeF.Empty, SizeF.Empty);
			destContentArea = AnchorArea.CalculateArea(area, measureSize, AnchorArea);
		}

		return destContentArea;
	}

	/// <summary>
	/// Measure the current VisualElement using the specified settings.
	/// </summary>
	/// <param name="measure"></param>
	/// <param name="minSize">If Empty is not used.</param>
	/// <param name="maxSize">If Empty is not used.</param>
	/// <returns></returns>
	public System.Drawing.SizeF Measure(GraphicsMeasurement measure, System.Drawing.SizeF minSize, System.Drawing.SizeF maxSize) {
		var measureSize = OnMeasureContent(measure, maxSize);
		return Sphere10.Framework.SizeFExtensions.Clip(measureSize, minSize, maxSize);
	}

	/// <summary>
	/// Clone the current VisualElement.
	/// </summary>
	/// <returns></returns>
	public abstract object Clone();

	/// <summary>
	/// Measure the current content of the VisualElement.
	/// </summary>
	/// <param name="measure"></param>
	/// <param name="maxSize">If empty is not used.</param>
	/// <returns></returns>
	protected abstract System.Drawing.SizeF OnMeasureContent(GraphicsMeasurement measure, System.Drawing.SizeF maxSize);

	/// <summary>
	/// Method used to draw the specified content based on the Visual element.
	/// This is the method that you must override to draw. The area used is already calculated based on the AnchorArea property.
	/// </summary>
	/// <param name="graphics"></param>
	/// <param name="area"></param>
	protected abstract void OnDraw(GraphicsCache graphics, System.Drawing.RectangleF area);

	protected virtual bool ShouldSerializeAnchorArea() {
		return AnchorArea != null && AnchorArea.IsEmpty == false;
	}
}
