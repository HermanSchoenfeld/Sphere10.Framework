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
using System.Net;
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
using Sphere10.Framework.Application.UI;
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
	public async Task CompactBlockMenuSupportsOrderedIconLinksAndFallbackLabels() {
		var services = CreateServices();
		services.AddApplicationBlock(block => block.WithId("with & spaces").WithName("Special block").WithPosition(2));
		await using var provider = services.BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var navigation = provider.GetRequiredService<NavigationManager>();
		await renderer.Dispatcher.InvokeAsync(async () => {
			var rendered = await renderer.RenderComponentAsync<ApplicationBlockMenu>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(ApplicationBlockMenu.Blocks)] = provider.GetRequiredService<IBlazorApplicationScreenHost>().Blocks,
				[nameof(ApplicationBlockMenu.Compact)] = true,
				[nameof(ApplicationBlockMenu.Href)] = (Func<IBlazorApplicationBlock, string>)(block =>
					navigation.ToAbsoluteUri($"application?block={Uri.EscapeDataString(block.Id)}").AbsoluteUri)
			}));
			var html = WebUtility.HtmlDecode(rendered.ToHtmlString());
			Assert.That(html, Does.Contain("application-blocks-compact").And.Not.Contain("<button"));
			Assert.That(html, Does.Contain("href=\"https://localhost/demo/application?block=first\""));
			Assert.That(html, Does.Contain("href=\"https://localhost/demo/application?block=with%20%26%20spaces\""));
			var firstIcon = Regex.Match(html, "<img[^>]+src=\"img/first\\.svg\"[^>]*>").Value;
			Assert.That(firstIcon, Does.Match("(?:^|\\s)alt(?:\\s*=\\s*(?:\"\"|''))?(?=\\s|/?>)"), "Decorative icons retain empty alt attributes.");
			Assert.That(html, Does.Contain("application-block-fallback").And.Contain("Special block"));
			Assert.That(html.IndexOf("First block", StringComparison.Ordinal), Is.LessThan(html.IndexOf("Second block", StringComparison.Ordinal)));
		});
	}

	[Test]
	public async Task FrameworkShellDockTracksCanonicalHostSelectionAndRegistration() {
		await using var provider = CreateServices().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var host = provider.GetRequiredService<IBlazorApplicationScreenHost>();
		await renderer.Dispatcher.InvokeAsync(async () => {
			var rendered = await renderer.RenderComponentAsync<ApplicationShell>();
			Assert.That(CurrentButton(DockMarkup(rendered.ToHtmlString())), Does.Contain("First block"));
			await host.ActivateBlockAsync("second");
			Assert.That(CurrentButton(DockMarkup(rendered.ToHtmlString())), Does.Contain("Second block"));
			await host.UnregisterBlockAsync("first");
			Assert.That(DockMarkup(rendered.ToHtmlString()), Does.Not.Contain("First block"));
			await host.RegisterBlockAsync("first");
			Assert.That(DockMarkup(rendered.ToHtmlString()), Does.Contain("First block"));
			Assert.That(Regex.Matches(rendered.ToHtmlString(), "aria-label=\"Application blocks\""), Has.Count.EqualTo(1),
				"The framework shell supplies the only block dock.");
		});
	}

	[Test]
	public async Task FrameworkDockBrowsesWithoutLeavingTheScreenAndScreenSelectionStillChecksItsGuard() {
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
			var route = ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(ApplicationShell.BlockId)] = original.Block.Id,
				[nameof(ApplicationShell.ScreenId)] = original.MenuItem.Id,
				[nameof(ApplicationShell.InstanceId)] = original.Id
			});
			await shell.SetParametersAsync(route);
			var originalUri = navigation.Uri;
			screen.IsDirty = true;
			var select = typeof(ApplicationShell).GetMethod("SelectBlockAsync", BindingFlags.Instance | BindingFlags.NonPublic);
			await (Task)select.Invoke(shell, new object[] { host.Blocks.Single(block => block.Id == "second") });
			await shell.SetParametersAsync(route);
			Assert.That(screen.GuardCalls, Is.Zero);
			Assert.That(host.ActiveScreen, Is.SameAs(original));
			Assert.That(original.Screen, Is.SameAs(screen));
			Assert.That(navigation.Uri, Is.EqualTo(originalUri));
			Assert.That(CurrentButton(DockMarkup(rendered.ToHtmlString())), Does.Contain("Second block"));
			Assert.That(host.OpenScreens, Has.Length.EqualTo(1));
			var sidebarMenus = Regex.Match(rendered.ToHtmlString(), "<nav[^>]*aria-label=\"Second block menus\".*?</nav>", RegexOptions.Singleline).Value;
			Assert.That(sidebarMenus, Does.Contain("Second screen").And.Not.Contain("aria-current=\"page\""),
				"Identical item IDs in different blocks must not mark an unrelated screen selected.");

			var execute = typeof(ApplicationShell).GetMethod("ExecuteMenuItemAsync", BindingFlags.Instance | BindingFlags.NonPublic);
			var item = host.ActiveBlock.Menus.Single().Items.Single();
			await (Task)execute.Invoke(shell, new object[] { item });
			Assert.That(screen.GuardCalls, Is.EqualTo(1));
			Assert.That(host.ActiveScreen, Is.SameAs(original));
			Assert.That(host.ActiveBlock.Id, Is.EqualTo("second"));
			screen.IsDirty = false;
			await (Task)execute.Invoke(shell, new object[] { item });
			Assert.That(host.ActiveScreen.Block.Id, Is.EqualTo("second"));
			Assert.That(host.OpenScreens, Has.Length.EqualTo(2));
		});
	}

	[Test]
	public async Task DisposingTheShellReleasesViewSubscriptionsWithoutDisposingScopedRuntime() {
		var services = CreateServices();
		services.AddScoped<IBlazorApplicationScreenHost>(provider => new TrackingHost(new BlazorApplicationScreenHost(provider.GetRequiredService<IBlazorApplicationBlockCatalog>(), provider)));
		await using var provider = services.BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var host = (TrackingHost)provider.GetRequiredService<IBlazorApplicationScreenHost>();
		var application = provider.GetRequiredService<IBlazorApplication>();
		var changed = typeof(ApplicationBase).GetField(nameof(ApplicationBase.Changed), BindingFlags.Instance | BindingFlags.NonPublic);
		await renderer.Dispatcher.InvokeAsync(() => renderer.RenderComponentAsync<ApplicationShell>());
		Assert.That(host.SubscriberCount, Is.EqualTo(2), "The scoped aggregate and screen view each observe the host.");
		Assert.That(((Delegate)changed.GetValue(application)).GetInvocationList(), Has.Length.EqualTo(1));
		await renderer.DisposeAsync();
		Assert.That(host.SubscriberCount, Is.EqualTo(1), "The surviving circuit aggregate remains subscribed.");
		Assert.That(changed.GetValue(application), Is.Null, "The disposed shell releases its aggregate subscription.");
		await host.ActivateBlockAsync("second");
		Assert.That(application.ActiveBlock.Id, Is.EqualTo("second"));
	}

	private static string CurrentButton(string html) => Regex.Match(html, "<button[^>]*aria-current=\"page\"[^>]*>.*?</button>", RegexOptions.Singleline).Value;

	private static string DockMarkup(string html) => Regex.Match(html, "<div class=\"sphere10-block-dock\"[^>]*>.*?</div>", RegexOptions.Singleline).Value;

	private static IServiceCollection CreateServices() {
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddSphere10Blazor();
		services.AddSingleton<NavigationManager>(new DockNavigationManager());
		services.AddSingleton<IJSRuntime, GalleryRenderingTests.TestJsRuntime>();
		services.AddApplicationBlock(block => block.WithId("second").WithName("Second block").WithPosition(1)
			.WithIconUrl("img/second.svg").WithDefaultScreen<OtherScreen>().AddMenu(menu => menu.WithText("Screens").AddScreenItem<OtherScreen>("screen", "Second screen")));
		services.AddApplicationBlock(block => block.WithId("first").WithName("First block").WithIconUrl("img/first.svg").WithDefaultScreen<GuardedScreen>().AddMenu(menu => menu.WithText("Screens").AddScreenItem<GuardedScreen>("screen", "First screen")));
		return services;
	}

	public sealed class WorkspaceHarness : ComponentBase {
		[Parameter] public Action<ApplicationShell> Capture { get; set; }

		protected override void BuildRenderTree(RenderTreeBuilder builder) {
			builder.OpenComponent<ApplicationShell>(0);
			builder.AddAttribute(1, nameof(ApplicationShell.BlockId), "first");
			builder.AddComponentReferenceCapture(2, component => Capture((ApplicationShell)component));
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