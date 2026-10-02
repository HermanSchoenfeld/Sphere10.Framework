// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NUnit.Framework;
using Sphere10.Framework.Web.AspNetCore.Blazor.Tests.Loader;
using Sphere10.Framework.Web.AspNetCore.Blazor.UI.Application;
using Sphere10.Framework.Web.AspNetCore.Blazor.UI.MainFrame;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationToolBarContentTests {
	[TestCase(typeof(ApplicationCommandBar))]
	[TestCase(typeof(ApplicationShell))]
	public async Task ToolbarContentRendersEvenWithoutCommandItems(Type componentType) {
		var services = new ServiceCollection().AddLogging().AddSphere10Blazor();
		services.AddSingleton<NavigationManager>(new TestNavigationManager());
		services.AddSingleton<IJSRuntime, GalleryRenderingTests.TestJsRuntime>();
		await using var provider = services.BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			var content = (RenderFragment)(builder => builder.AddMarkupContent(0, "<label>Search<input aria-label=\"Host search\" /></label>"));
			var rendered = await renderer.RenderComponentAsync(componentType, ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(ApplicationCommandBar.ToolBarContent)] = content
			}));
			Assert.That(rendered.ToHtmlString(), Does.Contain("role=\"toolbar\"").And.Contain("Application toolbar").And.Contain("Host search"));
			Assert.That(rendered.ToHtmlString(), Does.Contain("sphere10-tool-content").And.Not.Contain("Application commands"));
		});
	}
}
