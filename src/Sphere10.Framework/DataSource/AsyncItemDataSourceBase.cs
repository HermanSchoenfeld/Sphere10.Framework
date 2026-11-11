// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sphere10.Framework;

/// <summary>
/// Base class for async, item-by-item data source implementations. Batch operations loop over item operations; synchronous operations come from
/// <see cref="AsyncDataSourceBase{TItem}"/>. The dual is <see cref="SyncItemDataSourceBase{TItem}"/>.
/// </summary>
public abstract class AsyncItemDataSourceBase<TItem> : AsyncDataSourceBase<TItem> {

	// Sync batch: creation has no asynchronous form

	public override IEnumerable<TItem> NewRange(int count) => Enumerable.Range(0, count).Select(_ => New());

	// Async batch: derived from async item

	public override async Task CreateRangeAsync(IEnumerable<TItem> entities) {
		foreach (var entity in entities)
			await CreateAsync(entity);
	}

	public override async Task RefreshRangeAsync(TItem[] entities) {
		for (var i = 0; i < entities.Length; i++)
			entities[i] = await RefreshAsync(entities[i]);
	}

	public override async Task UpdateRangeAsync(IEnumerable<TItem> entities) {
		foreach (var entity in entities)
			await UpdateAsync(entity);
	}

	public override async Task DeleteRangeAsync(IEnumerable<TItem> entities) {
		foreach (var entity in entities)
			await DeleteAsync(entity);
	}

	public override async Task<Result> ValidateRangeAsync(IEnumerable<(TItem entity, CrudAction action)> actions) {
		var result = Result.Default;
		foreach (var (entity, action) in actions) {
			var itemResult = await ValidateAsync(entity, action);
			if (itemResult.IsFailure)
				foreach (var error in itemResult.ErrorMessages)
					result.AddError(error);
		}
		return result;
	}
}
