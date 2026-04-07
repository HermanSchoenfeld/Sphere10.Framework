using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;

namespace Sphere10.Framework;

/// <summary>
/// B-tree dictionary with configurable maximum children per node.
/// 
/// Semantics of <paramref name="order"/>:
/// - Max children per node = order
/// - Max keys per node      = order - 1
/// - Min keys per non-root  = ceil(order / 2.0) - 1
/// 
/// This implementation uses a standard node layout:
/// - Keys stored in a node-local sorted list
/// - Children stored in a parallel list where Children.Count == Keys.Count + 1 for internal nodes
/// 
/// It provides:
/// - centralised comparer semantics
/// - proper split / borrow / merge logic
/// - no brute-force recovery paths
/// - optional invariant validation for debugging
/// </summary>
public class BTree<K, V> : IDictionary<K, V> {
	private readonly int _order;
	private readonly int _maxKeys;
	private readonly int _minKeys;
	private readonly IComparer<K> _comparer;
	private Node _root;

	public BTree(int order, IComparer<K> keyComparer = null) {
		if (order < 3)
			throw new ArgumentOutOfRangeException(nameof(order), "B-tree order must be at least 3.");

		_order = order;
		_maxKeys = order - 1;
		_minKeys = ((order + 1) / 2) - 1; // ceil(order / 2) - 1
		_comparer = keyComparer ?? Comparer<K>.Default;
	}

	public int Count { get; private set; }

	public TreeTraversalType TraversalType { get; set; } = TreeTraversalType.InOrder;

	public bool IsReadOnly => false;

	public ICollection<K> Keys {
		get {
			var list = new List<K>(Count);
			foreach (var kv in this)
				list.Add(kv.Key);
			return list;
		}
	}

	public ICollection<V> Values {
		get {
			var list = new List<V>(Count);
			foreach (var kv in this)
				list.Add(kv.Value);
			return list;
		}
	}

	public V this[K key] {
		get {
			if (TryGetValue(key, out var value))
				return value;
			throw new KeyNotFoundException();
		}
		set => Set(key, value, overwriteIfExists: true);
	}

	public void Add(K key, V value) => Set(key, value, overwriteIfExists: false);

	public void Add(KeyValuePair<K, V> item) => Add(item.Key, item.Value);

	public void Set(K key, V value, bool overwriteIfExists) {
		if (_root == null) {
			_root = new Node(isLeaf: true);
			_root.Keys.Add(new KeyValuePair<K, V>(key, value));
			Count = 1;
			DebugValidate();
			return;
		}

		var split = InsertRecursive(_root, key, value, overwriteIfExists, out var insertedNewKey);
		if (split != null) {
			var newRoot = new Node(isLeaf: false);
			newRoot.Keys.Add(split.Promoted);
			newRoot.Children.Add(split.Left);
			newRoot.Children.Add(split.Right);
			_root = newRoot;
		}

		if (insertedNewKey)
			Count++;

		DebugValidate();
	}

	public bool Remove(K key) {
		if (_root == null)
			return false;

		var removed = RemoveRecursive(_root, key);
		if (!removed)
			return false;

		Count--;

		if (_root.Keys.Count == 0) {
			_root = _root.IsLeaf ? null : _root.Children[0];
		}

		DebugValidate();
		return true;
	}

	public bool Remove(KeyValuePair<K, V> item) {
		if (!TryGetValue(item.Key, out var current))
			return false;
		if (!EqualityComparer<V>.Default.Equals(current, item.Value))
			return false;
		return Remove(item.Key);
	}

	public void Clear() {
		_root = null;
		Count = 0;
	}

	public bool ContainsKey(K key) => TryGetValue(key, out _);

	public bool TryGetValue(K key, out V value) {
		var node = _root;
		while (node != null) {
			var index = node.FindKeyIndex(key, _comparer, out var found);
			if (found) {
				value = node.Keys[index].Value;
				return true;
			}
			if (node.IsLeaf)
				break;
			node = node.Children[index];
		}

		value = default;
		return false;
	}

	public bool Contains(KeyValuePair<K, V> item)
		=> TryGetValue(item.Key, out var current) && EqualityComparer<V>.Default.Equals(current, item.Value);

