// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NUnit.Framework;
using Microsoft.JSInterop;
using Sphere10.Framework.Web.AspNetCore.Blazor.Tests.Loader;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Web.AspNetCore.Blazor.UI.Application;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationScreenPolicyTests {
	[Test]
	public async Task StartupUsesPluginAndBlockOrderAndRunsOnce() {
		var first = Block("first", typeof(FirstScreen), position: 50);
		var second = Block("second", typeof(SecondScreen), position: -10);
		var services = new ServiceCollection();
		services.AddSingleton<IBlazorPlugin>(new BlazorPlugin("Ordered", new[] { first, second }));
		using var provider = services.BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(new BlazorApplicationBlockCatalog(new[] { second, first }), provider);
		await host.InitializeAsync();
		var startup = host.ActiveScreen;
		Assert.That(host.Blocks.Select(block => block.Id), Is.EqualTo(new[] { "second", "first" }));
		Assert.That(startup.Block.Id, Is.EqualTo("first"));
		await host.ActivateBlockAsync("second");
		await host.InitializeAsync();
		Assert.That(host.ActiveScreen.Block.Id, Is.EqualTo("second"));
		Assert.That(host.Screens, Has.Length.EqualTo(2));
	}

	[Test]
	public async Task MarkedMenuDefaultWinsOverEarlierUnmarkedScreen() {
		var block = new BlazorApplicationBlockBuilder().WithId("work").WithName("Work")
			.AddMenu(menu => menu.WithId("screens").WithText("Screens")
				.AddScreenItem<FirstScreen>("first", "First")
				.ConfigureItem(item => item.WithId("second").WithText("Second").WithScreen<SecondScreen>().AsDefault())).Build();
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(new BlazorApplicationBlockCatalog(new[] { block }), provider);
		await host.InitializeAsync();
		Assert.That(host.ActiveScreen.MenuItem.Id, Is.EqualTo("second"));
		Assert.That(host.ActiveScreen.MenuItem.IsDefault, Is.True);
	}

	[TestCase(ScreenMode.SingleView)]
	[TestCase(ScreenMode.MultiView)]
	public async Task PermanentSessionsAutoOpenRemainSelectableAndCannotBeClosed(ScreenMode mode) {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = CreateHost(provider, permanent: true);
		await host.InitializeAsync();
		var permanent = host.Screens.Single(session => session.IsPermanent);
		var regular = host.ActiveScreen;
		Assert.That(regular.IsPermanent, Is.False);
		Assert.That(await host.TrySetScreenModeAsync(mode), Is.True);
		Assert.That(host.Screens, Does.Contain(permanent));
		Assert.That(await host.ShowScreenAsync(permanent.Id), Is.True);
		Assert.That(await host.ActivateBlockAsync("normal"), Is.SameAs(regular));
		Assert.That(await host.CanCloseScreenAsync(permanent.Id), Is.False);
		Assert.That(await host.CloseScreenAsync(permanent.Id), Is.False);
		Assert.That(await host.CloseScreensAsync(new[] { regular.Id, permanent.Id }), Is.False);
		Assert.That(host.Screens, Does.Contain(regular));
		Assert.That(await host.CloseScreenAsync(regular.Id), Is.True);
		Assert.That(host.ActiveScreen, Is.SameAs(permanent));
		Assert.That(host.Screens.All(session => session.ScreenKind == ScreenKind.Normal), Is.True);
	}

	[TestCase(ScreenActivationMode.SingleInstance)]
	[TestCase(ScreenActivationMode.MultiInstance)]
	public async Task EmptyWorkspaceIsTablessAndItsLifetimeControlsRetention(ScreenActivationMode mode) {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = CreateHost(provider, emptyMode: mode);
		await host.InitializeAsync();
		Assert.That(host.ActiveScreen.Block.Id, Is.EqualTo("normal"));
		await host.CloseScreenAsync(host.ActiveScreen.Id);
		var empty = host.ActiveScreen;
		Assert.That(empty.ScreenKind, Is.EqualTo(ScreenKind.Empty));
		Assert.That(host.OpenScreens, Is.Empty);
		var probe = new EmptyScreen();
		await host.AttachScreenAsync(empty.Id, probe);
		await host.ActivateBlockAsync("normal");
		Assert.That(host.Screens.Contains(empty), Is.EqualTo(mode == ScreenActivationMode.SingleInstance));
		Assert.That(probe.Deactivations, Is.EqualTo(1));
		await host.CloseScreenAsync(host.ActiveScreen.Id);
		Assert.That(host.ActiveScreen.Id == empty.Id, Is.EqualTo(mode == ScreenActivationMode.SingleInstance));
		if (mode == ScreenActivationMode.SingleInstance)
			Assert.That(probe.Activations, Is.EqualTo(2));
		Assert.That(host.OpenScreens, Is.Empty);
	}

	[Test]
	public async Task EmptyCannotHideAnOpenNormalTabAndVetoPreservesEmptyState() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = CreateHost(provider);
		await host.InitializeAsync();
		var normal = host.ActiveScreen;
		Assert.That(await host.ActivateBlockAsync("empty"), Is.Null);
		Assert.That(host.ActiveScreen, Is.SameAs(normal));
		await host.CloseScreenAsync(normal.Id);
		var empty = host.ActiveScreen;
		var probe = new EmptyScreen { AllowLeave = false };
		await host.AttachScreenAsync(empty.Id, probe);
		Assert.That(await host.ActivateBlockAsync("normal"), Is.Null);
		Assert.That(host.ActiveScreen, Is.SameAs(empty));
		Assert.That(host.OpenScreens, Is.Empty);
		probe.AllowLeave = true;
		Assert.That(await host.ActivateBlockAsync("normal"), Is.Not.Null);
	}

	[Test]
	public async Task AdministrativeRemovalReleasesPermanentAndRegistrationReopensItWithoutReplacingNormal() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = CreateHost(provider, permanent: true);
		await host.InitializeAsync();
		var original = host.Screens.Single(session => session.IsPermanent);
		await host.CloseScreenAsync(host.ActiveScreen.Id);
		Assert.That(await host.UnregisterBlockAsync("permanent"), Is.True);
		Assert.That(host.ActiveScreen.ScreenKind, Is.EqualTo(ScreenKind.Empty));
		await host.ActivateBlockAsync("normal");
		var selected = host.ActiveScreen;
		await host.RegisterBlockAsync("permanent");
		Assert.That(host.ActiveScreen, Is.SameAs(selected));
		var restored = host.Screens.Single(session => session.IsPermanent);
		Assert.That(restored.Id, Is.Not.EqualTo(original.Id));
		await host.InitializeAsync();
		Assert.That(host.Screens.Count(session => session.IsPermanent), Is.EqualTo(1));
	}

	[Test]
	public async Task RemovingOneOwnerKeepsThePermanentTypeDeclaredByAnotherBlockOpen() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var blocks = new[] {
			Block("first", typeof(PermanentScreen), ScreenActivationMode.PermanentSingleton),
			Block("second", typeof(PermanentScreen), ScreenActivationMode.PermanentSingleton),
			Block("empty", typeof(EmptyScreen), ScreenActivationMode.SingleInstance, ScreenKind.Empty)
		};
		using var host = new BlazorApplicationScreenHost(new BlazorApplicationBlockCatalog(blocks), provider);
		await host.InitializeAsync();
		var original = host.ActiveScreen;
		Assert.That(original.Block.Id, Is.EqualTo("first"));
		Assert.That(await host.UnregisterBlockAsync("first"), Is.True);
		Assert.That(host.ActiveScreen.IsPermanent, Is.True);
		Assert.That(host.ActiveScreen.Block.Id, Is.EqualTo("second"));
		Assert.That(host.ActiveScreen.Id, Is.Not.EqualTo(original.Id));
		Assert.That(host.OpenScreens, Has.Length.EqualTo(1));
		Assert.That(host.Screens.All(session => session.ScreenKind == ScreenKind.Normal), Is.True);
	}

	[Test]
	public async Task RemovingAnUnrelatedBlockDoesNotBypassAnEmptyScreenStartupVeto() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var blocks = new[] {
			Block("permanent", typeof(PermanentScreen), ScreenActivationMode.PermanentSingleton),
			Block("empty", typeof(EmptyScreen), ScreenActivationMode.SingleInstance, ScreenKind.Empty),
			new BlazorApplicationBlockBuilder().WithId("unrelated").WithName("Unrelated").Build()
		};
		using var host = new BlazorApplicationScreenHost(new BlazorApplicationBlockCatalog(blocks), provider);
		var empty = await host.ActivateBlockAsync("empty");
		var probe = new EmptyScreen { AllowLeave = false };
		await host.AttachScreenAsync(empty.Id, probe);
		await host.InitializeAsync();
		Assert.That(host.ActiveScreen, Is.SameAs(empty));
		Assert.That(host.OpenScreens, Is.Empty);
		Assert.That(await host.UnregisterBlockAsync("unrelated"), Is.True);
		Assert.That(host.Screens.Single(), Is.SameAs(empty));
		Assert.That(host.OpenScreens, Is.Empty);
		probe.AllowLeave = true;
		await host.InitializeAsync();
		Assert.That(host.ActiveScreen.IsPermanent, Is.True);
	}

	[Test]
	public async Task CancelledStartupCanBeRetriedAndAnExplicitSelectionWins() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = CreateHost(provider, permanent: true);
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		Assert.That(async () => await host.InitializeAsync(cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
		Assert.That(host.Screens, Is.Empty);
		var selected = await host.ActivateBlockAsync("normal");
		await host.InitializeAsync();
		Assert.That(host.ActiveScreen, Is.SameAs(selected));
		Assert.That(host.Screens.Count(session => session.IsPermanent), Is.EqualTo(1));
	}

	[Test]
	public void UnspecifiedSyntheticDefaultCannotConflictWithPermanentSingleton() {
		var defaultBlock = Block("default", typeof(PermanentScreen));
		var declared = Block("declared", typeof(PermanentScreen), ScreenActivationMode.PermanentSingleton);
		Assert.That(() => new BlazorApplicationBlockCatalog(new[] { defaultBlock, declared }), Throws.ArgumentException);
	}

	[Test]
	public void BuildersPreserveFlagsAndRejectPermanentEmptyCombinations() {
		var item = (BlazorScreenMenuItem)new BlazorApplicationMenuItemBuilder().WithId("empty").WithText("Empty")
			.WithScreen<EmptyScreen>().WithScreenKind(ScreenKind.Empty).AsDefault().AsMultiInstance().Build();
		var block = new BlazorApplicationBlockBuilder().WithId("work").WithName("Work")
			.AddMenu(menu => menu.WithId("screens").WithText("Screens").AddItem(item)).Build();
		var copy = new BlazorApplicationBlockCatalog(new[] { block }).Blocks.Single().Menus.Single().Items.OfType<BlazorScreenMenuItem>().Single();
		Assert.That(copy.ScreenKind, Is.EqualTo(ScreenKind.Empty));
		Assert.That(copy.IsDefault, Is.True);
		Assert.That(copy.ActivationMode, Is.EqualTo(ScreenActivationMode.MultiInstance));
		Assert.That(() => new BlazorApplicationBlockBuilder().WithName("Invalid")
			.WithDefaultScreen<EmptyScreen>(activationMode: ScreenActivationMode.PermanentSingleton, screenKind: ScreenKind.Empty), Throws.ArgumentException);
	}

	[TestCase(ScreenActivationMode.SingleInstance)]
	[TestCase(ScreenActivationMode.MultiInstance)]
	public async Task RendererRetainsSingletonEmptyAndDisposesMultiInstanceEmpty(ScreenActivationMode mode) {
		var services = new ServiceCollection().AddLogging().AddSphere10Blazor();
		services.AddSingleton<RenderedScreenState>();
		services.AddApplicationBlock(Block("normal", typeof(FirstScreen)));
		services.AddApplicationBlock(Block("empty", typeof(RenderedEmptyScreen), mode, ScreenKind.Empty));
		await using var provider = services.BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var host = provider.GetRequiredService<IBlazorApplicationScreenHost>();
		await renderer.Dispatcher.InvokeAsync(async () => {
			await host.InitializeAsync();
			await host.CloseScreenAsync(host.ActiveScreen.Id);
			var view = await renderer.RenderComponentAsync<ApplicationScreenHostView>();
			var session = host.ActiveScreen;
			var component = (RenderedEmptyScreen)session.Screen;
			Assert.That(component, Is.Not.Null);
			Assert.That(view.ToHtmlString(), Does.Not.Contain("role=\"tabpanel\""));
			await host.ActivateBlockAsync("normal");
			if (mode == ScreenActivationMode.MultiInstance)
				await component.Disposed.Task;
			Assert.That(component.Disposals, Is.EqualTo(mode == ScreenActivationMode.SingleInstance ? 0 : 1));
			var state = provider.GetRequiredService<RenderedScreenState>();
			state.Attached = new TaskCompletionSource<RenderedEmptyScreen>(TaskCreationOptions.RunContinuationsAsynchronously);
			await host.CloseScreenAsync(host.ActiveScreen.Id);
			if (mode == ScreenActivationMode.MultiInstance)
				await state.Attached.Task;
			Assert.That(ReferenceEquals(host.ActiveScreen.Screen, component), Is.EqualTo(mode == ScreenActivationMode.SingleInstance));
		});
	}

	[Test]
	public async Task PermanentTabHasNoCloseButtonButNormalTabDoes() {
		using var provider = new ServiceCollection().AddLogging().BuildServiceProvider();
		using var host = CreateHost(provider, permanent: true);
		await host.InitializeAsync();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var html = await renderer.Dispatcher.InvokeAsync(async () => {
			var view = await renderer.RenderComponentAsync<ApplicationScreenTabs>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(ApplicationScreenTabs.Sessions)] = host.OpenScreens,
				[nameof(ApplicationScreenTabs.ActiveSession)] = host.ActiveScreen
			}));
			return view.ToHtmlString();
		});
		Assert.That(html, Does.Contain("aria-label=\"permanent\""));
		Assert.That(html, Does.Not.Contain("aria-label=\"Close permanent\""));
		Assert.That(html, Does.Contain("aria-label=\"Close normal\""));
	}

	[TestCase("missing", null, "requested application block was not found")]
	[TestCase("normal", "missing", "requested screen was not found")]
	public async Task InvalidBookmarkStillInitializesPermanentScreens(string block, string screen, string error) {
		var services = new ServiceCollection().AddLogging().AddSphere10Blazor();
		services.AddSingleton<NavigationManager>(new TestNavigationManager());
		services.AddSingleton<IJSRuntime, GalleryRenderingTests.TestJsRuntime>();
		services.AddApplicationBlock(Block("normal", typeof(FirstScreen)));
		services.AddApplicationBlock(Block("permanent", typeof(PermanentScreen), ScreenActivationMode.PermanentSingleton));
		await using var provider = services.BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var html = await renderer.Dispatcher.InvokeAsync(async () => {
			var view = await renderer.RenderComponentAsync<ApplicationShell>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(ApplicationShell.BlockId)] = block,
				[nameof(ApplicationShell.ScreenId)] = screen
			}));
			return view.ToHtmlString();
		});
		Assert.That(html, Does.Contain(error));
		Assert.That(provider.GetRequiredService<IBlazorApplicationScreenHost>().OpenScreens.Any(session => session.IsPermanent), Is.True);
	}

	private static BlazorApplicationScreenHost CreateHost(IServiceProvider provider, bool permanent = false, ScreenActivationMode emptyMode = ScreenActivationMode.SingleInstance) {
		var blocks = new[] {
			Block("normal", typeof(FirstScreen), ScreenActivationMode.SingleInstance),
			Block("empty", typeof(EmptyScreen), emptyMode, ScreenKind.Empty),
			Block("permanent", typeof(PermanentScreen), ScreenActivationMode.PermanentSingleton)
		};
		return new BlazorApplicationScreenHost(new BlazorApplicationBlockCatalog(permanent ? blocks : blocks.Take(2)), provider);
	}

	private static BlazorApplicationBlock Block(string id, Type type, ScreenActivationMode? mode = null, ScreenKind kind = ScreenKind.Normal, int position = 0) =>
		new BlazorApplicationBlockBuilder().WithId(id).WithName(id).WithPosition(position)
			.WithDefaultScreen(type, id, mode, kind).Build();

	public class RenderedScreenState {
		public TaskCompletionSource<RenderedEmptyScreen> Attached { get; set; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
	}
	public class RenderedEmptyScreen : BlazorApplicationScreen {
		[Inject] public RenderedScreenState State { get; set; }
		public TaskCompletionSource<bool> Disposed { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
		public int Disposals { get; private set; }
		protected override async Task OnInitializedAsync() {
			await base.OnInitializedAsync();
			State.Attached.TrySetResult(this);
		}
		protected override ValueTask DisposeAsyncCore() {
			Disposals++;
			Disposed.TrySetResult(true);
			return ValueTask.CompletedTask;
		}
	}
	public class FirstScreen : ComponentBase, IBlazorApplicationScreen { }
	public class SecondScreen : ComponentBase, IBlazorApplicationScreen { }
	public class PermanentScreen : ComponentBase, IBlazorApplicationScreen { }
	public class EmptyScreen : ComponentBase, IBlazorApplicationScreen {
		public bool AllowLeave { get; set; } = true;
		public int Activations { get; private set; }
		public int Deactivations { get; private set; }
		public Task<bool> CanDeactivateAsync(CancellationToken cancellationToken = default) => Task.FromResult(AllowLeave);
		public Task OnActivatedAsync(CancellationToken cancellationToken = default) { Activations++; return Task.CompletedTask; }
		public Task OnDeactivatedAsync(CancellationToken cancellationToken = default) { Deactivations++; return Task.CompletedTask; }
	}
}
