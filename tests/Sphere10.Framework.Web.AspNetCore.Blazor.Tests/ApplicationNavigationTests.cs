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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Web.AspNetCore.Blazor.UI.Application;
using BlazorUI = global::Sphere10.Framework.Web.AspNetCore.Blazor;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationNavigationTests {
	[Test]
	public async Task LastTabCloseDoesNotReactivateTheBlockDefaultWhenItsQueryUrlReturns() {
		var navigation = new ProbeNavigation();
		await using var provider = CreateRenderingServices(navigation).BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var host = provider.GetRequiredService<BlazorUI.IApplicationScreenHost>();
		await renderer.Dispatcher.InvokeAsync(async () => {
			BrowserShell shell = null;
			await renderer.RenderComponentAsync<NavigationHarness>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(NavigationHarness.UseQueryRoutes)] = true,
				[nameof(NavigationHarness.Capture)] = (Action<BrowserShell>)(component => shell = component)
			}));
			await shell.SynchronizeBrowserAsync();
			await navigation.PendingNavigation;
			var active = host.ActiveScreen;
			Assert.That(active, Is.Not.Null);
			Assert.That(await host.CloseScreenAsync(active.Id), Is.True);
			Synchronize(shell);
			await navigation.PendingNavigation;
			Assert.That(navigation.Uri, Is.EqualTo("http://localhost/app/application?block=projects"));

			await shell.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(ApplicationShell.BlockId)] = "projects",
				[nameof(ApplicationShell.ScreenId)] = null,
				[nameof(ApplicationShell.InstanceId)] = null
			}));

			Assert.That(host.ActiveScreen, Is.Null);
			Assert.That(host.OpenScreens, Is.Empty);
			Assert.That(host.Screens, Is.Empty);
		});
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task ProgrammaticActivationSynchronizesRenderedShellWithoutAParentParameterUpdate(bool useQueryRoutes) {
		var navigation = new ProbeNavigation();
		await using var provider = CreateRenderingServices(navigation, includeProjectMenu: !useQueryRoutes).BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var host = provider.GetRequiredService<BlazorUI.IApplicationScreenHost>();
		await renderer.Dispatcher.InvokeAsync(async () => {
			BrowserShell shell = null;
			var rendered = await renderer.RenderComponentAsync<NavigationHarness>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(NavigationHarness.Capture)] = (Action<BrowserShell>)(component => shell = component),
				[nameof(NavigationHarness.UseQueryRoutes)] = useQueryRoutes
			}));
			await shell.SynchronizeBrowserAsync();
			if (!useQueryRoutes)
				Assert.That(navigation.Uri, Is.EqualTo("http://localhost/app/application"));
			var handler = typeof(ApplicationShell).GetMethod("OnBeforeInternalNavigationAsync", BindingFlags.Instance | BindingFlags.NonPublic)
				.CreateDelegate<Func<LocationChangingContext, Task>>(shell);
			using var registration = navigation.RegisterLocationChangingHandler(context => new ValueTask(handler(context)));
			var block = host.Blocks.Single();
			var definition = block.ScreenDefinitions.Single(screen => screen.ScreenType == typeof(ProjectScreen));

			// A direct registration open has no command and must still publish its default stable bookmark.
			var firstId = Guid.NewGuid();
			var first = useQueryRoutes
				? await host.OpenScreenAsync(block, definition, Activation(firstId))
				: await host.ActivateScreenAsync("projects", "project", Activation(firstId));
			await host.ShowScreenAsync(first.Id);
			await rendered.QuiescenceTask;
			await shell.SynchronizeBrowserAsync();
			await navigation.PendingNavigation;
			var firstLocation = useQueryRoutes
				? $"http://localhost/app/application?block=projects&screen={Uri.EscapeDataString(first.MenuItemId)}&instance={first.Id:D}"
				: $"http://localhost/app/project/{firstId:D}";
			Assert.That(navigation.Uri, Is.EqualTo(firstLocation));
			if (useQueryRoutes) {
				Assert.That(first.MenuItem, Is.Null);
				Assert.That(first.MenuItemId, Does.StartWith("__screen:"));
				Assert.That(Uri.UnescapeDataString(first.MenuItemId), Does.Contain(typeof(ProjectScreen).FullName));
			}
			Assert.That(navigation.ReplacedLastEntry, Is.False, "Programmatic selection adds a browser history entry.");
			var firstComponent = (ProjectScreen)first.Screen;
			Assert.That(firstComponent, Is.Not.Null);

			// The accepted second transition runs its leave guard once; URL synchronization must not run it again.
			var secondId = Guid.NewGuid();
			var second = useQueryRoutes
				? await host.OpenScreenAsync(block, definition, Activation(secondId))
				: await host.ActivateScreenAsync("projects", "project", Activation(secondId));
			await shell.SynchronizeBrowserAsync();
			await navigation.PendingNavigation;
			var secondLocation = useQueryRoutes
				? $"http://localhost/app/application?block=projects&screen={Uri.EscapeDataString(second.MenuItemId)}&instance={second.Id:D}"
				: $"http://localhost/app/project/{secondId:D}";
			Assert.That(navigation.Uri, Is.EqualTo(secondLocation));
			Assert.That(navigation.ReplacedLastEntry, Is.False);
			Assert.That(firstComponent.GuardCalls, Is.EqualTo(1));
			if (useQueryRoutes)
				Assert.That(second.MenuItemId, Is.EqualTo(first.MenuItemId));
		});
	}

	[Test]
	public async Task RepeatingAcceptedRouteParametersAfterClosingTheirScreenDoesNotReopenIt() {
		var navigation = new ProbeNavigation();
		await using var provider = CreateRenderingServices(navigation).BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var host = provider.GetRequiredService<BlazorUI.IApplicationScreenHost>();
		await renderer.Dispatcher.InvokeAsync(async () => {
			await host.InitializeAsync();
			var project = await host.ActivateScreenAsync("projects", "project", Activation(Guid.NewGuid()));
			BrowserShell shell = null;
			await renderer.RenderComponentAsync<NavigationHarness>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(NavigationHarness.Capture)] = (Action<BrowserShell>)(component => shell = component)
			}));
			var suppliedRoute = new Dictionary<string, object> {
				[nameof(ApplicationShell.BlockId)] = "projects", [nameof(ApplicationShell.ScreenId)] = "project", [nameof(ApplicationShell.InstanceId)] = project.Id
			};
			await shell.SetParametersAsync(ParameterView.FromDictionary(suppliedRoute));
			await shell.SynchronizeBrowserAsync();
			await navigation.PendingNavigation;
			Assert.That(host.ActiveScreen, Is.SameAs(project));
			Assert.That(await host.CloseScreenAsync(project.Id), Is.True);
			var fallback = host.ActiveScreen;
			Assert.That(fallback.ScreenType, Is.EqualTo(typeof(AnchorScreen)));
			await shell.SynchronizeBrowserAsync();
			await navigation.PendingNavigation;
			Assert.That(navigation.Uri, Is.EqualTo("http://localhost/app/application"));
			await shell.SetParametersAsync(ParameterView.FromDictionary(suppliedRoute));
			Assert.That(host.ActiveScreen, Is.SameAs(fallback));
			Assert.That(host.Screens, Has.Length.EqualTo(1));
			Assert.That(host.OpenScreens, Is.EqualTo(new[] { fallback }));
		});
	}

	[Test]
	public async Task HostChangesDuringAnIncomingRouteDoNotOverwriteItsLocationBeforeGuardCompletion() {
		var navigation = new ProbeNavigation();
		await using var provider = CreateRenderingServices(navigation).BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var host = provider.GetRequiredService<BlazorUI.IApplicationScreenHost>();
		await renderer.Dispatcher.InvokeAsync(async () => {
			BrowserShell shell = null;
			await renderer.RenderComponentAsync<NavigationHarness>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(NavigationHarness.Capture)] = (Action<BrowserShell>)(component => shell = component)
			}));
			var session = await host.ActivateScreenAsync("projects", "project", Activation(Guid.NewGuid()));
			await host.ShowScreenAsync(session.Id);
			await shell.SynchronizeBrowserAsync();
			var component = (ProjectScreen)session.Screen;
			var release = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
			component.GuardResult = release.Task;
			await navigation.TryNavigateAsync("http://localhost/app/requested");
			var route = shell.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(ApplicationShell.BlockId)] = "projects", [nameof(ApplicationShell.ScreenId)] = "anchor"
			}));
			await component.GuardEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
			host.NotifyScreenChanged(session.Id);
			await shell.SynchronizeBrowserAsync();
			Assert.That(navigation.Uri, Is.EqualTo("http://localhost/app/requested"));
			release.SetResult(true);
			await route;
			await shell.SynchronizeBrowserAsync();
			await navigation.PendingNavigation;
			Assert.That(navigation.Uri, Is.EqualTo("http://localhost/app/application"));
		});
	}

	[Test]
	public async Task CustomNavigationUsesCurrentSessionImmediatelyAndOmitsEphemeralQueries() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = CreateHost(provider);
		var navigation = new ProbeNavigation();
		var shell = CreateShell(host, navigation, ProjectPath);
		var firstId = Guid.NewGuid();
		var secondId = Guid.NewGuid();
		var first = await host.ActivateScreenAsync("projects", "project", Activation(firstId));
		Synchronize(shell);
		await navigation.PendingNavigation;
		Assert.That(navigation.Uri, Is.EqualTo($"http://localhost/app/project/{firstId:D}"));
		await host.ActivateScreenAsync("projects", "project", Activation(secondId));
		Synchronize(shell);
		await navigation.PendingNavigation;
		Assert.That(navigation.Uri, Is.EqualTo($"http://localhost/app/project/{secondId:D}"));
		Assert.That(new Uri(navigation.Uri).Query, Is.Empty);
		Assert.That(await host.ShowScreenAsync(first.Id), Is.True);
		Synchronize(shell);
		await navigation.PendingNavigation;
		Assert.That(navigation.Uri, Is.EqualTo($"http://localhost/app/project/{firstId:D}"));
		Assert.That(await host.CloseScreensAsync(Array.ConvertAll(host.Screens, session => session.Id)), Is.True);
		Synchronize(shell);
		await navigation.PendingNavigation;
		Assert.That(navigation.Uri, Is.EqualTo("http://localhost/app/application"));
	}

	[Test]
	public async Task GeneratedCustomPathDoesNotRepeatGuardsButExternalPathsRemainGuarded() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = CreateHost(provider);
		var navigation = new ProbeNavigation();
		var shell = CreateShell(host, navigation, ProjectPath);
		var handler = typeof(ApplicationShell).GetMethod("OnBeforeInternalNavigationAsync", BindingFlags.Instance | BindingFlags.NonPublic)
			.CreateDelegate<Func<LocationChangingContext, Task>>(shell);
		using var registration = navigation.RegisterLocationChangingHandler(context => new ValueTask(handler(context)));
		var first = await host.ActivateScreenAsync("projects", "project", Activation(Guid.NewGuid()));
		var guards = 0;
		var screen = new ApplicationScreenHostTests.ProbeScreen { Guard = _ => { guards++; return Task.FromResult(true); } };
		await host.AttachScreenAsync(first.Id, screen);
		await host.ActivateScreenAsync("projects", "project", Activation(Guid.NewGuid()));
		Assert.That(guards, Is.EqualTo(1));
		Synchronize(shell);
		Assert.That(await navigation.PendingNavigation, Is.True);
		Assert.That(guards, Is.EqualTo(1), "The generated URL follows an accepted host transition.");
		screen.Guard = _ => { guards++; return Task.FromResult(false); };
		Assert.That(await navigation.TryNavigateAsync("http://localhost/app/project/another"), Is.False);
		Assert.That(guards, Is.EqualTo(2));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task PublicCustomCommandPreservesLeaveGuardsAndSynchronizesDrawerAndRoute(bool allowNavigation) {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var nextId = Guid.NewGuid();
		var block = new ApplicationBlockBuilder()
			.WithId("projects")
			.WithName("Projects")
			.AddScreen<ApplicationScreenHostTests.ProbeScreen>(screen => screen
				.WithTitle("Project")
				.WithActivationMode(ScreenActivationMode.MultiInstance)
				.WithParameters(new Dictionary<string, object> { ["ProjectId"] = nextId })
			)
			.AddMenu(menu => menu
				.WithText("Screens")
				.AddItem(item => item
					.WithId("project")
					.WithText("Project")
					.WithScreen<ApplicationScreenHostTests.ProbeScreen>()
				)
			)
			.Build();
		using var host = new BlazorUI.ApplicationScreenHost(new[] { block }, provider);
		var navigation = new ProbeNavigation();
		var shell = CreateShell(host, navigation, ProjectPath);
		var handler = typeof(ApplicationShell).GetMethod("OnBeforeInternalNavigationAsync", BindingFlags.Instance | BindingFlags.NonPublic)
			.CreateDelegate<Func<LocationChangingContext, Task>>(shell);
		using var registration = navigation.RegisterLocationChangingHandler(context => new ValueTask(handler(context)));
		var firstId = Guid.NewGuid();
		var first = await host.ActivateScreenAsync("projects", "project", Activation(firstId));
		var guards = 0;
		await host.AttachScreenAsync(first.Id, new ApplicationScreenHostTests.ProbeScreen {
			Guard = _ => { guards++; return Task.FromResult(allowNavigation); }
		});
		Synchronize(shell);
		await navigation.PendingNavigation;
		typeof(ApplicationShell).GetMethod("ToggleNavigation", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(shell, null);
		await shell.ExecuteCommandAsync(host.Blocks[0].Menus[0].Items[0]);
		await navigation.PendingNavigation;
		var drawerOpen = (bool)typeof(ApplicationShell).GetField("_navigationOpen", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(shell);
		Assert.That(guards, Is.EqualTo(1), "A custom command uses the host guard once; its generated route must not repeat it.");
		Assert.That(drawerOpen, Is.EqualTo(!allowNavigation));
		Assert.That(navigation.Uri, Is.EqualTo($"http://localhost/app/project/{(allowNavigation ? nextId : firstId):D}"));
		Assert.That(host.Screens, Has.Length.EqualTo(allowNavigation ? 2 : 1));
		Assert.That(navigation.ReplacedLastEntry, Is.False);
	}

	[TestCase("https://example.com/project/1")]
	[TestCase("//example.com/project/1")]
	[TestCase("../outside")]
	[TestCase("project/1?instance=ephemeral")]
	[TestCase("project/1#fragment")]
	[TestCase("")]
	[TestCase(null)]
	public void CustomNavigationRejectsPathsOutsideApplicationOrWithQueriesAndFragments(string path) {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = CreateHost(provider);
		var navigation = new ProbeNavigation();
		var shell = CreateShell(host, navigation, _ => path);
		Assert.That(() => Synchronize(shell), Throws.InstanceOf<ArgumentException>());
		Assert.That(navigation.Uri, Is.EqualTo("http://localhost/app/application"));
	}

	[Test]
	public async Task DefaultNavigationPreservesCanonicalSessionQueries() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = CreateHost(provider);
		var navigation = new ProbeNavigation();
		var shell = new ApplicationShell { ScreenHost = host, Navigation = navigation };
		var session = await host.ActivateScreenAsync("projects", "project", Activation(Guid.NewGuid()));
		Synchronize(shell);
		await navigation.PendingNavigation;
		Assert.That(navigation.Uri, Is.EqualTo($"http://localhost/app/application?block=projects&screen=project&instance={session.Id:D}"));
	}

	private static ServiceCollection CreateRenderingServices(NavigationManager navigation, bool includeProjectMenu = true) {
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddSphere10Blazor();
		services.AddSingleton(navigation);
		services.AddSingleton<IJSRuntime, GalleryRenderingTests.TestJsRuntime>();
		services
			.AddApplicationBlock(block => block
				.WithId("projects")
				.WithName("Projects")
				.AddScreen<ProjectScreen>(screen => screen
					.WithTitle("Project")
					.WithActivationMode(ScreenActivationMode.MultiInstance)
				)
				.AddScreen<AnchorScreen>(screen => screen
					.WithTitle("Projects")
					.WithActivationMode(ScreenActivationMode.SingleInstance)
				)
				.WithDefaultScreen<AnchorScreen>()
				.AddMenu(menu => {
					menu
						.WithText("Screens")
						.AddItem(item => item
							.WithId("anchor")
							.WithText("Projects")
							.WithScreen<AnchorScreen>()
						);

					if (includeProjectMenu)
						menu.AddShowScreenItem<ProjectScreen>(text: "Project", id: "project");
				})
			);
		return services;
	}

	private static ApplicationShell CreateShell(BlazorUI.IApplicationScreenHost host, NavigationManager navigation, Func<ApplicationScreenSession, string> factory) {
		var shell = new ApplicationShell { ScreenHost = host, Navigation = navigation };
		ParameterView.FromDictionary(new Dictionary<string, object> { [nameof(ApplicationShell.NavigationUriFactory)] = factory }).SetParameterProperties(shell);
		return shell;
	}

	private static void Synchronize(ApplicationShell shell) => typeof(ApplicationShell)
		.GetMethod("SynchronizeLocation", BindingFlags.Instance | BindingFlags.NonPublic).CreateDelegate<Action<bool>>(shell)(false);

	private static string ProjectPath(ApplicationScreenSession session) =>
		session?.Parameters.TryGetValue("ProjectId", out var value) == true && value is Guid projectId ? $"project/{projectId:D}" : "application";

	private static ApplicationScreenActivation Activation(Guid projectId) =>
		new(new Dictionary<string, object> { ["ProjectId"] = projectId }, projectId.ToString("N"));

	private static BlazorUI.ApplicationScreenHost CreateHost(IServiceProvider provider) {
		var block = new ApplicationBlockBuilder()
			.WithId("projects")
			.WithName("Projects")
			.AddScreen<ApplicationScreenHostTests.ProbeScreen>(screen => screen
				.WithTitle("Project")
				.WithActivationMode(ScreenActivationMode.MultiInstance)
			)
			.AddMenu(menu => menu
				.WithText("Screens")
				.AddShowScreenItem<ApplicationScreenHostTests.ProbeScreen>(text: "Project", id: "project")
			)
			.Build();
		return new BlazorUI.ApplicationScreenHost(new[] { block }, provider);
	}

	public class BrowserShell : ApplicationShell {
		// HtmlRenderer has no browser post-render phase; drive that phase explicitly using the normal lifecycle hook.
		public Task SynchronizeBrowserAsync() => base.OnAfterRenderAsync(false);
	}

	public class NavigationHarness : ComponentBase {
		[Parameter] public Action<BrowserShell> Capture { get; set; }

		[Parameter] public bool UseQueryRoutes { get; set; }

		protected override void BuildRenderTree(RenderTreeBuilder builder) {
			builder.OpenComponent<BrowserShell>(0);
			if (!UseQueryRoutes)
				builder.AddAttribute(1, nameof(ApplicationShell.NavigationUriFactory), (Func<ApplicationScreenSession, string>)ProjectPath);
			builder.AddComponentReferenceCapture(2, component => Capture((BrowserShell)component));
			builder.CloseComponent();
		}
	}

	public class AnchorScreen : ApplicationScreen {
	}

	public class ProjectScreen : ApplicationScreen {
		[Parameter] public Guid ProjectId { get; set; }

		public Task<bool> GuardResult { get; set; }

		public TaskCompletionSource GuardEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public int GuardCalls { get; private set; }

		protected override Task<bool> CanDeactivateAsync(CancellationToken cancellationToken) {
			GuardCalls++;
			GuardEntered.TrySetResult();
			return GuardResult ?? Task.FromResult(true);
		}
	}

	private sealed class ProbeNavigation : NavigationManager {
		public ProbeNavigation() => Initialize("http://localhost/app/", "http://localhost/app/application");

		public Task<bool> PendingNavigation { get; private set; } = Task.FromResult(true);

		public bool ReplacedLastEntry { get; private set; }

		public async Task<bool> TryNavigateAsync(string uri) {
			if (!await NotifyLocationChangingAsync(uri, null, false))
				return false;
			Uri = uri;
			return true;
		}

		protected override void NavigateToCore(string uri, NavigationOptions options) {
			ReplacedLastEntry = options.ReplaceHistoryEntry;
			PendingNavigation = TryNavigateAsync(ToAbsoluteUri(uri).AbsoluteUri);
		}

		protected override void SetNavigationLockState(bool value) {
		}
	}
}
