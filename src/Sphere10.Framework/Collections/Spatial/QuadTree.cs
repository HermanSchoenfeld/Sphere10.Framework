// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;

namespace Sphere10.Framework;

/// <summary>
/// A QuadTree is a spatial data structure that partitions 2D integer-coordinate space
/// for efficient region queries. See http://en.wikipedia.org/wiki/Quadtree
/// </summary>
/// <typeparam name="TBounds">The rectangle type used for bounds and items.</typeparam>
public class QuadTree<TBounds> where TBounds : struct, IIntegerRectangle<TBounds> {
	public QuadTree(int rows, int columns)
		: this(TBounds.Create(1, 1, rows, columns)) {
	}

	public QuadTree(TBounds bounds) {
		Bounds = bounds;
		Root = new QuadTreeNode<TBounds>(Bounds, 0, this);
		QuadTreeNodeDivider = new ProportionateSizeNodeDivider<TBounds>();
	}

	public IQuadTreeNodeDivider<TBounds> QuadTreeNodeDivider { get; set; }

	public TBounds Bounds { get; private set; }

	public QuadTreeNode<TBounds> Root { get; private set; }

	public int Count => Root.Count;

	public int MaxDepth => Root.MaxDepth;

	public List<TBounds> Contents => Query(Bounds);

	/// <summary>
	/// Double occupied space by creating a new root that contains the current root.
	/// </summary>
	public void Grow() {
		Root = QuadTreeNodeDivider.CreateNewRoot(Root);
		Bounds = Root.Bounds;
	}

	public QuadTree<TBounds> Insert(TBounds item) {
		Root.Insert(item);
		return this;
	}

	public QuadTree<TBounds> Insert(IEnumerable<TBounds> items) {
		foreach (var Item in items)
			Root.Insert(Item);
		return this;
	}

	public QuadTree<TBounds> Remove(TBounds range) {
		Root.Remove(range);
		return this;
	}

	public List<TBounds> Query(TBounds area) => Root.Query(area);

	public List<TBounds> Query(int row, int column) => Root.Query(row, column);

	public TBounds? QueryFirst(TBounds area) => Root.QueryFirst(area);

	public TBounds? QueryFirst(int row, int column) => Root.QueryFirst(row, column);

	public void ForEach(Action<QuadTreeNode<TBounds>> action) => Root.ForEach(action);
}
