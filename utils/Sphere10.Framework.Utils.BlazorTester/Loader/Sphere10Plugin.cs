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
using SharedUI = global::Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Utils.BlazorTester.Loader;

/// <summary>Configures the running workspace and services used by the original loader routes.</summary>
public static class Sphere10Plugin {
	/// <summary>Composes the tester plugin, screens and demonstration commands.</summary>
	public static void Configure(BlazorPluginBuilder plugin) {
		Guard.ArgumentNotNull(plugin, nameof(plugin));
		const string pluginName = "Sphere10.Framework";
		plugin
			.WithName(pluginName)
			.AddInitializer(provider => new DemoPluginInitializer(provider.GetRequiredService<SharedUI.IApplication>(), provider.GetRequiredService<WorkspaceStatus>(), pluginName))
			.AddFinalizer(provider => new DemoPluginFinalizer(provider.GetRequiredService<SharedUI.IApplication>(), provider.GetRequiredService<WorkspaceStatus>(), pluginName))
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
				.AddScreen<OverviewScreen>("Overview", activationMode: ScreenActivationMode.SingleInstance)
				.AddScreen<EditorScreen>("Single-instance screen", activationMode: ScreenActivationMode.SingleInstance)
				.AddScreen<ScratchpadScreen>("New screen instance", activationMode: ScreenActivationMode.MultiInstance)
				.AddScreen<RegistrationExampleScreen>(screen => screen
					.WithTitle("CPU inspector")
					.WithStableKey("cpu-inspector")
					.AsSingleInstance()
					.UseSettings(new RegistrationExampleScreen.RegistrationInspectorSettings { Caption = "CPU summary" })
					.WithActivator(context => new ApplicationScreenActivation(new Dictionary<string, object> { [nameof(RegistrationExampleScreen.Name)] = "CPU" })),
					out var cpuInspector
				)
				.AddScreen<RegistrationExampleScreen>(screen => screen
					.WithTitle("GPU inspector")
					.WithStableKey("gpu-inspector")
					.AsSingleInstance()
					.UseSettings(new RegistrationExampleScreen.RegistrationInspectorSettings { Caption = "GPU summary" })
					.WithSettingsEditor<RegistrationInspectorSettingsEditor>()
					.WithActivator(context => new ApplicationScreenActivation(new Dictionary<string, object> { [nameof(RegistrationExampleScreen.Name)] = "GPU" })),
					out var gpuInspector
				)
				.AddScreen<RegistrationDetailsScreen>("Menu-free details", activationMode: ScreenActivationMode.SingleInstance)
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
				.AddMenu("Screen hosting", id: "work", icon: "fas fa-desktop", configure: menu => menu
					.AddShowScreenItem(cpuInspector, id: "cpu-inspector", icon: "fas fa-microchip")
					.AddShowScreenItem(gpuInspector, id: "gpu-inspector", icon: "fas fa-desktop")
					.AddItem<OverviewScreen>("Overview", id: "overview", icon: "fas fa-home")
					.AddItem<EditorScreen>("Single-instance screen", id: "editor", icon: "fas fa-edit")
					.AddItem<ScratchpadScreen>("New screen instance", id: "scratchpad", icon: "fas fa-sticky-note")
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
