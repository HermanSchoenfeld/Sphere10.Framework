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
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Web.AspNetCore.Blazor.UI.Application;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationStartupScreenTests {
	[Test]
	public async Task SavedIdentityReopensCanonicalRegistrationWithItsConfiguredParameters() {
		using var services = new ServiceCollection().BuildServiceProvider();
		BlazorApplicationStartupScreen identity;
		using (var first = new BlazorApplicationScreenHost(CreateBlocks(), services)) {
			var session = await first.ActivateScreenAsync("block", "other");
			await first.AttachScreenAsync(session.Id, new OtherScreen());
			identity = first.GetStartupScreenIdentity(session);
			Assert.That(first.SetStartupScreen(identity), Is.True);
			Assert.That(first.IsStartupScreen(session), Is.True);
		}
		using var restored = new BlazorApplicationScreenHost(CreateBlocks(), services);

		Assert.That(restored.SetStartupScreen(identity), Is.True);
		await restored.InitializeAsync();

		Assert.That(restored.ActiveScreen.MenuItemId, Is.EqualTo("other"));
		Assert.That(restored.ActiveScreen.ScreenType, Is.EqualTo(typeof(OtherScreen)));
		Assert.That(restored.ActiveScreen.Parameters["Caption"], Is.EqualTo("Configured value"));
		Assert.That(restored.OpenScreens.Select(screen => screen.ScreenType), Does.Contain(typeof(PermanentScreen)));
	}

	[TestCase("block", "missing", "correct")]
	[TestCase("missing", "other", "correct")]
	[TestCase("block", "other", "arbitrary")]
	public async Task UnavailableOrForgedIdentityUsesTheExistingDefault(string block, string command, string type) {
		using var services = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(CreateBlocks(), services);
		var identity = Identity() with { BlockId = block, MenuItemId = command, ScreenTypeName = type == "correct" ? typeof(OtherScreen).FullName : "Unregistered.Type" };

		Assert.That(host.SetStartupScreen(identity), Is.False);
		await host.InitializeAsync();

		Assert.That(host.ActiveScreen.ScreenType, Is.EqualTo(typeof(DefaultScreen)));
		Assert.That(host.StartupScreen, Is.Null);
	}

	[Test]
	public async Task ExplicitActivationWinsOverSavedStartup() {
		using var services = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(CreateBlocks(), services);
		host.SetStartupScreen(Identity());
		var explicitScreen = await host.ActivateScreenAsync("block", "default");

		await host.InitializeAsync();

		Assert.That(host.ActiveScreen, Is.SameAs(explicitScreen));
		Assert.That(host.OpenScreens.Count(screen => screen.ScreenType == typeof(OtherScreen)), Is.Zero);
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task RuntimeActivationParametersAndInstanceKeysAreNeverPersistable(bool keyed) {
		using var services = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(CreateBlocks(), services);
		var activation = keyed ? new BlazorApplicationScreenActivation(instanceKey: "session-42")
			: new BlazorApplicationScreenActivation(new Dictionary<string, object> { ["Caption"] = "Session value" });
		var session = await host.ActivateScreenAsync("block", "other", activation);
		await host.AttachScreenAsync(session.Id, new OtherScreen());

		Assert.That(host.CanMakeStartupScreen(session), Is.False);
		Assert.That(host.GetStartupScreenUnavailableReason(session), Does.Contain("session-specific"));
		Assert.That(() => host.GetStartupScreenIdentity(session), Throws.InvalidOperationException);
	}

	[Test]
	public async Task ComponentOptOutAndPreconstructionHostPolicyRejectStartupChoices() {
		using var services = new ServiceCollection().BuildServiceProvider();
		using var host = new BlazorApplicationScreenHost(CreateBlocks(), services);
		var optedOut = await host.ActivateScreenAsync("block", "session");
		await host.AttachScreenAsync(optedOut.Id, new SessionScreen());
		Assert.That(host.CanMakeStartupScreen(optedOut), Is.False);

		using var restricted = new RestrictedHost(CreateBlocks(), services);
		Assert.That(restricted.SetStartupScreen(Identity()), Is.False);
		await restricted.InitializeAsync();
		Assert.That(restricted.ActiveScreen.ScreenType, Is.EqualTo(typeof(DefaultScreen)));
	}

	[Test]
	public async Task BrowserPreferenceLoadsBeforeBlankRouteDefaultButExplicitRouteStillWins() {
		await using var setup = CreateRenderer(new BrowserRuntime { Identity = Identity() });
		await setup.Renderer.Dispatcher.InvokeAsync(async () => {
			var rendered = await setup.RenderAsync();
			Assert.That(setup.Host.ActiveScreen, Is.Null);
			Assert.That(rendered.ToHtmlString(), Does.Contain("Loading workspace"));

			await setup.Shell.CompleteRenderAsync();

			Assert.That(setup.Host.ActiveScreen.ScreenType, Is.EqualTo(typeof(OtherScreen)));
			Assert.That(setup.Browser.ReadKeys, Is.EqualTo(new[] { "test.application" }));
		});
		await using var explicitSetup = CreateRenderer(new BrowserRuntime { Identity = Identity() });
		await explicitSetup.Renderer.Dispatcher.InvokeAsync(async () => {
			await explicitSetup.RenderAsync("default");
			var explicitScreen = explicitSetup.Host.ActiveScreen;
			await explicitSetup.Shell.CompleteRenderAsync();
			Assert.That(explicitSetup.Host.ActiveScreen, Is.SameAs(explicitScreen));
			Assert.That(explicitSetup.Host.ActiveScreen.ScreenType, Is.EqualTo(typeof(DefaultScreen)));
		});
	}

	[Test]
	public async Task SavingBrowserPreferenceUpdatesCheckedStateOnlyAfterSuccessfulStorage() {
		await using var setup = CreateRenderer(new BrowserRuntime());
		await setup.Renderer.Dispatcher.InvokeAsync(async () => {
			await setup.RenderAsync("other");
			await setup.Shell.CompleteRenderAsync();
			var session = setup.Host.ActiveScreen;

			await setup.Shell.MakeStartupAsync(session.Id);

			Assert.That(setup.Host.IsStartupScreen(session), Is.True);
			Assert.That(setup.Browser.Identity, Is.EqualTo(setup.Host.GetStartupScreenIdentity(session)));
			Assert.That(setup.Browser.WriteKeys, Is.EqualTo(new[] { "test.application" }));
			var previous = setup.Host.StartupScreen;
			var attached = new TaskCompletionSource<BlazorApplicationScreenSession>(TaskCreationOptions.RunContinuationsAsynchronously);
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
			Assert.That(setup.Browser.Identity, Is.EqualTo(previous));
			Assert.That(setup.Markup.ToHtmlString(), Does.Contain("could not be saved"));
		});
	}

	[Test]
	public async Task UnavailableBrowserStorageFallsBackWithoutLosingTheCircuit() {
		await using var setup = CreateRenderer(new BrowserRuntime { FailRead = true });
		await setup.Renderer.Dispatcher.InvokeAsync(async () => {
			var rendered = await setup.RenderAsync();
			await setup.Shell.CompleteRenderAsync();

			Assert.That(setup.Host.ActiveScreen.ScreenType, Is.EqualTo(typeof(DefaultScreen)));
			Assert.That(rendered.ToHtmlString(), Does.Contain("could not be read"));
		});
	}

	[Test]
	public async Task ShellWithoutSettingsKeyKeepsImmediateStartupAndDoesNotAccessBrowserStorage() {
		await using var setup = CreateRenderer(new BrowserRuntime { FailRead = true });
		await setup.Renderer.Dispatcher.InvokeAsync(async () => {
			await setup.RenderAsync(settingsKey: null);
			await setup.Shell.CompleteRenderAsync();

			Assert.That(setup.Host.ActiveScreen.ScreenType, Is.EqualTo(typeof(DefaultScreen)));
			Assert.That(setup.Browser.ReadKeys, Is.Empty);
		});
	}

	[Test]
	public async Task OpenScreensSelectorDisambiguatesTitlesAndSelectsTheExactRetainedInstance() {
		await using var setup = CreateRenderer(new BrowserRuntime());
		await setup.Renderer.Dispatcher.InvokeAsync(async () => {
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

	[Test]
	public async Task StartupTabCommandShowsCheckedChoiceAndExplainsSessionOnlyExclusion() {
		await using var setup = CreateRenderer(new BrowserRuntime());
		await setup.Renderer.Dispatcher.InvokeAsync(async () => {
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

	private static BlazorApplicationStartupScreen Identity() => new(string.Empty, "block", "other", typeof(OtherScreen).FullName, typeof(OtherScreen).Assembly.GetName().Name);

	private static IBlazorApplicationBlock[] CreateBlocks() => new[] {
		new BlazorApplicationBlockBuilder()
			.WithId("block")
			.WithName("Test block")
			.WithDefaultScreen<DefaultScreen>()
			.ConfigureMenu(menu => menu
				.WithText("Screens")
				.AddScreenItem<DefaultScreen>("default", "Default", ScreenActivationMode.SingleInstance)
				.AddScreenItem<OtherScreen>("other", "Other", ScreenActivationMode.MultiInstance, new Dictionary<string, object> { ["Caption"] = "Configured value" })
				.AddScreenItem<SessionScreen>("session", "Session", ScreenActivationMode.MultiInstance)
				.AddScreenItem<PermanentScreen>("permanent", "Permanent", ScreenActivationMode.PermanentSingleton)
			)
			.Build()
	};

	private static RendererSetup CreateRenderer(BrowserRuntime browser) {
		var services = new ServiceCollection().AddLogging();
		services.AddSphere10Blazor();
		services.AddApplicationBlock(CreateBlocks()[0]);
		services.AddSingleton<NavigationManager>(new TestNavigationManager());
		services.AddSingleton<IJSRuntime>(browser);
		return new RendererSetup(services.BuildServiceProvider(), browser);
	}

	public class DefaultScreen : BlazorApplicationScreen { }

	public class OtherScreen : BlazorApplicationScreen {
		[Parameter] public string Caption { get; set; }
	}

	public class PermanentScreen : BlazorApplicationScreen { }

	public class SessionScreen : BlazorApplicationScreen {
		public override bool CanRestoreAtStartup => false;
	}

	private sealed class RestrictedHost : BlazorApplicationScreenHost {
		public RestrictedHost(IEnumerable<IBlazorApplicationBlock> blocks, IServiceProvider services)
			: base(blocks, services) { }

		protected override bool CanRestoreScreenAtStartup(IBlazorApplicationBlock block, ApplicationScreenDefinition definition) =>
			definition.ScreenType != typeof(OtherScreen) && base.CanRestoreScreenAtStartup(block, definition);
	}

	public class ShellProbe : ApplicationShell {
		public Task CompleteRenderAsync() => OnAfterRenderAsync(false);

		public Task MakeStartupAsync(Guid id) => MakeStartupScreenAsync(id);
	}

	public class Harness : ComponentBase {
		[Parameter] public Action<ShellProbe> Capture { get; set; }
		[Parameter] public string ScreenId { get; set; }
		[Parameter] public string SettingsKey { get; set; }

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

	public class TabsProbe : ApplicationScreenTabs {
		public Task SelectOpenScreenAsync(Guid id) => (Task)typeof(ApplicationScreenTabs)
			.GetMethod("SelectOpenScreenAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
			.Invoke(this, new object[] { new ChangeEventArgs { Value = id.ToString() } });

		public async Task OpenContextAsync(Guid id) {
			await (Task)typeof(ApplicationScreenTabs).GetMethod("KeyDownAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)
				.Invoke(this, new object[] { id, new KeyboardEventArgs { Key = "F10", ShiftKey = true } });
			await SetParametersAsync(ParameterView.Empty);
		}
	}

	public class TabsHarness : ComponentBase {
		[Parameter] public IBlazorApplicationScreenHost Host { get; set; }
		[Parameter] public Action<TabsProbe> Capture { get; set; }

		protected override void BuildRenderTree(RenderTreeBuilder builder) {
			builder.OpenComponent<TabsProbe>(0);
			builder.AddAttribute(1, nameof(ApplicationScreenTabs.Sessions), Host.OpenScreens);
			builder.AddAttribute(2, nameof(ApplicationScreenTabs.ActiveSession), Host.ActiveScreen);
			builder.AddAttribute(3, nameof(ApplicationScreenTabs.OnSelect), EventCallback.Factory.Create<Guid>(this, async id => { await Host.ShowScreenAsync(id); }));
			builder.AddAttribute(4, nameof(ApplicationScreenTabs.IsStartupScreen), (Func<BlazorApplicationScreenSession, bool>)Host.IsStartupScreen);
			builder.AddAttribute(5, nameof(ApplicationScreenTabs.StartupUnavailableReason), (Func<BlazorApplicationScreenSession, string>)Host.GetStartupScreenUnavailableReason);
			builder.AddComponentReferenceCapture(6, component => Capture((TabsProbe)component));
			builder.CloseComponent();
		}
	}

	private sealed class RendererSetup : IAsyncDisposable {
		private readonly ServiceProvider _provider;

		public RendererSetup(ServiceProvider provider, BrowserRuntime browser) {
			_provider = provider;
			Browser = browser;
			Renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
			Host = provider.GetRequiredService<IBlazorApplicationScreenHost>();
		}

		public HtmlRenderer Renderer { get; }
		public BrowserRuntime Browser { get; }
		public IBlazorApplicationScreenHost Host { get; }
		public ShellProbe Shell { get; private set; }
		public Microsoft.AspNetCore.Components.Web.HtmlRendering.HtmlRootComponent Markup { get; private set; }

		public async Task<Microsoft.AspNetCore.Components.Web.HtmlRendering.HtmlRootComponent> RenderAsync(string screenId = null, string settingsKey = "test.application") {
			Markup = await Renderer.RenderComponentAsync<Harness>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(Harness.Capture)] = (Action<ShellProbe>)(shell => Shell = shell),
				[nameof(Harness.ScreenId)] = screenId,
				[nameof(Harness.SettingsKey)] = settingsKey
			}));
			return Markup;
		}

		public async ValueTask DisposeAsync() {
			await Renderer.DisposeAsync();
			await _provider.DisposeAsync();
		}
	}

	private sealed class TestNavigationManager : NavigationManager {
		public TestNavigationManager() => Initialize("http://localhost/", "http://localhost/application");

		protected override void NavigateToCore(string uri, bool forceLoad) => Uri = ToAbsoluteUri(uri).AbsoluteUri;

		protected override void NavigateToCore(string uri, NavigationOptions options) => Uri = ToAbsoluteUri(uri).AbsoluteUri;
	}

	private sealed class BrowserRuntime : IJSRuntime, IJSObjectReference {
		public BlazorApplicationStartupScreen Identity { get; set; }
		public bool FailRead { get; set; }
		public bool FailWrite { get; set; }
		public List<string> ReadKeys { get; } = new();
		public List<string> WriteKeys { get; } = new();

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] args) {
			if (identifier == "import")
				return ValueTask.FromResult((TValue)(object)this);
			if (identifier == "ReadStartupScreen") {
				ReadKeys.Add((string)args[0]);
				if (FailRead)
					throw new JSException("Storage unavailable.");
				return ValueTask.FromResult((TValue)(object)Identity);
			}
			if (identifier == "WriteStartupScreen") {
				WriteKeys.Add((string)args[0]);
				if (FailWrite)
					throw new JSException("Storage unavailable.");
				Identity = (BlazorApplicationStartupScreen)args[1];
			}
			return ValueTask.FromResult(default(TValue));
		}

		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken cancellationToken, object[] args) => InvokeAsync<TValue>(identifier, args);

		public ValueTask DisposeAsync() => ValueTask.CompletedTask;
	}
}
