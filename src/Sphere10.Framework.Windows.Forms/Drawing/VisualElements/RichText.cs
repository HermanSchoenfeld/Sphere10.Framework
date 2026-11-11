// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Drawing;
using Sphere10.Framework.Drawing;

namespace Sphere10.Framework.Windows.Forms.Drawing.VisualElements;

[Serializable]
public class RichText : VisualElementBase, IRichText {
	private Sphere10.Framework.Windows.Forms.RichText _value = null;
	private Color _foreColor = System.Windows.Forms.Control.DefaultForeColor;

	/// <summary>
	/// Text Alignment
	/// </summary>
	private System.Drawing.ContentAlignment _textAlignment;

	/// <summary>
	/// Text Font
	/// </summary>
	private Font _font = System.Windows.Forms.Control.DefaultFont;

	/// <summary>
	/// Rotate flip type
	/// </summary>
	private RotateFlipType _rotateFlipType = RotateFlipType.RotateNoneFlipNone;

	/// <summary>
	/// Default constructor
	/// </summary>
	public RichText() {
	}

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="value"></param>
	public RichText(Sphere10.Framework.Windows.Forms.RichText value) => Value = value;

	/// <summary>
	/// Copy constructor.
	/// </summary>
	/// <param name="other"></param>
	public RichText(RichText other)
		: base(other) {
		Value = other.Value;
		ForeColor = other.ForeColor;
		TextAlignment = other.TextAlignment;
		Font = other.Font;
	}

	/// <summary>
	/// Gets or sets the string of format rich text to draw. Default is null.
	/// </summary>
	[DefaultValue(null)]
	public virtual Sphere10.Framework.Windows.Forms.RichText Value {
		get => _value;

		set => _value = value;
	}

	/// <summary>
	/// Gets or sets the fore color of the content. System.Windows.Forms.Control.DefaultForeColor
	/// </summary>
	public virtual Color ForeColor {
		get => _foreColor;

		set => _foreColor = value;
	}

	public System.Drawing.ContentAlignment TextAlignment {
		get => _textAlignment;

		set => _textAlignment = value;
	}

	public virtual Font Font {
		get => _font;

		set => _font = value;
	}

	public RotateFlipType RotateFlipType {
		get => _rotateFlipType;

		set => _rotateFlipType = value;
	}

	/// <summary>
	/// Clone
	/// </summary>
	/// <returns></returns>
	public override object Clone() => new RichText(this);

	/// <summary>
	/// Measure the current content of the VisualElement.
	/// </summary>
	/// <param name="measure"></param>
	/// <param name="maxSize">If empty is not used.</param>
	/// <returns></returns>
	protected override System.Drawing.SizeF OnMeasureContent(GraphicsMeasurement measure, System.Drawing.SizeF maxSize) {
		var font = System.Windows.Forms.Control.DefaultFont;
		if (maxSize != System.Drawing.SizeF.Empty)
			return measure.Graphics.MeasureString(Value.Rtf, font, maxSize);
		return measure.Graphics.MeasureString(Value.Rtf, font);
	}

	protected override void OnDraw(GraphicsCache graphics, System.Drawing.RectangleF area) {
		var color = System.Windows.Forms.Control.DefaultForeColor;
		var font = System.Windows.Forms.Control.DefaultFont;
		var brush = graphics.BrushCache.GetBrush(color);
		graphics.Graphics.DrawString(Value.Rtf, font, brush, area);
	}
}
