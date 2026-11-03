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
public class RowHeader : RowHeaderBase {
	private Header _background = new Header(0);

	/// <summary>
	/// Default constructor
	/// </summary>
	public RowHeader() {
	}

	/// <summary>
	/// Copy constructor
	/// </summary>
	/// <param name="other"></param>
	public RowHeader(RowHeader other)
		: base(other) {
		_background = (Header)other._background.Clone();
	}

	public override ControlDrawStyle Style {
		get {
			return base.Style;
		}
		set {
			base.Style = value;
			_background.Style = value;
		}
	}

	/// <summary>
	/// Back Color
	/// </summary>
	public Color BackColor {
		get => _background.BackColor;
		set => _background.BackColor = value;
	}

	/// <summary>
	/// Draw mode for the header. Default is Linear.
	/// </summary>
	public BackgroundColorStyle BackgroundColorStyle {
		get => _background.BackgroundColorStyle;
		set => _background.BackgroundColorStyle = value;
	}

	/// <summary>
	/// Border
	/// </summary>
	public RectangleBorder Border {
		get => _background.Border;
		set => _background.Border = value;
	}

	/// <summary>
	/// Clone
	/// </summary>
	/// <returns></returns>
	public override object Clone() {
		return new RowHeader(this);
	}

	public override RectangleF GetBackgroundContentRectangle(GraphicsMeasurement measure, RectangleF backGroundArea) {
		backGroundArea = base.GetBackgroundContentRectangle(measure, backGroundArea);
		return _background.GetBackgroundContentRectangle(measure, backGroundArea);
	}

	public override SizeF GetBackgroundExtent(GraphicsMeasurement measure, SizeF contentSize) {
		contentSize = _background.GetBackgroundExtent(measure, contentSize);
		return base.GetBackgroundExtent(measure, contentSize);
	}

	protected override void OnDraw(GraphicsCache graphics, RectangleF area) {
		base.OnDraw(graphics, area);
		_background.Draw(graphics, area);
	}
}
