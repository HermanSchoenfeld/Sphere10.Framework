// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationTabsTests {
	[Test]
	public async Task ReorderingRetainsSelectionIdentityAndLifecycleAndUsesTabOrderOnClose() {
		using var services = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(Catalog(), services);
		var first = await host.ActivateScreenAsync("block", "single");
		var original = new Probe();
		await host.AttachScreenAsync(first.Id, original);
		var second = await host.ActivateScreenAsync("block", "multiple");
		var third = await host.ActivateScreenAsync("block", "multiple");
		await host.ShowScreenAsync(first.Id);
		var activations = original.Activations;
		var deactivations = original.Deactivations;
		await host.MoveScreenAsync(first.Id, 2);
		Assert.That(host.OpenScreens.Select(screen => screen.Id), Is.EqualTo(new[] { second.Id, third.Id, first.Id }));
		Assert.That(host.ActiveScreen, Is.SameAs(first));
		Assert.That(first.Screen, Is.SameAs(original));
		Assert.That(original.Activations, Is.EqualTo(activations));
		Assert.That(original.Deactivations, Is.EqualTo(deactivations));
		Assert.That(await host.CloseScreenAsync(first.Id), Is.True);
		Assert.That(host.ActiveScreen, Is.SameAs(third));
	}

	[Test]
	public async Task BatchCloseVetoAndModeSwitchAreAtomicAcrossHiddenAndVisibleGuards() {
		using var services = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(Catalog(), services);
		var first = await host.ActivateScreenAsync("block", "single");
		var guarded = new Probe();
		await host.AttachScreenAsync(first.Id, guarded);
		var second = await host.ActivateScreenAsync("block", "multiple");
		guarded.AllowLeave = false;
		Assert.That(await host.CloseScreensAsync(new[] { first.Id, second.Id, first.Id }), Is.False);
		Assert.That(await host.TrySetScreenModeAsync(ScreenMode.SingleView), Is.False);
		Assert.That(host.ScreenMode, Is.EqualTo(ScreenMode.MultiView));
		Assert.That(host.OpenScreens, Is.EqualTo(new[] { first, second }));
		Assert.That(host.ActiveScreen, Is.SameAs(second));
		guarded.AllowLeave = true;
		Assert.That(await host.TrySetScreenModeAsync(ScreenMode.SingleView), Is.True);
		Assert.That(host.OpenScreens, Is.EqualTo(new[] { second }));
		Assert.That(host.Screens, Is.EqualTo(new[] { second }));
	}

	[Test]
	public async Task SingleViewRetainsSingletonButReleasesDepartedMultiInstance() {
		using var services = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(Catalog(), services);
		await host.TrySetScreenModeAsync(ScreenMode.SingleView);
		var first = await host.ActivateScreenAsync("block", "single");
		var original = new Probe();
		await host.AttachScreenAsync(first.Id, original);
		var scratch = await host.ActivateScreenAsync("block", "multiple");
		Assert.That(host.Screens, Has.Length.EqualTo(2));
		Assert.That(host.OpenScreens.Single(), Is.SameAs(scratch));
		Assert.That(await host.ActivateScreenAsync("block", "single"), Is.SameAs(first));
		Assert.That(first.Screen, Is.SameAs(original));
		Assert.That(host.Screens.Single(), Is.SameAs(first));
		Assert.That(original.Activations, Is.EqualTo(2));
		await host.TrySetScreenModeAsync(ScreenMode.MultiView);
		Assert.That(host.ActiveScreen, Is.SameAs(first));
		Assert.That(original.Activations, Is.EqualTo(2), "Changing presentation alone must not activate a screen again.");
	}

	[Test]
	public async Task RetainedSingletonStillGuardsNavigationWhenSingleViewHasNoActiveScreen() {
		using var services = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(Catalog(), services);
		await host.TrySetScreenModeAsync(ScreenMode.SingleView);
		var first = await host.ActivateScreenAsync("block", "single");
		var screen = new Probe();
		await host.AttachScreenAsync(first.Id, screen);
		await host.ActivateBlockAsync("actions");
		screen.AllowLeave = false;
		Assert.That(host.OpenScreens, Is.Empty);
		Assert.That(host.Screens.Single(), Is.SameAs(first));
		Assert.That(await host.CanNavigateAsync(), Is.False);
	}

	[Test]
	public async Task ExplicitScreenCommandUsesActiveBlockAndCanonicalParameters() {
		using var services = new ServiceCollection().BuildServiceProvider();
		var firstBlock = new BlazorApplicationBlockBuilder().WithId("first").WithName("First")
			.AddMenu(menu => menu.WithText("Screens").AddScreenItem<Probe>("same", "First", ScreenActivationMode.MultiInstance)).Build();
		var secondBlock = new BlazorApplicationBlockBuilder().WithId("second").WithName("Second")
			.AddMenu(menu => menu.WithText("Screens").AddScreenItem<Probe>("same", "Second", ScreenActivationMode.MultiInstance,
				new System.Collections.Generic.Dictionary<string, object> { ["Caption"] = "Canonical" })).Build();
		using var host = new BlazorApplicationScreenHost(new BlazorApplicationBlockCatalog(new[] { firstBlock, secondBlock }), services);
		await host.ActivateBlockAsync("second");
		var supplied = new BlazorScreenMenuItem { Id = "same", Title = "Forged", ScreenType = typeof(Probe), ActivationMode = ScreenActivationMode.MultiInstance };
		Assert.That(await host.ExecuteMenuItemAsync(supplied), Is.True);
		Assert.That(host.ActiveScreen.Block.Id, Is.EqualTo("second"));
		Assert.That(host.ActiveScreen.Title, Is.EqualTo("Second"));
		Assert.That(host.ActiveScreen.Parameters["Caption"], Is.EqualTo("Canonical"));
		var canonicalFirst = host.Blocks.Single(block => block.Id == "first").Menus.Single().Items.Single();
		Assert.That(await host.ExecuteMenuItemAsync(canonicalFirst), Is.True);
		Assert.That(host.ActiveScreen.Block.Id, Is.EqualTo("first"), "A canonical command retains its owning block even when another block is active.");
	}

	[Test]
	public async Task CancelledBatchLeavesEverySessionAndSelectionUntouched() {
		using var services = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(Catalog(), services);
		var first = await host.ActivateScreenAsync("block", "single");
		using var cancellation = new CancellationTokenSource();
		await host.AttachScreenAsync(first.Id, new Probe { Guard = token => {
			cancellation.Cancel();
			return Task.FromResult(true);
		} });
		Assert.That(async () => await host.CloseScreensAsync(new[] { first.Id }, cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
		Assert.That(host.ActiveScreen, Is.SameAs(first));
		Assert.That(host.OpenScreens.Single(), Is.SameAs(first));
	}

	[Test]
	public void ToolbarScreenItemsMustMatchRegisteredActivationPolicies() {
		var builder = new BlazorApplicationBlockBuilder().WithId("block").WithName("Block")
			.AddMenu(menu => menu.WithText("Screens").AddScreenItem<Probe>("single", "Single", ScreenActivationMode.SingleInstance))
			.AddToolBarItem(item => item.WithId("single").WithText("Wrong policy").WithScreen<Probe>().AsMultiInstance());
		Assert.That(() => builder.Build(), Throws.ArgumentException);
	}

	private static BlazorApplicationBlockCatalog Catalog() => new(new[] {
		new BlazorApplicationBlockBuilder().WithId("block").WithName("Block")
			.AddMenu(menu => menu.WithId("work").WithText("Work").AddScreenItem<Probe>("single", "Single", ScreenActivationMode.SingleInstance)
				.AddScreenItem<OtherProbe>("multiple", "Multiple", ScreenActivationMode.MultiInstance)).Build(),
		new BlazorApplicationBlockBuilder().WithId("actions").WithName("Actions").Build()
	});

	public class Probe : ComponentBase, IBlazorApplicationScreen {
		public bool AllowLeave { get; set; } = true;

		public int Activations { get; private set; }

		public int Deactivations { get; private set; }

		public Func<CancellationToken, Task<bool>> Guard { get; set; }

		public Task<bool> CanDeactivateAsync(CancellationToken cancellationToken = default) => Guard?.Invoke(cancellationToken) ?? Task.FromResult(AllowLeave);

		public Task OnActivatedAsync(CancellationToken cancellationToken = default) {
			Activations++;
			return Task.CompletedTask;
		}

		public Task OnDeactivatedAsync(CancellationToken cancellationToken = default) {
			Deactivations++;
			return Task.CompletedTask;
		}
	}

	public class OtherProbe : Probe {
	}
}