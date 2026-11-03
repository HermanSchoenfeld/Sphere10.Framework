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
/// The background visualelement can be used as a background for a container element.
/// Override the OnDraw, GetBackgroundContentRectangle and GetBackgroundExtent to draw a custom background.
/// </summary>
[Serializable]
public abstract class BackgroundBase : VisualElementBase, IBackground {
	/// <summary>
	/// Default constructor
	/// </summary>
	public BackgroundBase() {
	}

	/// <summary>
	/// Copy constructor
	/// </summary>
	/// <param name="other"></param>
	public BackgroundBase(BackgroundBase other)
		: base(other) {
	}

	/// <summary>
	/// Calculate the client area where the content can be drawed, usually removing the area used by the background, for example removing a border.
	/// </summary>
	/// <returns></returns>
	public virtual RectangleF GetBackgroundContentRectangle(GraphicsMeasurement measure, RectangleF backGroundArea) {
		return backGroundArea;
	}

	protected override void OnDraw(GraphicsCache graphics, RectangleF area) {
	}

	protected override SizeF OnMeasureContent(GraphicsMeasurement measure, SizeF maxSize) {
		var clientSize = SizeF.Empty;
		return GetBackgroundExtent(measure, clientSize);
	}

	/// <summary>
	/// Calculate the total area used by the backgound and the content, adding the background area to the content area.
	/// </summary>
	/// <returns></returns>
	public virtual SizeF GetBackgroundExtent(GraphicsMeasurement measure, SizeF contentSize) {
		return contentSize;
	}
}
