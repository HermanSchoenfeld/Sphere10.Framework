// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT NON-AI software license, see the accompanying file
// LICENSE or visit https://sphere10.com/legal/NON-AI-MIT.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Sphere10.Framework;

public class ProjectedDataSource<TFrom, TTo> : IDataSource<TTo> {
	private readonly IDataSource<TFrom> _source;
	private readonly Func<TFrom, TTo> _projection;
   private readonly Func<TTo, TFrom>? _inverseProjection;

    public ProjectedDataSource(IDataSource<TFrom> source, Func<TFrom, TTo> projection, Func<TTo, TFrom>? inverseProjection = null) {
		Guard.ArgumentNotNull(source, nameof(source));
		Guard.ArgumentNotNull(projection, nameof(projection));
		_source = source;
		_projection = projection;
		_inverseProjection = inverseProjection;
	}

	private TFrom ToSource(TTo entity) {
		if (_inverseProjection == null)
			throw new NotSupportedException("Inverse projection is not available for this projected data source.");
		return _inverseProjection(entity);
	}

	// Sync item methods

	public TTo New()
		=> _projection(_source.New());

	public void Create(TTo entity)
      => _source.Create(ToSource(entity));

	public TTo Refresh(TTo entity)
        => _projection(_source.Refresh(ToSource(entity)));

	public void Update(TTo entity)
      => _source.Update(ToSource(entity));

	public void Delete(TTo entity)
      => _source.Delete(ToSource(entity));

	public Result Validate(TTo entity, CrudAction action)
        => _source.Validate(ToSource(entity), action);

	// Sync batch methods

  public IEnumerable<TTo> NewRange(int count)
		=> _source.NewRange(count).Select(_projection);

   public void CreateRange(IEnumerable<TTo> entities)
		=> _source.CreateRange(entities.Select(ToSource));

   public DataSourceItems<TTo> ReadRange(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection) {
		var result = _source.ReadRange(searchTerm, pageLength, page, sortProperty, sortDirection);
		return new DataSourceItems<TTo> {
			Items = result.Items.Select(_projection),
			Page = result.Page,
			TotalCount = result.TotalCount
		};
	}

   public void RefreshRange(TTo[] entities) {
		var typed = entities.Select(ToSource).ToArray();
		_source.RefreshRange(typed);
		for (var i = 0; i < entities.Length; i++)
			entities[i] = _projection(typed[i]);
	}

   public void UpdateRange(IEnumerable<TTo> entities)
		=> _source.UpdateRange(entities.Select(ToSource));

   public void DeleteRange(IEnumerable<TTo> entities)
		=> _source.DeleteRange(entities.Select(ToSource));

    public Result ValidateRange(IEnumerable<(TTo entity, CrudAction action)> actions)
		=> _source.ValidateRange(actions.Select(a => (ToSource(a.entity), a.action)));

	public int Count => _source.Count;

	public DataSourceCapabilities Capabilities => _source.Capabilities;

	// Async item methods

	public Task CreateAsync(TTo entity)
     => _source.CreateAsync(ToSource(entity));

	public async Task<TTo> RefreshAsync(TTo entity)
     => _projection(await _source.RefreshAsync(ToSource(entity)));

	public Task UpdateAsync(TTo entity)
     => _source.UpdateAsync(ToSource(entity));

	public Task DeleteAsync(TTo entity)
     => _source.DeleteAsync(ToSource(entity));

	public Task<Result> ValidateAsync(TTo entity, CrudAction action)
       => _source.ValidateAsync(ToSource(entity), action);

	// Async batch methods

  public Task CreateRangeAsync(IEnumerable<TTo> entities)
		=> _source.CreateRangeAsync(entities.Select(ToSource));

  public async Task<DataSourceItems<TTo>> ReadRangeAsync(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection) {
		var result = await _source.ReadRangeAsync(searchTerm, pageLength, page, sortProperty, sortDirection);
		return new DataSourceItems<TTo> {
			Items = result.Items.Select(_projection),
			Page = result.Page,
			TotalCount = result.TotalCount
		};
	}

    public async Task RefreshRangeAsync(TTo[] entities) {
		var typed = entities.Select(ToSource).ToArray();
		await _source.RefreshRangeAsync(typed);
		for (var i = 0; i < entities.Length; i++)
			entities[i] = _projection(typed[i]);
	}

  public Task UpdateRangeAsync(IEnumerable<TTo> entities)
		=> _source.UpdateRangeAsync(entities.Select(ToSource));

  public Task DeleteRangeAsync(IEnumerable<TTo> entities)
		=> _source.DeleteRangeAsync(entities.Select(ToSource));

 public Task<Result> ValidateRangeAsync(IEnumerable<(TTo entity, CrudAction action)> actions)
		=> _source.ValidateRangeAsync(actions.Select(a => (ToSource(a.entity), a.action)));

	public Task<int> CountAsync => _source.CountAsync;

	public Task<DataSourceCapabilities> CapabilitiesAsync => _source.CapabilitiesAsync;
}
