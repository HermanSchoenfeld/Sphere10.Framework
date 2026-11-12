// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Views;

/// <summary>
/// Summary description for a 3D themed Header.
/// </summary>
[Serializable]
public class RowHeader : Header {
	/// <summary>
	/// Represents a Column Header with the ability to draw an Image in the right to indicates the sort operation. You must use this model with a cell of type ICellSortableHeader.
	/// </summary>
	public new readonly static RowHeader Default;

	#region Constructors

	static RowHeader() => Default = new RowHeader();

	/// <summary>
	/// Use default setting
	/// </summary>
	public RowHeader() => Background = new Sphere10.Framework.Windows.Forms.Drawing.VisualElements.RowHeaderThemed();

	/// <summary>
	/// Copy constructor.  This method duplicate all the reference field (Image, Font, StringFormat) creating a new instance.
	/// </summary>
	/// <param name="source"></param>
	public RowHeader(RowHeader source)
		: base(source) {
	}

	#endregion

	#region Visual Elements

	public new Sphere10.Framework.Windows.Forms.Drawing.VisualElements.IRowHeader Background {
		get => (Sphere10.Framework.Windows.Forms.Drawing.VisualElements.IRowHeader)base.Background;
		set => base.Background = value;
	}

	#region Clone

	/// <summary>
	/// Clone this object. This method duplicate all the reference field (Image, Font, StringFormat) creating a new instance.
	/// </summary>
	/// <returns></returns>
	public override object Clone() => new RowHeader(this);

	#endregion

	protected override void PrepareView(CellContext context) => base.PrepareView(context);

	#endregion
}
