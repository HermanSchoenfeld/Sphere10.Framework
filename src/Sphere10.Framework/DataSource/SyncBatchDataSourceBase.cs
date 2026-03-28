// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sphere10.Framework;

/// <summary>
/// Base class for sync, batch-oriented data source implementations. Item operations delegate to batch operations, async operations wrap sync via Task.Run.
/// </summary>
public abstract class SyncBatchDataSourceBase<TItem> : DataSourceBase<TItem> {

	// Sync item: derived from sync batch

	public sealed override TItem New() {
     return NewRange(1).Single();
	}

	public sealed override void Create(TItem entity) {
       CreateRange(new[] { entity });
	}

	public sealed override TItem Refresh(TItem entity) {
		var arr = new[] { entity };
       RefreshRange(arr);
		return arr[0];
	}

	public sealed override void Update(TItem entity) {
       UpdateRange(new[] { entity });
	}

	public sealed override void Delete(TItem entity) {
       DeleteRange(new[] { entity });
	}

	public sealed override Result Validate(TItem entity, CrudAction action) {
        return ValidateRange(new[] { (entity, action) });
	}

	// Async item: wraps sync item via Task.Run

	public sealed override Task CreateAsync(TItem entity)
		=> Task.Run(() => Create(entity));

	public sealed override Task<TItem> RefreshAsync(TItem entity)
		=> Task.Run(() => Refresh(entity));

	public sealed override Task UpdateAsync(TItem entity)
		=> Task.Run(() => Update(entity));

	public sealed override Task DeleteAsync(TItem entity)
		=> Task.Run(() => Delete(entity));

	public sealed override Task<Result> ValidateAsync(TItem entity, CrudAction action)
		=> Task.Run(() => Validate(entity, action));

	// Async batch: wraps sync batch via Task.Run

   public override Task CreateRangeAsync(IEnumerable<TItem> entities)
		=> Task.Run(() => CreateRange(entities));

   public override Task<DataSourceItems<TItem>> ReadRangeAsync(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection)
		=> Task.Run(() => ReadRange(searchTerm, pageLength, page, sortProperty, sortDirection));

 public override Task RefreshRangeAsync(TItem[] entities)
		=> Task.Run(() => RefreshRange(entities));

   public override Task UpdateRangeAsync(IEnumerable<TItem> entities)
		=> Task.Run(() => UpdateRange(entities));

   public override Task DeleteRangeAsync(IEnumerable<TItem> entities)
		=> Task.Run(() => DeleteRange(entities));

  public override Task<Result> ValidateRangeAsync(IEnumerable<(TItem entity, CrudAction action)> actions)
		=> Task.Run(() => ValidateRange(actions));

	public override Task<int> CountAsync => Task.Run(() => Count);

	public override Task<DataSourceCapabilities> CapabilitiesAsync => Task.Run(() => Capabilities);
}
