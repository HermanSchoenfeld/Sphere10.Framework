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
	public static ApplicationScreenDefinition CreateScreenDefinition() => new(typeof(Screen), activationMode: ScreenActivationMode.SingleInstance);

	public static IApplicationScreen CreateScreen() => new Screen();

	public static IApplicationBlock CreateBlock() {
		var block = new Block {
			Name = "Portable application",
			DefaultScreenDefinition = new ApplicationScreenDefinition(typeof(Screen), activationMode: ScreenActivationMode.PermanentSingleton)
		};
		var menu = new Menu { Text = "Actions" };
		menu.AddItem(new Command { Title = "Refresh" });
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

	public static IApplicationPlugin[] GetLoadedPlugins(IApplication application) => application.LoadedPlugins;

	public static IApplicationBlock[] GetBlocks(IApplication application) => application.Blocks;

	public static ApplicationPluginRegistration<IApplicationPlugin> CaptureRegistration(IApplicationPlugin plugin, IApplicationBlock[] blocks) => new(plugin, blocks);

	public static IWizard<string, string> CreateWizard() => new Wizard<string, string>(
		"Portable wizard", "draft", new[] { "Details", "Review" },
		finish: _ => Task.FromResult<Result<bool>>(true));

	public static IApplicationMenuItem[] MergeCommands(params IApplicationMenuItem[][] layers) => Tools.UI.MergeMenuItems(layers);

	public static ApplicationCommandLayer[] GetCommandLayers(IApplication application) => Tools.UI.GetCommandLayers(application);

	public static IApplicationMenuItem[] MergeApplicationCommands(params ApplicationCommandLayer[] layers) =>
		Tools.UI.MergeApplicationToolBarItems<IApplicationMenuItem>(layers);

	public static Task<bool> CloseCurrentScreen(ISingleApplicationScreenHost host) => host.CloseActiveScreenAsync();

	public static IApplicationScreen[] GetOpenScreens(IMultiApplicationScreenHost host) => host.OpenScreens;

	public static Type ApplicationContract => typeof(IApplication);

	public static Type BlockContract => typeof(IApplicationBlock);

	private class Screen : IApplicationScreen {
		public IApplicationScreenHost Host { get; set; }

		public IApplicationBlock Block { get; set; }
	}

	private sealed class Block : ApplicationBlockBase {
	}

	private sealed class Menu : ApplicationMenuBase<Command> {
	}

	private sealed class Command : ApplicationMenuItemBase {
	}
}
