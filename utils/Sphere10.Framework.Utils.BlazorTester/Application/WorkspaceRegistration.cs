// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Web.AspNetCore.Blazor;
using Sphere10.Framework.Web.AspNetCore.Blazor.Logic;

namespace Sphere10.Framework.Utils.BlazorTester.Application;

public static class WorkspaceRegistration {
	public static void AddWorkspace(IServiceCollection services) {
		services.AddScoped<WorkspaceStatus>();
		services.AddApplicationBlock(block => block
			.WithId("workspace")
			.WithName("Workspace")
			.WithDefaultScreen<OverviewScreen>()
			.AddMenu(menu => menu.WithId("work").WithText("Work")
				.AddScreenItem<OverviewScreen>("overview", "Overview")
				.AddScreenItem<EditorScreen>("editor", "Guarded editor")
				.AddScreenItem<ScratchpadScreen>("scratchpad", "New scratchpad", ScreenActivationMode.MultiInstance)
				.AddActionItem("action", "Run scoped async action", async (provider, cancellationToken) => {
					// Simulate an asynchronous operation without contacting an external service.
					await Task.Delay(250, cancellationToken);
					provider.GetRequiredService<WorkspaceStatus>().Increment();
				})));
		services.AddApplicationBlock(block => block
			.WithId("components")
			.WithName("Component gallery")
			.WithPosition(1)
			.WithDefaultScreen<ComponentScreen>()
			.AddMenu(menu => menu.WithId("gallery").WithText("Components")
				.AddScreenItem<ComponentScreen>("gallery", "Grids, tables and dialogs")));
	}
}
