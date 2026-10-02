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
using System.Threading.Tasks;
using Sphere10.Framework.Web.AspNetCore.Blazor;
using Sphere10.Framework.Web.AspNetCore.Blazor.Models;

namespace Sphere10.Framework.Utils.BlazorTester.Layouts;

/// <summary>Compatibility routes and search over the same registered screens used by the application menus.</summary>
public static class DemoNavigation {
	private static readonly Alias[] _aliases = {
		new("/", "workspace", "overview"),
		new("/components/grid", "components", "gallery"),
		new("/components/tables", "components", "tables"),
		new("/components/dialogs", "components", "dialogs"),
		new("/components/wizards", "components", "wizards"),
		new("/modern", "components", "gallery"),
		new("/widget-gallery", "legacy", "gallery"),
		new("/widget-gallery/modals", "legacy", "dialogs"),
		new("/widget-gallery/tables", "legacy", "tables"),
		new("/widget-gallery/wizards", "legacy", "wizards"),
		new("/servers", "legacy", "servers"),
		new("/legacy/dashboard", "legacy", "dashboard")
	};

	public static Alias[] Aliases => Tools.Array.Clone(_aliases);

	public static Alias ResolveAlias(string path) {
		Guard.ArgumentNotNull(path, nameof(path));
		var normalized = "/" + path.Trim('/');
		return _aliases.FirstOrDefault(alias => string.Equals(alias.Route, normalized, StringComparison.OrdinalIgnoreCase));
	}

	public static Task<IEnumerable<SearchResult>> SearchAsync(IBlazorApplicationScreenHost host, string term) {
		Guard.ArgumentNotNull(host, nameof(host));
		var query = term?.Trim();
		IEnumerable<SearchResult> results = string.IsNullOrEmpty(query)
			? Array.Empty<SearchResult>()
			: host.Blocks.SelectMany(block => block.Menus.SelectMany(menu => menu.Items).OfType<BlazorScreenMenuItem>()
				.Where(item => item.Title.Contains(query, StringComparison.OrdinalIgnoreCase) || block.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
				.Select(item => new SearchResult(item.Title, new Uri($"application?block={Uri.EscapeDataString(block.Id)}&screen={Uri.EscapeDataString(item.Id)}", UriKind.Relative))))
				.ToArray();
		return Task.FromResult(results);
	}

	public sealed record Alias(string Route, string BlockId, string ScreenId);
}
