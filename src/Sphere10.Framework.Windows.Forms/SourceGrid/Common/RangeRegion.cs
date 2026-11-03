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
using Sphere10.Framework.Windows.Forms.SourceGrid.Selection;

namespace Sphere10.Framework.Windows.Forms.SourceGrid;

/// <summary>
/// RangeRegion is a collection of range that are never overlying each other.
/// </summary>
[Serializable]
public class RangeRegion : ICollection<CellRange> {
	public event RangeRegionCancelEventHandler AddingRange;

	public event RangeRegionCancelEventHandler RemovingRange;

	public event RangeRegionEventHandler AddedRange;

	public event RangeRegionEventHandler RemovedRange;

	public event RangeRegionChangedEventHandler Changed;

	//N.B. Il codice di questa classe è scritto in maniera tale da non avere mai sovrapposizioni.
	// Quindi una cella potrà essere contenuta in un solo range.
	// Nel caso vengano inseririti range sovrapposti questi vengono spezzati e reinseriti.
	private RangeCollection _rangeCollection = new RangeCollection();

	/// <summary>
	/// Variabile che indica se lo stato interno della classe è stato modificato, serve per poter ottimizzare alcune verifiche.
	/// </summary>
	private bool _validated = false;

	// Constructors
	public RangeRegion() {
	}

	public RangeRegion(Position position) {
		if (position.IsEmpty() == false)
			_rangeCollection.Add(new CellRange(position));
	}

	public RangeRegion(CellRange range) {
		if (range.IsEmpty() == false)
			_rangeCollection.Add(range);
	}

	/// <summary>
	/// Copy constructor
	/// </summary>
	/// <param name="other"></param>
	public RangeRegion(RangeRegion other) {
		_rangeCollection.AddRange(other._rangeCollection);
	}

	public int Count {
		get {
			return _rangeCollection.Count;
		}
	}

	public bool IsReadOnly {
		get {
			return false;
		}
	}

	// GetCellsPositions, GetRanges
	/// <summary>
	/// Returns the range at the specific position
	/// </summary>
	/// <param name="index"></param>
	/// <returns></returns>
	public CellRange this[int index] {
		get {
			return _rangeCollection[index];
		}
	}

	// GetRows/GetColumns
	/// <summary>
	/// Returns all the selected rows index
	/// </summary>
	/// <returns></returns>
	public virtual int[] GetRowsIndex() {
		var merger = new RangeMergerByRows();
		foreach (var range in this)
			merger.AddRange(range);

		var indexes = merger.GetRowsIndex();
		var result = new int[indexes.Count];
		indexes.CopyTo(result, 0);

		return result;
	}

	/// <summary>
	/// Returns all the selected columns index
	/// </summary>
	/// <returns></returns>
	public virtual int[] GetColumnsIndex() {
		var indexList = new List<int>();

		for (var rangeIndex = 0; rangeIndex < Count; rangeIndex++) {
			for (var column = this[rangeIndex].Start.Column; column <= this[rangeIndex].End.Column; column++) {
				if (indexList.Contains(column) == false)
					indexList.Add(column);
			}
		}

		return indexList.ToArray();
	}

	/// <summary>
	/// Indicates if the specified row is selected
	/// </summary>
	/// <param name="row"></param>
	/// <returns></returns>
	public virtual bool ContainsRow(int row) {
		if (IsEmpty())
			return false;

		//Range
		for (var index = 0; index < _rangeCollection.Count; index++) {
			if (_rangeCollection[index].ContainsRow(row))
				return true;
		}

		return false;
	}

	/// <summary>
	/// Indicates if the specified column is selected
	/// </summary>
	/// <param name="column"></param>
	/// <returns></returns>
	public virtual bool ContainsColumn(int column) {
		if (IsEmpty())
			return false;

		//Range
		for (var index = 0; index < _rangeCollection.Count; index++) {
			if (_rangeCollection[index].ContainsColumn(column))
				return true;
		}

		return false;
	}

	// Intersect
	/// <summary>
	/// Indicates if the specified range of cells is selected
	/// </summary>
	/// <param name="sourceRange"></param>
	/// <returns></returns>
	public virtual bool IntersectsWith(CellRange sourceRange) {
		if (sourceRange.IsEmpty() || IsEmpty())
			return false;

		var range = Intersect(sourceRange);
		return !range.IsEmpty();
	}

	public override string ToString() {
		var buffer = "RangeRegion";
		for (var index = 0; index < _rangeCollection.Count; index++)
			buffer += " | " + _rangeCollection[index].ToString();
		return buffer;
	}

	public void CopyTo(CellRange[] array, int arrayIndex) {
		_rangeCollection.CopyTo(array, arrayIndex);
	}

	// IEnumerable<CellRange> Members
	public IEnumerator<CellRange> GetEnumerator() {
		return _rangeCollection.GetEnumerator();
	}

