// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Drawing;
using Sphere10.Framework.Drawing;

namespace Sphere10.Framework.Windows.Forms.Drawing.VisualElements;

/// <summary>
/// The base abstract class for all container VisualElements.
/// Override the GetElements() to get a collection of elements to draw inside the container.
/// Use the Border, Padding and Background properties to customize the container
/// </summary>
[Serializable]
public abstract class ContainerBase : VisualElementBase, IContainer {
	private ElementsDrawMode _drawMode = ElementsDrawMode.Covering;
	private IVisualElement _background = null;
	private Padding _padding = Padding.Empty;
	private IBorder _border = null;

	#region Constructor

	/// <summary>
	/// Default constructor
	/// </summary>
	public ContainerBase() {
	}

	/// <summary>
	/// Copy constructor
	/// </summary>
	/// <param name="other"></param>
	public ContainerBase(ContainerBase other)
		: base(other) {
		if (other.Background != null)
			Background = (IVisualElement)other.Background.Clone();
		else
			Background = null;
		ElementsDrawMode = other.ElementsDrawMode;
		Padding = other.Padding;
		if (other.Border != null)
			Border = (IBorder)other.Border.Clone();
		else
			Border = null;
	}

	#endregion

	/// <summary>
	/// Gets or sets a property that specify how to draw the elements.
	/// </summary>
	protected ElementsDrawMode ElementsDrawMode {
		get => _drawMode;

		set => _drawMode = value;
	}

	/// <summary>
	/// The background used to draw
	/// </summary>
	protected IVisualElement Background {
		get => _background;

		set => _background = value;
	}

	protected Padding Padding {
		get => _padding;

		set => _padding = value;
	}

	protected IBorder Border {
		get => _border;

		set => _border = value;
	}

	/// <summary>
	/// Get the element at the specified point. Usually this methods simply return the current element, but an element can return inner elements drawed inside the main elements.
	/// Returns a list of elements, where the last element is the upper (foremost) element and the first element is the background element.
	/// </summary>
	/// <param name="measure"></param>
	/// <param name="area"></param>
	/// <param name="point"></param>
	/// <returns></returns>
	public override VisualElementList GetElementsAtPoint(GraphicsMeasurement measure, System.Drawing.RectangleF area, PointF point) {
		var list = base.GetElementsAtPoint(measure, area, point);
		area = GetContentRectangle(measure, area);
		//Use the same code of the OnDrawContent method
		//In this case the elements are drawed one over the another
		if (ElementsDrawMode == ElementsDrawMode.Covering) {
			foreach (var element in GetElements())
				list.AddRange(element.GetElementsAtPoint(measure, area, point));
		} else if (ElementsDrawMode == ElementsDrawMode.Align) {
			//In this case the elements are drawed considering an alignment
			foreach (var element in GetElements()) {
				list.AddRange(element.GetElementsAtPoint(measure, area, point));
				var elementArea = element.GetDrawingArea(measure, area);
				area = CalculateRemainingArea(area, element.AnchorArea, elementArea);
			}
		} else
			throw new ApplicationException("DrawMode not supported");
		return list;
	}

	/// <summary>
	/// Calculate the total area used by the backgound and the content, adding the background area to the content area.
	/// </summary>
	/// <returns></returns>
	public SizeF GetExtent(GraphicsMeasurement measure, SizeF contentSize) {
		if (Background is IBackground)
			contentSize = ((IBackground)Background).GetBackgroundExtent(measure, contentSize);

		if (Padding.IsEmpty == false)
			contentSize = Padding.GetExtent(contentSize);

		if (Border != null)
			contentSize = Border.GetExtent(contentSize);

		return contentSize;
	}

	/// <summary>
	/// Calculate the client area where the content can be drawed, usually removing the area used by the background, for example removing a border.
	/// </summary>
	/// <returns></returns>
	public RectangleF GetContentRectangle(GraphicsMeasurement measure, RectangleF backGroundArea) {
		if (Border != null)
			backGroundArea = Border.GetContentRectangle(backGroundArea);

		if (Padding.IsEmpty == false)
			backGroundArea = Padding.GetContentRectangle(backGroundArea);

		if (Background is IBackground)
			backGroundArea = ((IBackground)Background).GetBackgroundContentRectangle(measure, backGroundArea);

		return backGroundArea;
	}

	/// <summary>
	/// Utility function. Calculate the required size adding using the anchor informations of the available area.
	/// </summary>
	/// <param name="currentSize"></param>
	/// <param name="contentAnchor"></param>
	/// <param name="contentSize"></param>
	/// <returns></returns>
	public static SizeF CalculateSizeWithContent(SizeF currentSize, AnchorArea contentAnchor, SizeF contentSize) {
		if (contentAnchor != null && (contentAnchor.HasLeft || contentAnchor.HasRight))
			currentSize.Width += contentSize.Width;
		else {
			if (contentSize.Width > currentSize.Width)
				currentSize.Width = contentSize.Width;
		}

		if (contentAnchor != null && (contentAnchor.HasTop || contentAnchor.HasBottom))
			currentSize.Height += contentSize.Height;
		else {
			if (contentSize.Height > currentSize.Height)
				currentSize.Height = contentSize.Height;
		}

		return currentSize;
	}

