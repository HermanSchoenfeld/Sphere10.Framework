// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.Framework;

/// <summary>
/// Base for adding parent-child queries and entity access to an existing data source.
/// </summary>
/// <typeparam name="TNode">The row type served by the decorated source.</typeparam>
/// <typeparam name="TEntity">The entity value associated with each row.</typeparam>
/// <remarks>
/// CRUD operations, paging and change notifications are forwarded to the supplied source.
/// Derived classes define node identities, entity access and both forms of child query.
/// The supplied source is borrowed and remains under its existing owner's lifetime control.
/// </remarks>
public abstract class HierarchicalDataSourceBase<TNode, TEntity> : DataSourceDecorator<TNode>, IHierarchicalDataSource<TNode, TEntity> {

	protected HierarchicalDataSourceBase(IDataSource<TNode> dataSource)
		: base(dataSource) {
	}

	public abstract object GetNodeKey(TNode node);

	public virtual object GetChildQueryKey(TNode node) => GetNodeKey(node);

	public abstract bool HasChildren(TNode node);

	public abstract TEntity GetEntity(TNode node);

	public abstract IHierarchicalDataSource<TNode> GetChildren(TNode node);

	public abstract Task<IHierarchicalDataSource<TNode>> GetChildrenAsync(TNode node, CancellationToken cancellationToken = default);
}
