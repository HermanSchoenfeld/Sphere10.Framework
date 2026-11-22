// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>Immutable host branding and presentation defaults that can be resolved without constructing a circuit or its screen host.</summary>
public sealed record ApplicationOptions {
	public const string DefaultTitle = "Sphere10 application";

	public ApplicationOptions(string title = DefaultTitle, string faviconUrl = null, string faviconContentType = null, BlazorGridDensity gridDensity = BlazorGridDensity.Normal,
		bool toolBarText = false) {
		Guard.Argument(!string.IsNullOrWhiteSpace(title), nameof(title), "An application title is required.");
		Guard.Argument(faviconUrl == null || !string.IsNullOrWhiteSpace(faviconUrl), nameof(faviconUrl), "A favicon URL cannot be empty.");
		Guard.Argument(faviconContentType == null || !string.IsNullOrWhiteSpace(faviconContentType), nameof(faviconContentType), "A favicon content type cannot be empty.");
		Guard.Argument(System.Enum.IsDefined(gridDensity), nameof(gridDensity), "Select a supported grid density.");
		Title = title;
		FaviconUrl = faviconUrl;
		FaviconContentType = faviconContentType;
		GridDensity = gridDensity;
		ToolBarText = toolBarText;
	}

	public string Title { get; }

	public string FaviconUrl { get; }

	public string FaviconContentType { get; }

	/// <summary>The density of grids that do not set their own <c>Density</c>.</summary>
	public BlazorGridDensity GridDensity { get; }

	/// <summary>
	/// Shows every icon-bearing toolbar command's caption beside its icon, as a toolbar with text buttons does; otherwise a command
	/// shows its caption only when it sets <see cref="IApplicationMenuItem.ShowToolBarText"/>.
	/// </summary>
	public bool ToolBarText { get; }
}
