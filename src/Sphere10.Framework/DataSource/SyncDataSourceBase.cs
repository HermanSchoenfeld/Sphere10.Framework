// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sphere10.Framework;

/// <summary>
/// Base class for data sources implemented synchronously. Every asynchronous member runs its synchronous counterpart on a worker via
/// <see cref="Task.Run(System.Action)"/>; the dual is <see cref="AsyncDataSourceBase{TItem}"/>.
/// </summary>
public abstract class SyncDataSourceBase<TItem> : DataSourceBase<TItem> {

	public override Task<long> CountAsync => Task.Run(() => Count);

	public override Task<DataSourceCapabilities> CapabilitiesAsync => Task.Run(() => Capabilities);

	// Async item: runs the sync item on a worker

	public override Task CreateAsync(TItem entity) => Task.Run(() => Create(entity));

	public override Task<TItem> RefreshAsync(TItem entity) => Task.Run(() => Refresh(entity));

	public override Task UpdateAsync(TItem entity) => Task.Run(() => Update(entity));

	public override Task DeleteAsync(TItem entity) => Task.Run(() => Delete(entity));

	public override Task<Result> ValidateAsync(TItem entity, CrudAction action) => Task.Run(() => Validate(entity, action));

	// Async batch: runs the sync batch on a worker

	public override Task CreateRangeAsync(IEnumerable<TItem> entities) => Task.Run(() => CreateRange(entities));

	public override Task<DataSourceItems<TItem>> ReadRangeAsync(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection)
		=> Task.Run(() => ReadRange(searchTerm, pageLength, page, sortProperty, sortDirection));

	public override Task RefreshRangeAsync(TItem[] entities) => Task.Run(() => RefreshRange(entities));

	public override Task UpdateRangeAsync(IEnumerable<TItem> entities) => Task.Run(() => UpdateRange(entities));

	public override Task DeleteRangeAsync(IEnumerable<TItem> entities) => Task.Run(() => DeleteRange(entities));

	public override Task<Result> ValidateRangeAsync(IEnumerable<(TItem entity, CrudAction action)> actions) => Task.Run(() => ValidateRange(actions));
}
