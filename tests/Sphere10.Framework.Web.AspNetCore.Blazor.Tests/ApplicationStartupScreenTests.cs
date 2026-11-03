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
using SharedUI = global::Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationStartupScreenTests {

	/// <summary>Verifies saved identity reopens canonical registration with its configured parameters.</summary>
	[Test]
	public async Task SavedIdentityReopensCanonicalRegistrationWithItsConfiguredParameters() {
		using var services = new ServiceCollection().BuildServiceProvider();
		ApplicationStartupScreen identity;
		using (var first = new BlazorUI.ApplicationScreenHost(CreateBlocks(), services)) {
			var session = await first.ActivateScreenAsync("block", "other");
			await first.AttachScreenAsync(session.Id, new OtherScreen());
			identity = first.GetStartupScreenIdentity(session);
			Assert.That(first.SetStartupScreen(identity), Is.True);
			Assert.That(first.IsStartupScreen(session), Is.True);
		}
		using var restored = new BlazorUI.ApplicationScreenHost(CreateBlocks(), services);

		Assert.That(restored.SetStartupScreen(identity), Is.True);
		await restored.InitializeAsync();

		Assert.That(restored.ActiveScreen.MenuItemId, Is.EqualTo("other"));
		Assert.That(restored.ActiveScreen.ScreenType, Is.EqualTo(typeof(OtherScreen)));
		Assert.That(restored.ActiveScreen.Parameters["Caption"], Is.EqualTo("Configured value"));
		Assert.That(restored.OpenScreens.Select(screen => screen.ScreenType), Does.Contain(typeof(PermanentScreen)));
	}

	/// <summary>Verifies unavailable or forged identity uses the existing default.</summary>
	[TestCase("block", "missing", "correct")]
	[TestCase("missing", "other", "correct")]
	[TestCase("block", "other", "arbitrary")]
	public async Task UnavailableOrForgedIdentityUsesTheExistingDefault(string block, string command, string type) {
		using var services = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorUI.ApplicationScreenHost(CreateBlocks(), services);
		var identity = Identity() with { BlockId = block, MenuItemId = command, ScreenTypeName = type == "correct" ? typeof(OtherScreen).FullName : "Unregistered.Type" };

		Assert.That(host.SetStartupScreen(identity), Is.False);
		await host.InitializeAsync();

		Assert.That(host.ActiveScreen.ScreenType, Is.EqualTo(typeof(DefaultScreen)));
		Assert.That(host.StartupScreen, Is.Null);
	}

	/// <summary>Verifies explicit activation wins over saved startup.</summary>
	[Test]
	public async Task ExplicitActivationWinsOverSavedStartup() {
		using var services = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorUI.ApplicationScreenHost(CreateBlocks(), services);
		host.SetStartupScreen(Identity());
		var explicitScreen = await host.ActivateScreenAsync("block", "default");

		await host.InitializeAsync();

		Assert.That(host.ActiveScreen, Is.SameAs(explicitScreen));
		Assert.That(host.OpenScreens.Count(screen => screen.ScreenType == typeof(OtherScreen)), Is.Zero);
	}

	/// <summary>Verifies runtime activation parameters and instance keys are never persistable.</summary>
	[TestCase(false)]
	[TestCase(true)]
	public async Task RuntimeActivationParametersAndInstanceKeysAreNeverPersistable(bool keyed) {
		using var services = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorUI.ApplicationScreenHost(CreateBlocks(), services);
		var activation = keyed ? new ApplicationScreenActivation(instanceKey: "session-42")
			: new ApplicationScreenActivation(new Dictionary<string, object> { ["Caption"] = "Session value" });
		var session = await host.ActivateScreenAsync("block", "other", activation);
		await host.AttachScreenAsync(session.Id, new OtherScreen());

		Assert.That(host.CanMakeStartupScreen(session), Is.False);
		Assert.That(host.GetStartupScreenUnavailableReason(session), Does.Contain("session-specific"));
		Assert.That(() => host.GetStartupScreenIdentity(session), Throws.InvalidOperationException);
	}

	/// <summary>Verifies component opt out and preconstruction host policy reject startup choices.</summary>
	[Test]
	public async Task ComponentOptOutAndPreconstructionHostPolicyRejectStartupChoices() {
		using var services = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorUI.ApplicationScreenHost(CreateBlocks(), services);
		var optedOut = await host.ActivateScreenAsync("block", "session");
		await host.AttachScreenAsync(optedOut.Id, new SessionScreen());
		Assert.That(host.CanMakeStartupScreen(optedOut), Is.False);

		using var restricted = new RestrictedHost(CreateBlocks(), services);
		Assert.That(restricted.SetStartupScreen(Identity()), Is.False);
		await restricted.InitializeAsync();
		Assert.That(restricted.ActiveScreen.ScreenType, Is.EqualTo(typeof(DefaultScreen)));
	}

	/// <summary>Verifies browser preference loads before blank route default but explicit route still wins.</summary>
	[Test]
	public async Task BrowserPreferenceLoadsBeforeBlankRouteDefaultButExplicitRouteStillWins() {
		await using var setup = CreateRenderer(new BrowserRuntime { Identity = Identity() });
		await setup.InvokeAsync(async () => {
			var rendered = await setup.RenderAsync();
			Assert.That(setup.Host.ActiveScreen, Is.Null);
			Assert.That(rendered.ToHtmlString(), Does.Contain("Loading workspace"));

			await setup.Shell.CompleteRenderAsync();

			Assert.That(setup.Host.ActiveScreen.ScreenType, Is.EqualTo(typeof(OtherScreen)));
			Assert.That(setup.Browser.ReadKeys, Is.EqualTo(new[] { "test.application" }));
		});
		await using var explicitSetup = CreateRenderer(new BrowserRuntime { Identity = Identity() });
		await explicitSetup.InvokeAsync(async () => {
			await explicitSetup.RenderAsync("default");
			var explicitScreen = explicitSetup.Host.ActiveScreen;
			await explicitSetup.Shell.CompleteRenderAsync();
			Assert.That(explicitSetup.Host.ActiveScreen, Is.SameAs(explicitScreen));
			Assert.That(explicitSetup.Host.ActiveScreen.ScreenType, Is.EqualTo(typeof(DefaultScreen)));
		});
	}

	/// <summary>Verifies typed startup settings override the existing legacy browser identity without rewriting it.</summary>
	[Test]
	public async Task TypedStartupSettingsOverrideTheExistingLegacyBrowserIdentityWithoutRewritingIt() {
		var legacy = Identity();
		await using var setup = CreateRenderer(new BrowserRuntime { Identity = legacy });
		await setup.InvokeAsync(async () => {
			var settings = await UserSettings.GetAsync<ApplicationShell.StartupSettings>("test.application");
			settings.StartupScreen = legacy with { MenuItemId = "default", ScreenTypeName = typeof(DefaultScreen).FullName };
			await settings.SaveAsync();
			setup.Browser.WriteKeys.Clear();
			await setup.RenderAsync();
			await setup.Shell.CompleteRenderAsync();
			Assert.That(setup.Host.ActiveScreen.ScreenType, Is.EqualTo(typeof(DefaultScreen)));
			Assert.That(setup.Browser.ReadKeys, Is.Empty, "A current typed root suppresses legacy reads.");
			Assert.That(setup.Browser.WriteKeys, Is.Empty, "Restoration does not write preferences.");
			Assert.That(setup.Browser.Identity, Is.SameAs(legacy), "The original legacy record remains untouched.");
		});
	}

	/// <summary>Verifies saving browser preference updates checked state only after successful storage.</summary>
	[Test]
	public async Task SavingBrowserPreferenceUpdatesCheckedStateOnlyAfterSuccessfulStorage() {
		await using var setup = CreateRenderer(new BrowserRuntime());
		await setup.InvokeAsync(async () => {
			await setup.RenderAsync("other");
			await setup.Shell.CompleteRenderAsync();
			var session = setup.Host.ActiveScreen;

			await setup.Shell.MakeStartupAsync(session.Id);

			Assert.That(setup.Host.IsStartupScreen(session), Is.True);
			Assert.That((await UserSettings.GetAsync<ApplicationShell.StartupSettings>("test.application")).StartupScreen,
				Is.EqualTo(setup.Host.GetStartupScreenIdentity(session)));
			Assert.That(setup.Browser.WriteKeys, Has.Count.EqualTo(1));
			Assert.That(setup.Browser.WriteKeys.Single(), Does.EndWith("/test.application"));
			var previous = setup.Host.StartupScreen;
			var attached = new TaskCompletionSource<ApplicationScreenSession>(TaskCreationOptions.RunContinuationsAsynchronously);
			void ObserveAttachment() {
				if (setup.Host.ActiveScreen is { Screen: DefaultScreen } active)
					attached.TrySetResult(active);
			}
			setup.Host.Changed += ObserveAttachment;
			using var subscription = Tools.Scope.ExecuteOnDispose(() => setup.Host.Changed -= ObserveAttachment);
			var next = await setup.Host.ActivateScreenAsync("block", "default");
			ObserveAttachment();
			Assert.That(await attached.Task.WaitAsync(TimeSpan.FromSeconds(5)), Is.SameAs(next));
			setup.Browser.FailWrite = true;
			await setup.Shell.MakeStartupAsync(next.Id);
			Assert.That(setup.Host.StartupScreen, Is.EqualTo(previous));
			Assert.That((await UserSettings.GetAsync<ApplicationShell.StartupSettings>("test.application")).StartupScreen, Is.EqualTo(previous));
			Assert.That(setup.Markup.ToHtmlString(), Does.Contain("could not be saved"));
		});
	}

	/// <summary>Verifies unavailable browser storage falls back without losing the circuit.</summary>
	[Test]
	public async Task UnavailableBrowserStorageFallsBackWithoutLosingTheCircuit() {
		await using var setup = CreateRenderer(new BrowserRuntime { FailRead = true });
		await setup.InvokeAsync(async () => {
			var rendered = await setup.RenderAsync();
			await setup.Shell.CompleteRenderAsync();

			Assert.That(setup.Host.ActiveScreen.ScreenType, Is.EqualTo(typeof(DefaultScreen)));
			Assert.That(rendered.ToHtmlString(), Does.Contain("could not be read"));
		});
	}

	/// <summary>Verifies shell without settings key keeps immediate startup and does not access browser storage.</summary>
	[Test]
	public async Task ShellWithoutSettingsKeyKeepsImmediateStartupAndDoesNotAccessBrowserStorage() {
		await using var setup = CreateRenderer(new BrowserRuntime { FailRead = true });
		await setup.InvokeAsync(async () => {
			await setup.RenderAsync(settingsKey: null);
			await setup.Shell.CompleteRenderAsync();

			Assert.That(setup.Host.ActiveScreen.ScreenType, Is.EqualTo(typeof(DefaultScreen)));
			Assert.That(setup.Browser.ReadKeys, Is.Empty);
		});
	}

	/// <summary>Verifies open screens selector disambiguates titles and selects the exact retained instance.</summary>
	[Test]
	public async Task OpenScreensSelectorDisambiguatesTitlesAndSelectsTheExactRetainedInstance() {
		await using var setup = CreateRenderer(new BrowserRuntime());
		await setup.InvokeAsync(async () => {
			var first = await setup.Host.ActivateScreenAsync("block", "other");
			var second = await setup.Host.ActivateScreenAsync("block", "other");
			await setup.Host.ActivateScreenAsync("block", "permanent");
			await setup.Host.ShowScreenAsync(first.Id);
			TabsProbe tabs = null;
			var rendered = await setup.Renderer.RenderComponentAsync<TabsHarness>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(TabsHarness.Host)] = setup.Host,
				[nameof(TabsHarness.Capture)] = (Action<TabsProbe>)(instance => tabs = instance)
			}));
			var html = rendered.ToHtmlString();
			Assert.That(html, Does.Contain("aria-label=\"Open Screens\""));
			Assert.That(html, Does.Match($"<option\\b[^>]*value=\"{first.Id}\"[^>]*>Other</option>"));
			Assert.That(html, Does.Match($"<option\\b[^>]*value=\"{second.Id}\"[^>]*>Other \\(2\\)</option>"));
			Assert.That(html, Does.Contain(">Permanent</option>"));
			var before = setup.Host.Screens;

			await tabs.SelectOpenScreenAsync(second.Id);

			Assert.That(setup.Host.ActiveScreen, Is.SameAs(second));
			Assert.That(setup.Host.Screens, Is.EqualTo(before), "The selector switches existing sessions rather than activating another instance.");
			await tabs.SelectOpenScreenAsync(Guid.NewGuid());
			Assert.That(setup.Host.ActiveScreen, Is.SameAs(second));
		});
	}

	/// <summary>Verifies startup tab command shows checked choice and explains session only exclusion.</summary>
	[Test]
	public async Task StartupTabCommandShowsCheckedChoiceAndExplainsSessionOnlyExclusion() {
		await using var setup = CreateRenderer(new BrowserRuntime());
		await setup.InvokeAsync(async () => {
			var first = await setup.Host.ActivateScreenAsync("block", "other");
			await setup.Host.AttachScreenAsync(first.Id, new OtherScreen());
			setup.Host.SetStartupScreen(setup.Host.GetStartupScreenIdentity(first));
			var sessionOnly = await setup.Host.ActivateScreenAsync("block", "session");
			await setup.Host.AttachScreenAsync(sessionOnly.Id, new SessionScreen());
			TabsProbe tabs = null;
			var rendered = await setup.Renderer.RenderComponentAsync<TabsHarness>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(TabsHarness.Host)] = setup.Host,
				[nameof(TabsHarness.Capture)] = (Action<TabsProbe>)(instance => tabs = instance)
			}));

			await tabs.OpenContextAsync(first.Id);
			Assert.That(rendered.ToHtmlString(), Does.Contain("aria-checked=\"true\"").And.Contain("Make Startup Screen"));
			await tabs.OpenContextAsync(sessionOnly.Id);
			Assert.That(rendered.ToHtmlString(), Does.Contain("session-specific state").And.Contain("aria-checked=\"false\""));
		});
	}

	// Builds a persisted registration identity from the canonical block graph.
	private static ApplicationStartupScreen Identity() {
		return new(string.Empty, "block", "other", typeof(OtherScreen).FullName, typeof(OtherScreen).Assembly.GetName().Name);
	}

	// Creates independent registrations with distinct startup policies.
	private static BlazorUI.IApplicationBlock[] CreateBlocks() {
		return new[] {
			new ApplicationBlockBuilder()
				.WithId("block")
				.WithName("Test block")
				.AddScreen<PermanentScreen>(screen => screen
					.WithTitle("Permanent")
					.WithActivationMode(ScreenActivationMode.PermanentSingleton)
				)
				.AddScreen<SessionScreen>(screen => screen
					.WithTitle("Session")
					.WithActivationMode(ScreenActivationMode.MultiInstance)
				)
				.AddScreen<OtherScreen>(screen => screen
					.WithTitle("Other")
					.WithActivationMode(ScreenActivationMode.MultiInstance)
					.WithParameters(new Dictionary<string, object> { ["Caption"] = "Configured value" })
				)
				.AddScreen<DefaultScreen>(screen => screen
					.WithTitle("Default")
					.WithActivationMode(ScreenActivationMode.SingleInstance)
				)
				.WithDefaultScreen<DefaultScreen>()
				.AddMenu(menu => menu
					.WithText("Screens")
					.AddShowScreenItem<DefaultScreen>(text: "Default", id: "default")
					.AddShowScreenItem<OtherScreen>(text: "Other", id: "other")
					.AddShowScreenItem<SessionScreen>(text: "Session", id: "session")
					.AddShowScreenItem<PermanentScreen>(text: "Permanent", id: "permanent")
				)
				.Build()
		};
	}

	// Creates the real shell with an isolated browser-settings fixture.
	private static RendererSetup CreateRenderer(BrowserRuntime browser) {
		var services = new ServiceCollection().AddLogging();
		services.AddSphere10Blazor();
		services
			.AddApplicationBlock(CreateBlocks()[0]);
		services.AddSingleton<NavigationManager>(new TestNavigationManager());
		services.AddSingleton<IJSRuntime>(browser);
		return new RendererSetup(services.BuildServiceProvider(), browser);
	}

	#region Inner Types

	/// <summary>The block's ordinary startup screen.</summary>
	public class DefaultScreen : ApplicationScreen {
	}

	/// <summary>An alternative screen with configured parameters.</summary>
	public class OtherScreen : ApplicationScreen {

		/// <summary>The configured screen caption.</summary>
		[Parameter] public string Caption { get; set; }
	}

	/// <summary>A screen used to verify permanent singleton startup behavior.</summary>
	public class PermanentScreen : ApplicationScreen {
	}

	/// <summary>A session-only screen that declines startup restoration.</summary>
	public class SessionScreen : ApplicationScreen {

		/// <summary>Declines restoration for a session-only screen.</summary>
		public override bool CanRestoreAtStartup {
			get {
				return false;
			}
		}
	}

	// Applies an application policy before constructing a startup screen.
	private sealed class RestrictedHost : BlazorUI.ApplicationScreenHost {

		/// <summary>Creates the restricted host with its fixture dependencies.</summary>
		public RestrictedHost(IEnumerable<BlazorUI.IApplicationBlock> blocks, IServiceProvider services)
			: base(blocks, services) {
		}

		/// <summary>Rejects the registration configured as unavailable for startup.</summary>
		protected override bool CanRestoreScreenAtStartup(BlazorUI.IApplicationBlock block, SharedUI.ApplicationScreenDefinition definition) {
			return definition.ScreenType != typeof(OtherScreen) && base.CanRestoreScreenAtStartup(block, definition);
		}
	}

	/// <summary>Exposes actual shell lifecycle methods to the renderer fixture.</summary>
	public class ShellProbe : ApplicationShell {

		/// <summary>Completes the shell's actual interactive startup phase.</summary>
		public Task CompleteRenderAsync() {
			return OnAfterRenderAsync(false);
		}

		/// <summary>Invokes the shell's actual startup selection command.</summary>
		public Task MakeStartupAsync(Guid id) {
			return MakeStartupScreenAsync(id);
		}
	}

	/// <summary>Renders and captures the shell under test.</summary>
	public class Harness : ComponentBase {

		/// <summary>Captures the rendered component instance.</summary>
		[Parameter] public Action<ShellProbe> Capture { get; set; }

		/// <summary>The requested registered screen.</summary>
		[Parameter] public string ScreenId { get; set; }

		/// <summary>The shell's explicit startup preference identity.</summary>
		[Parameter] public string SettingsKey { get; set; }

		/// <summary>Renders the real component and captures its reference.</summary>
		protected override void BuildRenderTree(RenderTreeBuilder builder) {
			builder.OpenComponent<ShellProbe>(0);
			builder.AddAttribute(1, nameof(ApplicationShell.StartupScreenSettingsKey), SettingsKey);
			if (ScreenId != null) {
				builder.AddAttribute(2, nameof(ApplicationShell.BlockId), "block");
				builder.AddAttribute(3, nameof(ApplicationShell.ScreenId), ScreenId);
			}
			builder.AddComponentReferenceCapture(4, component => Capture((ShellProbe)component));
			builder.CloseComponent();
		}
	}

	/// <summary>Exposes actual tab commands to the renderer fixture.</summary>
	public class TabsProbe : ApplicationScreenTabs {

		/// <summary>Selects an existing screen through the tab control.</summary>
		public Task SelectOpenScreenAsync(Guid id) {
			return (Task)typeof(ApplicationScreenTabs)
				.GetMethod("SelectOpenScreenAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
				.Invoke(this, new object[] { new ChangeEventArgs { Value = id.ToString() } });
		}

		/// <summary>Opens the tab's actual context menu.</summary>
		public async Task OpenContextAsync(Guid id) {
			await (Task)typeof(ApplicationScreenTabs).GetMethod("KeyDownAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
				.Invoke(this, new object[] { id, new KeyboardEventArgs { Key = "F10", ShiftKey = true } });
			await SetParametersAsync(ParameterView.Empty);
		}
	}

	/// <summary>Renders and captures the tab presentation under test.</summary>
	public class TabsHarness : ComponentBase {

		/// <summary>The fixture's retained screen host.</summary>
		[Parameter] public BlazorUI.IApplicationScreenHost Host { get; set; }

		/// <summary>Captures the rendered component instance.</summary>
		[Parameter] public Action<TabsProbe> Capture { get; set; }

		/// <summary>Renders the real component and captures its reference.</summary>
		protected override void BuildRenderTree(RenderTreeBuilder builder) {
			builder.OpenComponent<TabsProbe>(0);
			builder.AddAttribute(1, nameof(ApplicationScreenTabs.Sessions), Host.OpenScreens);
			builder.AddAttribute(2, nameof(ApplicationScreenTabs.ActiveSession), Host.ActiveScreen);
			builder.AddAttribute(3, nameof(ApplicationScreenTabs.OnSelect), EventCallback.Factory.Create<Guid>(this, async id => { await Host.ShowScreenAsync(id); }));
			builder.AddAttribute(4, nameof(ApplicationScreenTabs.IsStartupScreen), (Func<ApplicationScreenSession, bool>)Host.IsStartupScreen);
			builder.AddAttribute(5, nameof(ApplicationScreenTabs.StartupUnavailableReason), (Func<ApplicationScreenSession, string>)Host.GetStartupScreenUnavailableReason);
			builder.AddComponentReferenceCapture(6, component => Capture((TabsProbe)component));
			builder.CloseComponent();
		}
	}

	// Owns the renderer, application scope and fake browser for one scenario.
	private sealed class RendererSetup : IAsyncDisposable {
		private readonly ServiceProvider _provider;

		/// <summary>Creates the renderer setup with its fixture dependencies.</summary>
		public RendererSetup(ServiceProvider provider, BrowserRuntime browser) {
			_provider = provider;
			Browser = browser;
			Renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
			Host = provider.GetRequiredService<BlazorUI.IApplicationScreenHost>();
		}

		/// <summary>The renderer owned by this fixture.</summary>
		public HtmlRenderer Renderer { get; }

		/// <summary>The fake browser owned by this fixture.</summary>
		public BrowserRuntime Browser { get; }

		/// <summary>The fixture's retained screen host.</summary>
		public BlazorUI.IApplicationScreenHost Host { get; }

		/// <summary>The shell captured from the actual render tree.</summary>
		public ShellProbe Shell { get; private set; }

		/// <summary>The latest rendered shell markup.</summary>
		public Microsoft.AspNetCore.Components.Web.HtmlRendering.HtmlRootComponent Markup { get; private set; }

		// The fixture simulates the existing circuit's provider binding for the complete renderer activity.
		public async Task InvokeAsync(Func<Task> operation) {
			using var settingsScope = UserSettings.UseProvider(_provider.GetRequiredService<BlazorSettingsProvider>());
			await Renderer.Dispatcher.InvokeAsync(operation);
		}

		/// <summary>Renders the component inside an isolated user-settings scope.</summary>
		public async Task<Microsoft.AspNetCore.Components.Web.HtmlRendering.HtmlRootComponent> RenderAsync(string screenId = null, string settingsKey = "test.application") {
			Markup = await Renderer.RenderComponentAsync<Harness>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(Harness.Capture)] = (Action<ShellProbe>)(shell => Shell = shell),
				[nameof(Harness.ScreenId)] = screenId,
				[nameof(Harness.SettingsKey)] = settingsKey
			}));
			return Markup;
		}

		/// <summary>Releases the fixture-owned asynchronous resources.</summary>
		public async ValueTask DisposeAsync() {
			await Renderer.DisposeAsync();
			await _provider.DisposeAsync();
		}
	}

	// Records navigation inside the fixture's local application URL.
	private sealed class TestNavigationManager : NavigationManager {

		/// <summary>Creates the test navigation manager with its fixture dependencies.</summary>
		public TestNavigationManager() {
			Initialize("http://localhost/", "http://localhost/application");
		}

		/// <summary>Updates the fixture's current application URI.</summary>
		protected override void NavigateToCore(string uri, bool forceLoad) {
			Uri = ToAbsoluteUri(uri).AbsoluteUri;
		}

		/// <summary>Updates the fixture's current application URI.</summary>
		protected override void NavigateToCore(string uri, NavigationOptions options) {
			Uri = ToAbsoluteUri(uri).AbsoluteUri;
		}
	}

	// Emulates typed settings records and existing legacy browser keys.
	private sealed class BrowserRuntime : IJSRuntime, IJSObjectReference {

		// Existing raw identities are a read-only legacy source; current records stay in the typed provider namespace.
		public ApplicationStartupScreen Identity { get; set; }

		/// <summary>The provider's serialized typed browser records.</summary>
		public Dictionary<string, string> TypedRecords { get; } = new();

		/// <summary>Simulates a browser read failure.</summary>
		public bool FailRead { get; set; }

		/// <summary>Simulates a browser write failure.</summary>
		public bool FailWrite { get; set; }

		/// <summary>The legacy browser keys read by the shell.</summary>
		public List<string> ReadKeys { get; } = new();

		/// <summary>The typed record keys written by the provider.</summary>
		public List<string> WriteKeys { get; } = new();

		/// <summary>Implements the fixture's browser or renderer invocation boundary.</summary>
		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args) {
			if (identifier == "import")
				return ValueTask.FromResult((TValue)(object)this);
			if (identifier == "ReadStartupScreen") {
				ReadKeys.Add((string)args[0]);
				if (FailRead)
					throw new JSException("Storage unavailable.");
				return ValueTask.FromResult((TValue)(object)Identity);
			}
			if (identifier == "GetAnonymousProfile")
				return ValueTask.FromResult((TValue)(object)"startup-browser");
			if (identifier == "Read") {
				if (FailRead)
					throw new JSException("Storage unavailable.");
				return ValueTask.FromResult(TypedRecords.TryGetValue((string)args[0], out var value) ? (TValue)(object)value : default);
			}
			if (identifier == "Write") {
				if (FailWrite)
					throw new JSException("Storage unavailable.");
				var key = (string)args[0];
				TypedRecords[key] = (string)args[1];
				WriteKeys.Add(key);
			}
			if (identifier == "WriteStartupScreen")
				throw new InvalidOperationException("Current startup preferences must use UserSettings.");
			return ValueTask.FromResult(default(TValue));
		}

		/// <summary>Implements the fixture's browser or renderer invocation boundary.</summary>
		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args) {
			return InvokeAsync<TValue>(identifier, args);
		}

		/// <summary>Releases the fixture-owned asynchronous resources.</summary>
		public ValueTask DisposeAsync() {
			return ValueTask.CompletedTask;
		}
	}

	#endregion
}
