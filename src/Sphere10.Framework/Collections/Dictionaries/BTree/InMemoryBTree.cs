// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;

namespace Sphere10.Framework;

/// <summary>
/// In-memory B-tree dictionary backed by object-graph nodes with List-based key and child storage.
/// </summary>
public class InMemoryBTree<K, V> : BTree<K, V, InMemoryBTree<K, V>.Node> {
	private Node _root;

	public InMemoryBTree(int order, IComparer<K> keyComparer = null)
		: base(order, keyComparer) {
	}

	#region Root Management

	protected override bool HasRoot => _root != null;

	protected override Node Root => _root;

	protected override void SetRoot(Node node) => _root = node;

	protected override void ClearRoot() => _root = null;

	#endregion

	#region Node Lifecycle

	protected override Node CreateLeafNode() => new Node(isLeaf: true);

	protected override Node CreateInternalNode() => new Node(isLeaf: false);

	protected override void DeleteNode(Node node) {
		// GC handles cleanup for in-memory nodes
	}

	#endregion

	#region Node Properties

	protected override bool IsLeaf(Node node) => node.IsLeaf;

	protected override int GetKeyCount(Node node) => node.Keys.Count;

	protected override int GetChildCount(Node node) => node.Children.Count;

	#endregion

	#region Key Operations

	protected override KeyValuePair<K, V> GetKey(Node node, int index) => node.Keys[index];

	protected override void SetKey(Node node, int index, KeyValuePair<K, V> entry) => node.Keys[index] = entry;

	protected override void InsertKey(Node node, int index, KeyValuePair<K, V> entry) => node.Keys.Insert(index, entry);

	protected override void AddKey(Node node, KeyValuePair<K, V> entry) => node.Keys.Add(entry);

	protected override void RemoveKeyAt(Node node, int index) => node.Keys.RemoveAt(index);

	#endregion

	#region Child Operations

	protected override Node GetChild(Node node, int index) => node.Children[index];

	protected override void SetChild(Node node, int index, Node child) => node.Children[index] = child;

	protected override void InsertChild(Node node, int index, Node child) => node.Children.Insert(index, child);

	protected override void AddChild(Node node, Node child) => node.Children.Add(child);

	protected override void RemoveChildAt(Node node, int index) => node.Children.RemoveAt(index);

	#endregion

	public override void Clear() {
		_root = null;
		Count = 0;
	}

	public sealed class Node {
		public Node(bool isLeaf) {
			IsLeaf = isLeaf;
			Keys = new List<KeyValuePair<K, V>>();
			Children = new List<Node>();
		}

		public bool IsLeaf { get; }
		public List<KeyValuePair<K, V>> Keys { get; }
		public List<Node> Children { get; }
	}
}
