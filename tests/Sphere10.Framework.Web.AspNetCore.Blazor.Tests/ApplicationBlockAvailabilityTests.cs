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
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Web.AspNetCore.Blazor.UI.Application;
using BlazorUI = global::Sphere10.Framework.Web.AspNetCore.Blazor;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

/// <summary>Exercises block availability, which each circuit's screen host decides once from its own scoped services.</summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationBlockAvailabilityTests {
	/// <summary>Verifies each circuit decides once from its own scoped services, after the predicate has survived every startup copy.</summary>
	[Test]
	public async Task EachCircuitDecidesItsBlocksOnceFromItsOwnScopedServices() {
		var evaluations = 0;
		await using var provider = CreateServices(services => {
			evaluations++;
			return services.GetRequiredService<Viewer>().IsAdministrator;
		}).BuildServiceProvider();
		Assert.That(evaluations, Is.Zero, "Startup composition and plugin registration must not decide availability.");
		await using var administrator = provider.CreateAsyncScope();
		administrator.ServiceProvider.GetRequiredService<Viewer>().IsAdministrator = true;
		await using var customer = provider.CreateAsyncScope();

		var administratorHost = administrator.ServiceProvider.GetRequiredService<BlazorUI.IApplicationScreenHost>();
		var customerHost = customer.ServiceProvider.GetRequiredService<BlazorUI.IApplicationScreenHost>();
		var customerApplication = customer.ServiceProvider.GetRequiredService<BlazorUI.IApplication>();
		await administratorHost.InitializeAsync();
		await customerHost.InitializeAsync();

		Assert.That(administratorHost.Blocks.Select(block => block.Id), Is.EqualTo(new[] { "administration", "customer" }));
		Assert.That(customerHost.Blocks.Select(block => block.Id), Is.EqualTo(new[] { "customer" }));
		Assert.That(customerHost.AvailableBlocks.Select(block => block.Id), Is.EqualTo(new[] { "customer" }));
		Assert.That(customerApplication.Blocks.Select(block => block.Id), Is.EqualTo(new[] { "customer" }));
		Assert.That(evaluations, Is.EqualTo(2), "Each circuit asks once; initialization and membership reads must not ask again.");
	}

	/// <summary>Verifies startup falls back to the first available block's default and never opens an excluded block's permanent screens.</summary>
	[Test]
	public async Task StartupSkipsAnExcludedFirstBlockAndItsPermanentScreens() {
		await using var provider = CreateServices(services => services.GetRequiredService<Viewer>().IsAdministrator).BuildServiceProvider();
		await using var administrator = provider.CreateAsyncScope();
		administrator.ServiceProvider.GetRequiredService<Viewer>().IsAdministrator = true;
		await using var customer = provider.CreateAsyncScope();
		var administratorHost = administrator.ServiceProvider.GetRequiredService<BlazorUI.IApplicationScreenHost>();
		var customerHost = customer.ServiceProvider.GetRequiredService<BlazorUI.IApplicationScreenHost>();

		await administratorHost.InitializeAsync();
		await customerHost.InitializeAsync();

		Assert.That(administratorHost.ActiveScreen.ScreenType, Is.EqualTo(typeof(AdministrationScreen)));
		Assert.That(administratorHost.OpenScreens.Select(screen => screen.ScreenType), Does.Contain(typeof(AuditScreen)));
		Assert.That(customerHost.ActiveScreen.ScreenType, Is.EqualTo(typeof(CustomerScreen)));
		Assert.That(customerHost.SelectedBlock.Id, Is.EqualTo("customer"));
		Assert.That(customerHost.Screens.Select(screen => screen.ScreenType), Is.EqualTo(new[] { typeof(CustomerScreen) }));
	}

	/// <summary>Verifies the shell's dock, sidebar and Screens menu present only the blocks this circuit is offered.</summary>
	[Test]
	public async Task ShellPresentsOnlyTheBlocksOfferedToItsCircuit() {
		await using var provider = CreateServices(_ => false).BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var host = provider.GetRequiredService<BlazorUI.IApplicationScreenHost>();
		await renderer.Dispatcher.InvokeAsync(async () => {
			ApplicationShell shell = null;
			var rendered = await renderer.RenderComponentAsync<ShellHarness>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(ShellHarness.Capture)] = (Action<ApplicationShell>)(instance => shell = instance)
			}));
			var html = WebUtility.HtmlDecode(rendered.ToHtmlString());
			var menus = (BlazorUI.IApplicationMenu[])typeof(ApplicationShell).GetProperty("MergedMenus", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(shell);
			var openScreens = menus.Single(menu => menu.Id == "sphere10-screens-menu").Items.OfType<BlazorUI.IApplicationCheckableMenuItem>();

			Assert.That(host.ActiveScreen.ScreenType, Is.EqualTo(typeof(CustomerScreen)));
			Assert.That(html, Does.Contain("Customer block").And.Contain("Customer menus"));
			Assert.That(html, Does.Not.Contain("Administration").And.Not.Contain("administration").And.Not.Contain("Audit trail"));
			Assert.That(openScreens.Select(item => item.Title), Is.EqualTo(new[] { "Customer home" }));
		});
	}

	/// <summary>Verifies a route to an excluded block answers exactly as a route to an unknown block.</summary>
	[TestCase("administration")]
	[TestCase("never-registered")]
	public async Task RouteToAnExcludedBlockAnswersAsAnUnknownBlock(string blockId) {
		await using var provider = CreateServices(_ => false).BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var host = provider.GetRequiredService<BlazorUI.IApplicationScreenHost>();
		await renderer.Dispatcher.InvokeAsync(async () => {
			var rendered = await renderer.RenderComponentAsync<ShellHarness>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(ShellHarness.Capture)] = (Action<ApplicationShell>)(_ => { }),
				[nameof(ShellHarness.BlockId)] = blockId
			}));
			var html = WebUtility.HtmlDecode(rendered.ToHtmlString());

			Assert.That(html, Does.Contain("The requested application block was not found."));
			Assert.That(html, Does.Not.Contain("Administration"));
			Assert.That(host.ActiveScreen.ScreenType, Is.EqualTo(typeof(CustomerScreen)));
			Assert.That(host.Screens.Select(screen => screen.Block.Id), Is.EqualTo(new[] { "customer" }));
		});
	}

	/// <summary>Verifies a circuit without any available block renders the shell's existing empty state.</summary>
	[Test]
	public async Task ACircuitWithoutAnAvailableBlockShowsTheEmptyWorkspace() {
		await using var provider = CreateServices(_ => false, _ => false).BuildServiceProvider();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		var host = provider.GetRequiredService<BlazorUI.IApplicationScreenHost>();
		await renderer.Dispatcher.InvokeAsync(async () => {
			var rendered = await renderer.RenderComponentAsync<ShellHarness>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(ShellHarness.Capture)] = (Action<ApplicationShell>)(_ => { })
			}));
			var html = WebUtility.HtmlDecode(rendered.ToHtmlString());

			Assert.That(html, Does.Contain("Select an application block to open a screen."));
			Assert.That(html, Does.Not.Contain("Administration").And.Not.Contain("Customer"));
			Assert.That(host.AvailableBlocks, Is.Empty);
			Assert.That(host.Blocks, Is.Empty);
			Assert.That(host.ActiveScreen, Is.Null);
			Assert.That(host.Screens, Is.Empty);
		});
	}

	/// <summary>Verifies a failing predicate fails the circuit's host with its own exception.</summary>
	[Test]
	public async Task AFailingPredicateFailsTheCircuitHost() {
		await using var provider = CreateServices(_ => throw new InvalidOperationException("Entitlements are unavailable.")).BuildServiceProvider();
		await using var circuit = provider.CreateAsyncScope();

		Assert.That(() => circuit.ServiceProvider.GetRequiredService<BlazorUI.IApplicationScreenHost>(),
			Throws.TypeOf<InvalidOperationException>().With.Message.EqualTo("Entitlements are unavailable."));
	}

	/// <summary>Verifies neither the block's ID nor its startup definition can register an excluded block into the circuit.</summary>
	[Test]
	public async Task AnExcludedBlockCannotBeRegisteredIntoTheCircuit() {
		var evaluations = 0;
		await using var provider = CreateServices(_ => {
			evaluations++;
			return false;
		}).BuildServiceProvider();
		await using var circuit = provider.CreateAsyncScope();
		var host = circuit.ServiceProvider.GetRequiredService<BlazorUI.IApplicationScreenHost>();
		var startupDefinition = provider.GetServices<BlazorUI.IApplicationBlock>().Single(block => block.Id == "administration");
		await host.InitializeAsync();

		await Assert.ThatAsync(async () => await host.RegisterBlockAsync("administration"), Throws.ArgumentException);
		await Assert.ThatAsync(async () => await host.RegisterBlockAsync(startupDefinition), Throws.ArgumentException);
		await Assert.ThatAsync(async () => await host.ActivateBlockAsync("administration"), Throws.ArgumentException);

		Assert.That(host.Blocks.Select(block => block.Id), Is.EqualTo(new[] { "customer" }));
		Assert.That(host.AvailableBlocks.Select(block => block.Id), Is.EqualTo(new[] { "customer" }));
		Assert.That(evaluations, Is.EqualTo(1));
	}

	/// <summary>Verifies Options is offered for a block's preferences only in circuits that are offered the block.</summary>
	[TestCase(true)]
	[TestCase(false)]
	public async Task OptionsCoverOnlyThePreferencesOfAvailableBlocks(bool available) {
		await using var provider = CreateServices(_ => available, administrationSettings: true).BuildServiceProvider();
		var application = provider.GetRequiredService<BlazorUI.IApplication>();

		await application.InitializeAsync();

		Assert.That(application.ScreenHost.AvailableBlocks.Any(block => block.BlockSettings != null), Is.EqualTo(available));
		Assert.That(application.Menus.SelectMany(menu => menu.Items).Any(item => item.Title == "Screen Options..."), Is.EqualTo(available));
	}

	/// <summary>Verifies a directly constructed delegate-taking block declares the same availability, and that each circuit's copy keeps deferring to it.</summary>
	[Test]
	public void ADirectlyConstructedActionBlockDeclaresItsOwnAvailability() {
		using var accepted = new ServiceCollection().BuildServiceProvider();
		using var declined = new ServiceCollection().BuildServiceProvider();
		var block = new BlazorUI.ActionApplicationBlock(services => ReferenceEquals(services, accepted)) { Id = "direct", Title = "Direct block" };
		using var acceptedHost = new BlazorUI.ApplicationScreenHost(new BlazorUI.IApplicationBlock[] { block }, accepted);
		using var declinedHost = new BlazorUI.ApplicationScreenHost(new BlazorUI.IApplicationBlock[] { block }, declined);

		Assert.That(acceptedHost.Blocks.Single().Id, Is.EqualTo("direct"));
		Assert.That(acceptedHost.Blocks.Single(), Is.Not.SameAs(block), "The circuit still owns a copy of an available block.");
		Assert.That(acceptedHost.Blocks.Single().CheckAvailability(accepted), Is.True, "A copy defers to its source's decision.");
		Assert.That(acceptedHost.Blocks.Single().CheckAvailability(declined), Is.False, "A copy defers to its source's decision.");
		Assert.That(declinedHost.AvailableBlocks, Is.Empty);
	}

	/// <summary>Verifies the web block's template guards its provider, offers an ordinary block to every circuit and rejects a missing delegate.</summary>
	[Test]
	public void TheWebBlockTemplateGuardsItsProviderAndAnActionBlockRequiresItsDelegate() {
		using var provider = new ServiceCollection().BuildServiceProvider();
		var ordinary = new BlazorUI.ApplicationBlock { Id = "ordinary", Title = "Ordinary block" };
		var asked = new List<IServiceProvider>();
		var action = new BlazorUI.ActionApplicationBlock(services => {
			asked.Add(services);
			return false;
		}) { Id = "action", Title = "Action block" };

		Assert.That(ordinary.CheckAvailability(provider), Is.True);
		Assert.That(action.CheckAvailability(provider), Is.False);
		Assert.That(asked, Is.EqualTo(new IServiceProvider[] { provider }), "The hook is asked with the supplied provider.");
		Assert.That(() => ordinary.CheckAvailability(null), Throws.ArgumentNullException);
		Assert.That(() => action.CheckAvailability(null), Throws.ArgumentNullException);
		Assert.That(asked, Has.Count.EqualTo(1), "The delegate is never run without a provider.");
		Assert.That(() => new BlazorUI.ActionApplicationBlock(null), Throws.ArgumentNullException);
	}

	/// <summary>Verifies the builder's built block carries its predicate without evaluating it, and a block built without one is offered to every circuit.</summary>
	[Test]
	public void TheBuilderCarriesItsPredicateIntoTheBuiltBlockWithoutEvaluatingIt() {
		using var accepted = new ServiceCollection().BuildServiceProvider();
		using var declined = new ServiceCollection().BuildServiceProvider();
		var evaluations = 0;
		var restricted = new BlazorUI.ApplicationBlockBuilder()
			.WithId("restricted")
			.WithName("Restricted block")
			.AvailableWhen(services => {
				evaluations++;
				return ReferenceEquals(services, accepted);
			})
			.Build();
		var open = new BlazorUI.ApplicationBlockBuilder()
			.WithId("open")
			.WithName("Open block")
			.Build();

		Assert.That(evaluations, Is.Zero, "Composition must not decide availability.");
		Assert.That(restricted.CheckAvailability(accepted), Is.True);
		Assert.That(restricted.CheckAvailability(declined), Is.False);
		Assert.That(evaluations, Is.EqualTo(2));
		Assert.That(open.CheckAvailability(declined), Is.True);
	}

	// Registers one plugin whose first block is restricted; the customer block is restricted only when a rule is supplied.
	private static ServiceCollection CreateServices(Func<IServiceProvider, bool> administrationAvailability, Func<IServiceProvider, bool> customerAvailability = null,
		bool administrationSettings = false) {
		var services = new ServiceCollection();
		services.AddLogging();
		services.AddScoped<Viewer>();
		services.AddSingleton<NavigationManager>(new WorkspaceNavigation());
		services.AddSingleton<IJSRuntime, GalleryRenderingTests.TestJsRuntime>();
		if (administrationSettings)
			services.AddSingleton<ISettingsProvider, MissingSettingsProvider>();
		services.AddSphere10BlazorPlugin(plugin => plugin
			.WithName("Workspace")
			.AddBlock(block => {
				block
					.WithId("administration")
					.WithName("Administration block")
					.AvailableWhen(administrationAvailability)
					.AddScreen<AdministrationScreen>("Administration home", activationMode: ScreenActivationMode.SingleInstance)
					.AddScreen<AuditScreen>("Audit trail", activationMode: ScreenActivationMode.PermanentSingleton)
					.WithDefaultScreen<AdministrationScreen>()
					.AddMenu(menu => menu
						.WithText("Administration menus")
						.AddShowScreenItem<AdministrationScreen>(text: "Administration home", id: "home")
					);

				if (administrationSettings)
					block.UseSettings<AdministrationPreferences>();
			})
			.AddBlock(block => {
				block
					.WithId("customer")
					.WithName("Customer block")
					.WithPosition(1)
					.AddScreen<CustomerScreen>("Customer home", activationMode: ScreenActivationMode.SingleInstance)
					.WithDefaultScreen<CustomerScreen>()
					.AddMenu(menu => menu
						.WithText("Customer menus")
						.AddShowScreenItem<CustomerScreen>(text: "Customer home", id: "home")
					);

				if (customerAvailability != null)
					block.AvailableWhen(customerAvailability);
			})
		);
		return services;
	}

	#region Inner Types

	// Stands in for per-user state that each circuit's scope supplies to the predicate.
	private sealed class Viewer {
		/// <summary>Gets or sets whether this circuit's viewer may see the administration block.</summary>
		public bool IsAdministrator { get; set; }
	}

	/// <summary>The restricted block's default screen.</summary>
	public class AdministrationScreen : ApplicationScreen {
	}

	/// <summary>The restricted block's permanently open screen.</summary>
	public class AuditScreen : ApplicationScreen {
	}

	/// <summary>The unrestricted block's default screen.</summary>
	public class CustomerScreen : ApplicationScreen {
	}

	/// <summary>Preferences owned by the restricted block.</summary>
	public sealed class AdministrationPreferences : SettingsObject {
		/// <summary>Gets or sets an ordinary preference value.</summary>
		public bool ShowDetails { get; set; } = true;
	}

	/// <summary>Renders the real shell for an optional block route and captures its instance.</summary>
	public sealed class ShellHarness : ComponentBase {
		/// <summary>Captures the rendered shell.</summary>
		[Parameter] public Action<ApplicationShell> Capture { get; set; }

		/// <summary>The requested block route, or null for the startup route.</summary>
		[Parameter] public string BlockId { get; set; }

		/// <summary>Renders the real component and captures its reference.</summary>
		protected override void BuildRenderTree(RenderTreeBuilder builder) {
			builder.OpenComponent<ApplicationShell>(0);
			if (BlockId != null)
				builder.AddAttribute(1, nameof(ApplicationShell.BlockId), BlockId);
			builder.AddComponentReferenceCapture(2, component => Capture((ApplicationShell)component));
			builder.CloseComponent();
		}
	}

	// Records navigation inside the fixture's local application URL.
	private sealed class WorkspaceNavigation : NavigationManager {
		/// <summary>Creates the navigation manager at the fixture's application route.</summary>
		public WorkspaceNavigation() => Initialize("https://localhost/demo/", "https://localhost/demo/application");

		/// <inheritdoc />
		protected override void NavigateToCore(string uri, bool forceLoad) => Uri = ToAbsoluteUri(uri).AbsoluteUri;

		/// <inheritdoc />
		protected override void NavigateToCore(string uri, NavigationOptions options) => Uri = ToAbsoluteUri(uri).AbsoluteUri;
	}

	// Restores declared defaults without browser storage and rejects any write.
	private sealed class MissingSettingsProvider : SyncSettingsProviderBase {
		/// <inheritdoc />
		public override bool EncryptSettings => false;

		/// <inheritdoc />
		public override bool ContainsSetting(Type settingsType, object id = null) => false;

		/// <inheritdoc />
		public override void DeleteSetting(SettingsObject settings) {
		}

		/// <inheritdoc />
		public override void ClearSettings() {
		}

		/// <inheritdoc />
		protected override SettingsObject LoadInternal(Type settingsType, object id = null)
			=> throw new InvalidOperationException("No test preferences have been saved.");

		/// <inheritdoc />
		protected override void SaveInternal(SettingsObject settings) => throw new InvalidOperationException("Availability checks must not write settings.");
	}

	#endregion
}
