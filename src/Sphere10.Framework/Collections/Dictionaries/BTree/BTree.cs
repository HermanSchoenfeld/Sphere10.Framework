// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Sphere10.Framework;

/// <summary>
/// Abstract base for B-tree dictionaries with configurable order and pluggable node storage.
///
/// Semantics of order:
/// - Max children per node = order
/// - Max keys per node      = order - 1
/// - Min keys per non-root  = ceil(order / 2.0) - 1
///
/// Subclasses provide node storage and mutation via abstract methods, enabling
/// in-memory, stream-mapped, buffer-backed, or other storage strategies.
/// </summary>
/// <typeparam name="K">Key type.</typeparam>
/// <typeparam name="V">Value type.</typeparam>
/// <typeparam name="TNode">Handle type representing a node in the storage backend.</typeparam>
public abstract class BTree<K, V, TNode> : BTreeBase<K, V> {
	private readonly int _maxKeys;
	private readonly int _minKeys;

	protected BTree(int order, IComparer<K> keyComparer = null)
		: base(order, keyComparer) {
		_maxKeys = order - 1;
		_minKeys = ((order + 1) / 2) - 1; // ceil(order / 2) - 1
	}

	protected int MaxKeys => _maxKeys;

	protected int MinKeys => _minKeys;

	#region Abstract — Root Management

	protected abstract bool HasRoot { get; }

	protected abstract TNode Root { get; }

	protected abstract void SetRoot(TNode node);

	protected abstract void ClearRoot();

	#endregion

	#region Abstract — Node Lifecycle

	protected abstract TNode CreateLeafNode();

	protected abstract TNode CreateInternalNode();

	protected abstract void DeleteNode(TNode node);

	#endregion

	#region Abstract — Node Properties

	protected abstract bool IsLeaf(TNode node);

	protected abstract int GetKeyCount(TNode node);

	protected abstract int GetChildCount(TNode node);

	#endregion

	#region Abstract — Key Operations

	protected abstract KeyValuePair<K, V> GetKey(TNode node, int index);

	protected abstract void SetKey(TNode node, int index, KeyValuePair<K, V> entry);

	protected abstract void InsertKey(TNode node, int index, KeyValuePair<K, V> entry);

	protected abstract void AddKey(TNode node, KeyValuePair<K, V> entry);

	protected abstract void RemoveKeyAt(TNode node, int index);

	#endregion

	#region Abstract — Child Operations

	protected abstract TNode GetChild(TNode node, int index);

	protected abstract void SetChild(TNode node, int index, TNode child);

	protected abstract void InsertChild(TNode node, int index, TNode child);

	protected abstract void AddChild(TNode node, TNode child);

	protected abstract void RemoveChildAt(TNode node, int index);

	#endregion

	public override void Set(K key, V value, bool overwriteIfExists) {
		if (!HasRoot) {
			var Root = CreateLeafNode();
			AddKey(Root, new KeyValuePair<K, V>(key, value));
			SetRoot(Root);
			Count = 1;
			DebugValidate();
			return;
		}

		var Split = InsertRecursive(Root, key, value, overwriteIfExists, out var InsertedNewKey);
		if (Split != null) {
			var NewRoot = CreateInternalNode();
			AddKey(NewRoot, Split.Promoted);
			AddChild(NewRoot, Split.Left);
			AddChild(NewRoot, Split.Right);
			SetRoot(NewRoot);
		}

		if (InsertedNewKey)
			Count++;

		DebugValidate();
	}

	public override bool Remove(K key) {
		if (!HasRoot)
			return false;

		var Removed = RemoveRecursive(Root, key);
		if (!Removed)
			return false;

		Count--;

		if (GetKeyCount(Root) == 0) {
			if (IsLeaf(Root)) {
				var OldRoot = Root;
				ClearRoot();
				DeleteNode(OldRoot);
			} else {
				var NewRoot = GetChild(Root, 0);
				var OldRoot = Root;
				SetRoot(NewRoot);
				DeleteNode(OldRoot);
			}
		}

		DebugValidate();
		return true;
	}

	public override void Clear() {
		if (HasRoot)
			DeleteSubtree(Root);
		ClearRoot();
		Count = 0;
	}

	public override bool TryGetValue(K key, out V value) {
		if (!HasRoot) {
			value = default;
			return false;
		}
		var Node = Root;
		while (true) {
			var Index = FindKeyIndex(Node, key, out var Found);
			if (Found) {
				value = GetKey(Node, Index).Value;
				return true;
			}
			if (IsLeaf(Node))
				break;
			Node = GetChild(Node, Index);
		}

		value = default;
		return false;
	}

