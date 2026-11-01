// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Utils.WinFormsTester.Lifecycle;
using Sphere10.Framework.Utils.WinFormsTester.Screens;
using Sphere10.Framework.Utils.WinFormsTester.Wizard;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

/// <summary>Registers the running tester's workspace and notes demonstrations through the framework's plugin builder.</summary>
public static class WinFormsDemoPlugin {
	/// <summary>Keeps the live mode toggle in the application layer while individual blocks contribute contextual commands.</summary>
	public static void ConfigureApplicationCommands(WinFormsApplication application) {
		Guard.ArgumentNotNull(application, nameof(application));
		var menu = new WinFormsApplicationMenuBuilder()
			.WithText("Views")
			.AddActionItem("Toggle Screen Mode", userInterface => {
				var mode = userInterface.ScreenHost.ScreenMode == ScreenMode.MultiView ? ScreenMode.SingleView : ScreenMode.MultiView;
				SetScreenMode(userInterface, mode);
			}, (Image)Resources.Generic16x16.Clone())
			.Build();
		application.SetMenus(new[] { menu });
		application.SetToolBarItems(menu.Items);
		if (application.ScreenHost.UserInterfaceServices.PrimaryUIController is Form form)
			form.Disposed += (_, _) => menu.Dispose();
	}

