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
/// Provides read-only pages of sibling nodes from a captured graph, with searching and sorting based on entity values.
/// </summary>
/// <typeparam name="TNode">A graph node whose connections identify its children.</typeparam>
/// <typeparam name="TEntity">The entity value used to display, search and sort a node.</typeparam>
/// <remarks>
/// Construction captures roots and connections without resolving entity values. Later connection changes require a new source.
/// Page reads resolve the returned nodes' entities, and search or sort may resolve other siblings as well.
/// Derived classes supply stable node keys and entity access. Child views share the captured graph and do not own its entities.
/// </remarks>
public abstract class GraphDataSourceBase<TNode, TEntity> : HierarchicalDataSourceBase<TNode, TEntity>
	where TNode : class, ISimpleGraph<TNode> {
	private readonly GraphData _graph;
	private readonly NodeStorage _storage;
	private readonly GraphDataSourceBase<TNode, TEntity> _root;

	protected GraphDataSourceBase(IEnumerable<TNode> roots)
		: this(new GraphData(roots), null, null) {
	}

	private GraphDataSourceBase(GraphData graph, TNode[] rows, GraphDataSourceBase<TNode, TEntity> root)
		: base(new NodeStorage(rows ?? graph.Roots)) {
		_graph = graph;
		_root = root ?? this;
		_storage = (NodeStorage)InternalDataSource;
		_storage.Owner = _root;
	}

	public string DefaultSortProperty {
		get => _storage.DefaultSortProperty;
		set => _storage.DefaultSortProperty = value;
	}

	public SortDirection DefaultSortDirection {
		get => _storage.DefaultSortDirection;
		set => _storage.DefaultSortDirection = value;
	}

	public override bool HasChildren(TNode node) => GetConnections(node).Length > 0;

	public override object GetChildQueryKey(TNode node) => new ChildQueryKey(_graph, GetNodeKey(node));

	public override TEntity GetEntity(TNode node) => _storage.GetEntity(node);

	public override IHierarchicalDataSource<TNode> GetChildren(TNode node)
		=> new ChildView(_graph, GetConnections(node), _root) {
			DefaultSortProperty = DefaultSortProperty,
			DefaultSortDirection = DefaultSortDirection
		};

	public override Task<IHierarchicalDataSource<TNode>> GetChildrenAsync(TNode node, CancellationToken cancellationToken = default) {
		cancellationToken.ThrowIfCancellationRequested();
		return Task.FromResult(GetChildren(node));
	}

	protected abstract TEntity ResolveEntity(TNode node);

	private TNode[] GetConnections(TNode node) {
		Guard.ArgumentNotNull(node, nameof(node));
		if (!_graph.Connections.TryGetValue(node, out var connections))
			throw new ArgumentException("The node does not belong to this graph's captured data.", nameof(node));
		return connections;
	}

	#region Inner Types
	/// <summary>Identifies a node within a captured graph so a replacement graph invalidates earlier child queries.</summary>
	private sealed record ChildQueryKey(GraphData Graph, object NodeKey);

	/// <summary>Stores the captured roots and connections shared by the root source and all of its child views.</summary>
	private sealed class GraphData {
		public GraphData(IEnumerable<TNode> roots) {
			Guard.ArgumentNotNull(roots, nameof(roots));
			var graph = roots.BuildGraph<TNode>(node => node.Connections, comparer: ReferenceEqualityComparer.Instance);
			Roots = graph.Select(node => node.Entity).ToArray();
			Connections = new Dictionary<TNode, TNode[]>(ReferenceEqualityComparer.Instance);
			foreach (var root in graph)
				foreach (var node in root.Visit(item => !Connections.ContainsKey(item.Entity)))
					Connections.Add(node.Entity, node.Connections.Select(child => child.Entity).ToArray());
		}

		public TNode[] Roots { get; }

		public Dictionary<TNode, TNode[]> Connections { get; }
	}

	/// <summary>Queries one sibling list by entity values while returning the original graph nodes.</summary>
	private sealed class NodeStorage : ArrayDataSource<TNode> {
		public NodeStorage(TNode[] rows)
			: base(rows) {
		}

		public GraphDataSourceBase<TNode, TEntity> Owner { get; set; }

		public override DataSourceCapabilities Capabilities => DataSourceCapabilities.ReadOnly;

		public TEntity GetEntity(TNode node) => Owner.ResolveEntity(node);

		public override DataSourceItems<TNode> ReadRange(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection) {
			var entities = new Dictionary<TNode, TEntity>(ReferenceEqualityComparer.Instance);
			var result = Tools.DataSources.Query(Data, ResolveOnce, searchTerm, pageLength, page, sortProperty, sortDirection,
				FormatValue, DefaultSortProperty, DefaultSortDirection);
			foreach (var node in result.Items)
				ResolveOnce(node);
			return result;

			TEntity ResolveOnce(TNode node) {
				if (!entities.TryGetValue(node, out var entity)) {
					entity = GetEntity(node);
					entities.Add(node, entity);
				}
				return entity;
			}
		}
	}

	/// <summary>Serves one node's captured children using the root source's identity and entity-access rules.</summary>
	private sealed class ChildView : GraphDataSourceBase<TNode, TEntity> {
		public ChildView(GraphData graph, TNode[] rows, GraphDataSourceBase<TNode, TEntity> root)
			: base(graph, rows, root) {
		}

		public override object GetNodeKey(TNode node) => _root.GetNodeKey(node);

		public override object GetChildQueryKey(TNode node) => _root.GetChildQueryKey(node);

		protected override TEntity ResolveEntity(TNode node) => _root.ResolveEntity(node);
	}
	#endregion
}
