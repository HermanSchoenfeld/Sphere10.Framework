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
public class BackgroundSolid : VisualElementBase {
	private Color _backColor = Color.Empty;

	public BackgroundSolid() {
	}

	public BackgroundSolid(Color backcolor) {
		BackColor = backcolor;
	}

	public BackgroundSolid(BackgroundSolid other)
		: base(other) {
		BackColor = other.BackColor;
	}

	/// <summary>
	/// Gets or sets the back color of the content.
	/// </summary>
	public virtual Color BackColor {
		get => _backColor;
		set => _backColor = value;
	}

	/// <summary>
	/// Clone
	/// </summary>
	/// <returns></returns>
	public override object Clone() {
		return new BackgroundSolid(this);
	}

	protected override void OnDraw(GraphicsCache graphics, System.Drawing.RectangleF area) {
		if (BackColor != Color.Empty) {
			var brush = graphics.BrushCache.GetBrush(BackColor);
			graphics.Graphics.FillRectangle(brush, area);
		}
	}

	protected override SizeF OnMeasureContent(GraphicsMeasurement measure, SizeF maxSize) {
		return SizeF.Empty;
	}

	protected virtual bool ShouldSerializeBackColor() {
		return BackColor != Color.Empty;
	}
}
