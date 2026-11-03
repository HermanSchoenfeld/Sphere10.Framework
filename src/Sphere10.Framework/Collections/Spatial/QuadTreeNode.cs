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
/// A node within a <see cref="QuadTree{TBounds}"/>. Each node stores items whose bounds
/// don't fit entirely within any single child quadrant.
/// </summary>
public class QuadTreeNode<TBounds> where TBounds : struct, IIntegerRectangle<TBounds> {
	public QuadTreeNode(TBounds bounds) {
		Bounds = bounds;
	}

	public QuadTreeNode(TBounds bounds, int currentDepth, QuadTree<TBounds> quadTree)
		: this(bounds) {
		QuadTree = quadTree;
		Depth = currentDepth + 1;
	}

	public int Depth { get; set; }

	public QuadTree<TBounds> QuadTree { get; set; }

	public TBounds Bounds { get; }

	public List<TBounds> Contents { get; } = new();

	public List<QuadTreeNode<TBounds>> Nodes { get; } = new(4);

	public bool IsEmpty => Nodes.Count == 0 && Contents.Count == 0;

	public int Count {
		get {
			var Count = 0;
			foreach (var Node in Nodes)
				Count += Node.Count;
			Count += Contents.Count;
			return Count;
		}
	}

	public int MaxDepth {
		get {
			var CurrentDepth = Depth;
			foreach (var Node in Nodes) {
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
			foreach (var Node in Nodes)
				Results.AddRange(Node.SubTreeContents);
			Results.AddRange(Contents);
			return Results;
		}
	}

	public void Insert(TBounds item) {
		if (!Bounds.Contains(item))
			throw new ArgumentException("Item is out of the bounds of this quadtree node");

		if (Nodes.Count == 0)
			QuadTree.QuadTreeNodeDivider.CreateSubNodes(this);

		foreach (var Node in Nodes) {
			if (Node.Bounds.Contains(item)) {
				Node.Insert(item);
				return;
			}
		}
		Contents.Add(item);
	}

	public bool Remove(TBounds range) {
		if (!Bounds.Contains(range))
			throw new ArgumentException("Range is out of the bounds of this quadtree node");

		foreach (var Node in Nodes) {
			if (Node.Bounds.Contains(range))
				return Node.Remove(range);
		}

		for (var I = 0; I < Contents.Count; I++) {
			if (Contents[I].Equals(range)) {
				Contents.RemoveAt(I);
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

		foreach (var Item in Contents) {
			if (queryArea.IntersectsWith(Item)) {
				Results.Add(Item);
				if (stopOnFirst)
					return Results;
			}
		}

		foreach (var Node in Nodes) {
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

		foreach (var Item in Contents) {
			if (Item.Contains(row, column))
				Results.Add(Item);
		}

		foreach (var Node in Nodes) {
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
		foreach (var Item in Contents) {
			if (Item.Contains(row, column))
				return Item;
		}

		foreach (var Node in Nodes) {
			if (Node.IsEmpty)
				continue;
			if (Node.Bounds.Contains(row, column))
				return Node.QueryFirst(row, column);
		}
		return null;
	}

	public void ForEach(Action<QuadTreeNode<TBounds>> action) {
		action(this);
		foreach (var Node in Nodes)
			Node.ForEach(action);
	}
}
