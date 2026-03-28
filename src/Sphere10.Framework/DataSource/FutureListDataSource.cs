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

namespace Sphere10.Framework;

public class FutureListDataSource<TEntity> : SyncBatchDataSourceBase<TEntity> {
	protected internal readonly IFuture<IExtendedList<TEntity>> Future;


	public FutureListDataSource(IFuture<IExtendedList<TEntity>> futureList) {
		Guard.ArgumentNotNull(futureList, nameof(futureList));
		Future = futureList;
	}

	protected virtual TEntity NewMethod() => TypeActivator.Activate<TEntity>();

	protected virtual TEntity RefreshMethod(TEntity entity) => entity;

	public override IEnumerable<TEntity> NewRange(int count)
		=> Enumerable.Range(0, count).Select(_ => NewMethod());

	public override void CreateRange(IEnumerable<TEntity> entities) => Future.Value.AddRange(entities);

	public override DataSourceItems<TEntity> ReadRange(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection) {
		var query = Future.Value;
		var totalItems = query.Count();

		if (pageLength * page > totalItems)
			page = (int)System.Math.Ceiling(totalItems / (decimal)pageLength) - 1;

		return new DataSourceItems<TEntity> {
			Items = query.Skip(pageLength * page).Take(pageLength),
			Page = page,
			TotalCount = totalItems
		};
	}

	public override void RefreshRange(TEntity[] entities) {
		for (var i = 0; i < entities.Length; i++)
			entities[i] = RefreshMethod(entities[i]);
	}

	public override void UpdateRange(IEnumerable<TEntity> entities) {
		var list = Future.Value;
		var entitiesArray = entities as TEntity[] ?? entities.ToArray();
		var updates = list.IndexOfRange(entitiesArray).Zip(entitiesArray);
		updates.ForEach(x => list.Update(x.Item1, x.Item2));
	}

	public override void DeleteRange(IEnumerable<TEntity> entities) {
		Future.Value.RemoveRange(entities);
	}

	public override Result ValidateRange(IEnumerable<(TEntity entity, CrudAction action)> actions) => Result.Default;

	public override long Count => Future.Value.Count;

	public override DataSourceCapabilities Capabilities => DataSourceCapabilities.Default;
}
