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

namespace Sphere10.Framework;

/// <summary>
/// A directed graph node containing an entity and ordered outgoing connections.
/// Connections may contain shared nodes, repeated edges and cycles.
/// </summary>
public class GraphNode<TEntity> : ISimpleGraph<TEntity, GraphNode<TEntity>> {
	public GraphNode(TEntity entity) {
		Entity = entity;
	}

	public TEntity Entity { get; set; }

	public IEnumerable<GraphNode<TEntity>> Connections { get; set; } = Enumerable.Empty<GraphNode<TEntity>>();

	/// <summary>
	/// Visits this node and its connections in depth-first preorder, once per node identity.
	/// A rejected node prunes its outgoing connections. Null connections are skipped.
	/// Each enumeration has independent traversal state; the default comparer uses reference identity.
	/// Connections are evaluated lazily, and their enumerators are disposed on completion, early exit or failure.
	/// </summary>
	public IEnumerable<GraphNode<TEntity>> Visit(Func<GraphNode<TEntity>, bool> discriminator = null, IEqualityComparer<GraphNode<TEntity>> comparer = null) {
		var visited = new HashSet<GraphNode<TEntity>>(comparer ?? ReferenceEqualityComparer.Instance);
		var pending = new Stack<IEnumerator<GraphNode<TEntity>>>();
		IEnumerable<GraphNode<TEntity>> roots = new[] { this };
		try {
			pending.Push(roots.GetEnumerator());
			while (pending.Count > 0) {
				var current = pending.Peek();
				if (!current.MoveNext()) {
					pending.Pop().Dispose();
					continue;
				}
				var node = current.Current;
				if (node is null || visited.Contains(node) || discriminator is not null && !discriminator(node))
					continue;
				visited.Add(node);
				yield return node;
				var connections = node.Connections;
				if (connections is not null)
					pending.Push(connections.GetEnumerator());
			}
		} finally {
			while (pending.Count > 0)
				pending.Pop().Dispose();
		}
	}

	#region Base Interface Bridges

	IEnumerable<GraphNode<TEntity>> ISimpleGraph<GraphNode<TEntity>>.Connections {
		get => Connections;
		set => Connections = value is null ? Enumerable.Empty<GraphNode<TEntity>>() : value.ToArray();
	}

	#endregion
}

