// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework;

/// <summary>
/// Represents a 2D axis-aligned rectangle defined by integer coordinates.
/// Used as a constraint for generic spatial data structures such as <see cref="QuadTree{TBounds}"/>.
/// </summary>
public interface IIntegerRectangle<TSelf> where TSelf : IIntegerRectangle<TSelf> {
	int StartRow { get; }
	int StartColumn { get; }
	int EndRow { get; }
	int EndColumn { get; }
	int RowsCount { get; }
	int ColumnsCount { get; }

	bool Contains(TSelf other);
	bool Contains(int row, int column);
	bool IntersectsWith(TSelf other);

	static abstract TSelf Create(int startRow, int startColumn, int endRow, int endColumn);
}
