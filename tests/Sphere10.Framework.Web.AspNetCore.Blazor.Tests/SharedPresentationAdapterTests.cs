// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Web.AspNetCore.Blazor.Plugins;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class SharedPresentationAdapterTests {
	[Test]
	public void RegistrationExposesTheSameCatalogThroughSharedContracts() {
		var services = new ServiceCollection();
		services.AddApplicationBlock(block => block.WithId("workspace").WithName("Workspace")
			.WithDefaultScreen<ExtensionScreen>()
			.AddMenu(menu => menu.WithText("Screens").AddScreenItem<ExtensionScreen>("overview", "Overview")));
		using var provider = services.BuildServiceProvider();
		var platformCatalog = provider.GetRequiredService<IBlazorApplicationBlockCatalog>();
		var sharedCatalog = provider.GetRequiredService<IApplicationBlockCatalog<IApplicationBlock>>();
		var registeredBlock = provider.GetServices<IApplicationBlock>().Single();
		IApplicationBlock block = sharedCatalog.Get("workspace");
		IApplicationMenu menu = block.Menus.Single();
		IScreenMenuItem screen = (IScreenMenuItem)menu.Items.Single();

		Assert.That(sharedCatalog, Is.SameAs(platformCatalog));
		Assert.That(registeredBlock.Name, Is.EqualTo("Workspace"));
		Assert.That(block.Name, Is.EqualTo("Workspace"));
		Assert.That(block.Id, Is.EqualTo("workspace"));
		Assert.That(menu.Text, Is.EqualTo("Screens"));
		Assert.That(screen.Id, Is.EqualTo("overview"));
		Assert.That(typeof(BlazorRoutedApplicationBlock).IsAssignableTo(typeof(IApplicationBlock)), Is.False);
		Assert.That(screen.ScreenType, Is.EqualTo(typeof(ExtensionScreen)));
		Assert.That(screen.ActivationMode, Is.EqualTo(ScreenActivationMode.MultiInstance));
	}

	[Test]
	public void ExplicitPlatformCatalogMembersDispatchThroughSharedRegistration() {
		var block = new BlazorApplicationBlockBuilder().WithId("workspace").WithName("Workspace").Build();
		var catalog = new ExplicitPlatformCatalog(block);
		var services = new ServiceCollection();
		services.AddSingleton<IBlazorApplicationBlockCatalog>(catalog);
		services.AddSphere10Blazor();
		using var provider = services.BuildServiceProvider();
		var platformCatalog = provider.GetRequiredService<IBlazorApplicationBlockCatalog>();
		var sharedCatalog = provider.GetRequiredService<IApplicationBlockCatalog<IApplicationBlock>>();

		Assert.That(platformCatalog, Is.SameAs(catalog));
		Assert.That(sharedCatalog, Is.SameAs(catalog));
		Assert.That(platformCatalog.Get("workspace"), Is.SameAs(block));
		Assert.That(sharedCatalog.Get("workspace"), Is.SameAs(block));
		Assert.That(sharedCatalog.Blocks, Is.EqualTo(platformCatalog.Blocks));
		Assert.That(sharedCatalog.Blocks.Single(), Is.SameAs(block));
	}

	[Test]
	public async Task NewBlazorScreenExtensionParticipatesInSharedLifecycle() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var block = new BlazorApplicationBlockBuilder().WithId("workspace").WithName("Workspace").WithDefaultScreen<ExtensionScreen>().Build();
		using var host = new BlazorApplicationScreenHost(new BlazorApplicationBlockCatalog(new[] { block }), provider);
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

	private sealed class ExplicitPlatformCatalog : IBlazorApplicationBlockCatalog {
		private readonly IBlazorApplicationBlock[] _blocks;

		public ExplicitPlatformCatalog(IBlazorApplicationBlock block) {
			_blocks = new[] { block };
		}

		IBlazorApplicationBlock[] IBlazorApplicationBlockCatalog.Blocks => Tools.Array.Clone(_blocks);

		IBlazorApplicationBlock IBlazorApplicationBlockCatalog.Get(string id) => _blocks.Single(block => block.Id == id);
	}

	private sealed class NeutralScreen : IApplicationScreen {
	}
}
