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
public class TextGDI : Text {
	private StringFormat _stringFormat = new StringFormat(StringFormat.GenericDefault);

	#region Constructor

	/// <summary>
	/// Default constructor
	/// </summary>
	public TextGDI() {
	}

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="value"></param>
	public TextGDI(string value) => Value = value;

	/// <summary>
	/// Copy constructor
	/// </summary>
	/// <param name="other"></param>
	public TextGDI(TextGDI other)
		: base(other) {
		if (other.StringFormat != null)
			StringFormat = (StringFormat)other.StringFormat.Clone();
		else
			StringFormat = null;
	}

	#endregion

	/// <summary>
	/// Gets or sets the StringFormat object.
	/// Encapsulates text layout information (such as alignment, orientation and tab stops) display manipulations (such as ellipsis insertion and national digit substitution) and OpenType features.
	/// </summary>
	[TypeConverter(typeof(ExpandableObjectConverter))]
	public virtual StringFormat StringFormat {
		get => _stringFormat;

		set => _stringFormat = value;
	}

	/// <summary>
	/// Gets or sets the alignment of the content.
	/// </summary>
	public virtual ContentAlignment Alignment {
		get => Tools.Drawing.StringFormatToContentAlignment(StringFormat);

		set => Tools.Drawing.ApplyContentAlignmentToStringFormat(value, StringFormat);
	}

	/// <summary>
	/// Clone
	/// </summary>
	/// <returns></returns>
	public override object Clone() => new TextGDI(this);

	protected override void OnDraw(GraphicsCache graphics, RectangleF area) {
		//base.OnDrawContent(graphics, area); //Don't call the base method because this class draw the class in a different way
		if (Value == null || Value.Length == 0)
			return;
		SolidBrush brush;
		if (Enabled)
			brush = graphics.BrushCache.GetBrush(ForeColor);
		else
			brush = graphics.BrushCache.GetBrush(Color.FromKnownColor(KnownColor.GrayText));
		graphics.Graphics.DrawString(Value, Font, brush, area, StringFormat);
	}

	/// <summary>
	/// Measure the current content of the VisualElement.
	/// </summary>
	/// <param name="measure"></param>
	/// <param name="maxSize">If empty is not used.</param>
	/// <returns></returns>
	protected override SizeF OnMeasureContent(GraphicsMeasurement measure, System.Drawing.SizeF maxSize) {
		if (maxSize != System.Drawing.SizeF.Empty)
			return measure.Graphics.MeasureString(Value, Font, maxSize, StringFormat);
		return measure.Graphics.MeasureString(Value, Font, new SizeF(5000, 5000), StringFormat);
	}

	protected virtual bool ShouldSerializeAlignment() {
		return false; //Always false because this property simply change the StringFormat
	}
}
