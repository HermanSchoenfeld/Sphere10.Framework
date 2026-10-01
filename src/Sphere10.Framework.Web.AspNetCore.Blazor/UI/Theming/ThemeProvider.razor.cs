// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Microsoft.AspNetCore.Components;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.UI.Theming;

/// <summary>
/// Applies theme tokens to its descendants without replacing their component instances.
/// Include the library's css/themes.css stylesheet and place this provider inside the interactive render boundary.
/// </summary>
public partial class ThemeProvider {
	[Parameter]
	public RenderFragment ChildContent { get; set; }

	[Parameter]
	public string Class { get; set; }

	[Parameter]
	public string Style { get; set; }
}
