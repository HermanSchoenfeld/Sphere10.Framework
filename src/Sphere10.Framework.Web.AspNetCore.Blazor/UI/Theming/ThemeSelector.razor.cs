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

/// <summary>Selects any supported theme without changing the current route or recreating its components.</summary>
public partial class ThemeSelector {
	[Parameter]
	public string Class { get; set; }

	private void ChangeTheme(ChangeEventArgs args) {
		var theme = args.Value?.ToString() switch {
			"classic-blue" => ThemeMode.ClassicBlue,
			"blue" => ThemeMode.Blue,
			"light" => ThemeMode.Light,
			"dark" => ThemeMode.Dark,
			_ => ThemeService.CurrentTheme
		};
		ThemeService.SetTheme(theme);
	}
}