	public override IEnumerator<KeyValuePair<K, V>> GetEnumerator() {
		if (!HasRoot)
			yield break;

		switch (TraversalType) {
			case TreeTraversalType.PreOrder:
				foreach (var Item in EnumeratePreOrder(Root))
					yield return Item;
				break;
			case TreeTraversalType.PostOrder:
				foreach (var Item in EnumeratePostOrder(Root))
					yield return Item;
				break;
			case TreeTraversalType.LevelOrder:
				foreach (var Item in EnumerateLevelOrder(Root))
					yield return Item;
				break;
			default:
				foreach (var Item in EnumerateInOrder(Root))
					yield return Item;
				break;
		}
	}

	public override bool Validate(out string error) {
		if (!HasRoot) {
			if (Count != 0) {
				error = "Count is non-zero while root is null.";
				return false;
			}
			error = null;
			return true;
		}

		var LeafDepth = -1;
		var VisitedCount = 0;
		var Ok = ValidateNode(
			Root,
			isRoot: true,
			minExclusive: default,
			hasMin: false,
			maxExclusive: default,
			hasMax: false,
			depth: 0,
			ref LeafDepth,
			ref VisitedCount,
			out error);

		if (!Ok)
			return false;

		if (VisitedCount != Count) {
			error = $"Reachable key count {VisitedCount} does not match Count {Count}.";
			return false;
		}

		error = null;
		return true;
	}

	protected virtual int FindKeyIndex(TNode node, K key, out bool found) {
		var Lo = 0;
		var Hi = GetKeyCount(node) - 1;
		while (Lo <= Hi) {
			var Mid = Lo + ((Hi - Lo) >> 1);
			var Cmp = Compare(key, GetKey(node, Mid).Key);
			if (Cmp == 0) {
				found = true;
				return Mid;
			}
			if (Cmp < 0)
				Hi = Mid - 1;
			else
				Lo = Mid + 1;
		}
		found = false;
		return Lo;
	}

	protected virtual SplitResult SplitNode(TNode node) {
		var KeyCount = GetKeyCount(node);
		var MedianIndex = KeyCount / 2;
		var Promoted = GetKey(node, MedianIndex);
		var NodeIsLeaf = IsLeaf(node);

		var Left = NodeIsLeaf ? CreateLeafNode() : CreateInternalNode();
		var Right = NodeIsLeaf ? CreateLeafNode() : CreateInternalNode();

		for (var i = 0; i < MedianIndex; i++)
			AddKey(Left, GetKey(node, i));
		for (var i = MedianIndex + 1; i < KeyCount; i++)
			AddKey(Right, GetKey(node, i));

		if (!NodeIsLeaf) {
			var ChildCount = GetChildCount(node);
			for (var i = 0; i <= MedianIndex; i++)
				AddChild(Left, GetChild(node, i));
			for (var i = MedianIndex + 1; i < ChildCount; i++)
				AddChild(Right, GetChild(node, i));
		}

		DeleteNode(node);
		return new SplitResult(Left, Promoted, Right);
	}

	protected virtual void MergeChildren(TNode parent, int leftIndex) {
		var Left = GetChild(parent, leftIndex);
		var Right = GetChild(parent, leftIndex + 1);
		var Separator = GetKey(parent, leftIndex);

		AddKey(Left, Separator);
		var RightKeyCount = GetKeyCount(Right);
		for (var i = 0; i < RightKeyCount; i++)
			AddKey(Left, GetKey(Right, i));

		if (!IsLeaf(Left)) {
			var RightChildCount = GetChildCount(Right);
			for (var i = 0; i < RightChildCount; i++)
				AddChild(Left, GetChild(Right, i));
		}

		RemoveKeyAt(parent, leftIndex);
		RemoveChildAt(parent, leftIndex + 1);
		DeleteNode(Right);
	}

	protected virtual void BorrowFromLeft(TNode parent, int childIndex) {
		var Child = GetChild(parent, childIndex);
		var Left = GetChild(parent, childIndex - 1);
		var LeftKeyCount = GetKeyCount(Left);

		InsertKey(Child, 0, GetKey(parent, childIndex - 1));
		SetKey(parent, childIndex - 1, GetKey(Left, LeftKeyCount - 1));
		RemoveKeyAt(Left, LeftKeyCount - 1);

		if (!IsLeaf(Left)) {
			var LeftChildCount = GetChildCount(Left);
			var MovedChild = GetChild(Left, LeftChildCount - 1);
			RemoveChildAt(Left, LeftChildCount - 1);
			InsertChild(Child, 0, MovedChild);
		}
	}

