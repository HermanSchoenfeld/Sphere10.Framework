// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework;

namespace Sphere10.Framework.Windows.Forms.SourceGrid;

/// <summary>
/// Represents a range of cells. Once created cannot be modified. This Range has always Start in the Top-Left, and End in the Bottom-Right (see Normalize method).
/// </summary>
[Serializable]
public struct CellRange : IIntegerRectangle<CellRange> {
	private Position _start, _end;

	/// <summary>
	/// Represents an empty range
	/// </summary>
	public readonly static CellRange Empty;

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="start"></param>
	/// <param name="end"></param>
	public CellRange(Position start, Position end)
		: this(start, end, true) {
	}

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="startRow"></param>
	/// <param name="startCol"></param>
	/// <param name="endRow"></param>
	/// <param name="endCol"></param>
	public CellRange(int startRow, int startCol, int endRow, int endCol) {
		_start = new Position(startRow, startCol);
		_end = new Position(endRow, endCol);

		Normalize();
	}

	private CellRange(Position start, Position end, bool check) {
		_start = start;
		_end = end;

		if (check)
			Normalize();
	}

	static CellRange() {
		Empty = new CellRange(Position.Empty, Position.Empty, false);
	}

	/// <summary>
	/// Construct a Range of a single cell
	/// </summary>
	/// <param name="singlePosition"></param>
	public CellRange(Position singlePosition)
		: this(singlePosition, singlePosition, false) {
	}

	public Position Start {
		get {
			return _start;
		}
		//set{m_Start = value; Normalize();}
	}

	public Position End {
		get {
			return _end;
		}
		//set{m_End = value; Normalize();}
	}

	/// <summary>
	/// Sets or Gets the columns count (End.Column-Start.Column)
	/// </summary>
	public int ColumnsCount {
		get {
			return (_end.Column - _start.Column) + 1;
		}
		set {
			if (value <= 0) //facendo questo controllo non serve richiamare Normalize
				throw new SourceGridException("Invalid columns count");
			_end = new Position(_end.Row, _start.Column + value - 1);
		}
	}

	/// <summary>
	/// Sets or Gets the rows count (End.Row-Start.Row)
	/// </summary>
	public int RowsCount {
		get {
			return (_end.Row - _start.Row) + 1;
		}
		set {
			if (value <= 0) //facendo questo controllo non serve richiamare Normalize
				throw new SourceGridException("Invalid columns count");
			_end = new Position(_start.Row + value - 1, _end.Column);
		}
	}

	int IIntegerRectangle<CellRange>.StartRow {
		get {
			return _start.Row;
		}
	}

	int IIntegerRectangle<CellRange>.StartColumn {
		get {
			return _start.Column;
		}
	}

	int IIntegerRectangle<CellRange>.EndRow {
		get {
			return _end.Row;
		}
	}

	int IIntegerRectangle<CellRange>.EndColumn {
		get {
			return _end.Column;
		}
	}

	public static CellRange FromPosition(Position startPosition) {
		return new CellRange(startPosition);
	}

	public static CellRange From(Position startPosition, int rowCount, int colCount) {
		var range = new CellRange(startPosition);
		range.RowsCount = rowCount;
		range.ColumnsCount = colCount;
		return range;
	}

	/// <summary>
	/// Move the current range to the specified position, leaving the current ColumnsCount and RowsCount
	/// </summary>
	/// <param name="startPosition"></param>
	public void MoveTo(Position startPosition) {
		var colCount = ColumnsCount;
		var rowCount = RowsCount;
		_start = startPosition;
		RowsCount = rowCount;
		ColumnsCount = colCount;
	}

	/// <summary>
	/// Returns true if the specified row is present in the current range.
	/// </summary>
	/// <param name="row"></param>
	/// <returns></returns>
	public bool ContainsRow(int row) {
		return (row >= _start.Row &&
				row <= _end.Row);
	}

	/// <summary>
	/// Returns true if the specified column is present in the current range.
	/// </summary>
	/// <param name="column"></param>
	/// <returns></returns>
	public bool ContainsColumn(int column) {
		return (column >= _start.Column &&
				column <= _end.Column);
	}

