// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Web.AspNetCore.Blazor.UI.Application;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
public class ApplicationSettingsMenuTests {
	[TestCase(false)]
	[TestCase(true)]
	public async Task OptionsReusesContextualHeadingAndPreservesProductMenuOrder(bool existingOptions) {
		var calls = 0;
		var block = new BlazorApplicationBlockBuilder()
			.WithId("product")
			.WithName("Product")
			.AddMenu(menu => menu
				.WithId("file")
				.WithText("File")
				.AddActionItem("open", "Open", () => { })
			)
			.AddMenu(menu => menu
				.WithId("views")
				.WithText("Views")
				.AddActionItem("view", "View", () => { })
			);
		if (existingOptions)
			block.AddMenu(menu => menu
				.WithId("product-preferences")
				.WithText("&Options")
				.AddActionItem("existing-preference", "Existing preference", () => calls++)
			);
		block.AddMenu(menu => menu
			.WithId("help")
			.WithText("Help")
			.AddActionItem("about", "About", () => { })
		);
		var services = new ServiceCollection().AddLogging();
		services.AddSingleton<IApplicationSettingsStore, MissingStore>();
		services.BuildBlazorApplication()
			.WithSettingsKey("product")
			.AddSettings<Preferences>("preferences")
			.AddPlugin(plugin => plugin
				.WithName("Product")
				.AddBlock(block.Build())
			)
			.Build();
		await using var provider = services.BuildServiceProvider();
		var application = provider.GetRequiredService<IBlazorApplication>();
		await application.InitializeAsync();
		await application.ScreenHost.SelectBlockAsync("product");
		using var shell = new ApplicationShell { Application = application, ScreenHost = application.ScreenHost };
		var menus = (IBlazorApplicationMenu[])typeof(ApplicationShell).GetProperty("MergedMenus", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(shell);

		Assert.That(menus.Select(menu => menu.Text.Replace("&", string.Empty)), Is.EqualTo(new[] { "File", "Views", "Options", "Help" }));
		var options = menus[2];
		Assert.That(options.Items.Count(item => item.Title == "Screen Options..."), Is.EqualTo(1));
		if (existingOptions) {
			Assert.That(options.Id, Is.EqualTo("product-preferences"));
			await application.ScreenHost.UserInterfaceServices.ExecuteMenuItemAsync(options.Items.Single(item => item.Id == "existing-preference"));
			Assert.That(calls, Is.EqualTo(1));
		}
	}

	// This composition test restores declared defaults without invoking browser storage or writing preferences.
	private sealed class MissingStore : IApplicationSettingsStore {
		public Task<ApplicationSettingsStoredProfile> LoadAsync(string profileKey, object defaults, bool hasExplicitStorageId, object storageId, CancellationToken cancellationToken = default) {
			return Task.FromResult<ApplicationSettingsStoredProfile>(null);
		}

		public Task SaveAsync(string profileKey, ApplicationSettingsStoredProfile snapshot, bool hasExplicitStorageId, object storageId, CancellationToken cancellationToken = default) {
			throw new InvalidOperationException("Menu composition must not write settings.");
		}
	}

	public sealed class Preferences : SettingsObject {
		public bool ShowDetails { get; set; } = true;
	}
}
