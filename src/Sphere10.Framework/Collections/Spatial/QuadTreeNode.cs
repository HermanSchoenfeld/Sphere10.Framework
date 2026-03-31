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
/// A node within a <see cref="QuadTree{TBounds}"/>. Each node stores items whose bounds
/// don't fit entirely within any single child quadrant.
/// </summary>
public class QuadTreeNode<TBounds> where TBounds : struct, IIntegerRectangle<TBounds> {
	private TBounds _bounds;
	private readonly List<TBounds> _contents = new();
	private readonly List<QuadTreeNode<TBounds>> _nodes = new(4);

	public QuadTreeNode(TBounds bounds) {
		_bounds = bounds;
	}

	public QuadTreeNode(TBounds bounds, int currentDepth, QuadTree<TBounds> quadTree)
		: this(bounds) {
		QuadTree = quadTree;
		Depth = currentDepth + 1;
	}

	public int Depth { get; set; }

	public QuadTree<TBounds> QuadTree { get; set; }

	public TBounds Bounds => _bounds;

	public List<TBounds> Contents => _contents;

	public List<QuadTreeNode<TBounds>> Nodes => _nodes;

	public bool IsEmpty => _nodes.Count == 0 && _contents.Count == 0;

	public int Count {
		get {
			var Count = 0;
			foreach (var Node in _nodes)
				Count += Node.Count;
			Count += _contents.Count;
			return Count;
		}
	}

	public int MaxDepth {
		get {
			var CurrentDepth = Depth;
			foreach (var Node in _nodes) {
				var NodeDepth = Node.MaxDepth;
				if (NodeDepth > CurrentDepth)
					CurrentDepth = NodeDepth;
			}
			return CurrentDepth;
		}
	}

	public List<TBounds> SubTreeContents {
		get {
			var Results = new List<TBounds>();
			foreach (var Node in _nodes)
				Results.AddRange(Node.SubTreeContents);
			Results.AddRange(_contents);
			return Results;
		}
	}

	public void Insert(TBounds item) {
		if (!_bounds.Contains(item))
			throw new ArgumentException("Item is out of the bounds of this quadtree node");

		if (_nodes.Count == 0)
			QuadTree.QuadTreeNodeDivider.CreateSubNodes(this);

		foreach (var Node in _nodes) {
			if (Node.Bounds.Contains(item)) {
				Node.Insert(item);
				return;
			}
		}
		_contents.Add(item);
	}

	public bool Remove(TBounds range) {
		if (!_bounds.Contains(range))
			throw new ArgumentException("Range is out of the bounds of this quadtree node");

		foreach (var Node in _nodes) {
			if (Node.Bounds.Contains(range))
				return Node.Remove(range);
		}

		for (var I = 0; I < _contents.Count; I++) {
			if (_contents[I].Equals(range)) {
				_contents.RemoveAt(I);
				return true;
			}
		}
		return false;
	}

	public List<TBounds> Query(TBounds queryArea) => QueryInternal(queryArea, false);

	public TBounds? QueryFirst(TBounds queryArea) {
		var Results = QueryInternal(queryArea, true);
		return Results.Count == 0 ? null : Results[0];
	}

	public List<TBounds> QueryInternal(TBounds queryArea, bool stopOnFirst) {
		var Results = new List<TBounds>();

		foreach (var Item in _contents) {
			if (queryArea.IntersectsWith(Item)) {
				Results.Add(Item);
				if (stopOnFirst)
					return Results;
			}
		}

		foreach (var Node in _nodes) {
			if (Node.IsEmpty)
				continue;

			if (Node.Bounds.Contains(queryArea)) {
				Results.AddRange(Node.QueryInternal(queryArea, stopOnFirst));
				break;
			}

			if (queryArea.Contains(Node.Bounds)) {
				Results.AddRange(Node.SubTreeContents);
				continue;
			}

			if (Node.Bounds.IntersectsWith(queryArea))
				Results.AddRange(Node.QueryInternal(queryArea, stopOnFirst));
		}

		return Results;
	}

	public List<TBounds> Query(int row, int column) {
		var Results = new List<TBounds>();

		foreach (var Item in _contents) {
			if (Item.Contains(row, column))
				Results.Add(Item);
		}

		foreach (var Node in _nodes) {
			if (Node.IsEmpty)
				continue;
			if (Node.Bounds.Contains(row, column)) {
				Results.AddRange(Node.Query(row, column));
				break;
			}
		}

		return Results;
	}

	public TBounds? QueryFirst(int row, int column) {
		foreach (var Item in _contents) {
			if (Item.Contains(row, column))
				return Item;
		}

		foreach (var Node in _nodes) {
			if (Node.IsEmpty)
				continue;
			if (Node.Bounds.Contains(row, column))
				return Node.QueryFirst(row, column);
		}
		return null;
	}

	public void ForEach(Action<QuadTreeNode<TBounds>> action) {
		action(this);
		foreach (var Node in _nodes)
			Node.ForEach(action);
	}
}
