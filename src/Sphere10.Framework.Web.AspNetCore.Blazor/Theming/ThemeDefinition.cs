// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Theming;

/// <summary>Immutable presentation metadata for a built-in theme, independent of application structure.</summary>
public sealed class ThemeDefinition {
	internal ThemeDefinition(ThemeMode mode, string id, string name, string stylesheet, bool isDark = false) {
		Mode = mode;
		Id = id;
		Name = name;
		StylesheetPath = $"_content/Sphere10.Framework.Web.AspNetCore.Blazor/css/themes/{stylesheet}.css";
		IsDark = isDark;
	}

	public ThemeMode Mode { get; }

	public string Id { get; }

	public string Name { get; }

	public string StylesheetPath { get; }

	public bool IsDark { get; }
}
