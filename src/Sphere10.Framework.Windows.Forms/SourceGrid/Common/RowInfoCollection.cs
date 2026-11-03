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
/// Collection of RowInfo
/// </summary>
public abstract class RowInfoCollection : RowsBase, IEnumerable<RowInfo> {
	/// <summary>
	/// Fired when the number of rows change
	/// </summary>
	public event IndexRangeEventHandler RowsAdded;

	/// <summary>
	/// Fired when some rows are removed
	/// </summary>
	public event IndexRangeEventHandler RowsRemoved;

	/// <summary>
	/// Fired before some rows are removed
	/// </summary>
	public event IndexRangeEventHandler RowsRemoving;

	/// <summary>
	/// Fired when the user change the Height property of one of the Row
	/// </summary>
	public event RowInfoEventHandler RowHeightChanged;

	private List<RowInfo> _list = new List<RowInfo>();

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="grid"></param>
	public RowInfoCollection(GridVirtual grid)
		: base(grid) {
		HiddenRowCoordinator = new RowInfoCollectoinHiddenRowCoordinator(this);
	}

	public override int Count => _list.Count;

	/// <summary>
	/// Indexer. Returns a RowInfo at the specified position
	/// </summary>
	public RowInfo this[int p] {
		get {
			if (p < 0)
				return null;
			if (p >= _list.Count)
				return null;
			return _list[p];
		}
	}

	/// <summary>
	/// Remove a row at the speicifed position
	/// </summary>
	/// <param name="index"></param>
	public void Remove(int index) {
		RemoveRange(index, 1);
	}

	/// <summary>
	/// Move a row from one position to another position
	/// </summary>
	/// <param name="currentRowPosition"></param>
	/// <param name="newRowPosition"></param>
	public void Move(int currentRowPosition, int newRowPosition) {
		if (currentRowPosition == newRowPosition)
			return;

		if (currentRowPosition < newRowPosition) {
			for (var r = currentRowPosition; r < newRowPosition; r++)
				Swap(r, r + 1);
		} else {
			for (var r = currentRowPosition; r > newRowPosition; r--)
				Swap(r, r - 1);
		}
	}

	/// <summary>
	/// Change the position of row 1 with row 2.
	/// </summary>
	/// <param name="rowIndex1"></param>
	/// <param name="rowIndex2"></param>
	public virtual void Swap(int rowIndex1, int rowIndex2) {
		if (rowIndex1 == rowIndex2)
			return;

		var row1 = this[rowIndex1];
		var row2 = this[rowIndex2];

		_list[rowIndex1] = row2;
		_list[rowIndex2] = row1;

		PerformLayout();
	}

	/// <summary>
	/// Execute the RowHeightChanged event
	/// </summary>
	/// <param name="e"></param>
	public void OnRowHeightChanged(RowInfoEventArgs e) {
		PerformLayout();

		if (RowHeightChanged != null)
			RowHeightChanged(this, e);
	}

	public int IndexOf(RowInfo info) {
		return _list.IndexOf(info);
	}

	/// <summary>
	/// Auto size the rows calculating the required size only on the columns currently visible
	/// </summary>
	public void AutoSizeView() {
		var list = Grid.Columns.ColumnsInsideRegion(Grid.DisplayRectangle.X, Grid.DisplayRectangle.Width, true, false);
		if (list.Count > 0)
			AutoSize(false, list[0], list[list.Count - 1]);
	}

	/// <summary>
	/// Remove all the columns
	/// </summary>
	public void Clear() {
		if (Count == 0)
			return;
		this.Grid.LinkedControls.Clear();
		RemoveRange(0, Count);
	}

	/// <summary>
	/// Remove the RowInfo at the specified positions
	/// </summary>
	/// <param name="startIndex"></param>
	/// <param name="count"></param>
	public virtual void RemoveRange(int startIndex, int count) {
		if (IsValidRange(startIndex, count) == false)
			throw new SourceGridException("Invalid index");

		var eventArgs = new IndexRangeEventArgs(startIndex, count);
		OnRowsRemoving(eventArgs);

		_list.RemoveRange(startIndex, count);

		OnRowsRemoved(eventArgs);

		PerformLayout();
	}

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

	// RowsBase
	public override int GetHeight(int row) {
		return this[row].Height;
	}

	public override void SetHeight(int row, int height) {
		this[row].Height = height;
	}

	public override AutoSizeMode GetAutoSizeMode(int row) {
		return this[row].AutoSizeMode;
	}

	// IEnumerable<RowInfo> Members
	public IEnumerator<RowInfo> GetEnumerator() {
		return _list.GetEnumerator();
	}

	protected override void OnLayout() {
		base.OnLayout();
	}

	// Insert/Remove Methods
	/// <summary>
	/// Insert the specified number of rows at the specified position
	/// </summary>
	/// <param name="startIndex"></param>
	/// <param name="rows"></param>
	protected void InsertRange(int startIndex, RowInfo[] rows) {
		if (IsValidRangeForInsert(startIndex, rows.Length) == false)
			throw new SourceGridException("Invalid index");

		for (var r = 0; r < rows.Length; r++)
			_list.Insert(startIndex + r, rows[r]);

		PerformLayout();

		OnRowsAdded(new IndexRangeEventArgs(startIndex, rows.Length));
	}

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

	/// <summary>
	/// Fired when the number of rows change
	/// </summary>
	/// <param name="e"></param>
	protected virtual void OnRowsAdded(IndexRangeEventArgs e) {
		if (RowsAdded != null)
			RowsAdded(this, e);

		RowsChanged();
	}

	/// <summary>
	/// Fired when some rows are removed
	/// </summary>
	/// <param name="e"></param>
	protected virtual void OnRowsRemoved(IndexRangeEventArgs e) {
		if (RowsRemoved != null)
			RowsRemoved(this, e);

		RowsChanged();
	}

	/// <summary>
	/// Fired before some rows are removed
	/// </summary>
	/// <param name="e"></param>
	protected virtual void OnRowsRemoving(IndexRangeEventArgs e) {
		if (RowsRemoving != null)
			RowsRemoving(this, e);
		//Grid.OnRowsRemoving(e);
	}

	// IEnumerable Members
	IEnumerator IEnumerable.GetEnumerator() {
		return _list.GetEnumerator();
	}
}
