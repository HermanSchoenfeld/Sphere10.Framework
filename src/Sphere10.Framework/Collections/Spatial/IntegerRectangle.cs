// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework;

/// <summary>
/// Default struct implementation of <see cref="IIntegerRectangle{TSelf}"/> for use with <see cref="QuadTree{TBounds}"/>.
/// </summary>
[Serializable]
public readonly struct IntegerRectangle : IIntegerRectangle<IntegerRectangle>, IEquatable<IntegerRectangle> {

	public IntegerRectangle(int startRow, int startColumn, int endRow, int endColumn) {
		StartRow = Math.Min(startRow, endRow);
		StartColumn = Math.Min(startColumn, endColumn);
		EndRow = Math.Max(startRow, endRow);
		EndColumn = Math.Max(startColumn, endColumn);
	}

	public int StartRow { get; }
	public int StartColumn { get; }
	public int EndRow { get; }
	public int EndColumn { get; }
	public int RowsCount => EndRow - StartRow + 1;
	public int ColumnsCount => EndColumn - StartColumn + 1;

	public bool Contains(IntegerRectangle other) =>
		other.StartRow >= StartRow && other.EndRow <= EndRow &&
		other.StartColumn >= StartColumn && other.EndColumn <= EndColumn;

	public bool Contains(int row, int column) =>
		row >= StartRow && row <= EndRow &&
		column >= StartColumn && column <= EndColumn;

	public bool IntersectsWith(IntegerRectangle other) =>
		StartRow <= other.EndRow && EndRow >= other.StartRow &&
		StartColumn <= other.EndColumn && EndColumn >= other.StartColumn;

	public static IntegerRectangle Create(int startRow, int startColumn, int endRow, int endColumn) =>
		new(startRow, startColumn, endRow, endColumn);

	public bool Equals(IntegerRectangle other) =>
		StartRow == other.StartRow && StartColumn == other.StartColumn &&
		EndRow == other.EndRow && EndColumn == other.EndColumn;

	public override bool Equals(object obj) => obj is IntegerRectangle other && Equals(other);

	public override int GetHashCode() => HashCode.Combine(StartRow, StartColumn, EndRow, EndColumn);

	public static bool operator ==(IntegerRectangle left, IntegerRectangle right) => left.Equals(right);

	public static bool operator !=(IntegerRectangle left, IntegerRectangle right) => !left.Equals(right);

	public override string ToString() => $"({StartRow},{StartColumn}) to ({EndRow},{EndColumn})";
}
