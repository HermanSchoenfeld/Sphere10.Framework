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
using Sphere10.Framework.Web.AspNetCore.Blazor.UI.MainFrame;
using BlazorUI = global::Sphere10.Framework.Web.AspNetCore.Blazor;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationCommandStateTests {
	[Test]
	public async Task HiddenRegistrationStaysOutOfNavigationAndCommandsWhileKeyedActivationWorks() {
		var hidden = new ApplicationMenuItemBuilder()
			.WithId("project")
			.WithText("Hidden project launcher")
			.WithScreen<ProjectScreen>()
			.WithVisibility(false)
			.Build();
		var visible = new ApplicationMenuItemBuilder()
			.WithId("browse")
			.WithText("Browse projects")
			.WithAction((_, _) => Task.CompletedTask)
			.Build();
		var block = new ApplicationBlockBuilder()
			.WithId("projects")
			.WithName("Projects")
			.AddScreen<ProjectScreen>(screen => screen
				.WithTitle("Hidden project launcher")
				.AsMultiInstance()
			)
			.AddMenu(menu => menu
				.WithId("navigation")
				.WithText("Project navigation")
				.AddItem(hidden)
				.AddItem(visible)
			)
			.Build();
		await using var provider = Services().BuildServiceProvider();
		using var host = new BlazorUI.ApplicationScreenHost(new[] { block }, provider);
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var registered = host.Blocks.Single();
		Assert.That(registered.Menus.Single().Items.Select(item => item.Id), Is.EqualTo(new[] { "project", "browse" }));
		await renderer.Dispatcher.InvokeAsync(async () => {
			var sidebar = await renderer.RenderComponentAsync<ApplicationMenus>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(ApplicationMenus.Block)] = registered
			}));
			ApplicationCommandBar bar = null;
			var commands = await renderer.RenderComponentAsync<CommandBarHarness>(BarParameters(registered.Menus, registered.Menus.Single().Items,
				component => bar = component));
			Toggle(bar, "menu:navigation");
			await bar.SetParametersAsync(ParameterView.Empty);
			Assert.That(sidebar.ToHtmlString(), Does.Contain("Browse projects").And.Not.Contain("Hidden project launcher"));
			Assert.That(commands.ToHtmlString(), Does.Contain("Browse projects").And.Not.Contain("Hidden project launcher"));
		});

		var projectId = Guid.NewGuid();
		var first = await host.ActivateScreenAsync("projects", "project", Activation(projectId, "first"));
		var second = await host.ActivateScreenAsync("projects", "project", Activation(Guid.NewGuid(), "second"));
		var repeated = await host.ActivateScreenAsync("projects", "project", Activation(Guid.NewGuid(), "first"));
		Assert.That(first, Is.Not.Null);
		Assert.That(repeated, Is.SameAs(first));
		Assert.That(second, Is.Not.SameAs(first));
		Assert.That(first.Parameters[nameof(ProjectScreen.ProjectId)], Is.EqualTo(projectId));
		Assert.That(((ApplicationScreenSession)first).MenuItem.IsVisible, Is.False);
		Assert.That(host.Screens, Has.Length.EqualTo(2));
	}

	[Test]
	public async Task HiddenSeparatorRetainsPresentationMetadataThroughRegistration() {
		var enabled = false;
		var separator = new BlazorMenuSeparator { Id = "boundary", IsVisible = false, CanExecute = () => enabled };
		var before = new ApplicationMenuItemBuilder()
			.WithId("before")
			.WithText("Before")
			.WithAction((_, _) => Task.CompletedTask)
			.Build();
		var after = new ApplicationMenuItemBuilder()
			.WithId("after")
			.WithText("After")
			.WithAction((_, _) => Task.CompletedTask)
			.Build();
		var block = new ApplicationBlockBuilder()
			.WithId("projects")
			.WithName("Projects")
			.AddMenu(menu => menu
				.WithId("work")
				.WithText("Work")
				.AddItem(before)
				.AddItem(separator)
				.AddItem(after)
			)
			.Build();
		await using var provider = Services().BuildServiceProvider();
		using var host = new BlazorUI.ApplicationScreenHost(new[] { block }, provider);
		var registered = host.Blocks.Single();
		var boundary = registered.Menus.Single().Items.Single(item => item.Id == "boundary");
		Assert.That(boundary, Is.Not.SameAs(separator));
		Assert.That(boundary.IsVisible, Is.False);
		Assert.That(boundary.IsEnabled, Is.False);
		enabled = true;
		Assert.That(boundary.IsEnabled, Is.True, "Separator snapshots retain live availability metadata like other items.");
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			ApplicationCommandBar bar = null;
			var rendered = await renderer.RenderComponentAsync<CommandBarHarness>(BarParameters(registered.Menus, registered.Menus.Single().Items,
				component => bar = component));
			Toggle(bar, "menu:work");
			await bar.SetParametersAsync(ParameterView.Empty);
			Assert.That(rendered.ToHtmlString(), Does.Contain("Before").And.Contain("After").And.Not.Contain("<hr").And.Not.Contain("role=\"separator\""));
		});
	}

	[Test]
	public async Task HiddenScreenRemainsReachableThroughIncomingShellLocation() {
		var services = Services();
		services
			.AddApplicationBlock(block => block
				.WithId("projects")
				.WithName("Projects")
				.AddScreen<ProjectScreen>(screen => screen
					.WithTitle("Hidden project launcher")
					.WithActivationMode(ScreenActivationMode.MultiInstance)
				)
				.AddMenu(menu => menu
					.WithId("navigation")
					.WithText("Project navigation")
					.AddItem(item => item
						.WithId("project")
						.WithText("Hidden project launcher")
						.WithScreen<ProjectScreen>()
						.WithVisibility(false)
					)
				)
			);
		await using var provider = services.BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var host = provider.GetRequiredService<BlazorUI.IApplicationScreenHost>();
		await renderer.Dispatcher.InvokeAsync(async () => {
			var rendered = await renderer.RenderComponentAsync<ApplicationShell>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(ApplicationShell.BlockId)] = "projects", [nameof(ApplicationShell.ScreenId)] = "project"
			}));
			Assert.That(host.ActiveScreen.MenuItemId, Is.EqualTo("project"));
			Assert.That(rendered.ToHtmlString(), Does.Contain("Project content"));
			var header = Regex.Match(rendered.ToHtmlString(), "<header\\b.*?</header>", RegexOptions.Singleline).Value;
			var sidebar = Regex.Match(rendered.ToHtmlString(), "<aside\\b.*?</aside>", RegexOptions.Singleline).Value;
			Assert.That(header, Does.Not.Contain("Hidden project launcher").And.Not.Contain("Project navigation"));
			Assert.That(sidebar, Does.Not.Contain("Hidden project launcher").And.Not.Contain("Project navigation"));
		});
	}

	[Test]
	public async Task DisabledLeafCannotExecuteThroughHost() {
		var enabled = false;
		var calls = new List<string>();
		var command = new ApplicationMenuItemBuilder()
			.WithId("save")
			.WithText("Save draft")
			.EnabledWhen(() => enabled)
			.WithAction((_, _) => { calls.Add("action"); return Task.CompletedTask; })
			.Build();
		command.Select += () => calls.Add("select");
		var block = new ApplicationBlockBuilder()
			.WithId("projects")
			.WithName("Projects")
			.AddMenu(menu => menu
				.WithId("work")
				.WithText("Work")
				.AddItem(command)
			)
			.Build();
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorUI.ApplicationScreenHost(new[] { block }, provider);
		var hostUserInterface = (IBlazorUserInterfaceServices)host.UserInterfaceServices;
		var registered = host.Blocks.Single().Menus.Single().Items.Single();

		Assert.That(registered.IsEnabled, Is.False);
		Assert.That(await hostUserInterface.ExecuteMenuItemAsync(registered), Is.False);
		Assert.That(calls, Is.Empty);
		enabled = true;
		Assert.That(registered.IsEnabled, Is.True, "The registration snapshot preserves a live predicate.");
		Assert.That(await hostUserInterface.ExecuteMenuItemAsync(registered), Is.True);
		Assert.That(calls, Is.EqualTo(new[] { "action", "select" }));
		enabled = false;
		Assert.That(await hostUserInterface.ExecuteMenuItemAsync(registered), Is.False);
		Assert.That(calls, Has.Count.EqualTo(2), "A later disabled state is checked again before executing.");
	}

	[Test]
	public async Task EnabledPredicateUpdatesRenderedMenuAndToolbarWithoutReplacingTheCommand() {
		var enabled = false;
		var command = new ApplicationMenuItemBuilder()
			.WithId("save")
			.WithText("Save draft")
			.EnabledWhen(() => enabled)
			.WithAction((_, _) => Task.CompletedTask)
			.Build();
		var menu = new ApplicationMenuBuilder()
			.WithId("work")
			.WithText("Work")
			.AddItem(command)
			.Build();
		await using var provider = Services().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			ApplicationCommandBar bar = null;
			var rendered = await renderer.RenderComponentAsync<CommandBarHarness>(BarParameters(new[] { menu }, new[] { command }, component => bar = component));
			Toggle(bar, "menu:work");
			await bar.SetParametersAsync(ParameterView.Empty);
			var disabledButtons = Regex.Matches(rendered.ToHtmlString(), "<button\\b(?<attributes>[^>]*)>\\s*Save draft\\s*</button>");
			Assert.That(disabledButtons, Has.Count.EqualTo(2));
			Assert.That(disabledButtons.All(button => button.Groups["attributes"].Value.Contains("disabled")), Is.True);
			enabled = true;
			await bar.SetParametersAsync(ParameterView.Empty);
			var enabledButtons = Regex.Matches(rendered.ToHtmlString(), "<button\\b(?<attributes>[^>]*)>\\s*Save draft\\s*</button>");
			Assert.That(enabledButtons, Has.Count.EqualTo(2));
			Assert.That(enabledButtons.All(button => !button.Groups["attributes"].Value.Contains("disabled")), Is.True);
		});
	}

	[Test]
	public async Task ToolbarGroupDispatchesSelectedChildOnceAndClosesTheMenu() {
		var calls = new List<string>();
		var child = new ApplicationMenuItemBuilder()
			.WithId("route")
			.WithText("Export route")
			.WithAction((_, _) => { calls.Add("action"); return Task.CompletedTask; })
			.Build();
		child.Select += () => calls.Add("select");
		var hidden = new ApplicationMenuItemBuilder()
			.WithId("hidden")
			.WithText("Hidden export")
			.WithVisibility(false)
			.WithAction((_, _) => Task.CompletedTask)
			.Build();
		var group = (ApplicationMenuGroup)new ApplicationMenuItemBuilder()
			.WithId("export")
			.WithText("Export")
			.WithChildren(child, hidden)
			.Build();
		group.Select += () => calls.Add("group");
		await using var provider = Services().BuildServiceProvider();
		using var host = new BlazorUI.ApplicationScreenHost(Array.Empty<BlazorUI.IApplicationBlock>(), provider);
		var hostUserInterface = (IBlazorUserInterfaceServices)host.UserInterfaceServices;
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			ApplicationCommandBar bar = null;
			var selected = 0;
			var callback = EventCallback.Factory.Create<BlazorUI.IApplicationMenuItem>(new object(), async item => {
				selected++;
				await hostUserInterface.ExecuteMenuItemAsync(item);
			});
			var rendered = await renderer.RenderComponentAsync<CommandBarHarness>(BarParameters(Array.Empty<BlazorUI.IApplicationMenu>(), new[] { group },
				component => bar = component, callback));
			Assert.That(rendered.ToHtmlString(), Does.Contain("aria-haspopup=\"true\"").And.Contain("aria-expanded=\"false\"").And.Not.Contain("Export route"));
			Toggle(bar, "toolbar:export");
			await bar.SetParametersAsync(ParameterView.Empty);
			Assert.That(rendered.ToHtmlString(), Does.Contain("role=\"group\" aria-label=\"Export\"").And.Contain("Export route").And.Not.Contain("Hidden export"));
			await Execute(bar, group.Items[0]);
			await bar.SetParametersAsync(ParameterView.Empty);
			Assert.That(selected, Is.EqualTo(1));
			Assert.That(calls, Is.EqualTo(new[] { "action", "select" }), "Opening a group must not execute or notify the group itself.");
			Assert.That(rendered.ToHtmlString(), Does.Contain("aria-expanded=\"false\"").And.Not.Contain("Dismiss application menu").And.Not.Contain("Export route"));
		});
	}

	[TestCase("Escape")]
	[TestCase("Outside")]
	[TestCase("Toggle")]
	public async Task ToolbarDropdownDismissesWithoutExecutingItsChildren(string dismissal) {
		var selections = 0;
		var child = new ApplicationMenuItemBuilder()
			.WithId("route")
			.WithText("Export route")
			.WithAction((_, _) => Task.CompletedTask)
			.Build();
		var group = new ApplicationMenuItemBuilder()
			.WithId("export")
			.WithText("Export")
			.WithChildren(child)
			.Build();
		await using var provider = Services().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			ApplicationCommandBar bar = null;
			var rendered = await renderer.RenderComponentAsync<CommandBarHarness>(BarParameters(Array.Empty<BlazorUI.IApplicationMenu>(), new[] { group },
				component => bar = component, EventCallback.Factory.Create<BlazorUI.IApplicationMenuItem>(new object(), _ => selections++)));
			Toggle(bar, "toolbar:export");
			await bar.SetParametersAsync(ParameterView.Empty);
			Assert.That(rendered.ToHtmlString(), Does.Contain("Dismiss application menu").And.Contain("Export route"));
			if (dismissal == "Escape")
				typeof(ApplicationCommandBar).GetMethod("KeyDown", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(bar, new object[] { new KeyboardEventArgs { Key = "Escape" } });
			else if (dismissal == "Outside")
				typeof(ApplicationCommandBar).GetMethod("CloseMenu", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(bar, null);
			else
				Toggle(bar, "toolbar:export");
			await bar.SetParametersAsync(ParameterView.Empty);
			Assert.That(rendered.ToHtmlString(), Does.Contain("aria-expanded=\"false\"").And.Not.Contain("Dismiss application menu").And.Not.Contain("Export route"));
			Assert.That(selections, Is.Zero);
		});
	}

	[Test]
	public async Task RepeatedToolbarSelectionWhileChildIsRunningIsCoalesced() {
		var child = new ApplicationMenuItemBuilder()
			.WithId("route")
			.WithText("Export route")
			.WithAction((_, _) => Task.CompletedTask)
			.Build();
		var group = (ApplicationMenuGroup)new ApplicationMenuItemBuilder()
			.WithId("export")
			.WithText("Export")
			.WithChildren(child)
			.Build();
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		using var unblock = Tools.Scope.ExecuteOnDispose(() => release.TrySetResult());
		var selections = 0;
		await using var provider = Services().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			ApplicationCommandBar bar = null;
			var callback = EventCallback.Factory.Create<BlazorUI.IApplicationMenuItem>(new object(), async _ => {
				selections++;
				entered.TrySetResult();
				await release.Task;
			});
			await renderer.RenderComponentAsync<CommandBarHarness>(BarParameters(Array.Empty<BlazorUI.IApplicationMenu>(), new[] { group }, component => bar = component, callback));
			var first = Execute(bar, group.Items[0]);
			await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
			var repeated = Execute(bar, group.Items[0]);
			Assert.That(repeated.IsCompleted, Is.True);
			Assert.That(selections, Is.EqualTo(1));
			release.TrySetResult();
			await first;
		});
	}

	[Test]
	public void GroupBuilderSnapshotsChildrenAndRejectsDuplicateSiblingIds() {
		var child = new ApplicationMenuItemBuilder()
			.WithId("route")
			.WithText("Export route")
			.WithAction((_, _) => Task.CompletedTask)
			.Build();
		var replacement = new ApplicationMenuItemBuilder()
			.WithId("summary")
			.WithText("Print summary")
			.WithAction((_, _) => Task.CompletedTask)
			.Build();
		var children = new BlazorUI.IApplicationMenuItem[] { child };
		var builder = new ApplicationMenuItemBuilder()
			.WithId("export")
			.WithText("Export")
			.WithChildren(children);
		children[0] = replacement;
		var group = (ApplicationMenuGroup)builder.Build();
		var exposed = group.Items;
		exposed[0] = replacement;
		Assert.That(group.Items.Single().Id, Is.EqualTo("route"));
		Assert.That(() => new ApplicationMenuItemBuilder()
			.WithId("duplicate")
			.WithText("Duplicate")
			.WithChildren(child, child)
			.Build(), Throws.ArgumentException);
	}

	private static IServiceCollection Services() {
		var services = new ServiceCollection().AddLogging().AddSphere10Blazor();
		services.AddSingleton<NavigationManager>(new TestNavigationManager("http://localhost/", "http://localhost/application"));
		services.AddSingleton<IJSRuntime, GalleryRenderingTests.TestJsRuntime>();
		return services;
	}

	private static ApplicationScreenActivation Activation(Guid projectId, string key) =>
		new(new Dictionary<string, object> { [nameof(ProjectScreen.ProjectId)] = projectId }, key);

	private static ParameterView BarParameters(BlazorUI.IApplicationMenu[] menus, BlazorUI.IApplicationMenuItem[] items, Action<ApplicationCommandBar> capture,
		EventCallback<BlazorUI.IApplicationMenuItem> onSelect = default
	) => ParameterView.FromDictionary(new Dictionary<string, object> {
		[nameof(CommandBarHarness.Menus)] = menus, [nameof(CommandBarHarness.Items)] = items,
		[nameof(CommandBarHarness.Capture)] = capture, [nameof(CommandBarHarness.Select)] = onSelect
	});

	private static void Toggle(ApplicationCommandBar bar, string key) =>
		typeof(ApplicationCommandBar).GetMethod("ToggleMenu", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(bar, new object[] { key });

	private static Task Execute(ApplicationCommandBar bar, BlazorUI.IApplicationMenuItem item) =>
		(Task)typeof(ApplicationCommandBar).GetMethod("ExecuteAsync", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(bar, new object[] { item });

	public class ProjectScreen : ApplicationScreen {
		[Parameter] public Guid ProjectId { get; set; }

		protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, "Project content");
	}

	public class CommandBarHarness : ComponentBase {
		[Parameter] public BlazorUI.IApplicationMenu[] Menus { get; set; }

		[Parameter] public BlazorUI.IApplicationMenuItem[] Items { get; set; }

		[Parameter] public Action<ApplicationCommandBar> Capture { get; set; }

		[Parameter] public EventCallback<BlazorUI.IApplicationMenuItem> Select { get; set; }

		protected override void BuildRenderTree(RenderTreeBuilder builder) {
			builder.OpenComponent<ApplicationCommandBar>(0);
			builder.AddAttribute(1, nameof(ApplicationCommandBar.Menus), Menus);
			builder.AddAttribute(2, nameof(ApplicationCommandBar.ToolBarItems), Items);
			builder.AddAttribute(3, nameof(ApplicationCommandBar.OnSelect), Select);
			builder.AddComponentReferenceCapture(4, component => Capture((ApplicationCommandBar)component));
			builder.CloseComponent();
		}
	}
}
