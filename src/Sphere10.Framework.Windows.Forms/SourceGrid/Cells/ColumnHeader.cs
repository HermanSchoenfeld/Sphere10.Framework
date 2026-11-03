// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells;

/// <summary>
/// A cell that rappresent a header of a table.
/// View: Views.ColumnHeader.Default
/// Model: Models.SortableHeader
/// Controllers: Controllers.Unselectable.Default, Controllers.MouseInvalidate.Default, Controllers.Resizable.ResizeWidth, Controllers.SortableHeader.Default
/// </summary>
public class ColumnHeader : Cell {
	/// <summary>
	/// Constructor
	/// </summary>
	public ColumnHeader()
		: this(null) {
	}

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="cellValue"></param>
	public ColumnHeader(object cellValue)
		: base(cellValue) {
		View = Views.ColumnHeader.Default;
		Model.AddModel(new Models.SortableHeader());
		AddController(Controllers.Unselectable.Default);
		AddController(Controllers.MouseInvalidate.Default);
		ResizeEnabled = true;
		AutomaticSortEnabled = true;
	}

	/// <summary>
	/// Gets or sets if enable the resize of the width of the column. This property internally use the Controllers.Resizable.ResizeWidth.
	/// </summary>
	public bool ResizeEnabled {
		get {
			return FindController(typeof(Controllers.Resizable)) == Controllers.Resizable.ResizeWidth;
		}
		set {
			if (value == ResizeEnabled)
				return;

			if (value)
				AddController(Controllers.Resizable.ResizeWidth);
			else
				RemoveController(Controllers.Resizable.ResizeWidth);
		}
	}

	/// <summary>
	/// Gets or sets if enable the automatic sort features of the column. This property internally use the Controllers.SortableHeader.Default.
	/// If you want to use a custom sort you can add a customized Controller or a customized instance of Controllers.SortableHeader.
	/// </summary>
	public bool AutomaticSortEnabled {
		get {
			return FindController(typeof(Controllers.SortableHeader)) == Controllers.SortableHeader.Default;
		}
		set {
			if (value == AutomaticSortEnabled)
				return;

			if (value)
				AddController(Controllers.SortableHeader.Default);
			else
				RemoveController(Controllers.SortableHeader.Default);
		}
	}

	/// <summary>
	/// Status of the sort.
	/// </summary>
	public Models.SortStatus SortStatus {
		get => SortableHeaderModel.SortStatus;
		set => SortableHeaderModel.SortStatus = value;
	}

	/// <summary>
	/// Comparer used.
	/// </summary>
	public System.Collections.IComparer SortComparer {
		get => SortStatus.Comparer;
		set => SortStatus = new SourceGrid.Cells.Models.SortStatus(SortStatus.Style, value);
	}

	/// <summary>
	/// Sort style.
	/// </summary>
	public Sphere10.Framework.Windows.Forms.Drawing.HeaderSortStyle SortStyle {
		get => SortStatus.Style;
		set => SortStatus = new SourceGrid.Cells.Models.SortStatus(value, SortStatus.Comparer);
	}

	/// <summary>
	/// Gets the used SortableHeader model.
	/// </summary>
	private Models.SortableHeader SortableHeaderModel => (Models.SortableHeader)Model.FindModel(typeof(Models.SortableHeader));

	/// <summary>
	/// Sort the column
	/// </summary>
	/// <param name="ascending"></param>
	public void Sort(bool ascending) {
		var sortableHeader = (Controllers.SortableHeader)FindController(typeof(Controllers.SortableHeader));
		if (sortableHeader == null)
			throw new SourceGridException("No SortableHeader controller found");
		sortableHeader.SortColumn(new CellContext(Grid, Range.Start, this), ascending, SortComparer);
	}
}
