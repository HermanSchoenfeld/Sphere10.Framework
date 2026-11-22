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
/// Extends a data source of sibling nodes with stable identities, entity access and child data sources.
/// </summary>
/// <typeparam name="TNode">The node type used for rows, selection and CRUD operations.</typeparam>
/// <remarks>
/// A node may be the entity itself or a wrapper containing its value. Entity access is synchronous, while child queries
/// have synchronous and asynchronous forms. Each child source represents the data associated with the parent node that
/// requested it and belongs to its caller. A borrowed root source remains the responsibility of its existing owner.
/// Shared nodes may occur under several parents, so consumers detect cycles along the current ancestor path.
/// </remarks>
public interface IHierarchicalDataSource<TNode> : IDataSource<TNode> {

	/// <summary>Returns the non-null stable identity of a node. The identity must not resolve a lazy payload.</summary>
	object GetNodeKey(TNode node);

	/// <summary>Identifies compatible child queries for this node. Change it when pending results must be rejected. Compatible live updates may retain it.</summary>
	object GetChildQueryKey(TNode node);

	/// <summary>Reports whether expansion can produce children, without fetching payloads. An unknown lazy membership may return true.</summary>
	bool HasChildren(TNode node);

	/// <summary>Returns the node's entity value synchronously, resolving a wrapped or deferred value when the source uses one.</summary>
	object GetEntity(TNode node);

	/// <summary>Returns a fresh caller-owned child view, including a non-null empty view for a leaf.</summary>
	/// <remarks>Never return the borrowed root or acquisition owner. The caller releases the view through its supported disposal interface.</remarks>
	IHierarchicalDataSource<TNode> GetChildren(TNode node);

	/// <summary>Asynchronously returns a fresh caller-owned child view, including a non-null empty view for a leaf.</summary>
	/// <remarks>Never return the borrowed root or acquisition owner. The caller releases the view through its supported disposal interface.</remarks>
	Task<IHierarchicalDataSource<TNode>> GetChildrenAsync(TNode node, CancellationToken cancellationToken = default);
}

/// <summary>
/// Provides strongly typed entity values for the nodes in a hierarchical data source.
/// </summary>
/// <typeparam name="TNode">The node type used for rows, selection and CRUD operations.</typeparam>
/// <typeparam name="TEntity">The value returned when a node's entity is requested.</typeparam>
/// <remarks>
/// Nodes and entities may be the same object, or a node may contain the entity directly or through a future.
/// The inherited interface exposes the same entity as an object for callers that do not know its concrete type.
/// </remarks>
public interface IHierarchicalDataSource<TNode, TEntity> : IHierarchicalDataSource<TNode> {

	/// <summary>Returns the node's entity value in its declared type.</summary>
	new TEntity GetEntity(TNode node);

	object IHierarchicalDataSource<TNode>.GetEntity(TNode node) => GetEntity(node);
}
