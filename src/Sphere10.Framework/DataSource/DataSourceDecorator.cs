// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Sphere10.Framework;

/// <summary>
/// Decorator pattern for an IDataSource. All methods are virtual and delegate to the inner data source.
/// </summary>
/// <typeparam name="TItem">The entity type.</typeparam>
/// <typeparam name="TConcrete">The concrete type of the inner data source.</typeparam>
/// <remarks>The <see cref="TConcrete"/> generic argument ensures sub-classes can retrieve the decorated data source
/// in its type, without an expensive chain of casts/retrieves.</remarks>
public abstract class DataSourceDecorator<TItem, TConcrete> : DataSourceBase<TItem>
	where TConcrete : IDataSource<TItem> {

	protected DataSourceDecorator(TConcrete internalDataSource) {
		Guard.ArgumentNotNull(internalDataSource, nameof(internalDataSource));
		InternalDataSource = internalDataSource;
	}

	protected TConcrete InternalDataSource { get; }

	// Sync item methods

	public override TItem New() => InternalDataSource.New();

	public override void Create(TItem entity) => InternalDataSource.Create(entity);

	public override TItem Refresh(TItem entity) => InternalDataSource.Refresh(entity);

	public override void Update(TItem entity) => InternalDataSource.Update(entity);

	public override void Delete(TItem entity) => InternalDataSource.Delete(entity);

	public override Result Validate(TItem entity, CrudAction action) => InternalDataSource.Validate(entity, action);

	// Sync batch methods

	public override IEnumerable<TItem> NewRange(int count) => InternalDataSource.NewRange(count);

	public override void CreateRange(IEnumerable<TItem> entities) => InternalDataSource.CreateRange(entities);

	public override DataSourceItems<TItem> ReadRange(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection)
		=> InternalDataSource.ReadRange(searchTerm, pageLength, page, sortProperty, sortDirection);

	public override void RefreshRange(TItem[] entities) => InternalDataSource.RefreshRange(entities);

	public override void UpdateRange(IEnumerable<TItem> entities) => InternalDataSource.UpdateRange(entities);

	public override void DeleteRange(IEnumerable<TItem> entities) => InternalDataSource.DeleteRange(entities);

	public override Result ValidateRange(IEnumerable<(TItem entity, CrudAction action)> actions) => InternalDataSource.ValidateRange(actions);

	public override int Count => InternalDataSource.Count;

	public override DataSourceCapabilities Capabilities => InternalDataSource.Capabilities;

	// Async item methods

	public override Task CreateAsync(TItem entity) => InternalDataSource.CreateAsync(entity);

	public override Task<TItem> RefreshAsync(TItem entity) => InternalDataSource.RefreshAsync(entity);

	public override Task UpdateAsync(TItem entity) => InternalDataSource.UpdateAsync(entity);

	public override Task DeleteAsync(TItem entity) => InternalDataSource.DeleteAsync(entity);

	public override Task<Result> ValidateAsync(TItem entity, CrudAction action) => InternalDataSource.ValidateAsync(entity, action);

	// Async batch methods

	public override Task CreateRangeAsync(IEnumerable<TItem> entities) => InternalDataSource.CreateRangeAsync(entities);

	public override Task<DataSourceItems<TItem>> ReadRangeAsync(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection)
		=> InternalDataSource.ReadRangeAsync(searchTerm, pageLength, page, sortProperty, sortDirection);

	public override Task RefreshRangeAsync(TItem[] entities) => InternalDataSource.RefreshRangeAsync(entities);

	public override Task UpdateRangeAsync(IEnumerable<TItem> entities) => InternalDataSource.UpdateRangeAsync(entities);

	public override Task DeleteRangeAsync(IEnumerable<TItem> entities) => InternalDataSource.DeleteRangeAsync(entities);

	public override Task<Result> ValidateRangeAsync(IEnumerable<(TItem entity, CrudAction action)> actions) => InternalDataSource.ValidateRangeAsync(actions);

	public override Task<int> CountAsync => InternalDataSource.CountAsync;

	public override Task<DataSourceCapabilities> CapabilitiesAsync => InternalDataSource.CapabilitiesAsync;
}


/// <summary>
/// Decorator pattern for an IDataSource. All methods are virtual and delegate to the inner data source.
/// </summary>
/// <typeparam name="TItem">The entity type.</typeparam>
public abstract class DataSourceDecorator<TItem> : DataSourceDecorator<TItem, IDataSource<TItem>> {
	protected DataSourceDecorator(IDataSource<TItem> internalDataSource)
		: base(internalDataSource) {
	}
}
