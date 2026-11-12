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
public class Icon : VisualElementBase, IIcon {
	private System.Drawing.Icon _value = null;

	#region Constructor

	/// <summary>
	/// Default constructor
	/// </summary>
	public Icon() {
	}

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="value"></param>
	public Icon(System.Drawing.Icon value) => Value = value;

	/// <summary>
	/// Copy constructor
	/// </summary>
	/// <param name="other"></param>
	public Icon(Icon other)
		: base(other) {
		if (other.Value != null)
			Value = (System.Drawing.Icon)other.Value.Clone();
		else
			Value = null;
	}

	#endregion

	/// <summary>
	/// Gets or sets the Icon to draw. Default is null.
	/// </summary>
	[DefaultValue(null)]
	public System.Drawing.Icon Value {
		get => _value;

		set => _value = value;
	}

	public override object Clone() => new Icon(this);

	protected override void OnDraw(GraphicsCache graphics, System.Drawing.RectangleF area) {
		if (Value != null)
			graphics.Graphics.DrawIcon(Value, Rectangle.Round(area));
	}

	protected override System.Drawing.SizeF OnMeasureContent(GraphicsMeasurement measure, System.Drawing.SizeF maxSize) {
		if (Value != null)
			return Value.Size;
		return SizeF.Empty;
	}
}
