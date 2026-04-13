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
/// In-memory B+ tree dictionary backed by object-graph nodes with List-based key, entry, and child storage.
/// </summary>
public class InMemoryBTreePlus<K, V> : BTreePlus<K, V, InMemoryBTreePlus<K, V>.Node> {
	private Node _root;

	public InMemoryBTreePlus(int order, IComparer<K> keyComparer = null)
		: base(order, keyComparer) {
	}

	#region Root Management

	protected override bool HasRoot => _root != null;

	protected override Node Root => _root;

	protected override void SetRoot(Node node) => _root = node;

	protected override void ClearRoot() => _root = null;

	#endregion

	#region Node Lifecycle

	protected override Node CreateLeafNode() => new Node(isLeaf: true, Order);

	protected override Node CreateInternalNode() => new Node(isLeaf: false, Order);

	protected override void DeleteNode(Node node) {
		// GC handles cleanup for in-memory nodes
	}

	#endregion

	#region Node Properties

	protected override bool IsLeaf(Node node) => node.IsLeaf;

	protected override int GetKeyCount(Node node) => node.IsLeaf ? node.Entries.Count : node.Keys.Count;

	protected override int GetChildCount(Node node) => node.Children?.Count ?? 0;

	#endregion

	#region Leaf Entry Operations

	protected override KeyValuePair<K, V> GetLeafEntry(Node node, int index) => node.Entries[index];

	protected override void SetLeafEntry(Node node, int index, KeyValuePair<K, V> entry) => node.Entries[index] = entry;

	protected override void InsertLeafEntry(Node node, int index, KeyValuePair<K, V> entry) => node.Entries.Insert(index, entry);

	protected override void AddLeafEntry(Node node, KeyValuePair<K, V> entry) => node.Entries.Add(entry);

	protected override void RemoveLeafEntryAt(Node node, int index) => node.Entries.RemoveAt(index);

	#endregion

	#region Internal Key Operations

	protected override K GetInternalKey(Node node, int index) => node.Keys[index];

	protected override void SetInternalKey(Node node, int index, K key) => node.Keys[index] = key;

	protected override void InsertInternalKey(Node node, int index, K key) => node.Keys.Insert(index, key);

	protected override void AddInternalKey(Node node, K key) => node.Keys.Add(key);

	protected override void RemoveInternalKeyAt(Node node, int index) => node.Keys.RemoveAt(index);

	#endregion

	#region Child Operations

	protected override Node GetChild(Node node, int index) => node.Children[index];

	protected override void SetChild(Node node, int index, Node child) => node.Children[index] = child;

	protected override void InsertChild(Node node, int index, Node child) => node.Children.Insert(index, child);

	protected override void AddChild(Node node, Node child) => node.Children.Add(child);

	protected override void RemoveChildAt(Node node, int index) => node.Children.RemoveAt(index);

	#endregion

	#region Leaf Link Operations

	protected override Node GetNextLeaf(Node leafNode) => leafNode.NextLeaf;

	protected override void SetNextLeaf(Node leafNode, Node nextLeaf) => leafNode.NextLeaf = nextLeaf;

	#endregion

	public override void Clear() {
		_root = null;
		Count = 0;
	}

	public sealed class Node {
		public Node(bool isLeaf, int order = 0) {
			IsLeaf = isLeaf;
			if (isLeaf) {
				Entries = new List<KeyValuePair<K, V>>(order > 0 ? order : 0);
			} else {
				Keys = new List<K>(order > 0 ? order : 0);
				Children = new List<Node>(order > 0 ? order + 1 : 0);
			}
		}

		public bool IsLeaf { get; }

		public List<K> Keys { get; }

		public List<KeyValuePair<K, V>> Entries { get; }

		public List<Node> Children { get; }

		public Node NextLeaf { get; set; }
	}
}
