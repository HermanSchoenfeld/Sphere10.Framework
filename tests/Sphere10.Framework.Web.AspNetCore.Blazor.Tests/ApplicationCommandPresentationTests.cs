// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.Web.HtmlRendering;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Web.AspNetCore.Blazor.Tests.Loader;
using Sphere10.Framework.Web.AspNetCore.Blazor.UI.Application;
using Sphere10.Framework.Web.AspNetCore.Blazor.UI.MainFrame;
using BlazorUI = global::Sphere10.Framework.Web.AspNetCore.Blazor;
using CommandBarHarness = Sphere10.Framework.Web.AspNetCore.Blazor.Tests.ApplicationCommandStateTests.CommandBarHarness;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

/// <summary>Verifies power-user command presentation: icon-only toolbars, the standard File menu and screen command state that follows each render.</summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationCommandPresentationTests {

	[Test]
	public async Task IconToolbarCommandsShowOnlyTheirIconWithTheCaptionAsTooltipWhileMenusKeepTheCaption() {
		var enabled = false;
		var save = new ApplicationMenuItemBuilder()
			.WithId("save")
			.WithText("Save draft")
			.WithIcon("fas fa-save")
			.EnabledWhen(() => enabled)
			.WithAction((_, _) => Task.CompletedTask)
			.Build();
		var menu = new ApplicationMenuBuilder()
			.WithId("work")
			.WithText("Work")
			.AddItem(save)
			.Build();
		await RenderBarAsync(new[] { menu }, new[] { save }, async (bar, rendered) => {
			var toolbar = Toolbar(rendered.ToHtmlString());
			var button = Regex.Match(toolbar, "<button\\b(?<attributes>[^>]*)>(?<content>.*?)</button>", RegexOptions.Singleline);
			Assert.That(button.Groups["attributes"].Value, Does.Contain("class=\"sphere10-tool-icon\"").And.Contain("title=\"Save draft\"").And.Contain("aria-label=\"Save draft\""));
			Assert.That(button.Groups["attributes"].Value, Does.Contain("disabled"), "A disabled command is rendered disabled.");
			Assert.That(button.Groups["content"].Value, Does.Contain("class=\"fas fa-save\"").And.Not.Contain("Save draft"), "The caption is the tooltip, not visible text.");
			Toggle(bar, "menu:work");
			enabled = true;
			await bar.SetParametersAsync(ParameterView.Empty);
			var html = rendered.ToHtmlString();
			Assert.That(Regex.IsMatch(html, "role=\"group\" aria-label=\"Work\"[^>]*>.*?class=\"fas fa-save\" aria-hidden=\"true\"[^>]*></span>\\s*Save draft\\s*</button>", RegexOptions.Singleline), Is.True,
				"Menu items keep their icon and caption.");
			Assert.That(Regex.Match(Toolbar(html), "<button\\b[^>]*>").Value, Does.Not.Contain("disabled"));
		});
	}

	[Test]
	public async Task ToolbarTextIsShownForAnOptedInCommandAndForACommandWithoutIcon() {
		var labelled = new ApplicationMenuItemBuilder()
			.WithId("new")
			.WithText("New record")
			.WithIcon("fas fa-plus")
			.WithToolBarText()
			.WithAction((_, _) => Task.CompletedTask)
			.Build();
		var plain = new ApplicationMenuItemBuilder()
			.WithId("plain")
			.WithText("Plain command")
			.WithAction((_, _) => Task.CompletedTask)
			.Build();
		await RenderBarAsync(Array.Empty<BlazorUI.IApplicationMenu>(), new BlazorUI.IApplicationMenuItem[] { labelled, new BlazorMenuSeparator(), plain }, (_, rendered) => {
			var buttons = Regex.Matches(Toolbar(rendered.ToHtmlString()), "<button\\b(?<attributes>[^>]*)>(?<content>.*?)</button>", RegexOptions.Singleline);
			Assert.That(buttons, Has.Count.EqualTo(2));
			Assert.That(buttons[0].Groups["content"].Value, Does.Contain("class=\"fas fa-plus\"").And.Contain("New record"));
			Assert.That(buttons[0].Groups["attributes"].Value, Does.Not.Contain("aria-label").And.Not.Contain("sphere10-tool-icon"));
			Assert.That(buttons[1].Groups["content"].Value.Trim(), Is.EqualTo("Plain command"), "A command without an icon keeps its caption.");
			Assert.That(rendered.ToHtmlString(), Does.Contain("role=\"separator\" aria-orientation=\"vertical\""), "Separators group the toolbar.");
			return Task.CompletedTask;
		});
	}

	// The owner's applications caption every toolbar button (19 November 2026); a screen command shows its plain name while the
	// tooltip keeps the name with what blocks it.
	[Test]
	public async Task ApplicationToolBarTextCaptionsEveryIconCommandWithItsToolBarCaption() {
		var locked = new ApplicationMenuItemBuilder()
			.WithId("lock")
			.WithText("Lock Product (Product: Locked)")
			.WithToolBarCaption("Lock Product")
			.WithIcon("fas fa-lock")
			.WithAction((_, _) => Task.CompletedTask)
			.Build();
		var refresh = new ApplicationMenuItemBuilder()
			.WithId("refresh")
			.WithText("Refresh")
			.WithIcon("fas fa-sync")
			.WithAction((_, _) => Task.CompletedTask)
			.Build();
		await RenderBarAsync(Array.Empty<BlazorUI.IApplicationMenu>(), new BlazorUI.IApplicationMenuItem[] { locked, refresh }, (_, rendered) => {
			var buttons = Regex.Matches(Toolbar(rendered.ToHtmlString()), "<button\\b(?<attributes>[^>]*)>(?<content>.*?)</button>", RegexOptions.Singleline);
			Assert.That(buttons, Has.Count.EqualTo(2));
			Assert.That(buttons[0].Groups["attributes"].Value, Does.Contain("class=\"sphere10-tool-text\"").And.Contain("title=\"Lock Product (Product: Locked)\"").And.Not.Contain("aria-label"));
			Assert.That(buttons[0].Groups["content"].Value, Does.Contain("class=\"fas fa-lock\"").And.Contain("Lock Product").And.Not.Contain("Product: Locked"),
				"The button shows the plain caption; the tooltip names the blocker.");
			Assert.That(buttons[1].Groups["content"].Value, Does.Contain("class=\"fas fa-sync\"").And.Contain("Refresh"), "A command without its own caption shows its title.");
			return Task.CompletedTask;
		}, new BlazorUI.ApplicationOptions(toolBarText: true));
	}

	[Test]
	public void ToolbarTextChoiceSurvivesBlockRegistrationSnapshots() {
		var block = new ApplicationBlockBuilder()
			.WithId("records")
			.WithName("Records")
			.AddToolBarItem(item => item
				.WithId("labelled")
				.WithText("Labelled")
				.WithIcon("fas fa-tag")
				.WithToolBarText()
				.WithAction((_, _) => Task.CompletedTask)
			)
			.AddToolBarItem(item => item
				.WithId("icon")
				.WithText("Icon only")
				.WithIcon("fas fa-tag")
				.WithAction((_, _) => Task.CompletedTask)
			)
			.Build();
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorUI.ApplicationScreenHost(new[] { block }, provider);
		var items = host.Blocks.Single().ToolBarItems;
		Assert.That(items.Select(item => item.ShowToolBarText), Is.EqualTo(new[] { true, false }));
		Assert.That(items[0], Is.Not.SameAs(block.ToolBarItems[0]), "The circuit copy is a separate command.");
	}

	[Test]
	public async Task ScreenFileMenuExtendsTheStandardFileMenuInFirstPosition() {
		await RenderShellAsync(application => application.SetMenus(new[] { ApplicationMenu("view", "View", "Tabs") }), (shell, _, _) => {
			var menus = MergedMenus(shell);
			Assert.That(menus.Select(menu => menu.Id), Is.EqualTo(new[] { BlazorUI.Application.FileMenuId, "view", "records-product", "sphere10-screens-menu" }));
			Assert.That(menus[0].Text, Is.EqualTo("File"));
			Assert.That(menus[0].Items.Select(item => item.Title), Is.EqualTo(new[] { "Save draft" }));
			return Task.CompletedTask;
		});
	}

	[Test]
	public async Task ApplicationFileMenuIsPlacedFirstAndTheScreenExtendsIt() {
		await RenderShellAsync(application => application.SetMenus(new[] { ApplicationMenu("view", "View", "Tabs"), ApplicationMenu(BlazorUI.Application.FileMenuId, "File", "Exit") }),
			(shell, _, _) => {
				var menus = MergedMenus(shell);
				Assert.That(menus.Select(menu => menu.Id).Take(2), Is.EqualTo(new[] { BlazorUI.Application.FileMenuId, "view" }));
				Assert.That(menus[0].Items.Where(item => item is not IApplicationMenuSeparator).Select(item => item.Title), Is.EqualTo(new[] { "Exit", "Save draft" }),
					"Application commands come first; the screen's follow while it is active.");
				Assert.That(menus[0].Items, Has.Length.EqualTo(3));
				Assert.That(menus[0].Items[1], Is.InstanceOf<IApplicationMenuSeparator>(), "A separator divides the application's items from the screen's.");
				return Task.CompletedTask;
			});
	}

	[Test]
	public async Task StandardFileMenuIsOmittedWhenNoLayerContributesToIt() {
		await RenderShellAsync(application => application.SetMenus(new[] { ApplicationMenu("view", "View", "Tabs") }), (shell, _, _) => {
			var screen = (StateScreen)shell.ScreenHost.ActiveScreen.Screen;
			screen.ContributeMenus = false;
			Assert.That(MergedMenus(shell).Select(menu => menu.Id), Is.EqualTo(new[] { "view", "sphere10-screens-menu" }));
			return Task.CompletedTask;
		});
	}

	[Test]
	public async Task RenderingAScreenPublishesChangedCommandStateWithoutNotifyScreenChanged() {
		await RenderShellAsync(null, async (shell, host, rendered) => {
			var screen = (StateScreen)host.ActiveScreen.Screen;
			IHandleAfterRender afterRender = screen;
			var notifications = 0;
			host.Changed += () => notifications++;

			// The first render after attachment publishes once; an unchanged render publishes nothing, so no render loop can form.
			await afterRender.OnAfterRenderAsync();
			var published = notifications;
			Assert.That(published, Is.EqualTo(1));
			await afterRender.OnAfterRenderAsync();
			Assert.That(notifications, Is.EqualTo(published));
			Assert.That(SaveButton(rendered), Does.Contain("disabled"));

			// A local state change and render reach the shell's toolbar without the screen calling NotifyScreenChanged.
			await screen.AllowSaveAsync();
			await afterRender.OnAfterRenderAsync();
			Assert.That(notifications, Is.EqualTo(published + 1));
			await rendered.QuiescenceTask;
			Assert.That(SaveButton(rendered), Does.Not.Contain("disabled"));
			Assert.That(rendered.ToHtmlString(), Does.Contain("Ready"));
			await afterRender.OnAfterRenderAsync();
			Assert.That(notifications, Is.EqualTo(published + 1), "Rebuilt but equivalent commands are compared by value.");
		});
	}

	[Test]
	public async Task ScreenTitleChangePublishedByRenderUpdatesTheTab() {
		await RenderShellAsync(null, async (_, host, rendered) => {
			var screen = (StateScreen)host.ActiveScreen.Screen;
			IHandleAfterRender afterRender = screen;
			await afterRender.OnAfterRenderAsync();
			await screen.RenameAsync("Renamed records");
			await afterRender.OnAfterRenderAsync();
			await rendered.QuiescenceTask;
			Assert.That(rendered.ToHtmlString(), Does.Contain("Renamed records"));
		});
	}

	// Renders a command bar for the supplied menus and toolbar commands.
	private static async Task RenderBarAsync(BlazorUI.IApplicationMenu[] menus, BlazorUI.IApplicationMenuItem[] items, Func<ApplicationCommandBar, HtmlRootComponent, Task> assertions,
		BlazorUI.ApplicationOptions options = null) {
		var services = Services();
		if (options != null)
			services.AddSingleton(options);
		await using var provider = services.BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			ApplicationCommandBar bar = null;
			var rendered = await renderer.RenderComponentAsync<CommandBarHarness>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(CommandBarHarness.Menus)] = menus, [nameof(CommandBarHarness.Items)] = items,
				[nameof(CommandBarHarness.Capture)] = (Action<ApplicationCommandBar>)(component => bar = component)
			}));
			await assertions(bar, rendered);
		});
	}

	// Renders the shell over one block whose default screen contributes File and Product menus and a toolbar command.
	private static async Task RenderShellAsync(Action<BlazorUI.Application> configure, Func<ApplicationShell, BlazorUI.IApplicationScreenHost, HtmlRootComponent, Task> assertions) {
		var services = Services();
		services.AddApplicationBlock(block => block
			.WithId("records")
			.WithName("Records")
			.AddScreen<StateScreen>("Records", activationMode: ScreenActivationMode.SingleInstance)
			.WithDefaultScreen<StateScreen>()
		);
		if (configure != null)
			services.AddSphere10BlazorApplication(configure);
		await using var provider = services.BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var host = provider.GetRequiredService<BlazorUI.IApplicationScreenHost>();
		await renderer.Dispatcher.InvokeAsync(async () => {
			ComponentParameterLifecycleTests.ComponentHost harness = null;
			var rendered = await renderer.RenderComponentAsync<ComponentParameterLifecycleTests.ComponentHost>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(ComponentParameterLifecycleTests.ComponentHost.ComponentType)] = typeof(ApplicationShell),
				[nameof(ComponentParameterLifecycleTests.ComponentHost.Parameters)] = new Dictionary<string, object>(),
				[nameof(ComponentParameterLifecycleTests.ComponentHost.Capture)] = (Action<ComponentParameterLifecycleTests.ComponentHost>)(value => harness = value)
			}));
			await rendered.QuiescenceTask;
			Assert.That(host.ActiveScreen?.Screen, Is.TypeOf<StateScreen>());
			await assertions((ApplicationShell)harness.Component, host, rendered);
		});
	}

	private static IServiceCollection Services() {
		var services = new ServiceCollection().AddLogging().AddSphere10Blazor();
		services.AddSingleton<NavigationManager>(new TestNavigationManager("http://localhost/", "http://localhost/application"));
		services.AddSingleton<IJSRuntime, GalleryRenderingTests.TestJsRuntime>();
		return services;
	}

	private static BlazorUI.ApplicationMenu ApplicationMenu(string id, string text, string item) => new BlazorUI.ApplicationMenuBuilder()
		.WithId(id)
		.WithText(text)
		.AddItem(command => command
			.WithId(id + "-" + item.ToLowerInvariant())
			.WithText(item)
			.WithAction((_, _) => Task.CompletedTask)
		)
		.Build() as BlazorUI.ApplicationMenu;

	private static BlazorUI.IApplicationMenu[] MergedMenus(ApplicationShell shell) =>
		(BlazorUI.IApplicationMenu[])typeof(ApplicationShell).GetProperty("MergedMenus", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(shell);

	private static string Toolbar(string html) => Regex.Match(html, "role=\"toolbar\".*", RegexOptions.Singleline).Value;

	private static string SaveButton(HtmlRootComponent rendered) =>
		Regex.Match(Toolbar(rendered.ToHtmlString()), "<button\\b[^>]*title=\"Save draft\"[^>]*>").Value;

	private static void Toggle(ApplicationCommandBar bar, string key) =>
		typeof(ApplicationCommandBar).GetMethod("ToggleMenu", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(bar, new object[] { key });

	#region Inner Types

	/// <summary>A screen whose Save command follows local state; it rebuilds its menus on every read and never calls NotifyScreenChanged.</summary>
	public sealed class StateScreen : ApplicationScreen {
		private readonly BlazorUI.ApplicationMenuItemBase _save;
		private readonly BlazorUI.ApplicationMenuItemBase _open;
		private bool _canSave;
		private string _title = "Records";

		/// <summary>Creates the screen's commands.</summary>
		public StateScreen() {
			_save = new ApplicationMenuItemBuilder()
				.WithId("save")
				.WithText("Save draft")
				.WithIcon("fas fa-save")
				.EnabledWhen(() => _canSave)
				.WithAction((_, _) => Task.CompletedTask)
				.Build();
			_open = new ApplicationMenuItemBuilder()
				.WithId("open")
				.WithText("Open record")
				.WithIcon("fas fa-folder-open")
				.WithAction((_, _) => Task.CompletedTask)
				.Build();
		}

		/// <summary>Whether the screen contributes its File and Product menus.</summary>
		public bool ContributeMenus { get; set; } = true;

		/// <inheritdoc />
		public override string Title => _title;

		/// <inheritdoc />
		public override BlazorUI.IApplicationMenu[] Menus => !ContributeMenus ? Array.Empty<BlazorUI.IApplicationMenu>() : new BlazorUI.IApplicationMenu[] {
			new BlazorUI.ApplicationMenu(ownsItems: false) { Id = BlazorUI.Application.FileMenuId, Text = "File", Items = new BlazorUI.IApplicationMenuItem[] { _save } },
			new BlazorUI.ApplicationMenu(ownsItems: false) { Id = "records-product", Text = "Product", Items = new BlazorUI.IApplicationMenuItem[] { _open } }
		};

		/// <inheritdoc />
		public override BlazorUI.IApplicationMenuItem[] ToolBarItems => new BlazorUI.IApplicationMenuItem[] { _save, _open };

		/// <summary>Enables Save and renders, as a selection change in the screen would.</summary>
		public Task AllowSaveAsync() => InvokeAsync(() => {
			_canSave = true;
			StateHasChanged();
		});

		/// <summary>Changes the dynamic title and renders.</summary>
		public Task RenameAsync(string title) => InvokeAsync(() => {
			_title = title;
			StateHasChanged();
		});

		/// <inheritdoc />
		protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, _canSave ? "Ready" : "Draft");
	}

	#endregion
}
