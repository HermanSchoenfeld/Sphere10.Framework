// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>Chooses when a grid shows its footer, which holds the item count, the paging mode and the pager.</summary>
public enum BlazorGridFooterMode {

	/// <summary>The footer is always shown.</summary>
	Always,

	/// <summary>The footer is shown only while the results span more than one page.</summary>
	Paged,

	/// <summary>The footer is never shown. Use <see cref="BlazorGridPageMode.Single"/> so every row stays reachable.</summary>
	Hidden
}
