// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading.Tasks;
using Sphere10.Framework;
using Sphere10.Framework.Application.UI;

namespace PackageConsumer.Application;

public static class PackageUsage {
	public static IScreenActivationPolicyRegistry CreatePolicies() {
		var policies = new ScreenActivationPolicyRegistry();
		policies.RegisterInstance(typeof(Screen), ScreenActivationMode.SingleInstance);
		return policies;
	}

	public static IApplicationScreen CreateScreen() => new Screen();

	public static IApplicationBlock CreateBlock() {
		var block = new ApplicationBlock { Name = "Portable application", DefaultScreen = typeof(Screen), DefaultScreenActivationMode = ScreenActivationMode.PermanentSingleton };
		var menu = new ApplicationMenu<ApplicationMenuItem> { Text = "Actions" };
		menu.AddItem(new ApplicationMenuItem { Title = "Refresh" });
		block.AddMenu(menu);
		return block;
	}

	public static ApplicationScreenDefinition[] CreateScreenDefinitions() => Tools.UI.GetScreenDefinitions(new[] { CreateBlock() });

	public static IApplicationMenu[] CreateMenus() => CreateBlock().Menus;

	public static IApplicationMenuItem[] CreateItems() => CreateMenus()[0].Items;

	public static IApplicationPlugin CreatePlugin() => new ApplicationPluginBuilder()
		.WithName("Portable plugin")
		.AddBlock(CreateBlock())
		.Build();

	public static IApplicationPlugin[] GetPlugins(IApplication application) => application.Plugins;

	public static IWizard<string, string> CreateWizard() => new Wizard<string, string>(
		"Portable wizard", "draft", new[] { "Details", "Review" },
		finish: _ => Task.FromResult<Result<bool>>(true));

	public static IApplicationMenuItem[] MergeCommands(params IApplicationMenuItem[][] layers) => Tools.UI.MergeMenuItems(layers);

	public static Type ApplicationContract => typeof(IApplication);

	public static Type BlockContract => typeof(IApplicationBlock);

	private class Screen : IApplicationScreen {
	}
}
