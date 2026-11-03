// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Web.AspNetCore.Blazor;
using Sphere10.Framework.Utils.BlazorTester.Application;
using Sphere10.Framework.Utils.BlazorTester.Application.Legacy;
using Sphere10.Framework.Utils.BlazorTester.Lifecycle;
using Sphere10.Framework.Utils.BlazorTester.WidgetGallery.Widgets.Models;
using Sphere10.Framework.Utils.BlazorTester.WidgetGallery.Widgets.Services;
using Sphere10.Framework.Utils.BlazorTester.WidgetGallery.Widgets.Validators;
using Sphere10.Framework.Utils.BlazorTester.WidgetGallery.Widgets.ViewModels;
using SharedUI = global::Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Utils.BlazorTester.WidgetGallery;

/// <summary>Configures the component workspace and services used by the original widget-gallery routes.</summary>
public static class WidgetGalleryPlugin {
	public static void Configure(BlazorPluginBuilder plugin) {
		Guard.ArgumentNotNull(plugin, nameof(plugin));
		const string pluginName = "Widget Gallery";
		plugin
			.WithName(pluginName)
			.AddInitializer(provider => new DemoPluginInitializer(provider.GetRequiredService<SharedUI.IApplication>(), provider.GetRequiredService<WorkspaceStatus>(), pluginName))
			.AddFinalizer(provider => new DemoPluginFinalizer(provider.GetRequiredService<SharedUI.IApplication>(), provider.GetRequiredService<WorkspaceStatus>(), pluginName))
			.AddServices(services => {
				services.AddTransient<IRandomNumberService, RandomNumberService>();
				services.AddTransient<IValidator<NewWidgetModel>, NewWidgetModelValidator>();
				services.AddTransient<WidgetGalleryViewModel>();
				services.AddTransient<TablesViewModel>();
				services.AddTransient<WizardsViewModel>();
				services.AddTransient<NewWidgetWizardStepViewModel>();
				services.AddTransient<WidgetDimensionsStepViewModel>();
				services.AddTransient<NewWidgetSummaryViewModel>();
				services.AddTransient<WidgetModalViewModel>();
			})
			.AddBlock(block => block
				.WithId("components")
				.WithName("Component gallery")
				.WithIconUrl("img/boxes-solid.svg")
				.WithTooltip("Component gallery")
				.WithPosition(1)
				.AddScreen<ComponentScreen>(screen => screen
					.WithTitle("Visual grid")
				)
				.AddScreen<TablesScreen>("Tables")
				.AddScreen<DialogsScreen>("Dialogs")
				.AddScreen<WizardsScreen>("Wizards")
				.AddScreen<ThemeScreen>("Themes")
				.WithDefaultScreen<ComponentScreen>()
				.AddMenu("Components", id: "gallery", icon: "fas fa-th-large", configure: menu => menu
					.AddItem<ComponentScreen>("Visual grid", id: "gallery", icon: "fas fa-table")
					.AddItem<TablesScreen>("Tables", id: "tables", icon: "fas fa-list-alt")
					.AddItem<DialogsScreen>("Dialogs", id: "dialogs", icon: "fas fa-comment-alt")
					.AddItem<WizardsScreen>("Wizards", id: "wizards", icon: "fas fa-magic")
					.AddItem<ThemeScreen>("Themes", id: "themes", icon: "fas fa-palette")
				)
			)
			.AddBlock(block => block
				.WithId("legacy")
				.WithName("Legacy examples")
				.WithIconUrl("img/logo.svg")
				.WithTooltip("Original gallery and loader examples")
				.WithPosition(3)
				.AddScreen<LegacyGalleryScreen>("Plugin gallery")
				.AddScreen<LegacyDialogsScreen>("Legacy dialogs")
				.AddScreen<LegacyTablesScreen>("Legacy tables")
				.AddScreen<LegacyWizardsScreen>("Legacy wizard")
				.AddScreen<EndpointScreen>("Endpoint sample")
				.AddScreen<LegacyDashboardScreen>("Legacy dashboard")
				.WithDefaultScreen<LegacyGalleryScreen>()
				.AddMenu("Examples", id: "examples", icon: "fas fa-history", configure: menu => menu
					.AddItem<LegacyGalleryScreen>("Plugin gallery", id: "gallery", icon: "fas fa-th-large")
					.AddItem<LegacyDialogsScreen>("Legacy dialogs", id: "dialogs", icon: "fas fa-comment")
					.AddItem<LegacyTablesScreen>("Legacy tables", id: "tables", icon: "fas fa-list")
					.AddItem<LegacyWizardsScreen>("Legacy wizard", id: "wizards", icon: "fas fa-magic")
				)
				.AddMenu("Loader", id: "loader", icon: "fas fa-server", configure: menu => menu
					.AddItem<EndpointScreen>("Endpoint sample", id: "servers", icon: "fas fa-server")
					.AddItem<LegacyDashboardScreen>("Legacy dashboard", id: "dashboard", icon: "fas fa-tachometer-alt")
				)
			);
	}
}