	public void CopyTo(KeyValuePair<K, V>[] array, int arrayIndex) {
		if (array == null)
			throw new ArgumentNullException(nameof(array));
		if (arrayIndex < 0 || arrayIndex > array.Length)
			throw new ArgumentOutOfRangeException(nameof(arrayIndex));
		if (array.Length - arrayIndex < Count)
			throw new ArgumentException("Destination array is too small.", nameof(array));

		foreach (var kv in this)
			array[arrayIndex++] = kv;
	}

	public IEnumerator<KeyValuePair<K, V>> GetEnumerator() {
		if (_root == null)
			yield break;

		switch (TraversalType) {
			case TreeTraversalType.PreOrder:
				foreach (var item in EnumeratePreOrder(_root))
					yield return item;
				break;
			case TreeTraversalType.PostOrder:
				foreach (var item in EnumeratePostOrder(_root))
					yield return item;
				break;
			case TreeTraversalType.LevelOrder:
				foreach (var item in EnumerateLevelOrder(_root))
					yield return item;
				break;
			default:
				foreach (var item in EnumerateInOrder(_root))
					yield return item;
				break;
		}
	}

	IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

	public bool Validate(out string error) {
		if (_root == null) {
			if (Count != 0) {
				error = "Count is non-zero while root is null.";
				return false;
			}
			error = null;
			return true;
		}

		var leafDepth = -1;
		var visitedCount = 0;
		var ok = ValidateNode(
			_root,
			isRoot: true,
			minExclusive: default,
			hasMin: false,
			maxExclusive: default,
			hasMax: false,
			depth: 0,
			ref leafDepth,
			ref visitedCount,
			out error);

		if (!ok)
			return false;

		if (visitedCount != Count) {
			error = $"Reachable key count {visitedCount} does not match Count {Count}.";
			return false;
		}

		error = null;
		return true;
	}

	private SplitResult InsertRecursive(Node node, K key, V value, bool overwriteIfExists, out bool insertedNewKey) {
		insertedNewKey = false;

		var index = node.FindKeyIndex(key, _comparer, out var found);
		if (found) {
			if (!overwriteIfExists)
				throw new InvalidOperationException($"Key {key} already exists in tree.");
			node.Keys[index] = new KeyValuePair<K, V>(key, value);
			return null;
		}

		if (node.IsLeaf) {
			node.Keys.Insert(index, new KeyValuePair<K, V>(key, value));
			insertedNewKey = true;
			return node.Keys.Count > _maxKeys ? SplitNode(node) : null;
		}

		var childSplit = InsertRecursive(node.Children[index], key, value, overwriteIfExists, out insertedNewKey);
		if (childSplit == null)
			return null;

		node.Keys.Insert(index, childSplit.Promoted);
		node.Children[index] = childSplit.Left;
		node.Children.Insert(index + 1, childSplit.Right);

		return node.Keys.Count > _maxKeys ? SplitNode(node) : null;
	}

	private SplitResult SplitNode(Node node) {
		var medianIndex = node.Keys.Count / 2;
		var promoted = node.Keys[medianIndex];

		var left = new Node(node.IsLeaf);
		var right = new Node(node.IsLeaf);

		for (var i = 0; i < medianIndex; i++)
			left.Keys.Add(node.Keys[i]);
		for (var i = medianIndex + 1; i < node.Keys.Count; i++)
			right.Keys.Add(node.Keys[i]);

		if (!node.IsLeaf) {
			for (var i = 0; i <= medianIndex; i++)
				left.Children.Add(node.Children[i]);
			for (var i = medianIndex + 1; i < node.Children.Count; i++)
				right.Children.Add(node.Children[i]);
		}

		return new SplitResult(left, promoted, right);
	}

	private bool RemoveRecursive(Node node, K key) {
		var index = node.FindKeyIndex(key, _comparer, out var found);

		if (found) {
			if (node.IsLeaf) {
				node.Keys.RemoveAt(index);
				return true;
			}

			// Replace with in-order predecessor, then remove it from the left subtree
			var predecessor = GetMax(node.Children[index]);
			node.Keys[index] = predecessor;
			var removed = RemoveRecursive(node.Children[index], predecessor.Key);
			Debug.Assert(removed);
			FixChildUnderflow(node, index);
			return true;
		}

		if (node.IsLeaf)
			return false;

		var childIndex = index;
		if (!RemoveRecursive(node.Children[childIndex], key))
			return false;
		FixChildUnderflow(node, childIndex);
		return true;
	}