	/// <summary>
	/// Returns true if the specified cell position is present in the current range.
	/// </summary>
	/// <param name="position"></param>
	/// <returns></returns>
	public bool Contains(Position position) {
		return (position.Row >= _start.Row &&
				position.Column >= _start.Column &&
				position.Row <= _end.Row &&
				position.Column <= _end.Column);
	}

	/// <summary>
	/// Returns true if the specified range is present in the current range.
	/// </summary>
	/// <param name="range"></param>
	/// <returns></returns>
	public bool Contains(CellRange range) {
		return (Contains(range.Start) &&
				Contains(range.End));
	}

	/// <summary>
	///
	/// </summary>
	/// <returns></returns>
	public override int GetHashCode() {
		return _start.Row;
	}

	/// <summary>
	///
	/// </summary>
	/// <param name="range"></param>
	/// <returns></returns>
	public bool Equals(CellRange range) {
		return (Start.Equals(range.Start) && End.Equals(range.End));
	}

	/// <summary>
	///
	/// </summary>
	/// <param name="obj"></param>
	/// <returns></returns>
	public override bool Equals(object obj) {
		return Equals((CellRange)obj);
	}

	/// <summary>
	///
	/// </summary>
	/// <returns></returns>
	public PositionCollection GetCellsPositions() {
		var list = new PositionCollection();
		for (var r = Start.Row; r <= End.Row; r++)
			for (var c = Start.Column; c <= End.Column; c++)
				list.Add(new Position(r, c));

		return list;
	}

	/// <summary>
	///
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public static bool operator ==(CellRange left, CellRange right) {
		return left.Equals(right);
	}

	/// <summary>
	///
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public static bool operator !=(CellRange left, CellRange right) {
		return !left.Equals(right);
	}

	/// <summary>
	///
	/// </summary>
	/// <returns></returns>
	public override string ToString() {
		return Start.ToString() + " to " + End.ToString();
	}

	/// <summary>
	/// Returns a range with the smaller Start and the bigger End. The Union of the 2 Range. If one of the range is empty then the return is the other range.
	/// </summary>
	/// <param name="range1"></param>
	/// <param name="range2"></param>
	/// <returns></returns>
	public static RangeRegion Union(CellRange range1, CellRange range2) {
		var range = new RangeRegion();
		range.Add(range1);
		range.Add(range2);
		return range;
	}

	/// <summary>
	/// Returns a range with the smaller Start and the bigger End. The Union of the 2 Range. If one of the range is empty then the return is the other range.
	/// </summary>
	/// <param name="range1"></param>
	/// <param name="range2"></param>
	/// <returns></returns>
	public static CellRange GetBounds(CellRange range1, CellRange range2) {
		if (range1.IsEmpty())
			return range2;
		if (range2.IsEmpty())
			return range1;
		return new CellRange(Position.Min(range1.Start, range2.Start), Position.Max(range1.End, range2.End), false);
	}

	/// <summary>
	/// Returns true if the specified range intersects (one or more cells) with the current range.
	/// If one of the range is empty then the return is false.
	/// </summary>
	/// <param name="range1"></param>
	/// <param name="range2"></param>
	/// <returns></returns>
	public static bool IntersectsWith(CellRange range1, CellRange range2) {
		return Intersect(range1, range2).IsEmpty() == false;
	}

	/// <summary>
	/// Returns true if the specified range intersects (one or more cells) with the current range.
	/// If one of the range is empty then the return is false.
	/// </summary>
	/// <param name="range"></param>
	/// <returns></returns>
	public bool IntersectsWith(CellRange range) {
		return IntersectsWith(this, range);
	}

