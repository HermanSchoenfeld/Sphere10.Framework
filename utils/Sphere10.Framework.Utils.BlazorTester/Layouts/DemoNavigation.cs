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
using Sphere10.Framework.Web.AspNetCore.Blazor.Models;

namespace Sphere10.Framework.Utils.BlazorTester.Layouts;

public static class DemoNavigation {
	private static readonly Group[] _groups = {
		new Group("Overview", new[] {
			new Link("Overview", "/", "fas fa-home")
		}),
		new Group("Components", new[] {
			new Link("CRUD grid", "/components/grid", "fas fa-table"),
			new Link("Tables", "/components/tables", "fas fa-list-alt"),
			new Link("Dialogs", "/components/dialogs", "fas fa-comment-alt"),
			new Link("Wizards", "/components/wizards", "fas fa-magic")
		}),
		new Group("Workspace", new[] {
			new Link("Application workspace", "/application", "fas fa-desktop")
		}),
		new Group("Legacy examples", new[] {
			new Link("Plugin gallery", "/widget-gallery", "fas fa-archive"),
			new Link("Original dialogs", "/widget-gallery/modals", "fas fa-comment"),
			new Link("Original tables", "/widget-gallery/tables", "fas fa-list"),
			new Link("Original wizard", "/widget-gallery/wizards", "fas fa-magic"),
			new Link("Endpoint sample", "/servers", "fas fa-server"),
			new Link("Original dashboard", "/legacy/dashboard", "fas fa-chart-bar")
		})
	};

	public static Group[] Groups => _groups.Select(group => new Group(group.Title, Tools.Array.Clone(group.Links))).ToArray();

	public static Task<IEnumerable<SearchResult>> SearchAsync(string term) {
		var query = term?.Trim();
		IEnumerable<SearchResult> results = string.IsNullOrEmpty(query)
			? Array.Empty<SearchResult>()
			: Groups.SelectMany(group => group.Links.Where(link => link.Title.Contains(query, StringComparison.OrdinalIgnoreCase)
				|| group.Title.Contains(query, StringComparison.OrdinalIgnoreCase)))
				.Select(link => new SearchResult(link.Title, new Uri(link.Href, UriKind.Relative))).ToArray();
		return Task.FromResult(results);
	}

	public sealed record Group(string Title, Link[] Links);

	public sealed record Link(string Title, string Href, string Icon);
}