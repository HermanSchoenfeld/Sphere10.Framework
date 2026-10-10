// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Sphere10.Framework.Utils.BlazorTester.Showcase.Cards;

/// <summary>The same lazy, invalidatable IDataSource feeds a custom card presentation and the native BlazorGrid.</summary>
public sealed class CollectionDataSource : BulkFetchDataSource<CreativeCollection> {
	public CollectionDataSource(CollectionCatalogue catalogue)
		: base(() => new ExtendedList<CreativeCollection>(catalogue.ReadAll())) {
		Guard.ArgumentNotNull(catalogue, nameof(catalogue));
	}

	public string FilterText { get; set; } = string.Empty;

	public string StatusFilter { get; set; } = string.Empty;

	public string CategoryFilter { get; set; } = string.Empty;

	public override DataSourceCapabilities Capabilities => DataSourceCapabilities.CanRead | DataSourceCapabilities.CanSearch
		| DataSourceCapabilities.CanSort | DataSourceCapabilities.CanPage;

	public override DataSourceItems<CreativeCollection> ReadRange(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection) {
		Guard.ArgumentGT(pageLength, 0, nameof(pageLength));
		IEnumerable<CreativeCollection> query = Future.Value;
		foreach (var term in new[] { FilterText, searchTerm }.Where(term => !string.IsNullOrWhiteSpace(term))) {
			var trimmed = term.Trim();
			query = query.Where(item => item.Name.Contains(trimmed, StringComparison.OrdinalIgnoreCase)
				|| item.Owner.Contains(trimmed, StringComparison.OrdinalIgnoreCase) || item.Description.Contains(trimmed, StringComparison.OrdinalIgnoreCase));
		}
		if (!string.IsNullOrEmpty(StatusFilter))
			query = query.Where(item => item.Status.ToString() == StatusFilter);
		if (!string.IsNullOrEmpty(CategoryFilter))
			query = query.Where(item => item.Category == CategoryFilter);
		Func<CreativeCollection, object> sortValue = sortProperty switch {
			nameof(CreativeCollection.Owner) => item => item.Owner,
			nameof(CreativeCollection.Category) => item => item.Category,
			nameof(CreativeCollection.Description) => item => item.Description,
			nameof(CreativeCollection.Status) => item => item.Status,
			nameof(CreativeCollection.ItemCount) => item => item.ItemCount,
			nameof(CreativeCollection.Progress) => item => item.Progress,
			nameof(CreativeCollection.Updated) => item => item.Updated,
			_ => item => item.Name
		};
		query = sortDirection == SortDirection.Descending ? query.OrderByDescending(sortValue).ThenBy(item => item.Id)
			: query.OrderBy(sortValue).ThenBy(item => item.Id);
		var items = query.ToArray();
		page = Math.Clamp(page, 0, Math.Max(0, (items.Length - 1) / pageLength));
		return new DataSourceItems<CreativeCollection> { Items = items.Skip(page * pageLength).Take(pageLength).ToArray(), Page = page, TotalCount = items.Length };
	}
}
