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
/// Abstract base for B+ tree dictionaries with configurable order and pluggable node storage.
/// Data entries are stored only in leaf nodes, which are linked for efficient sequential access.
/// Internal nodes hold separator keys that guide searches.
/// </summary>
/// <typeparam name="K">Key type.</typeparam>
/// <typeparam name="V">Value type.</typeparam>
/// <typeparam name="TNode">Handle type representing a node in the storage backend.</typeparam>
public abstract class BTreePlus<K, V, TNode> : BTreeBase<K, V> {
	private readonly int _maxInternalKeys;
	private readonly int _maxLeafKeys;
	private readonly int _minInternalKeys;
	private readonly int _minLeafKeys;

	protected BTreePlus(int order, IComparer<K> keyComparer = null)
		: base(order, keyComparer) {
		_maxInternalKeys = order - 1;
		_maxLeafKeys = order - 1;
		_minInternalKeys = ((order + 1) / 2) - 1;
		_minLeafKeys = order / 2;
	}

	protected int MaxInternalKeys => _maxInternalKeys;

	protected int MaxLeafKeys => _maxLeafKeys;

	protected int MinInternalKeys => _minInternalKeys;

	protected int MinLeafKeys => _minLeafKeys;

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

	#region Abstract — Leaf Entry Operations

	protected abstract KeyValuePair<K, V> GetLeafEntry(TNode node, int index);

	protected abstract void SetLeafEntry(TNode node, int index, KeyValuePair<K, V> entry);

	protected abstract void InsertLeafEntry(TNode node, int index, KeyValuePair<K, V> entry);

	protected abstract void AddLeafEntry(TNode node, KeyValuePair<K, V> entry);

	protected abstract void RemoveLeafEntryAt(TNode node, int index);

	#endregion

	#region Abstract — Internal Key Operations

	protected abstract K GetInternalKey(TNode node, int index);

	protected abstract void SetInternalKey(TNode node, int index, K key);

	protected abstract void InsertInternalKey(TNode node, int index, K key);

	protected abstract void AddInternalKey(TNode node, K key);

	protected abstract void RemoveInternalKeyAt(TNode node, int index);

	#endregion

	#region Abstract — Child Operations

	protected abstract TNode GetChild(TNode node, int index);

	protected abstract void SetChild(TNode node, int index, TNode child);

	protected abstract void InsertChild(TNode node, int index, TNode child);

	protected abstract void AddChild(TNode node, TNode child);

	protected abstract void RemoveChildAt(TNode node, int index);

	#endregion

	#region Abstract — Leaf Link Operations

	protected abstract TNode GetNextLeaf(TNode leafNode);

	protected abstract void SetNextLeaf(TNode leafNode, TNode nextLeaf);

	#endregion

