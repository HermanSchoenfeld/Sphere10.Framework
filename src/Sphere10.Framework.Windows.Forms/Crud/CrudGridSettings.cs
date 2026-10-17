// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework.Application;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Per-user paging and sorting preferences for a logically identified CRUD grid.</summary>
public class CrudGridSettings : SettingsObject {
	/// <summary>Optional consumer-owned migration version, saved with the next accepted grid preference change.</summary>
	public int ConsumerVersion { get; set; }

	/// <summary>The selected page mode. Null preserves compatibility with settings saved before the dropdown existed.</summary>
	public CrudGridPageMode? PageMode { get; set; }

	/// <summary>Legacy Auto/Paged preference, retained for migration and older consumers.</summary>
	public bool AutoPageSize { get; set; }

	public int PageSize { get; set; } = 100;

	/// <summary>The zero-based Paged/Auto page; Single always uses zero. The source clamps it when its record count changes.</summary>
	public int PageNumber { get; set; }

	public string SortColumnName { get; set; } = string.Empty;

	public SortDirection SortDirection { get; set; } = SortDirection.Ascending;
}
