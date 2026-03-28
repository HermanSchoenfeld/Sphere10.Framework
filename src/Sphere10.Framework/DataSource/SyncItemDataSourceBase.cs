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
/// Base class for sync, item-by-item data source implementations. Batch operations loop over item operations, async operations wrap sync via Task.Run.
/// </summary>
public abstract class SyncItemDataSourceBase<TItem> : DataSourceBase<TItem> {

	// Sync batch: derived from sync item

 public override IEnumerable<TItem> NewRange(int count) {
		return Enumerable.Range(0, count).Select(_ => New());
	}

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

	// Async item: wraps sync item via Task.Run

	public override Task CreateAsync(TItem entity)
		=> Task.Run(() => Create(entity));

	public override Task<TItem> RefreshAsync(TItem entity)
		=> Task.Run(() => Refresh(entity));

	public override Task UpdateAsync(TItem entity)
		=> Task.Run(() => Update(entity));

	public override Task DeleteAsync(TItem entity)
		=> Task.Run(() => Delete(entity));

	public override Task<Result> ValidateAsync(TItem entity, CrudAction action)
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

	public override Task<long> CountAsync => Task.Run(() => Count);

	public override Task<DataSourceCapabilities> CapabilitiesAsync => Task.Run(() => Capabilities);
}
