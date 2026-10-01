// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Theming;

public class ThemeService : ThemeServiceBase {
	private ThemeMode _currentTheme;

	public ThemeService()
		: this(ThemeMode.Light) {
	}

	public ThemeService(ThemeMode initialTheme) {
		Guard.Argument(
			initialTheme is ThemeMode.Light or ThemeMode.Dark or ThemeMode.Blue or ThemeMode.ClassicBlue,
			nameof(initialTheme),
			"The theme must be Light, Dark, Blue or ClassicBlue."
		);
		_currentTheme = initialTheme;
	}

	public override ThemeMode CurrentTheme => _currentTheme;

	public override void SetTheme(ThemeMode theme) {
		Guard.Argument(theme is ThemeMode.Light or ThemeMode.Dark or ThemeMode.Blue or ThemeMode.ClassicBlue, nameof(theme), "The theme must be Light, Dark, Blue or ClassicBlue.");
		if (_currentTheme == theme)
			return;
		_currentTheme = theme;
		OnChanged();
	}
}
