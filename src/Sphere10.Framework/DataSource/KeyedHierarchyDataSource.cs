// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework;

/// <summary>
/// Presents a flat source's rows as a tree, by each row's own key and the key of its parent: as top-level rows with child views, as an outline
/// or as the flat list it is.
/// </summary>
/// <typeparam name="T">The row type. Each row is its own entity.</typeparam>
/// <remarks>
/// A row whose parent key is null or names no row in the source is a root; in a loop of parents the first row read is the root and the others
/// stay under it. With <see cref="HierarchyPresentation.Tree"/> a page holds root rows only and each row's children are its child view, in the
/// order the page was sorted. With <see cref="HierarchyPresentation.Outline"/> a page holds every shown row in tree order and
/// <see cref="IHierarchyOutline"/> gives each row's depth and expansion; every row starts expanded. A search keeps every row that matches and the
/// rows above it, so the path to each match stays visible, and an outline shows a search's rows expanded.
/// <see cref="HierarchyPresentation.Flat"/> reads the source's rows as they are, with no children.
/// The decorated source is borrowed: CRUD, counts and change notifications are forwarded to it. Each tree or outline read captures the whole
/// matching result once; child views, depths and subtrees use that capture, and a later read replaces it, so child queries of an earlier read are
/// told apart by <see cref="GetChildQueryKey"/>.
/// </remarks>
public sealed class KeyedHierarchyDataSource<T> : HierarchicalDataSourceBase<T, T>, IHierarchyOutline {
	private readonly Func<T, object> _key;
	private readonly Func<T, object> _parentKey;
	private readonly Func<object, T> _createParent;
	private readonly object _expansionLock = new();
	// The node keys whose expansion differs from the default; ExpandAll and CollapseAll set the default and clear them.
	private readonly HashSet<object> _toggled = new();
	private bool _expandedByDefault = true;
	private volatile Tree _tree;

	/// <param name="createParent">Optional. Makes a row for a parent key that names no row, such as a process for the handles it holds or a
	/// folder for the tasks in it. The made row is keyed by the parent key it was made for, whatever <paramref name="key"/> would say of it, and its
	/// own parent key is followed in turn (a parent key that names the made row itself makes it a root). Without it such a row is a root. A made
	/// row is shown only in a tree or outline, never in the flat list, and a search keeps it only as the parent of a match. Among its siblings it
	/// follows the read rows, in the order of the first row read beneath it.</param>
	public KeyedHierarchyDataSource(IDataSource<T> source, Func<T, object> key, Func<T, object> parentKey, Func<object, T> createParent = null)
		: base(source) {
		Guard.ArgumentNotNull(key, nameof(key));
		Guard.ArgumentNotNull(parentKey, nameof(parentKey));
		_key = key;
		_parentKey = parentKey;
		_createParent = createParent;
	}

	/// <summary>How reads serve the rows. A grid bound to this source shows row details only for <see cref="HierarchyPresentation.Tree"/>.</summary>
	public HierarchyPresentation Presentation { get; set; }

	bool IHierarchyOutline.IsOutline => Presentation == HierarchyPresentation.Outline;

	public override object GetNodeKey(T node) => KeyOf(_tree, node);

	public override object GetChildQueryKey(T node) => new ChildQueryKey(_tree, KeyOf(_tree, node));

	// Only a tree drills into child views; an outline lists the children itself and a flat list has none.
	public override bool HasChildren(T node) => Presentation == HierarchyPresentation.Tree && _tree?.GetChildren(KeyOf(_tree, node)).Length > 0;

	public override T GetEntity(T node) => node;

	public override IHierarchicalDataSource<T> GetChildren(T node)
		=> new ChildView(this, _tree, Presentation == HierarchyPresentation.Tree ? _tree?.GetChildren(KeyOf(_tree, node)) ?? Array.Empty<T>() : Array.Empty<T>());

	public override Task<IHierarchicalDataSource<T>> GetChildrenAsync(T node, CancellationToken cancellationToken = default) {
		cancellationToken.ThrowIfCancellationRequested();
		return Task.FromResult(GetChildren(node));
	}