	public override void Set(K key, V value, bool overwriteIfExists) {
		if (!HasRoot) {
			var Root = CreateLeafNode();
			AddLeafEntry(Root, new KeyValuePair<K, V>(key, value));
			SetRoot(Root);
			Count = 1;
			DebugValidate();
			return;
		}

		var Split = InsertRecursive(Root, key, value, overwriteIfExists, out var InsertedNewKey);
		if (Split != null) {
			var NewRoot = CreateInternalNode();
			AddInternalKey(NewRoot, Split.PromotedKey);
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

		if (HasRoot && !IsLeaf(Root))
			RebuildInternalSeparators(Root);

		if (HasRoot && GetKeyCount(Root) == 0) {
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

		var Leaf = FindLeaf(Root, key);
		var Index = FindLeafKeyIndex(Leaf, key, out var Found);
		if (Found) {
			value = GetLeafEntry(Leaf, Index).Value;
			return true;
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
				foreach (var Item in EnumerateInOrder())
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
		var TotalLeafEntries = 0;
		var Leaves = new List<TNode>();
		if (!ValidateNode(
			Root,
			isRoot: true,
			minInclusive: default,
			hasMin: false,
			maxExclusive: default,
			hasMax: false,
			depth: 0,
			ref LeafDepth,
			ref TotalLeafEntries,
			Leaves,
			out error
		))
			return false;

		if (TotalLeafEntries != Count) {
			error = $"Reachable key count {TotalLeafEntries} does not match Count {Count}.";
			return false;
		}

		for (var i = 0; i < Leaves.Count; i++) {
			var ExpectedNext = i < Leaves.Count - 1 ? Leaves[i + 1] : default;
			var ActualNext = GetNextLeaf(Leaves[i]);
			if (!EqualityComparer<TNode>.Default.Equals(ActualNext, ExpectedNext)) {
				error = "Leaf linked-list order does not match in-order leaf sequence.";
				return false;
			}
		}

		error = null;
		return true;
	}

	protected virtual int FindLeafKeyIndex(TNode leaf, K key, out bool found) {
		var Lo = 0;
		var Hi = GetKeyCount(leaf) - 1;
		while (Lo <= Hi) {
			var Mid = Lo + ((Hi - Lo) >> 1);
			var Cmp = Compare(key, GetLeafEntry(leaf, Mid).Key);
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

	protected virtual int FindInternalChildIndex(TNode node, K key) {
		var Lo = 0;
		var Hi = GetKeyCount(node) - 1;
		while (Lo <= Hi) {
			var Mid = Lo + ((Hi - Lo) >> 1);
			var Cmp = Compare(key, GetInternalKey(node, Mid));
			if (Cmp < 0)
				Hi = Mid - 1;
			else
				Lo = Mid + 1;
		}

		return Lo;
	}

	protected virtual SplitResult SplitLeafNode(TNode leaf) {
		var Right = CreateLeafNode();
		var KeyCount = GetKeyCount(leaf);
		var LeftCount = (KeyCount + 1) / 2;

		while (GetKeyCount(leaf) > LeftCount) {
			var Entry = GetLeafEntry(leaf, LeftCount);
			AddLeafEntry(Right, Entry);
			RemoveLeafEntryAt(leaf, LeftCount);
		}

		var Next = GetNextLeaf(leaf);
		SetNextLeaf(Right, Next);
		SetNextLeaf(leaf, Right);

		return new SplitResult(leaf, GetLeafEntry(Right, 0).Key, Right);
	}

	protected virtual SplitResult SplitInternalNode(TNode node) {
		var Right = CreateInternalNode();
		var KeyCount = GetKeyCount(node);
		var MedianIndex = KeyCount / 2;
		var PromotedKey = GetInternalKey(node, MedianIndex);

		for (var i = MedianIndex + 1; i < KeyCount; i++)
			AddInternalKey(Right, GetInternalKey(node, i));

		for (var i = KeyCount - 1; i >= MedianIndex; i--)
			RemoveInternalKeyAt(node, i);

		var ChildCount = GetChildCount(node);
		for (var i = MedianIndex + 1; i < ChildCount; i++)
			AddChild(Right, GetChild(node, i));

		for (var i = ChildCount - 1; i >= MedianIndex + 1; i--)
			RemoveChildAt(node, i);

		return new SplitResult(node, PromotedKey, Right);
	}

	protected virtual void MergeChildren(TNode parent, int leftIndex) {
		var Left = GetChild(parent, leftIndex);
		var Right = GetChild(parent, leftIndex + 1);

		if (IsLeaf(Left)) {
			var RightKeyCount = GetKeyCount(Right);
			for (var i = 0; i < RightKeyCount; i++)
				AddLeafEntry(Left, GetLeafEntry(Right, i));

			SetNextLeaf(Left, GetNextLeaf(Right));
			RemoveInternalKeyAt(parent, leftIndex);
			RemoveChildAt(parent, leftIndex + 1);
			DeleteNode(Right);
			return;
		}

		AddInternalKey(Left, GetInternalKey(parent, leftIndex));
		var RightInternalKeyCount = GetKeyCount(Right);
		for (var i = 0; i < RightInternalKeyCount; i++)
			AddInternalKey(Left, GetInternalKey(Right, i));

		var RightChildCount = GetChildCount(Right);
		for (var i = 0; i < RightChildCount; i++)
			AddChild(Left, GetChild(Right, i));

		RemoveInternalKeyAt(parent, leftIndex);
		RemoveChildAt(parent, leftIndex + 1);
		DeleteNode(Right);
	}

	protected virtual void BorrowFromLeft(TNode parent, int childIndex) {
		var Child = GetChild(parent, childIndex);
		var Left = GetChild(parent, childIndex - 1);

		if (IsLeaf(Child)) {
			var LeftKeyCount = GetKeyCount(Left);
			InsertLeafEntry(Child, 0, GetLeafEntry(Left, LeftKeyCount - 1));
			RemoveLeafEntryAt(Left, LeftKeyCount - 1);
			SetInternalKey(parent, childIndex - 1, GetLeafEntry(Child, 0).Key);
			return;
		}

		var LeftKeyCountInternal = GetKeyCount(Left);
		var LeftChildCount = GetChildCount(Left);
		InsertInternalKey(Child, 0, GetInternalKey(parent, childIndex - 1));
		SetInternalKey(parent, childIndex - 1, GetInternalKey(Left, LeftKeyCountInternal - 1));
		RemoveInternalKeyAt(Left, LeftKeyCountInternal - 1);
		InsertChild(Child, 0, GetChild(Left, LeftChildCount - 1));
		RemoveChildAt(Left, LeftChildCount - 1);
	}

	protected virtual void BorrowFromRight(TNode parent, int childIndex) {
		var Child = GetChild(parent, childIndex);
		var Right = GetChild(parent, childIndex + 1);

		if (IsLeaf(Child)) {
			AddLeafEntry(Child, GetLeafEntry(Right, 0));
			RemoveLeafEntryAt(Right, 0);
			SetInternalKey(parent, childIndex, GetLeafEntry(Right, 0).Key);
			return;
		}

		AddInternalKey(Child, GetInternalKey(parent, childIndex));
		SetInternalKey(parent, childIndex, GetInternalKey(Right, 0));
		RemoveInternalKeyAt(Right, 0);
		AddChild(Child, GetChild(Right, 0));
		RemoveChildAt(Right, 0);
	}

	private SplitResult InsertRecursive(TNode node, K key, V value, bool overwriteIfExists, out bool insertedNewKey) {
		insertedNewKey = false;

		if (IsLeaf(node)) {
			var LeafIndex = FindLeafKeyIndex(node, key, out var Found);
			if (Found) {
				if (!overwriteIfExists)
					throw new ArgumentException($"Key {key} already exists in tree.", nameof(key));
				SetLeafEntry(node, LeafIndex, new KeyValuePair<K, V>(key, value));
				return null;
			}

			InsertLeafEntry(node, LeafIndex, new KeyValuePair<K, V>(key, value));
			insertedNewKey = true;
			return GetKeyCount(node) > _maxLeafKeys ? SplitLeafNode(node) : null;
		}

		var ChildIndex = FindInternalChildIndex(node, key);
		var ChildSplit = InsertRecursive(GetChild(node, ChildIndex), key, value, overwriteIfExists, out insertedNewKey);
		if (ChildSplit == null)
			return null;

		InsertInternalKey(node, ChildIndex, ChildSplit.PromotedKey);
		SetChild(node, ChildIndex, ChildSplit.Left);
		InsertChild(node, ChildIndex + 1, ChildSplit.Right);

		return GetKeyCount(node) > _maxInternalKeys ? SplitInternalNode(node) : null;
	}

	private bool RemoveRecursive(TNode node, K key) {
		if (IsLeaf(node)) {
			var LeafIndex = FindLeafKeyIndex(node, key, out var Found);
			if (!Found)
				return false;

			RemoveLeafEntryAt(node, LeafIndex);
			return true;
		}

		var ChildIndex = FindInternalChildIndex(node, key);
		if (!RemoveRecursive(GetChild(node, ChildIndex), key))
			return false;

		FixChildUnderflow(node, ChildIndex);
		RefreshAllSeparators(node);
		return true;
	}

	private void FixChildUnderflow(TNode parent, int childIndex) {
		if (IsLeaf(parent))
			return;

		var ChildCount = GetChildCount(parent);
		if (childIndex < 0 || childIndex >= ChildCount)
			return;

		var Child = GetChild(parent, childIndex);
		var MinKeys = IsLeaf(Child) ? _minLeafKeys : _minInternalKeys;
		if (GetKeyCount(Child) >= MinKeys)
			return;

		if (childIndex > 0) {
			var Left = GetChild(parent, childIndex - 1);
			if (GetKeyCount(Left) > MinKeys) {
				BorrowFromLeft(parent, childIndex);
				return;
			}
		}

		if (childIndex < ChildCount - 1) {
			var Right = GetChild(parent, childIndex + 1);
			if (GetKeyCount(Right) > MinKeys) {
				BorrowFromRight(parent, childIndex);
				return;
			}
		}

		if (childIndex > 0)
			MergeChildren(parent, childIndex - 1);
		else if (childIndex < GetChildCount(parent) - 1)
			MergeChildren(parent, childIndex);
	}

	private void RebuildInternalSeparators(TNode node) {
		if (IsLeaf(node))
			return;
		var ChildCount = GetChildCount(node);
		for (var i = 0; i < ChildCount; i++)
			RebuildInternalSeparators(GetChild(node, i));
		RefreshAllSeparators(node);
	}

	private void RefreshAllSeparators(TNode parent) {
		if (IsLeaf(parent))
			return;

		var ChildCount = GetChildCount(parent);
		if (ChildCount <= 1)
			return;

		for (var i = 1; i < ChildCount; i++)
			SetInternalKey(parent, i - 1, GetSubtreeMinKey(GetChild(parent, i)));
	}

	private TNode FindLeaf(TNode node, K key) {
		while (!IsLeaf(node)) {
			var ChildIndex = FindInternalChildIndex(node, key);
			node = GetChild(node, ChildIndex);
		}
		return node;
	}

	private K GetSubtreeMinKey(TNode node) {
		while (!IsLeaf(node))
			node = GetChild(node, 0);
		return GetLeafEntry(node, 0).Key;
	}

	private TNode GetLeftMostLeaf() {
		if (!HasRoot)
			return default;

		var Node = Root;
		while (!IsLeaf(Node))
			Node = GetChild(Node, 0);
		return Node;
	}

	private void DeleteSubtree(TNode node) {
		if (!IsLeaf(node)) {
			var ChildCount = GetChildCount(node);
			for (var i = 0; i < ChildCount; i++)
				DeleteSubtree(GetChild(node, i));
		}
		DeleteNode(node);
	}

	private IEnumerable<KeyValuePair<K, V>> EnumerateInOrder() {
		var Leaf = GetLeftMostLeaf();
		while (!EqualityComparer<TNode>.Default.Equals(Leaf, default)) {
			var KeyCount = GetKeyCount(Leaf);
			for (var i = 0; i < KeyCount; i++)
				yield return GetLeafEntry(Leaf, i);
			Leaf = GetNextLeaf(Leaf);
		}
	}

	private IEnumerable<KeyValuePair<K, V>> EnumeratePreOrder(TNode node) {
		if (IsLeaf(node)) {
			var KeyCount = GetKeyCount(node);
			for (var i = 0; i < KeyCount; i++)
				yield return GetLeafEntry(node, i);
			yield break;
		}

		var ChildCount = GetChildCount(node);
		for (var i = 0; i < ChildCount; i++)
			foreach (var Item in EnumeratePreOrder(GetChild(node, i)))
				yield return Item;
	}

	private IEnumerable<KeyValuePair<K, V>> EnumeratePostOrder(TNode node) {
		if (IsLeaf(node)) {
			var KeyCount = GetKeyCount(node);
			for (var i = 0; i < KeyCount; i++)
				yield return GetLeafEntry(node, i);
			yield break;
		}

		var ChildCount = GetChildCount(node);
		for (var i = 0; i < ChildCount; i++)
			foreach (var Item in EnumeratePostOrder(GetChild(node, i)))
				yield return Item;
	}

	private IEnumerable<KeyValuePair<K, V>> EnumerateLevelOrder(TNode root) {
		var Queue = new Queue<TNode>();
		Queue.Enqueue(root);
		while (Queue.Count > 0) {
			var Node = Queue.Dequeue();
			if (IsLeaf(Node)) {
				var KeyCount = GetKeyCount(Node);
				for (var i = 0; i < KeyCount; i++)
					yield return GetLeafEntry(Node, i);
				continue;
			}

			var ChildCount = GetChildCount(Node);
			for (var i = 0; i < ChildCount; i++)
				Queue.Enqueue(GetChild(Node, i));
		}
	}

	private bool ValidateNode(
		TNode node,
		bool isRoot,
		K minInclusive,
		bool hasMin,
		K maxExclusive,
		bool hasMax,
		int depth,
		ref int leafDepth,
		ref int totalLeafKeys,
		IList<TNode> leaves,
		out string error) {

		var KeyCount = GetKeyCount(node);

		if (IsLeaf(node)) {
			if (!isRoot && KeyCount < _minLeafKeys) {
				error = $"Leaf node contains {KeyCount} keys, below min {_minLeafKeys}.";
				return false;
			}

			if (KeyCount > _maxLeafKeys) {
				error = $"Leaf node contains {KeyCount} keys, exceeding max {_maxLeafKeys}.";
				return false;
			}

			if (GetChildCount(node) != 0) {
				error = "Leaf node contains children.";
				return false;
			}

			for (var i = 1; i < KeyCount; i++) {
				if (Compare(GetLeafEntry(node, i - 1).Key, GetLeafEntry(node, i).Key) >= 0) {
					error = "Leaf keys are not strictly increasing.";
					return false;
				}
			}

			for (var i = 0; i < KeyCount; i++) {
				var Key = GetLeafEntry(node, i).Key;
				if (hasMin && Compare(Key, minInclusive) < 0) {
					error = "A key violates its lower bound.";
					return false;
				}
				if (hasMax && Compare(Key, maxExclusive) >= 0) {
					error = "A key violates its upper bound.";
					return false;
				}
			}

			totalLeafKeys += KeyCount;
			if (leafDepth < 0)
				leafDepth = depth;
			else if (leafDepth != depth) {
				error = "Leaves are not all at the same depth.";
				return false;
			}

			leaves.Add(node);
			error = null;
			return true;
		}

		if (!isRoot && KeyCount < _minInternalKeys) {
			error = $"Internal node contains {KeyCount} keys, below min {_minInternalKeys}.";
			return false;
		}

		if (KeyCount > _maxInternalKeys) {
			error = $"Internal node contains {KeyCount} keys, exceeding max {_maxInternalKeys}.";
			return false;
		}

		var ChildCount = GetChildCount(node);
		if (ChildCount != KeyCount + 1) {
			error = "Internal node child count must equal key count + 1.";
			return false;
		}

		if (isRoot && ChildCount > 0 && ChildCount < 2) {
			error = "Internal root must contain at least 2 children.";
			return false;
		}

		for (var i = 1; i < KeyCount; i++) {
			if (Compare(GetInternalKey(node, i - 1), GetInternalKey(node, i)) >= 0) {
				error = "Internal keys are not strictly increasing.";
				return false;
			}
		}

		for (var i = 0; i < ChildCount; i++) {
			var ChildHasMin = hasMin;
			var ChildMin = minInclusive;
			var ChildHasMax = hasMax;
			var ChildMax = maxExclusive;

			if (i == 0) {
				ChildHasMax = KeyCount > 0;
				if (ChildHasMax)
					ChildMax = GetInternalKey(node, 0);
			} else if (i == ChildCount - 1) {
				ChildHasMin = true;
				ChildMin = GetInternalKey(node, KeyCount - 1);
			} else {
				ChildHasMin = true;
				ChildMin = GetInternalKey(node, i - 1);
				ChildHasMax = true;
				ChildMax = GetInternalKey(node, i);
			}

			if (!ValidateNode(GetChild(node, i), false, ChildMin, ChildHasMin, ChildMax, ChildHasMax, depth + 1, ref leafDepth, ref totalLeafKeys, leaves, out error))
				return false;
		}

		for (var i = 1; i < ChildCount; i++) {
			var Expected = GetSubtreeMinKey(GetChild(node, i));
			if (Compare(GetInternalKey(node, i - 1), Expected) != 0) {
				error = "Internal separator key does not match right-child minimum key.";
				return false;
			}
		}

		error = null;
		return true;
	}

	protected sealed class SplitResult {
		public SplitResult(TNode left, K promotedKey, TNode right) {
			Left = left;
			PromotedKey = promotedKey;
			Right = right;
		}

		public TNode Left { get; }
		public K PromotedKey { get; }
		public TNode Right { get; }
	}
}
