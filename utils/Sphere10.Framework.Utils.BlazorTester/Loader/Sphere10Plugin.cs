// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Hamish Rose
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Sphere10.Framework.Web.AspNetCore.Blazor;
using Sphere10.Framework.Web.AspNetCore.Blazor.Plugins;

namespace Sphere10.Framework.Utils.BlazorTester.Loader;

public class Sphere10Plugin : Plugin {
	public override IEnumerable<IApp> Apps { get; } = new List<IApp> {
		new Sphere10.Framework.Web.AspNetCore.Blazor.Plugins.App("/",
			"Sphere10.Framework",
			"./img/heading-solid.svg",
			new[] {
				new AppBlock("Sphere10.Framework",
					"fa-link",
					new[] {
						new AppBlockPage("/",
							"Home",
							"fa-home",
							new List<MenuItem> {
								new("File",
									"#",
									new List<MenuItem> {
										new("Print", "#", "fa-print")
									},
									"fa-list")
							}),
						new AppBlockPage("/servers", "Servers", "fa-cogs")
					})
			})
	};

	protected override void ConfigureServicesInternal(IServiceCollection serviceCollection) {
		serviceCollection.AddViewModelsFromAssembly(typeof(Sphere10.Framework.Web.AspNetCore.Blazor.Components.ComponentWithViewModel<>).Assembly);
	}
}


