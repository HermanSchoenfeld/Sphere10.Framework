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
/// Exposes a graph whose nodes obtain their entity values through futures.
/// </summary>
/// <typeparam name="TEntity">The entity value supplied by each node's future.</typeparam>
/// <remarks>
/// Construction, node identity and child discovery leave future values unresolved.
/// Entity access reads the future's value synchronously. Page reads resolve the returned nodes' values,
/// while searching or sorting may resolve other siblings. Each future controls its own caching and failure behavior.
/// The key selector must identify nodes without reading their future values.
/// </remarks>
public sealed class ActionFutureGraphDataSource<TEntity> : GraphDataSourceBase<GraphNode<IFuture<TEntity>>, TEntity> {
	private readonly Func<GraphNode<IFuture<TEntity>>, object> _getNodeKey;

	public ActionFutureGraphDataSource(IEnumerable<GraphNode<IFuture<TEntity>>> roots, Func<GraphNode<IFuture<TEntity>>, object> getNodeKey)
		: base(roots) {
		Guard.ArgumentNotNull(getNodeKey, nameof(getNodeKey));
		_getNodeKey = getNodeKey;
	}

	public override object GetNodeKey(GraphNode<IFuture<TEntity>> node) => _getNodeKey(node);

	protected override TEntity ResolveEntity(GraphNode<IFuture<TEntity>> node) => node.Entity.Value;
}
