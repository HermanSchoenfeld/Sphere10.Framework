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
/// Exposes graph roots and their children as a read-only hierarchical data source.
/// </summary>
/// <typeparam name="TEntity">The entity stored directly in each graph node.</typeparam>
/// <remarks>
/// The supplied callback identifies each node. Queries return graph nodes and use their entity values
/// for searching and sorting. Connections are captured when the source is constructed.
/// </remarks>
public sealed class ActionGraphDataSource<TEntity> : GraphDataSourceBase<GraphNode<TEntity>, TEntity> {
	private readonly Func<GraphNode<TEntity>, object> _getNodeKey;

	public ActionGraphDataSource(IEnumerable<GraphNode<TEntity>> roots, Func<GraphNode<TEntity>, object> getNodeKey)
		: base(roots) {
		Guard.ArgumentNotNull(getNodeKey, nameof(getNodeKey));
		_getNodeKey = getNodeKey;
	}

	public override object GetNodeKey(GraphNode<TEntity> node) => _getNodeKey(node);

	protected override TEntity ResolveEntity(GraphNode<TEntity> node) => node.Entity;
}
