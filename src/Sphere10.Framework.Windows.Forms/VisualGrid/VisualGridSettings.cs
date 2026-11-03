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
using System.Drawing.Design;
using System.Linq;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Per-user paging and sorting preferences for a logically identified CRUD grid.</summary>
[UserEditable]
public class VisualGridSettings : SettingsObject {
	/// <summary>Optional consumer-owned migration version, saved with the next accepted grid preference change.</summary>
	[DefaultValue(0)]
	[Browsable(false)]
	public int ConsumerVersion {
		get; set;
	}

	/// <summary>The selected page mode. Null preserves compatibility with settings saved before the dropdown existed.</summary>
	[DefaultValue(null)]
	public VisualGridPageMode? PageMode {
		get; set;
	}

	/// <summary>Legacy Auto/Paged preference, retained for migration and older consumers.</summary>
	[DefaultValue(false)]
	[Browsable(false)]
	public bool AutoPageSize {
		get; set;
	}

	/// <summary>The positive number of rows requested in manual page mode.</summary>
	[DefaultValue(100)]
	public int PageSize { get; set; } = 100;

	/// <summary>The zero-based Paged/Auto page; Single always uses zero. The source clamps it when its record count changes.</summary>
	[DefaultValue(0)]
	public int PageNumber {
		get; set;
	}

	/// <summary>Displayed column order, widths and stretch choices, keyed by stable column identity; widths use 96-DPI logical pixels.</summary>
	[Description("Column array order determines displayed column order. Widths use 96-DPI logical pixels.")]
	[Editor(typeof(VisualGridColumnsEditor), typeof(UITypeEditor))]
	public ApplicationGridColumnPreferences[] Columns { get; set; } = Array.Empty<ApplicationGridColumnPreferences>();

	/// <summary>The source sort identity, or an empty value when no column is selected.</summary>
	[DefaultValue("")]
	public string SortColumnName { get; set; } = string.Empty;

	/// <summary>The requested source sort direction.</summary>
	[DefaultValue(SortDirection.Ascending)]
	public SortDirection SortDirection { get; set; } = SortDirection.Ascending;

	/// <summary>Restores paging/sort defaults and clears layout edits while retaining the grid's declared column identities.</summary>
	public override void RestoreDefaultValues() {
		base.RestoreDefaultValues();
		if (Columns == null)
			return;
		foreach (var column in Columns) {
			if (column == null)
				continue;
			column.Width = null;
			column.ExpandsToFit = null;
		}
	}

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

	/// <summary>Copies presentation values without transferring their owner's provider or identity.</summary>
	internal VisualGridSettings Copy() {
		return new VisualGridSettings {
			ConsumerVersion = ConsumerVersion,
			PageMode = PageMode,
			AutoPageSize = AutoPageSize,
			PageSize = PageSize,
			PageNumber = PageNumber,
			SortColumnName = SortColumnName,
			SortDirection = SortDirection,
			Columns = CopyColumns(Columns)
		};
	}

	// Compare accepted configuration values so unrelated screen saves preserve temporary grid choices.
	internal bool HasSameConfiguration(VisualGridSettings other) {
		if (other == null || PageMode != other.PageMode || AutoPageSize != other.AutoPageSize
			|| PageSize != other.PageSize || PageNumber != other.PageNumber || SortColumnName != other.SortColumnName || SortDirection != other.SortDirection)
			return false;

		var columns = Columns ?? Array.Empty<ApplicationGridColumnPreferences>();
		var otherColumns = other.Columns ?? Array.Empty<ApplicationGridColumnPreferences>();
		return columns.Length == otherColumns.Length && Enumerable.Range(0, columns.Length).All(index => {
			var column = columns[index];
			var otherColumn = otherColumns[index];
			return ReferenceEquals(column, otherColumn) || column != null && otherColumn != null
				&& column.Key == otherColumn.Key && column.Title == otherColumn.Title && column.Width == otherColumn.Width && column.ExpandsToFit == otherColumn.ExpandsToFit;
		});
	}

	/// <summary>Retains column order while detaching editable widths and stretch choices.</summary>
	internal static ApplicationGridColumnPreferences[] CopyColumns(ApplicationGridColumnPreferences[] columns) {
		return (columns ?? Array.Empty<ApplicationGridColumnPreferences>()).Where(column => column != null)
			.Select(column => new ApplicationGridColumnPreferences {
				Key = column.Key,
				Title = column.Title,
				Width = column.Width,
				ExpandsToFit = column.ExpandsToFit
			}).ToArray();
	}
}
