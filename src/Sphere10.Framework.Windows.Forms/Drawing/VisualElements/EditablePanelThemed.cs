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
public class EditablePanelThemed : EditablePanelBase {
	/// <summary>
	/// Standard button used when the XP style are disabled.
	/// </summary>
	private EditablePanel _standard = new EditablePanel();
	private RectangleBorder _equivalentPadding = new RectangleBorder(new BorderLine(Color.Empty, 2));

	/// <summary>
	/// Default constructor
	/// </summary>
	public EditablePanelThemed() {
	}

	/// <summary>
	/// Copy constructor
	/// </summary>
	/// <param name="other"></param>
	public EditablePanelThemed(EditablePanelThemed other)
		: base(other) {
	}

	public override BorderStyle BorderStyle {
		get {
			return base.BorderStyle;
		}

		set {
			base.BorderStyle = value;
			_standard.BorderStyle = value;
		}
	}

	/// <summary>
	/// Clone
	/// </summary>
	/// <returns></returns>
	public override object Clone() {
		return new EditablePanelThemed(this);
	}

	public override void Draw(GraphicsCache graphics, RectangleF area) {
		if (System.Windows.Forms.Application.RenderWithVisualStyles && VisualStyleRenderer.IsElementDefined(GetBackgroundElement())) {
			if (BorderStyle == BorderStyle.System)
				GetRenderer(GetBackgroundElement()).DrawBackground(graphics.Graphics, Rectangle.Round(area));
		} else
			_standard.Draw(graphics, area);
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

	protected VisualStyleElement GetBackgroundElement() {
		return VisualStyleElement.TextBox.TextEdit.Normal;
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
