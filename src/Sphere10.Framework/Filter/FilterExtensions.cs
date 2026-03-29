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

/// <summary>
/// Extension methods for applying <see cref="FilterExpression"/> and <see cref="SortExpression"/>
/// to in-memory sequences.
/// </summary>
public static class FilterExtensions {

	/// <summary>
	/// Applies a <see cref="FilterExpression"/> as a Where predicate.
	/// </summary>
	public static IEnumerable<T> ApplyFilter<T>(this IEnumerable<T> source, FilterExpression filter) {
		if (filter == null)
			return source;
		var predicate = FilterCompiler<T>.CompileFunc(filter);
		return source.Where(predicate);
	}

	/// <summary>
	/// Applies a <see cref="SortExpression"/> as an OrderBy clause.
	/// </summary>
	public static IOrderedEnumerable<T> ApplySort<T>(this IEnumerable<T> source, SortExpression sort) {
		Guard.ArgumentNotNull(sort, nameof(sort));
		Func<T, object> selector = item => Tools.Reflection.GetPropertyValue(item, sort.Property);
		return sort.Direction == SortDirection.Descending
			? source.OrderByDescending(selector)
			: source.OrderBy(selector);
	}

	/// <summary>
	/// Applies an optional filter, sort, and paging to produce a <see cref="DataSourceItems{T}"/>.
	/// This is the common query pipeline used by data source implementations.
	/// </summary>
	public static DataSourceItems<T> ApplyQuery<T>(
		this IEnumerable<T> source,
		FilterExpression filter = null,
		SortExpression sort = null,
		int pageLength = int.MaxValue,
		int page = 0) {

		var query = source;

		if (filter != null)
			query = query.ApplyFilter(filter);

		if (sort != null && sort.Direction != SortDirection.None)
			query = query.ApplySort(sort);

		var totalItems = query.Count();

		if (pageLength * page > totalItems)
			page = Math.Max(0, (int)Math.Ceiling(totalItems / (decimal)pageLength) - 1);

		return new DataSourceItems<T> {
			Items = query.Skip(pageLength * page).Take(pageLength),
			Page = page,
			TotalCount = totalItems
		};
	}
}
