// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>Browser-local preferences for a grid with an explicit stable settings key.</summary>
public class BlazorGridSettings {
	public BlazorGridPageMode PageMode { get; set; }

	public int PageSize { get; set; } = 10;

	public int PageNumber { get; set; }

	public string SortColumnName { get; set; }

	public SortDirection SortDirection { get; set; }

	public Dictionary<string, double> ColumnWidths { get; set; } = new();
}
