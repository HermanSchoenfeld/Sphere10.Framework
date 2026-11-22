// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading.Tasks;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Supplies a row node, its resolved entity and its child query source to a detail factory.</summary>
/// <remarks>
/// For hierarchical rows, the grid resolves the entity and child source before invoking <see cref="VisualGrid.CreateDetail"/>.
/// <see cref="ChildDataSource"/> is null for custom details on rows without children. The grid or detail adapter disposes
/// the context when its result is rejected, replaced or closed, releasing the owned child source. Node and entity values remain borrowed.
/// </remarks>
public sealed class VisualGridDetailContext : AsyncDisposable {
	internal VisualGridDetailContext(object node, object entity, IHierarchicalDataSource<object> childDataSource) {
		Node = node;
		Entity = entity;
		ChildDataSource = childDataSource;
	}

	public object Node { get; }

	public object Entity { get; }

	public IHierarchicalDataSource<object> ChildDataSource { get; }

	protected override async ValueTask DisposeInternalAsync() {
		if (ChildDataSource is IAsyncDisposable asynchronous)
			await asynchronous.DisposeAsync().ConfigureAwait(false);
		else if (ChildDataSource is IDisposable disposable)
			disposable.Dispose();
	}
}
