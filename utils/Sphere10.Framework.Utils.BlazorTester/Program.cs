// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Sphere10.Framework.Web.AspNetCore.Blazor;
using Sphere10.Framework.Web.AspNetCore.Blazor.Plugins;
using Sphere10.Framework.Web.AspNetCore.Blazor.Services;
using Sphere10.Framework.Utils.BlazorTester.Loader;
using Sphere10.Framework.Utils.BlazorTester.Loader.Plugins;
using Sphere10.Framework.Utils.BlazorTester.Loader.Services;
using Sphere10.Framework.Utils.BlazorTester.WidgetGallery;

namespace Sphere10.Framework.Utils.BlazorTester;

public static class Program {
	public static void Main(string[] args) {
		var builder = WebApplication.CreateBuilder(args);
		builder.Services.AddRazorComponents().AddInteractiveServerComponents();
		ConfigureServices(builder.Services);

		var app = builder.Build();
		app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
		app.UseAntiforgery();
		app.MapStaticAssets();
		app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
		app.Run();
	}

	public static void ConfigureServices(IServiceCollection services) {
		services.AddSphere10Blazor();
		Application.WorkspaceRegistration.AddWorkspace(services);
		services.AddViewModelsFromAssembly(typeof(Program).Assembly);
		services.AddScoped<IPluginLocator, StaticPluginLocator>();
		services.AddScoped<IPluginManager, DefaultPluginManager>();
		services.AddScoped<IAppManager, DefaultAppManager>();
		services.AddScoped<INodeService, MockNodeService>();
		services.AddScoped<IEndpointManager, DefaultEndpointManager>();
		new Sphere10Plugin().ConfigureServices(services);
		new WidgetGalleryPlugin().ConfigureServices(services);
	}

}
