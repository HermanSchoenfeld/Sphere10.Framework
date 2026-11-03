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
using SharedUI = global::Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Utils.BlazorTester.Showcase;

/// <summary>Composes complete application examples from the same public library APIs as the component gallery.</summary>
public static class ShowcasePlugin {
	public static void Configure(BlazorPluginBuilder plugin) {
		Guard.ArgumentNotNull(plugin, nameof(plugin));
		const string pluginName = "Application patterns";
		plugin
			.WithName(pluginName)
			.AddInitializer(provider => new DemoPluginInitializer(provider.GetRequiredService<SharedUI.IApplication>(), provider.GetRequiredService<WorkspaceStatus>(), pluginName))
			.AddFinalizer(provider => new DemoPluginFinalizer(provider.GetRequiredService<SharedUI.IApplication>(), provider.GetRequiredService<WorkspaceStatus>(), pluginName))
			.AddServices(services => services.AddScoped<CollectionCatalogue>())
			.AddBlock(block => block
				.WithId("patterns")
				.WithName("Application patterns")
				.WithPosition(2)
				.WithIconUrl("img/patterns.svg")
				.WithTooltip("Cards, workflows, native commands and complete theme layouts")
				.AddScreen<CardsScreen>("Cards & collections", activationMode: ScreenActivationMode.SingleInstance)
				.AddScreen<CommandsScreen>("Menus & commands", activationMode: ScreenActivationMode.SingleInstance)
				.AddScreen<GuidedWizardsScreen>("Guided workflows", activationMode: ScreenActivationMode.SingleInstance)
				.AddScreen<ThemeStudioScreen>("Theme studio", activationMode: ScreenActivationMode.SingleInstance)
				.AddScreen<CollectionDetailScreen>("Collection details", activationMode: ScreenActivationMode.MultiInstance)
				.WithDefaultScreen<CardsScreen>()
				.AddMenu("Application patterns", id: "patterns", icon: "fas fa-layer-group", configure: menu => menu
					.AddItem<CardsScreen>("Cards & collections", id: "cards-gallery", icon: "fas fa-th-large")
					.AddItem<CommandsScreen>("Menus & commands", id: "commands", icon: "fas fa-bars")
					.AddItem<GuidedWizardsScreen>("Guided workflows", id: "guided-wizards", icon: "fas fa-magic")
					.AddItem<ThemeStudioScreen>("Theme studio", id: "theme-studio", icon: "fas fa-palette")
				)
				.AddMenu("Collection editors", id: "collection-screens", configure: menu => menu
					.AddItem<CollectionDetailScreen>("Collection details", id: "collection-detail", icon: "fas fa-folder-open", isVisible: false)
				)
			);
	}
}
