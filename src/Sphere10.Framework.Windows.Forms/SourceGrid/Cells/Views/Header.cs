// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework.Drawing;
using Sphere10.Framework.Windows.Forms.Drawing;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Views;

/// <summary>
/// Summary description for a 3D Header.
/// </summary>
[Serializable]
public class Header : Cell {
	public new static RectangleBorder DefaultBorder = RectangleBorder.NoBorder;

	/// <summary>
	/// Represents a default Header, with a 3D border and a LightGray BackColor
	/// </summary>
	public new readonly static Header Default;

	// Constructors
	static Header() {
		Default = new Header();
	}

	/// <summary>
	/// Use default setting
	/// </summary>
	public Header() {
		Background = new Sphere10.Framework.Windows.Forms.Drawing.VisualElements.HeaderThemed();
		Border = Header.DefaultBorder;
	}

	/// <summary>
	/// Copy constructor.  This method duplicate all the reference field (Image, Font, StringFormat) creating a new instance.
	/// </summary>
	/// <param name="source"></param>
	public Header(Header source)
		: base(source) {
		Background = (Sphere10.Framework.Windows.Forms.Drawing.VisualElements.IHeader)source.Background.Clone();
	}

	// Visual Elements
	public new Sphere10.Framework.Windows.Forms.Drawing.VisualElements.IHeader Background {
		get => (Sphere10.Framework.Windows.Forms.Drawing.VisualElements.IHeader)base.Background;
		set => base.Background = value;
	}

	// Clone
	/// <summary>
	/// Clone this object. This method duplicate all the reference field (Image, Font, StringFormat) creating a new instance.
	/// </summary>
	/// <returns></returns>
	public override object Clone() {
		return new Header(this);
	}

	protected override void PrepareView(CellContext context) {
		base.PrepareView(context);

		if (context.CellRange.Contains(context.Grid.MouseDownPosition))
			Background.Style = Sphere10.Framework.Windows.Forms.Drawing.ControlDrawStyle.Pressed;
		else if (context.CellRange.Contains(context.Grid.MouseCellPosition))
			Background.Style = Sphere10.Framework.Windows.Forms.Drawing.ControlDrawStyle.Hot;
		else
			Background.Style = Sphere10.Framework.Windows.Forms.Drawing.ControlDrawStyle.Normal;
	}
}