	private static KeyValuePair<K, V> GetMax(Node node) {
		while (!node.IsLeaf)
			node = node.Children[node.Children.Count - 1];
		return node.Keys[node.Keys.Count - 1];
	}

	private void FixChildUnderflow(Node parent, int childIndex) {
		if (parent.IsLeaf)
			return;
		if (childIndex < 0 || childIndex >= parent.Children.Count)
			return;

		var child = parent.Children[childIndex];
		if (child.Keys.Count >= _minKeys)
			return;

		if (childIndex > 0 && parent.Children[childIndex - 1].Keys.Count > _minKeys) {
			BorrowFromLeft(parent, childIndex);
			return;
		}

		if (childIndex < parent.Children.Count - 1 && parent.Children[childIndex + 1].Keys.Count > _minKeys) {
			BorrowFromRight(parent, childIndex);
			return;
		}

		if (childIndex > 0)
			MergeChildren(parent, childIndex - 1);
		else if (childIndex < parent.Children.Count - 1)
			MergeChildren(parent, childIndex);
	}

	private void BorrowFromLeft(Node parent, int childIndex) {
		var child = parent.Children[childIndex];
		var left = parent.Children[childIndex - 1];

		child.Keys.Insert(0, parent.Keys[childIndex - 1]);
		parent.Keys[childIndex - 1] = left.Keys[left.Keys.Count - 1];
		left.Keys.RemoveAt(left.Keys.Count - 1);

		if (!left.IsLeaf) {
			var movedChild = left.Children[left.Children.Count - 1];
			left.Children.RemoveAt(left.Children.Count - 1);
			child.Children.Insert(0, movedChild);
		}
	}

	private void BorrowFromRight(Node parent, int childIndex) {
		var child = parent.Children[childIndex];
		var right = parent.Children[childIndex + 1];

		child.Keys.Add(parent.Keys[childIndex]);
		parent.Keys[childIndex] = right.Keys[0];
		right.Keys.RemoveAt(0);

		if (!right.IsLeaf) {
			var movedChild = right.Children[0];
			right.Children.RemoveAt(0);
			child.Children.Add(movedChild);
		}
	}

	private void MergeChildren(Node parent, int leftIndex) {
		var left = parent.Children[leftIndex];
		var right = parent.Children[leftIndex + 1];
		var separator = parent.Keys[leftIndex];

		left.Keys.Add(separator);
		left.Keys.AddRange(right.Keys);

		if (!left.IsLeaf)
			left.Children.AddRange(right.Children);

		parent.Keys.RemoveAt(leftIndex);
		parent.Children.RemoveAt(leftIndex + 1);
	}

	private IEnumerable<KeyValuePair<K, V>> EnumerateInOrder(Node node) {
		if (node.IsLeaf) {
			for (var i = 0; i < node.Keys.Count; i++)
				yield return node.Keys[i];
			yield break;
		}

		for (var i = 0; i < node.Keys.Count; i++) {
			foreach (var item in EnumerateInOrder(node.Children[i]))
				yield return item;
			yield return node.Keys[i];
		}
		foreach (var item in EnumerateInOrder(node.Children[node.Keys.Count]))
			yield return item;
	}

	private IEnumerable<KeyValuePair<K, V>> EnumeratePreOrder(Node node) {
		for (var i = 0; i < node.Keys.Count; i++)
			yield return node.Keys[i];
		if (node.IsLeaf)
			yield break;
		for (var i = 0; i < node.Children.Count; i++)
			foreach (var item in EnumeratePreOrder(node.Children[i]))
				yield return item;
	}

	private IEnumerable<KeyValuePair<K, V>> EnumeratePostOrder(Node node) {
		if (!node.IsLeaf) {
			for (var i = 0; i < node.Children.Count; i++)
				foreach (var item in EnumeratePostOrder(node.Children[i]))
					yield return item;
		}
		for (var i = 0; i < node.Keys.Count; i++)
			yield return node.Keys[i];
	}

