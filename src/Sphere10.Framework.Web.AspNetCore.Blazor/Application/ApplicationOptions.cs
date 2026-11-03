// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Web.AspNetCore.Blazor;

/// <summary>Immutable host branding that can be resolved without constructing a circuit or its screen host.</summary>
public sealed record BlazorApplicationOptions {
	public const string DefaultTitle = "Sphere10 application";

	public BlazorApplicationOptions(string title = DefaultTitle, string faviconUrl = null, string faviconContentType = null) {
		Guard.Argument(!string.IsNullOrWhiteSpace(title), nameof(title), "An application title is required.");
		Guard.Argument(faviconUrl == null || !string.IsNullOrWhiteSpace(faviconUrl), nameof(faviconUrl), "A favicon URL cannot be empty.");
		Guard.Argument(faviconContentType == null || !string.IsNullOrWhiteSpace(faviconContentType), nameof(faviconContentType), "A favicon content type cannot be empty.");
		Title = title;
		FaviconUrl = faviconUrl;
		FaviconContentType = faviconContentType;
	}

	public string Title { get; }

	public string FaviconUrl { get; }

	public string FaviconContentType { get; }
}
