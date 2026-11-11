// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework;

namespace Tools;

/// <summary>Shared data-source algorithms and one-shot acquisition helpers.</summary>
public static class DataSources {
	/// <summary>Reads every row of a source, loading it first when it requires a load.</summary>
	public static async Task<T[]> ReadAllAsync<T>(IDataSource<T> source, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(source, nameof(source));
		cancellationToken.ThrowIfCancellationRequested();
		if (source is ILoadable { RequiresLoad: true } loadable)
			await loadable.LoadAsync(cancellationToken).ConfigureAwait(false);
		cancellationToken.ThrowIfCancellationRequested();
		var page = await source.ReadRangeAsync(null, int.MaxValue, 0, null, SortDirection.None).ConfigureAwait(false);
		return page.Items as T[] ?? page.Items.ToArray();
	}

	/// <summary>Creates a one-shot source on a worker, reads all of its rows and releases it, including when this caller is canceled.</summary>
	public static async Task<T[]> CaptureAsync<T>(Func<IDataSource<T>> sourceFactory, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(sourceFactory, nameof(sourceFactory));
		cancellationToken.ThrowIfCancellationRequested();
		var source = await Task.Run(sourceFactory, cancellationToken).ConfigureAwait(false);
		Guard.Ensure(source != null, "A source factory must return its owned source.");
		try {
			return await ReadAllAsync(source, cancellationToken).ConfigureAwait(false);
		} finally {
			switch (source) {
				case IAsyncDisposable asyncDisposable:
					await asyncDisposable.DisposeAsync().ConfigureAwait(false);
					break;
				case IDisposable disposable:
					disposable.Dispose();
					break;
			}
		}
	}

	/// <summary>Queries rows, applying filtering/sorting before clamped paging. Natural extended-list reads copy only the requested range.</summary>
	public static DataSourceItems<T> Query<T>(IEnumerable<T> source, string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection,
		Func<object, string> formatValue = null, string defaultSortProperty = null, SortDirection defaultSortDirection = SortDirection.Ascending) {
		Guard.ArgumentNotNull(source, nameof(source));
		Guard.ArgumentGT(pageLength, 0, nameof(pageLength));
		Guard.ArgumentGTE(page, 0, nameof(page));
		if (sortDirection != SortDirection.None && string.IsNullOrEmpty(sortProperty)) {
			sortProperty = defaultSortProperty;
			sortDirection = defaultSortDirection;
		}
		if (string.IsNullOrWhiteSpace(searchTerm) && (sortDirection == SortDirection.None || string.IsNullOrEmpty(sortProperty)) && source is IReadOnlyExtendedList<T> list) {
			var count = source is IExtendedCollection<T> extended ? checked((int)extended.Count) : list.Count;
			var actual = count == 0 ? 0 : Math.Min(page, (count - 1) / pageLength);
			var offset = (long)actual * pageLength;
			return new DataSourceItems<T> { Items = list.ReadRange(offset, Math.Min(pageLength, count - offset)).ToArray(), Page = actual, TotalCount = count };
		}
		var format = formatValue ?? (value => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);
		IEnumerable<T> query = source;
		if (!string.IsNullOrWhiteSpace(searchTerm))
			query = query.Where(row => QueryProperties<T>.Properties.Any(property => (format(ReadProperty(row, property)) ?? string.Empty).Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));
		if (sortDirection != SortDirection.None && !string.IsNullOrEmpty(sortProperty)) {
			// A column may sort by a name that is not a data property (a scalar row or a computed column): scalar rows then sort by their own value and other rows keep the source order.
			var property = QueryProperties<T>.Properties.FirstOrDefault(candidate => string.Equals(candidate.Name, sortProperty, StringComparison.OrdinalIgnoreCase));
			var comparer = Comparer<object>.Create((left, right) => CompareValues(left, right, format));
			Func<T, object> key = property != null ? row => ReadProperty(row, property) : typeof(IComparable).IsAssignableFrom(typeof(T)) ? row => row : null;
			if (key != null)
				query = sortDirection == SortDirection.Descending ? query.OrderByDescending(key, comparer) : query.OrderBy(key, comparer);
		}
		var rows = query as T[] ?? query.ToArray();
		var actualPage = rows.Length == 0 ? 0 : Math.Min(page, (rows.Length - 1) / pageLength);
		var start = checked((int)((long)actualPage * pageLength));
		var items = new T[Math.Min(pageLength, rows.Length - start)];
		System.Array.Copy(rows, start, items, 0, items.Length);
		return new DataSourceItems<T> { Items = items, Page = actualPage, TotalCount = rows.Length };
	}

	private static object ReadProperty<T>(T row, PropertyInfo property) => row is null ? null : property.GetValue(row);

	private static int CompareValues(object left, object right, Func<object, string> format) {
		if (ReferenceEquals(left, right))
			return 0;
		if (left == null)
			return -1;
		if (right == null)
			return 1;
		if (left is string leftText && right is string rightText)
			return StringComparer.OrdinalIgnoreCase.Compare(leftText, rightText);
		if (left.GetType() == right.GetType() && left is IComparable comparable)
			return comparable.CompareTo(right);
		return StringComparer.OrdinalIgnoreCase.Compare(format(left), format(right));
	}

	#region Inner Types
	private static class QueryProperties<T> {
		public static readonly PropertyInfo[] Properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public)
			.Where(property => property.GetMethod?.IsPublic == true && property.GetIndexParameters().Length == 0).ToArray();
	}
	#endregion

}
