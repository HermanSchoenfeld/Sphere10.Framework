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

public interface IDataSource<TItem> {

	// Sync item methods
	TItem New();

	void Create(TItem entity);

	TItem Refresh(TItem entity);

	void Update(TItem entity);

	void Delete(TItem entity);

	Result Validate(TItem entity, CrudAction action);

	// Sync batch methods
  IEnumerable<TItem> NewRange(int count);

   void CreateRange(IEnumerable<TItem> entities);

 DataSourceItems<TItem> ReadRange(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection);

 void RefreshRange(TItem[] entities);

   void UpdateRange(IEnumerable<TItem> entities);

   void DeleteRange(IEnumerable<TItem> entities);

    Result ValidateRange(IEnumerable<(TItem entity, CrudAction action)> actions);

	int Count { get; }

	DataSourceCapabilities Capabilities { get; }

	// Async item methods
	Task CreateAsync(TItem entity);

	Task<TItem> RefreshAsync(TItem entity);

	Task UpdateAsync(TItem entity);

	Task DeleteAsync(TItem entity);

	Task<Result> ValidateAsync(TItem entity, CrudAction action);

	// Async batch methods
  Task CreateRangeAsync(IEnumerable<TItem> entities);

  Task<DataSourceItems<TItem>> ReadRangeAsync(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection);

    Task RefreshRangeAsync(TItem[] entities);

  Task UpdateRangeAsync(IEnumerable<TItem> entities);

  Task DeleteRangeAsync(IEnumerable<TItem> entities);

 Task<Result> ValidateRangeAsync(IEnumerable<(TItem entity, CrudAction action)> actions);

	Task<int> CountAsync { get; }

	Task<DataSourceCapabilities> CapabilitiesAsync { get; }

}

