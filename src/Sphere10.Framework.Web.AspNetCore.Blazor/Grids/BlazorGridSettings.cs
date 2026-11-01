// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>Plain grid query and deliberate width values for a screen-owned or standalone browser profile.</summary>
[UserEditable]
public class BlazorGridSettings : IValidatableObject {
	/// <summary>The grid's existing paging mode, independent of its current viewport capacity.</summary>
	[DisplayName("Page mode")]
	public BlazorGridPageMode PageMode { get; set; }

	/// <summary>The remembered manual page size.</summary>
	[DisplayName("Page size")]
	public int PageSize { get; set; } = 10;

	/// <summary>The zero-based current page, clamped by the grid to its source.</summary>
	[DisplayName("Page number")]
	public int PageNumber { get; set; }

	/// <summary>The stable sort-column identity.</summary>
	[DisplayName("Sort column")]
	public string SortColumnName { get; set; }

	/// <summary>The ordering applied to the selected sort column.</summary>
	[DisplayName("Sort direction")]
	public SortDirection SortDirection { get; set; }

	/// <summary>Legacy standalone width values, used when no declared column schema is supplied.</summary>
	[Browsable(false)]
	public Dictionary<string, double> ColumnWidths { get; set; } = new();

	/// <summary>The declared stable column identities and optional manual widths shown by Options.</summary>
	[DisplayName("Column widths")]
	public ApplicationGridColumnPreferences[] Columns { get; set; } = Array.Empty<ApplicationGridColumnPreferences>();

	/// <summary>Checks query and schema values without constructing a control or reading data.</summary>
	public Result Validate() {
		var result = new Result();
		if (!Enum.IsDefined(PageMode))
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
				result.AddError("The manual width for '{0}' must be greater than zero and no more than 100000 CSS pixels.", column.Title ?? column.Key);
		}
		return result;
	}

	/// <summary>Screen-owned records save their column schema once; legacy standalone JSON keeps its original dictionary.</summary>
	public bool ShouldSerializeColumnWidths() {
		return Columns == null || Columns.Length == 0;
	}

	// Standard object validation also supports using this record directly as an Options page.
	IEnumerable<ValidationResult> IValidatableObject.Validate(ValidationContext validationContext) {
		foreach (var error in Validate().ErrorMessages)
			yield return new ValidationResult(error);
	}
}