	public static void Configure(WinFormsApplicationPluginBuilder plugin, bool freshEmptyWorkspace = false) {
		Guard.ArgumentNotNull(plugin, nameof(plugin));
		var emptyScreen = freshEmptyWorkspace ? typeof(ScreenHostingEmptyMultiTestScreen) : typeof(ScreenHostingEmptyTestScreen);
		var emptyLifetime = freshEmptyWorkspace ? ScreenActivationMode.MultiInstance : ScreenActivationMode.SingleInstance;
		IWinFormsApplicationBlock workspace = null;

		// Native definitions own their images; copies keep block removal from disposing resources used by another block or demo run.
		plugin
			.WithName("Workspace demonstrations")
			.AddInitializer<DemoPluginInitializer>()
			.AddInitializer<DemoWorkspaceInitializer>()
			.AddFinalizer<DemoPluginFinalizer>()
			.AddFinalizer<DemoWorkspaceFinalizer>()
			.AddBlock(block => block
				.WithName("Workspace")
				.WithImage32x32((Image)Resources.TestBlock32x32.Clone())
				.WithImage8x8((Image)Resources.TestBlock8x8.Clone())
				.AddScreen<ScreenHostingSettingsTestScreen>(screen => screen
					.WithTitle("Settings")
					.WithActivationMode(ScreenActivationMode.SingleInstance)
				)
				.AddScreen<ScreenHostingDesignTestScreen>(screen => screen
					.WithTitle("Design")
					.WithStableKey("design")
				)
				.AddScreen<ScreenHostingDesignTestScreen>(screen => screen
					.WithTitle("CPU Inspector")
					.WithStableKey("cpu-inspector")
					.WithActivator(context => new ScreenHostingDesignTestScreen("CPU Inspector", "tester.cpu-inspector.standalone-grid"))
					.UseSettings(
						() => new ScreenHostingPreferences { Heading = "CPU summary" }
					)
					.AsSingleInstance(),
					out var cpuInspector
				)
				.AddScreen<ScreenHostingDesignTestScreen>(screen => screen
					.WithTitle("GPU Inspector")
					.WithStableKey("gpu-inspector")
					.WithActivator(context => new ScreenHostingDesignTestScreen("GPU Inspector", "tester.gpu-inspector.standalone-grid"))
					.UseSettings(
						() => new ScreenHostingPreferences { Heading = "GPU summary" }
					)
					.WithSettingsEditor(context => new ScreenHostingPreferencesEditor((ScreenHostingPreferences)context.Settings))
					.AsSingleInstance(),
					out var gpuInspector
				)
				.AddScreen<ScreenHostingDesignTestScreen>(screen => screen
					.WithTitle("Menu-free Inspector")
					.WithStableKey("menu-free-inspector")
					.WithActivator(context => new ScreenHostingDesignTestScreen("Menu-free Inspector")),
					out var menuFreeInspector
				)
				.AddScreen<ScreenHostingPlainTestScreen>(screen => screen
					.WithTitle("Plain Screen")
				)
				.AddScreen(screen => screen
					.WithScreen(emptyScreen)
					.WithTitle("Empty Workspace")
					.WithActivationMode(freshEmptyWorkspace ? ScreenActivationMode.MultiInstance : ScreenActivationMode.SingleInstance)
					.WithScreenKind(ScreenKind.Empty)
				)
				.AddScreen<ScreenA>(screen => screen
					.WithTitle("Calendar And Toolbar")
				)
				.AddScreen<ScreenB>(screen => screen
					.WithTitle("Expandable Panels")
				)
				.AddScreen<ScreenC>(screen => screen
					.WithTitle("Task Pane Grids")
				)
				.WithDefaultScreen(freshEmptyWorkspace ? emptyScreen : typeof(ScreenHostingSettingsTestScreen))
				.AddMenu(menu => menu
					.WithText("Screen Hosting")
					.WithImage32x32((Image)Resources.Tests32x32.Clone())
					.AddItem(item => item
						.AsScreenItem()
						.WithText("Settings")
						.WithScreen<ScreenHostingSettingsTestScreen>()
						.WithImage((Image)Resources.Settings16x16.Clone())
					)
					.AddShowScreenItem<ScreenHostingDesignTestScreen>("New Design", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<ScreenHostingDesignTestScreen>("First Design (by type)", (Image)Resources.Generic16x16.Clone(), showOnToolBar: false)
					.AddShowScreenItem(typeof(ScreenHostingDesignTestScreen), "First Design (by runtime type)", (Image)Resources.Generic16x16.Clone(), showOnToolBar: false)
					.AddShowScreenItem<ScreenHostingPlainTestScreen>("Plain Screen (No Bars)", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem(cpuInspector)
					.AddShowScreenItem(gpuInspector)
					.AddActionItem("Open Menu-free Inspector", userInterface => {
						var host = ((BlockMainForm)userInterface.PrimaryUIController).ScreenHost;
						host.ActivateScreen(workspace, Tools.UI.ResolveScreenRegistration(workspace.ScreenDefinitions, menuFreeInspector));
					})
					.AddActionItem("Use SingleView", userInterface => SetScreenMode(userInterface, ScreenMode.SingleView), (Image)Resources.Generic16x16.Clone())
					.AddActionItem("Use MultiView", userInterface => SetScreenMode(userInterface, ScreenMode.MultiView), (Image)Resources.Generic16x16.Clone())
					.AddActionItem("Close Ordinary Screens", userInterface => CloseOrdinaryScreens(userInterface, workspace), (Image)Resources.Generic16x16.Clone())
					.AddItem(item => item
						.AsScreenItem()
						.WithText("Empty Workspace")
						.WithScreen(emptyScreen)
						.ShowOnExplorerBar(false)
						.ShowOnToolBar(false)
					)
				)
				.AddMenu(menu => menu
					.WithText("Navigation Samples")
					.WithImage32x32((Image)Resources.Menu32x32.Clone())
					.AddShowScreenItem<ScreenA>("Calendar And Toolbar", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<ScreenB>("Expandable Panels", (Image)Resources.Generic16x16.Clone())
					.AddShowScreenItem<ScreenC>("Task Pane Grids", (Image)Resources.Generic16x16.Clone())
				)
				.AddMenu(menu => menu
					.WithText("Wizard")
					.WithImage32x32((Image)Resources.Wizard32x32.Clone())
					.AddActionItem("Demo Wizard", async userInterface => await ShowDemoWizardAsync((Form)userInterface.PrimaryUIController), (Image)Resources.Wizard16x16.Clone())
				),
				out workspace
			);

		// The notes feature demonstrates permanent screens and guarded removal of a real application block.
		if (!freshEmptyWorkspace)
			plugin
				.AddBlock(block => block
					.WithName("Notes")
					.WithImage32x32((Image)Resources.Menu32x32.Clone())
					.AddScreen<ScreenHostingPermanentTestScreen>(screen => screen
						.WithTitle("Permanent Notes")
						.WithActivationMode(ScreenActivationMode.PermanentSingleton)
					)
					.AddMenu(menu => menu
						.WithText("Notes")
						.WithImage32x32((Image)Resources.Menu32x32.Clone())
						.AddItem(item => item
							.AsScreenItem()
							.WithText("Permanent Notes")
							.WithScreen<ScreenHostingPermanentTestScreen>()
							.WithImage((Image)Resources.Generic16x16.Clone())
						)
						.AddActionItem("Remove Notes Block", RemoveNotesBlock, (Image)Resources.Generic16x16.Clone())
					)
				);
	}

	private static void SetScreenMode(IUserInterfaceServices userInterface, ScreenMode mode) {
		var form = (BlockMainForm)userInterface.PrimaryUIController;
		form.Status = form.ScreenHost.TrySetScreenMode(mode)
			? $"Screen mode: {mode}"
			: "A screen blocked the mode change. Clear its cancellation checkbox and try again.";
	}

	private static void RemoveNotesBlock(IUserInterfaceServices userInterface) {
		var form = (BlockMainForm)userInterface.PrimaryUIController;
		var block = form.RegisteredBlocks.FirstOrDefault(block => block.Name == "Notes");
		if (block == null)
			return;
		form.UnregisterBlock(block);
		form.Status = form.IsBlockRegistered(block)
			? "The notes screen blocked removal. Clear its cancellation checkbox and try again."
			: "Notes block removed. Close the remaining ordinary screens to reveal the empty workspace.";
	}

	private static void CloseOrdinaryScreens(IUserInterfaceServices userInterface, IWinFormsApplicationBlock workspace) {
		var form = (BlockMainForm)userInterface.PrimaryUIController;
		var closing = form.ScreenHost.OpenScreens.Where(screen => screen.ActivationMode != ScreenActivationMode.PermanentSingleton).ToArray();
		var closed = form.ScreenHost.CloseScreens(closing);
		if (closed && form.ScreenHost.OpenScreens.Length == 0) {
			var empty = workspace.ScreenDefinitions.Single(definition => definition.ScreenKind == ScreenKind.Empty);
			form.ScreenHost.ActivateScreen(workspace, empty);
		}
		form.Status = !closed ? "A screen blocked closing. Clear its cancellation checkbox and try again."
			: form.ScreenHost.OpenScreens.Length > 0 ? "Permanent notes remain. Remove the Notes block before the empty workspace can appear."
			: "The empty workspace has no tab. Open a screen and close it again to check its instance lifetime.";
	}

	private static async Task ShowDemoWizardAsync(Form owner) {
		using var wizard = new WinFormsWizardBuilder<DemoWizardModel>()
			.WithTitle("Demo Wizard")
			.WithModel(DemoWizardModel.Default)
			.AddScreen(new EnterNameScreen())
			.AddScreen(new EnterAgeScreen())
			.AddScreen(new CantGoBackScreen())
			.AddScreen(new ConfirmScreen())
			.OnFinished(async model => {
				await DialogEx.ShowAsync(owner, SystemIconType.Information, "Result", $"Name: {model.Name}, Age: {model.Age}", "OK");
				return Result.Success;
			})
			.OnCancelled(_ => Result.Success)
			.Build();
		await wizard.Start(owner);
	}
}
