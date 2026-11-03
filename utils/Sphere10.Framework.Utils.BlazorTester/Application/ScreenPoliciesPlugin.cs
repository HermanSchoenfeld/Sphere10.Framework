// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Utils.BlazorTester.Lifecycle;
using Sphere10.Framework.Web.AspNetCore.Blazor;
using SharedUI = global::Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Utils.BlazorTester.Application;

/// <summary>Uses the ordinary plugin and block builders for the empty and permanent screen profiles.</summary>
public static class ScreenPoliciesPlugin {
	public static void Configure(BlazorPluginBuilder plugin, bool permanentScreen, ScreenActivationMode emptyScreenLifetime) {
		Guard.ArgumentNotNull(plugin, nameof(plugin));
		const string pluginName = "Screen policies";
		plugin
			.WithName(pluginName)
			.AddInitializer(provider => new DemoPluginInitializer(provider.GetRequiredService<SharedUI.IApplication>(), provider.GetRequiredService<WorkspaceStatus>(), pluginName))
			.AddFinalizer(provider => new DemoPluginFinalizer(provider.GetRequiredService<SharedUI.IApplication>(), provider.GetRequiredService<WorkspaceStatus>(), pluginName))
			.AddBlock(block => block
				.WithId("empty-workspace")
				.WithName("Empty workspace")
				.WithPosition(4)
				.WithIconUrl("img/heading-solid.svg")
				.WithTooltip("Open the tabless empty-workspace screen when no normal screens are open")
				.AddScreen<EmptyWorkspaceScreen>(screen => screen
					.WithTitle("Empty workspace")
					.WithActivationMode(emptyScreenLifetime)
					.WithScreenKind(ScreenKind.Empty)
				)
				.WithDefaultScreen<EmptyWorkspaceScreen>()
				.AddMenu("Workspace", id: "empty", configure: menu => menu
					.AddItem<EmptyWorkspaceScreen>("Empty workspace", id: "empty", icon: "fas fa-window-maximize")
				)
			);
		if (permanentScreen)
			plugin
				.AddBlock(block => block
					.WithId("permanent-workspace")
					.WithName("Permanent workspace")
					.WithPosition(5)
					.WithIconUrl("img/heading-solid.svg")
					.WithTooltip("A screen that stays open for this application session")
					.AddScreen<PermanentWorkspaceScreen>("Permanent screen", activationMode: ScreenActivationMode.PermanentSingleton)
					.WithDefaultScreen<PermanentWorkspaceScreen>()
					.AddMenu("Permanent screen", id: "permanent", configure: menu => menu
						.AddItem<PermanentWorkspaceScreen>("Permanent screen", id: "permanent", icon: "fas fa-thumbtack")
					)
				);
	}
}
