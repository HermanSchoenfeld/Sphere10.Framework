// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Net;
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
using Sphere10.Framework.Utils.BlazorTester.Layouts;
using Sphere10.Framework.Web.AspNetCore.Blazor.UI.Application;
using Sphere10.Framework.Web.AspNetCore.Blazor.UI.MainFrame;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class BlockDockTests {
	[Test]
	public async Task ExistingBlockMenuDefaultsToSelectionButtons() {
		await using var provider = CreateServices().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var blocks = provider.GetRequiredService<IBlazorApplicationBlockCatalog>().Blocks;
		await renderer.Dispatcher.InvokeAsync(async () => {
			var rendered = await renderer.RenderComponentAsync<ApplicationBlockMenu>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(ApplicationBlockMenu.Blocks)] = blocks,
				[nameof(ApplicationBlockMenu.ActiveBlock)] = blocks[0]
			}));
			var html = rendered.ToHtmlString();
			Assert.That(html, Does.Contain("<button").And.Not.Contain("<a "));
			Assert.That(html, Does.Contain("First block").And.Contain("Second block").And.Contain("aria-current=\"page\""));
			Assert.That(html, Does.Not.Contain("application-blocks-compact"));
		});
	}

	[Test]
	public async Task DockRendersOrderedIconLinksAndEscapesBlockIdsWithinTheApplicationBasePath() {
		var services = CreateServices();
		services.AddApplicationBlock(block => block.WithId("with & spaces").WithName("Special block").WithPosition(2));
		await using var provider = services.BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			var rendered = await renderer.RenderComponentAsync<DemoBlockDock>();
			var html = WebUtility.HtmlDecode(rendered.ToHtmlString());
			Assert.That(html, Does.Contain("application-blocks-compact").And.Not.Contain("<button"));
			Assert.That(html, Does.Contain("href=\"https://localhost/demo/application?block=first\""));
			Assert.That(html, Does.Contain("href=\"https://localhost/demo/application?block=with%20%26%20spaces\""));
			var firstIcon = Regex.Match(html, "<img[^>]+src=\"img/first\\.svg\"[^>]*>").Value;
			Assert.That(firstIcon, Does.Match("(?:^|\\s)alt(?:\\s*=\\s*(?:\"\"|''))?(?=\\s|/?>)"), "The decorative icon must retain an empty alt attribute.");
			Assert.That(html, Does.Contain("application-block-fallback").And.Contain("Special block"));
			Assert.That(html.IndexOf("First block", StringComparison.Ordinal), Is.LessThan(html.IndexOf("Second block", StringComparison.Ordinal)));
		});
	}

	[Test]
	public async Task DockTracksTheScopedHostAndHidesSelectionOutsideTheWorkspace() {
		await using var provider = CreateServices().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var host = provider.GetRequiredService<IBlazorApplicationScreenHost>();
		var navigation = provider.GetRequiredService<NavigationManager>();
		await renderer.Dispatcher.InvokeAsync(async () => {
			await host.ActivateBlockAsync("first");
			navigation.NavigateTo("components/grid");
			var rendered = await renderer.RenderComponentAsync<DemoBlockDock>();
			Assert.That(rendered.ToHtmlString(), Does.Not.Contain("aria-current"));

			navigation.NavigateTo("application?block=first");
			Assert.That(CurrentLink(rendered.ToHtmlString()), Does.Contain("First block"));
			await host.ActivateBlockAsync("second");
			Assert.That(CurrentLink(rendered.ToHtmlString()), Does.Contain("Second block"));
			await host.UnregisterBlockAsync("first");
			Assert.That(rendered.ToHtmlString(), Does.Not.Contain("block=first"));
			await host.RegisterBlockAsync("first");
			Assert.That(rendered.ToHtmlString(), Does.Contain("block=first"));
			navigation.NavigateTo("widget-gallery");
			Assert.That(rendered.ToHtmlString(), Does.Not.Contain("aria-current"));
		});
	}

	[Test]
	public async Task DockLinkRunsThroughTheExistingShellGuardBeforeChangingTheActiveBlock() {
		await using var provider = CreateServices().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var host = provider.GetRequiredService<IBlazorApplicationScreenHost>();
		var navigation = provider.GetRequiredService<NavigationManager>();
		await renderer.Dispatcher.InvokeAsync(async () => {
			ApplicationShell shell = null;
			var rendered = await renderer.RenderComponentAsync<WorkspaceHarness>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(WorkspaceHarness.Capture)] = (Action<ApplicationShell>)(instance => shell = instance)
			}));
			var original = host.ActiveScreen;
			var screen = (GuardedScreen)original.Screen;
			screen.IsDirty = true;
			var dock = DockMarkup(rendered.ToHtmlString());
			var destination = WebUtility.HtmlDecode(Regex.Match(dock, "href=\"([^\"]+block=second)\"").Groups[1].Value);
			Assert.That(destination, Is.EqualTo("https://localhost/demo/application?block=second"));

			// Follow the dock's ordinary link, then supply its query as the routable workspace page does.
			navigation.NavigateTo(destination);
			await shell.SetParametersAsync(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(ApplicationShell.BlockId)] = "second",
				[nameof(ApplicationShell.ScreenId)] = null,
				[nameof(ApplicationShell.InstanceId)] = null
			}));

			Assert.That(screen.GuardCalls, Is.EqualTo(1));
			Assert.That(host.ActiveScreen, Is.SameAs(original));
			Assert.That(host.ActiveBlock.Id, Is.EqualTo("first"));
			Assert.That(CurrentLink(DockMarkup(rendered.ToHtmlString())), Does.Contain("First block"));
			Assert.That(host.OpenScreens, Has.Length.EqualTo(1));
		});
	}

	[Test]
	public async Task DisposingTheDockReleasesItsHostSubscription() {
		var services = CreateServices();
		services.AddScoped<IBlazorApplicationScreenHost>(provider => new TrackingHost(new BlazorApplicationScreenHost(provider.GetRequiredService<IBlazorApplicationBlockCatalog>(), provider)));
		await using var provider = services.BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var host = (TrackingHost)provider.GetRequiredService<IBlazorApplicationScreenHost>();
		await renderer.Dispatcher.InvokeAsync(() => renderer.RenderComponentAsync<DemoBlockDock>());
		Assert.That(host.SubscriberCount, Is.EqualTo(1));
		await renderer.DisposeAsync();
		Assert.That(host.SubscriberCount, Is.Zero);
		await host.ActivateBlockAsync("first");
		Assert.That(() => provider.GetRequiredService<NavigationManager>().NavigateTo("components/grid"), Throws.Nothing);
	}

	private static string CurrentLink(string html) => Regex.Match(html, "<a[^>]*aria-current=\"page\"[^>]*>.*?</a>", RegexOptions.Singleline).Value;

	private static string DockMarkup(string html) => Regex.Match(html, "<div class=\"demo-block-dock\"[^>]*>.*?</div>", RegexOptions.Singleline).Value;

	private static IServiceCollection CreateServices() {
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddSphere10Blazor();
		services.AddSingleton<NavigationManager>(new DockNavigationManager());
		services.AddSingleton<IJSRuntime, GalleryRenderingTests.TestJsRuntime>();
		services.AddApplicationBlock(block => block.WithId("second").WithName("Second block").WithPosition(1)
			.WithIconUrl("img/second.svg").WithDefaultScreen<OtherScreen>());
		services.AddApplicationBlock(block => block.WithId("first").WithName("First block").WithIconUrl("img/first.svg").WithDefaultScreen<GuardedScreen>());
		return services;
	}

	public sealed class WorkspaceHarness : ComponentBase {
		[Parameter] public Action<ApplicationShell> Capture { get; set; }

		protected override void BuildRenderTree(RenderTreeBuilder builder) {
			builder.OpenComponent<DemoBlockDock>(0);
			builder.CloseComponent();
			builder.OpenComponent<ApplicationShell>(1);
			builder.AddAttribute(2, nameof(ApplicationShell.BlockId), "first");
			builder.AddComponentReferenceCapture(3, component => Capture((ApplicationShell)component));
			builder.CloseComponent();
		}
	}

	public class GuardedScreen : BlazorApplicationScreen {
		public bool IsDirty { get; set; }

		public int GuardCalls { get; private set; }

		public override bool HasUnsavedChanges => IsDirty;

		protected override Task<bool> CanDeactivateAsync(CancellationToken cancellationToken) {
			GuardCalls++;
			return Task.FromResult(!IsDirty);
		}

		protected override void BuildRenderTree(RenderTreeBuilder builder) => builder.AddContent(0, "Guarded screen");
	}

	public sealed class OtherScreen : GuardedScreen {
	}

	private sealed class DockNavigationManager : NavigationManager {
		public DockNavigationManager() => Initialize("https://localhost/demo/", "https://localhost/demo/application");

		protected override void NavigateToCore(string uri, bool forceLoad) {
			Uri = ToAbsoluteUri(uri).AbsoluteUri;
			NotifyLocationChanged(false);
		}

		protected override void NavigateToCore(string uri, NavigationOptions options) => NavigateToCore(uri, options.ForceLoad);
	}

	private sealed class TrackingHost : BlazorApplicationScreenHostDecorator, IBlazorApplicationScreenHost {
		event EventHandlerEx IBlazorApplicationScreenHost.Changed {
			add {
				SubscriberCount++;
				InternalHost.Changed += value;
			}
			remove {
				SubscriberCount--;
				InternalHost.Changed -= value;
			}
		}

		public TrackingHost(IBlazorApplicationScreenHost host)
			: base(host) {
		}

		public int SubscriberCount { get; private set; }
	}
}
