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
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Web.AspNetCore.Blazor.Tests.Loader;
using Sphere10.Framework.Web.AspNetCore.Blazor.UI.Application;
using BlazorUI = global::Sphere10.Framework.Web.AspNetCore.Blazor;
using SharedUI = global::Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationCommandRenderingTests {
	[Test]
	public async Task ApplicationCommandsStayVisibleAlongsideScreenCommandsWithTheSameId() {
		await using var provider = Services().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var host = provider.GetRequiredService<BlazorUI.IApplicationScreenHost>();
		var hostUserInterface = provider.GetRequiredService<IBlazorUserInterfaceServices>();
		var application = (BlazorUI.Application)provider.GetRequiredService<BlazorUI.IApplication>();
		var shared = provider.GetRequiredService<SharedUI.IApplication>();
		var calls = 0;
		var applicationSave = new BlazorActionMenuItem { Id = "save", Title = "Application save", Action = () => calls++ };
		application.SetMenus(new[] { new ApplicationMenu { Id = "work", Text = "Work", Items = new[] { applicationSave } } });
		application.SetToolBarItems(new[] { applicationSave });
		Assert.That(shared.Menus.Single(), Is.SameAs(application.Menus.Single()));
		Assert.That(shared.ToolBarItems.Single(), Is.SameAs(application.ToolBarItems.Single()));
		await renderer.Dispatcher.InvokeAsync(async () => {
			var first = await host.ActivateScreenAsync("test", "one");
			var rendered = await renderer.RenderComponentAsync<ApplicationShell>();
			var screen = (CommandScreen)first.Screen;
			Assert.That(Regex.Matches(rendered.ToHtmlString(), "Screen save").Count, Is.EqualTo(2));
			Assert.That(Regex.Matches(rendered.ToHtmlString(), "Application save").Count, Is.EqualTo(2));
			// The web menu and toolbar execute the descriptors exposed by the shared application provider.
			await hostUserInterface.ExecuteMenuItemAsync((BlazorUI.IApplicationMenuItem)shared.Menus.Single().Items.Single());
			Assert.That(calls, Is.EqualTo(1));
			Assert.That(screen.Saves, Is.Zero);
			await hostUserInterface.ExecuteMenuItemAsync((BlazorUI.IApplicationMenuItem)shared.ToolBarItems.Single());
			Assert.That(calls, Is.EqualTo(2));
			Assert.That(screen.Saves, Is.Zero);
			await hostUserInterface.ExecuteMenuItemAsync(screen.ToolBarItems.Single());
			Assert.That(screen.Saves, Is.EqualTo(1));
			Assert.That(calls, Is.EqualTo(2));
			await host.ActivateScreenAsync("test", "two");
			Assert.That(Regex.Matches(rendered.ToHtmlString(), "Application save").Count, Is.EqualTo(2));
			Assert.That(rendered.ToHtmlString(), Does.Not.Contain("Screen save"));
			await host.ShowScreenAsync(first.Id);
			await hostUserInterface.ExecuteMenuItemAsync(screen.ToolBarItems.Single());
			Assert.That(screen.Saves, Is.EqualTo(2), "Returning to the screen must not duplicate command subscriptions.");
			Assert.That(first.Screen, Is.SameAs(screen));
		});
	}

	[Test]
	public async Task ApplicationCommandExecutesWhenThereIsNoActiveBlockOrScreen() {
		var services = new ServiceCollection().AddLogging().AddSphere10Blazor();
		services.AddSingleton<NavigationManager>(new TestNavigationManager("http://localhost/", "http://localhost/application"));
		services.AddSingleton<IJSRuntime, GalleryRenderingTests.TestJsRuntime>();
		await using var provider = services.BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var application = (BlazorUI.Application)provider.GetRequiredService<BlazorUI.IApplication>();
		var host = provider.GetRequiredService<BlazorUI.IApplicationScreenHost>();
		var calls = 0;
		application.SetToolBarItems(new[] { new BlazorActionMenuItem { Id = "run", Title = "Run application command", Action = () => calls++ } });
		await renderer.Dispatcher.InvokeAsync(async () => {
			var rendered = await renderer.RenderComponentAsync<ApplicationShell>();
			Assert.That(rendered.ToHtmlString(), Does.Contain("Run application command"));
			Assert.That(host.SelectedBlock, Is.Null);
			Assert.That(host.ActiveScreen, Is.Null);
			using var shell = new ApplicationShell { Application = application, ScreenHost = host, Navigation = provider.GetRequiredService<NavigationManager>() };
			await shell.ExecuteCommandAsync(application.ToolBarItems.Single());
			Assert.That(calls, Is.EqualTo(1));
		});
	}

	[Test]
	public async Task DuplicateApplicationAndScreenGroupsKeepIndependentRenderIdentityAndActions() {
		await using var provider = Services().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var application = (BlazorUI.Application)provider.GetRequiredService<BlazorUI.IApplication>();
		var host = provider.GetRequiredService<BlazorUI.IApplicationScreenHost>();
		var calls = 0;
		var action = new BlazorActionMenuItem { Id = "save", Title = "Application child", Action = () => calls++ };
		application.SetToolBarItems(new[] { new ApplicationMenuGroup { Id = "group", Title = "Application group", Items = new[] { action } } });
		await renderer.Dispatcher.InvokeAsync(async () => {
			var session = await host.ActivateScreenAsync("test", "one");
			var rendered = await renderer.RenderComponentAsync<ApplicationShell>();
			var screen = (CommandScreen)session.Screen;
			screen.UseCommandGroup = true;
			host.NotifyScreenChanged(session);
			Assert.That(rendered.ToHtmlString(), Does.Contain("Application group").And.Contain("Screen group"));
			var appGroup = (ApplicationMenuGroup)application.ToolBarItems.Single();
			var screenGroup = (ApplicationMenuGroup)screen.ToolBarItems.Single();
			await host.UserInterfaceServices.ExecuteMenuItemAsync(appGroup.Items.Single());
			await host.UserInterfaceServices.ExecuteMenuItemAsync(screenGroup.Items.Single());
			Assert.That(calls, Is.EqualTo(1));
			Assert.That(screen.Saves, Is.EqualTo(1));
		});
	}

	[Test]
	public async Task BrowsingAnotherBlockKeepsCommandsBoundToTheActiveScreenUntilItCloses() {
		var services = Services();
		services
			.AddApplicationBlock(block => block
				.WithId("browse")
				.WithName("Browse")
				.AddMenu(menu => menu
					.WithText("Browse menu")
					.AddActionItem("browse-action", "Browse action", () => { })
				)
				.AddToolBarItem(item => item
					.WithId("browse-tool")
					.WithText("Browse tool")
					.WithAction((_, _) => Task.CompletedTask)
				)
			);
		await using var provider = services.BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var host = provider.GetRequiredService<BlazorUI.IApplicationScreenHost>();
		await renderer.Dispatcher.InvokeAsync(async () => {
			var session = await host.ActivateScreenAsync("test", "one");
			var rendered = await renderer.RenderComponentAsync<ApplicationShell>();
			var original = session.Screen;
			await host.SelectBlockAsync("browse");
			var html = rendered.ToHtmlString();
			var chrome = Regex.Match(html, "<header\\b.*?</header>", RegexOptions.Singleline).Value;
			Assert.That(chrome, Does.Contain("Work").And.Contain("Screen save").And.Not.Contain("Browse menu").And.Not.Contain("Browse tool"));
			Assert.That(html, Does.Contain("Browse action"), "The browsed block's sidebar remains usable.");
			Assert.That(host.ActiveScreen, Is.SameAs(session));
			Assert.That(session.Screen, Is.SameAs(original));
			await host.CloseScreenAsync(session.Id);
			chrome = Regex.Match(rendered.ToHtmlString(), "<header\\b.*?</header>", RegexOptions.Singleline).Value;
			Assert.That(chrome, Does.Not.Contain("Browse menu").And.Not.Contain("Browse tool").And.Not.Contain("Screen save"));
		});
	}

	/// <summary>The shell composes the visible plugin path and removes all contextual commands when the last screen closes.</summary>
	[Test]
	public async Task PluginCommandsFollowTheVisibleScreenOwnersAndDisappearWithTheScreen() {
		var services = Services();
		var blockCalls = 0;
		var block = new ApplicationBlockBuilder()
			.WithId("plugin")
			.WithName("Plugin block")
			.AddScreen<CommandScreen>("Plugin screen")
			.AddMenu(menu => menu
				.WithId("work")
				.WithText("Work")
				.AddActionItem("save", "Block save", () => blockCalls++)
				.AddActionItem("block", "Block command", () => blockCalls++)
			)
			.AddToolBarItem(item => item
				.WithId("save")
				.WithText("Block save")
				.WithAction((_, _) => Task.CompletedTask)
			)
			.AddToolBarItem(item => item
				.WithId("block")
				.WithText("Block command")
				.WithAction((_, _) => Task.CompletedTask)
			)
			.Build();
		var plugin = new CommandPlugin(block);
		services.AddSphere10BlazorPlugin(plugin);
		await using var provider = services.BuildServiceProvider();
		var application = (BlazorUI.Application)provider.GetRequiredService<BlazorUI.IApplication>();
		var host = application.ScreenHost;
		var applicationCalls = 0;
		var applicationSave = new BlazorActionMenuItem { Id = "save", Title = "Application save", Action = () => applicationCalls++ };
		application.SetMenus(new[] { new ApplicationMenu { Id = "work", Text = "Work", Items = new[] { applicationSave } } });
		application.SetToolBarItems(new[] { applicationSave });
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			var actual = host.Blocks.Single(candidate => candidate.Id == "plugin");
			var session = await host.OpenScreenAsync(actual, actual.ScreenDefinitions.Single());
			var rendered = await renderer.RenderComponentAsync<ApplicationShell>();
			await host.SelectBlockAsync("empty");
			var header = Regex.Match(rendered.ToHtmlString(), "<header\\b.*?</header>", RegexOptions.Singleline).Value;
			Assert.That(Regex.Matches(header, "Application save").Count, Is.EqualTo(2));
			Assert.That(Regex.Matches(header, "Plugin command").Count, Is.EqualTo(2));
			Assert.That(Regex.Matches(header, "Block command").Count, Is.EqualTo(2));
			Assert.That(Regex.Matches(header, "Screen save").Count, Is.EqualTo(2));
			Assert.That(header, Does.Not.Contain("Plugin save").And.Not.Contain("Block save"));
			Assert.That(actual.Source, Is.SameAs(plugin));

			// Composition retains the real descriptor and callback rather than creating a second command owner.
			await host.UserInterfaceServices.ExecuteMenuItemAsync((BlazorUI.IApplicationMenuItem)plugin.Menus.Single().Items.Last());
			Assert.That(plugin.Calls, Is.EqualTo(1));
			Assert.That(blockCalls, Is.Zero);
			Assert.That(applicationCalls, Is.Zero);
			await host.CloseScreenAsync(session.Id);
			header = Regex.Match(rendered.ToHtmlString(), "<header\\b.*?</header>", RegexOptions.Singleline).Value;
			Assert.That(header, Does.Contain("Application save").And.Not.Contain("Plugin command").And.Not.Contain("Block command").And.Not.Contain("Screen save"));
		});
	}

	[Test]
	public async Task SingleViewKeepsHiddenSingletonMountedAndDisposesMultiInstanceOnDeparture() {
		await using var provider = Services().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var host = provider.GetRequiredService<BlazorUI.IApplicationScreenHost>();
		await renderer.Dispatcher.InvokeAsync(async () => {
			await host.TrySetScreenModeAsync(ScreenMode.SingleView);
			var first = await host.ActivateScreenAsync("test", "one");
			var rendered = await renderer.RenderComponentAsync<ApplicationShell>();
			var original = (CommandScreen)first.Screen;
			var second = await host.ActivateScreenAsync("test", "two");
			await rendered.QuiescenceTask;
			var temporary = await provider.GetRequiredService<ScreenRenderState>().TemporaryAttached.Task.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.That(original.Disposals, Is.Zero);
			await host.ShowScreenAsync(first.Id);
			Assert.That(first.Screen, Is.SameAs(original));
			Assert.That(temporary.Disposals, Is.EqualTo(1));
			await host.ActivateBlockAsync("empty");
			Assert.That(host.OpenScreens, Is.Empty);
			Assert.That(original.Disposals, Is.Zero, "An empty presentation must not unmount retained singleton components.");
			await host.ActivateScreenAsync("test", "one");
			Assert.That(first.Screen, Is.SameAs(original));
		});
	}

	[Test]
	public async Task TabsExposeSelectionPanelsFullTitlesAndDirtyState() {
		await using var provider = Services().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var host = provider.GetRequiredService<BlazorUI.IApplicationScreenHost>();
		await renderer.Dispatcher.InvokeAsync(async () => {
			var first = await host.ActivateScreenAsync("test", "one");
			var rendered = await renderer.RenderComponentAsync<ApplicationShell>();
			Assert.That(rendered.ToHtmlString(), Does.Contain("role=\"tablist\"").And.Contain("role=\"tab\"").And.Contain("role=\"tabpanel\""));
			Assert.That(rendered.ToHtmlString(), Does.Contain("aria-selected=\"true\"").And.Contain($"id=\"tab-{first.Id:N}\""));
			await host.ActivateScreenAsync("test", "two");
			await host.MoveScreenAsync(first.Id, 1);
			Assert.That(host.OpenScreens[1], Is.SameAs(first));
			Assert.That(rendered.ToHtmlString(), Does.Contain("aria-selected=\"false\""));
		});
	}

	[Test]
	public async Task RepeatedCloseDuringAnAsyncGuardIsCoalesced() {
		await using var provider = Services().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var host = provider.GetRequiredService<BlazorUI.IApplicationScreenHost>();
		await renderer.Dispatcher.InvokeAsync(async () => {
			var session = await host.ActivateScreenAsync("test", "one");
			var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			using var unblock = Tools.Scope.ExecuteOnDispose(() => release.TrySetResult());
			var calls = 0;
			ApplicationScreenTabs tabs = null;
			Func<Guid[], Task> onClose = async ids => {
				calls++;
				entered.TrySetResult();
				await release.Task;
				await host.CloseScreensAsync(ids);
			};
			await renderer.RenderComponentAsync<TabsHarness>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(TabsHarness.Session)] = session,
				[nameof(TabsHarness.Capture)] = (Action<ApplicationScreenTabs>)(instance => tabs = instance),
				[nameof(TabsHarness.Close)] = EventCallback.Factory.Create(new object(), onClose)
			}));
			var close = typeof(ApplicationScreenTabs).GetMethod("CloseAsync", BindingFlags.Instance | BindingFlags.NonPublic);
			var first = (Task)close.Invoke(tabs, new object[] { new[] { session.Id } });
			await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
			var repeated = (Task)close.Invoke(tabs, new object[] { new[] { session.Id } });
			Assert.That(repeated.IsCompleted, Is.True);
			Assert.That(calls, Is.EqualTo(1));
			release.SetResult();
			await first;
			Assert.That(host.Screens, Is.Empty);
		});
	}

	private static IServiceCollection Services() {
		var services = new ServiceCollection().AddLogging().AddSphere10Blazor();
		services.AddSingleton<NavigationManager>(new TestNavigationManager("http://localhost/", "http://localhost/application"));
		services.AddSingleton<IJSRuntime, GalleryRenderingTests.TestJsRuntime>();
		services.AddScoped<ScreenRenderState>();
		services
			.AddApplicationBlock(block => block
				.WithId("test")
				.WithName("Test")
				.AddScreen<TemporaryScreen>(screen => screen
					.WithTitle("Two")
					.WithActivationMode(ScreenActivationMode.MultiInstance)
				)
				.AddScreen<CommandScreen>(screen => screen
					.WithTitle("One")
					.WithActivationMode(ScreenActivationMode.SingleInstance)
				)
				.AddMenu(menu => menu
					.WithId("work")
					.WithText("Work")
					.AddShowScreenItem<CommandScreen>(text: "One", id: "one")
					.AddShowScreenItem<TemporaryScreen>(text: "Two", id: "two")
				)
			);
		services
			.AddApplicationBlock(block => block
				.WithId("empty")
				.WithName("Empty")
			);
		return services;
	}

	#region Inner Types

	/// <summary>Contributes real plugin commands through the same contract as application, block and screen owners.</summary>
	private sealed class CommandPlugin : ApplicationPluginBase<BlazorUI.IApplicationBlock>, IBlazorPlugin {
		/// <summary>Captures fixed plugin membership and command descriptors before activation.</summary>
		public CommandPlugin(BlazorUI.IApplicationBlock block)
			: base("Command plugin", new[] { block }) {
			var save = new BlazorActionMenuItem { Id = "save", Title = "Plugin save", Action = () => Calls++ };
			var command = new BlazorActionMenuItem { Id = "plugin", Title = "Plugin command", Action = () => Calls++ };
			Menus = new[] { new ApplicationMenu { Id = "work", Text = "Work", Items = new[] { save, command } } };
			ToolBarItems = new[] { save, command };
		}

		/// <summary>The exact plugin menu descriptors used by the shell.</summary>
		public SharedUI.IApplicationMenu[] Menus { get; }

		/// <summary>The exact plugin toolbar descriptors used by the shell.</summary>
		public SharedUI.IApplicationMenuItem[] ToolBarItems { get; }

		/// <summary>The observed execution count for the existing plugin callbacks.</summary>
		public int Calls { get; private set; }

		/// <summary>Plugins borrow the executing host's services instead of constructing another container.</summary>
		public IServiceProvider IoCContainer {
			get {
				return null;
			}
		}
	}

	public class CommandScreen : ApplicationScreen {
		private readonly BlazorActionMenuItem _save;
		private readonly ApplicationMenuGroup _group;
		private readonly ApplicationMenu _menu;

		public CommandScreen() {
			_save = new BlazorActionMenuItem { Id = "save", Title = "Screen save", Action = () => Saves++ };
			_group = new ApplicationMenuGroup { Id = "group", Title = "Screen group", Items = new[] { _save } };
			_menu = new ApplicationMenu { Id = "work", Text = "Work", Items = new[] { _save } };
		}

		public int Saves { get; private set; }

		public int Disposals { get; private set; }

		public bool UseCommandGroup { get; set; }

		public override BlazorUI.IApplicationMenu[] Menus => new[] { _menu };

		public override BlazorUI.IApplicationMenuItem[] ToolBarItems => UseCommandGroup ? new BlazorUI.IApplicationMenuItem[] { _group } : new[] { _save };

		protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, "Command screen");

		protected override ValueTask DisposeAsyncCore() {
			Disposals++;
			return ValueTask.CompletedTask;
		}
	}

	public class TabsHarness : ComponentBase {
		[Parameter] public ApplicationScreenSession Session { get; set; }

		[Parameter] public Action<ApplicationScreenTabs> Capture { get; set; }

		[Parameter] public EventCallback<Guid[]> Close { get; set; }

		protected override void BuildRenderTree(RenderTreeBuilder builder) {
			builder.OpenComponent<ApplicationScreenTabs>(0);
			builder.AddAttribute(1, nameof(ApplicationScreenTabs.Sessions), new[] { Session });
			builder.AddAttribute(2, nameof(ApplicationScreenTabs.ActiveSession), Session);
			builder.AddAttribute(3, nameof(ApplicationScreenTabs.OnClose), Close);
			builder.AddComponentReferenceCapture(4, component => Capture((ApplicationScreenTabs)component));
			builder.CloseComponent();
		}
	}

	public class ScreenRenderState {
		public TaskCompletionSource<TemporaryScreen> TemporaryAttached { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
	}

	public class TemporaryScreen : ApplicationScreen {
		[Inject] public ScreenRenderState State { get; set; }

		protected override async Task OnInitializedAsync() {
			await base.OnInitializedAsync();
			State.TemporaryAttached.TrySetResult(this);
		}
		public int Disposals { get; private set; }

		protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, "Temporary screen");

		protected override ValueTask DisposeAsyncCore() {
			Disposals++;
			return ValueTask.CompletedTask;
		}
	}

	#endregion
}
