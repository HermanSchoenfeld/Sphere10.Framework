// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Application;
using Sphere10.Framework.Web.AspNetCore.Blazor.Plugins;

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
				.WithDefaultScreen<ExtensionScreen>()
				.ConfigureMenu(menu => menu
					.WithText("Screens")
					.AddScreenItem<ExtensionScreen>("overview", "Overview")
				)
			);
		using var provider = services.BuildServiceProvider();
		var platformUi = provider.GetRequiredService<IBlazorUserInterfaceServices>();
		var sharedUi = provider.GetRequiredService<IUserInterfaceServices>();
		var registeredBlock = provider.GetServices<IApplicationBlock>().Single();
		IApplicationBlock block = sharedUi.ScreenHost.AvailableBlocks.Single(candidate => candidate.Name == "Workspace");
		IApplicationMenu menu = block.Menus.Single();
		IScreenMenuItem screen = (IScreenMenuItem)menu.Items.Single();

		Assert.That(sharedUi, Is.SameAs(platformUi));
		Assert.That(registeredBlock.Name, Is.EqualTo("Workspace"));
		Assert.That(block.Name, Is.EqualTo("Workspace"));
		Assert.That(((IBlazorApplicationBlock)block).Id, Is.EqualTo("workspace"));
		Assert.That(menu.Text, Is.EqualTo("Screens"));
		Assert.That(((IBlazorApplicationMenuItem)screen).Id, Is.EqualTo("overview"));
		Assert.That(typeof(BlazorRoutedApplicationBlock).IsAssignableTo(typeof(IApplicationBlock)), Is.False);
		Assert.That(screen.ScreenDefinition.ScreenType, Is.EqualTo(typeof(ExtensionScreen)));
		Assert.That(screen.ScreenDefinition.ActivationMode, Is.EqualTo(ScreenActivationMode.MultiInstance));
	}

	[Test]
	public void TypedHostMembersDispatchThroughSharedRegistration() {
		var block = new BlazorApplicationBlockBuilder().WithId("workspace").WithName("Workspace").Build();
		var services = new ServiceCollection();
		services.AddScoped<IBlazorApplicationScreenHost>(provider => new BlazorApplicationScreenHost(new[] { block }, provider));
		services.AddSphere10Blazor();
		using var provider = services.BuildServiceProvider();
		var platformHost = provider.GetRequiredService<IBlazorApplicationScreenHost>();
		var sharedHost = provider.GetRequiredService<IApplicationScreenHost>();
		var typedHost = provider.GetRequiredService<IApplicationScreenHost<IBlazorApplicationBlock, BlazorApplicationScreenSession>>();

		Assert.That(sharedHost, Is.SameAs(platformHost));
		Assert.That(typedHost, Is.SameAs(platformHost));
		Assert.That(sharedHost.AvailableBlocks.Single(), Is.SameAs(platformHost.AvailableBlocks.Single()));
		Assert.That(sharedHost.Blocks, Is.EqualTo(platformHost.Blocks));
		Assert.That(((IBlazorApplicationBlock)sharedHost.Blocks.Single()).Id, Is.EqualTo("workspace"));
	}

	[Test]
	public async Task NewBlazorScreenExtensionParticipatesInSharedLifecycle() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = new BlazorApplicationBlockBuilder()
			.WithId("workspace")
			.WithName("Workspace")
			.WithDefaultScreen<ExtensionScreen>()
			.Build();
		using var host = new BlazorApplicationScreenHost(new[] { block }, provider);
		var session = await host.ActivateBlockAsync("workspace");
		var screen = new ExtensionScreen { AllowDeactivation = false };
		await host.AttachScreenAsync(session.Id, screen);

		Assert.That(screen, Is.AssignableTo<IApplicationScreen>());
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
		Assert.That(() => new BlazorApplicationBlockBuilder().WithName("Workspace").WithDefaultScreen(typeof(NeutralScreen)),
			Throws.ArgumentException);
	}

	public class ExtensionScreen : ComponentBase, IBlazorApplicationScreen {
		public IBlazorApplicationScreenHost Host { get; set; }

		public IBlazorApplicationBlock Block { get; set; }

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

	private sealed class NeutralScreen : IApplicationScreen {
		public IApplicationScreenHost Host { get; set; }

		public IApplicationBlock Block { get; set; }

	}
}
