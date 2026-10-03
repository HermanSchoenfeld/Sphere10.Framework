// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Web.AspNetCore.Blazor.UI.Application;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationNavigationTests {
	[Test]
	public async Task ProgrammaticActivationSynchronizesRenderedShellWithoutAParentParameterUpdate() {
		var navigation = new ProbeNavigation();
		await using var provider = CreateRenderingServices(navigation).BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var host = provider.GetRequiredService<IBlazorApplicationScreenHost>();
		await renderer.Dispatcher.InvokeAsync(async () => {
			BrowserShell shell = null;
			await renderer.RenderComponentAsync<NavigationHarness>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(NavigationHarness.Capture)] = (Action<BrowserShell>)(component => shell = component)
			}));
			await shell.SynchronizeBrowserAsync();
			Assert.That(navigation.Uri, Is.EqualTo("http://localhost/app/application"));
			var firstId = Guid.NewGuid();
			await host.ActivateScreenAsync("projects", "project", Activation(firstId));
			await shell.SynchronizeBrowserAsync();
			await navigation.PendingNavigation;
			Assert.That(navigation.Uri, Is.EqualTo($"http://localhost/app/project/{firstId:D}"));
			Assert.That(navigation.ReplacedLastEntry, Is.False, "Programmatic selection adds a browser history entry.");
			var secondId = Guid.NewGuid();
			await host.ActivateScreenAsync("projects", "project", Activation(secondId));
			await shell.SynchronizeBrowserAsync();
			await navigation.PendingNavigation;
			Assert.That(navigation.Uri, Is.EqualTo($"http://localhost/app/project/{secondId:D}"));
		});
	}

	[Test]
	public async Task HostChangesDuringAnIncomingRouteDoNotOverwriteItsLocationBeforeGuardCompletion() {
		var navigation = new ProbeNavigation();
		await using var provider = CreateRenderingServices(navigation).BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var host = provider.GetRequiredService<IBlazorApplicationScreenHost>();
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

	private static ServiceCollection CreateRenderingServices(NavigationManager navigation) {
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddSphere10Blazor();
		services.AddSingleton(navigation);
		services.AddSingleton<IJSRuntime, GalleryRenderingTests.TestJsRuntime>();
		services.AddApplicationBlock(block => block.WithId("projects").WithName("Projects")
			.AddMenu(menu => menu.WithText("Screens")
				.ConfigureItem(item => item.WithId("anchor").WithText("Projects").WithScreen<AnchorScreen>().AsSingleInstance().AsDefault())
				.AddScreenItem<ProjectScreen>("project", "Project", ScreenActivationMode.MultiInstance)));
		return services;
	}

	private static ApplicationShell CreateShell(IBlazorApplicationScreenHost host, NavigationManager navigation, Func<BlazorApplicationScreenSession, string> factory) {
		var shell = new ApplicationShell { ScreenHost = host, Navigation = navigation };
		ParameterView.FromDictionary(new Dictionary<string, object> { [nameof(ApplicationShell.NavigationUriFactory)] = factory }).SetParameterProperties(shell);
		return shell;
	}

	private static void Synchronize(ApplicationShell shell) => typeof(ApplicationShell)
		.GetMethod("SynchronizeLocation", BindingFlags.Instance | BindingFlags.NonPublic).CreateDelegate<Action<bool>>(shell)(false);

	private static string ProjectPath(BlazorApplicationScreenSession session) =>
		session?.Parameters.TryGetValue("ProjectId", out var value) == true && value is Guid projectId ? $"project/{projectId:D}" : "application";

	private static BlazorApplicationScreenActivation Activation(Guid projectId) =>
		new(new Dictionary<string, object> { ["ProjectId"] = projectId }, projectId.ToString("N"));

	private static BlazorApplicationScreenHost CreateHost(IServiceProvider provider) {
		var block = new BlazorApplicationBlockBuilder().WithId("projects").WithName("Projects")
			.AddMenu(menu => menu.WithText("Screens").AddScreenItem<ApplicationScreenHostTests.ProbeScreen>("project", "Project", ScreenActivationMode.MultiInstance)).Build();
		return new BlazorApplicationScreenHost(new BlazorApplicationBlockCatalog(new[] { block }), provider);
	}

	public class BrowserShell : ApplicationShell {
		// HtmlRenderer has no browser post-render phase; drive that phase explicitly using the normal lifecycle hook.
		public Task SynchronizeBrowserAsync() => base.OnAfterRenderAsync(false);
	}

	public class NavigationHarness : ComponentBase {
		[Parameter] public Action<BrowserShell> Capture { get; set; }

		protected override void BuildRenderTree(RenderTreeBuilder builder) {
			builder.OpenComponent<BrowserShell>(0);
			builder.AddAttribute(1, nameof(ApplicationShell.NavigationUriFactory), (Func<BlazorApplicationScreenSession, string>)ProjectPath);
			builder.AddComponentReferenceCapture(2, component => Capture((BrowserShell)component));
			builder.CloseComponent();
		}
	}

	public class AnchorScreen : BlazorApplicationScreen {
	}

	public class ProjectScreen : BlazorApplicationScreen {
		[Parameter] public Guid ProjectId { get; set; }

		public Task<bool> GuardResult { get; set; }

		public TaskCompletionSource GuardEntered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		protected override Task<bool> CanDeactivateAsync(CancellationToken cancellationToken) {
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
