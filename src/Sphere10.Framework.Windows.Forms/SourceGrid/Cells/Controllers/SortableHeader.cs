// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Controllers;

/// <summary>
/// A behavior that support sort and resize. Once created cannot be modified. When calculated automatically the range to sort is all the grid range without the rows minor of the current row and the range header is all the grid range with the rows minor or equal of the current row
/// </summary>
public class SortableHeader : ControllerBase {
	// Sort Methods
	// Status Properties
	/// <summary>
	/// Range to sort
	/// </summary>
	private IRangeLoader _rangeToSort;

	/// <summary>
	/// Header range (can be null).
	/// </summary>
	private IRangeLoader _headerRange;

	/// <summary>
	/// Column header behavior with sort and resize support, same as SortResizeHeader.
	/// </summary>
	public readonly static SortableHeader Default;

	/// <summary>
	/// Border used to calculate the region where the sort is enabled.
	/// </summary>
	public Sphere10.Framework.Drawing.RectangleBorder LogicalBorder = new Sphere10.Framework.Drawing.RectangleBorder(new Sphere10.Framework.Drawing.BorderLine(System.Drawing.Color.Black, 4), new Sphere10.Framework.Drawing.BorderLine(System.Drawing.Color.Black, 4));

	// Constructor
	/// <summary>
	/// Static constructor
	/// </summary>
	static SortableHeader() {
		Default = new SortableHeader();
	}

	public SortableHeader()
		: this(null, null) {
	}

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="rangeToSort">If null then the range is automatically calculated.</param>
	/// <param name="headerRange">If null then the range is automatically calculated.</param>
	public SortableHeader(IRangeLoader rangeToSort, IRangeLoader headerRange) {
		_headerRange = headerRange;
		_rangeToSort = rangeToSort;
	}

	/// <summary>
	/// Range to sort. If null and EnableSort is true the range is automatically calculated.
	/// </summary>
	public IRangeLoader RangeToSort {
		get {
			return _rangeToSort;
		}
	}

	/// <summary>
	/// Header range. If null and EnableSort is true the range is automatically calculated.
	/// </summary>
	public IRangeLoader RangeHeader {
		get {
			return _headerRange;
		}
	}

	// IBehaviorModel Members
	public override void OnMouseUp(CellContext sender, MouseEventArgs e) {
		base.OnMouseUp(sender, e);

		//Note: I can't use the click event because I don't have Button information (Control.MouseButtons returns always None inside the Click event)

		var currentPoint = sender.Grid.PointToClient(System.Windows.Forms.Control.MousePosition);
		var cellRect = sender.Grid.PositionToRectangle(sender.Position);
		float distance;
		var partType = LogicalBorder.GetPointPartType(cellRect, currentPoint, out distance);

		//eseguo il sort solo se non sono attualmente in resize
		if (IsSortEnable(sender) &&
			partType == Sphere10.Framework.Drawing.RectanglePartType.ContentArea &&
			e.Button == System.Windows.Forms.MouseButtons.Left) {
			var sortHeader = (Models.ISortableHeader)sender.Cell.Model.FindModel(typeof(Models.ISortableHeader));
			var status = sortHeader.GetSortStatus(sender);
			if (status.Style == Sphere10.Framework.Windows.Forms.Drawing.HeaderSortStyle.Ascending)
				SortColumn(sender, false, status.Comparer);
			else
				SortColumn(sender, true, status.Comparer);
		}
	}

	/// <summary>
	/// Sort the current column. If the range contains all the columns this method move directly the row object otherwise move each cell.
	/// </summary>
	/// <param name="sender"></param>
	/// <param name="ascending"></param>
	/// <param name="comparer"></param>
	public void SortColumn(CellContext sender, bool ascending, System.Collections.IComparer comparer) {
		if (IsSortEnable(sender) == false)
			return;
		//verifico che il sort sia abilitato e che ci sia almeno una riga da ordinare oltra a quella corrente
		if (sender.Position.Row < (sender.Grid.Rows.Count) && sender.Grid.Columns.Count > 0) {
			CellRange rangeToSort;
			CellRange rangeHeader;
			if (_rangeToSort != null)
				rangeToSort = _rangeToSort.GetRange(sender.Grid);
			else
				//the range to sort is all the grid range without the rows < of the current row
				rangeToSort = new CellRange(sender.Position.Row + 1, 0, sender.Grid.Rows.Count - 1, sender.Grid.Columns.Count - 1);

			if (_headerRange != null)
				rangeHeader = _headerRange.GetRange(sender.Grid);
			else
				//the range header is all the grid range with the rows <= of the current row
				rangeHeader = new CellRange(0, 0, sender.Position.Row, sender.Grid.Columns.Count - 1);

			var modelSortable = (Models.ISortableHeader)sender.Cell.Model.FindModel(typeof(Models.ISortableHeader));

			if (sender.Grid.Rows.Count > (sender.Position.Row + 1) &&
				sender.Grid.Columns.Count > sender.Grid.FixedColumns) {
				//Sort
				sender.Grid.SortRangeRows(rangeToSort, sender.Position.Column, ascending, comparer);
				if (ascending)
					modelSortable.SetSortMode(sender, Sphere10.Framework.Windows.Forms.Drawing.HeaderSortStyle.Ascending);
				else
					modelSortable.SetSortMode(sender, Sphere10.Framework.Windows.Forms.Drawing.HeaderSortStyle.Descending);

				//Remove the image from others ColHeaderSort
				for (var r = rangeHeader.Start.Row; r <= rangeHeader.End.Row; r++) {
					for (var c = rangeHeader.Start.Column; c <= rangeHeader.End.Column; c++) {
						var tmpCell = sender.Grid.GetCell(r, c);

						if (tmpCell != sender.Cell &&
							tmpCell != null &&
							tmpCell.Model.FindModel(typeof(Models.ISortableHeader)) != null) {
							var header = (Models.ISortableHeader)tmpCell.Model.FindModel(typeof(Models.ISortableHeader));

							header.SetSortMode(new CellContext(sender.Grid, new Position(r, c), tmpCell), Sphere10.Framework.Windows.Forms.Drawing.HeaderSortStyle.None);
						}
					}
				}

				sender.Grid.InvalidateRange(rangeHeader);
			}
		}
	}

	// Support Function
	/// <summary>
	/// Indicates if for the specified cell the sort is enabled.
	/// </summary>
	/// <param name="sender"></param>
	/// <returns></returns>
	public bool IsSortEnable(CellContext sender) {
		if (sender.Grid.EnableSort == false)
			return false;

		if (sender.Cell.Model.FindModel(typeof(Models.ISortableHeader)) != null)
			return true;
		return false;
	}
}
