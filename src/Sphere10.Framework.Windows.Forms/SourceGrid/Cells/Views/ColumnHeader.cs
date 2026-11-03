// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Views;

/// <summary>
/// Summary description for a 3D Header.
/// This is a standard header without theme support. Use the ColumnHeaderThemed for theme support.
/// </summary>
[Serializable]
public class ColumnHeader : Header {
	private Sphere10.Framework.Windows.Forms.Drawing.VisualElements.ISortIndicator _elementSort = new Sphere10.Framework.Windows.Forms.Drawing.VisualElements.SortIndicator();

	/// <summary>
	/// Represents a Column Header with the ability to draw an Image in the right to indicates the sort operation. You must use this model with a cell of type ICellSortableHeader.
	/// </summary>
	public new readonly static ColumnHeader Default;

	// Constructors
	static ColumnHeader() {
		Default = new ColumnHeader();
	}

	/// <summary>
	/// Use default setting
	/// </summary>
	public ColumnHeader() {
		Background = new Sphere10.Framework.Windows.Forms.Drawing.VisualElements.ColumnHeaderThemed();
	}

	/// <summary>
	/// Copy constructor.  This method duplicate all the reference field (Image, Font, StringFormat) creating a new instance.
	/// </summary>
	/// <param name="source"></param>
	public ColumnHeader(ColumnHeader source)
		: base(source) {
	}

	// Visual Elements
	public new Sphere10.Framework.Windows.Forms.Drawing.VisualElements.IColumnHeader Background {
		get {
			return (Sphere10.Framework.Windows.Forms.Drawing.VisualElements.IColumnHeader)base.Background;
		}
		set {
			base.Background = value;
		}
	}

	/// <summary>
	/// Gets or sets the visual element used to draw the sort indicator. Default is Sphere10.Framework.Windows.Forms.Drawing.VisualElements.SortIndicator
	/// </summary>
	public Sphere10.Framework.Windows.Forms.Drawing.VisualElements.ISortIndicator ElementSort {
		get {
			return _elementSort;
		}
		set {
			_elementSort = value;
		}
	}

	// Clone
	/// <summary>
	/// Clone this object. This method duplicate all the reference field (Image, Font, StringFormat) creating a new instance.
	/// </summary>
	/// <returns></returns>
	public override object Clone() {
		return new ColumnHeader(this);
	}

	protected override void PrepareView(CellContext context) {
		base.PrepareView(context);

		PrepareVisualElementSortIndicator(context);
	}

	protected virtual void PrepareVisualElementSortIndicator(CellContext context) {
		var sortModel = (Models.ISortableHeader)context.Cell.Model.FindModel(typeof(Models.ISortableHeader));
		if (sortModel != null) {
			var status = sortModel.GetSortStatus(context);
			ElementSort.SortStyle = status.Style;
		} else
			ElementSort.SortStyle = Sphere10.Framework.Windows.Forms.Drawing.HeaderSortStyle.None;
	}

	protected override IEnumerable<Sphere10.Framework.Windows.Forms.Drawing.VisualElements.IVisualElement> GetElements() {
		if (ElementSort != null)
			yield return ElementSort;

		foreach (var v in GetBaseElements())
			yield return v;
	}

	private IEnumerable<Sphere10.Framework.Windows.Forms.Drawing.VisualElements.IVisualElement> GetBaseElements() {
		return base.GetElements();
	}
}
