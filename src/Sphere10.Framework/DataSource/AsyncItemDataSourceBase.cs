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
/// Base class for async, item-by-item data source implementations. Batch operations loop over item operations, sync operations wrap async via .WaitSafe/.ResultSafe.
/// </summary>
public abstract class AsyncItemDataSourceBase<TItem> : DataSourceBase<TItem> {

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

	// Sync item: wraps async item via .WaitSafe/.ResultSafe

	public override void Create(TItem entity) {
		DataSourceThreadDiagnostics.Default.AssertBackgroundExecution(this);
		CreateAsync(entity).WaitSafe();
	}

	public override TItem Refresh(TItem entity) {
		DataSourceThreadDiagnostics.Default.AssertBackgroundExecution(this);
		return RefreshAsync(entity).ResultSafe();
	}

	public override void Update(TItem entity) {
		DataSourceThreadDiagnostics.Default.AssertBackgroundExecution(this);
		UpdateAsync(entity).WaitSafe();
	}

	public override void Delete(TItem entity) {
		DataSourceThreadDiagnostics.Default.AssertBackgroundExecution(this);
		DeleteAsync(entity).WaitSafe();
	}

	public override Result Validate(TItem entity, CrudAction action) {
		DataSourceThreadDiagnostics.Default.AssertBackgroundExecution(this);
		return ValidateAsync(entity, action).ResultSafe();
	}

	// Sync batch: wraps async batch via .WaitSafe/.ResultSafe

 public override IEnumerable<TItem> NewRange(int count) {
		return Enumerable.Range(0, count).Select(_ => New());
	}

	public override void CreateRange(IEnumerable<TItem> entities) {
		DataSourceThreadDiagnostics.Default.AssertBackgroundExecution(this);
		CreateRangeAsync(entities).WaitSafe();
	}

	public override DataSourceItems<TItem> ReadRange(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection) {
		DataSourceThreadDiagnostics.Default.AssertBackgroundExecution(this);
		return ReadRangeAsync(searchTerm, pageLength, page, sortProperty, sortDirection).ResultSafe();
	}

	public override void RefreshRange(TItem[] entities) {
		DataSourceThreadDiagnostics.Default.AssertBackgroundExecution(this);
		RefreshRangeAsync(entities).WaitSafe();
	}

	public override void UpdateRange(IEnumerable<TItem> entities) {
		DataSourceThreadDiagnostics.Default.AssertBackgroundExecution(this);
		UpdateRangeAsync(entities).WaitSafe();
	}

	public override void DeleteRange(IEnumerable<TItem> entities) {
		DataSourceThreadDiagnostics.Default.AssertBackgroundExecution(this);
		DeleteRangeAsync(entities).WaitSafe();
	}

	public override Result ValidateRange(IEnumerable<(TItem entity, CrudAction action)> actions) {
		DataSourceThreadDiagnostics.Default.AssertBackgroundExecution(this);
		return ValidateRangeAsync(actions).ResultSafe();
	}

	public override long Count {
		get {
			var result = CountAsync;
			if (result.IsCompletedSuccessfully)
				return result.Result;
			if (!result.IsCompleted)
				DataSourceThreadDiagnostics.Default.AssertBackgroundExecution(this);
			return result.ResultSafe();
		}
	}

	public override DataSourceCapabilities Capabilities {
		get {
			var result = CapabilitiesAsync;
			if (result.IsCompletedSuccessfully)
				return result.Result;
			if (!result.IsCompleted)
				DataSourceThreadDiagnostics.Default.AssertBackgroundExecution(this);
			return result.ResultSafe();
		}
	}
}
