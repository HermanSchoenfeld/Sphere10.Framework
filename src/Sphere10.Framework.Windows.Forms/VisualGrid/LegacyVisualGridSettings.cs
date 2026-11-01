// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms;

// Keep the original serialized names private so existing JSON/XML provider records survive the public grid rename.
internal sealed class CrudGridSettings : SettingsObject {
	public CrudGridSettings() {
	}

	public int ConsumerVersion { get; set; }

	public CrudGridPageMode? PageMode { get; set; }

	public bool AutoPageSize { get; set; }

	public int PageSize { get; set; } = 100;

	public int PageNumber { get; set; }

	public string SortColumnName { get; set; } = string.Empty;

	public SortDirection SortDirection { get; set; } = SortDirection.Ascending;

	public ApplicationGridColumnPreferences[] Columns { get; set; } = Array.Empty<ApplicationGridColumnPreferences>();
}

// XML records include the enum's original CLR name as well as the record's name.
internal enum CrudGridPageMode {
	Paged,
	Auto,
	Single
}