	public override DataSourceItems<T> ReadRange(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection) {
		if (Presentation == HierarchyPresentation.Flat)
			return base.ReadRange(searchTerm, pageLength, page, sortProperty, sortDirection);
		var all = InternalDataSource.ReadRange(null, int.MaxValue, 0, sortProperty, sortDirection).Items;
		var matching = string.IsNullOrWhiteSpace(searchTerm) ? null : InternalDataSource.ReadRange(searchTerm, int.MaxValue, 0, sortProperty, sortDirection).Items;
		return ReadTree(all, matching, pageLength, page);
	}

	public override async Task<DataSourceItems<T>> ReadRangeAsync(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection) {
		if (Presentation == HierarchyPresentation.Flat)
			return await base.ReadRangeAsync(searchTerm, pageLength, page, sortProperty, sortDirection);
		var all = (await InternalDataSource.ReadRangeAsync(null, int.MaxValue, 0, sortProperty, sortDirection)).Items;
		var matching = string.IsNullOrWhiteSpace(searchTerm) ? null : (await InternalDataSource.ReadRangeAsync(searchTerm, int.MaxValue, 0, sortProperty, sortDirection)).Items;
		return ReadTree(all, matching, pageLength, page);
	}

	/// <summary>Whether the row was made for a parent that is not in the source, rather than read from it.</summary>
	public bool IsCreatedParent(T node) => _tree?.IsCreated(node) == true;

	/// <summary>How many levels lie above the row in the last tree or outline read: zero for a root, and for a row that read did not hold.</summary>
	public int GetDepth(T node) => _tree?.GetDepth(KeyOf(_tree, node)) ?? 0;

	/// <summary>The row followed by every row beneath it in the last tree or outline read, in tree order, whether or not an outline shows them.</summary>
	public T[] GetSubtree(T node) => _tree?.GetSubtree(node, row => KeyOf(_tree, row)) ?? new[] { node };

	/// <summary>Whether an outline shows the rows beneath the row.</summary>
	public bool IsExpanded(T node) {
		var key = KeyOf(_tree, node);
		lock (_expansionLock)
			return _expandedByDefault != _toggled.Contains(key);
	}

	/// <summary>Shows or hides the rows beneath the row in an outline, from the next read.</summary>
	public void SetExpanded(T node, bool expanded) {
		var key = KeyOf(_tree, node);
		lock (_expansionLock)
			if (expanded == _expandedByDefault)
				_toggled.Remove(key);
			else
				_toggled.Add(key);
	}

	/// <summary>Shows every row of an outline from the next read, including rows not yet read.</summary>
	public void ExpandAll() {
		lock (_expansionLock) {
			_expandedByDefault = true;
			_toggled.Clear();
		}
	}

	/// <summary>Shows only an outline's top-level rows from the next read, including rows not yet read.</summary>
	public void CollapseAll() {
		lock (_expansionLock) {
			_expandedByDefault = false;
			_toggled.Clear();
		}
	}

	int IHierarchyOutline.GetOutlineDepth(object row) => row is T node ? GetDepth(node) : 0;

	bool IHierarchyOutline.HasOutlineChildren(object row) => row is T node && _tree?.GetChildren(KeyOf(_tree, node)).Length > 0;

	bool IHierarchyOutline.IsOutlineExpanded(object row) => row is T node && IsExpanded(node);

	void IHierarchyOutline.SetOutlineExpanded(object row, bool expanded) {
		if (row is T node)
			SetExpanded(node, expanded);
	}

	// A made row is keyed by the parent key it was made for; a read row by the owner's key.
	private object KeyOf(Tree tree, T node) => tree != null && tree.TryGetCreatedKey(node, out var key) ? key : _key(node);

