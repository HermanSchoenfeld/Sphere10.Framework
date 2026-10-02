// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework.Application.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NUnit.Framework;
using Sphere10.Framework.Web.AspNetCore.Blazor.Tests.Loader;
using Sphere10.Framework.Web.AspNetCore.Blazor.UI.Application;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationRenderingTests {
	[Test]
	public async Task SwitchingScreensRetainsInstancesAndClosingDisposesExactlyOnce() {
		await using var provider = CreateServices().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var host = provider.GetRequiredService<IBlazorApplicationScreenHost>();
		var state = provider.GetRequiredService<ProbeState>();
		await renderer.Dispatcher.InvokeAsync(async () => {
			var first = await host.ActivateScreenAsync("test", "single");
			var rendered = await renderer.RenderComponentAsync<ApplicationScreenHostView>();
			var original = (ProbeScreen)first.Screen;
			await original.IncrementAsync();
			var second = await host.ActivateScreenAsync("test", "multiple");
			await rendered.QuiescenceTask;
			Assert.That(rendered.ToHtmlString(), Does.Contain("hidden"));
			Assert.That(state.Created, Has.Count.EqualTo(2));
			Assert.That(original.DisposalCount, Is.Zero);
			await host.ActivateScreenAsync("test", "single");
			Assert.That(first.Screen, Is.SameAs(original));
			Assert.That(original.Count, Is.EqualTo(1));
			Assert.That(rendered.ToHtmlString(), Does.Contain("Count: 1"));
			await host.CloseScreenAsync(first.Id);
			Assert.That(original.DisposalCount, Is.EqualTo(1));
			Assert.That(host.ActiveScreen.Id, Is.EqualTo(second.Id));
		});
		await renderer.DisposeAsync();
		Assert.That(state.Created.All(screen => screen.DisposalCount == 1), Is.True);
	}

	[Test]
	public async Task ScreenChangesNotifyWorkspaceAndGuardRejectsNavigation() {
		await using var provider = CreateServices().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var host = provider.GetRequiredService<IBlazorApplicationScreenHost>();
		await renderer.Dispatcher.InvokeAsync(async () => {
			var original = await host.ActivateScreenAsync("test", "single");
			await renderer.RenderComponentAsync<ApplicationScreenHostView>();
			var notifications = 0;
			host.Changed += () => notifications++;
			((ProbeScreen)original.Screen).SetDirty(true);
			Assert.That(notifications, Is.EqualTo(1));
			Assert.That(host.HasUnsavedChanges, Is.True);
			Assert.That(await host.ActivateScreenAsync("test", "multiple"), Is.Null);
			Assert.That(await host.CanNavigateAsync(), Is.False);
			Assert.That(host.ActiveScreen, Is.SameAs(original));
			Assert.That(host.OpenScreens, Has.Length.EqualTo(1));
		});
	}

	[Test]
	public async Task ShellResolvesBookmarksAndHistoryWithoutDuplicatingExistingSessions() {
		await using var provider = CreateServices().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var host = provider.GetRequiredService<IBlazorApplicationScreenHost>();
		await renderer.Dispatcher.InvokeAsync(async () => {
			ApplicationShell shell = null;
			var rendered = await renderer.RenderComponentAsync<ShellHarness>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(ShellHarness.Capture)] = (Action<ApplicationShell>)(instance => shell = instance)
			}));
			var first = host.ActiveScreen;
			Assert.That(rendered.ToHtmlString(), Does.Contain("Test application"));
			await shell.SetParametersAsync(Request("multiple"));
			var second = host.ActiveScreen;
			Assert.That(second, Is.Not.SameAs(first));
			await shell.SetParametersAsync(Request("single", first.Id));
			Assert.That(host.ActiveScreen, Is.SameAs(first));
			await shell.SetParametersAsync(Request("multiple", second.Id));
			Assert.That(host.ActiveScreen, Is.SameAs(second));
			Assert.That(host.OpenScreens, Has.Length.EqualTo(2));
			await shell.SetParametersAsync(Request("missing"));
			Assert.That(rendered.ToHtmlString(), Does.Contain("requested screen was not found"));
			Assert.That(host.OpenScreens, Has.Length.EqualTo(2));
		});
	}

	[Test]
	public async Task DefaultScreenWithoutMenuCanBeReopenedFromItsBookmark() {
		var services = CreateServices();
		services.AddApplicationBlock(block => block.WithId("default").WithName("Default only").WithDefaultScreen<DefaultOnlyScreen>());
		await using var provider = services.BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var host = provider.GetRequiredService<IBlazorApplicationScreenHost>();
		await renderer.Dispatcher.InvokeAsync(async () => {
			var session = await host.ActivateBlockAsync("default");
			var rendered = await renderer.RenderComponentAsync<ApplicationShell>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(ApplicationShell.BlockId)] = "default",
				[nameof(ApplicationShell.ScreenId)] = session.MenuItem.Id,
				[nameof(ApplicationShell.InstanceId)] = session.Id
			}));
			Assert.That(rendered.ToHtmlString(), Does.Not.Contain("not found"));
			Assert.That(host.OpenScreens, Has.Length.EqualTo(1));
		});
	}

	[Test]
	public async Task NewRouteCancelsAnOlderRequestWaitingForAnAsyncGuard() {
		await using var provider = CreateServices().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var host = provider.GetRequiredService<IBlazorApplicationScreenHost>();
		await renderer.Dispatcher.InvokeAsync(async () => {
			ApplicationShell shell = null;
			await renderer.RenderComponentAsync<ShellHarness>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(ShellHarness.Capture)] = (Action<ApplicationShell>)(instance => shell = instance)
			}));
			var original = host.ActiveScreen;
			var probe = (ProbeScreen)original.Screen;
			var releaseGuard = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			probe.GuardStarted = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			probe.GuardResult = releaseGuard.Task;
			var superseded = shell.SetParametersAsync(Request("multiple"));
			await probe.GuardStarted.Task;
			var latest = shell.SetParametersAsync(Request("single", original.Id));
			releaseGuard.SetResult(true);
			await Task.WhenAll(superseded, latest);
			Assert.That(host.ActiveScreen, Is.SameAs(original));
			Assert.That(host.OpenScreens, Has.Length.EqualTo(1), "A superseded browser request must not create a hidden session.");
		});
	}
	[Test]
	public async Task SingleViewHistorySelectsTheRetainedInstanceWithoutReexecutingItsMenu() {
		await using var provider = CreateServices().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var host = provider.GetRequiredService<IBlazorApplicationScreenHost>();
		await renderer.Dispatcher.InvokeAsync(async () => {
			await host.TrySetScreenModeAsync(ScreenMode.SingleView);
			var selections = 0;
			var item = host.Blocks.Single().Menus.Single().Items.Single(item => item.Id == "single");
			item.Select += () => selections++;
			ApplicationShell shell = null;
			await renderer.RenderComponentAsync<ShellHarness>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(ShellHarness.Capture)] = (Action<ApplicationShell>)(instance => shell = instance)
			}));
			var original = host.ActiveScreen;
			var component = original.Screen;
			var initialSelections = selections;
			await shell.SetParametersAsync(Request("multiple"));
			Assert.That(host.OpenScreens, Has.Length.EqualTo(1));
			Assert.That(host.Screens, Has.Length.EqualTo(2));
			await shell.SetParametersAsync(Request("single", original.Id));
			Assert.That(host.ActiveScreen, Is.SameAs(original));
			Assert.That(original.Screen, Is.SameAs(component));
			Assert.That(selections, Is.EqualTo(initialSelections), "A history entry selects a retained instance rather than invoking its menu again.");
		});
	}

	[Test]
	public async Task ShellPlacesSharedCommandsAndOptionalChromeOutsideTheScreenBody() {
		await using var provider = CreateServices().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var application = (BlazorApplication)provider.GetRequiredService<IBlazorApplication>();
		application.SetToolBarItems(new[] { new BlazorActionMenuItem { Id = "refresh", Title = "Refresh application", Action = () => { } } });
		await renderer.Dispatcher.InvokeAsync(async () => {
			var rendered = await renderer.RenderComponentAsync<ApplicationShell>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(ApplicationShell.Title)] = "Test workspace",
				[nameof(ApplicationShell.ShowScreenModeSelector)] = true,
				[nameof(ApplicationShell.Header)] = (RenderFragment)(builder => builder.AddContent(0, "Header tools")),
				[nameof(ApplicationShell.SidebarHeader)] = (RenderFragment)(builder => builder.AddContent(0, "Endpoint controls")),
				[nameof(ApplicationShell.Sidebar)] = (RenderFragment)(builder => builder.AddContent(0, "Supplementary navigation")),
				[nameof(ApplicationShell.Footer)] = (RenderFragment)(builder => builder.AddContent(0, "Ready status"))
			}));
			var html = rendered.ToHtmlString();
			var header = Regex.Match(html, "<header\\b.*?</header>", RegexOptions.Singleline).Value;
			var sidebar = Regex.Match(html, "<aside\\b.*?</aside>", RegexOptions.Singleline).Value;
			var content = Regex.Match(html, "<main\\b.*?</main>", RegexOptions.Singleline).Value;
			Assert.That(header, Does.Contain("Test workspace").And.Contain("Header tools").And.Contain("Screen layout").And.Contain("Refresh application"));
			Assert.That(sidebar, Does.Contain("Endpoint controls").And.Contain("Supplementary navigation").And.Contain("application-blocks-compact").And.Contain("class=\"fa fa-folder\""));
			Assert.That(sidebar.IndexOf("Endpoint controls", StringComparison.Ordinal), Is.LessThan(sidebar.IndexOf("Screens", StringComparison.Ordinal)));
			Assert.That(sidebar.IndexOf("Supplementary navigation", StringComparison.Ordinal), Is.LessThan(sidebar.IndexOf("application-blocks-compact", StringComparison.Ordinal)));
			Assert.That(content, Does.Contain("role=\"tablist\"").And.Contain("sphere10-screen-body").And.Contain("Count: 0"));
			Assert.That(content, Does.Not.Contain("Header tools").And.Not.Contain("Endpoint controls").And.Not.Contain("Refresh application"));
			Assert.That(html, Does.Contain("aria-label=\"Application status\"").And.Contain("Ready status"));
			var skipTarget = Regex.Match(html, "href=\"#(?<target>sphere10-navigation-[^\"]+-content)\"").Groups["target"].Value;
			Assert.That(skipTarget, Is.Not.Empty);
			Assert.That(content, Does.Contain($"id=\"{skipTarget}\"").And.Contain("tabindex=\"-1\""));
		});
	}

	[Test]
	public async Task CollapsingNavigationKeepsTheScreenMountedAndEscapeDismissesTheDrawer() {
		await using var provider = CreateServices().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var host = provider.GetRequiredService<IBlazorApplicationScreenHost>();
		await renderer.Dispatcher.InvokeAsync(async () => {
			ApplicationShell shell = null;
			var rendered = await renderer.RenderComponentAsync<ShellHarness>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(ShellHarness.Capture)] = (Action<ApplicationShell>)(instance => shell = instance)
			}));
			var original = host.ActiveScreen.Screen;
			var toggle = typeof(ApplicationShell).GetMethod("ToggleNavigation", BindingFlags.Instance | BindingFlags.NonPublic);
			var keyDown = typeof(ApplicationShell).GetMethod("OnShellKeyDown", BindingFlags.Instance | BindingFlags.NonPublic);
			toggle.Invoke(shell, null);
			await shell.SetParametersAsync(ParameterView.Empty);
			Assert.That(rendered.ToHtmlString(), Does.Contain("aria-expanded=\"true\"").And.Contain("Close application navigation"));
			keyDown.Invoke(shell, new object[] { new KeyboardEventArgs { Key = "Escape" } });
			await shell.SetParametersAsync(ParameterView.Empty);
			Assert.That(rendered.ToHtmlString(), Does.Not.Contain("Close application navigation"));
			Assert.That(host.ActiveScreen.Screen, Is.SameAs(original));
			Assert.That(((ProbeScreen)original).DisposalCount, Is.Zero);
			Assert.That(host.Screens, Has.Length.EqualTo(1));
		});
	}

	private static ParameterView Request(string screenId, Guid? instanceId = null) => ParameterView.FromDictionary(new Dictionary<string, object> {
		[nameof(ApplicationShell.BlockId)] = "test",
		[nameof(ApplicationShell.ScreenId)] = screenId,
		[nameof(ApplicationShell.InstanceId)] = instanceId
	});

	private static IServiceCollection CreateServices() {
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddSphere10Blazor();
		services.AddScoped<ProbeState>();
		services.AddSingleton<NavigationManager>(new TestNavigationManager("http://localhost/", "http://localhost/application"));
		services.AddSingleton<IJSRuntime, GalleryRenderingTests.TestJsRuntime>();
		services.AddApplicationBlock(block => block.WithId("test").WithName("Test application").WithDefaultScreen<ProbeScreen>()
			.AddMenu(menu => menu.WithText("Screens").WithIcon("fa fa-folder").AddScreenItem<ProbeScreen>("single", "Single", ScreenActivationMode.SingleInstance)
				.AddScreenItem<OtherScreen>("multiple", "Multiple", ScreenActivationMode.MultiInstance)));
		return services;
	}

	public class ProbeState {
		public List<ProbeScreen> Created { get; } = new();
	}

	public class ProbeScreen : BlazorApplicationScreen {
		private bool _dirty;

		[Inject] public ProbeState State { get; set; }

		public TaskCompletionSource<bool> GuardStarted { get; set; }

		public Task<bool> GuardResult { get; set; }

		public int Count { get; private set; }

		public int DisposalCount { get; private set; }

		public override bool HasUnsavedChanges => _dirty;

		public Task IncrementAsync() {
			Count++;
			return InvokeAsync(StateHasChanged);
		}

		public void SetDirty(bool value) {
			_dirty = value;
			NotifyScreenChanged();
		}

		protected override void OnInitialized() => State.Created.Add(this);

		protected override Task<bool> CanDeactivateAsync(CancellationToken cancellationToken) {
			GuardStarted?.TrySetResult(true);
			return GuardResult ?? Task.FromResult(!_dirty);
		}

		protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, $"Count: {Count}");

		protected override ValueTask DisposeAsyncCore() {
			DisposalCount++;
			return ValueTask.CompletedTask;
		}
	}

	public class DefaultOnlyScreen : ProbeScreen {
	}

	public class OtherScreen : ProbeScreen {
	}

	public class ShellHarness : ComponentBase {
		[Parameter] public Action<ApplicationShell> Capture { get; set; }

		protected override void BuildRenderTree(RenderTreeBuilder builder) {
			builder.OpenComponent<ApplicationShell>(0);
			builder.AddAttribute(1, nameof(ApplicationShell.BlockId), "test");
			builder.AddComponentReferenceCapture(2, component => Capture((ApplicationShell)component));
			builder.CloseComponent();
		}
	}
}