	/// <summary>
	/// Return all the cells that don't intersect with the specified cells. (Remove the specified cells from the current cells ad returns the remaining cells)
	/// </summary>
	/// <param name="range"></param>
	/// <returns></returns>
	public RangeRegion Exclude(CellRange range) {
		RangeRegion excluded;

		var intersection = Intersect(range);
		if (intersection.IsEmpty())
			excluded = new RangeRegion(this);
		else {
			excluded = new RangeRegion();

			//Top Left
			if (this.Start.Row < intersection.Start.Row &&
				this.Start.Column < intersection.Start.Column)
				excluded.Add(new CellRange(this.Start.Row, this.Start.Column, intersection.Start.Row - 1, intersection.Start.Column - 1));

			//Top
			if (this.Start.Row < intersection.Start.Row)
				excluded.Add(new CellRange(this.Start.Row, intersection.Start.Column, intersection.Start.Row - 1, intersection.End.Column));

			//Top Right
			if (this.Start.Row < intersection.Start.Row &&
				this.End.Column > intersection.End.Column)
				excluded.Add(new CellRange(this.Start.Row, intersection.End.Column + 1, intersection.Start.Row - 1, this.End.Column));

			//----------

			//Left
			if (this.Start.Column < intersection.Start.Column)
				excluded.Add(new CellRange(intersection.Start.Row, this.Start.Column, intersection.End.Row, intersection.Start.Column - 1));

			//Right
			if (this.End.Column > intersection.End.Column)
				excluded.Add(new CellRange(intersection.Start.Row, intersection.End.Column + 1, intersection.End.Row, this.End.Column));

			//--------

			//Bottom Left
			if (this.End.Row > intersection.End.Row &&
				this.Start.Column < intersection.Start.Column)
				excluded.Add(new CellRange(intersection.End.Row + 1, this.Start.Column, this.End.Row, intersection.Start.Column - 1));

			//Bottom
			if (this.End.Row > intersection.End.Row)
				excluded.Add(new CellRange(intersection.End.Row + 1, intersection.Start.Column, this.End.Row, intersection.End.Column));

			//Bottom Right
			if (this.End.Row > intersection.End.Row &&
				this.End.Column > intersection.End.Column)
				excluded.Add(new CellRange(intersection.End.Row + 1, intersection.End.Column + 1, this.End.Row, this.End.Column));
		}

		return excluded;
	}

	/// <summary>
	/// Returns the intersection between the 2 Range. If one of the range is empty then the return is empty.
	/// </summary>
	/// <param name="range1"></param>
	/// <param name="range2"></param>
	/// <returns></returns>
	public static CellRange Intersect(CellRange range1, CellRange range2) {
		if (range1.IsEmpty() || range2.IsEmpty())
			return CellRange.Empty;

		var startNew = Position.Max(range1.Start, range2.Start);
		var endNew = Position.Min(range1.End, range2.End);

		if (startNew.Column > endNew.Column ||
			startNew.Row > endNew.Row)
			return CellRange.Empty;
		return new CellRange(startNew, endNew, false);
	}

	/// <summary>
	/// Determines if the current range is empty
	/// </summary>
	/// <returns></returns>
	public bool IsEmpty() {
		return (Start.IsEmpty() || End.IsEmpty());
	}

	/// <summary>
	/// Returns the intersection between the 2 Range. If one of the range is empty then the return is empty.
	/// </summary>
	/// <param name="range"></param>
	/// <returns></returns>
	public CellRange Intersect(CellRange range) {
		return Intersect(this, range);
	}

	bool IIntegerRectangle<CellRange>.Contains(int row, int column) {
		return Contains(new Position(row, column));
	}

	static CellRange IIntegerRectangle<CellRange>.Create(int startRow, int startColumn, int endRow, int endColumn) {
		return new CellRange(startRow, startColumn, endRow, endColumn);
	}

	/// <summary>
	/// Check and fix the range to always have Start smaller than End
	/// </summary>
	private void Normalize() {
		int minRow, minCol, maxRow, maxCol;

		if (_start.Row < _end.Row)
			minRow = _start.Row;
		else
			minRow = _end.Row;

		if (_start.Column < _end.Column)
			minCol = _start.Column;
		else
			minCol = _end.Column;

		if (_start.Row > _end.Row)
			maxRow = _start.Row;
		else
			maxRow = _end.Row;

		if (_start.Column > _end.Column)
			maxCol = _start.Column;
		else
			maxCol = _end.Column;

		_start = new Position(minRow, minCol);
		_end = new Position(maxRow, maxCol);
	}
}
