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
/// Abstract base for all data source implementations. Declares all item, batch, sync, and async methods as abstract.
/// </summary>
public abstract class DataSourceBase<TItem> : IDataSource<TItem> {

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

	public abstract int Count { get; }

	public abstract DataSourceCapabilities Capabilities { get; }

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

	public abstract Task<int> CountAsync { get; }

	public abstract Task<DataSourceCapabilities> CapabilitiesAsync { get; }
}

