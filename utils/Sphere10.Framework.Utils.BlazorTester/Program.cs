// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Web.AspNetCore.Blazor;
using Sphere10.Framework.Web.AspNetCore.Blazor.Theming;
using Sphere10.Framework.Utils.BlazorTester.Loader;
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
		services.AddScoped<IThemeService>(_ => new ThemeService(ThemeMode.ClassicBlue));
		services.AddSphere10Blazor()
			.AddSphere10BlazorPlugin(Sphere10Plugin.Configure)
			.AddSphere10BlazorPlugin(WidgetGalleryPlugin.Configure);
	}
}
