// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Web.AspNetCore.Blazor.UI.Application;
using Sphere10.Framework.Web.AspNetCore.Blazor.UI.MainFrame;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationCommandScopeTests {
	[Test]
	public async Task ActivatingAnotherBlockReplacesItsCommandsAndRestoresCommonFileCommands() {
		await using var provider = Services().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var host = provider.GetRequiredService<IBlazorApplicationScreenHost>();
		ConfigureApplication(provider);
		await renderer.Dispatcher.InvokeAsync(async () => {
			var pipe = await host.ActivateScreenAsync("pipes", "pipe-screen");
			var rendered = await renderer.RenderComponentAsync<ApplicationShell>();
			var bar = provider.GetRequiredService<CommandCapture>().Bar;
			Assert.That(bar.Menus.Where(menu => menu.Items.Any(item => item.IsVisible)).Select(menu => menu.Id), Is.EqualTo(new[] { "file", "tools", "inspect", "help" }));
			Assert.That(bar.Menus.Count(menu => menu.Id == "file"), Is.EqualTo(1));
			Assert.That(bar.Menus.Single(menu => menu.Id == "file").Items.Where(item => item.IsVisible).Select(item => item.Id),
				Is.EqualTo(new[] { "common-open", "save", "export", "pipe-file", "export", "save" }));
			Assert.That(bar.ToolBarItems.Select(item => item.Title), Is.EqualTo(new[] { "Common tool", "Common save", "Pipe calculator", "Pipe save" }));
			Assert.That(Header(rendered.ToHtmlString()), Does.Not.Contain("Internal commands").And.Not.Contain("Hidden pipe registration"));

			var admin = await host.ActivateScreenAsync("administration", "admin-screen");
			Assert.That(host.ActiveBlock.Id, Is.EqualTo("administration"));
			Assert.That(bar.Menus.Where(menu => menu.Items.Any(item => item.IsVisible)).Select(menu => menu.Id), Is.EqualTo(new[] { "file", "reports", "help" }));
			Assert.That(bar.Menus.Single(menu => menu.Id == "file").Items.Where(item => item.IsVisible).Select(item => item.Id),
				Is.EqualTo(new[] { "common-open", "save", "export", "admin-file" }));
			Assert.That(bar.ToolBarItems.Select(item => item.Title), Is.EqualTo(new[] { "Common tool", "Common save", "Administration tool" }));
			Assert.That(Header(rendered.ToHtmlString()), Does.Contain("Reports").And.Contain("Common save").And.Not.Contain("Pipe calculator").And.Not.Contain("Pipe save").And.Not.Contain("Tools"));

			await host.ShowScreenAsync(pipe.Id);
			Assert.That(host.ActiveScreen, Is.SameAs(pipe));
			Assert.That(host.Screens, Has.Length.EqualTo(2));
			Assert.That(host.Screens.Single(session => session.Id == admin.Id), Is.SameAs(admin));
			Assert.That(bar.Menus.Single(menu => menu.Id == "file").Items.Where(item => item.Id == "save").Select(item => item.Title),
				Is.EqualTo(new[] { "Common save", "Pipe save" }));
			Assert.That(bar.ToolBarItems.Select(item => item.Title), Is.EqualTo(new[] { "Common tool", "Common save", "Pipe calculator", "Pipe save" }));
		});
	}

	[Test]
	public async Task NestedScreenGroupOverridesInPlaceAndHiddenItemsStayHiddenAcrossScopeChanges() {
		await using var provider = Services().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var host = provider.GetRequiredService<IBlazorApplicationScreenHost>();
		var application = ConfigureApplication(provider);
		await renderer.Dispatcher.InvokeAsync(async () => {
			await host.ActivateScreenAsync("pipes", "pipe-screen");
			var rendered = await renderer.RenderComponentAsync<ApplicationShell>();
			var bar = provider.GetRequiredService<CommandCapture>().Bar;
			var file = bar.Menus.Single(menu => menu.Id == "file");
			var export = (BlazorApplicationMenuGroup)file.Items.Single(item => item.Id == "export" && item.Title == "Pipe export");
			Assert.That(file.Items.Where(item => item.Id == "export").Select(item => item.Title), Is.EqualTo(new[] { "Common export", "Pipe export" }));
			Assert.That(export.Title, Is.EqualTo("Pipe export"));
			Assert.That(export.Items.Single().Id, Is.EqualTo("formats"));
			Assert.That(((BlazorApplicationMenuGroup)application.Menus.Single(menu => menu.Id == "file").Items.Single(item => item.Id == "export")).Items.Single().Id,
				Is.EqualTo("common-report"), "Merging a narrower group must not mutate the application definition.");

			await DispatchAsync(bar, "ToggleMenu", "menu:file");
			await DispatchAsync(bar, "ToggleSubmenu", "menu:file/context%3Aexport");
			await DispatchAsync(bar, "ToggleSubmenu", "menu:file/context%3Aexport/context%3Aformats");
			Assert.That(Header(rendered.ToHtmlString()), Does.Contain("Route CSV").And.Not.Contain("Internal export").And.Not.Contain("Hidden pipe registration"));
			Assert.That(export.Items.OfType<BlazorApplicationMenuGroup>().Single().Items, Has.Length.EqualTo(2), "Visibility must not remove registered children.");

			await host.ActivateScreenAsync("administration", "admin-screen");
			Assert.That(Header(rendered.ToHtmlString()), Does.Contain("Common export").And.Not.Contain("Pipe export").And.Not.Contain("Route CSV"));
			var restored = (BlazorApplicationMenuGroup)bar.Menus.Single(menu => menu.Id == "file").Items.Single(item => item.Id == "export");
			Assert.That(restored.Items.Single().Id, Is.EqualTo("common-report"));
		});
	}

	[TestCase("administration", true)]
	[TestCase("administration", false)]
	[TestCase("actions", true)]
	[TestCase("actions", false)]
	public async Task ActivatingDockSelectionUsesGuardsAndUpdatesCommandScopeDrawerAndLocation(string blockId, bool allowNavigation) {
		await using var provider = Services().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var host = provider.GetRequiredService<IBlazorApplicationScreenHost>();
		var navigation = (ShellNavigation)provider.GetRequiredService<NavigationManager>();
		ConfigureApplication(provider);
		await renderer.Dispatcher.InvokeAsync(async () => {
			var pipe = await host.ActivateScreenAsync("pipes", "pipe-screen");
			var rendered = await renderer.RenderComponentAsync<ApplicationShell>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(ApplicationShell.ActivateBlockOnSelection)] = true
			}));
			var capture = provider.GetRequiredService<CommandCapture>();
			var screen = (PipeScreen)pipe.Screen;
			screen.AllowNavigation = allowNavigation;
			typeof(ApplicationShell).GetMethod("SynchronizeLocation", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(capture.Shell, new object[] { false });
			var originalUri = navigation.Uri;
			var originalNavigationCount = navigation.NavigationCount;
			typeof(ApplicationShell).GetMethod("ToggleNavigation", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(capture.Shell, null);

			await SelectBlockAsync(capture.Shell, host.Blocks.Single(block => block.Id == blockId));

			Assert.That(screen.GuardCalls, Is.EqualTo(1), "Dock activation must use the existing leave guard exactly once.");
			Assert.That(host.ActiveBlock.Id, Is.EqualTo(allowNavigation ? blockId : "pipes"));
			Assert.That((bool)typeof(ApplicationShell).GetField("_navigationOpen", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(capture.Shell),
				Is.EqualTo(!allowNavigation), "Only a successful activation closes the navigation drawer.");
			Assert.That(navigation.NavigationCount, Is.EqualTo(originalNavigationCount + (allowNavigation ? 1 : 0)));
			Assert.That(host.Screens.Any(session => session.Id == pipe.Id), Is.True, "Switching blocks retains the original editor.");
			if (!allowNavigation) {
				Assert.That(host.ActiveScreen, Is.SameAs(pipe));
				Assert.That(navigation.Uri, Is.EqualTo(originalUri));
				Assert.That(Header(rendered.ToHtmlString()), Does.Contain("Pipe calculator").And.Contain("Pipe save"));
				return;
			}

			Assert.That(navigation.ReplacedLastEntry, Is.False);
			Assert.That(Header(rendered.ToHtmlString()), Does.Not.Contain("Pipe calculator").And.Not.Contain("Pipe save").And.Contain("Common save"));
			if (blockId == "actions") {
				Assert.That(host.ActiveScreen, Is.Null);
				Assert.That(navigation.Uri, Is.EqualTo("http://localhost/application?block=actions"));
				Assert.That(Header(rendered.ToHtmlString()), Does.Contain("Action tool"));
			} else {
				Assert.That(host.ActiveScreen.ScreenType, Is.EqualTo(typeof(AdministrationScreen)));
				Assert.That(navigation.Uri, Is.EqualTo($"http://localhost/application?block=administration&screen=admin-screen&instance={host.ActiveScreen.Id:D}"));
				Assert.That(Header(rendered.ToHtmlString()), Does.Contain("Administration tool").And.Contain("Reports"));
			}
		});
	}

	[Test]
	public async Task DockSelectionDefaultsToBrowsingWithoutGuardsOrChangingScreenCommandsAndLocation() {
		await using var provider = Services().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var host = provider.GetRequiredService<IBlazorApplicationScreenHost>();
		var navigation = (ShellNavigation)provider.GetRequiredService<NavigationManager>();
		ConfigureApplication(provider);
		await renderer.Dispatcher.InvokeAsync(async () => {
			var pipe = await host.ActivateScreenAsync("pipes", "pipe-screen");
			var rendered = await renderer.RenderComponentAsync<ApplicationShell>();
			var shell = provider.GetRequiredService<CommandCapture>().Shell;
			var screen = (PipeScreen)pipe.Screen;
			screen.AllowNavigation = false;
			typeof(ApplicationShell).GetMethod("ToggleNavigation", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(shell, null);

			await SelectBlockAsync(shell, host.Blocks.Single(block => block.Id == "administration"));

			Assert.That(shell.ActivateBlockOnSelection, Is.False);
			Assert.That(screen.GuardCalls, Is.Zero);
			Assert.That(host.ActiveBlock.Id, Is.EqualTo("administration"));
			Assert.That(host.ActiveScreen, Is.SameAs(pipe));
			Assert.That(navigation.NavigationCount, Is.Zero);
			Assert.That(navigation.Uri, Is.EqualTo("http://localhost/application"));
			Assert.That((bool)typeof(ApplicationShell).GetField("_navigationOpen", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(shell), Is.True);
			Assert.That(Header(rendered.ToHtmlString()), Does.Contain("Pipe calculator").And.Contain("Pipe save").And.Not.Contain("Administration tool"));
		});
	}
	private static IServiceCollection Services() {
		var services = new ServiceCollection().AddLogging().AddSphere10Blazor();
		services.AddSingleton<NavigationManager, ShellNavigation>();
		services.AddSingleton<IJSRuntime, GalleryRenderingTests.TestJsRuntime>();
		services.AddSingleton<CommandCapture>();
		services.AddSingleton<IComponentActivator>(provider => provider.GetRequiredService<CommandCapture>());
		services
			.AddApplicationBlock(block => block
				.WithId("pipes")
				.WithName("Pipe block")
				.ConfigureMenu(menu => menu
					.WithId("file")
					.WithText("File")
					.ConfigureItem(item => item
						.WithId("pipe-screen")
						.WithText("Hidden pipe registration")
						.WithScreen<PipeScreen>()
						.AsSingleInstance()
						.WithVisibility(false)
					)
					.AddItem(Command("pipe-file", "Pipe command"))
					.AddItem(Group("export", "Block export", Command("block-export", "Block output")))
				)
				.ConfigureMenu(menu => menu
					.WithId("tools")
					.WithText("Tools")
					.AddItem(Command("calculator", "Pipe calculator"))
				)
				.ConfigureMenu(menu => menu
					.WithId("internal")
					.WithText("Internal commands")
					.AddItem(Command("internal", "Internal command", false))
				)
				.AddToolBarItem(Command("calculator", "Pipe calculator"))
			)
			.AddApplicationBlock(block => block
				.WithId("administration")
				.WithName("Administration")
				.ConfigureMenu(menu => menu
					.WithId("file")
					.WithText("File")
					.ConfigureItem(item => item
						.WithId("admin-screen")
						.WithText("Hidden administration registration")
						.WithScreen<AdministrationScreen>()
						.AsSingleInstance()
						.AsDefault()
						.WithVisibility(false)
					)
					.AddItem(Command("admin-file", "Administration command"))
				)
				.ConfigureMenu(menu => menu
					.WithId("reports")
					.WithText("Reports")
					.AddItem(Command("audit", "Access audit"))
				)
				.AddToolBarItem(Command("administration-tool", "Administration tool"))
			)
			.AddApplicationBlock(block => block
				.WithId("actions")
				.WithName("Action block")
				.ConfigureMenu(menu => menu
					.WithId("actions")
					.WithText("Actions")
					.AddItem(Command("action", "Run action"))
				)
				.AddToolBarItem(Command("action-tool", "Action tool"))
			);
		return services;
	}

	private static BlazorApplication ConfigureApplication(IServiceProvider provider) {
		var application = (BlazorApplication)provider.GetRequiredService<IBlazorApplication>();
		var save = Command("save", "Common save");
		application.SetMenus(new[] {
			Menu("file", "File", Command("common-open", "Common open"), save, Group("export", "Common export", Command("common-report", "Common report"))),
			Menu("help", "Help", Command("about", "About"))
		});
		application.SetToolBarItems(new[] { Command("common-tool", "Common tool"), save });
		return application;
	}

	private static IBlazorApplicationMenuItem Command(string id, string title, bool visible = true) =>
		new BlazorApplicationMenuItemBuilder()
			.WithId(id)
			.WithText(title)
			.WithVisibility(visible)
			.WithAction((_, _) => Task.CompletedTask)
			.Build();

	private static IBlazorApplicationMenuItem Group(string id, string title, params IBlazorApplicationMenuItem[] children) =>
		new BlazorApplicationMenuItemBuilder()
			.WithId(id)
			.WithText(title)
			.WithChildren(children)
			.Build();

	private static IBlazorApplicationMenu Menu(string id, string title, params IBlazorApplicationMenuItem[] items) =>
		new BlazorApplicationMenu { Id = id, Text = title, Items = items };

	private static string Header(string html) => Regex.Match(html, "<header\\b.*?</header>", RegexOptions.Singleline).Value;

	private static Task DispatchAsync(ApplicationCommandBar bar, string method, string key) =>
		((IHandleEvent)bar).HandleEventAsync(new EventCallbackWorkItem((Action)(() =>
			typeof(ApplicationCommandBar).GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(bar, new object[] { key }))), null);

	private static Task SelectBlockAsync(ApplicationShell shell, IBlazorApplicationBlock block) =>
		((IHandleEvent)shell).HandleEventAsync(new EventCallbackWorkItem((Func<Task>)(() =>
			(Task)typeof(ApplicationShell).GetMethod("SelectBlockAsync", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(shell, new object[] { block }))), null);

	private sealed class ShellNavigation : NavigationManager {
		public ShellNavigation() => Initialize("http://localhost/", "http://localhost/application");

		public int NavigationCount { get; private set; }

		public bool ReplacedLastEntry { get; private set; }

		protected override void NavigateToCore(string uri, NavigationOptions options) {
			Uri = ToAbsoluteUri(uri).AbsoluteUri;
			ReplacedLastEntry = options.ReplaceHistoryEntry;
			NavigationCount++;
		}

		protected override void SetNavigationLockState(bool value) {
		}
	}

	private sealed class CommandCapture(IServiceProvider provider) : IComponentActivator {
		public ApplicationShell Shell { get; private set; }

		public ApplicationCommandBar Bar { get; private set; }

		public IComponent CreateInstance(Type componentType) {
			var component = (IComponent)ActivatorUtilities.CreateInstance(provider, componentType);
			if (component is ApplicationCommandBar bar)
				Bar = bar;
			if (component is ApplicationShell shell)
				Shell = shell;
			return component;
		}
	}

	public class PipeScreen : BlazorApplicationScreen {
		public bool AllowNavigation { get; set; } = true;

		public int GuardCalls { get; private set; }

		public override IBlazorApplicationMenu[] Menus => new[] {
			Menu("file", "File", Command("save", "Pipe save"),
				Group("export", "Pipe export", Group("formats", "Formats", Command("route", "Route CSV"), Command("internal-export", "Internal export", false)))),
			Menu("inspect", "Inspect", Command("inspect-pipe", "Inspect pipe"))
		};

		public override IBlazorApplicationMenuItem[] ToolBarItems => new[] { Command("save", "Pipe save") };

		protected override Task<bool> CanDeactivateAsync(CancellationToken cancellationToken) {
			GuardCalls++;
			return Task.FromResult(AllowNavigation);
		}

		protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, "Pipe screen");
	}

	public class AdministrationScreen : BlazorApplicationScreen {
		protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, "Administration screen");
	}
}
