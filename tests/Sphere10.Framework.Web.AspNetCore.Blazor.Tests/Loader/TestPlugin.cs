// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Web.AspNetCore.Blazor.Plugins;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests.Loader;

public class TestPlugin : BlazorRoutedPlugin {
	public TestPlugin() {
		Apps = new[] {
			new Sphere10.Framework.Web.AspNetCore.Blazor.Plugins.BlazorRoutedApplication("/",
				"Home",
				"abc",
				new[] {
					new BlazorRoutedApplicationBlock("test",
						"abc",
						new[] {
							new BlazorRoutedApplicationPage("/test",
								"test page",
								"abc",
								new[] {
									new BlazorRoutedMenuItem("Test Menu", "/app1/page1", new List<BlazorRoutedMenuItem>())
								})
						})
				}),
			new Sphere10.Framework.Web.AspNetCore.Blazor.Plugins.BlazorRoutedApplication("/app1",
				"app1",
				"abc",
				new[] {
					new BlazorRoutedApplicationBlock("app1",
						"abc",
						new[] {
							new BlazorRoutedApplicationPage("/app1/page1",
								"app1 page",
								"abc",
								new[] {
									new BlazorRoutedMenuItem("Test Menu", "/app1/page1", new List<BlazorRoutedMenuItem>())
								})
						})
				})
		};
	}

	public override IBlazorRoutedApplication[] Apps { get; }

	protected override void ConfigureServicesInternal(IServiceCollection serviceCollection) {
		serviceCollection.AddTransient<TestViewModel>();
	}
}


internal class TestViewModel {
}


