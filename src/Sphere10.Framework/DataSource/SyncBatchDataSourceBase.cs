// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Linq;

namespace Sphere10.Framework;

/// <summary>
/// Base class for sync, batch-oriented data source implementations. Item operations delegate to batch operations; asynchronous operations come from
/// <see cref="SyncDataSourceBase{TItem}"/>. The dual is <see cref="AsyncBatchDataSourceBase{TItem}"/>.
/// </summary>
public abstract class SyncBatchDataSourceBase<TItem> : SyncDataSourceBase<TItem> {

	// Sync item: derived from sync batch

	public sealed override TItem New() => NewRange(1).Single();

	public sealed override void Create(TItem entity) => CreateRange(new[] { entity });

	public sealed override TItem Refresh(TItem entity) {
		var entities = new[] { entity };
		RefreshRange(entities);
		return entities[0];
	}

	public sealed override void Update(TItem entity) => UpdateRange(new[] { entity });

	public sealed override void Delete(TItem entity) => DeleteRange(new[] { entity });

	public sealed override Result Validate(TItem entity, CrudAction action) => ValidateRange(new[] { (entity, action) });
}
