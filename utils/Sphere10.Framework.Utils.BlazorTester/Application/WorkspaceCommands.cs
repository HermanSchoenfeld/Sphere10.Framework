// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Application;
using Sphere10.Framework.Web.AspNetCore.Blazor;
using Sphere10.Framework.Web.AspNetCore.Blazor.Modal;
using BlazorUI = global::Sphere10.Framework.Web.AspNetCore.Blazor;
using SharedUI = global::Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Utils.BlazorTester.Application;

/// <summary>Application commands shared by every block and extended by the active screen.</summary>
public static class WorkspaceCommands {
	public static void Configure(BlazorUI.Application application) {
		Guard.ArgumentNotNull(application, nameof(application));
		var scratchpadDefinition = application.Blocks.SelectMany(block => block.ScreenDefinitions).SingleOrDefault(definition => definition.ScreenType == typeof(ScratchpadScreen));
		var scratchpadCommand = scratchpadDefinition?.Activators.OfType<BlazorScreenMenuItem>().FirstOrDefault();
		var workspace = scratchpadCommand?.Parent?.Parent ?? application.Blocks.SingleOrDefault(block => block.Name == "Workspace");
		var newScratchpad = new ApplicationMenuItemBuilder()
			.WithId("new-scratchpad")
			.WithText("New scratchpad")
			.WithIcon("fas fa-plus")
			.EnabledWhen(() => scratchpadCommand?.IsEnabled ?? false)
			.WithAction(async (_, token) => await application.ScreenHost.ExecuteMenuItemAsync(scratchpadCommand, token))
			.Build();
		var applicationAction = new ApplicationMenuItemBuilder()
			.WithId("save")
			.WithText("Application command")
			.WithIcon("fas fa-play")
			.WithAction((provider, token) => {
				token.ThrowIfCancellationRequested();
				provider.GetRequiredService<WorkspaceStatus>().Increment();
				return Task.CompletedTask;
			})
			.Build();
		application.SetMenus(new[] {
			new ApplicationMenuBuilder()
				.WithId("file")
				.WithText("File")
				.AddItem(newScratchpad)
				.AddItem(applicationAction)
				.AddItem(new BlazorMenuSeparator())
				.AddItem(item => item
					.WithId("close-screen")
					.WithText("Close screen")
					.WithIcon("fas fa-times")
					.WithAction(async (provider, token) => {
						await provider.GetRequiredService<ISingleApplicationScreenHost>().CloseActiveScreenAsync(token);
					})
				)
				.AddItem(item => item
					.WithId("close-all")
					.WithText("Close all screens")
					.WithIcon("fas fa-times-circle")
					.WithAction(async (provider, token) => {
						var host = provider.GetRequiredService<IMultiApplicationScreenHost<BlazorUI.IApplicationBlock, ApplicationScreenSession>>();
						await host.CloseScreensAsync(host.Screens.Where(screen => !screen.IsPermanent && screen.ScreenKind == ScreenKind.Normal), token);
					})
				)
				.AddItem(new BlazorMenuSeparator())
				.AddItem(item => item
					.WithId("exit")
					.WithText("Exit application")
					.WithIcon("fas fa-sign-out-alt")
					.WithAction(async (provider, token) =>
						await provider.GetRequiredService<SharedUI.IApplication>().ExitAsync(cancellationToken: token))
				)
				.Build(),
			new ApplicationMenuBuilder()
				.WithId("view")
				.WithText("View")
				.AddItem(item => item
					.WithId("layout-tabs")
					.WithText("Tabbed screens")
					.WithIcon("fas fa-folder")
					.WithAction(async (provider, token) =>
						await provider.GetRequiredService<SharedUI.IApplicationScreenHost>().TrySetScreenModeAsync(ScreenMode.MultiView, token))
				)
				.AddItem(item => item
					.WithId("layout-single")
					.WithText("Single screen")
					.WithIcon("fas fa-window-maximize")
					.WithAction(async (provider, token) =>
						await provider.GetRequiredService<SharedUI.IApplicationScreenHost>().TrySetScreenModeAsync(ScreenMode.SingleView, token))
				)
				.Build(),
			new ApplicationMenuBuilder()
				.WithId("help")
				.WithText("Help")
				.AddItem(item => item
					.WithId("workspace-help")
					.WithText("Application guide")
					.WithIcon("fas fa-question-circle")
					.EnabledWhen(() => workspace != null)
					.WithAction(async (_, token) => await application.ScreenHost.ActivateBlockAsync(workspace, token))
				)
				.AddItem(item => item
					.WithId("about")
					.WithText("About Sphere10 Framework")
					.WithIcon("fas fa-info-circle")
					.WithAction((provider, token) => {
						token.ThrowIfCancellationRequested();
						return provider.GetRequiredService<ViewService>().DialogAsync("About Sphere10 Framework",
							"This application uses Sphere10 application blocks, shared menu and toolbar commands, retained screen tabs, and the original component examples.");
					})
				)
				.Build()
		});
		application.SetToolBarItems(new BlazorUI.IApplicationMenuItem[] { newScratchpad, new BlazorMenuSeparator(), applicationAction });
	}
}