	/// <summary>
	/// Add the anchor informations to the element content size
	/// </summary>
	/// <returns></returns>
	public static SizeF CalculateSizeWithAnchor(SizeF contentSize, AnchorArea contentAnchor, SizeF clientSize) {
		if (contentAnchor == null)
			return contentSize;
		if (contentAnchor.HasLeft && contentAnchor.HasRight) {
			if (clientSize.IsEmpty == false)
				contentSize.Width = clientSize.Width - (contentAnchor.Left + contentAnchor.Right);
		} else if (contentAnchor.HasLeft)
			contentSize.Width += contentAnchor.Left;
		else if (contentAnchor.HasRight)
			contentSize.Width += contentAnchor.Right;
		if (contentSize.Width < 0)
			contentSize.Width = 0;
		if (contentAnchor.HasTop && contentAnchor.HasBottom) {
			if (clientSize.IsEmpty == false)
				contentSize.Height = clientSize.Height - (contentAnchor.Top + contentAnchor.Bottom);
		} else if (contentAnchor.HasTop)
			contentSize.Height += contentAnchor.Top;
		else if (contentAnchor.HasBottom)
			contentSize.Height += contentAnchor.Bottom;
		if (contentSize.Height < 0)
			contentSize.Height = 0;
		return contentSize;
	}

	/// <summary>
	/// Utility function. Calculates the remaining empty area of a specified area and a given content.
	/// </summary>
	/// <returns></returns>
	public static RectangleF CalculateRemainingArea(RectangleF parentArea, AnchorArea contentAnchor, RectangleF contentArea) {
		if (contentAnchor != null) {
			float left, top, height, width;
			if (contentAnchor.HasTop && !contentAnchor.HasBottom) {
				top = contentArea.Bottom;
				height = parentArea.Height - contentArea.Height;
			} else if (contentAnchor.HasBottom && !contentAnchor.HasTop) {
				top = parentArea.Top;
				height = parentArea.Height - contentArea.Height;
			} else {
				top = parentArea.Top;
				height = parentArea.Height;
			}

			if (contentAnchor.HasLeft && !contentAnchor.HasRight) {
				left = contentArea.Right;
				width = parentArea.Width - contentArea.Width;
			} else if (contentAnchor.HasRight && !contentAnchor.HasLeft) {
				left = parentArea.Left;
				width = parentArea.Width - contentArea.Width;
			} else {
				left = parentArea.Left;
				width = parentArea.Width;
			}

			var newArea = new RectangleF(left, top, width, height);
			newArea.Intersect(parentArea);
			return newArea;
		}
		return parentArea;
	}

	protected override void OnDraw(GraphicsCache graphics, RectangleF area) {
		OnDrawBackground(graphics, area);
		using (var measure = new GraphicsMeasurement(graphics)) {
			var contentArea = GetContentRectangle(measure, area);
			OnDrawContent(graphics, contentArea);
		}
	}

	protected override SizeF OnMeasureContent(GraphicsMeasurement measure, SizeF maxSize) {
		var clientSize = new SizeF(0, 0);
		//In this case the elements are drawed one over the another so to measure I must simply take the greatest
		if (ElementsDrawMode == ElementsDrawMode.Covering) {
			foreach (var element in GetElements()) {
				var elementSize = element.Measure(measure, Size.Empty, maxSize);
				elementSize = CalculateSizeWithAnchor(elementSize, element.AnchorArea, maxSize);
				if (elementSize.Width > clientSize.Width)
					clientSize.Width = elementSize.Width;
				if (elementSize.Height > clientSize.Height)
					clientSize.Height = elementSize.Height;
			}
		} else if (ElementsDrawMode == ElementsDrawMode.Align) {
			//In this case the elements are drawed considering an alignment, so to measure I must consider the anchor area of the element and add the size to the if the alignment is set. This code reflect the drawing code
			AnchorArea previousAnchor = null;
			foreach (var element in GetElements()) {
				var elementSize = element.Measure(measure, Size.Empty, maxSize);
				elementSize = CalculateSizeWithAnchor(elementSize, element.AnchorArea, maxSize);
				clientSize = CalculateSizeWithContent(clientSize, previousAnchor, elementSize);
				previousAnchor = element.AnchorArea;
			}
		} else
			throw new ApplicationException("DrawMode not supported");
		return GetExtent(measure, clientSize);
	}

	protected virtual void OnDrawBackground(GraphicsCache graphics, RectangleF area) {
		if (Border != null) {
			Border.Draw(graphics, area);
			area = Border.GetContentRectangle(area);
		}

		if (Background != null)
			Background.Draw(graphics, area);
	}

	protected virtual void OnDrawContent(GraphicsCache graphics, RectangleF area) {
		//In this case the elements are drawed one over the another
		if (ElementsDrawMode == ElementsDrawMode.Covering) {
			foreach (var element in GetElements())
				element.Draw(graphics, area);
		} else if (ElementsDrawMode == ElementsDrawMode.Align) {
			//In this case the elements are drawed considering an alignment
			using (var measure = new GraphicsMeasurement(graphics)) {
				foreach (var element in GetElements()) {
					RectangleF elementArea;
					element.Draw(graphics, area, out elementArea);
					area = CalculateRemainingArea(area, element.AnchorArea, elementArea);
				}
			}
		} else
			throw new ApplicationException("DrawMode not supported");
	}

	protected abstract IEnumerable<IVisualElement> GetElements();

	protected virtual bool ShouldSerializeElementsDrawMode() => ElementsDrawMode != ElementsDrawMode.Covering;

	protected virtual bool ShouldSerializeBackground() => Background != null;

	protected virtual bool ShouldSerializePadding() => Padding != Padding.Empty;

	protected virtual bool ShouldSerializeBorder() => Border != null;
}
