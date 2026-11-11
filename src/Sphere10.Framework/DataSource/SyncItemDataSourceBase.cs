// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;
using System.Linq;

namespace Sphere10.Framework;

/// <summary>
/// Base class for sync, item-by-item data source implementations. Batch operations loop over item operations; asynchronous operations come from
/// <see cref="SyncDataSourceBase{TItem}"/>. The dual is <see cref="AsyncItemDataSourceBase{TItem}"/>.
/// </summary>
public abstract class SyncItemDataSourceBase<TItem> : SyncDataSourceBase<TItem> {

	// Sync batch: derived from sync item

	public override IEnumerable<TItem> NewRange(int count) => Enumerable.Range(0, count).Select(_ => New());

	public override void CreateRange(IEnumerable<TItem> entities) {
		foreach (var entity in entities)
			Create(entity);
	}

	public override void RefreshRange(TItem[] entities) {
		for (var i = 0; i < entities.Length; i++)
			entities[i] = Refresh(entities[i]);
	}

	public override void UpdateRange(IEnumerable<TItem> entities) {
		foreach (var entity in entities)
			Update(entity);
	}

	public override void DeleteRange(IEnumerable<TItem> entities) {
		foreach (var entity in entities)
			Delete(entity);
	}

	public override Result ValidateRange(IEnumerable<(TItem entity, CrudAction action)> actions) {
		var result = Result.Default;
		foreach (var (entity, action) in actions) {
			var itemResult = Validate(entity, action);
			if (itemResult.IsFailure)
				foreach (var error in itemResult.ErrorMessages)
					result.AddError(error);
		}
		return result;
	}
}
