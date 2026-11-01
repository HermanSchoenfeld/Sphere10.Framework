// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Web.AspNetCore.Blazor;
using Sphere10.Framework.Web.AspNetCore.Blazor.Services;
using Sphere10.Framework.Utils.BlazorTester.Application;
using Sphere10.Framework.Utils.BlazorTester.Loader.Services;
using Sphere10.Framework.Utils.BlazorTester.Loader.ViewModels;
using Sphere10.Framework.Utils.BlazorTester.Lifecycle;

namespace Sphere10.Framework.Utils.BlazorTester.Loader;

/// <summary>Configures the running workspace and services used by the original loader routes.</summary>
public static class Sphere10Plugin {
	public static void Configure(BlazorPluginBuilder plugin) {
		Guard.ArgumentNotNull(plugin, nameof(plugin));
		const string pluginName = "Sphere10.Framework";
		plugin
			.WithName(pluginName)
			.AddInitializer(provider => new DemoPluginInitializer(provider.GetRequiredService<IApplication>(), provider.GetRequiredService<WorkspaceStatus>(), pluginName))
			.AddFinalizer(provider => new DemoPluginFinalizer(provider.GetRequiredService<IApplication>(), provider.GetRequiredService<WorkspaceStatus>(), pluginName))
			.AddServices(services => {
				services.AddScoped<WorkspaceStatus>();
				services.AddScoped<IEndpointManager, DefaultEndpointManager>();
				services.AddScoped<INodeService, MockNodeService>();
				services.AddTransient<HomeViewModel>();
				services.AddTransient<ServersViewModel>();
				services.AddTransient<SidebarBrandViewModel>();
			})
			.AddBlock(block => block
				.WithId("workspace")
				.WithName("Workspace")
				.WithIconUrl("img/heading-solid.svg")
				.WithTooltip("Workspace")
				.AddScreen<OverviewScreen>(screen => screen
					.WithTitle("Overview")
					.AsSingleInstance()
				)
				.AddScreen<EditorScreen>(screen => screen
					.WithTitle("Single-instance screen")
					.AsSingleInstance()
				)
				.AddScreen<ScratchpadScreen>(screen => screen
					.WithTitle("New screen instance")
					.AsMultiInstance()
				)
				.AddScreen<RegistrationExampleScreen>(screen => screen
					.WithTitle("CPU inspector")
					.WithStableKey("cpu-inspector")
					.AsSingleInstance()
					.UseSettings<RegistrationInspectorSettings>(() => new RegistrationInspectorSettings { Caption = "CPU summary" })
					.WithActivator(context => new BlazorApplicationScreenActivation(new Dictionary<string, object> { [nameof(RegistrationExampleScreen.Name)] = "CPU" })),
					out var cpuInspector
				)
				.AddScreen<RegistrationExampleScreen>(screen => screen
					.WithTitle("GPU inspector")
					.WithStableKey("gpu-inspector")
					.AsSingleInstance()
					.UseSettings<RegistrationInspectorSettings, RegistrationInspectorSettingsEditor>(() => new RegistrationInspectorSettings { Caption = "GPU summary" })
					.WithActivator(context => new BlazorApplicationScreenActivation(new Dictionary<string, object> { [nameof(RegistrationExampleScreen.Name)] = "GPU" })),
					out var gpuInspector
				)
				.AddScreen<RegistrationDetailsScreen>(screen => screen
					.WithTitle("Menu-free details")
					.AsSingleInstance()
				)
				.WithDefaultScreen<OverviewScreen>()
				.AddToolBarSeparator()
				.AddToolBarItem(item => item
					.WithId("block-action")
					.WithText("Workspace action")
					.WithIcon("fas fa-desktop")
					.WithAction((provider, token) => {
						token.ThrowIfCancellationRequested();
						provider.GetRequiredService<WorkspaceStatus>().Increment();
						return Task.CompletedTask;
					})
				)
				.AddMenu(menu => menu
					.WithId("work")
					.WithText("Screen hosting")
					.WithIcon("fas fa-desktop")
					.AddShowScreenItem(cpuInspector, id: "cpu-inspector", icon: "fas fa-microchip")
					.AddShowScreenItem(gpuInspector, id: "gpu-inspector", icon: "fas fa-desktop")
					.AddItem(item => item
						.WithId("overview")
						.WithText("Overview")
						.WithIcon("fas fa-home")
						.WithScreen<OverviewScreen>()
					)
					.AddItem(item => item
						.WithId("editor")
						.WithText("Single-instance screen")
						.WithIcon("fas fa-edit")
						.WithScreen<EditorScreen>()
					)
					.AddItem(item => item
						.WithId("scratchpad")
						.WithText("New screen instance")
						.WithIcon("fas fa-sticky-note")
						.WithScreen<ScratchpadScreen>()
					)
					.AddItem(item => item
						.WithId("action")
						.WithText("Run scoped async action")
						.WithIcon("fas fa-play")
						.WithAction(async (provider, cancellationToken) => {
							await Task.Delay(250, cancellationToken);
							provider.GetRequiredService<WorkspaceStatus>().Increment();
						})
					)
				)
			);
	}
}
