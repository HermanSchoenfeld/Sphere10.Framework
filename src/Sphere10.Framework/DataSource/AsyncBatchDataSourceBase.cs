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
/// Base class for async, batch-oriented data source implementations. Item operations delegate to batch operations, sync operations wrap async via .WaitSafe/.ResultSafe.
/// </summary>
public abstract class AsyncBatchDataSourceBase<TItem> : DataSourceBase<TItem> {

	// Async item: derived from async batch

	public sealed override async Task CreateAsync(TItem entity) {
        await CreateRangeAsync(new[] { entity });
	}

	public sealed override async Task<TItem> RefreshAsync(TItem entity) {
		var arr = new[] { entity };
        await RefreshRangeAsync(arr);
		return arr[0];
	}

	public sealed override async Task UpdateAsync(TItem entity) {
        await UpdateRangeAsync(new[] { entity });
	}

	public sealed override async Task DeleteAsync(TItem entity) {
        await DeleteRangeAsync(new[] { entity });
	}

	public sealed override Task<Result> ValidateAsync(TItem entity, CrudAction action) {
       return ValidateRangeAsync(new[] { (entity, action) });
	}

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

	// Sync batch: wraps async batch via .WaitSafe/.ResultSafe

    public override void CreateRange(IEnumerable<TItem> entities)
		=> CreateRangeAsync(entities).WaitSafe();

  public override DataSourceItems<TItem> ReadRange(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection)
		=> ReadRangeAsync(searchTerm, pageLength, page, sortProperty, sortDirection).ResultSafe();

  public override void RefreshRange(TItem[] entities)
		=> RefreshRangeAsync(entities).WaitSafe();

    public override void UpdateRange(IEnumerable<TItem> entities)
		=> UpdateRangeAsync(entities).WaitSafe();

    public override void DeleteRange(IEnumerable<TItem> entities)
		=> DeleteRangeAsync(entities).WaitSafe();

 public override Result ValidateRange(IEnumerable<(TItem entity, CrudAction action)> actions)
		=> ValidateRangeAsync(actions).ResultSafe();

	public override int Count => CountAsync.ResultSafe();

	public override DataSourceCapabilities Capabilities => CapabilitiesAsync.ResultSafe();
}
