// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
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
	private QuadTreeNode<TBounds> _root;
	private TBounds _bounds;

	public QuadTree(int rows, int columns)
		: this(TBounds.Create(1, 1, rows, columns)) {
	}

	public QuadTree(TBounds bounds) {
		_bounds = bounds;
		_root = new QuadTreeNode<TBounds>(_bounds, 0, this);
		QuadTreeNodeDivider = new ProportionateSizeNodeDivider<TBounds>();
	}

	public IQuadTreeNodeDivider<TBounds> QuadTreeNodeDivider { get; set; }

	public TBounds Bounds => _bounds;

	public QuadTreeNode<TBounds> Root => _root;

	public int Count => _root.Count;

	public int MaxDepth => _root.MaxDepth;

	public List<TBounds> Contents => Query(_bounds);

	/// <summary>
	/// Double occupied space by creating a new root that contains the current root.
	/// </summary>
	public void Grow() {
		_root = QuadTreeNodeDivider.CreateNewRoot(_root);
		_bounds = _root.Bounds;
	}

	public QuadTree<TBounds> Insert(TBounds item) {
		_root.Insert(item);
		return this;
	}

	public QuadTree<TBounds> Insert(IEnumerable<TBounds> items) {
		foreach (var Item in items)
			_root.Insert(Item);
		return this;
	}

	public QuadTree<TBounds> Remove(TBounds range) {
		_root.Remove(range);
		return this;
	}

	public List<TBounds> Query(TBounds area) => _root.Query(area);

	public List<TBounds> Query(int row, int column) => _root.Query(row, column);

	public TBounds? QueryFirst(TBounds area) => _root.QueryFirst(area);

	public TBounds? QueryFirst(int row, int column) => _root.QueryFirst(row, column);

	public void ForEach(Action<QuadTreeNode<TBounds>> action) => _root.ForEach(action);
}
