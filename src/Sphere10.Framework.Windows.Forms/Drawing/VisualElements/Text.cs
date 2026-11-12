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
public class Text : VisualElementBase, IText {
	private Font _font = System.Windows.Forms.Control.DefaultFont;
	private Color _foreColor = System.Windows.Forms.Control.DefaultForeColor;
	private string _value = null;
	private bool _enabled = true;

	#region Constructor

	/// <summary>
	/// Default constructor
	/// </summary>
	public Text() {
	}

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="value"></param>
	public Text(string value) => Value = value;

	/// <summary>
	/// Copy constructor.
	/// </summary>
	/// <param name="other"></param>
	public Text(Text other)
		: base(other) {
		Value = other.Value;
		Font = other.Font;
		ForeColor = other.ForeColor;
		Enabled = other.Enabled;
	}

	#endregion

	/// <summary>
	/// Gets or sets the Font of the content. Default is System.Windows.Forms.Control.DefaultFont.
	/// </summary>
	public virtual Font Font {
		get => _font;

		set => _font = value;
	}

	/// <summary>
	/// Gets or sets the fore color of the content. System.Windows.Forms.Control.DefaultForeColor
	/// </summary>
	public virtual Color ForeColor {
		get => _foreColor;

		set => _foreColor = value;
	}

	/// <summary>
	/// Gets or sets the string to draw. Default is null.
	/// </summary>
	[DefaultValue(null)]
	public virtual string Value {
		get => _value;

		set => _value = value;
	}

	public bool Enabled {
		get => _enabled;

		set => _enabled = value;
	}

	/// <summary>
	/// Clone
	/// </summary>
	/// <returns></returns>
	public override object Clone() => new Text(this);

	#region Measure

	/// <summary>
	/// Measure the current content of the VisualElement.
	/// </summary>
	/// <param name="measure"></param>
	/// <param name="maxSize">If empty is not used.</param>
	/// <returns></returns>
	protected override System.Drawing.SizeF OnMeasureContent(GraphicsMeasurement measure, System.Drawing.SizeF maxSize) {
		if (maxSize != System.Drawing.SizeF.Empty)
			return measure.Graphics.MeasureString(Value, Font, maxSize);
		return measure.Graphics.MeasureString(Value, Font);
	}

	#endregion

	protected override void OnDraw(GraphicsCache graphics, System.Drawing.RectangleF area) {
		SolidBrush brush;
		if (Enabled)
			brush = graphics.BrushCache.GetBrush(ForeColor);
		else
			brush = graphics.BrushCache.GetBrush(Color.FromKnownColor(KnownColor.GrayText));
		graphics.Graphics.DrawString(Value, Font, brush, area);
	}

	private bool ShouldSerializeFont() => Font.ToString() != System.Windows.Forms.Control.DefaultFont.ToString();

	private bool ShouldSerializeForeColor() => ForeColor != System.Windows.Forms.Control.DefaultForeColor;
}
