// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Utils.BlazorTester.Application;
using Sphere10.Framework.Utils.BlazorTester.Lifecycle;
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
		const string pluginName = "Application patterns";
		plugin
			.WithName(pluginName)
			.AddInitializer(provider => new DemoPluginInitializer(provider.GetRequiredService<IApplication>(), provider.GetRequiredService<WorkspaceStatus>(), pluginName))
			.AddFinalizer(provider => new DemoPluginFinalizer(provider.GetRequiredService<IApplication>(), provider.GetRequiredService<WorkspaceStatus>(), pluginName))
			.AddServices(services => services.AddScoped<CollectionCatalogue>())
			.AddBlock(block => block
				.WithId("patterns")
				.WithName("Application patterns")
				.WithPosition(2)
				.WithIconUrl("img/patterns.svg")
				.WithTooltip("Cards, workflows, native commands and complete theme layouts")
				.AddScreen<CardsScreen>(screen => screen
					.WithTitle("Cards & collections")
					.AsSingleInstance()
				)
				.AddScreen<CommandsScreen>(screen => screen
					.WithTitle("Menus & commands")
					.AsSingleInstance()
				)
				.AddScreen<GuidedWizardsScreen>(screen => screen
					.WithTitle("Guided workflows")
					.AsSingleInstance()
				)
				.AddScreen<ThemeStudioScreen>(screen => screen
					.WithTitle("Theme studio")
					.AsSingleInstance()
				)
				.AddScreen<CollectionDetailScreen>(screen => screen
					.WithTitle("Collection details")
					.AsMultiInstance()
				)
				.WithDefaultScreen<CardsScreen>()
				.AddMenu(menu => menu
					.WithId("patterns")
					.WithText("Application patterns")
					.WithIcon("fas fa-layer-group")
					.AddItem(item => item
						.WithId("cards-gallery")
						.WithText("Cards & collections")
						.WithIcon("fas fa-th-large")
						.WithScreen<CardsScreen>()
					)
					.AddItem(item => item
						.WithId("commands")
						.WithText("Menus & commands")
						.WithIcon("fas fa-bars")
						.WithScreen<CommandsScreen>()
					)
					.AddItem(item => item
						.WithId("guided-wizards")
						.WithText("Guided workflows")
						.WithIcon("fas fa-magic")
						.WithScreen<GuidedWizardsScreen>()
					)
					.AddItem(item => item
						.WithId("theme-studio")
						.WithText("Theme studio")
						.WithIcon("fas fa-palette")
						.WithScreen<ThemeStudioScreen>()
					)
				)
				.AddMenu(menu => menu
					.WithId("collection-screens")
					.WithText("Collection editors")
					.AddItem(item => item
						.WithId("collection-detail")
						.WithText("Collection details")
						.WithIcon("fas fa-folder-open")
						.WithVisibility(false)
						.WithScreen<CollectionDetailScreen>()
					)
				)
			);
	}
}