	private DataSourceItems<T> ReadTree(IEnumerable<T> all, IEnumerable<T> matching, int pageLength, int page) {
		var tree = new Tree(all.ToArray(), matching?.ToArray(), _key, _parentKey, _createParent);
		_tree = tree;
		if (Presentation != HierarchyPresentation.Outline)
			return Page(tree.Roots, pageLength, page);
		// A search shows its matches and the rows above them, whatever was collapsed.
		var searching = matching != null;
		bool Expanded(object key) {
			if (searching)
				return true;
			lock (_expansionLock)
				return _expandedByDefault != _toggled.Contains(key);
		}
		return Page(tree.GetOutline(row => KeyOf(tree, row), Expanded), pageLength, page);
	}

	private static DataSourceItems<T> Page(T[] rows, int pageLength, int page) {
		pageLength = Math.Max(1, pageLength);
		page = Math.Clamp(page, 0, Math.Max(0, (rows.Length - 1) / pageLength));
		return new DataSourceItems<T> { Items = rows.Skip((int)Math.Min(int.MaxValue, (long)page * pageLength)).Take(pageLength).ToArray(), Page = page, TotalCount = rows.Length };
	}

	#region Inner Types

	/// <summary>Identifies one row's children within one captured tree, so a later read rejects child queries of an earlier one.</summary>
	private sealed record ChildQueryKey(Tree Tree, object NodeKey);

	/// <summary>One read's rows arranged by parent, keeping only the rows a search leaves visible.</summary>
	private sealed class Tree {
		private static readonly object RootKey = new();
		private readonly Dictionary<object, T[]> _children;
		private readonly Dictionary<object, int> _depths = new();
		private readonly Dictionary<object, object> _created = new(ReferenceEqualityComparer.Instance);

		public Tree(T[] read, T[] matching, Func<T, object> key, Func<T, object> parentKey, Func<object, T> createParent) {
			var rowList = new List<T>(read);
			var keyList = read.Select(key).ToList();
			var byKey = new Dictionary<object, int>();
			for (var index = 0; index < rowList.Count; index++)
				byKey.TryAdd(keyList[index] ?? RootKey, index);

			// A parent that is not a row is made when the owner can make it, together with the parents above it, so made rows follow the read rows
			// in the order of the first row read beneath them, however deep that row is (20 November 2026: a category reached through a group was
			// listed after one reached directly). A bound on how many are made stops a factory whose parents never end. A missing parent otherwise
			// makes the row a root.
			var parentList = new List<int>(rowList.Count);
			var limit = read.Length * 4 + 1024;
			for (var index = 0; index < rowList.Count; index++) {
				var parent = parentKey(rowList[index]);
				for (var missing = parent; missing != null && !byKey.ContainsKey(missing) && createParent != null && _created.Count < limit && createParent(missing) is { } made; missing = parentKey(made)) {
					_created.Add(made, missing);
					rowList.Add(made);
					keyList.Add(missing);
					byKey.Add(missing, rowList.Count - 1);
				}
				parentList.Add(parent != null && byKey.TryGetValue(parent, out var found) && found != index ? found : -1);
			}
			var rows = rowList.ToArray();
			var keys = keyList.ToArray();
			var parents = parentList.ToArray();

			// A chain that leads back to the row breaks the loop at its first row, which becomes a root.
			for (var index = 0; index < rows.Length; index++) {
				var seen = new HashSet<int> { index };
				for (var ancestor = parents[index]; ancestor >= 0; ancestor = parents[ancestor])
					if (!seen.Add(ancestor)) {
						parents[index] = -1;
						break;
					}
			}

			// A search shows each match and every row above it.
			var visible = new bool[rows.Length];
			if (matching == null)
				Array.Fill(visible, true);
			else
				foreach (var match in matching)
					if (byKey.TryGetValue(key(match) ?? RootKey, out var index))
						for (var row = index; row >= 0 && !visible[row]; row = parents[row])
							visible[row] = true;

			var children = new Dictionary<object, List<T>>();
			var roots = new List<T>();
			for (var index = 0; index < rows.Length; index++) {
				if (!visible[index])
					continue;
				var depth = 0;
				for (var ancestor = parents[index]; ancestor >= 0; ancestor = parents[ancestor])
					depth++;
				_depths.TryAdd(keys[index] ?? RootKey, depth);
				if (parents[index] < 0) {
					roots.Add(rows[index]);
					continue;
				}
				var parent = keys[parents[index]] ?? RootKey;
				if (!children.TryGetValue(parent, out var siblings))
					children.Add(parent, siblings = new List<T>());
				siblings.Add(rows[index]);
			}
			Roots = roots.ToArray();
			_children = children.ToDictionary(pair => pair.Key, pair => pair.Value.ToArray());
		}