	protected virtual void BorrowFromRight(TNode parent, int childIndex) {
		var Child = GetChild(parent, childIndex);
		var Right = GetChild(parent, childIndex + 1);

		AddKey(Child, GetKey(parent, childIndex));
		SetKey(parent, childIndex, GetKey(Right, 0));
		RemoveKeyAt(Right, 0);

		if (!IsLeaf(Right)) {
			var MovedChild = GetChild(Right, 0);
			RemoveChildAt(Right, 0);
			AddChild(Child, MovedChild);
		}
	}

	private SplitResult InsertRecursive(TNode node, K key, V value, bool overwriteIfExists, out bool insertedNewKey) {
		insertedNewKey = false;

		var Index = FindKeyIndex(node, key, out var Found);
		if (Found) {
			if (!overwriteIfExists)
				throw new InvalidOperationException($"Key {key} already exists in tree.");
			SetKey(node, Index, new KeyValuePair<K, V>(key, value));
			return null;
		}

		if (IsLeaf(node)) {
			InsertKey(node, Index, new KeyValuePair<K, V>(key, value));
			insertedNewKey = true;
			return GetKeyCount(node) > _maxKeys ? SplitNode(node) : null;
		}

		var ChildSplit = InsertRecursive(GetChild(node, Index), key, value, overwriteIfExists, out insertedNewKey);
		if (ChildSplit == null)
			return null;

		InsertKey(node, Index, ChildSplit.Promoted);
		SetChild(node, Index, ChildSplit.Left);
		InsertChild(node, Index + 1, ChildSplit.Right);

		return GetKeyCount(node) > _maxKeys ? SplitNode(node) : null;
	}

	private bool RemoveRecursive(TNode node, K key) {
		var Index = FindKeyIndex(node, key, out var Found);

		if (Found) {
			if (IsLeaf(node)) {
				RemoveKeyAt(node, Index);
				return true;
			}

			// Replace with in-order predecessor, then remove it from the left subtree
			var Predecessor = GetMax(GetChild(node, Index));
			SetKey(node, Index, Predecessor);
			var Removed = RemoveRecursive(GetChild(node, Index), Predecessor.Key);
			Debug.Assert(Removed);
			FixChildUnderflow(node, Index);
			return true;
		}

		if (IsLeaf(node))
			return false;

		var ChildIndex = Index;
		if (!RemoveRecursive(GetChild(node, ChildIndex), key))
			return false;
		FixChildUnderflow(node, ChildIndex);
		return true;
	}

	private KeyValuePair<K, V> GetMax(TNode node) {
		while (!IsLeaf(node))
			node = GetChild(node, GetChildCount(node) - 1);
		return GetKey(node, GetKeyCount(node) - 1);
	}

	private void FixChildUnderflow(TNode parent, int childIndex) {
		if (IsLeaf(parent))
			return;
		if (childIndex < 0 || childIndex >= GetChildCount(parent))
			return;

		var Child = GetChild(parent, childIndex);
		if (GetKeyCount(Child) >= _minKeys)
			return;

		if (childIndex > 0 && GetKeyCount(GetChild(parent, childIndex - 1)) > _minKeys) {
			BorrowFromLeft(parent, childIndex);
			return;
		}

		if (childIndex < GetChildCount(parent) - 1 && GetKeyCount(GetChild(parent, childIndex + 1)) > _minKeys) {
			BorrowFromRight(parent, childIndex);
			return;
		}

		if (childIndex > 0)
			MergeChildren(parent, childIndex - 1);
		else if (childIndex < GetChildCount(parent) - 1)
			MergeChildren(parent, childIndex);
	}

	private void DeleteSubtree(TNode node) {
		if (!IsLeaf(node)) {
			var ChildCount = GetChildCount(node);
			for (var i = 0; i < ChildCount; i++)
				DeleteSubtree(GetChild(node, i));
		}
		DeleteNode(node);
	}

	private IEnumerable<KeyValuePair<K, V>> EnumerateInOrder(TNode node) {
		if (IsLeaf(node)) {
			var KeyCount = GetKeyCount(node);
			for (var i = 0; i < KeyCount; i++)
				yield return GetKey(node, i);
			yield break;
		}

		var NodeKeyCount = GetKeyCount(node);
		for (var i = 0; i < NodeKeyCount; i++) {
			foreach (var Item in EnumerateInOrder(GetChild(node, i)))
				yield return Item;
			yield return GetKey(node, i);
		}
		foreach (var Item in EnumerateInOrder(GetChild(node, NodeKeyCount)))
			yield return Item;
	}

