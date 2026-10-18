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

namespace Sphere10.Framework;

/// <summary>A read-only, searchable and sortable data source whose collection membership is atomically replaced.</summary>
public class SnapshotDataSource<T> : SyncBatchDataSourceBase<T> {
	private static readonly PropertyInfo[] _properties = typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public)
		.Where(property => property.GetMethod?.IsPublic == true && property.GetIndexParameters().Length == 0).ToArray();
	private readonly Func<object, string> _formatValue;
	private readonly SnapshotValueComparer _comparer;
	private T[] _snapshot = Array.Empty<T>();

	public SnapshotDataSource(Func<object, string> formatValue = null) {
		_formatValue = formatValue ?? (value => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty);
		_comparer = new SnapshotValueComparer(_formatValue);
	}

	/// <summary>Copies the current collection membership. The referenced row objects retain their existing ownership.</summary>
	public virtual T[] Snapshot => (T[])Volatile.Read(ref _snapshot).Clone();

	public string DefaultSortProperty { get; set; }

	public SortDirection DefaultSortDirection { get; set; } = SortDirection.Ascending;

	public override long Count => Volatile.Read(ref _snapshot).LongLength;

	public override DataSourceCapabilities Capabilities => DataSourceCapabilities.CanRead | DataSourceCapabilities.CanSearch | DataSourceCapabilities.CanSort | DataSourceCapabilities.CanPage;

	/// <summary>Copies supplied membership before publishing it. Configure default sorting before publishing the source to readers.</summary>
	public virtual void ReplaceSnapshot(T[] snapshot) {
		Guard.ArgumentNotNull(snapshot, nameof(snapshot));
		Volatile.Write(ref _snapshot, (T[])snapshot.Clone());
	}

	public override DataSourceItems<T> ReadRange(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection) {
		DataSourceThreadDiagnostics.Default.AssertBackgroundExecution(this);
		Guard.ArgumentGT(pageLength, 0, nameof(pageLength));
		Guard.ArgumentGTE(page, 0, nameof(page));
		IEnumerable<T> query = Volatile.Read(ref _snapshot);
		if (sortDirection != SortDirection.None && string.IsNullOrEmpty(sortProperty)) {
			sortProperty = DefaultSortProperty;
			sortDirection = DefaultSortDirection;
		}
		if (!string.IsNullOrWhiteSpace(searchTerm))
			query = query.Where(row => _properties.Any(property => (_formatValue(ReadProperty(row, property)) ?? string.Empty).Contains(searchTerm, StringComparison.OrdinalIgnoreCase)));
		if (sortDirection != SortDirection.None && !string.IsNullOrEmpty(sortProperty)) {
			var property = _properties.FirstOrDefault(candidate => string.Equals(candidate.Name, sortProperty, StringComparison.OrdinalIgnoreCase));
			Guard.Argument(property != null, nameof(sortProperty), "Unknown snapshot property.");
			query = sortDirection == SortDirection.Descending
				? query.OrderByDescending(row => ReadProperty(row, property), _comparer)
				: query.OrderBy(row => ReadProperty(row, property), _comparer);
		}
		// Unfiltered natural-order reads copy only the requested page, never the full published membership.
		var rows = query as T[] ?? query.ToArray();
		var actualPage = rows.Length == 0 ? 0 : Math.Min(page, (rows.Length - 1) / pageLength);
		var offset = checked((int)((long)actualPage * pageLength));
		var items = new T[Math.Min(pageLength, rows.Length - offset)];
		Array.Copy(rows, offset, items, 0, items.Length);
		return new DataSourceItems<T> { Items = items, Page = actualPage, TotalCount = rows.Length };
	}

	public override IEnumerable<T> NewRange(int count) => throw new NotSupportedException("Snapshots are read only.");

	public override void CreateRange(IEnumerable<T> entities) => throw new NotSupportedException("Snapshots are read only.");

	public override void RefreshRange(T[] entities) => throw new NotSupportedException("Replace the complete snapshot to refresh its membership.");

	public override void UpdateRange(IEnumerable<T> entities) => throw new NotSupportedException("Snapshots are read only.");

	public override void DeleteRange(IEnumerable<T> entities) => throw new NotSupportedException("Snapshots are read only.");

	public override Result ValidateRange(IEnumerable<(T entity, CrudAction action)> actions) => throw new NotSupportedException("Snapshots are read only.");

	/// <summary>Transforms a copied membership once, publishing it only if no snapshot was replaced while the transform ran.</summary>
	/// <remarks>The transform runs without a lock and is not retried. A false result leaves the newer snapshot intact.</remarks>
	protected bool TryTransformSnapshot(Func<T[], T[]> transform) {
		Guard.ArgumentNotNull(transform, nameof(transform));
		var original = Volatile.Read(ref _snapshot);
		var replacement = transform(Tools.Array.Clone(original));
		Guard.Ensure(replacement != null, "A snapshot transform must return its replacement membership.");
		return ReferenceEquals(Interlocked.CompareExchange(ref _snapshot, Tools.Array.Clone(replacement), original), original);
	}

	private static object ReadProperty(T row, PropertyInfo property) => row is null ? null : property.GetValue(row);

	private sealed class SnapshotValueComparer : IComparer<object> {
		private readonly Func<object, string> _formatValue;

		public SnapshotValueComparer(Func<object, string> formatValue) {
			_formatValue = formatValue;
		}

		public int Compare(object left, object right) {
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
			return StringComparer.OrdinalIgnoreCase.Compare(_formatValue(left), _formatValue(right));
		}
	}
}
