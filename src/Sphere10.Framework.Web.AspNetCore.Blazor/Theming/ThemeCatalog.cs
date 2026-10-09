// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Theming;

/// <summary>Shared definitions used by theme selection, stylesheet loading and Bootstrap color-scheme integration.</summary>
public static class ThemeCatalog {
	private static readonly ThemeDefinition[] _themes = {
		new(ThemeMode.ClassicBlue, "classic-blue", "Classic blue", "classic-blue"),
		new(ThemeMode.Blue, "blue", "Blue", "blue"),
		new(ThemeMode.Light, "light", "Light", "light"),
		new(ThemeMode.Dark, "dark", "Dark", "dark", true),
		new(ThemeMode.ArtificialReason, "artificial-reason", "Artificial Reason", "artificial-reason"),
		new(ThemeMode.ArtificialReasonGreen, "artificial-reason-green", "Artificial Reason Green", "artificial-reason"),
		new(ThemeMode.ArtificialReasonOrange, "artificial-reason-orange", "Artificial Reason Orange", "artificial-reason"),
		new(ThemeMode.ArtificialReasonViolet, "artificial-reason-violet", "Artificial Reason Violet", "artificial-reason"),
		new(ThemeMode.Boost, "boost", "Boost", "boost"),
		new(ThemeMode.Hud, "hud", "HUD", "hud", true),
		new(ThemeMode.HudLight, "hud-light", "HUD Light", "hud"),
		new(ThemeMode.Landkit, "landkit", "Landkit", "landkit"),
		new(ThemeMode.MacAdmin, "macadmin", "MacAdmin", "macadmin"),
		new(ThemeMode.MacBeath, "macbeath", "MacBeath", "macbeath"),
		new(ThemeMode.MacKart, "mackart", "MacKart", "mackart"),
		new(ThemeMode.Raleway, "raleway", "Raleway", "raleway"),
		new(ThemeMode.RalewayPurple, "raleway-purple", "Raleway Purple", "raleway"),
		new(ThemeMode.RalewayGreen, "raleway-green", "Raleway Green", "raleway"),
		new(ThemeMode.AppStack, "appstack", "AppStack", "appstack"),
		new(ThemeMode.AppStackDark, "appstack-dark", "AppStack Dark", "appstack", true)
	};
	private static readonly Dictionary<ThemeMode, ThemeDefinition> _byMode = _themes.ToDictionary(theme => theme.Mode);
	private static readonly Dictionary<string, ThemeDefinition> _byId = _themes.ToDictionary(theme => theme.Id, StringComparer.OrdinalIgnoreCase);

	public static ThemeDefinition[] All => (ThemeDefinition[])_themes.Clone();

	public static ThemeDefinition Get(ThemeMode mode) {
		Guard.Argument(_byMode.TryGetValue(mode, out var definition), nameof(mode), "Choose a theme from ThemeCatalog.All.");
		return definition;
	}

	public static bool TryGet(string id, out ThemeDefinition definition) {
		definition = null;
		return id != null && _byId.TryGetValue(id, out definition);
	}
}
