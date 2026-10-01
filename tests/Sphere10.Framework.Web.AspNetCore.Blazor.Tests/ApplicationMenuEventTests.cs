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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationMenuEventTests {
	[Test]
	public async Task ScreenSelectionsPreserveRegistrationCallbacksOnceThroughEverySnapshot() {
		var selections = 0;
		var lateSelections = 0;
		EventHandlerEx selected = () => selections++;
		var item = new BlazorScreenMenuItem { Id = "screen", Title = "Screen", ScreenType = typeof(TestScreen) };
		item.Select += selected;
		var catalog = CreateCatalog(item);
		item.Select -= selected;
		item.Select += () => lateSelections++;
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(catalog, provider);

		Assert.That(await host.ExecuteMenuItemAsync("block", "screen"), Is.True);
		Assert.That(selections, Is.EqualTo(1));
		await host.ActivateScreenAsync("block", "screen");
		Assert.That(selections, Is.EqualTo(2));
		await host.ShowScreenAsync(host.ActiveScreen.Id);
		Assert.That(selections, Is.EqualTo(2), "Selecting an existing tab does not select a menu item.");
		Assert.That(lateSelections, Is.Zero, "Subscriptions added to the original item after registration do not alter the catalog snapshot.");
	}

	[Test]
	public async Task MatchingDefaultScreenKeepsItsSelectionCallback() {
		var selections = 0;
		var item = new BlazorScreenMenuItem { Id = "screen", Title = "Screen", ScreenType = typeof(TestScreen) };
		item.Select += () => selections++;
		var block = new BlazorApplicationBlockBuilder().WithId("block").WithName("Block").WithDefaultScreen<TestScreen>("Dashboard")
			.AddMenu(menu => menu.WithText("Menu").AddItem(item)).Build();
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(new BlazorApplicationBlockCatalog(new[] { block }), provider);

		var session = await host.ActivateBlockAsync("block");
		Assert.That(session.Title, Is.EqualTo("Dashboard"));
		Assert.That(selections, Is.EqualTo(1));
	}

	[Test]
	public async Task GuardVetoAndCancellationDoNotRaiseSelection() {
		var selections = 0;
		var item = new BlazorScreenMenuItem {
			Id = "screen", Title = "Screen", ScreenType = typeof(TestScreen), ActivationMode = ScreenActivationMode.MultiInstance
		};
		item.Select += () => selections++;
		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(CreateCatalog(item), provider);
		var session = await host.ActivateScreenAsync("block", "screen");
		var component = new TestScreen { AllowDeactivation = false };
		await host.AttachScreenAsync(session.Id, component);

		Assert.That(await host.ExecuteMenuItemAsync("block", "screen"), Is.False);
		Assert.That(selections, Is.EqualTo(1));
		using var cancellation = new CancellationTokenSource();
		cancellation.Cancel();
		Assert.That(async () => await host.ActivateScreenAsync("block", "screen", cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
		Assert.That(selections, Is.EqualTo(1));
	}

	[Test]
	public async Task SuccessfulActionRaisesSelectionAfterTheActionExactlyOnce() {
		var calls = new List<string>();
		var item = new BlazorActionMenuItem {
			Id = "action", Title = "Action", AsyncAction = async (_, _) => {
				await Task.Yield();
				calls.Add("action");
			}
		};
		item.Select += () => calls.Add("select");

		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(CreateCatalog(item), provider);
		Assert.That(await host.ExecuteMenuItemAsync("block", "action"), Is.True);
		Assert.That(calls, Is.EqualTo(new[] { "action", "select" }));
	}

	[Test]
	public void FailedActionDoesNotRaiseSelection() {
		var selections = 0;
		var item = new BlazorActionMenuItem {
			Id = "action", Title = "Action", AsyncAction = (_, _) => Task.FromException(new InvalidOperationException("Failure"))
		};
		item.Select += () => selections++;

		using var provider = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(CreateCatalog(item), provider);
		Assert.That(async () => await host.ExecuteMenuItemAsync("block", "action"), Throws.InstanceOf<InvalidOperationException>());
		Assert.That(selections, Is.Zero);
	}

	[Test]
	public void HoverSnapshotPreservesItsOwnRegistrationTimeInvocationList() {
		var hovers = 0;
		var lateHovers = 0;
		EventHandlerEx hovered = () => hovers++;
		var item = new BlazorScreenMenuItem { Id = "screen", Title = "Screen", ScreenType = typeof(TestScreen) };
		item.Hover += hovered;
		var snapshot = CreateCatalog(item).Get("block").Menus.Single().Items.Single();
		item.Hover -= hovered;
		item.Hover += () => lateHovers++;
		// Exercise the internal UI notification entry point without exposing it as a public consumer API.
		typeof(BlazorApplicationMenuItem).GetMethod("NotifyHover", BindingFlags.Instance | BindingFlags.NonPublic).Invoke(snapshot, null);

		Assert.That(hovers, Is.EqualTo(1));
		Assert.That(lateHovers, Is.Zero);
	}

	[Test]
	public void CatalogFreezesMenuTextAndIconFromMutableRegistrationImplementations() {
		var menu = new MutableMenu { Id = "menu", Text = "Original title", Icon = "original-icon" };
		var catalog = new BlazorApplicationBlockCatalog(new[] {
			new BlazorApplicationBlock { Id = "block", Title = "Block", Menus = new[] { menu } }
		});
		menu.Text = "Changed title";
		menu.Icon = "changed-icon";
		var snapshot = catalog.Get("block").Menus.Single();

		Assert.That(snapshot.Text, Is.EqualTo("Original title"));
		Assert.That(snapshot.Icon, Is.EqualTo("original-icon"));
		Assert.That(typeof(IApplicationMenu).GetProperty(nameof(IApplicationMenu.Text)).CanWrite, Is.False);
		Assert.That(typeof(IBlazorApplicationMenu).GetProperty(nameof(IBlazorApplicationMenu.Icon)).CanWrite, Is.False);
	}

	private static BlazorApplicationBlockCatalog CreateCatalog(IBlazorApplicationMenuItem item) => new(new[] {
		new BlazorApplicationBlockBuilder().WithId("block").WithName("Block").AddMenu(menu => menu.WithText("Menu").AddItem(item)).Build()
	});

	public class TestScreen : ComponentBase, IBlazorApplicationScreen {
		public bool AllowDeactivation { get; set; } = true;

		public Task<bool> CanDeactivateAsync(CancellationToken cancellationToken = default) => Task.FromResult(AllowDeactivation);
	}

	private sealed class MutableMenu : IBlazorApplicationMenu {
		public string Id { get; set; }

		public string Text { get; set; }

		public string Icon { get; set; }

		public IBlazorApplicationMenuItem[] Items => Array.Empty<IBlazorApplicationMenuItem>();
	}
}

