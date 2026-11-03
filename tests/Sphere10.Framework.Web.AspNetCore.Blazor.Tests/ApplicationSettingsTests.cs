// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization;
using System.Security.Claims;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Utils.BlazorTester.Application;
using Sphere10.Framework.Web.AspNetCore.Blazor.Modal;
using Sphere10.Framework.Web.AspNetCore.Blazor.UI.Application;
using Sphere10.Framework.Web.AspNetCore.Blazor.UI.Dialogs;
using BlazorUI = global::Sphere10.Framework.Web.AspNetCore.Blazor;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

/// <summary>Exercises actual settings ownership, browser persistence, editor validation and renderer notifications.</summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationSettingsTests {

	/// <summary>Verifies settings class editor is inherited and registration can override it.</summary>
	[TestCase(false)]
	[TestCase(true)]
	public async Task SettingsClassEditorIsInheritedAndRegistrationCanOverrideIt(bool overrideEditor) {
		await using var owner = new ProviderOwner(new BrowserState("declared editor"), null);
		var defaults = new DeclaredEditorScreen.Preferences();
		var definition = new BlazorUI.ApplicationScreenDefinition(typeof(DeclaredEditorScreen), "Declared", stableKey: "declared",
			settings: defaults, settingsEditor: overrideEditor ? typeof(CustomEditor) : null);
		await using var services = CreateServices(owner.Provider, new ApplicationBlock { Id = "declared", Title = "Declared", ScreenDefinitions = new[] { definition } });
		var application = services.GetRequiredService<BlazorUI.IApplication>();
		await application.InitializeAsync();
		var actualDefinition = application.Blocks.Single().ScreenDefinitions.Single();
		await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			ApplicationSettingsDialog dialog = null;
			await renderer.RenderComponentAsync<DialogHarness>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(DialogHarness.Application)] = application,
				[nameof(DialogHarness.Owner)] = actualDefinition,
				[nameof(DialogHarness.Capture)] = (Action<ApplicationSettingsDialog>)(component => dialog = component)
			}));
			var editor = GetField<DynamicComponent>(dialog, "_editor").Instance;
			Assert.That(editor, Is.TypeOf(overrideEditor ? typeof(CustomEditor) : typeof(DeclaredSettingsEditor)));
			Assert.That(((IApplicationSettingsEditor)editor).Value, Is.SameAs(actualDefinition.Settings));
			Assert.That(actualDefinition.Settings, Is.Not.SameAs(defaults), "The runtime circuit owns this settings instance.");
		});
	}

	/// <summary>Options omits screens without settings while retaining unopened and noneditable settings owners.</summary>
	[TestCase(false)]
	[TestCase(true)]
	public async Task OptionsHideScreensWithoutSettingsAndKeepUnopenedAndUnmarkedSettings(bool selectScreenWithoutSettings) {
		var browser = new BrowserState("filtered Options");
		await using var owner = new ProviderOwner(browser, null);
		var plain = new BlazorUI.ApplicationScreenDefinition(typeof(DeclaredEditorScreen), "No settings", stableKey: "plain");
		var unopened = new BlazorUI.ApplicationScreenDefinition(typeof(UnopenedAttributeSettingsScreen), "Unopened settings");
		var unmarked = new BlazorUI.ApplicationScreenDefinition(typeof(DeclaredEditorScreen), "Stored settings", stableKey: "stored", settings: new TransientPreferences());
		await using var services = CreateServices(owner.Provider,
			new ApplicationBlock { Id = "filtered", Title = "Filtered Options", ScreenDefinitions = new[] { plain, unopened, unmarked } });
		var application = services.GetRequiredService<BlazorUI.IApplication>();
		await application.InitializeAsync();
		var definitions = application.Blocks.Single().ScreenDefinitions;
		await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			ApplicationSettingsDialog dialog = null;
			var rendered = await renderer.RenderComponentAsync<DialogHarness>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(DialogHarness.Application)] = application,
				[nameof(DialogHarness.Owner)] = selectScreenWithoutSettings ? definitions[0] : null,
				[nameof(DialogHarness.Capture)] = (Action<ApplicationSettingsDialog>)(component => dialog = component)
			}));
			Assert.That(rendered.ToHtmlString(), Does.Not.Contain("No settings").And.Contain("Unopened settings").And.Contain("Stored settings"));
			Assert.That(GetField<object>(dialog, "_selected"), Is.SameAs(definitions[1]));
			Assert.That(((IApplicationSettingsEditor)GetField<DynamicComponent>(dialog, "_editor").Instance).Value, Is.SameAs(definitions[1].Settings));
			Assert.That(definitions[1].Settings, Is.TypeOf<UnopenedAttributeSettingsScreen.Preferences>());
			Assert.That(browser.Values, Is.Empty, "Opening Options must not save or activate an unopened settings screen.");
		});
	}

	/// <summary>Only real owner groups appear below the application heading, with standalone blocks directly at the root.</summary>
	[TestCase(false, false)]
	[TestCase(true, false)]
	[TestCase(false, true)]
	[TestCase(true, true)]
	public async Task OptionsHierarchyContainsOnlyRealPluginAndBlockOwners(bool includePlugin, bool includeStandaloneBlock) {
		// Compose actual plugin-owned and standalone registrations without activating their operational screens.
		await using var owner = new ProviderOwner(new BrowserState("owner hierarchy"), null);
		var registrations = new ServiceCollection()
			.AddLogging()
			.AddSingleton<ISettingsProvider>(owner.Provider)
			.AddSphere10Blazor();
		if (includePlugin)
			registrations.AddSphere10BlazorPlugin(plugin => plugin
				.WithName("Plugin owner")
				.AddBlock(block => block
					.WithId("plugin-block")
					.WithName("Plugin block")
					.AddScreen<UnopenedAttributeSettingsScreen>("Plugin screen")
				)
			);
		if (includeStandaloneBlock)
			registrations.AddApplicationBlock(block => block
				.WithId("standalone")
				.WithName("Standalone block")
				.AddScreen<UnopenedAttributeSettingsScreen>("Standalone screen")
			);
		await using var services = registrations.BuildServiceProvider();
		var application = services.GetRequiredService<BlazorUI.IApplication>();
		await application.InitializeAsync();
		await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			var rendered = await renderer.RenderComponentAsync<DialogHarness>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(DialogHarness.Application)] = application,
				[nameof(DialogHarness.Capture)] = (Action<ApplicationSettingsDialog>)(_ => { })
			}));

			// Remove valueless CSS isolation markers, then normalize the navigation's HTML boolean attribute for XML assertions.
			var markup = Regex.Match(rendered.ToHtmlString(), "<nav class=\"sphere10-options-navigation\"[^>]*>.*?</nav>", RegexOptions.Singleline).Value;
			var navigationMarkup = Regex.Replace(markup, @"\s+b-[a-z0-9]+(?=[\s>])", string.Empty);
			navigationMarkup = Regex.Replace(navigationMarkup, @"(<details)\s+open(?=[\s>])", "$1 open=\"open\"");
			var navigation = XElement.Parse(navigationMarkup);
			var roots = navigation.Elements("details").ToArray();
			var expected = new[] { includePlugin ? "Plugin owner" : null, includeStandaloneBlock ? "Standalone block" : null }.Where(name => name != null);
			Assert.That(roots.Select(node => node.Element("summary").Value.Trim()), Is.EqualTo(expected));
			Assert.That(navigation.Descendants("summary").Select(node => node.Value.Trim()), Does.Not.Contain("Application"));
			if (includePlugin) {
				var block = roots[0].Elements("details").Single();
				Assert.That(block.Element("summary").Value.Trim(), Is.EqualTo("Plugin block"));
				Assert.That(block.Elements("button").Select(button => button.Value), Is.EqualTo(new[] { "Plugin screen" }));
			}
			if (includeStandaloneBlock)
				Assert.That(roots.Last().Elements("button").Select(button => button.Value), Is.EqualTo(new[] { "Standalone screen" }));
		});
	}

	/// <summary>The visible session's settings take priority over sidebar browsing unless a caller requests another owner.</summary>
	[TestCase(false)]
	[TestCase(true)]
	public async Task OptionsPreferActiveScreenSettingsAndRetainExplicitSelection(bool selectExplicitOwner) {
		await using var owner = new ProviderOwner(new BrowserState("active Options"), null);
		await using var services = CreateServices(owner.Provider, SettingsBlock("first", Defaults()), SettingsBlock("active", Defaults()));
		var application = services.GetRequiredService<BlazorUI.IApplication>();
		await application.InitializeAsync();
		var first = application.Blocks[0];
		var active = application.Blocks[1];
		await application.ScreenHost.OpenScreenAsync(active, active.ScreenDefinitions.Single());
		await application.ScreenHost.SelectBlockAsync(first);
		await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			ApplicationSettingsDialog dialog = null;
			await renderer.RenderComponentAsync<DialogHarness>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(DialogHarness.Application)] = application,
				[nameof(DialogHarness.Owner)] = selectExplicitOwner ? first.ScreenDefinitions.Single() : null,
				[nameof(DialogHarness.Capture)] = (Action<ApplicationSettingsDialog>)(component => dialog = component)
			}));
			var expected = (selectExplicitOwner ? first : active).ScreenDefinitions.Single();
			Assert.That(GetField<object>(dialog, "_selected"), Is.SameAs(expected));
			Assert.That(((IApplicationSettingsEditor)GetField<DynamicComponent>(dialog, "_editor").Instance).Value, Is.SameAs(expected.Settings));
		});
	}

	/// <summary>Verifies dialog saves actual settings and cancel restores only unapplied edits.</summary>
	[Test]
	public async Task DialogSavesActualSettingsAndCancelRestoresOnlyUnappliedEdits() {
		var browser = new BrowserState("dialog");
		await using var owner = new ProviderOwner(browser, null);
		await using var services = CreateServices(owner.Provider, SettingsBlock("preferences", Defaults()));
		var application = services.GetRequiredService<BlazorUI.IApplication>();
		await application.InitializeAsync();
		var definition = application.Blocks.Single().ScreenDefinitions.Single();
		var settings = (Options)definition.Settings;
		var changes = 0;
		settings.OnChanged += () => changes++;
		await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			ApplicationSettingsDialog dialog = null;
			await renderer.RenderComponentAsync<DialogHarness>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(DialogHarness.Application)] = application,
				[nameof(DialogHarness.Owner)] = definition,
				[nameof(DialogHarness.Capture)] = (Action<ApplicationSettingsDialog>)(component => dialog = component)
			}));
			var editor = (IApplicationSettingsEditor)GetField<DynamicComponent>(dialog, "_editor").Instance;
			Assert.That(editor.Value, Is.SameAs(settings));
			settings.Number = -1;
			await editor.Changed.InvokeAsync();
			await dialog.ApplyAsync();
			Assert.That(browser.Values, Is.Empty);
			Assert.That(changes, Is.Zero);
			settings.Number = 25;
			await editor.Changed.InvokeAsync();
			await dialog.ApplyAsync();
			Assert.That(changes, Is.EqualTo(1));
			Assert.That(browser.Values, Has.Count.EqualTo(1));
			settings.Number = 99;
			await editor.Changed.InvokeAsync();
			dialog.CancelEditing();
			Assert.That(settings.Number, Is.EqualTo(25));
			Assert.That(changes, Is.EqualTo(1), "Discarding unapplied edits does not publish another change.");
		});
	}

	/// <summary>Verifies actual settings save refreshes both mounted screens without replacing their business draft.</summary>
	[Test]
	public async Task ActualSettingsSaveRefreshesBothMountedScreensWithoutReplacingTheirBusinessDraft() {
		await using var provider = new ProviderOwner(new BrowserState("screen settings"), null);
		await using var owner = await LifecycleOwner.CreateAsync(provider.Provider);
		await owner.Renderer.Dispatcher.InvokeAsync(async () => {
			owner.FirstScreen.OperationalDraft = "Unsaved operation";
			Assert.That(owner.FirstScreen.ScreenSettings, Is.SameAs(owner.Settings));
			Assert.That(owner.SecondScreen.ScreenSettings, Is.SameAs(owner.Settings));
			((Options)owner.Settings).Number = 37;
			await owner.Settings.SaveAsync();
		});
		await owner.WaitForSettingsAsync();
		Assert.That(owner.FirstScreen.Values.Number, Is.EqualTo(37));
		Assert.That(owner.SecondScreen.Values.Number, Is.EqualTo(37));
		Assert.That(owner.FirstScreen.OperationalDraft, Is.EqualTo("Unsaved operation"));
	}

	/// <summary>Verifies renderer veto and hide do not save while failed accepted close keeps ownership for retry.</summary>
	[Test]
	public async Task RendererVetoAndHideDoNotSaveWhileFailedAcceptedCloseKeepsOwnershipForRetry() {
		var browser = new BrowserState("lifecycle");
		await using var provider = new ProviderOwner(browser, null);
		await using var owner = await LifecycleOwner.CreateAsync(provider.Provider);
		var screen = owner.FirstScreen;
		await owner.Renderer.Dispatcher.InvokeAsync(async () => {
			await owner.Host.ShowScreenAsync(owner.First);
			screen.Edit(15);
			await owner.Host.ShowScreenAsync(owner.Second);
			Assert.That(screen.Deactivations, Is.EqualTo(1));
			Assert.That(browser.Values, Is.Empty, "Hiding a retained screen does not save preferences.");
			await owner.Host.ShowScreenAsync(owner.First);
			screen.AllowLeave = false;
			Assert.That(await owner.Host.CloseScreenAsync(owner.First), Is.False);
			Assert.That(browser.Values, Is.Empty);
			screen.AllowLeave = true;
			browser.FailWrite = true;
			var deactivations = screen.Deactivations;
			await Assert.ThatAsync(async () => await owner.Host.CloseScreenAsync(owner.First), Throws.InstanceOf<JSException>());
			Assert.That(owner.Host.ActiveScreen, Is.SameAs(owner.First));
			Assert.That(owner.First.Screen, Is.SameAs(screen));
			Assert.That(screen.Host, Is.SameAs(owner.Host));
			Assert.That(screen.Deactivations, Is.EqualTo(deactivations));
			Assert.That(screen.SettingsError, Does.Contain("unavailable"));
			browser.FailWrite = false;
			Assert.That(await owner.Host.CloseScreenAsync(owner.First), Is.True);
		});
		Assert.That(browser.Values, Has.Count.EqualTo(1));
	}

	/// <summary>Approved application finalization persists dirty mounted screens before their provider is released.</summary>
	[Test]
	public async Task ApprovedFinalizationSavesMountedSettingsOnceAfterInitializedStateIsCleared() {
		var browser = new BrowserState("finalization");
		await using var provider = new ProviderOwner(browser, null);
		await using var owner = await LifecycleOwner.CreateAsync(provider.Provider);
		var application = owner.First.Block.Parent;
		var changes = 0;
		owner.Settings.OnChanged += () => changes++;
		Assert.That(application.IsInitialized, Is.True);

		await owner.Renderer.Dispatcher.InvokeAsync(async () => {
			owner.FirstScreen.Edit(63);
			await application.FinalizeAsync();
		});

		Assert.That(application.IsInitialized, Is.False);
		Assert.That(browser.Values, Has.Count.EqualTo(1));
		Assert.That(changes, Is.EqualTo(1));
		var stored = (Options)await provider.Provider.LoadSettingAsync(typeof(Options), owner.Settings.ID);
		Assert.That(stored.Number, Is.EqualTo(63));

		await application.FinalizeAsync();
		Assert.That(changes, Is.EqualTo(1), "Repeated finalization must not save the accepted edit again.");
	}

	/// <summary>Verifies committed browser write notifies even when the caller cancels at acknowledgement.</summary>
	[Test]
	public async Task CommittedBrowserWriteNotifiesEvenWhenTheCallerCancelsAtAcknowledgement() {
		using var cancellation = new CancellationTokenSource();
		var browser = new BrowserState("browser") { AfterWrite = cancellation.Cancel };
		await using var owner = new ProviderOwner(browser, null);
		var settings = Snapshot(44);
		settings.Provider = owner.Provider;
		var changes = 0;
		settings.OnChanged += () => changes++;
		await settings.SaveAsync(cancellation.Token);
		Assert.That(changes, Is.EqualTo(1));
		Assert.That(((Options)await owner.Provider.LoadSettingAsync(Defaults())).Number, Is.EqualTo(44));
	}

	/// <summary>Verifies reset and cancel use the actual root and preserve previously applied grid values.</summary>
	[Test]
	public async Task ResetAndCancelUseTheActualRootAndPreservePreviouslyAppliedGridValues() {
		await using var owner = new ProviderOwner(new BrowserState("reset"), null);
		await using var services = CreateServices(owner.Provider, SettingsBlock("preferences", Defaults()));
		var application = services.GetRequiredService<BlazorUI.IApplication>();
		await application.InitializeAsync();
		var definition = application.Blocks.Single().ScreenDefinitions.Single();
		var settings = (Options)definition.Settings;
		await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			ApplicationSettingsDialog dialog = null;
			await renderer.RenderComponentAsync<DialogHarness>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(DialogHarness.Application)] = application,
				[nameof(DialogHarness.Owner)] = definition,
				[nameof(DialogHarness.Capture)] = (Action<ApplicationSettingsDialog>)(component => dialog = component)
			}));
			var editor = (IApplicationSettingsEditor)GetField<DynamicComponent>(dialog, "_editor").Instance;
			settings.Number = 33;
			settings.Grid.Columns.Single().Width = 281;
			await editor.Changed.InvokeAsync();
			await dialog.ApplyAsync();
			dialog.ResetDefaults();
			Assert.That(settings.Number, Is.EqualTo(10));
			Assert.That(settings.Grid.Columns.Single().Width, Is.EqualTo(150));
			dialog.CancelEditing();
			Assert.That(definition.Settings, Is.SameAs(settings));
			Assert.That(settings.Number, Is.EqualTo(33));
			Assert.That(settings.Grid.Columns.Single().Width, Is.EqualTo(281));
		});
	}

	/// <summary>Verifies pending custom validation keeps its owner and prevents reset cancel or another selection.</summary>
	[TestCase(false)]
	[TestCase(true)]
	public async Task PendingCustomValidationKeepsItsOwnerAndPreventsResetCancelOrAnotherSelection(bool valid) {
		await using var owner = new ProviderOwner(new BrowserState("validation"), null);
		await using var services = CreateServices(owner.Provider, SettingsBlock("first", Defaults(), typeof(BlockingValidationEditor)), SettingsBlock("second", Defaults()));
		var application = services.GetRequiredService<BlazorUI.IApplication>();
		await application.InitializeAsync();
		var definitions = application.Blocks.Select(block => block.ScreenDefinitions.Single()).ToArray();
		await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			ApplicationSettingsDialog dialog = null;
			await renderer.RenderComponentAsync<DialogHarness>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(DialogHarness.Application)] = application,
				[nameof(DialogHarness.Owner)] = definitions[0],
				[nameof(DialogHarness.Capture)] = (Action<ApplicationSettingsDialog>)(component => dialog = component)
			}));
			var editor = (BlockingValidationEditor)GetField<DynamicComponent>(dialog, "_editor").Instance;
			var settings = (Options)definitions[0].Settings;
			settings.Number = 41;
			await editor.Changed.InvokeAsync();
			var release = new TaskCompletionSource<Result>(TaskCreationOptions.RunContinuationsAsynchronously);
			editor.PendingValidation = release.Task;
			var apply = dialog.ApplyAsync();
			dialog.ResetDefaults();
			dialog.CancelEditing();
			await dialog.SelectOwnerAsync(definitions[1]);
			Assert.That(GetField<object>(dialog, "_selected"), Is.SameAs(definitions[0]));
			Assert.That(settings.Number, Is.EqualTo(41));
			release.SetResult(valid ? Result.Success : Result.Error("Pending text is invalid."));
			await apply;
			Assert.That(await owner.Provider.ContainsSettingAsync(typeof(Options), settings.ID), Is.EqualTo(valid));
		});
	}

	/// <summary>Verifies restoration populates the actual owner when the provider returns another record.</summary>
	[Test]
	public async Task RestorationPopulatesTheActualOwnerWhenTheProviderReturnsAnotherRecord() {
		var provider = new DistinctRecordProvider();
		await using var services = CreateServices(provider, SettingsBlock("preferences", Defaults()));
		var application = services.GetRequiredService<BlazorUI.IApplication>();
		var definition = application.Blocks.Single().ScreenDefinitions.Single();
		var actual = definition.Settings;
		await application.InitializeAsync();
		Assert.That(definition.Settings, Is.SameAs(actual));
		Assert.That(((Options)actual).Number, Is.EqualTo(73));
		Assert.That(actual.ID, Is.EqualTo("preferences"));
		Assert.That(actual.Provider, Is.SameAs(provider));
	}

	/// <summary>Damaged or invalid browser records cannot replace usable owner defaults or publish changes.</summary>
	[TestCase("{\"Number\":0,\"Nested\":{\"Text\":\"Changed\"}}", false)]
	[TestCase("{\"Number\":31,\"Nested\":{\"Text\":\"Changed\"},\"Numbers\":\"broken\"}", false)]
	[TestCase("{\"Number\":0,\"Nested\":{\"Text\":\"Changed\"}}", true)]
	[TestCase("{\"Number\":31,\"Nested\":{\"Text\":\"Changed\"},\"Numbers\":\"broken\"}", true)]
	public async Task ApplicationRestorationRejectsInvalidAndPartiallyPopulatedValues(string payload, bool legacyEnvelope) {
		var browser = new BrowserState("invalid application settings");
		await using var owner = new ProviderOwner(browser, null);
		var defaults = Defaults();
		defaults.Nested.Text = "Configured nested default";
		await owner.Provider.SaveSettingAsync(defaults);
		var key = browser.Values.Keys.Single();
		browser.Values[key] = payload;
		var provider = new SettingsProviderDecorator(owner.Provider);
		await using var services = CreateServices(provider, SettingsBlock("preferences", defaults));
		var application = services.GetRequiredService<BlazorUI.IApplication>();
		var block = application.Blocks.Single();
		var definition = block.ScreenDefinitions.Single();
		var actual = (Options)definition.Settings;
		var changes = 0;
		actual.OnChanged += () => changes++;
		if (legacyEnvelope) {
			var prefix = key[..(key.IndexOf(":app/", StringComparison.Ordinal) + 1)];
			var legacyKey = ApplicationHelper.GetScreenSettingsKey(Tools.Runtime.GetEntryAssembly().GetName().Name, block, definition, block.Id);
			browser.Values.Clear();
			key = prefix + legacyKey;
			browser.Values[key] = "{\"values\":" + payload + "}";
		}
		var storedPayload = browser.Values[key];

		await application.InitializeAsync();

		Assert.That(definition.Settings, Is.SameAs(actual));
		Assert.That(actual.Number, Is.EqualTo(10));
		Assert.That(actual.Nested.Text, Is.EqualTo("Configured nested default"));
		Assert.That(actual.Numbers, Is.EqualTo(new[] { 10, 20 }));
		Assert.That(actual.ID, Is.EqualTo("preferences"));
		Assert.That(actual.Provider, Is.SameAs(provider));
		Assert.That(changes, Is.Zero);
		Assert.That(browser.Values[key], Is.EqualTo(storedPayload), "An optional rejected record is not rewritten during startup.");
	}

	/// <summary>Annotations participate in load validation even when the settings class has no custom Validate override.</summary>
	[Test]
	public async Task ApplicationRestorationRejectsAnnotationFailures() {
		var browser = new BrowserState("annotation validation");
		await using var owner = new ProviderOwner(browser, null);
		var defaults = new AnnotatedOptions { ID = "annotations", Number = 5, Caption = "Configured default" };
		await owner.Provider.SaveSettingAsync(defaults);
		var key = browser.Values.Keys.Single();
		browser.Values[key] = "{\"Number\":0,\"Caption\":\"Changed\"}";
		var definition = new BlazorUI.ApplicationScreenDefinition(typeof(DeclaredEditorScreen), "Annotations", stableKey: "annotations", settings: defaults);
		await using var services = CreateServices(owner.Provider,
			new ApplicationBlock { Id = "annotations", Title = "Annotations", ScreenDefinitions = new[] { definition } });
		var application = services.GetRequiredService<BlazorUI.IApplication>();
		var actual = (AnnotatedOptions)application.Blocks.Single().ScreenDefinitions.Single().Settings;

		await application.InitializeAsync();

		Assert.That(actual.Number, Is.EqualTo(5));
		Assert.That(actual.Caption, Is.EqualTo("Configured default"));
		Assert.That(actual.ID, Is.EqualTo("annotations"));
		Assert.That(actual.Provider, Is.SameAs(owner.Provider));
	}

	/// <summary>Cancellation restores a partial overlay before the normal cancellation escapes initialization.</summary>
	[Test]
	public async Task CancelledRestorationRetainsTheActualDefaultValues() {
		await using var owner = new ProviderOwner(new BrowserState("cancelled restoration"), null);
		using var cancellation = new CancellationTokenSource();
		var provider = new CancelledOverlayProvider(owner.Provider, cancellation);
		await using var services = CreateServices(provider, SettingsBlock("preferences", Defaults()));
		var application = services.GetRequiredService<BlazorUI.IApplication>();
		var definition = application.Blocks.Single().ScreenDefinitions.Single();
		var actual = (Options)definition.Settings;

		await Assert.ThatAsync(async () => await application.InitializeAsync(cancellation.Token), Throws.InstanceOf<OperationCanceledException>());

		Assert.That(definition.Settings, Is.SameAs(actual));
		Assert.That(actual.Number, Is.EqualTo(10));
		Assert.That(actual.Nested.Text, Is.EqualTo("default"));
		Assert.That(actual.ID, Is.EqualTo("preferences"));
		Assert.That(actual.Provider, Is.SameAs(provider));
	}

	/// <summary>A second completed edit remains dirty while an earlier save waits for acknowledgement.</summary>
	[Test]
	public async Task EditDuringPendingSaveIsPersistedByTheNextSave() {
		var browser = new BrowserState("pending save edits");
		await using var store = new ProviderOwner(browser, null);
		var provider = new PendingSaveProvider(store.Provider);
		await using var owner = await LifecycleOwner.CreateAsync(provider);
		using var release = Tools.Scope.ExecuteOnDispose(() => provider.Release.TrySetResult());
		await owner.Renderer.Dispatcher.InvokeAsync(async () => {
			owner.FirstScreen.Edit(20);
			var saving = owner.FirstScreen.SaveSettingsAsync();
			await provider.Entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
			Assert.That(JObject.Parse(browser.Values.Single().Value)[nameof(Options.Number)].Value<int>(), Is.EqualTo(20));
			owner.FirstScreen.Edit(37);
			provider.Release.TrySetResult();
			await saving;
			await owner.FirstScreen.SaveSettingsAsync();
		});

		Assert.That(provider.SaveCalls, Is.EqualTo(2));
		Assert.That(JObject.Parse(browser.Values.Single().Value)[nameof(Options.Number)].Value<int>(), Is.EqualTo(37));
	}

	/// <summary>Verifies unmarked settings require an explicit editor without losing their persistence.</summary>
	[TestCase(false)]
	[TestCase(true)]
	public async Task UnmarkedSettingsRequireAnExplicitEditorWithoutLosingTheirPersistence(bool customEditor) {
		await using var owner = new ProviderOwner(new BrowserState("unmarked"), null);
		var definition = new BlazorUI.ApplicationScreenDefinition(typeof(DeclaredEditorScreen), "Preferences", stableKey: "preferences",
			settings: new TransientPreferences(), settingsEditor: customEditor ? typeof(CustomEditor) : null);
		await using var services = CreateServices(owner.Provider,
			new ApplicationBlock { Id = "preferences", Title = "Preferences", ScreenDefinitions = new[] { definition } });
		var application = services.GetRequiredService<BlazorUI.IApplication>();
		await application.InitializeAsync();
		var actualDefinition = application.Blocks.Single().ScreenDefinitions.Single();
		await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			ApplicationSettingsDialog dialog = null;
			var rendered = await renderer.RenderComponentAsync<DialogHarness>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(DialogHarness.Application)] = application,
				[nameof(DialogHarness.Owner)] = customEditor ? actualDefinition : null,
				[nameof(DialogHarness.Capture)] = (Action<ApplicationSettingsDialog>)(component => dialog = component)
			}));
			Assert.That(GetField<object>(dialog, "_selected"), Is.SameAs(actualDefinition));
			if (customEditor)
				Assert.That(((CustomEditor)GetField<DynamicComponent>(dialog, "_editor").Instance).Value, Is.SameAs(actualDefinition.Settings));
			else
				Assert.That(rendered.ToHtmlString(), Does.Contain("not enabled automatic user editing"));
		});
		((TransientPreferences)actualDefinition.Settings).Name = "Saved by the owner";
		await actualDefinition.Settings.SaveAsync();
		Assert.That(await owner.Provider.ContainsSettingAsync(typeof(TransientPreferences), actualDefinition.Settings.ID), Is.True);
	}

	// Compose the actual application/provider lifecycle without activating operational screens.
	private static ServiceProvider CreateServices(ISettingsProvider provider, params BlazorUI.IApplicationBlock[] blocks) {
		var services = new ServiceCollection().AddLogging().AddSingleton<ISettingsProvider>(provider).AddSphere10Blazor();
		foreach (var block in blocks)
			services.AddApplicationBlock(block);
		return services.BuildServiceProvider();
	}


	/// <summary>Verifies transient properties remain excluded across overrides and circuit recreation.</summary>
	[TestCase(false)]
	[TestCase(true)]
	public async Task TransientPropertiesRemainExcludedAcrossOverridesAndCircuitRecreation(bool overridden) {
		var browser = new BrowserState("browser");
		TransientPreferences values = overridden ? new InheritedTransientPreferences() : new TransientPreferences();
		values.Name = "Accepted";
		values.Runtime = "must-not-persist";
		values.ID = "preferences";
		await using (var owner = new ProviderOwner(browser, null)) {
			await owner.Provider.SaveSettingAsync(values);
		}

		var record = browser.Values.Single();
		Assert.That(record.Value, Does.Not.Contain("must-not-persist").And.Not.Contain("Runtime"));
		var payload = JObject.Parse(record.Value);
		payload[nameof(TransientPreferences.Runtime)] = "ignored-old-runtime";
		browser.Values[record.Key] = payload.ToString();

		await using var restarted = new ProviderOwner(browser, null);
		TransientPreferences defaults = overridden ? new InheritedTransientPreferences() : new TransientPreferences();
		defaults.ID = "preferences";
		var runtimeDefault = defaults.Runtime;
		var restored = (TransientPreferences)await restarted.Provider.LoadSettingAsync(defaults);
		Assert.That(restored.Name, Is.EqualTo("Accepted"));
		Assert.That(restored.Runtime, Is.EqualTo(runtimeDefault));
	}

	/// <summary>Verifies provider survives circuit recreation and keeps application user and registration namespaces separate.</summary>
	[TestCase(false)]
	[TestCase(true)]
	public async Task ProviderSurvivesCircuitRecreationAndKeepsApplicationUserAndRegistrationNamespacesSeparate(bool authenticated) {
		var browser = new BrowserState("browser-one");
		await using (var first = new ProviderOwner(browser, authenticated ? "alice" : null, "app-one")) {
			await first.Provider.SaveSettingAsync(Snapshot(25));
		}
		await using var restarted = new ProviderOwner(browser, authenticated ? "alice" : null, "app-one");
		await using var otherApplication = new ProviderOwner(browser, authenticated ? "alice" : null, "app-two");
		await using var otherUser = new ProviderOwner(browser, authenticated ? "bob" : "signed-in-user", "app-one");
		await using var otherBrowser = new ProviderOwner(new BrowserState("browser-two"), authenticated ? "alice" : null, "app-one");

		var loaded = (Options)await restarted.Provider.LoadSettingAsync(Defaults());
		Assert.That(loaded.Number, Is.EqualTo(25));
		Assert.That(loaded.Provider, Is.SameAs(restarted.Provider));
		Assert.That(loaded.ID, Is.EqualTo("preferences"));
		Assert.That(await restarted.Provider.ContainsSettingAsync(typeof(Options), "another-registration"), Is.False);
		Assert.That(await otherApplication.Provider.ContainsSettingAsync(typeof(Options), "preferences"), Is.False);
		Assert.That(await otherUser.Provider.ContainsSettingAsync(typeof(Options), "preferences"), Is.False);
		Assert.That(await otherBrowser.Provider.ContainsSettingAsync(typeof(Options), "preferences"), Is.False);
	}

	/// <summary>Verifies existing circuit cannot retarget its live profile when the authenticated subject changes.</summary>
	[Test]
	public async Task ExistingCircuitCannotRetargetItsLiveProfileWhenTheAuthenticatedSubjectChanges() {
		var browser = new BrowserState("browser");
		await using var owner = new ProviderOwner(browser, "alice");
		await owner.Provider.SaveSettingAsync(Snapshot(10));
		var original = browser.Values.Single();
		owner.Authentication.Subject = "bob";

		await Assert.ThatAsync(async () => await owner.Provider.LoadSettingAsync(Defaults()), Throws.InvalidOperationException);
		await Assert.ThatAsync(async () => await owner.Provider.SaveSettingAsync(Snapshot(20)), Throws.InvalidOperationException);
		Assert.That(browser.Values.Single(), Is.EqualTo(original));
	}

	/// <summary>Verifies user change during serialization cannot commit into the old browser namespace.</summary>
	[Test]
	public async Task UserChangeDuringSerializationCannotCommitIntoTheOldBrowserNamespace() {
		var browser = new BrowserState("browser");
		await using var owner = new ProviderOwner(browser, "alice");
		await owner.Provider.SaveSettingAsync(Snapshot(10));
		var original = browser.Values.Single();
		var settings = new SerializationProbe { Serialized = () => owner.Authentication.Subject = "bob" };

		await Assert.ThatAsync(async () => await owner.Provider.SaveSettingAsync(settings), Throws.InvalidOperationException);

		Assert.That(browser.Values.Single(), Is.EqualTo(original));
	}

	/// <summary>Verifies delete and clear retain other application and user settings.</summary>
	[Test]
	public async Task DeleteAndClearRetainOtherApplicationAndUserSettings() {
		var browser = new BrowserState("browser");
		await using var owner = new ProviderOwner(browser, "alice", "one");
		await using var otherApplication = new ProviderOwner(browser, "alice", "two");
		await using var otherUser = new ProviderOwner(browser, "bob", "one");
		await owner.Provider.SaveSettingAsync(Snapshot(10));
		await otherApplication.Provider.SaveSettingAsync(Snapshot(20));
		await otherUser.Provider.SaveSettingAsync(Snapshot(30));
		await owner.Provider.DeleteSettingAsync(Defaults());
		Assert.That(await owner.Provider.ContainsSettingAsync(typeof(Options), "preferences"), Is.False);
		await owner.Provider.SaveSettingAsync(Snapshot(40));
		await owner.Provider.ClearSettingsAsync();

		Assert.That(await owner.Provider.ContainsSettingAsync(typeof(Options), "preferences"), Is.False);
		Assert.That(((Options)await otherApplication.Provider.LoadSettingAsync(Defaults())).Number, Is.EqualTo(20));
		Assert.That(((Options)await otherUser.Provider.LoadSettingAsync(Defaults())).Number, Is.EqualTo(30));
	}

	/// <summary>Verifies provider hydrates only the declared type and rejects malformed settings.</summary>
	[Test]
	public async Task ProviderHydratesOnlyTheDeclaredTypeAndRejectsMalformedSettings() {
		var browser = new BrowserState("browser");
		await using var owner = new ProviderOwner(browser, "alice");
		await owner.Provider.SaveSettingAsync(Snapshot(18));
		var key = browser.Values.Keys.Single();
		var payload = JObject.Parse(browser.Values[key]);
		payload["$type"] = typeof(UnrelatedSettings).AssemblyQualifiedName;
		browser.Values[key] = payload.ToString();

		var loaded = await owner.Provider.LoadSettingAsync(Defaults());
		Assert.That(loaded.GetType(), Is.EqualTo(typeof(Options)));
		Assert.That(((Options)loaded).Number, Is.EqualTo(18));
		browser.Values[key] = "[]";
		await Assert.ThatAsync(async () => await owner.Provider.LoadSettingAsync(Defaults()), Throws.TypeOf<JsonSerializationException>());
	}

	/// <summary>Verifies partial payload keeps factory defaults and explicit empty collections replace them.</summary>
	[Test]
	public async Task PartialPayloadKeepsFactoryDefaultsAndExplicitEmptyCollectionsReplaceThem() {
		var browser = new BrowserState("browser");
		await using var owner = new ProviderOwner(browser, null);
		var defaults = new PartialOptions {
			ID = "partial",
			Caption = "Registration factory default",
			Nested = new NestedOptions { Text = "Nested factory default" },
			Items = new List<string> { "Factory item" },
			Tags = new[] { "Factory tag" },
			Roles = new Dictionary<string, string> { ["factory"] = "Factory role" }
		};
		await owner.Provider.SaveSettingAsync(defaults);
		var key = browser.Values.Keys.Single();
		browser.Values[key] = "{\"Nested\":{},\"Items\":[],\"Tags\":[],\"Roles\":{}}";
		var storedPayload = browser.Values[key];

		var values = (PartialOptions)await owner.Provider.LoadSettingAsync(defaults);

		Assert.That(values.Caption, Is.EqualTo("Registration factory default"));
		Assert.That(values.Nested.Text, Is.EqualTo("Nested factory default"));
		Assert.That(values.Items, Is.Empty);
		Assert.That(values.Tags, Is.Empty);
		Assert.That(values.Roles, Is.Empty);
		Assert.That(browser.Values[key], Is.EqualTo(storedPayload), "Restoring missing defaults must not rewrite the stored record.");
	}

	/// <summary>Verifies explicit null in stored values is not replaced with factory defaults.</summary>
	[Test]
	public async Task ExplicitNullInStoredValuesIsNotReplacedWithFactoryDefaults() {
		var browser = new BrowserState("browser");
		await using var owner = new ProviderOwner(browser, null);
		var defaults = new PartialOptions { ID = "partial", Caption = "Factory caption" };
		await owner.Provider.SaveSettingAsync(defaults);
		var key = browser.Values.Keys.Single();
		browser.Values[key] = "{\"Caption\":null}";

		var loaded = await owner.Provider.LoadSettingAsync(defaults);

		Assert.That(((PartialOptions)loaded).Caption, Is.Null);
	}

	/// <summary>Verifies provider serializes its exclusive snapshot outside the owners synchronization context.</summary>
	[Test]
	public async Task ProviderSerializesItsExclusiveSnapshotOutsideTheOwnersSynchronizationContext() {
		await using var owner = new ProviderOwner(new BrowserState("browser"), null);
		var context = new SynchronizationContext();
		SynchronizationContext observed = context;
		var values = new SerializationProbe { Serialized = () => observed = SynchronizationContext.Current };
		var original = SynchronizationContext.Current;
		Task saving;
		SynchronizationContext.SetSynchronizationContext(context);
		try {
			saving = owner.Provider.SaveSettingAsync(values);
		} finally {
			SynchronizationContext.SetSynchronizationContext(original);
		}
		await saving;

		Assert.That(observed, Is.Not.SameAs(context));
	}

	/// <summary>Verifies default editor renders readable nested values and schema columns without provider or membership controls.</summary>
	[Test]
	public async Task DefaultEditorRendersReadableNestedValuesAndSchemaColumnsWithoutProviderOrMembershipControls() {
		await using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
		var values = new Options { ID = "private-storage-id" };
		await renderer.Dispatcher.InvokeAsync(async () => {
			var rendered = await renderer.RenderComponentAsync<ApplicationSettingsValueEditor>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(ApplicationSettingsValueEditor.Value)] = values
			}));
			var html = rendered.ToHtmlString();
			Assert.That(html, Does.Contain("Refresh count"));
			Assert.That(html, Does.Contain("Nested preference"));
			Assert.That(html, Does.Contain("Name column"));
			Assert.That(html, Does.Contain("Manual width"));
			Assert.That(html, Does.Contain("<output"));
			Assert.That(html, Does.Not.Contain(">Column key<"));
			Assert.That(html, Does.Not.Contain("private-storage-id"));
			Assert.That(html, Does.Not.Contain(">Provider<"));
			Assert.That(html, Does.Not.Contain("Add Column widths"));
			Assert.That(html, Does.Contain("Add Words"));
		});
	}

	/// <summary>Verifies dictionary grid roles render their readable schema and editable widths.</summary>
	[Test]
	public async Task DictionaryGridRolesRenderTheirReadableSchemaAndEditableWidths() {
		await using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			var rendered = await renderer.RenderComponentAsync<ApplicationSettingsValueEditor>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(ApplicationSettingsValueEditor.Value)] = new DictionaryOptions()
			}));
			var html = rendered.ToHtmlString();
			Assert.That(html, Does.Contain("processes"));
			Assert.That(html, Does.Contain("Process name"));
			Assert.That(html, Does.Contain("Manual width"));
		});
	}

	/// <summary>Verifies invalid collection item text remains an error when another item is edited successfully.</summary>
	[Test]
	public async Task InvalidCollectionItemTextRemainsAnErrorWhenAnotherItemIsEditedSuccessfully() {
		await using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			ApplicationSettingsValueEditor editor = null;
			var errors = new Dictionary<string, string>();
			var values = new Options();
			await renderer.RenderComponentAsync<ValueHarness>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(ValueHarness.Value)] = values,
				[nameof(ValueHarness.Errors)] = errors,
				[nameof(ValueHarness.Capture)] = (Action<ApplicationSettingsValueEditor>)(component => editor = component)
			}));
			var property = TypeDescriptor.GetProperties(values)[nameof(Options.Numbers)];
			await InvokeAsync(editor, "ChangeItemAsync", property, 0, "not-a-number");
			await InvokeAsync(editor, "ChangeItemAsync", property, 1, "30");

			Assert.That(errors, Has.Count.EqualTo(1), "Correcting a different collection item cannot discard invalid pending input.");
			Assert.That(values.Numbers, Is.EqualTo(new[] { 10, 30 }));
		});
	}

	/// <summary>Verifies application installs one screen options command into the existing heading without running operations.</summary>
	[Test]
	public async Task ApplicationInstallsOneScreenOptionsCommandIntoTheExistingHeadingWithoutRunningOperations() {
		var browser = new BrowserState("browser");
		var runtime = new BrowserRuntime(browser);
		var calls = 0;
		var services = new ServiceCollection().AddLogging().AddSingleton<IJSRuntime>(runtime);
		new ApplicationBuilder(services)
			.WithSettingsKey("stable-app")
			.AddPlugin(plugin => plugin.WithName("Preferences").UseSettings<Options>())
			.OnApplicationCreated(application => application.SetMenus(new[] {
				new ApplicationMenu { Id = "existing-options", Text = "&Options", Items = new[] {
					new BlazorActionMenuItem { Id = "operation", Title = "Existing operation", Action = () => calls++ }
				} }
			}))
			.Build();
		await using var provider = services.BuildServiceProvider();
		var application = provider.GetRequiredService<BlazorUI.IApplication>();

		await application.InitializeAsync();
		await application.InitializeAsync();

		Assert.That(application.Menus, Has.Length.EqualTo(1));
		Assert.That(application.Menus.Single().Id, Is.EqualTo("existing-options"));
		Assert.That(application.Menus.Single().Items.Select(item => item.Id), Is.EqualTo(new[] { "operation", "sphere10-screen-options" }));
		Assert.That(calls, Is.Zero);
		((BlazorActionMenuItem)application.Menus.Single().Items.First()).Action();
		Assert.That(calls, Is.EqualTo(1));
	}

	/// <summary>The real Options command passes the configured product name to its modal rather than the shell caption.</summary>
	[TestCase("System Expert", "System Expert")]
	[TestCase(null, "Host caption")]
	[TestCase("", "Host caption")]
	[TestCase("  ", "Host caption")]
	public async Task OptionsCommandUsesTheConfiguredProductName(string productName, string expectedCaption) {
		var displayed = new TaskCompletionSource<ApplicationSettingsDialog>(TaskCreationOptions.RunContinuationsAsynchronously);
		var services = new ServiceCollection()
			.AddLogging()
			.AddSingleton<IJSRuntime>(new BrowserRuntime(new BrowserState("product caption")))
			.AddSingleton(displayed);
		services.AddApplicationBlock(SettingsBlock("options", Defaults()));
		services
			.BuildBlazorApplication()
			.WithTitle("Host caption")
			.Build();
		services.AddSingleton<IProductInformationProvider>(new CaptionProductInformationProvider(productName));
		await using var provider = services.BuildServiceProvider();
		var application = provider.GetRequiredService<BlazorUI.IApplication>();
		await application.InitializeAsync();
		await using var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			await renderer.RenderComponentAsync<OptionsCaptionHost>();
			var command = (BlazorActionMenuItem)application.Menus.SelectMany(menu => menu.Items).Single(item => item.Id == "sphere10-screen-options");
			await command.AsyncAction(provider, CancellationToken.None);
		});

		var dialog = await displayed.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(dialog.Application, Is.SameAs(application));
		Assert.That(dialog.ApplicationTitle, Is.EqualTo(expectedCaption));
	}

	// Provider calls exercise actual persistence; only browser interop and principal discovery are substituted.
	private static Options Defaults() {
		return new Options { ID = "preferences" };
	}

	/// <summary>Creates a settings value with the requested refresh count and persistent ID.</summary>
	private static Options Snapshot(int number) {
		return new Options { ID = "preferences", Number = number };
	}

	// Each Options page belongs to a statically declared screen rather than a separate root descriptor.
	private static BlazorUI.IApplicationBlock SettingsBlock(string key, SettingsObject settings, Type editor = null) {
		var screenType = settings is RegistrationExampleScreen.RegistrationInspectorSettings ? typeof(RegistrationExampleScreen) : typeof(UnopenedScreen);
		var definition = new BlazorUI.ApplicationScreenDefinition(screenType, key, stableKey: key, settings: settings, settingsEditor: editor);
		return new ApplicationBlock { Id = key, Title = key, ScreenDefinitions = new[] { definition } };
	}

	// HtmlRenderer owns components; reflection invokes the same private event handlers used by their rendered controls.
	private static Task InvokeAsync(object component, string method, params object[] arguments) {
		return (Task)component.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(component, arguments);
	}

	/// <summary>Reads a renderer-owned test component member for its interaction assertions.</summary>
	private static T GetField<T>(object component, string field) {
		return (T)component.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(component);
	}

	#region Inner Types

	/// <summary>Provides annotation-only constraints for optional owner restoration.</summary>
	public class AnnotatedOptions : SettingsObject {
		/// <summary>The validated positive page size.</summary>
		[System.ComponentModel.DataAnnotations.Range(1, 10)]
		public int Number { get; set; }

		/// <summary>A configured scalar default that must survive rejection.</summary>
		public string Caption { get; set; }
	}

	/// <summary>Pauses the first acknowledged write at the outer provider boundary.</summary>
	private sealed class PendingSaveProvider : SettingsProviderDecorator {
		/// <summary>Borrows the actual browser-backed store used by the application.</summary>
		public PendingSaveProvider(ISettingsProvider provider)
			: base(provider) {
		}

		/// <summary>Signals the first complete snapshot before its outer acknowledgement.</summary>
		public TaskCompletionSource Entered { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		/// <summary>Releases the pending first acknowledgement.</summary>
		public TaskCompletionSource Release { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		/// <summary>The number of actual persistence calls received by this outer provider.</summary>
		public int SaveCalls { get; private set; }

		/// <inheritdoc />
		public override async Task SaveSettingAsync(SettingsObject settings, CancellationToken cancellationToken = default) {
			SaveCalls++;
			await base.SaveSettingAsync(settings, cancellationToken);
			if (SaveCalls != 1)
				return;
			Entered.TrySetResult();
			await Release.Task.WaitAsync(cancellationToken);
		}
	}

	/// <summary>Simulates cancellation after storage has changed part of the supplied actual object.</summary>
	private sealed class CancelledOverlayProvider : SettingsProviderDecorator {
		private readonly CancellationTokenSource _cancellation;

		/// <summary>Borrows a real provider and the cancellation owner used by this read.</summary>
		public CancelledOverlayProvider(ISettingsProvider provider, CancellationTokenSource cancellation)
			: base(provider) {
			_cancellation = cancellation;
		}

		/// <inheritdoc />
		public override Task<bool> ContainsSettingAsync(Type settingsObjectType, object id = null, CancellationToken cancellationToken = default) {
			return Task.FromResult(true);
		}

		/// <inheritdoc />
		public override Task<SettingsObject> LoadSettingAsync(SettingsObject defaults, CancellationToken cancellationToken = default) {
			var values = (Options)defaults;
			values.Number = 99;
			values.Nested.Text = "Partially loaded";
			_cancellation.Cancel();
			cancellationToken.ThrowIfCancellationRequested();
			return Task.FromResult(defaults);
		}
	}

	/// <summary>Supplies product identity independently of the web host caption.</summary>
	private sealed class CaptionProductInformationProvider : IProductInformationProvider {
		/// <summary>Retains the product name selected by application composition.</summary>
		public CaptionProductInformationProvider(string productName) {
			ProductInformation = new ProductInformation { ProductName = productName };
		}

		/// <inheritdoc />
		public ProductInformation ProductInformation { get; }
	}

	/// <summary>Captures the actual command-created Options component and dismisses it without browser interaction.</summary>
	public sealed class OptionsCaptionHost : Sphere10.Framework.Web.AspNetCore.Blazor.UI.Dialogs.ModalHost {
		/// <summary>The existing modal service used by application commands.</summary>
		[Inject] public ModalService ModalService { get; set; }

		/// <summary>The completion receiving the real rendered Options component.</summary>
		[Inject] public TaskCompletionSource<ApplicationSettingsDialog> Displayed { get; set; }

		/// <inheritdoc />
		protected override void OnInitialized() {
			ModalService.Initialize(this);
		}

		/// <inheritdoc />
		protected override Task WaitUntilRenderedAsync(ModalComponent component) {
			return Task.CompletedTask;
		}

		/// <inheritdoc />
		protected override Task<ModalResult> GetResultAsync(ModalComponent component) {
			Displayed.TrySetResult((ApplicationSettingsDialog)component);
			return Task.FromResult(ModalResult.Cancel);
		}
	}

	/// <summary>Hosts the real default value editor and exposes its renderer-created instance to interaction tests.</summary>
	public sealed class ValueHarness : ComponentBase {
		/// <summary>The actual settings value supplied to this fixture editor.</summary>
		[Parameter] public object Value { get; set; }

		/// <summary>Pending input errors retained by the default editor.</summary>
		[Parameter] public IDictionary<string, string> Errors { get; set; } = new Dictionary<string, string>();

		/// <summary>Captures the renderer-created component for interaction checks.</summary>
		[Parameter] public Action<ApplicationSettingsValueEditor> Capture { get; set; }

		/// <inheritdoc />
		protected override void BuildRenderTree(RenderTreeBuilder builder) {
			builder.OpenComponent<ApplicationSettingsValueEditor>(0);
			builder.AddAttribute(1, nameof(ApplicationSettingsValueEditor.Value), Value);
			builder.AddAttribute(2, nameof(ApplicationSettingsValueEditor.Errors), Errors);
			builder.AddComponentReferenceCapture(3, value => Capture((ApplicationSettingsValueEditor)value));
			builder.CloseComponent();
		}
	}

	/// <summary>Hosts the real Options dialog with an actual application owner and selection.</summary>
	public sealed class DialogHarness : ComponentBase {
		/// <summary>The actual application supplied to the dialog.</summary>
		[Parameter] public BlazorUI.IApplication Application { get; set; }

		/// <summary>The actual plugin, block or screen definition selected in the dialog.</summary>
		[Parameter] public object Owner { get; set; }

		/// <summary>Captures the renderer-created component for interaction checks.</summary>
		[Parameter] public Action<ApplicationSettingsDialog> Capture { get; set; }

		/// <inheritdoc />
		protected override void BuildRenderTree(RenderTreeBuilder builder) {
			builder.OpenComponent<ApplicationSettingsDialog>(0);
			builder.AddAttribute(1, nameof(ApplicationSettingsDialog.Application), Application);
			builder.AddAttribute(2, nameof(ApplicationSettingsDialog.Options), new ModalOptions());
			builder.AddAttribute(3, nameof(ApplicationSettingsDialog.SelectedOwner), Owner);
			builder.AddComponentReferenceCapture(4, value => Capture((ApplicationSettingsDialog)value));
			builder.CloseComponent();
		}
	}

	/// <summary>Supplies scalar, nested, collection and grid values for settings behavior checks.</summary>
	[UserEditable]
	public class Options : SettingsObject {
		/// <summary>The refresh count used by the fixture.</summary>
		[DisplayName("Refresh count")]
		[DefaultValue(10)]
		public int Number { get; set; } = 10;

		/// <summary>A scalar value that the automatic editor must display without allowing changes.</summary>
		[ReadOnly(true)]
		[DisplayName("Read-only preference")]
		[DefaultValue("read only")]
		public string ReadOnlyValue { get; set; } = "read only";

		/// <summary>The selected enum value used by conversion and rendering checks.</summary>
		[DefaultValue(OptionMode.First)]
		public OptionMode Mode { get; set; }

		/// <summary>The nested editable settings values.</summary>
		public NestedOptions Nested { get; set; } = new();

		/// <summary>The numeric collection used to verify independent pending-input errors.</summary>
		public int[] Numbers { get; set; } = { 10, 20 };

		/// <summary>The editable text collection.</summary>
		public string[] Words { get; set; } = { "first" };

		/// <summary>The declared grid schema and manual widths.</summary>
		public BlazorGridSettings Grid { get; set; } = new() {
			Columns = new[] { new ApplicationGridColumnPreferences { Key = "name", Title = "Name column", Width = 150 } }
		};

		/// <summary>Restores the fixture's declared nested defaults through the normal settings lifecycle.</summary>
		public override void RestoreDefaultValues() {
			base.RestoreDefaultValues();
			Nested = new NestedOptions();
			Numbers = new[] { 10, 20 };
			Words = new[] { "first" };
			Grid = new BlazorGridSettings {
				Columns = new[] { new ApplicationGridColumnPreferences { Key = "name", Title = "Name column", Width = 150 } }
			};
		}

		/// <inheritdoc />
		public override Result Validate() {
			return Number > 0 ? Result.Success : Result.Error("Refresh count must be positive.");
		}
	}

	/// <summary>Supplies constructor defaults for partial-record hydration checks.</summary>
	public class PartialOptions : SettingsObject {
		/// <summary>The settings heading used by default hydration and editor checks.</summary>
		public string Caption { get; set; } = "Constructor caption";

		/// <summary>The nested editable settings values.</summary>
		public NestedOptions Nested { get; set; } = new();

		/// <summary>The list used to verify replacement of explicit empty stored collections.</summary>
		public List<string> Items { get; set; } = new() { "Constructor item" };

		/// <summary>The array used to verify replacement of explicit empty stored collections.</summary>
		public string[] Tags { get; set; } = { "Constructor tag" };

		/// <summary>The dictionary used to verify replacement of explicit empty stored collections.</summary>
		public Dictionary<string, string> Roles { get; set; } = new() { ["constructor"] = "Constructor role" };
	}

	/// <summary>Supplies nested editable values for default-editor and persistence checks.</summary>
	public class NestedOptions {
		/// <summary>The nested text value used by the default editor.</summary>
		[DisplayName("Nested preference")]
		public string Text { get; set; } = "default";
	}

	/// <summary>Supplies named grid roles for dictionary editor coverage.</summary>
	[UserEditable]
	public class DictionaryOptions : SettingsObject {
		/// <summary>The named grid roles shown by the dictionary editor.</summary>
		public Dictionary<string, BlazorGridSettings> Grids { get; set; } = new() {
			["processes"] = new BlazorGridSettings {
				Columns = new[] { new ApplicationGridColumnPreferences { Key = "name", Title = "Process name", Width = 190 } }
			}
		};
	}

	/// <summary>Supplies persisted and transient properties for serializer filtering checks.</summary>
	public class TransientPreferences : SettingsObject {
		/// <summary>The persisted name used by the fixture.</summary>
		[DefaultValue("Default")]
		public string Name { get; set; } = "Default";

		/// <summary>The transient value that must remain excluded from persistence.</summary>
		[Transient]
		public virtual string Runtime { get; set; } = "Runtime default";
	}

	/// <summary>Verifies transient member metadata survives property overrides.</summary>
	public class InheritedTransientPreferences : TransientPreferences {
		/// <inheritdoc />
		public override string Runtime { get; set; } = "Overridden runtime default";
	}

	/// <summary>Supplies enum choices for the automatic value editor.</summary>
	public enum OptionMode { First, Second }

	/// <summary>Exposes a callback at the actual JSON serialization boundary.</summary>
	public class SerializationProbe : SettingsObject {
		/// <summary>The callback executed while the provider serializes this value.</summary>
		[JsonIgnore] public Action Serialized { get; set; }

		/// <summary>Runs the fixture callback at the provider serialization boundary.</summary>
		[OnSerializing]
		private void OnSerializing(StreamingContext context) {
			Serialized?.Invoke();
		}
	}

	/// <summary>Supplies an unrelated declared type for persisted type-name rejection checks.</summary>
	public class UnrelatedSettings : SettingsObject;

	/// <summary>Rejects operational construction so Options discovery must use registration metadata.</summary>
	public class UnopenedScreen : ApplicationScreen {
		/// <inheritdoc />
		public override Options ScreenSettings => (Options)base.ScreenSettings;

		/// <summary>Rejects operational construction during metadata-only Options checks.</summary>
		public UnopenedScreen() {
			throw new InvalidOperationException("Options discovery cannot activate an operational screen.");
		}
	}

	/// <summary>Declares Options values by attribute without constructing an operational component.</summary>
	[Settings(typeof(UnopenedAttributeSettingsScreen.Preferences))]
	public class UnopenedAttributeSettingsScreen : ApplicationScreen {
		/// <summary>Metadata-only Options discovery must not construct this component.</summary>
		public UnopenedAttributeSettingsScreen() {
			throw new InvalidOperationException("Options must not activate an unopened screen.");
		}

		#region Inner Types

		/// <summary>The screen's ordinary editable settings value.</summary>
		[UserEditable]
		public class Preferences : SettingsObject {
			/// <summary>A value supplied by normal attribute-based defaults.</summary>
			[DefaultValue(10)]
			public int Number { get; set; }
		}

		#endregion
	}

	/// <summary>Provides a pending validation boundary for modal ownership checks.</summary>
	public class BlockingValidationEditor : ComponentBase, IApplicationSettingsEditor {
		/// <summary>The actual settings value supplied to this fixture editor.</summary>
		[Parameter] public object Value { get; set; }

		/// <summary>The editor callback marking a completed input change.</summary>
		[Parameter] public EventCallback Changed { get; set; }

		/// <summary>The optional asynchronous validation decision.</summary>
		public Task<Result> PendingValidation { get; set; }

		/// <summary>The number of validation calls received by the editor.</summary>
		public int Validations { get; private set; }

		/// <summary>Returns the fixture editor's pending or immediate validation decision.</summary>
		public Task<Result> ValidateAsync(CancellationToken cancellationToken = default) {
			Validations++;
			return PendingValidation?.WaitAsync(cancellationToken) ?? Task.FromResult(Result.Success);
		}
	}

	/// <summary>Owns settings classes that declare their editor through ordinary component metadata.</summary>
	public class DeclaredEditorScreen : ApplicationScreen {
		#region Inner Types

		/// <summary>Declares an editor inherited by the concrete settings class.</summary>
		[Editor(typeof(DeclaredSettingsEditor), typeof(IApplicationSettingsEditor))]
		public class PreferenceDefaults : SettingsObject {
			/// <summary>The settings heading used by default hydration and editor checks.</summary>
			public string Caption { get; set; } = "Declared default";
		}

		/// <summary>Retains the settings class's inherited editor metadata.</summary>
		public sealed class Preferences : PreferenceDefaults {
		}

		#endregion
	}

	/// <summary>Distinguishes the attribute-selected editor from a registration override.</summary>
	public sealed class DeclaredSettingsEditor : CustomEditor {
	}

	/// <summary>Implements the explicit editor contract over the actual supplied settings object.</summary>
	public class CustomEditor : ComponentBase, IApplicationSettingsEditor {
		/// <summary>The actual settings value supplied to this fixture editor.</summary>
		[Parameter] public object Value { get; set; }

		/// <summary>The editor callback marking a completed input change.</summary>
		[Parameter] public EventCallback Changed { get; set; }

		/// <summary>Returns the fixture editor's pending or immediate validation decision.</summary>
		public Task<Result> ValidateAsync(CancellationToken cancellationToken = default) {
			return Task.FromResult(Result.Success);
		}
	}

	/// <summary>Captures actual settings, business drafts and screen deactivation behavior.</summary>
	public class LifecycleScreen : ApplicationScreen {
		/// <inheritdoc />
		public override Options ScreenSettings => (Options)base.ScreenSettings;

		/// <summary>The current actual settings instance applied to this screen.</summary>
		public Options Values { get; private set; }

		/// <summary>An independent unsaved business draft retained across preference changes.</summary>
		public string OperationalDraft { get; set; }

		/// <summary>Whether this screen accepts the next navigation guard.</summary>
		public bool AllowLeave { get; set; } = true;

		/// <summary>The number of accepted deactivation callbacks.</summary>
		public int Deactivations { get; private set; }

		/// <summary>Updates the actual settings value and marks a completed local screen edit.</summary>
		public void Edit(int number) {
			Values.Number = number;
			MarkSettingsChanged();
		}

		/// <summary>Saves the completed local edit through the screen's existing settings lifecycle.</summary>
		public Task SaveChangeAsync() {
			return SaveSettingsChangesAsync();
		}

		/// <inheritdoc />
		protected override Task OnSettingsChangedAsync(SettingsObject values) {
			Values = (Options)values;
			return Task.CompletedTask;
		}

		/// <inheritdoc />
		protected override Task<bool> CanDeactivateAsync(CancellationToken cancellationToken) {
			return Task.FromResult(AllowLeave);
		}

		/// <inheritdoc />
		protected override Task OnDeactivatedAsync(CancellationToken cancellationToken) {
			Deactivations++;
			return Task.CompletedTask;
		}

		/// <inheritdoc />
		protected override void BuildRenderTree(RenderTreeBuilder builder) {
			builder.AddContent(0, Values?.Number);
			builder.AddContent(1, OperationalDraft);
		}
	}

	/// <summary>Owns a real renderer and application for accepted-close and notification checks.</summary>
	private sealed class LifecycleOwner : IAsyncDisposable {
		private readonly ServiceProvider _services;
		private bool _rendererDisposed;

		/// <summary>Owns the configured renderer, services and screen host.</summary>
		private LifecycleOwner(ServiceProvider services, HtmlRenderer renderer) {
			_services = services;
			Renderer = renderer;
			Host = services.GetRequiredService<BlazorUI.IApplicationScreenHost>();
		}

		/// <summary>The renderer owned by this fixture lifetime.</summary>
		public HtmlRenderer Renderer { get; }

		/// <summary>The existing circuit presentation host.</summary>
		public BlazorUI.IApplicationScreenHost Host { get; }

		/// <summary>The first retained logical screen.</summary>
		public ApplicationScreenSession First { get; private set; }

		/// <summary>The second retained logical screen.</summary>
		public ApplicationScreenSession Second { get; private set; }

		/// <summary>The actual registration-owned settings shared by both screen instances.</summary>
		public SettingsObject Settings { get; private set; }

		/// <summary>The first renderer-created screen component.</summary>
		public LifecycleScreen FirstScreen => (LifecycleScreen)First.Screen;

		/// <summary>The second renderer-created screen component.</summary>
		public LifecycleScreen SecondScreen => (LifecycleScreen)Second.Screen;

		/// <summary>Creates the actual application, host and two retained renderer components for lifecycle checks.</summary>
		public static async Task<LifecycleOwner> CreateAsync(ISettingsProvider store) {
			var services = new ServiceCollection().AddLogging().AddSingleton<ISettingsProvider>(store).AddSphere10Blazor();
			services.AddSingleton<IJSRuntime>(new BrowserRuntime(new BrowserState("renderer")));
			services.AddApplicationBlock(block => block
				.WithId("settings")
				.WithName("Settings")
				.AddScreen<LifecycleScreen>(screen => screen
					.WithStableKey("shared-preferences")
					.AsMultiInstance()
					.UseSettings<Options>()
				)
			);
			var provider = services.BuildServiceProvider();
			var renderer = new HtmlRenderer(provider, provider.GetRequiredService<ILoggerFactory>());
			var owner = new LifecycleOwner(provider, renderer);
			try {
				var application = provider.GetRequiredService<BlazorUI.IApplication>();
				await application.InitializeAsync();
				var block = owner.Host.Blocks.Single();
				var definition = block.ScreenDefinitions.Single();
				owner.Settings = definition.Settings;
				await renderer.Dispatcher.InvokeAsync(async () => {
					owner.First = await owner.Host.OpenScreenAsync(block, definition);
					owner.Second = await owner.Host.OpenScreenAsync(block, definition);
					await renderer.RenderComponentAsync<ApplicationScreenHostView>();
				});
				return owner;
			} catch {
				await owner.DisposeAsync();
				throw;
			}
		}

		// Drain settings notifications already queued on this renderer.
		public Task WaitForSettingsAsync() {
			return Renderer.Dispatcher.InvokeAsync(() => Task.CompletedTask);
		}

		/// <summary>Disposes the owned renderer once before releasing its application services.</summary>
		public async Task DisposeRendererAsync() {
			if (_rendererDisposed)
				return;
			_rendererDisposed = true;
			await Renderer.DisposeAsync();
		}

		/// <summary>Releases the fixture-owned resources after pending asynchronous work completes.</summary>
		public async ValueTask DisposeAsync() {
			await DisposeRendererAsync();
			await _services.DisposeAsync();
		}
	}

	// Exercise the existing provider contract that may return a different loaded record.
	private sealed class DistinctRecordProvider : SyncSettingsProviderBase {
		/// <inheritdoc />
		public override bool EncryptSettings => false;

		/// <inheritdoc />
		public override bool ContainsSetting(Type settingsObjectType, object id = null) {
			return true;
		}

		/// <inheritdoc />
		protected override SettingsObject LoadInternal(Type settingsObjectType, object id = null) {
			return new Options { ID = id, Number = 73 };
		}

		/// <inheritdoc />
		protected override void SaveInternal(SettingsObject settings) {
		}

		/// <inheritdoc />
		public override void DeleteSetting(SettingsObject settings) {
		}

		/// <inheritdoc />
		public override void ClearSettings() {
		}
	}

	/// <summary>Retains isolated browser records and acknowledgement/failure controls for the fixture.</summary>
	private sealed class BrowserState(string anonymousProfile) {
		/// <summary>The browser's anonymous owner identity.</summary>
		public string AnonymousProfile { get; } = anonymousProfile;

		/// <summary>The current actual settings instance applied to this screen.</summary>
		public ConcurrentDictionary<string, string> Values { get; } = new();

		/// <summary>The callback executed after browser storage acknowledges a write.</summary>
		public Action AfterWrite { get; init; }

		/// <summary>Whether the fixture rejects a browser write.</summary>
		public bool FailWrite { get; set; }
	}

	/// <summary>Owns a browser-backed settings provider and its authentication services.</summary>
	private sealed class ProviderOwner : IAsyncDisposable {
		private readonly ServiceProvider _services;

		/// <summary>Creates the actual browser provider with isolated storage and authentication services.</summary>
		public ProviderOwner(BrowserState browser, string subject, string applicationKey = "app") {
			Authentication = new TestAuthentication { Subject = subject };
			_services = new ServiceCollection().AddSingleton<IJSRuntime>(new BrowserRuntime(browser))
				.AddSingleton<AuthenticationStateProvider>(Authentication).BuildServiceProvider();
			Provider = new BlazorSettingsProvider(_services, applicationKey);
		}

		/// <summary>The circuit authentication state supplied to the provider.</summary>
		public TestAuthentication Authentication { get; }

		/// <summary>The browser-backed provider owned by this fixture.</summary>
		public BlazorSettingsProvider Provider { get; }

		/// <summary>Releases the fixture-owned resources after pending asynchronous work completes.</summary>
		public async ValueTask DisposeAsync() {
			await Provider.DisposeAsync();
			await _services.DisposeAsync();
		}
	}

	/// <summary>Supplies the circuit's current authenticated subject to provider checks.</summary>
	private sealed class TestAuthentication : AuthenticationStateProvider {
		/// <summary>The circuit's current authenticated user identity.</summary>
		public string Subject { get; set; }

		/// <inheritdoc />
		public override Task<AuthenticationState> GetAuthenticationStateAsync() {
			return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(Subject == null ? new ClaimsIdentity()
				: new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, Subject) }, "test"))));
		}
	}

	/// <summary>Implements only the browser storage calls used by the real provider.</summary>
	private sealed class BrowserRuntime(BrowserState browser) : IJSRuntime, IJSObjectReference {
		/// <summary>Implements the fixture browser interop boundary while retaining its declared cancellation contract.</summary>
		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] arguments) {
			return InvokeAsync<TValue>(identifier, CancellationToken.None, arguments);
		}

		/// <summary>Implements the fixture browser interop boundary while retaining its declared cancellation contract.</summary>
		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, CancellationToken token, object[] arguments) {
			token.ThrowIfCancellationRequested();
			object value = null;
			switch (identifier) {
				case "import":
					value = this;
					break;
				case "GetAnonymousProfile":
					value = browser.AnonymousProfile;
					break;
				case "Read":
					browser.Values.TryGetValue((string)arguments[0], out var stored);
					value = stored;
					break;
				case "Delete":
					browser.Values.TryRemove((string)arguments[0], out _);
					break;
				case "Clear":
					foreach (var key in browser.Values.Keys.Where(key => key.StartsWith((string)arguments[0], StringComparison.Ordinal)))
						browser.Values.TryRemove(key, out _);
					break;
				case "Write":
					if (browser.FailWrite)
						throw new JSException("Browser storage is unavailable.");
					browser.Values[(string)arguments[0]] = (string)arguments[1];
					browser.AfterWrite?.Invoke();
					break;
			}
			return ValueTask.FromResult(value == null ? default : (TValue)value);
		}

		/// <summary>Releases the fixture-owned resources after pending asynchronous work completes.</summary>
		public ValueTask DisposeAsync() {
			return ValueTask.CompletedTask;
		}
	}

	#endregion
}
