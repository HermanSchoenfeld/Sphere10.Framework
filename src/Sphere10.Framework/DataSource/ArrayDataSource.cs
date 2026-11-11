// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Threading;

namespace Sphere10.Framework;

/// <summary>Stores its data as a read-only array which an owner replaces wholesale; reads search, sort and page that array.</summary>
public class ArrayDataSource<TItem> : SyncBatchDataSourceBase<TItem> {
	private TItem[] _data;

	public ArrayDataSource()
		: this(Array.Empty<TItem>()) {
	}

	public ArrayDataSource(TItem[] data) {
		Guard.ArgumentNotNull(data, nameof(data));
		_data = Tools.Array.Clone(data);
	}

	public string DefaultSortProperty { get; set; }

	public SortDirection DefaultSortDirection { get; set; } = SortDirection.Ascending;

	/// <summary>Copies the current data. Row objects retain their existing ownership unless <see cref="CopyItemForRead"/> says otherwise.</summary>
	public virtual TItem[] Data {
		get {
			var rows = Tools.Array.Clone(Volatile.Read(ref _data));
			for (var index = 0; index < rows.Length; index++)
				rows[index] = CopyItemForRead(rows[index]);
			return rows;
		}
	}

	public override long Count => Volatile.Read(ref _data).LongLength;

	public override DataSourceCapabilities Capabilities => DataSourceCapabilities.ReadOnly | DataSourceCapabilities.CanNotify;

	/// <summary>Replaces the data and notifies consumers.</summary>
	public virtual void ReplaceData(TItem[] data) {
		PublishData(data);
		NotifyChanged();
	}

	public override DataSourceItems<TItem> ReadRange(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection) {
		Debug.AssertNotMainThread();
		var result = Tools.DataSources.Query(Volatile.Read(ref _data), searchTerm, pageLength, page, sortProperty, sortDirection, FormatValue, DefaultSortProperty, DefaultSortDirection);
		return new DataSourceItems<TItem> { Items = result.Items.Select(CopyItemForRead).ToArray(), Page = result.Page, TotalCount = result.TotalCount };
	}

	public override IEnumerable<TItem> NewRange(int count) => throw new NotSupportedException("The array is read only.");

	public override void CreateRange(IEnumerable<TItem> entities) => throw new NotSupportedException("The array is read only.");

	public override void RefreshRange(TItem[] entities) => throw new NotSupportedException("Replace the complete data to refresh it.");

	public override void UpdateRange(IEnumerable<TItem> entities) => throw new NotSupportedException("The array is read only.");

	public override void DeleteRange(IEnumerable<TItem> entities) => throw new NotSupportedException("The array is read only.");

	public override Result ValidateRange(IEnumerable<(TItem entity, CrudAction action)> actions) => throw new NotSupportedException("The array is read only.");

	/// <summary>Replaces the data without notifying, for an owner which notifies as part of its own operation.</summary>
	protected void PublishData(TItem[] data) {
		Guard.ArgumentNotNull(data, nameof(data));
		Volatile.Write(ref _data, Tools.Array.Clone(data));
	}

	/// <summary>Formats a property value for searching and sorting.</summary>
	protected virtual string FormatValue(object value) => Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;

	/// <summary>Lets a source with owned mutable aggregate rows preserve its defensive-read contract. Ordinary row references are returned as they are.</summary>
	protected virtual TItem CopyItemForRead(TItem item) => item;
}
