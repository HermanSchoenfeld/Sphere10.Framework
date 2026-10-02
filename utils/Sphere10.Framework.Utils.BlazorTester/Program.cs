// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Configuration;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Web.AspNetCore.Blazor;
using Sphere10.Framework.Web.AspNetCore.Blazor.Theming;
using Sphere10.Framework.Utils.BlazorTester.Application;
using Sphere10.Framework.Utils.BlazorTester.Loader;
using Sphere10.Framework.Utils.BlazorTester.WidgetGallery;

namespace Sphere10.Framework.Utils.BlazorTester;

public static class Program {
	public static void Main(string[] args) {
		var builder = WebApplication.CreateBuilder(args);
		builder.Services.AddRazorComponents().AddInteractiveServerComponents();
		ConfigureServices(builder.Services, builder.Configuration.GetValue<bool>("Demo:PermanentScreen"),
			builder.Configuration.GetValue("Demo:EmptyScreenLifetime", ScreenActivationMode.SingleInstance));

		var app = builder.Build();
		app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
		app.UseAntiforgery();
		app.MapStaticAssets();
		app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
		app.Run();
	}

	public static void ConfigureServices(IServiceCollection services, bool permanentScreen = false, ScreenActivationMode emptyScreenLifetime = ScreenActivationMode.SingleInstance) {
		Guard.Argument(emptyScreenLifetime is ScreenActivationMode.SingleInstance or ScreenActivationMode.MultiInstance, nameof(emptyScreenLifetime), "Choose a supported empty-screen lifetime.");
		services.AddScoped<IThemeService>(_ => new ThemeService(ThemeMode.ClassicBlue));
		services.BuildBlazorApplication()
			.WithTitle("Sphere10 Blazor demos")
			.WithFavicon("img/logo.svg", "image/svg+xml")
			.ConfigureApplication(WorkspaceCommands.Configure)
			.AddPlugin(Sphere10Plugin.Configure)
			.AddPlugin(WidgetGalleryPlugin.Configure)
			.AddPlugin(plugin => ScreenPoliciesPlugin.Configure(plugin, permanentScreen, emptyScreenLifetime))
			.Build();
	}
}
