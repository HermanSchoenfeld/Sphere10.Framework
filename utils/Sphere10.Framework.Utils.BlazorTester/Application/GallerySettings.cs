// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Linq;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Web.AspNetCore.Blazor;

namespace Sphere10.Framework.Utils.BlazorTester.Application;

/// <summary>Plain preferences for the two named gallery grids; live records and editing drafts are never stored here.</summary>
[UserEditable]
public class GallerySettings : SettingsObject {
	[DisplayName("Editable grid")]
	public BlazorGridSettings Grid { get; set; } = CreateGrid();

	[DisplayName("Live samples grid")]
	public BlazorGridSettings LiveGrid { get; set; } = CreateLiveGrid();

	/// <summary>Validates both independently named grid roles before Options accepts their values.</summary>
	public override Result Validate() {
		var result = new Result();
		if (Grid == null || LiveGrid == null)
			result.AddError("Both gallery grids require preferences.");
		else {
			result.Merge(Grid.Validate());
			result.Merge(LiveGrid.Validate());
		}
		return result;
	}

	// The same defaults supply first use, Options Reset and column metadata before the screen opens.
	private static BlazorGridSettings CreateGrid() {
		return Create(10, BlazorGridPageMode.Paged,
			(nameof(Modern.UI.Controls.TestClass.Id), "ID"), ("Name", "Name"), ("Color", "Color"),
			("CreationDate", "Created"), ("Age", "Age"), ("IsActive", "Active"), ("Note", "Note"),
			("RelatedItem", "Related record"), ("Summary", "Summary"));
	}

	// Column keys are stable across visible caption changes and independent of column positions.
	private static BlazorGridSettings CreateLiveGrid() {
		return Create(12, BlazorGridPageMode.Auto,
			("Icon", "Icon"), ("Id", "ID"), ("Name", "Name"), ("Group", "Group"),
			("Revision", "Revision"), ("Usage", "Usage (%)"), ("Details", "Details"));
	}

	// Builds detached value metadata only; no grid or data source is activated to declare defaults.
	private static BlazorGridSettings Create(int pageSize, BlazorGridPageMode mode, params (string key, string title)[] columns) {
		return new BlazorGridSettings {
			PageSize = pageSize,
			PageMode = mode,
			SortDirection = SortDirection.None,
			Columns = columns.Select(column => new ApplicationGridColumnPreferences { Key = column.key, Title = column.title }).ToArray()
		};
	}
}
