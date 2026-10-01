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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NUnit.Framework;
using Sphere10.Framework.Web.AspNetCore.Blazor.Logic;
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
		var host = provider.GetRequiredService<IApplicationScreenHost>();
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
		var host = provider.GetRequiredService<IApplicationScreenHost>();
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
			Assert.That(host.OpenScreens, Has.Count.EqualTo(1));
		});
	}

	[Test]
	public async Task ShellResolvesBookmarksAndHistoryWithoutDuplicatingExistingSessions() {
		await using var provider = CreateServices().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var host = provider.GetRequiredService<IApplicationScreenHost>();
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
			Assert.That(host.OpenScreens, Has.Count.EqualTo(2));
			await shell.SetParametersAsync(Request("missing"));
			Assert.That(rendered.ToHtmlString(), Does.Contain("requested screen was not found"));
			Assert.That(host.OpenScreens, Has.Count.EqualTo(2));
		});
	}

	[Test]
	public async Task DefaultScreenWithoutMenuCanBeReopenedFromItsBookmark() {
		var services = CreateServices();
		services.AddApplicationBlock(block => block.WithId("default").WithName("Default only").WithDefaultScreen<DefaultOnlyScreen>());
		await using var provider = services.BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var host = provider.GetRequiredService<IApplicationScreenHost>();
		await renderer.Dispatcher.InvokeAsync(async () => {
			var session = await host.ActivateBlockAsync("default");
			var rendered = await renderer.RenderComponentAsync<ApplicationShell>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(ApplicationShell.BlockId)] = "default",
				[nameof(ApplicationShell.ScreenId)] = session.MenuItem.Id
			}));
			Assert.That(rendered.ToHtmlString(), Does.Not.Contain("not found"));
			Assert.That(host.OpenScreens, Has.Count.EqualTo(1));
		});
	}

	[Test]
	public async Task NewRouteCancelsAnOlderRequestWaitingForAnAsyncGuard() {
		await using var provider = CreateServices().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var host = provider.GetRequiredService<IApplicationScreenHost>();
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
			Assert.That(host.OpenScreens, Has.Count.EqualTo(1), "A superseded browser request must not create a hidden session.");
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
			.AddMenu(menu => menu.WithText("Screens").AddScreenItem<ProbeScreen>("single", "Single")
				.AddScreenItem<OtherScreen>("multiple", "Multiple", ScreenActivationMode.MultiInstance)));
		return services;
	}

	public class ProbeState {
		public List<ProbeScreen> Created { get; } = new();
	}

	public class ProbeScreen : ApplicationScreen {
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
