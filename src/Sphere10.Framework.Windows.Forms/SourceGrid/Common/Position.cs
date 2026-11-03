// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.Forms.SourceGrid;

/// <summary>
/// Represents a cell position (Row, Col). Once created connot be modified
/// </summary>
[Serializable]
public struct Position {
	private int _row;
	private int _col;

	/// <summary>
	/// Empty position
	/// </summary>
	public readonly static Position Empty;

	/// <summary>
	/// An empty index constant
	/// </summary>
	public const int EmptyIndex = -1;

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="row"></param>
	/// <param name="column"></param>
	public Position(int row, int column) {
		_row = row;
		_col = column;
	}

	static Position() {
		Empty = new Position(EmptyIndex, EmptyIndex);
	}

	/// <summary>
	/// Row
	/// </summary>
	public int Row {
		get {
			return _row;
		}
	}

	/// <summary>
	/// Column
	/// </summary>
	public int Column {
		get {
			return _col;
		}
	}

	/// <summary>
	/// Returns true if the current struct is empty
	/// </summary>
	/// <returns></returns>
	public bool IsEmpty() {
		return this.Equals(Empty);
	}

	/// <summary>
	/// GetHashCode
	/// </summary>
	/// <returns></returns>
	public override int GetHashCode() {
		return Row;
	}

	/// <summary>
	///
	/// </summary>
	/// <param name="position"></param>
	/// <returns></returns>
	public bool Equals(Position position) {
		return (_col == position._col && _row == position._row);
	}

	/// <summary>
	///
	/// </summary>
	/// <param name="obj"></param>
	/// <returns></returns>
	public override bool Equals(object obj) {
		return Equals((Position)obj);
	}

	/// <summary>
	///
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public static bool operator ==(Position left, Position right) {
		return left.Equals(right);
	}

	/// <summary>
	///
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public static bool operator !=(Position left, Position right) {
		return !left.Equals(right);
	}

	/// <summary>
	///
	/// </summary>
	/// <returns></returns>
	public override string ToString() {
		return Row.ToString() + ";" + Column.ToString();
	}

	/// <summary>
	/// Returns a position with the smaller Row and the smaller column
	/// </summary>
	/// <param name="position1"></param>
	/// <param name="position2"></param>
	/// <returns></returns>
	public static Position Min(Position position1, Position position2) {
		int row, column;
		if (position1.Row < position2.Row)
			row = position1.Row;
		else
			row = position2.Row;
		if (position1.Column < position2.Column)
			column = position1.Column;
		else
			column = position2.Column;
		return new Position(row, column);
	}

	/// <summary>
	/// Returns a position with the bigger Row and the bigger column
	/// </summary>
	/// <param name="position1"></param>
	/// <param name="position2"></param>
	/// <returns></returns>
	public static Position Max(Position position1, Position position2) {
		int row, column;
		if (position1.Row > position2.Row)
			row = position1.Row;
		else
			row = position2.Row;
		if (position1.Column > position2.Column)
			column = position1.Column;
		else
			column = position2.Column;
		return new Position(row, column);
	}
}
