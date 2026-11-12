// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Sphere10.Framework;

/// <summary>
/// Base class for data sources implemented asynchronously. Every synchronous member waits on its asynchronous counterpart (sync-over-async), which
/// must not run on the UI thread; the dual is <see cref="SyncDataSourceBase{TItem}"/>.
/// </summary>
public abstract class AsyncDataSourceBase<TItem> : DataSourceBase<TItem> {

	public override long Count {
		get {
			var result = CountAsync;
			if (result.IsCompletedSuccessfully)
				return result.Result;
			if (!result.IsCompleted)
				Debug.AssertNotMainThread();
			return result.ResultSafe();
		}
	}

	public override DataSourceCapabilities Capabilities {
		get {
			var result = CapabilitiesAsync;
			if (result.IsCompletedSuccessfully)
				return result.Result;
			if (!result.IsCompleted)
				Debug.AssertNotMainThread();
			return result.ResultSafe();
		}
	}

	// Sync item: waits on the async item

	public override void Create(TItem entity) {
		Debug.AssertNotMainThread();
		Task.ExecuteSyncOverAsync(() => CreateAsync(entity));
	}

	public override TItem Refresh(TItem entity) {
		Debug.AssertNotMainThread();
		return Task.ExecuteSyncOverAsync(() => RefreshAsync(entity));
	}

	public override void Update(TItem entity) {
		Debug.AssertNotMainThread();
		Task.ExecuteSyncOverAsync(() => UpdateAsync(entity));
	}

	public override void Delete(TItem entity) {
		Debug.AssertNotMainThread();
		Task.ExecuteSyncOverAsync(() => DeleteAsync(entity));
	}

	public override Result Validate(TItem entity, CrudAction action) {
		Debug.AssertNotMainThread();
		return Task.ExecuteSyncOverAsync(() => ValidateAsync(entity, action));
	}

	// Sync batch: waits on the async batch

	public override void CreateRange(IEnumerable<TItem> entities) {
		Debug.AssertNotMainThread();
		Task.ExecuteSyncOverAsync(() => CreateRangeAsync(entities));
	}

	public override DataSourceItems<TItem> ReadRange(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection) {
		Debug.AssertNotMainThread();
		return Task.ExecuteSyncOverAsync(() => ReadRangeAsync(searchTerm, pageLength, page, sortProperty, sortDirection));
	}

	public override void RefreshRange(TItem[] entities) {
		Debug.AssertNotMainThread();
		Task.ExecuteSyncOverAsync(() => RefreshRangeAsync(entities));
	}

	public override void UpdateRange(IEnumerable<TItem> entities) {
		Debug.AssertNotMainThread();
		Task.ExecuteSyncOverAsync(() => UpdateRangeAsync(entities));
	}

	public override void DeleteRange(IEnumerable<TItem> entities) {
		Debug.AssertNotMainThread();
		Task.ExecuteSyncOverAsync(() => DeleteRangeAsync(entities));
	}

	public override Result ValidateRange(IEnumerable<(TItem entity, CrudAction action)> actions) {
		Debug.AssertNotMainThread();
		return Task.ExecuteSyncOverAsync(() => ValidateRangeAsync(actions));
	}
}
