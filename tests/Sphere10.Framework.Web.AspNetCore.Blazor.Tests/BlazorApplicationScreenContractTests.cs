// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class BlazorApplicationScreenContractTests {
	[Test]
	public async Task ExplicitSharedLifecycleDispatchesThroughSharedAndBlazorInterfaces() {
		var screen = new ExplicitScreen { IsDirty = true, AllowDeactivation = false };
		IApplicationScreen sharedScreen = screen;
		IBlazorApplicationScreen blazorScreen = screen;

		Assert.That(sharedScreen.HasUnsavedChanges, Is.True);
		Assert.That(blazorScreen.HasUnsavedChanges, Is.True);
		Assert.That(await sharedScreen.CanDeactivateAsync(), Is.False);
		Assert.That(await blazorScreen.CanDeactivateAsync(), Is.False);
		await sharedScreen.OnActivatedAsync();
		await blazorScreen.OnActivatedAsync();
		await sharedScreen.OnDeactivatedAsync();
		await blazorScreen.OnDeactivatedAsync();

		Assert.That(screen.GuardCalls, Is.EqualTo(2));
		Assert.That(screen.Activations, Is.EqualTo(2));
		Assert.That(screen.Deactivations, Is.EqualTo(2));
	}

	[TestCase(true)]
	[TestCase(false)]
	public async Task HostHonorsExplicitAndPublicSharedScreenLifecycle(bool useExplicitContract) {
		var screen = useExplicitContract ? (ProbeScreen)new ExplicitScreen() : new PublicScreen();
		screen.IsDirty = true;
		screen.AllowDeactivation = false;
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = new BlazorApplicationBlockBuilder().WithId("workspace").WithName("Workspace").WithDefaultScreen(screen.GetType()).Build();
		using var host = new BlazorApplicationScreenHost(new BlazorApplicationBlockCatalog(new[] { block }), provider);
		var session = await host.ActivateBlockAsync("workspace");
		await host.AttachScreenAsync(session.Id, (IBlazorApplicationScreen)screen);

		Assert.That(host.HasUnsavedChanges, Is.True);
		Assert.That(screen.Activations, Is.EqualTo(1));
		Assert.That(await host.CanNavigateAsync(), Is.False);
		Assert.That(await host.CloseScreenAsync(session.Id), Is.False);
		Assert.That(screen.GuardCalls, Is.EqualTo(2));
		Assert.That(screen.Deactivations, Is.Zero);
		Assert.That(host.ActiveScreen, Is.SameAs(session));

		screen.IsDirty = false;
		screen.AllowDeactivation = true;
		Assert.That(host.HasUnsavedChanges, Is.False);
		Assert.That(await host.CloseScreenAsync(session.Id), Is.True);
		Assert.That(screen.Deactivations, Is.EqualTo(1));
		Assert.That(host.OpenScreens, Is.Empty);
	}

	[Test]
	public async Task ComponentBaseProtectedHooksDispatchThroughBlazorAndSharedContracts() {
		await using var screen = new DerivedScreen();
		IBlazorApplicationScreen blazorScreen = screen;
		IApplicationScreen sharedScreen = screen;

		Assert.That(blazorScreen.HasUnsavedChanges, Is.True);
		Assert.That(sharedScreen.HasUnsavedChanges, Is.True);
		Assert.That(await blazorScreen.CanDeactivateAsync(), Is.False);
		Assert.That(await sharedScreen.CanDeactivateAsync(), Is.False);
		await blazorScreen.OnActivatedAsync();
		await sharedScreen.OnActivatedAsync();
		await blazorScreen.OnDeactivatedAsync();
		await sharedScreen.OnDeactivatedAsync();

		Assert.That(screen.GuardCalls, Is.EqualTo(2));
		Assert.That(screen.Activations, Is.EqualTo(2));
		Assert.That(screen.Deactivations, Is.EqualTo(2));
	}

	public abstract class ProbeScreen : ComponentBase {
		public bool IsDirty { get; set; }

		public bool AllowDeactivation { get; set; } = true;

		public int GuardCalls { get; private set; }

		public int Activations { get; private set; }

		public int Deactivations { get; private set; }

		protected Task<bool> CheckGuardAsync() {
			GuardCalls++;
			return Task.FromResult(AllowDeactivation);
		}

		protected Task ActivateAsync() {
			Activations++;
			return Task.CompletedTask;
		}

		protected Task DeactivateAsync() {
			Deactivations++;
			return Task.CompletedTask;
		}
	}

	public sealed class ExplicitScreen : ProbeScreen, IBlazorApplicationScreen {
		bool IApplicationScreen.HasUnsavedChanges => IsDirty;

		Task<bool> IApplicationScreen.CanDeactivateAsync(CancellationToken cancellationToken) => CheckGuardAsync();

		Task IApplicationScreen.OnActivatedAsync(CancellationToken cancellationToken) => ActivateAsync();

		Task IApplicationScreen.OnDeactivatedAsync(CancellationToken cancellationToken) => DeactivateAsync();
	}

	public sealed class PublicScreen : ProbeScreen, IBlazorApplicationScreen {
		public bool HasUnsavedChanges => IsDirty;

		public Task<bool> CanDeactivateAsync(CancellationToken cancellationToken) => CheckGuardAsync();

		public Task OnActivatedAsync(CancellationToken cancellationToken) => ActivateAsync();

		public Task OnDeactivatedAsync(CancellationToken cancellationToken) => DeactivateAsync();
	}

	private sealed class DerivedScreen : BlazorApplicationScreen {
		public override bool HasUnsavedChanges => true;

		public int GuardCalls { get; private set; }

		public int Activations { get; private set; }

		public int Deactivations { get; private set; }

		protected override Task<bool> CanDeactivateAsync(CancellationToken cancellationToken) {
			GuardCalls++;
			return Task.FromResult(false);
		}

		protected override Task OnActivatedAsync(CancellationToken cancellationToken) {
			Activations++;
			return Task.CompletedTask;
		}

		protected override Task OnDeactivatedAsync(CancellationToken cancellationToken) {
			Deactivations++;
			return Task.CompletedTask;
		}
	}
}
