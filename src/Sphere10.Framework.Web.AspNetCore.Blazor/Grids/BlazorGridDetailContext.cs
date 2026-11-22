// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>Provides the row, resolved entity and child source passed to a Blazor grid's detail template.</summary>
/// <typeparam name="TNode">The row type used by the grid's columns, selection and data source.</typeparam>
/// <remarks>
/// The grid creates this context after resolving an expanded row. Hierarchical sources supply the entity and immediate-child view.
/// Ordinary rows use the row itself as the entity and have no child source.
/// The grid owns the child view and releases it when replaced, collapsed or disposed. Templates borrow it for the current detail presentation.
/// </remarks>
public sealed class BlazorGridDetailContext<TNode> {

	/// <summary>Associates an expanded row with its resolved payload and optional child view.</summary>
	public BlazorGridDetailContext(TNode node, object entity, IHierarchicalDataSource<TNode> childDataSource) {
		Node = node;
		Entity = entity;
		ChildDataSource = childDataSource;
	}

	/// <summary>The row whose detail is being displayed.</summary>
	public TNode Node { get; }

	/// <summary>The hierarchy source's resolved payload, or <see cref="Node"/> for an ordinary row.</summary>
	public object Entity { get; }

	/// <summary>A borrowed child view, or null for an ordinary row or a known hierarchical leaf.</summary>
	public IHierarchicalDataSource<TNode> ChildDataSource { get; }
}
