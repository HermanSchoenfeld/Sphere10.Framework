// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Web.AspNetCore.Blazor;

namespace Sphere10.Framework.Utils.BlazorTester.Application;

/// <summary>Uses the ordinary plugin and block builders for the empty and permanent screen profiles.</summary>
public static class ScreenPoliciesPlugin {
	public static void Configure(BlazorPluginBuilder plugin, bool permanentScreen, ScreenActivationMode emptyScreenLifetime) {
		Guard.ArgumentNotNull(plugin, nameof(plugin));
		plugin.WithName("Screen policies")
			.AddBlock(block => block.WithId("empty-workspace").WithName("Empty workspace").WithPosition(3)
				.WithIconUrl("img/heading-solid.svg").WithTooltip("Shown when all normal screens are closed")
				.WithDefaultScreen<EmptyWorkspaceScreen>("Empty workspace", emptyScreenLifetime, ScreenKind.Empty));
		if (permanentScreen)
			plugin.AddBlock(block => block.WithId("permanent-workspace").WithName("Permanent workspace").WithPosition(4)
				.WithIconUrl("img/heading-solid.svg").WithTooltip("A screen that stays open for this application session")
				.WithDefaultScreen<PermanentWorkspaceScreen>("Permanent screen", ScreenActivationMode.PermanentSingleton)
				.AddMenu(menu => menu.WithId("permanent").WithText("Permanent screen")
					.ConfigureItem(item => item.WithId("permanent").WithText("Permanent screen").WithScreen<PermanentWorkspaceScreen>().AsPermanentSingleton())));
	}
}
