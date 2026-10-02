// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Web;
using Sphere10.Framework.Web.AspNetCore.Blazor;
using Sphere10.Framework.Web.AspNetCore.MVC;
using PackageConsumer.Web;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddRazorComponents().AddInteractiveServerComponents();
builder.Services.BuildBlazorApplication()
	.WithTitle("Sphere10 package consumer")
	.WithFavicon("data:,")
	.AddPlugin(plugin => plugin
		.WithName("Package plugin")
		.AddBlock(block => block.WithId("package").WithName("Package block")
			.WithDefaultScreen<WelcomeScreen>("Package screen")
			.AddMenu(menu => menu.WithId("records").WithText("Records")
				.ConfigureItem(item => item.WithId("grid").WithText("Package grid").WithScreen<WelcomeScreen>().AsPermanentSingleton()))))
	.Build();
var app = builder.Build();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
app.MapGet("/sitemap.xml", () => {
	var result = new XmlResult(new SitemapXml());
	return Results.Content(result.Content, result.ContentType);
});
app.Run();