		public T[] Roots { get; }

		public T[] GetChildren(object key) => _children.TryGetValue(key ?? RootKey, out var children) ? children : Array.Empty<T>();

		public int GetDepth(object key) => _depths.TryGetValue(key ?? RootKey, out var depth) ? depth : 0;

		public bool IsCreated(T row) => row != null && _created.ContainsKey(row);

		public bool TryGetCreatedKey(T row, out object key) {
			key = null;
			return row != null && _created.TryGetValue(row, out key);
		}

		/// <summary>The roots and, beneath each expanded row, its children, in tree order.</summary>
		public T[] GetOutline(Func<T, object> keyOf, Func<object, bool> expanded) {
			var outline = new List<T>();
			var visited = new HashSet<object>();
			foreach (var root in Roots)
				Walk(root, keyOf, row => expanded(keyOf(row)), outline, visited);
			return outline.ToArray();
		}

		/// <summary>The row and every row beneath it, in tree order.</summary>
		public T[] GetSubtree(T row, Func<T, object> keyOf) {
			var subtree = new List<T>();
			Walk(row, keyOf, _ => true, subtree, new HashSet<object>());
			return subtree.ToArray();
		}

		// Rows that share a key share its children, so a key already walked is not walked again.
		private void Walk(T row, Func<T, object> keyOf, Func<T, bool> descend, List<T> output, HashSet<object> visited) {
			output.Add(row);
			var key = keyOf(row) ?? RootKey;
			if (!descend(row) || !visited.Add(key))
				return;
			foreach (var child in GetChildren(key))
				Walk(child, keyOf, descend, output, visited);
		}
	}

	/// <summary>One row's children in the order of the read that captured them, served with the owner's keys.</summary>
	private sealed class ChildView : HierarchicalDataSourceBase<T, T> {
		private readonly KeyedHierarchyDataSource<T> _owner;
		private readonly Tree _tree;
		private readonly T[] _rows;

		public ChildView(KeyedHierarchyDataSource<T> owner, Tree tree, T[] rows)
			: base(new ArrayDataSource<T>(rows)) {
			_owner = owner;
			_tree = tree;
			_rows = rows;
		}

		public override DataSourceCapabilities Capabilities => DataSourceCapabilities.ReadOnly;

		public override Task<DataSourceCapabilities> CapabilitiesAsync => Task.FromResult(Capabilities);

		public override object GetNodeKey(T node) => _owner.KeyOf(_tree, node);

		public override object GetChildQueryKey(T node) => new ChildQueryKey(_tree, _owner.KeyOf(_tree, node));

		public override bool HasChildren(T node) => _tree?.GetChildren(_owner.KeyOf(_tree, node)).Length > 0;

		public override T GetEntity(T node) => node;

		public override IHierarchicalDataSource<T> GetChildren(T node) => new ChildView(_owner, _tree, _tree?.GetChildren(_owner.KeyOf(_tree, node)) ?? Array.Empty<T>());

		public override Task<IHierarchicalDataSource<T>> GetChildrenAsync(T node, CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();
			return Task.FromResult(GetChildren(node));
		}

		public override DataSourceItems<T> ReadRange(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection) => Page(_rows, pageLength, page);

		public override Task<DataSourceItems<T>> ReadRangeAsync(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection)
			=> Task.FromResult(ReadRange(searchTerm, pageLength, page, sortProperty, sortDirection));
	}

	#endregion
}
