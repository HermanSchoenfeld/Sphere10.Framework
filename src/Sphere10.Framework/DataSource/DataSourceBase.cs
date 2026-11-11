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
/// Abstract base for all data source implementations. Declares all item, batch, sync, and async methods as abstract.
/// </summary>
/// <remarks>
/// Implementations choose their direction through <see cref="SyncDataSourceBase{TItem}"/> or <see cref="AsyncDataSourceBase{TItem}"/>; decorators
/// derive from <see cref="DataSourceDecorator{TItem, TConcrete}"/> and forward both directions.
/// </remarks>
public abstract class DataSourceBase<TItem> : IDataSource<TItem> {
	public virtual event EventHandlerEx Changed;

	public abstract long Count { get; }

	public abstract DataSourceCapabilities Capabilities { get; }

	public abstract Task<long> CountAsync { get; }

	public abstract Task<DataSourceCapabilities> CapabilitiesAsync { get; }

	// Sync item methods
	public abstract TItem New();

	public abstract void Create(TItem entity);

	public abstract TItem Refresh(TItem entity);

	public abstract void Update(TItem entity);

	public abstract void Delete(TItem entity);

	public abstract Result Validate(TItem entity, CrudAction action);

	// Sync batch methods
	public abstract IEnumerable<TItem> NewRange(int count);

	public abstract void CreateRange(IEnumerable<TItem> entities);

	public abstract DataSourceItems<TItem> ReadRange(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection);

	public abstract void RefreshRange(TItem[] entities);

	public abstract void UpdateRange(IEnumerable<TItem> entities);

	public abstract void DeleteRange(IEnumerable<TItem> entities);

	public abstract Result ValidateRange(IEnumerable<(TItem entity, CrudAction action)> actions);

	// Async item methods
	public abstract Task CreateAsync(TItem entity);

	public abstract Task<TItem> RefreshAsync(TItem entity);

	public abstract Task UpdateAsync(TItem entity);

	public abstract Task DeleteAsync(TItem entity);

	public abstract Task<Result> ValidateAsync(TItem entity, CrudAction action);

	// Async batch methods
	public abstract Task CreateRangeAsync(IEnumerable<TItem> entities);

	public abstract Task<DataSourceItems<TItem>> ReadRangeAsync(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection);

	public abstract Task RefreshRangeAsync(TItem[] entities);

	public abstract Task UpdateRangeAsync(IEnumerable<TItem> entities);

	public abstract Task DeleteRangeAsync(IEnumerable<TItem> entities);

	public abstract Task<Result> ValidateRangeAsync(IEnumerable<(TItem entity, CrudAction action)> actions);

	/// <summary>Notifies accepted data changes outside source locks, isolating one consumer's failure from other consumers and the committed operation.</summary>
	protected void NotifyChanged() => NotifyChanged(Changed);

	/// <summary>Notifies the given subscribers; a decorator which owns its own subscription list passes that list.</summary>
	protected void NotifyChanged(EventHandlerEx subscribers) {
		// The subclass hook first, isolated from the subscribers
		Tools.Exceptions.Execute(OnChanged, error => Tools.Exceptions.ExecuteIgnoringException(() => SystemLog.Logger.Exception(error, "Data source change hook failed.")));

		// Each subscriber in isolation
		if (subscribers == null)
			return;
		foreach (EventHandlerEx subscriber in subscribers.GetInvocationList())
			Tools.Exceptions.Execute(subscriber.Invoke, error => Tools.Exceptions.ExecuteIgnoringException(() => SystemLog.Logger.Exception(error, "Data source change subscriber failed.")));
	}

	protected virtual void OnChanged() {
	}
}
