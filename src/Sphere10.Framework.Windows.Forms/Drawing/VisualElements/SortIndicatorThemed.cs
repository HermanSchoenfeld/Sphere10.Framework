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
public class SortIndicatorThemed : SortIndicator {
	/// <summary>
	/// Default constructor
	/// </summary>
	public SortIndicatorThemed() {
	}

	/// <summary>
	/// Copy constructor
	/// </summary>
	/// <param name="other"></param>
	public SortIndicatorThemed(SortIndicatorThemed other)
		: base(other) {
	}

	/// <summary>
	/// Clone
	/// </summary>
	/// <returns></returns>
	public override object Clone() {
		return new SortIndicatorThemed(this);
	}

	protected override void OnDraw(GraphicsCache graphics, System.Drawing.RectangleF area) {
		if (SortStyle != HeaderSortStyle.None && System.Windows.Forms.Application.RenderWithVisualStyles && VisualStyleRenderer.IsElementDefined(GetSortElement())) {
			var sortArea = Rectangle.Round(area);
			var renderer = GetRenderer(GetSortElement());
			var sortSize = renderer.GetPartSize(graphics.Graphics, sortArea, ThemeSizeType.Draw);
			sortArea = new Rectangle(sortArea.Right - sortSize.Width, sortArea.Top, sortSize.Width, sortSize.Height);
			renderer.DrawBackground(graphics.Graphics, sortArea);
		} else
			base.OnDraw(graphics, area);
	}

	protected override SizeF OnMeasureContent(GraphicsMeasurement measure, SizeF maxSize) {
		if (SortStyle != HeaderSortStyle.None && System.Windows.Forms.Application.RenderWithVisualStyles && VisualStyleRenderer.IsElementDefined(GetSortElement())) {
			var renderer = GetRenderer(GetSortElement());
			return renderer.GetPartSize(measure.Graphics, ThemeSizeType.Draw);
		}
		return base.OnMeasureContent(measure, maxSize);
	}

	/// <summary>
	/// Gets the System.Windows.Forms.VisualStyles.VisualStyleRenderer to draw the specified element.
	/// </summary>
	/// <param name="element"></param>
	/// <returns></returns>
	public System.Windows.Forms.VisualStyles.VisualStyleRenderer GetRenderer(VisualStyleElement element) {
		return new System.Windows.Forms.VisualStyles.VisualStyleRenderer(element);
	}

	protected VisualStyleElement GetSortElement() {
		if (SortStyle == HeaderSortStyle.Ascending)
			return VisualStyleElement.Header.SortArrow.SortedUp;
		if (SortStyle == HeaderSortStyle.Descending)
			return VisualStyleElement.Header.SortArrow.SortedDown;
		return null;
	}
}
