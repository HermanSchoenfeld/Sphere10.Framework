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
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;
using BlazorUI = global::Sphere10.Framework.Web.AspNetCore.Blazor;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

/// <summary>Protects independent registrations, graph-local monikers and renderer activation preparation.</summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class BlazorScreenRegistrationTests {
	[Test]
	public void ToolbarReferencesCaptureConfiguredDefinitionsWithoutRetargetingIndependentGraphs() {
		var calls = 0;
		var existing = new BlazorActionMenuItem { Id = "existing", Title = "Existing", Action = () => calls++ };
		var builder = new ApplicationBlockBuilder()
			.WithName("Work");
		Assert.That(builder.AddToolBarItem(existing, out var existingReference), Is.SameAs(builder));
		Assert.That(existingReference, Is.SameAs(existing));
		Assert.That(builder.AddToolBarItem(item => item
			.WithId("configured")
			.WithText("Configured")
			.WithAction(() => calls++), out var configuredReference
		), Is.SameAs(builder));
		Assert.That(configuredReference.Id, Is.EqualTo("configured"));

		var first = builder.Build();
		var second = builder.Build();
		Assert.That(calls, Is.Zero);
		Assert.That(first.ToolBarItems.Select(item => item.Id), Is.EqualTo(new[] { "existing", "configured" }));
		Assert.That(second.ToolBarItems[1], Is.Not.SameAs(first.ToolBarItems[1]));
		Assert.That(configuredReference.Parent, Is.Null);
		Assert.That(existingReference.Parent, Is.Null);
		Assert.That(first.ToolBarItems[1].Parent.Parent, Is.SameAs(first));
		Assert.That(second.ToolBarItems[1].Parent.Parent, Is.SameAs(second));
	}

	[Test]
	public async Task SameTypeSingletonsHaveIndependentActivatorsAndCommandsShareOnlyTheirRegistration() {
		var cpuCalls = 0;
		var gpuCalls = 0;
		var builder = new ApplicationBlockBuilder()
			.WithId("inspectors")
			.WithName("Inspectors")
			.AddScreen<ProbeScreen>(screen => screen
				.WithTitle("CPU")
				.WithStableKey("cpu")
				.AsSingleInstance()
				.WithActivator(context => {
					cpuCalls++;
					return new ApplicationScreenActivation(new Dictionary<string, object> { [nameof(ProbeScreen.Name)] = "CPU" });
				}), out var cpu
			)
			.AddScreen<ProbeScreen>(screen => screen
				.WithTitle("GPU")
				.WithStableKey("gpu")
				.AsSingleInstance()
				.WithActivator(context => {
					gpuCalls++;
					return new ApplicationScreenActivation(new Dictionary<string, object> { [nameof(ProbeScreen.Name)] = "GPU" });
				}), out var gpu
			)
			.AddMenu(menu => menu
				.WithText("Inspect")
				.AddShowScreenItem(cpu, id: "cpu")
				.AddShowScreenItem(cpu, "CPU again", "cpu-again")
				.AddShowScreenItem(gpu, id: "gpu")
			);
		var block = builder.Build();
		using var services = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorUI.ApplicationScreenHost(new[] { block }, services);
		Assert.That(cpuCalls + gpuCalls, Is.Zero, "Configuration and graph capture must not execute activators.");

		var cpuSession = await host.ActivateScreenAsync("inspectors", "cpu");
		var gpuSession = await host.ActivateScreenAsync("inspectors", "gpu");
		var cpuAgain = await host.ActivateScreenAsync("inspectors", "cpu-again");
		Assert.That(cpuAgain, Is.SameAs(cpuSession));
		Assert.That(gpuSession, Is.Not.SameAs(cpuSession));
		Assert.That(cpuSession.Parameters[nameof(ProbeScreen.Name)], Is.EqualTo("CPU"));
		Assert.That(gpuSession.Parameters[nameof(ProbeScreen.Name)], Is.EqualTo("GPU"));
		Assert.That(cpuCalls, Is.EqualTo(1));
		Assert.That(gpuCalls, Is.EqualTo(1));
		Assert.That(host.Blocks.Single().Menus.Single().Items.First().Title, Is.EqualTo("CPU"));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task ExplicitSingletonDoesNotChangeAnUnspecifiedSiblingPolicy(bool defaultFirst) {
		var block = new ApplicationBlockBuilder()
			.WithId("work")
			.WithName("Work")
			.AddScreen<ProbeScreen>(screen => screen
				.WithTitle("Singleton")
				.AsSingleInstance(), out var singleton
			)
			.AddScreen<ProbeScreen>(screen => screen.WithTitle("Platform default"), out var ordinary)
			.Build();
		using var services = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorUI.ApplicationScreenHost(new[] { block }, services);
		var owner = host.Blocks.Single();
		var first = await host.OpenScreenAsync(owner, defaultFirst ? ordinary : singleton);
		var second = await host.OpenScreenAsync(owner, defaultFirst ? singleton : ordinary);
		var ordinaryAgain = await host.OpenScreenAsync(owner, ordinary);
		var singletonAgain = await host.OpenScreenAsync(owner, singleton);
		Assert.That(ordinaryAgain, Is.Not.SameAs(defaultFirst ? first : second));
		Assert.That(singletonAgain, Is.SameAs(defaultFirst ? second : first));
		Assert.That(ordinaryAgain.ActivationMode, Is.EqualTo(ScreenActivationMode.MultiInstance));
		Assert.That(singletonAgain.ActivationMode, Is.EqualTo(ScreenActivationMode.SingleInstance));
	}

	[Test]
	public async Task RetainedMonikerResolvesWithinEachIndependentGraphAndCircuitServices() {
		var calls = 0;
		var builder = new ApplicationBlockBuilder()
			.WithName("Work")
			.AddScreen<ProbeScreen>(screen => screen
				.AsSingleInstance()
				.WithActivator(context => {
					calls++;
					return new ApplicationScreenActivation(new Dictionary<string, object> { [nameof(ProbeScreen.Name)] = context.Services.GetRequiredService<Marker>().Name });
				}), out var reference
			);
		var firstGraph = builder.Build();
		var secondGraph = builder.Build();
		using var firstServices = new ServiceCollection().AddSingleton(new Marker("First")).BuildServiceProvider();
		using var secondServices = new ServiceCollection().AddSingleton(new Marker("Second")).BuildServiceProvider();
		using var firstHost = new BlazorUI.ApplicationScreenHost(new[] { firstGraph }, firstServices);
		using var secondHost = new BlazorUI.ApplicationScreenHost(new[] { secondGraph }, secondServices);
		var first = await firstHost.OpenScreenAsync(firstHost.Blocks.Single(), reference);
		var second = await secondHost.OpenScreenAsync(secondHost.Blocks.Single(), reference);
		Assert.That(first, Is.Not.SameAs(second));
		Assert.That(first.Definition, Is.SameAs(firstHost.Blocks.Single().ScreenDefinitions.Single()));
		Assert.That(second.Definition, Is.SameAs(secondHost.Blocks.Single().ScreenDefinitions.Single()));
		Assert.That(first.Definition, Is.Not.SameAs(second.Definition));
		Assert.That(first.Parameters[nameof(ProbeScreen.Name)], Is.EqualTo("First"));
		Assert.That(second.Parameters[nameof(ProbeScreen.Name)], Is.EqualTo("Second"));
		Assert.That(await firstHost.OpenScreenAsync(firstHost.Blocks.Single(), reference), Is.SameAs(first));
		Assert.That(calls, Is.EqualTo(2));
		Assert.That(firstGraph.ScreenDefinitions.Single().Activators, Is.Empty);
		Assert.That(secondGraph.ScreenDefinitions.Single().Activators, Is.Empty);
	}

	[TestCase(false)]
	[TestCase(true)]
	public void TypeMenuSelectsFirstRegistrationWhileMonikerSelectsExactRegistration(bool runtimeType) {
		var block = new ApplicationBlockBuilder()
			.WithName("Work")
			.AddScreen<ProbeScreen>(screen => screen.WithTitle("First"), out var first)
			.AddScreen<ProbeScreen>(screen => screen.WithTitle("Second"), out var second)
			.AddMenu(menu => {
				menu.WithText("Screens");
				if (runtimeType)
					menu.AddShowScreenItem(typeof(ProbeScreen), id: "type");
				else
					menu.AddShowScreenItem<ProbeScreen>(id: "type");
				menu.AddShowScreenItem(second, id: "exact");
			})
			.Build();
		var items = block.Menus.Single().Items.Cast<BlazorScreenMenuItem>().ToArray();
		Assert.That(items[0].ScreenDefinition.Registration.Token, Is.EqualTo(first.Token));
		Assert.That(items[0].Title, Is.EqualTo("First"));
		Assert.That(items[1].ScreenDefinition.Registration.Token, Is.EqualTo(second.Token));
		Assert.That(items[1].Title, Is.EqualTo("Second"));
	}

	[Test]
	public async Task MenuFreeRegistrationHasAStableRouteAcrossBuildOrderAndCaptionChanges() {
		var firstBlock = new ApplicationBlockBuilder()
			.WithName("Work")
			.AddScreen<ProbeScreen>(screen => screen.WithTitle("Original"), out var reference)
			.Build();
		using var services = new ServiceCollection().BuildServiceProvider();
		using var firstHost = new BlazorUI.ApplicationScreenHost(new[] { firstBlock }, services);
		var first = await firstHost.OpenScreenAsync(firstHost.Blocks.Single(), reference);
		var route = first.MenuItemId;
		var secondBlock = new ApplicationBlockBuilder()
			.WithName("Work")
			.AddScreen<OtherScreen>(screen => screen.WithTitle("Inserted"))
			.AddScreen<ProbeScreen>(screen => screen.WithTitle("Renamed"))
			.Build();
		using var secondHost = new BlazorUI.ApplicationScreenHost(new[] { secondBlock }, services);
		var restored = await secondHost.ActivateScreenAsync("Work", route);
		Assert.That(restored.ScreenType, Is.EqualTo(typeof(ProbeScreen)));
		Assert.That(restored.Title, Is.EqualTo("Renamed"));
		Assert.That(restored.MenuItem, Is.Null, "Programmatic opening must not fabricate navigation commands.");
		Assert.That(restored.Definition.Activators, Is.Empty);
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task FailedOrCancelledPreparationDoesNotRetainASession(bool cancel) {
		var calls = 0;
		var block = new ApplicationBlockBuilder()
			.WithName("Work")
			.AddScreen<ProbeScreen>(screen => screen.WithActivator(context => {
				calls++;
				return null;
			}), out var reference)
			.Build();
		using var services = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorUI.ApplicationScreenHost(new[] { block }, services);
		using var cancellation = new CancellationTokenSource();
		if (cancel)
			cancellation.Cancel();
		await Assert.ThatAsync(() => host.OpenScreenAsync(host.Blocks.Single(), reference, cancellation.Token),
			cancel ? Throws.InstanceOf<OperationCanceledException>() : Throws.InstanceOf<InvalidOperationException>());
		Assert.That(host.Screens, Is.Empty);
		Assert.That(calls, Is.EqualTo(cancel ? 0 : 1));
		Assert.That(host.Blocks.Single().ScreenDefinitions.Single().Activators, Is.Empty);
	}

	[Test]
	public async Task AutomaticallyNamedMonikerCommandsUseTheirStableRegistrationRoute() {
		var firstBlock = new ApplicationBlockBuilder()
			.WithName("Work")
			.AddScreen<ProbeScreen>(screen => screen.WithStableKey("inspector"), out var reference)
			.AddMenu(menu => menu
				.WithText("Screens")
				.AddShowScreenItem(reference)
			)
			.Build();
		using var services = new ServiceCollection().BuildServiceProvider();
		using var firstHost = new BlazorUI.ApplicationScreenHost(new[] { firstBlock }, services);
		var first = await firstHost.ActivateScreenAsync("Work", firstHost.Blocks.Single().Menus.Single().Items.Single().Id);
		var route = first.MenuItemId;
		Assert.That(route, Is.EqualTo("__screen:inspector"));
		var secondBlock = new ApplicationBlockBuilder()
			.WithName("Work")
			.AddScreen<ProbeScreen>(screen => screen.WithStableKey("inspector"), out var secondReference)
			.AddMenu(menu => menu
				.WithText("Screens")
				.AddShowScreenItem(secondReference)
			)
			.Build();
		using var secondHost = new BlazorUI.ApplicationScreenHost(new[] { secondBlock }, services);
		Assert.That((await secondHost.ActivateScreenAsync("Work", route)).ScreenType, Is.EqualTo(typeof(ProbeScreen)));
		Assert.That(reference.Token, Is.Not.EqualTo(secondReference.Token));
	}

	[Test]
	public async Task LegacyNumericStartupRouteUsesUniqueRegisteredTypeRatherThanCurrentOrder() {
		var block = new ApplicationBlockBuilder()
			.WithName("Work")
			.AddScreen<OtherScreen>(screen => screen.WithTitle("Inserted first"))
			.AddScreen<ProbeScreen>(screen => screen.WithTitle("Original choice"))
			.Build();
		using var services = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorUI.ApplicationScreenHost(new[] { block }, services);
		var legacy = new ApplicationStartupScreen("", "Work", "__screen:0", typeof(ProbeScreen).FullName, typeof(ProbeScreen).Assembly.GetName().Name);
		Assert.That(host.SetStartupScreen(legacy), Is.True);
		await host.InitializeAsync();
		Assert.That(host.ActiveScreen.ScreenType, Is.EqualTo(typeof(ProbeScreen)));
	}

	[Test]
	public async Task ActivatorCannotIntroduceAReuseKeyAfterTheHostHasResolvedInstanceIdentity() {
		var block = new ApplicationBlockBuilder()
			.WithName("Work")
			.AddScreen<ProbeScreen>(screen => screen.WithActivator(context => new ApplicationScreenActivation(instanceKey: "late-key")), out var reference)
			.Build();
		using var services = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorUI.ApplicationScreenHost(new[] { block }, services);
		await Assert.ThatAsync(() => host.OpenScreenAsync(host.Blocks.Single(), reference), Throws.InvalidOperationException.With.Message.Contains("reuse keys"));
		Assert.That(host.Screens, Is.Empty);
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task FailingOrCancellingActivatorPreservesTheCurrentScreenLifecycle(bool cancel) {
		using var cancellation = new CancellationTokenSource();
		var block = new ApplicationBlockBuilder()
			.WithName("Work")
			.AddScreen<ProbeScreen>(screen => screen.AsSingleInstance(), out var original)
			.AddScreen<OtherScreen>(screen => screen.WithActivator(context => {
				if (!cancel)
					throw new InvalidOperationException("Factory failed.");
				cancellation.Cancel();
				return new ApplicationScreenActivation();
			}), out var failing)
			.Build();
		using var services = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorUI.ApplicationScreenHost(new[] { block }, services);
		var owner = host.Blocks.Single();
		var session = await host.OpenScreenAsync(owner, original);
		await using var component = new ProbeScreen();
		await host.AttachScreenAsync(session.Id, component);
		await Assert.ThatAsync(() => host.OpenScreenAsync(owner, failing, cancellation.Token),
			cancel ? Throws.InstanceOf<OperationCanceledException>() : Throws.InstanceOf<InvalidOperationException>());
		Assert.That(host.Screens, Is.EqualTo(new[] { session }));
		Assert.That(host.ActiveScreen, Is.SameAs(session));
		Assert.That(component.Deactivations, Is.Zero);
		Assert.That(component.Host, Is.SameAs(host));
		Assert.That(component.Block, Is.SameAs(owner));
	}

	[Test]
	public async Task CancelledKeyedCreationCanRetryWithoutReusingAnUnretainedSession() {
		using var cancellation = new CancellationTokenSource();
		var calls = 0;
		var block = new ApplicationBlockBuilder()
			.WithName("Work")
			.AddScreen<ProbeScreen>(screen => screen
				.AsMultiInstance()
				.WithActivator(context => {
					if (++calls == 1)
						cancellation.Cancel();
					return new ApplicationScreenActivation();
				})
			)
			.Build();
		using var services = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorUI.ApplicationScreenHost(new[] { block }, services);
		var owner = host.Blocks.Single();
		var definition = owner.ScreenDefinitions.Single();
		var activation = new ApplicationScreenActivation(instanceKey: "document");
		await Assert.ThatAsync(() => host.OpenScreenAsync(owner, definition, activation, cancellation.Token),
			Throws.InstanceOf<OperationCanceledException>());
		Assert.That(host.Screens, Is.Empty);

		var session = await host.OpenScreenAsync(owner, definition, activation);
		Assert.That(calls, Is.EqualTo(2));
		Assert.That(host.Screens, Is.EqualTo(new[] { session }));
		Assert.That(session.Host, Is.SameAs(host));
		Assert.That(await host.OpenScreenAsync(owner, definition, activation), Is.SameAs(session));
		Assert.That(calls, Is.EqualTo(2));
	}

	private sealed record Marker(string Name);

	public class ProbeScreen : ApplicationScreen {
		[Parameter]
		public string Name { get; set; }

		public int Deactivations { get; private set; }

		protected override Task OnDeactivatedAsync(CancellationToken cancellationToken) {
			Deactivations++;
			return Task.CompletedTask;
		}
	}

	public sealed class OtherScreen : ProbeScreen {
	}
}
