// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Web.AspNetCore.Blazor;
using Sphere10.Framework.Utils.BlazorTester.Application;
using Sphere10.Framework.Utils.BlazorTester.Application.Legacy;
using Sphere10.Framework.Utils.BlazorTester.WidgetGallery.Widgets.Models;
using Sphere10.Framework.Utils.BlazorTester.WidgetGallery.Widgets.Services;
using Sphere10.Framework.Utils.BlazorTester.WidgetGallery.Widgets.Validators;
using Sphere10.Framework.Utils.BlazorTester.WidgetGallery.Widgets.ViewModels;

namespace Sphere10.Framework.Utils.BlazorTester.WidgetGallery;

/// <summary>Configures the component workspace and services used by the original widget-gallery routes.</summary>
public static class WidgetGalleryPlugin {
	public static void Configure(BlazorPluginBuilder plugin) {
		Guard.ArgumentNotNull(plugin, nameof(plugin));
		plugin.WithName("Widget Gallery")
			.ConfigureServices(services => {
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
				.WithDefaultScreen<ComponentScreen>()
				.AddMenu(menu => menu.WithId("gallery").WithText("Components").WithIcon("fas fa-th-large")
					.ConfigureItem(item => item.WithId("gallery").WithText("CRUD grid").WithIcon("fas fa-table").WithScreen<ComponentScreen>())
					.ConfigureItem(item => item.WithId("tables").WithText("Tables").WithIcon("fas fa-list-alt").WithScreen<TablesScreen>())
					.ConfigureItem(item => item.WithId("dialogs").WithText("Dialogs").WithIcon("fas fa-comment-alt").WithScreen<DialogsScreen>())
					.ConfigureItem(item => item.WithId("wizards").WithText("Wizards").WithIcon("fas fa-magic").WithScreen<WizardsScreen>())))
			.AddBlock(block => block
				.WithId("legacy")
				.WithName("Legacy examples")
				.WithIconUrl("img/logo.svg")
				.WithTooltip("Original gallery and loader examples")
				.WithPosition(2)
				.WithDefaultScreen<LegacyGalleryScreen>()
				.AddMenu(menu => menu.WithId("examples").WithText("Examples").WithIcon("fas fa-history")
					.ConfigureItem(item => item.WithId("gallery").WithText("Plugin gallery").WithIcon("fas fa-th-large").WithScreen<LegacyGalleryScreen>())
					.ConfigureItem(item => item.WithId("dialogs").WithText("Legacy dialogs").WithIcon("fas fa-comment").WithScreen<LegacyDialogsScreen>())
					.ConfigureItem(item => item.WithId("tables").WithText("Legacy tables").WithIcon("fas fa-list").WithScreen<LegacyTablesScreen>())
					.ConfigureItem(item => item.WithId("wizards").WithText("Legacy wizard").WithIcon("fas fa-magic").WithScreen<LegacyWizardsScreen>()))
				.AddMenu(menu => menu.WithId("loader").WithText("Loader").WithIcon("fas fa-server")
					.ConfigureItem(item => item.WithId("servers").WithText("Endpoint sample").WithIcon("fas fa-server").WithScreen<EndpointScreen>())
					.ConfigureItem(item => item.WithId("dashboard").WithText("Legacy dashboard").WithIcon("fas fa-tachometer-alt").WithScreen<LegacyDashboardScreen>())));
	}
}
