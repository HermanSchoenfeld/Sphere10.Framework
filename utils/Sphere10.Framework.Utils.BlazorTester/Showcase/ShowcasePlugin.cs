// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Utils.BlazorTester.Showcase.Cards;
using Sphere10.Framework.Utils.BlazorTester.Showcase.Commands;
using Sphere10.Framework.Utils.BlazorTester.Showcase.Themes;
using Sphere10.Framework.Utils.BlazorTester.Showcase.Wizards;
using Sphere10.Framework.Web.AspNetCore.Blazor;

namespace Sphere10.Framework.Utils.BlazorTester.Showcase;

/// <summary>Composes complete application examples from the same public library APIs as the component gallery.</summary>
public static class ShowcasePlugin {
	public static void Configure(BlazorPluginBuilder plugin) {
		Guard.ArgumentNotNull(plugin, nameof(plugin));
		plugin
			.WithName("Application patterns")
			.ConfigureServices(services => services.AddScoped<CollectionCatalogue>())
			.ConfigureBlock(block => block
				.WithId("patterns")
				.WithName("Application patterns")
				.WithPosition(2)
				.WithIconUrl("img/patterns.svg")
				.WithTooltip("Cards, workflows, native commands and complete theme layouts")
				.WithDefaultScreen<CardsScreen>()
				.ConfigureMenu(menu => menu
					.WithId("patterns")
					.WithText("Application patterns")
					.WithIcon("fas fa-layer-group")
					.ConfigureItem(item => item
						.WithId("cards-gallery")
						.WithText("Cards & collections")
						.WithIcon("fas fa-th-large")
						.WithScreen<CardsScreen>()
						.AsSingleInstance()
					)
					.ConfigureItem(item => item
						.WithId("commands")
						.WithText("Menus & commands")
						.WithIcon("fas fa-bars")
						.WithScreen<CommandsScreen>()
						.AsSingleInstance()
					)
					.ConfigureItem(item => item
						.WithId("guided-wizards")
						.WithText("Guided workflows")
						.WithIcon("fas fa-magic")
						.WithScreen<GuidedWizardsScreen>()
						.AsSingleInstance()
					)
					.ConfigureItem(item => item
						.WithId("theme-studio")
						.WithText("Theme studio")
						.WithIcon("fas fa-palette")
						.WithScreen<ThemeStudioScreen>()
						.AsSingleInstance()
					)
				)
				.ConfigureMenu(menu => menu
					.WithId("collection-screens")
					.WithText("Collection editors")
					.ConfigureItem(item => item
						.WithId("collection-detail")
						.WithText("Collection details")
						.WithIcon("fas fa-folder-open")
						.WithVisibility(false)
						.WithScreen<CollectionDetailScreen>()
						.AsMultiInstance()
					)
				)
			);
	}
}
