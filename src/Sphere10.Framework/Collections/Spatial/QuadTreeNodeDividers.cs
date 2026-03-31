// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework;

/// <summary>
/// Divides quad tree nodes by considering proportionality of rows vs columns.
/// When the bounds are highly rectangular (many more rows than columns), it divides
/// only along the row axis to avoid extremely narrow column splits. This is more
/// efficient for grid-like data where row count greatly exceeds column count.
/// </summary>
public class ProportionateSizeNodeDivider<TBounds> : IQuadTreeNodeDivider<TBounds>
	where TBounds : struct, IIntegerRectangle<TBounds> {

	public QuadTreeNode<TBounds> CreateNewRoot(QuadTreeNode<TBounds> currentRoot) {
		var Bounds = currentRoot.Bounds;
		var StartRow = Bounds.StartRow;
		var StartCol = Bounds.StartColumn;
		var HalfCol = Bounds.ColumnsCount;
		var HalfRow = Bounds.RowsCount;
		var Depth = currentRoot.Depth;
		var Tree = currentRoot.QuadTree;

		var NewRoot = new QuadTreeNode<TBounds>(
			TBounds.Create(StartRow, StartCol, StartRow + HalfRow * 2 - 1, StartCol + HalfCol * 2 - 1),
			currentRoot.Depth,
			Tree);

		NewRoot.Nodes.Add(currentRoot);
		NewRoot.Nodes.Add(new QuadTreeNode<TBounds>(
			TBounds.Create(StartRow, StartCol + HalfCol, StartRow + HalfRow - 1, StartCol + HalfCol * 2 - 1),
			Depth, Tree));
		NewRoot.Nodes.Add(new QuadTreeNode<TBounds>(
			TBounds.Create(StartRow + HalfRow, StartCol, StartRow + HalfRow * 2 - 1, StartCol + HalfCol - 1),
			Depth, Tree));
		NewRoot.Nodes.Add(new QuadTreeNode<TBounds>(
			TBounds.Create(StartRow + HalfRow, StartCol + HalfCol, StartRow + HalfRow * 2 - 1, StartCol + HalfCol * 2 - 1),
			Depth, Tree));
		return NewRoot;
	}

	public void CreateSubNodes(QuadTreeNode<TBounds> parentNode) {
		if (parentNode.Bounds.ColumnsCount * parentNode.Bounds.RowsCount <= 10)
			return;

		if (parentNode.Bounds.ColumnsCount * 2 < parentNode.Bounds.RowsCount)
			CreateNotProportionate(parentNode);
		else
			CreateProportionate(parentNode);
	}

	private void CreateNotProportionate(QuadTreeNode<TBounds> parentNode) {
		var Bounds = parentNode.Bounds;
		var StartRow = Bounds.StartRow;
		var StartCol = Bounds.StartColumn;
		var HalfCol = Bounds.ColumnsCount;
		var HalfRow = Bounds.RowsCount / 2;
		var Depth = parentNode.Depth;
		var Tree = parentNode.QuadTree;

		parentNode.Nodes.Add(new QuadTreeNode<TBounds>(
			TBounds.Create(StartRow, StartCol, StartRow + HalfRow - 1, StartCol + HalfCol - 1),
			Depth, Tree));
		parentNode.Nodes.Add(new QuadTreeNode<TBounds>(
			TBounds.Create(StartRow + HalfRow, StartCol, StartRow + HalfRow * 2 - 1, StartCol + HalfCol - 1),
			Depth, Tree));
	}

	private void CreateProportionate(QuadTreeNode<TBounds> parentNode) {
		var Bounds = parentNode.Bounds;
		var StartRow = Bounds.StartRow;
		var StartCol = Bounds.StartColumn;
		var HalfCol = Bounds.ColumnsCount / 2;
		var HalfRow = Bounds.RowsCount / 2;
		var Depth = parentNode.Depth;
		var Tree = parentNode.QuadTree;

		parentNode.Nodes.Add(new QuadTreeNode<TBounds>(
			TBounds.Create(StartRow, StartCol, StartRow + HalfRow - 1, StartCol + HalfCol - 1),
			Depth, Tree));
		parentNode.Nodes.Add(new QuadTreeNode<TBounds>(
			TBounds.Create(StartRow, StartCol + HalfCol, StartRow + HalfRow - 1, StartCol + HalfCol * 2 - 1),
			Depth, Tree));
		parentNode.Nodes.Add(new QuadTreeNode<TBounds>(
			TBounds.Create(StartRow + HalfRow, StartCol, StartRow + HalfRow * 2 - 1, StartCol + HalfCol - 1),
			Depth, Tree));
		parentNode.Nodes.Add(new QuadTreeNode<TBounds>(
			TBounds.Create(StartRow + HalfRow, StartCol + HalfCol, StartRow + HalfRow * 2 - 1, StartCol + HalfCol * 2 - 1),
			Depth, Tree));
	}
}

/// <summary>
/// Simple divider that always splits into 4 equal quadrants. Less efficient for
/// highly rectangular regions. Use <see cref="ProportionateSizeNodeDivider{TBounds}"/> instead.
/// </summary>
public class HalfSizeNodeDivider<TBounds> : IQuadTreeNodeDivider<TBounds>
	where TBounds : struct, IIntegerRectangle<TBounds> {

	public QuadTreeNode<TBounds> CreateNewRoot(QuadTreeNode<TBounds> currentRoot) =>
		new ProportionateSizeNodeDivider<TBounds>().CreateNewRoot(currentRoot);

	public void CreateSubNodes(QuadTreeNode<TBounds> parentNode) {
		var Bounds = parentNode.Bounds;
		if (Bounds.ColumnsCount * Bounds.RowsCount <= 10)
			return;

		var StartRow = Bounds.StartRow;
		var StartCol = Bounds.StartColumn;
		var HalfCol = Bounds.ColumnsCount / 2;
		var HalfRow = Bounds.RowsCount / 2;
		var Depth = parentNode.Depth;
		var Tree = parentNode.QuadTree;

		parentNode.Nodes.Add(new QuadTreeNode<TBounds>(
			TBounds.Create(StartRow, StartCol, StartRow + HalfRow - 1, StartCol + HalfCol - 1),
			Depth, Tree));
		parentNode.Nodes.Add(new QuadTreeNode<TBounds>(
			TBounds.Create(StartRow, StartCol + HalfCol, StartRow + HalfRow - 1, StartCol + HalfCol * 2 - 1),
			Depth, Tree));
		parentNode.Nodes.Add(new QuadTreeNode<TBounds>(
			TBounds.Create(StartRow + HalfRow, StartCol, StartRow + HalfRow * 2 - 1, StartCol + HalfCol - 1),
			Depth, Tree));
		parentNode.Nodes.Add(new QuadTreeNode<TBounds>(
			TBounds.Create(StartRow + HalfRow, StartCol + HalfCol, StartRow + HalfRow * 2 - 1, StartCol + HalfCol * 2 - 1),
			Depth, Tree));
	}
}
