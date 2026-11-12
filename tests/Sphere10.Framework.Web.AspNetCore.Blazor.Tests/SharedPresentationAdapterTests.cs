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
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using BlazorUI = global::Sphere10.Framework.Web.AspNetCore.Blazor;
using SharedUI = global::Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class SharedPresentationAdapterTests {
	[Test]
	public void RegistrationExposesTheSameUiServicesThroughSharedContracts() {
		var services = new ServiceCollection();
		services
			.AddApplicationBlock(block => block
				.WithId("workspace")
				.WithName("Workspace")
				.AddScreen<ExtensionScreen>(screen => screen
					.WithTitle("Overview")
				)
				.WithDefaultScreen<ExtensionScreen>()
				.AddMenu(menu => menu
					.WithText("Screens")
					.AddShowScreenItem<ExtensionScreen>(text: "Overview", id: "overview")
				)
			);
		using var provider = services.BuildServiceProvider();
		var platformUi = provider.GetRequiredService<IBlazorUserInterfaceServices>();
		var sharedUi = provider.GetRequiredService<IUserInterfaceServices>();
		var registeredBlock = provider.GetServices<SharedUI.IApplicationBlock>().Single();
		SharedUI.IApplicationBlock block = sharedUi.ScreenHost.AvailableBlocks.Single(candidate => candidate.Name == "Workspace");
		SharedUI.IApplicationMenu menu = ((BlazorUI.IApplicationBlock)block).Menus.Single();
		IScreenMenuItem screen = (IScreenMenuItem)menu.Items.Single();

		Assert.That(sharedUi, Is.SameAs(platformUi));
		Assert.That(registeredBlock.Name, Is.EqualTo("Workspace"));
		Assert.That(block.Name, Is.EqualTo("Workspace"));
		Assert.That(((BlazorUI.IApplicationBlock)block).Id, Is.EqualTo("workspace"));
		Assert.That(menu.Text, Is.EqualTo("Screens"));
		Assert.That(((BlazorUI.IApplicationMenuItem)screen).Id, Is.EqualTo("overview"));
		Assert.That(screen.ScreenDefinition.ScreenType, Is.EqualTo(typeof(ExtensionScreen)));
		Assert.That(screen.ScreenDefinition.ActivationMode, Is.Null, "Omitted registration policy remains a platform default.");
		Assert.That(((BlazorScreenMenuItem)screen).ActivationMode, Is.EqualTo(ScreenActivationMode.MultiInstance));
	}

	[Test]
	public void TypedHostMembersDispatchThroughSharedRegistration() {
		var block = new ApplicationBlockBuilder()
			.WithId("workspace")
			.WithName("Workspace")
			.Build();
		var services = new ServiceCollection();
		services.AddScoped<BlazorUI.IApplicationScreenHost>(provider => new BlazorUI.ApplicationScreenHost(new[] { block }, provider));
		services.AddSphere10Blazor();
		using var provider = services.BuildServiceProvider();
		var platformHost = provider.GetRequiredService<BlazorUI.IApplicationScreenHost>();
		var sharedHost = provider.GetRequiredService<SharedUI.IApplicationScreenHost>();
		var typedHost = provider.GetRequiredService<SharedUI.IApplicationScreenHost<BlazorUI.IApplicationBlock, ApplicationScreenSession>>();

		Assert.That(sharedHost, Is.SameAs(platformHost));
		Assert.That(typedHost, Is.SameAs(platformHost));
		Assert.That(sharedHost.AvailableBlocks.Single(), Is.SameAs(platformHost.AvailableBlocks.Single()));
		Assert.That(sharedHost.Blocks, Is.EqualTo(platformHost.Blocks));
		Assert.That(((BlazorUI.IApplicationBlock)sharedHost.Blocks.Single()).Id, Is.EqualTo("workspace"));
	}

	[Test]
	public async Task NewBlazorScreenExtensionParticipatesInSharedLifecycle() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = new ApplicationBlockBuilder()
			.WithId("workspace")
			.WithName("Workspace")
			.AddScreen<ExtensionScreen>(screen => screen
				.WithTitle(nameof(ExtensionScreen))
			)
			.WithDefaultScreen<ExtensionScreen>()
			.Build();
		using var host = new BlazorUI.ApplicationScreenHost(new[] { block }, provider);
		var session = await host.ActivateBlockAsync("workspace");
		var screen = new ExtensionScreen { AllowDeactivation = false };
		await host.AttachScreenAsync(session.Id, screen);

		Assert.That(screen, Is.AssignableTo<SharedUI.IApplicationScreen>());
		Assert.That(screen.Activations, Is.EqualTo(1));
		Assert.That(await host.CloseScreenAsync(session.Id), Is.False);
		Assert.That(screen.Deactivations, Is.Zero);
		Assert.That(host.ActiveScreen, Is.SameAs(session));

		screen.AllowDeactivation = true;
		Assert.That(await host.CloseScreenAsync(session.Id), Is.True);
		Assert.That(screen.Deactivations, Is.EqualTo(1));
		Assert.That(host.OpenScreens, Is.Empty);
	}

	[Test]
	public void BlazorBuilderRejectsAPlatformNeutralScreenWithoutComponentSupport() {
		Assert.That(() => new ApplicationBlockBuilder()
			.WithName("Workspace")
			.AddScreen(new BlazorUI.ApplicationScreenDefinition(typeof(NeutralScreen), null, null, ScreenKind.Normal))
			.WithDefaultScreen(typeof(NeutralScreen)),
			Throws.ArgumentException);
	}

	public class ExtensionScreen : ComponentBase, BlazorUI.IApplicationScreen {
		public BlazorUI.IApplicationScreenHost Host { get; set; }

		public BlazorUI.IApplicationBlock Block { get; set; }

		public bool AllowDeactivation { get; set; } = true;

		public int Activations { get; private set; }

		public int Deactivations { get; private set; }

		public Task<bool> CanDeactivateAsync(CancellationToken cancellationToken = default) => Task.FromResult(AllowDeactivation);

		public Task OnActivatedAsync(CancellationToken cancellationToken = default) {
			Activations++;
			return Task.CompletedTask;
		}

		public Task OnDeactivatedAsync(CancellationToken cancellationToken = default) {
			Deactivations++;
			return Task.CompletedTask;
		}
	}

	private sealed class NeutralScreen : SharedUI.IApplicationScreen {
		public SharedUI.IApplicationScreenHost Host { get; set; }

		public SharedUI.IApplicationBlock Block { get; set; }

	}
}
