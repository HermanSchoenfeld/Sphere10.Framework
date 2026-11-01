// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Per-user paging and sorting preferences for a logically identified CRUD grid.</summary>
[UserEditable]
public class VisualGridSettings : SettingsObject {
	/// <summary>Optional consumer-owned migration version, saved with the next accepted grid preference change.</summary>
	[Browsable(false)]
	public int ConsumerVersion { get; set; }

	/// <summary>The selected page mode. Null preserves compatibility with settings saved before the dropdown existed.</summary>
	public VisualGridPageMode? PageMode { get; set; }

	/// <summary>Legacy Auto/Paged preference, retained for migration and older consumers.</summary>
	[Browsable(false)]
	public bool AutoPageSize { get; set; }

	public int PageSize { get; set; } = 100;

	/// <summary>The zero-based Paged/Auto page; Single always uses zero. The source clamps it when its record count changes.</summary>
	public int PageNumber { get; set; }

	/// <summary>Only deliberate manual widths, in 96-DPI logical pixels, keyed by column schema identity.</summary>
	public ApplicationGridColumnPreferences[] Columns { get; set; } = Array.Empty<ApplicationGridColumnPreferences>();

	public string SortColumnName { get; set; } = string.Empty;

	public SortDirection SortDirection { get; set; } = SortDirection.Ascending;

	/// <summary>Validates query preferences and stable column identities before accepting edited values.</summary>
	public override Result Validate() {
		var result = base.Validate();
		if (PageMode.HasValue && !Enum.IsDefined(PageMode.Value))
			result.AddError("Select a supported grid page mode.");
		if (PageSize <= 0)
			result.AddError("Grid page size must be positive.");
		if (PageNumber < 0)
			result.AddError("Grid page number cannot be negative.");
		if (!Enum.IsDefined(SortDirection))
			result.AddError("Select a supported grid sort direction.");
		if (Columns == null) {
			result.AddError("The grid column schema cannot be null.");
			return result;
		}

		var keys = new HashSet<string>(StringComparer.Ordinal);
		foreach (var column in Columns) {
			if (column == null || string.IsNullOrWhiteSpace(column.Key) || !keys.Add(column.Key)) {
				result.AddError("Grid columns need distinct nonempty stable keys.");
				continue;
			}
			if (column.Width is { } width && (!double.IsFinite(width) || width <= 0 || width > 100000))
				result.AddError("The manual width for '{0}' must be greater than zero and no more than 100000 logical pixels.", column.Title ?? column.Key);
		}
		return result;
	}
}