	private IEnumerable<KeyValuePair<K, V>> EnumeratePreOrder(TNode node) {
		var KeyCount = GetKeyCount(node);
		for (var i = 0; i < KeyCount; i++)
			yield return GetKey(node, i);
		if (IsLeaf(node))
			yield break;
		var ChildCount = GetChildCount(node);
		for (var i = 0; i < ChildCount; i++)
			foreach (var Item in EnumeratePreOrder(GetChild(node, i)))
				yield return Item;
	}

	private IEnumerable<KeyValuePair<K, V>> EnumeratePostOrder(TNode node) {
		if (!IsLeaf(node)) {
			var ChildCount = GetChildCount(node);
			for (var i = 0; i < ChildCount; i++)
				foreach (var Item in EnumeratePostOrder(GetChild(node, i)))
					yield return Item;
		}
		var KeyCount = GetKeyCount(node);
		for (var i = 0; i < KeyCount; i++)
			yield return GetKey(node, i);
	}

	private IEnumerable<KeyValuePair<K, V>> EnumerateLevelOrder(TNode root) {
		var Queue = new Queue<TNode>();
		Queue.Enqueue(root);
		while (Queue.Count > 0) {
			var Node = Queue.Dequeue();
			var KeyCount = GetKeyCount(Node);
			for (var i = 0; i < KeyCount; i++)
				yield return GetKey(Node, i);
			if (!IsLeaf(Node)) {
				var ChildCount = GetChildCount(Node);
				for (var i = 0; i < ChildCount; i++)
					Queue.Enqueue(GetChild(Node, i));
			}
		}
	}

	private bool ValidateNode(
		TNode node,
		bool isRoot,
		K minExclusive,
		bool hasMin,
		K maxExclusive,
		bool hasMax,
		int depth,
		ref int leafDepth,
		ref int totalKeys,
		out string error) {

		var KeyCount = GetKeyCount(node);

		if (KeyCount == 0 && !isRoot) {
			error = "Non-root node contains zero keys.";
			return false;
		}

		if (KeyCount > _maxKeys) {
			error = $"Node contains {KeyCount} keys, exceeding max {_maxKeys}.";
			return false;
		}

		if (!isRoot && KeyCount < _minKeys) {
			error = $"Non-root node contains {KeyCount} keys, below min {_minKeys}.";
			return false;
		}

		for (var i = 1; i < KeyCount; i++) {
			if (Compare(GetKey(node, i - 1).Key, GetKey(node, i).Key) >= 0) {
				error = "Node keys are not strictly increasing.";
				return false;
			}
		}

		for (var i = 0; i < KeyCount; i++) {
			var Key = GetKey(node, i).Key;
			if (hasMin && Compare(Key, minExclusive) <= 0) {
				error = "A key violates its lower bound.";
				return false;
			}
			if (hasMax && Compare(Key, maxExclusive) >= 0) {
				error = "A key violates its upper bound.";
				return false;
			}
		}

		totalKeys += KeyCount;

		if (IsLeaf(node)) {
			if (GetChildCount(node) != 0) {
				error = "Leaf node contains children.";
				return false;
			}
			if (leafDepth < 0)
				leafDepth = depth;
			else if (leafDepth != depth) {
				error = "Leaves are not all at the same depth.";
				return false;
			}
			error = null;
			return true;
		}

		var ChildCount = GetChildCount(node);
		if (ChildCount != KeyCount + 1) {
			error = "Internal node child count must equal key count + 1.";
			return false;
		}

		for (var i = 0; i < ChildCount; i++) {
			var ChildHasMin = hasMin;
			var ChildMin = minExclusive;
			var ChildHasMax = hasMax;
			var ChildMax = maxExclusive;

			if (i == 0) {
				ChildHasMax = true;
				ChildMax = GetKey(node, 0).Key;
			} else if (i == ChildCount - 1) {
				ChildHasMin = true;
				ChildMin = GetKey(node, KeyCount - 1).Key;
			} else {
				ChildHasMin = true;
				ChildMin = GetKey(node, i - 1).Key;
				ChildHasMax = true;
				ChildMax = GetKey(node, i).Key;
			}

			if (!ValidateNode(GetChild(node, i), false, ChildMin, ChildHasMin, ChildMax, ChildHasMax, depth + 1, ref leafDepth, ref totalKeys, out error))
				return false;
		}

		error = null;
		return true;
	}

	protected sealed class SplitResult {
		public SplitResult(TNode left, KeyValuePair<K, V> promoted, TNode right) {
			Left = left;
			Promoted = promoted;
			Right = right;
		}

		public TNode Left { get; }
		public KeyValuePair<K, V> Promoted { get; }
		public TNode Right { get; }
	}
}