	/// <summary>
	/// Reset the object to its original state. It is similar to the Clear method but doesn't call any events when removeing the saved positions, usually used when refreshing the cells with new data.
	/// To simply clear the object use the Clear method, only use this method when you want to force a reset of the object without calling additional methods.
	/// </summary>
	protected virtual void ResetRange() {
		_validated = false;
		_rangeCollection.Clear();
	}

	// IEnumerable Members
	System.Collections.IEnumerator System.Collections.IEnumerable.GetEnumerator() {
		return _rangeCollection.GetEnumerator();
	}

	// IsEmpty
	public virtual bool IsEmpty() {
		if (_rangeCollection.Count == 0)
			return true;
		return false;
	}

	/// <summary>
	/// Returns a Collection of cells that represents the current class
	/// </summary>
	/// <returns></returns>
	public virtual PositionCollection GetCellsPositions() {
		var positions = new PositionCollection();

		//Range
		for (var index = 0; index < _rangeCollection.Count; index++)
			positions.AddRange(_rangeCollection[index].GetCellsPositions());

		return positions;
	}

	// Contains
	/// <summary>
	/// Indicates if the specified cell is selected
	/// </summary>
	/// <param name="cell"></param>
	/// <returns></returns>
	public virtual bool Contains(Position cell) {
		if (cell.IsEmpty() || IsEmpty())
			return false;

		//Range
		for (var index = 0; index < _rangeCollection.Count; index++) {
			if (_rangeCollection[index].Contains(cell))
				return true;
		}

		return false;
	}

	/// <summary>
	/// Indicates if the specified range of cells is selected
	/// </summary>
	/// <param name="range"></param>
	/// <returns></returns>
	public virtual bool Contains(CellRange range) {
		if (range.IsEmpty() || IsEmpty())
			return false;

		if (range.ColumnsCount == 1 && range.RowsCount == 1)
			return Contains(range.Start);

		//Range
		for (var index = 0; index < _rangeCollection.Count; index++) {
			if (_rangeCollection[index].Contains(range))
				return true;
		}

		var positions = range.GetCellsPositions();
		for (var iPos = 0; iPos < positions.Count; iPos++) {
			if (Contains(positions[iPos]) == false)
				return false;
		}
		return true;
	}

	/// <summary>
	/// Indicates if the specified range of cells is selected
	/// </summary>
	/// <param name="range"></param>
	/// <returns></returns>
	public virtual bool Contains(RangeRegion range) {
		if (range.IsEmpty() || IsEmpty())
			return false;

		var positions = range.GetCellsPositions();
		for (var index = 0; index < positions.Count; index++) {
			if (Contains(positions[index]) == false)
				return false;
		}

		return true;
	}

	/// <summary>
	/// Returns a non contiguous range of cells of the intersection between the current range and the specified range.
	/// </summary>
	/// <param name="sourceRange"></param>
	/// <returns></returns>
	public virtual RangeRegion Intersect(CellRange sourceRange) {
		var range = new RangeRegion();

		if (sourceRange.IsEmpty() == false && IsEmpty() == false) {
			//Range
			for (var index = 0; index < _rangeCollection.Count; index++) {
				var intersectRange = sourceRange.Intersect(_rangeCollection[index]);
				if (intersectRange.IsEmpty() == false)
					range._rangeCollection.Add(intersectRange);
			}
		}

		return range;
	}

	public RangeRegion Intersect(RangeRegion range) {
		var result = new RangeRegion();
		for (var rToCheck = 0; rToCheck < range._rangeCollection.Count; rToCheck++) {
			var rangeToCheck = range._rangeCollection[rToCheck];
			var intersect = Intersect(rangeToCheck);
			result._rangeCollection.AddRange(intersect._rangeCollection);
		}
		return result;
	}

	// Exclude
	/// <summary>
	/// Returns the cells of the current range that don't intersect with the specified range
	/// </summary>
	/// <param name="sourceRange"></param>
	/// <returns></returns>
	public RangeRegion Exclude(CellRange sourceRange) {
		var range = new RangeRegion();

		for (var index = 0; index < _rangeCollection.Count; index++) {
			var excludedSubRange = _rangeCollection[index].Exclude(sourceRange);
			range._rangeCollection.AddRange(excludedSubRange._rangeCollection);
		}

		return range;
	}

	public RangeRegion Exclude(RangeRegion range) {
		var excludedRange = new RangeRegion(this);
		if (excludedRange.IsEmpty() == false) {
			for (var rToCheck = 0; rToCheck < range._rangeCollection.Count; rToCheck++)
				excludedRange = excludedRange.Exclude(range._rangeCollection[rToCheck]);
		}

		return excludedRange;
	}

	// Add/Remove/Clear
	/// <summary>
	/// Remove all the cells
	/// </summary>
	public void Clear() {
		Remove(new RangeRegion(this));
	}

