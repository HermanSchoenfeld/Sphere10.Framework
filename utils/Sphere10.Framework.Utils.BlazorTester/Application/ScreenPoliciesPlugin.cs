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

namespace Sphere10.Framework.Utils.BlazorTester.Application;

/// <summary>Uses the ordinary plugin and block builders for the empty and permanent screen profiles.</summary>
public static class ScreenPoliciesPlugin {
	public static void Configure(BlazorPluginBuilder plugin, bool permanentScreen, ScreenActivationMode emptyScreenLifetime) {
		Guard.ArgumentNotNull(plugin, nameof(plugin));
		const string pluginName = "Screen policies";
		plugin
			.WithName(pluginName)
			.RegisterInitializer(provider => new DemoPluginInitializer(provider.GetRequiredService<IApplication>(), provider.GetRequiredService<WorkspaceStatus>(), pluginName))
			.RegisterFinalizer(provider => new DemoPluginFinalizer(provider.GetRequiredService<IApplication>(), provider.GetRequiredService<WorkspaceStatus>(), pluginName))
			.ConfigureBlock(block => block
				.WithId("empty-workspace")
				.WithName("Empty workspace")
				.WithPosition(4)
				.WithIconUrl("img/heading-solid.svg")
				.WithTooltip("Open the tabless empty-workspace screen when no normal screens are open")
				.WithDefaultScreen<EmptyWorkspaceScreen>("Empty workspace", emptyScreenLifetime, ScreenKind.Empty)
				.ConfigureMenu(menu => menu
					.WithId("empty")
					.WithText("Workspace")
					.AddScreenItem<EmptyWorkspaceScreen>("empty", "Empty workspace", emptyScreenLifetime, screenKind: ScreenKind.Empty)
				)
			);
		if (permanentScreen)
			plugin
				.ConfigureBlock(block => block
					.WithId("permanent-workspace")
					.WithName("Permanent workspace")
					.WithPosition(5)
					.WithIconUrl("img/heading-solid.svg")
					.WithTooltip("A screen that stays open for this application session")
					.WithDefaultScreen<PermanentWorkspaceScreen>("Permanent screen", ScreenActivationMode.PermanentSingleton)
					.ConfigureMenu(menu => menu
						.WithId("permanent")
						.WithText("Permanent screen")
						.ConfigureItem(item => item
							.WithId("permanent")
							.WithText("Permanent screen")
							.WithScreen<PermanentWorkspaceScreen>()
							.AsPermanentSingleton()
						)
					)
				);
	}
}
