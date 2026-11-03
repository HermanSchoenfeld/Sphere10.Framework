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
public class ColumnHeaderThemed : ColumnHeaderBase {
	/// <summary>
	/// Standard column header used when the XP style are disabled.
	/// </summary>
	private ColumnHeader _standardHeader = new ColumnHeader();

	/// <summary>
	/// Default constructor
	/// </summary>
	public ColumnHeaderThemed() {
	}

	/// <summary>
	/// Copy constructor
	/// </summary>
	/// <param name="other"></param>
	public ColumnHeaderThemed(ColumnHeaderThemed other)
		: base(other) {
	}

	public override ControlDrawStyle Style {
		get {
			return base.Style;
		}
		set {
			base.Style = value;
			_standardHeader.Style = value;
		}
	}

	/// <summary>
	/// Clone
	/// </summary>
	/// <returns></returns>
	public override object Clone() {
		return new ColumnHeaderThemed(this);
	}

	public override RectangleF GetBackgroundContentRectangle(GraphicsMeasurement measure, RectangleF backGroundArea) {
		//First calls the base class for standard calculation
		backGroundArea = base.GetBackgroundContentRectangle(measure, backGroundArea);
		if (System.Windows.Forms.Application.RenderWithVisualStyles && VisualStyleRenderer.IsElementDefined(GetBackgroundElement()))
			return GetRenderer(GetBackgroundElement()).GetBackgroundContentRectangle(measure.Graphics, Rectangle.Round(backGroundArea));
		return _standardHeader.GetBackgroundContentRectangle(measure, backGroundArea);
	}

	public override SizeF GetBackgroundExtent(GraphicsMeasurement measure, SizeF contentSize) {
		if (System.Windows.Forms.Application.RenderWithVisualStyles && VisualStyleRenderer.IsElementDefined(GetBackgroundElement())) {
			var content = new Rectangle(new Point(0, 0), Size.Ceiling(contentSize));
			contentSize = GetRenderer(GetBackgroundElement()).GetBackgroundExtent(measure.Graphics, content).Size;
		} else
			contentSize = _standardHeader.GetBackgroundExtent(measure, contentSize);
		//Calls the base class for standard calculation
		return base.GetBackgroundExtent(measure, contentSize);
	}

	protected override void OnDraw(GraphicsCache graphics, RectangleF area) {
		base.OnDraw(graphics, area);
		if (System.Windows.Forms.Application.RenderWithVisualStyles && VisualStyleRenderer.IsElementDefined(GetBackgroundElement()))
			GetRenderer(GetBackgroundElement()).DrawBackground(graphics.Graphics, Rectangle.Round(area));
		else
			_standardHeader.Draw(graphics, area);
	}

	protected VisualStyleElement GetBackgroundElement() {
		if (Style == ControlDrawStyle.Hot)
			return VisualStyleElement.Header.Item.Hot;
		if (Style == ControlDrawStyle.Pressed)
			return VisualStyleElement.Header.Item.Pressed;
		return VisualStyleElement.Header.Item.Normal;
	}

	/// <summary>
	/// Gets the System.Windows.Forms.VisualStyles.VisualStyleRenderer to draw the specified element.
	/// </summary>
	/// <param name="element"></param>
	/// <returns></returns>
	protected System.Windows.Forms.VisualStyles.VisualStyleRenderer GetRenderer(VisualStyleElement element) {
		return new System.Windows.Forms.VisualStyles.VisualStyleRenderer(element);
	}
}