	/// <summary>
	/// Remove all the cells excluse the specified range
	/// </summary>
	/// <param name="rangeToLeave"></param>
	public void Clear(CellRange rangeToLeave) {
		var region = new RangeRegion(this);
		region.Remove(rangeToLeave);
		Remove(region);
	}

	/// <summary>
	/// Add the specified cell and add the cell to the collection.
	/// </summary>
	/// <param name="cell"></param>
	/// <returns>Returns true if sucesfully added</returns>
	public bool Add(Position cell) {
		return Add(new CellRange(cell));
	}

	/// <summary>
	/// Remove from the collection the specified cell
	/// </summary>
	/// <param name="cell"></param>
	/// <returns>Returns true if sucesfully removed</returns>
	public bool Remove(Position cell) {
		return Remove(new CellRange(cell));
	}

	/// <summary>
	/// Add the specified Range of cells
	/// </summary>
	/// <param name="range"></param>
	/// <returns>Returns true if sucesfully added</returns>
	public bool Add(CellRange range) {
		return InternalAdd(new RangeRegion(range));
	}

	/// <summary>
	/// Remove from the collection the specified range of cells
	/// </summary>
	/// <param name="range"></param>
	/// <returns>Returns true if sucesfully removed</returns>
	public bool Remove(CellRange range) {
		return InternalRemove(new RangeRegion(range));
	}

	/// <summary>
	/// Add the specified ranges of cells
	/// </summary>
	/// <param name="range"></param>
	/// <returns></returns>
	public bool Add(RangeRegion range) {
		return InternalAdd(range);
	}

	/// <summary>
	/// Remove the specified ranges of cells
	/// </summary>
	/// <param name="range"></param>
	/// <returns></returns>
	public bool Remove(RangeRegion range) {
		return InternalRemove(range);
	}

	/// <summary>
	/// Prende un range che è già stato filtrato con solo le celle non presenti nell'attuale range
	/// </summary>
	/// <param name="range"></param>
	/// <returns></returns>
	private bool InternalAdd(RangeRegion range) {
		if (Contains(range))
			return true;

		if (range.Contains(this)) {
			//change all the contents with the new range

			var existingRange = new RangeRegion(this);

			_rangeCollection.Clear();
			_rangeCollection.AddRange(range._rangeCollection);

			range = range.Exclude(existingRange);
		} else {
			range = range.Exclude(this);
			if (range.IsEmpty())
				return true; //il range è vuoto
			range._validated = true;

			var args = new RangeRegionCancelEventArgs(range);
			OnAddingRange(args); //calling this method the range can change
			if (args.Cancel)
				return false;

			if (range._validated == false) {
				range = range.Exclude(this);
				if (range.IsEmpty())
					return true; //il range è vuoto
			}

			for (var rToAdd = 0; rToAdd < range._rangeCollection.Count; rToAdd++) {
				var rangeToAdd = range._rangeCollection[rToAdd];
				_rangeCollection.Add(rangeToAdd);
			}
		}

		OnAddedRange(new RangeRegionCancelEventArgs(range));

		_validated = false;

		return true;
	}

	/// <summary>
	/// Prende un range che è già stato filtrato con solo le celle presenti nell'attuale range
	/// </summary>
	/// <param name="range"></param>
	/// <returns></returns>
	private bool InternalRemove(RangeRegion range) {
		range = Intersect(range);
		if (range.IsEmpty())
			return true; //il range non è presente
		range._validated = true;

		var args = new RangeRegionCancelEventArgs(range);
		OnRemovingRange(args); //calling this method the range can change
		if (args.Cancel)
			return false;

		if (range._validated == false) {
			range = Intersect(range);
			if (range.IsEmpty())
				return true; //il range non è presente
		}

		_rangeCollection = Exclude(range)._rangeCollection;

		OnRemovedRange(args);

		_validated = false;

		return true;
	}

	// Events
	public virtual void OnAddingRange(RangeRegionCancelEventArgs e) {
		if (AddingRange != null)
			AddingRange(this, e);
	}

	public virtual void OnRemovingRange(RangeRegionCancelEventArgs e) {
		if (RemovingRange != null)
			RemovingRange(this, e);
	}

	public virtual void OnAddedRange(RangeRegionEventArgs e) {
		if (AddedRange != null)
			AddedRange(this, e);

		OnChanged(new RangeRegionChangedEventArgs(e.RangeRegion, null));
	}

	public virtual void OnRemovedRange(RangeRegionEventArgs e) {
		if (RemovedRange != null)
			RemovedRange(this, e);

		OnChanged(new RangeRegionChangedEventArgs(null, e.RangeRegion));
	}

	public virtual void OnChanged(RangeRegionChangedEventArgs e) {
		if (Changed != null)
			Changed(this, e);
	}

	// ICollection<CellRange> Members
	void ICollection<CellRange>.Add(CellRange item) {
		this.Add(item);
	}
}