	private IEnumerable<KeyValuePair<K, V>> EnumerateLevelOrder(Node root) {
		var queue = new Queue<Node>();
		queue.Enqueue(root);
		while (queue.Count > 0) {
			var node = queue.Dequeue();
			for (var i = 0; i < node.Keys.Count; i++)
				yield return node.Keys[i];
			if (!node.IsLeaf)
				for (var i = 0; i < node.Children.Count; i++)
					queue.Enqueue(node.Children[i]);
		}
	}

	private bool ValidateNode(
		Node node,
		bool isRoot,
		K minExclusive,
		bool hasMin,
		K maxExclusive,
		bool hasMax,
		int depth,
		ref int leafDepth,
		ref int totalKeys,
		out string error) {

		if (node == null) {
			error = "Encountered null node in reachable structure.";
			return false;
		}

		if (node.Keys.Count == 0 && !isRoot) {
			error = "Non-root node contains zero keys.";
			return false;
		}

		if (node.Keys.Count > _maxKeys) {
			error = $"Node contains {node.Keys.Count} keys, exceeding max {_maxKeys}.";
			return false;
		}

		if (!isRoot && node.Keys.Count < _minKeys) {
			error = $"Non-root node contains {node.Keys.Count} keys, below min {_minKeys}.";
			return false;
		}

		for (var i = 1; i < node.Keys.Count; i++) {
			if (Compare(node.Keys[i - 1].Key, node.Keys[i].Key) >= 0) {
				error = "Node keys are not strictly increasing.";
				return false;
			}
		}

		for (var i = 0; i < node.Keys.Count; i++) {
			var key = node.Keys[i].Key;
			if (hasMin && Compare(key, minExclusive) <= 0) {
				error = "A key violates its lower bound.";
				return false;
			}
			if (hasMax && Compare(key, maxExclusive) >= 0) {
				error = "A key violates its upper bound.";
				return false;
			}
		}

		totalKeys += node.Keys.Count;

		if (node.IsLeaf) {
			if (node.Children.Count != 0) {
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

		if (node.Children.Count != node.Keys.Count + 1) {
			error = "Internal node child count must equal key count + 1.";
			return false;
		}

		for (var i = 0; i < node.Children.Count; i++) {
			var childHasMin = hasMin;
			var childMin = minExclusive;
			var childHasMax = hasMax;
			var childMax = maxExclusive;

			if (i == 0) {
				childHasMax = true;
				childMax = node.Keys[0].Key;
			} else if (i == node.Children.Count - 1) {
				childHasMin = true;
				childMin = node.Keys[node.Keys.Count - 1].Key;
			} else {
				childHasMin = true;
				childMin = node.Keys[i - 1].Key;
				childHasMax = true;
				childMax = node.Keys[i].Key;
			}

			if (!ValidateNode(node.Children[i], false, childMin, childHasMin, childMax, childHasMax, depth + 1, ref leafDepth, ref totalKeys, out error))
				return false;
		}

		error = null;
		return true;
	}

	[Conditional("DIAGNOSTIC")]
	private void DebugValidate() {
		if (!Validate(out var error))
			throw new InvalidOperationException("B-tree invariant violation: " + error);
	}

	private int Compare(K x, K y) => _comparer.Compare(x, y);

	private sealed class Node {
		public Node(bool isLeaf) {
			IsLeaf = isLeaf;
			Keys = new List<KeyValuePair<K, V>>();
			Children = new List<Node>();
		}

		public bool IsLeaf { get; }
		public List<KeyValuePair<K, V>> Keys { get; }
		public List<Node> Children { get; }

		public int FindKeyIndex(K key, IComparer<K> comparer, out bool found) {
			var lo = 0;
			var hi = Keys.Count - 1;
			while (lo <= hi) {
				var mid = lo + ((hi - lo) >> 1);
				var cmp = comparer.Compare(key, Keys[mid].Key);
				if (cmp == 0) {
					found = true;
					return mid;
				}
				if (cmp < 0)
					hi = mid - 1;
				else
					lo = mid + 1;
			}
			found = false;
			return lo;
		}
	}

	private sealed class SplitResult {
		public SplitResult(Node left, KeyValuePair<K, V> promoted, Node right) {
			Left = left;
			Promoted = promoted;
			Right = right;
		}

		public Node Left { get; }
		public KeyValuePair<K, V> Promoted { get; }
		public Node Right { get; }
	}
}
