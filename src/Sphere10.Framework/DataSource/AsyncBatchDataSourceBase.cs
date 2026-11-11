// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Linq;
using System.Threading.Tasks;

namespace Sphere10.Framework;

/// <summary>
/// Base class for async, batch-oriented data source implementations. Item operations delegate to batch operations; synchronous operations come from
/// <see cref="AsyncDataSourceBase{TItem}"/>. The dual is <see cref="SyncBatchDataSourceBase{TItem}"/>.
/// </summary>
public abstract class AsyncBatchDataSourceBase<TItem> : AsyncDataSourceBase<TItem> {

	// Sync item: creation has no asynchronous form

	public sealed override TItem New() => NewRange(1).Single();

	// Async item: derived from async batch

	public sealed override Task CreateAsync(TItem entity) => CreateRangeAsync(new[] { entity });

	public sealed override async Task<TItem> RefreshAsync(TItem entity) {
		var entities = new[] { entity };
		await RefreshRangeAsync(entities);
		return entities[0];
	}

	public sealed override Task UpdateAsync(TItem entity) => UpdateRangeAsync(new[] { entity });

	public sealed override Task DeleteAsync(TItem entity) => DeleteRangeAsync(new[] { entity });

	public sealed override Task<Result> ValidateAsync(TItem entity, CrudAction action) => ValidateRangeAsync(new[] { (entity, action) });
}
