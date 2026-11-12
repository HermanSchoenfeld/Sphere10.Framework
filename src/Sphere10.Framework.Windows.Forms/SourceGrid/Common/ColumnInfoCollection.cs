// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections;
using System.Collections.Generic;

namespace Sphere10.Framework.Windows.Forms.SourceGrid;

/// <summary>
/// Collection of ColumnInfo
/// </summary>
public class ColumnInfoCollection : ColumnsBase, IEnumerable<ColumnInfo> {
	/// <summary>
	/// Fired when the number of columns change
	/// </summary>
	public event IndexRangeEventHandler ColumnsAdded;

	/// <summary>
	/// Fired when some columns are removed
	/// </summary>
	public event IndexRangeEventHandler ColumnsRemoved;

	/// <summary>
	/// Fired before some columns are removed
	/// </summary>
	public event IndexRangeEventHandler ColumnsRemoving;

	/// <summary>
	/// Fired when the user change the Width property of one of the Column
	/// </summary>
	public event ColumnInfoEventHandler ColumnWidthChanged;

	private List<ColumnInfo> _list = new List<ColumnInfo>();

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="grid"></param>
	public ColumnInfoCollection(GridVirtual grid)
		: base(grid) {
	}

	public override int Count => _list.Count;

	/// <summary>
	/// Indexer. Returns a ColumnInfo at the specified position
	/// </summary>
	public ColumnInfo this[int p] {
		get {
			if (p < 0)
				return null;
			if (p >= _list.Count)
				return null;
			return _list[p];
		}
	}

	#region Insert/Remove Methods

	public void Add(ColumnInfo column) => Insert(Count, column);

	/// <summary>
	/// Remove a column at the speicifed position
	/// </summary>
	/// <param name="index"></param>
	public void Remove(int index) => RemoveRange(index, 1);

	/// <summary>
	/// Move a column from one position to another position
	/// </summary>
	/// <param name="currentColumnPosition"></param>
	/// <param name="newColumnPosition"></param>
	public void Move(int currentColumnPosition, int newColumnPosition) {
		if (currentColumnPosition == newColumnPosition)
			return;

		if (currentColumnPosition < newColumnPosition) {
			for (var r = currentColumnPosition; r < newColumnPosition; r++)
				Swap(r, r + 1);
		} else {
			for (var r = currentColumnPosition; r > newColumnPosition; r--)
				Swap(r, r - 1);
		}
	}

	/// <summary>
	/// Change the position of column 1 with column 2.
	/// </summary>
	/// <param name="columnIndex1"></param>
	/// <param name="columnIndex2"></param>
	public void Swap(int columnIndex1, int columnIndex2) {
		if (columnIndex1 == columnIndex2)
			return;

		var column1 = this[columnIndex1];
		var column2 = this[columnIndex2];

		_list[columnIndex1] = column2;
		_list[columnIndex2] = column1;

		PerformLayout();
	}

	/// <summary>
	/// Execute the RowHeightChanged event
	/// </summary>
	/// <param name="e"></param>
	public void OnColumnWidthChanged(ColumnInfoEventArgs e) {
		PerformLayout();

		if (ColumnWidthChanged != null)
			ColumnWidthChanged(this, e);
	}

	public int IndexOf(ColumnInfo info) => _list.IndexOf(info);

	/// <summary>
	/// Auto size the columns calculating the required size only on the rows currently visible
	/// </summary>
	public void AutoSizeView() {
		var list = Grid.Rows.RowsInsideRegion(Grid.DisplayRectangle.Y, Grid.DisplayRectangle.Height, true, false);
		if (list.Count > 0)
			AutoSize(false, list[0], list[list.Count - 1]);
	}

	/// <summary>
	/// Remove all the columns
	/// </summary>
	public void Clear() {
		if (Count > 0)
			RemoveRange(0, Count);
	}

	/// <summary>
	/// Remove the ColumnInfo at the specified positions
	/// </summary>
	/// <param name="startIndex"></param>
	/// <param name="count"></param>
	public virtual void RemoveRange(int startIndex, int count) {
		if (IsValidRange(startIndex, count) == false)
			throw new SourceGridException("Invalid index");

		var eventArgs = new IndexRangeEventArgs(startIndex, count);
		OnColumnsRemoving(eventArgs);

		_list.RemoveRange(startIndex, count);

		OnColumnsRemoved(eventArgs);

		PerformLayout();
	}

	#endregion

	/// <summary>
	/// Returns true if the range passed is valid
	/// </summary>
	/// <param name="startIndex"></param>
	/// <param name="count"></param>
	/// <returns></returns>
	public bool IsValidRange(int startIndex, int count) {
		if (startIndex < Count && startIndex >= 0 &&
			count > 0 && (startIndex + count) <= Count)
			return true;
		return false;
	}

	#region ColumnsBase

	public override int GetWidth(int column) => (IsColumnVisible(column) ? this[column].Width : 0);

	public override void SetWidth(int column, int width) => this[column].Width = width;

	public override AutoSizeMode GetAutoSizeMode(int column) => this[column].AutoSizeMode;

	public override bool IsColumnVisible(int column) => this[column].Visible;

	public override void HideColumn(int column) => this[column].Visible = false;

	public override void ShowColumn(int column) => this[column].Visible = true;

	#endregion

	/// <summary>Enumerates the current columns in their display order.</summary>
	public IEnumerator<ColumnInfo> GetEnumerator() => _list.GetEnumerator();

	#region IEnumerable Members

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	#endregion

	/// <summary>
	/// Returns true if the range passed is valid for insert method
	/// </summary>
	/// <param name="startIndex"></param>
	/// <param name="count"></param>
	/// <returns></returns>
	public bool IsValidRangeForInsert(int startIndex, int count) {
		if (startIndex <= Count && startIndex >= 0 &&
			count > 0)
			return true;
		return false;
	}

	public void Insert(int index, ColumnInfo dataGridColumn) => InsertRange(index, dataGridColumn);

	/// <summary>
	/// Insert the specified number of Columns at the specified position
	/// </summary>
	/// <param name="startIndex"></param>
	/// <param name="columns"></param>
	public void InsertRange(int startIndex, params ColumnInfo[] columns) {
		if (IsValidRangeForInsert(startIndex, columns.Length) == false)
			throw new SourceGridException("Invalid index");

		for (var c = 0; c < columns.Length; c++)
			_list.Insert(startIndex + c, columns[c]);

		PerformLayout();

		OnColumnsAdded(new IndexRangeEventArgs(startIndex, columns.Length));
	}

	protected override void OnLayout() => base.OnLayout();

	/// <summary>
	/// Fired when some columns are removed
	/// </summary>
	/// <param name="e"></param>
	protected virtual void OnColumnsRemoved(IndexRangeEventArgs e) {
		if (ColumnsRemoved != null)
			ColumnsRemoved(this, e);

		ColumnsChanged();
	}

	/// <summary>
	/// Fired before some columns are removed
	/// </summary>
	/// <param name="e"></param>
	protected virtual void OnColumnsRemoving(IndexRangeEventArgs e) {
		if (ColumnsRemoving != null)
			ColumnsRemoving(this, e);
		//Grid.OnColumnsRemoving(e);
	}

	/// <summary>
	/// Fired when the number of columns change
	/// </summary>
	/// <param name="e"></param>
	protected virtual void OnColumnsAdded(IndexRangeEventArgs e) {
		if (ColumnsAdded != null)
			ColumnsAdded(this, e);

		ColumnsChanged();
	}
}
