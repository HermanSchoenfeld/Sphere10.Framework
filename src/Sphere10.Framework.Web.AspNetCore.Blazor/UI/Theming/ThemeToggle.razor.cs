// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Microsoft.AspNetCore.Components;
using Sphere10.Framework.Web.AspNetCore.Blazor.Theming;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.UI.Theming;

/// <summary>A toggle button whose pressed state indicates that the dark theme is selected.</summary>
public partial class ThemeToggle {
	[Parameter]
	public string Class { get; set; }

	private string Pressed => ThemeService.CurrentTheme == ThemeMode.Dark ? "true" : "false";

	private void ToggleTheme() => ThemeService.Toggle();
}
