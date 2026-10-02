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
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Web.AspNetCore.Blazor.Tests.Loader;
using Sphere10.Framework.Web.AspNetCore.Blazor.UI.Application;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationCommandRenderingTests {
	[Test]
	public async Task SwitchingScreensRestoresBroaderCommandsWithoutDuplicationOrLostSubscriptions() {
		await using var provider = Services().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var host = provider.GetRequiredService<IBlazorApplicationScreenHost>();
		var application = (BlazorApplication)provider.GetRequiredService<IBlazorApplication>();
		var calls = 0;
		var applicationSave = new BlazorActionMenuItem { Id = "save", Title = "Application save", Action = () => calls++ };
		application.SetMenus(new[] { new BlazorApplicationMenu { Id = "work", Text = "Work", Items = new[] { applicationSave } } });
		application.SetToolBarItems(new[] { applicationSave });
		await renderer.Dispatcher.InvokeAsync(async () => {
			var first = await host.ActivateScreenAsync("test", "one");
			var rendered = await renderer.RenderComponentAsync<ApplicationShell>();
			var screen = (CommandScreen)first.Screen;
			Assert.That(Regex.Matches(rendered.ToHtmlString(), "Screen save").Count, Is.EqualTo(2));
			Assert.That(rendered.ToHtmlString(), Does.Not.Contain("Application save"));
			await host.ExecuteMenuItemAsync(screen.ToolBarItems.Single());
			Assert.That(screen.Saves, Is.EqualTo(1));
			Assert.That(calls, Is.Zero);
			await host.ActivateScreenAsync("test", "two");
			Assert.That(Regex.Matches(rendered.ToHtmlString(), "Application save").Count, Is.EqualTo(2));
			Assert.That(rendered.ToHtmlString(), Does.Not.Contain("Screen save"));
			await host.ShowScreenAsync(first.Id);
			await host.ExecuteMenuItemAsync(screen.ToolBarItems.Single());
			Assert.That(screen.Saves, Is.EqualTo(2), "Returning to the screen must not duplicate command subscriptions.");
			Assert.That(first.Screen, Is.SameAs(screen));
		});
	}

	[Test]
	public async Task BrowsingAnotherBlockKeepsCommandsBoundToTheActiveScreenUntilItCloses() {
		var services = Services();
		services.AddApplicationBlock(block => block.WithId("browse").WithName("Browse")
			.AddMenu(menu => menu.WithText("Browse menu").AddActionItem("browse-action", "Browse action", () => { }))
			.AddToolBarItem(item => item.WithId("browse-tool").WithText("Browse tool").WithAction((_, _) => Task.CompletedTask)));
		await using var provider = services.BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var host = provider.GetRequiredService<IBlazorApplicationScreenHost>();
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
			Assert.That(chrome, Does.Contain("Browse menu").And.Contain("Browse tool").And.Not.Contain("Screen save"));
		});
	}

	[Test]
	public async Task SingleViewKeepsHiddenSingletonMountedAndDisposesMultiInstanceOnDeparture() {
		await using var provider = Services().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var host = provider.GetRequiredService<IBlazorApplicationScreenHost>();
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
		var host = provider.GetRequiredService<IBlazorApplicationScreenHost>();
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
		var host = provider.GetRequiredService<IBlazorApplicationScreenHost>();
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
		services.AddApplicationBlock(block => block.WithId("test").WithName("Test")
			.AddMenu(menu => menu.WithId("work").WithText("Work").AddScreenItem<CommandScreen>("one", "One", ScreenActivationMode.SingleInstance)
				.AddScreenItem<TemporaryScreen>("two", "Two", ScreenActivationMode.MultiInstance)));
		services.AddApplicationBlock(block => block.WithId("empty").WithName("Empty"));
		return services;
	}

	public class CommandScreen : BlazorApplicationScreen {
		private readonly BlazorActionMenuItem _save;

		public CommandScreen() {
			_save = new BlazorActionMenuItem { Id = "save", Title = "Screen save", Action = () => Saves++ };
		}

		public int Saves { get; private set; }

		public int Disposals { get; private set; }

		public override IBlazorApplicationMenu[] Menus => new[] { new BlazorApplicationMenu { Id = "work", Text = "Work", Items = new[] { _save } } };

		public override IBlazorApplicationMenuItem[] ToolBarItems => new[] { _save };

		protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, "Command screen");

		protected override ValueTask DisposeAsyncCore() {
			Disposals++;
			return ValueTask.CompletedTask;
		}
	}

	public class TabsHarness : ComponentBase {
		[Parameter] public BlazorApplicationScreenSession Session { get; set; }

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

	public class TemporaryScreen : BlazorApplicationScreen {
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
}