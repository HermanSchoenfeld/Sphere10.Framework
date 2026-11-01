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

namespace Sphere10.Framework.Web.AspNetCore.Blazor.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class BlazorApplicationSettingsTests {
	[TestCase(false)]
	[TestCase(true)]
	public async Task UnmarkedObjectsPersistButRequireAnExplicitEditorForOptions(bool customEditor) {
		await using var owner = new StoreOwner(new BrowserState("browser"), null);
		var catalog = new ApplicationSettingsCatalog("app", owner.Store, Array.Empty<IApplicationBlock>(), new[] {
			new ApplicationSettingsDefinition("plain", "Plain values", new ConstructorOptions("Original"),
				settingsEditor: customEditor ? typeof(CustomEditor) : null)
		});
		await catalog.InitializeAsync();
		var profile = catalog.GetProfile("plain");
		Assert.That(profile.HasOptions, Is.True, "Automatic editing policy must not change settings ownership or persistence.");
		var edit = profile.BeginEdit();
		((ConstructorOptions)edit.Draft).Caption = "Accepted unmarked values";
		Assert.That((await edit.ApplyAsync()).IsSuccess, Is.True);

		await using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			BlazorApplicationSettingsDialog dialog = null;
			var rendered = await renderer.RenderComponentAsync<DialogHarness>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(DialogHarness.Catalog)] = catalog,
				[nameof(DialogHarness.Capture)] = (Action<BlazorApplicationSettingsDialog>)(component => dialog = component)
			}));
			var edits = GetField<Dictionary<ApplicationSettingsProfile, ApplicationSettingsEditSession>>(dialog, "_edits");
			Assert.That(edits.ContainsKey(profile), Is.EqualTo(customEditor));
			Assert.That(rendered.ToHtmlString().Contains("have not enabled automatic user editing"), Is.EqualTo(!customEditor));
			dialog.ResetDefaults();
			Assert.That(profile.GetValues<ConstructorOptions>().Caption, Is.EqualTo("Accepted unmarked values"));
		});
	}

	[Test]
	public async Task ConstructorArgumentObjectFactoryWorksWithTheDirectEditorConvenienceOverload() {
		var block = new BlazorApplicationBlockBuilder()
			.WithName("Factory settings")
			.AddScreen<UnopenedScreen>(screen => screen
				.WithStableKey("factory")
				.UseSettings<ConstructorOptions, CustomEditor>(() => new ConstructorOptions("Factory caption")), out var registration
			)
			.Build();
		await using var owner = new StoreOwner(new BrowserState("browser"), null);
		var catalog = new ApplicationSettingsCatalog("app", owner.Store, new[] { block });
		await catalog.InitializeAsync();
		var profile = catalog.GetProfile(block, registration);
		var edit = profile.BeginEdit();
		((ConstructorOptions)edit.Draft).Caption = "Accepted caption";
		Assert.That((await edit.ApplyAsync()).IsSuccess, Is.True);
		edit.Reset();
		Assert.That(((ConstructorOptions)edit.Draft).Caption, Is.EqualTo("Factory caption"));
		Assert.That(profile.GetValues<ConstructorOptions>().Caption, Is.EqualTo("Accepted caption"));
		Assert.That(((BlazorApplicationScreenDefinition)profile.ScreenDefinition).SettingsEditor, Is.EqualTo(typeof(CustomEditor)));
	}

	[Test]
	public async Task OrdinaryPrototypeAndDirectEditorValidateResetAndSurviveCircuitRecreation() {
		var prototype = new RegistrationInspectorSettings { Caption = "Prototype caption" };
		var block = new BlazorApplicationBlockBuilder()
			.WithId("plain-settings")
			.WithName("Plain settings")
			.AddScreen<UnopenedScreen>(screen => screen
				.WithStableKey("plain")
				.UseSettings(prototype)
				.WithSettingsEditor<RegistrationInspectorSettingsEditor>(), out var registration
			)
			.Build();
		var browser = new BrowserState("browser");
		await using var firstOwner = new StoreOwner(browser, null);
		var catalog = new ApplicationSettingsCatalog("app", firstOwner.Store, new[] { block });
		await catalog.InitializeAsync();
		var profile = catalog.GetProfile(block, registration);
		Assert.That(typeof(SettingsObject).IsAssignableFrom(profile.SettingsType), Is.False);
		Assert.That(((BlazorApplicationScreenDefinition)profile.ScreenDefinition).SettingsEditor, Is.EqualTo(typeof(RegistrationInspectorSettingsEditor)));

		var edit = profile.BeginEdit();
		((RegistrationInspectorSettings)edit.Draft).Caption = " ";
		Assert.That((await edit.ApplyAsync()).IsFailure, Is.True);
		Assert.That(browser.Values, Is.Empty, "Ordinary object validation runs before persistence.");
		((RegistrationInspectorSettings)edit.Draft).Caption = "Accepted plain object";
		Assert.That((await edit.ApplyAsync()).IsSuccess, Is.True);
		edit.Reset();
		Assert.That(((RegistrationInspectorSettings)edit.Draft).Caption, Is.EqualTo("Prototype caption"));
		Assert.That(profile.GetValues<RegistrationInspectorSettings>().Caption, Is.EqualTo("Accepted plain object"));
		Assert.That(prototype.Caption, Is.EqualTo("Prototype caption"));
		edit.Cancel();

		await using var restartedOwner = new StoreOwner(browser, null);
		var restarted = new ApplicationSettingsCatalog("app", restartedOwner.Store, new[] { block });
		await restarted.InitializeAsync();
		Assert.That(restarted.GetProfile(block, registration).GetValues<RegistrationInspectorSettings>().Caption, Is.EqualTo("Accepted plain object"));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task StoreSurvivesCircuitRecreationAndKeepsApplicationAndUserNamespacesSeparate(bool authenticated) {
		var browser = new BrowserState("browser-one");
		await using (var first = new StoreOwner(browser, authenticated ? "alice" : null)) {
			await first.Store.SaveAsync("app-one/preferences", Snapshot(25), false, null);
		}
		await using var restarted = new StoreOwner(browser, authenticated ? "alice" : null);
		await using var otherUser = new StoreOwner(browser, authenticated ? "bob" : "signed-in-user");
		await using var otherBrowser = new StoreOwner(new BrowserState("browser-two"), authenticated ? "alice" : null);

		var loaded = await restarted.Store.LoadAsync("app-one/preferences", Defaults(), false, null);
		Assert.That(((Options)loaded.Values).Number, Is.EqualTo(25));
		Assert.That(loaded.SectionVersions["Grid"], Is.EqualTo(2));
		Assert.That(await restarted.Store.LoadAsync("app-two/preferences", Defaults(), false, null), Is.Null);
		Assert.That(await otherUser.Store.LoadAsync("app-one/preferences", Defaults(), false, null), Is.Null);
		Assert.That(await otherBrowser.Store.LoadAsync("app-one/preferences", Defaults(), false, null), Is.Null);
	}

	[Test]
	public async Task ExistingCircuitCannotRetargetItsLiveProfileWhenTheAuthenticatedSubjectChanges() {
		var browser = new BrowserState("browser");
		await using var owner = new StoreOwner(browser, "alice");
		await owner.Store.SaveAsync("app/preferences", Snapshot(10), false, null);
		var original = browser.Values.Single();
		owner.Authentication.Subject = "bob";

		await Assert.ThatAsync(async () => await owner.Store.LoadAsync("app/preferences", Defaults(), false, null), Throws.InvalidOperationException);
		await Assert.ThatAsync(async () => await owner.Store.SaveAsync("app/preferences", Snapshot(20), false, null), Throws.InvalidOperationException);
		Assert.That(browser.Values.Single(), Is.EqualTo(original));
	}

	[Test]
	public async Task StoreHydratesOnlyTheDeclaredTypeAndRejectsMalformedEnvelopes() {
		var browser = new BrowserState("browser");
		await using var owner = new StoreOwner(browser, "alice");
		await owner.Store.SaveAsync("app/preferences", Snapshot(18), false, null);
		var key = browser.Values.Keys.Single();
		var envelope = JObject.Parse(browser.Values[key]);
		((JObject)envelope["values"])["$type"] = typeof(UnrelatedSettings).AssemblyQualifiedName;
		browser.Values[key] = envelope.ToString();

		var loaded = await owner.Store.LoadAsync("app/preferences", Defaults(), false, null);
		Assert.That(loaded.Values.GetType(), Is.EqualTo(typeof(Options)));
		Assert.That(((Options)loaded.Values).Number, Is.EqualTo(18));
		browser.Values[key] = "{ \"values\": [] }";
		await Assert.ThatAsync(async () => await owner.Store.LoadAsync("app/preferences", Defaults(), false, null), Throws.TypeOf<JsonSerializationException>());
	}

	[Test]
	public async Task PartialPayloadKeepsFactoryDefaultsAndExplicitEmptyCollectionsReplaceThem() {
		var browser = new BrowserState("browser");
		await using var owner = new StoreOwner(browser, null);
		var defaults = new PartialOptions {
			Caption = "Registration factory default",
			Nested = new NestedOptions { Text = "Nested factory default" },
			Items = new List<string> { "Factory item" },
			Tags = new[] { "Factory tag" },
			Roles = new Dictionary<string, string> { ["factory"] = "Factory role" }
		};
		await owner.Store.SaveAsync("app/partial", new ApplicationSettingsStoredProfile { Values = defaults }, false, null);
		var key = browser.Values.Keys.Single();
		browser.Values[key] = "{\"values\":{\"Nested\":{},\"Items\":[],\"Tags\":[],\"Roles\":{}},\"sectionVersions\":{\"Grid\":2}}";
		var storedPayload = browser.Values[key];

		var loaded = await owner.Store.LoadAsync("app/partial", defaults, false, null);
		var values = (PartialOptions)loaded.Values;

		Assert.That(values.Caption, Is.EqualTo("Registration factory default"));
		Assert.That(values.Nested.Text, Is.EqualTo("Nested factory default"));
		Assert.That(values.Items, Is.Empty);
		Assert.That(values.Tags, Is.Empty);
		Assert.That(values.Roles, Is.Empty);
		Assert.That(loaded.SectionVersions["Grid"], Is.EqualTo(2));
		Assert.That(browser.Values[key], Is.EqualTo(storedPayload), "Restoring missing defaults must not rewrite the stored record.");
	}

	[Test]
	public async Task ExplicitNullInStoredValuesIsNotReplacedWithFactoryDefaults() {
		var browser = new BrowserState("browser");
		await using var owner = new StoreOwner(browser, null);
		var defaults = new PartialOptions { Caption = "Factory caption" };
		await owner.Store.SaveAsync("app/partial", new ApplicationSettingsStoredProfile { Values = defaults }, false, null);
		var key = browser.Values.Keys.Single();
		browser.Values[key] = "{\"values\":{\"Caption\":null}}";

		var loaded = await owner.Store.LoadAsync("app/partial", defaults, false, null);

		Assert.That(((PartialOptions)loaded.Values).Caption, Is.Null);
	}

	[Test]
	public async Task StoreSerializesItsExclusiveSnapshotOutsideTheOwnersSynchronizationContext() {
		await using var owner = new StoreOwner(new BrowserState("browser"), null);
		var context = new SynchronizationContext();
		SynchronizationContext observed = context;
		var values = new SerializationProbe { Serialized = () => observed = SynchronizationContext.Current };
		var original = SynchronizationContext.Current;
		Task saving;
		SynchronizationContext.SetSynchronizationContext(context);
		try {
			saving = owner.Store.SaveAsync("app/preferences", new ApplicationSettingsStoredProfile { Values = values }, false, null);
		} finally {
			SynchronizationContext.SetSynchronizationContext(original);
		}
		await saving;

		Assert.That(observed, Is.Not.SameAs(context));
	}

	[Test]
	public async Task CommittedBrowserWritePublishesEvenWhenTheCallerCancelsAtAcknowledgement() {
		using var cancellation = new CancellationTokenSource();
		var browser = new BrowserState("browser") { AfterWrite = cancellation.Cancel };
		await using var owner = new StoreOwner(browser, null);
		var profile = await ProfileAsync(owner.Store);
		var edit = profile.BeginEdit();
		((Options)edit.Draft).Number = 44;

		Assert.That((await edit.ApplyAsync(cancellation.Token)).IsSuccess, Is.True);
		Assert.That(profile.GetValues<Options>(out var revision).Number, Is.EqualTo(44));
		Assert.That(revision, Is.EqualTo(1));
	}

	[Test]
	public async Task DirectEditorTypesAndSourceMonikersSurviveIndependentCatalogGraphs() {
		var root = new ApplicationSettingsDefinition("root", "Root", new Options(), settingsEditor: typeof(CustomEditor)).WithStorageId(null);
		var builder = new BlazorApplicationBlockBuilder()
			.WithId("stable-block")
			.WithName("A caption")
			.AddScreen<UnopenedScreen>(screen => screen
				.WithStableKey("preferences")
				.UseSettings<Options>()
				.WithSettingsEditor<CustomEditor>(), out var registration
			);
		var first = builder.Build();
		var second = builder.WithName("A changed caption").Build();
		await using var storeOwner = new StoreOwner(new BrowserState("browser"), null);
		var firstCatalog = new ApplicationSettingsCatalog("app", storeOwner.Store, new[] { first }, new[] { root }, blockKeySelector: block => ((IBlazorApplicationBlock)block).Id);
		var secondCatalog = new ApplicationSettingsCatalog("app", storeOwner.Store, new[] { second }, new[] { root }, blockKeySelector: block => ((IBlazorApplicationBlock)block).Id);
		await firstCatalog.InitializeAsync();
		await secondCatalog.InitializeAsync();

		var profile = firstCatalog.GetProfile(first, registration);
		Assert.That(((BlazorApplicationScreenDefinition)profile.ScreenDefinition).SettingsEditor, Is.EqualTo(typeof(CustomEditor)));
		Assert.That(firstCatalog.GetProfile("root").ApplicationDefinition.SettingsEditor, Is.EqualTo(typeof(CustomEditor)));
		Assert.That(firstCatalog.GetProfile("root").HasExplicitStorageId, Is.True);
		Assert.That(firstCatalog.GetProfile("root").StorageId, Is.Null);
		Assert.That(secondCatalog.GetProfile(second, registration).ProfileKey, Is.EqualTo(profile.ProfileKey));
		Assert.That(secondCatalog.GetProfile(second, registration).ScreenDefinition, Is.Not.SameAs(profile.ScreenDefinition));
		Assert.That(profile.ScreenDefinition.Activators, Is.Empty);
	}

	[Test]
	public async Task DefaultEditorRendersReadableNestedValuesAndSchemaColumnsWithoutProviderOrMembershipControls() {
		await using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
		var values = new Options { ID = "private-storage-id" };
		await renderer.Dispatcher.InvokeAsync(async () => {
			var rendered = await renderer.RenderComponentAsync<BlazorApplicationSettingsValueEditor>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(BlazorApplicationSettingsValueEditor.Value)] = values
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

	[Test]
	public async Task DictionaryGridRolesRenderTheirReadableSchemaAndEditableWidths() {
		await using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			var rendered = await renderer.RenderComponentAsync<BlazorApplicationSettingsValueEditor>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(BlazorApplicationSettingsValueEditor.Value)] = new DictionaryOptions()
			}));
			var html = rendered.ToHtmlString();
			Assert.That(html, Does.Contain("processes"));
			Assert.That(html, Does.Contain("Process name"));
			Assert.That(html, Does.Contain("Manual width"));
		});
	}

	[Test]
	public async Task TesterCustomEditorPreviewsTheDetachedCaptionRatherThanItsModelTypeName() {
		await using var owner = new StoreOwner(new BrowserState("browser"), null);
		var catalog = new ApplicationSettingsCatalog("app", owner.Store, Array.Empty<IApplicationBlock>(),
			new[] { new ApplicationSettingsDefinition("gpu", "GPU", new RegistrationInspectorSettings { Caption = "GPU summary" }, settingsEditor: typeof(RegistrationInspectorSettingsEditor)) });
		await catalog.InitializeAsync();
		await using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			var rendered = await renderer.RenderComponentAsync<RegistrationInspectorSettingsEditor>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(RegistrationInspectorSettingsEditor.Value)] = catalog.GetProfile("gpu").BeginEdit().Draft
			}));
			Assert.That(rendered.ToHtmlString(), Does.Contain(">GPU summary</output>"));
		});
	}

	[Test]
	public async Task InvalidCollectionItemTextRemainsAnErrorWhenAnotherItemIsEditedSuccessfully() {
		await using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			BlazorApplicationSettingsValueEditor editor = null;
			var errors = new Dictionary<string, string>();
			var values = new Options();
			await renderer.RenderComponentAsync<ValueHarness>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(ValueHarness.Value)] = values,
				[nameof(ValueHarness.Errors)] = errors,
				[nameof(ValueHarness.Capture)] = (Action<BlazorApplicationSettingsValueEditor>)(component => editor = component)
			}));
			var property = TypeDescriptor.GetProperties(values)[nameof(Options.Numbers)];
			await InvokeAsync(editor, "ChangeItemAsync", property, 0, "not-a-number");
			await InvokeAsync(editor, "ChangeItemAsync", property, 1, "30");

			Assert.That(errors, Has.Count.EqualTo(1), "Correcting a different collection item cannot discard invalid pending input.");
			Assert.That(values.Numbers, Is.EqualTo(new[] { 10, 30 }));
		});
	}

	[Test]
	public async Task ValueEditorUsesDeclaredScalarTypesAndNestedDraftEditsCancelWithoutPublishing() {
		await using var owner = new StoreOwner(new BrowserState("browser"), null);
		var profile = await ProfileAsync(owner.Store);
		var session = profile.BeginEdit();
		var draft = (Options)session.Draft;
		await using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			BlazorApplicationSettingsValueEditor editor = null;
			await renderer.RenderComponentAsync<ValueHarness>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(ValueHarness.Value)] = draft,
				[nameof(ValueHarness.Capture)] = (Action<BlazorApplicationSettingsValueEditor>)(component => editor = component)
			}));
			await InvokeAsync(editor, "ChangeValueAsync", TypeDescriptor.GetProperties(draft)[nameof(Options.Number)], "21");
			await InvokeAsync(editor, "ChangeValueAsync", TypeDescriptor.GetProperties(draft)[nameof(Options.Mode)], "Second");
			await InvokeAsync(editor, "AddItemAsync", TypeDescriptor.GetProperties(draft)[nameof(Options.Words)]);
			draft.Nested.Text = "provisional nested edit";
			Assert.That(draft.Number, Is.EqualTo(21));
			Assert.That(draft.Mode, Is.EqualTo(OptionMode.Second));
			Assert.That(draft.Words, Has.Length.EqualTo(2));
			Assert.That(profile.GetValues<Options>().Number, Is.EqualTo(10));
			session.Cancel();
			Assert.That(((Options)session.Draft).Nested.Text, Is.EqualTo("default"));
			Assert.That(((Options)session.Draft).Words, Is.EqualTo(new[] { "first" }));
		});
	}

	[TestCase(false, false)]
	[TestCase(false, true)]
	[TestCase(true, false)]
	[TestCase(true, true)]
	public async Task BlockedCustomValidationKeepsItsPageAndRejectsOtherDialogActions(bool selecting, bool valid) {
		var browser = new BrowserState("browser");
		await using var owner = new StoreOwner(browser, null);
		var catalog = new ApplicationSettingsCatalog("app", owner.Store, Array.Empty<IApplicationBlock>(), new[] {
			new ApplicationSettingsDefinition("first", "First", new Options(), settingsEditor: typeof(BlockingValidationEditor)),
			new ApplicationSettingsDefinition("second", "Second", Defaults())
		});
		await catalog.InitializeAsync();
		var first = catalog.GetProfile("first");
		var second = catalog.GetProfile("second");
		await using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
		var validation = new TaskCompletionSource<Result>(TaskCreationOptions.RunContinuationsAsynchronously);
		using var release = Tools.Scope.ExecuteOnDispose(() => validation.TrySetResult(Result.Success));
		await renderer.Dispatcher.InvokeAsync(async () => {
			BlazorApplicationSettingsDialog dialog = null;
			await renderer.RenderComponentAsync<DialogHarness>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(DialogHarness.Catalog)] = catalog,
				[nameof(DialogHarness.Capture)] = (Action<BlazorApplicationSettingsDialog>)(component => dialog = component)
			}));
			var edits = GetField<Dictionary<ApplicationSettingsProfile, ApplicationSettingsEditSession>>(dialog, "_edits");
			dialog.ResetDefaults();
			((Options)edits[first].Draft).Number = 31;
			await EventCallback.Factory.Create(dialog, () => InvokeAsync(dialog, "SelectPageAsync", second)).InvokeAsync();
			dialog.ResetDefaults();
			((Options)edits[second].Draft).Number = 41;
			await EventCallback.Factory.Create(dialog, () => InvokeAsync(dialog, "SelectPageAsync", first)).InvokeAsync();
			var draft = edits[first].Draft;
			var editor = (BlockingValidationEditor)GetField<DynamicComponent>(dialog, "_editor").Instance;
			var previousValidations = editor.Validations;
			editor.PendingValidation = validation.Task;

			// A real asynchronous custom editor holds the selected page while other UI events are admitted.
			var pending = selecting
				? EventCallback.Factory.Create(dialog, () => InvokeAsync(dialog, "SelectPageAsync", second)).InvokeAsync()
				: EventCallback.Factory.Create(dialog, dialog.ApplyAsync).InvokeAsync();
			Assert.That(pending.IsCompleted, Is.False);
			Assert.That(GetField<bool>(dialog, "_saving"), Is.True);
			await dialog.ApplyAsync();
			await dialog.AcceptAsync();
			await InvokeAsync(dialog, "SelectPageAsync", second);
			dialog.ResetDefaults();
			dialog.CancelEditing();
			Assert.That(await dialog.RequestCloseAsync(), Is.False);
			Assert.That(GetField<ApplicationSettingsProfile>(dialog, "_selected"), Is.SameAs(first));
			Assert.That(edits[first].Draft, Is.SameAs(draft));
			Assert.That(((Options)draft).Number, Is.EqualTo(31));
			Assert.That(((Options)edits[second].Draft).Number, Is.EqualTo(41));
			Assert.That(editor.Validations, Is.EqualTo(previousValidations + 1));
			Assert.That(browser.Values, Is.Empty);
			Assert.That(dialog.ShowAsync().IsCompleted, Is.False);

			validation.SetResult(valid ? Result.Success : Result.Error("Correct the selected page."));
			await pending;
			Assert.That(GetField<bool>(dialog, "_saving"), Is.False, "Both successful and rejected validation release the dialog admission guard.");
			Assert.That(GetField<ApplicationSettingsProfile>(dialog, "_selected"), Is.SameAs(selecting && valid ? second : first));
			Assert.That(first.GetValues<Options>().Number, Is.EqualTo(!selecting && valid ? 31 : 10));
			Assert.That(second.GetValues<Options>().Number, Is.EqualTo(10));
			if (!valid)
				Assert.That(GetField<string[]>(dialog, "_errors"), Does.Contain("Correct the selected page."));

			// A rejected validation keeps its draft editable, and each later Apply still accepts only its own page.
			editor.PendingValidation = null;
			await EventCallback.Factory.Create(dialog, () => InvokeAsync(dialog, "SelectPageAsync", first)).InvokeAsync();
			await dialog.ApplyAsync();
			await EventCallback.Factory.Create(dialog, () => InvokeAsync(dialog, "SelectPageAsync", second)).InvokeAsync();
			await dialog.ApplyAsync();
			Assert.That(first.GetValues<Options>().Number, Is.EqualTo(31));
			Assert.That(second.GetValues<Options>().Number, Is.EqualTo(41));
		});
	}

	[Test]
	public async Task DialogValidationAndFailedApplyRetainTheDetachedDraftAndDoNotClose() {
		var browser = new BrowserState("browser");
		await using var owner = new StoreOwner(browser, null);
		var catalog = Catalog(owner.Store);
		await catalog.InitializeAsync();
		var profile = catalog.GetProfile("preferences");
		await using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			BlazorApplicationSettingsDialog dialog = null;
			var rendered = await renderer.RenderComponentAsync<DialogHarness>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(DialogHarness.Catalog)] = catalog,
				[nameof(DialogHarness.Capture)] = (Action<BlazorApplicationSettingsDialog>)(component => dialog = component)
			}));
			Assert.That(Regex.Match(rendered.ToHtmlString(), "<fieldset[^>]*sphere10-options-fieldset[^>]*>").Value, Does.Not.Contain("disabled"),
				"An idle Options page must leave its navigation and value controls enabled.");
			dialog.ResetDefaults();
			var session = GetField<Dictionary<ApplicationSettingsProfile, ApplicationSettingsEditSession>>(dialog, "_edits")[profile];
			((Options)session.Draft).Number = -1;
			await dialog.AcceptAsync();
			Assert.That(dialog.ShowAsync().IsCompleted, Is.False);
			Assert.That(GetField<string[]>(dialog, "_errors"), Is.Not.Empty);
			Assert.That(browser.Values, Is.Empty);
			((Options)session.Draft).Number = 23;
			browser.FailWrite = true;
			var draft = session.Draft;
			await dialog.ApplyAsync();
			Assert.That(dialog.ShowAsync().IsCompleted, Is.False);
			Assert.That(session.Draft, Is.SameAs(draft));
			Assert.That(((Options)session.Draft).Number, Is.EqualTo(23));
			Assert.That(profile.Revision, Is.Zero);
			Assert.That(GetField<string[]>(dialog, "_errors").Single(), Does.Contain("try again"));
			browser.FailWrite = false;
			await dialog.ApplyAsync();
			Assert.That(profile.GetValues<Options>().Number, Is.EqualTo(23));
			((Options)session.Draft).Nested.Text = "cancel since Apply";
			dialog.CancelEditing();
			Assert.That((await dialog.ShowAsync()).ResultType, Is.EqualTo(ModalResult.Cancel.ResultType));
			Assert.That(profile.GetValues<Options>().Nested.Text, Is.EqualTo("default"));
		});
	}

	[Test]
	public async Task DialogOkAcceptsOnlyTheSelectedPageAndRetainsOtherUnappliedDrafts() {
		await using var owner = new StoreOwner(new BrowserState("browser"), null);
		var catalog = new ApplicationSettingsCatalog("app", owner.Store, Array.Empty<IApplicationBlock>(), new[] {
			new ApplicationSettingsDefinition("first", "First", Defaults()),
			new ApplicationSettingsDefinition("second", "Second", Defaults())
		});
		await catalog.InitializeAsync();
		var first = catalog.GetProfile("first");
		var second = catalog.GetProfile("second");
		await using var services = new ServiceCollection().AddLogging().BuildServiceProvider();
		await using var renderer = new HtmlRenderer(services, services.GetRequiredService<ILoggerFactory>());
		await renderer.Dispatcher.InvokeAsync(async () => {
			BlazorApplicationSettingsDialog dialog = null;
			await renderer.RenderComponentAsync<DialogHarness>(ParameterView.FromDictionary(new Dictionary<string, object> {
				[nameof(DialogHarness.Catalog)] = catalog,
				[nameof(DialogHarness.Capture)] = (Action<BlazorApplicationSettingsDialog>)(component => dialog = component)
			}));
			dialog.ResetDefaults();
			var edits = GetField<Dictionary<ApplicationSettingsProfile, ApplicationSettingsEditSession>>(dialog, "_edits");
			((Options)edits[first].Draft).Number = 31;
			await InvokeAsync(dialog, "SelectPageAsync", second);
			dialog.ResetDefaults();
			((Options)edits[second].Draft).Number = 41;

			await dialog.AcceptAsync();

			Assert.That(first.GetValues<Options>().Number, Is.EqualTo(10));
			Assert.That(second.GetValues<Options>().Number, Is.EqualTo(41));
			Assert.That(((Options)edits[first].Draft).Number, Is.EqualTo(31));
			Assert.That(dialog.ShowAsync().IsCompleted, Is.False);
			Assert.That(GetField<string[]>(dialog, "_errors").Single(), Does.Contain("Other pages"));
			await InvokeAsync(dialog, "SelectPageAsync", first);
			await dialog.ApplyAsync();
			await dialog.AcceptAsync();
			Assert.That(first.GetValues<Options>().Number, Is.EqualTo(31));
			Assert.That((await dialog.ShowAsync()).ResultType, Is.EqualTo(ModalResult.Ok.ResultType));
		});
	}

	[Test]
	public async Task ApplicationInstallsOneScreenOptionsCommandIntoTheExistingHeadingWithoutRunningOperations() {
		var browser = new BrowserState("browser");
		var runtime = new BrowserRuntime(browser);
		var calls = 0;
		var services = new ServiceCollection().AddLogging().AddSingleton<IJSRuntime>(runtime);
		new BlazorApplicationBuilder(services)
			.WithSettingsKey("stable-app")
			.AddSettings<Options>("preferences", "Preferences")
			.OnApplicationCreated(application => application.SetMenus(new[] {
				new BlazorApplicationMenu { Id = "existing-options", Text = "&Options", Items = new[] {
					new BlazorActionMenuItem { Id = "operation", Title = "Existing operation", Action = () => calls++ }
				} }
			}))
			.Build();
		await using var provider = services.BuildServiceProvider();
		var application = provider.GetRequiredService<IBlazorApplication>();

		await application.InitializeAsync();
		await application.InitializeAsync();

		Assert.That(application.Menus, Has.Length.EqualTo(1));
		Assert.That(application.Menus.Single().Id, Is.EqualTo("existing-options"));
		Assert.That(application.Menus.Single().Items.Select(item => item.Id), Is.EqualTo(new[] { "operation", "sphere10-screen-options" }));
		Assert.That(calls, Is.Zero);
		((BlazorActionMenuItem)application.Menus.Single().Items.First()).Action();
		Assert.That(calls, Is.EqualTo(1));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task LateSectionInitializationRebasesBothInstancesWithoutAcceptingOrDiscardingDirtyValues(bool failedSave) {
		var store = new LifecycleStore { FailWrite = failedSave };
		await using var owner = await LifecycleOwner.CreateAsync(store);
		await owner.Renderer.Dispatcher.InvokeAsync(async () => {
			owner.FirstScreen.OperationalDraft = "Retained business draft";
			owner.FirstScreen.Edit(41);
			if (failedSave)
				await owner.FirstScreen.SaveChangeAsync();

			// Occupy the renderer while both worker notifications queue; each frozen delta must survive the newer global revision.
			Task.Run(() => {
				Assert.That(owner.Profile.TryInitializeSection("Grid", 1, values => ((Options)values).Grid.PageSize = 29,
					(source, target) => ((Options)target).Grid = ((Options)source).Grid), Is.True);
				Assert.That(owner.Profile.TryInitializeSection("Nested", 1, values => ((Options)values).Nested.Text = "Imported section",
					(source, target) => ((Options)target).Nested = ((Options)source).Nested), Is.True);
			}).GetAwaiter().GetResult();
		});
		await owner.WaitForSettingsAsync();

		Assert.That(owner.FirstScreen.Values.Number, Is.EqualTo(41));
		Assert.That(owner.SecondScreen.Values.Number, Is.EqualTo(10));
		Assert.That(owner.FirstScreen.Values.Grid.PageSize, Is.EqualTo(29));
		Assert.That(owner.SecondScreen.Values.Grid.PageSize, Is.EqualTo(29));
		Assert.That(owner.FirstScreen.Values.Nested.Text, Is.EqualTo("Imported section"));
		Assert.That(owner.SecondScreen.Values.Nested.Text, Is.EqualTo("Imported section"));
		Assert.That(owner.FirstScreen.OperationalDraft, Is.EqualTo("Retained business draft"));
		Assert.That(owner.Profile.GetValues<Options>().Number, Is.EqualTo(10));
		Assert.That(store.SavedNumbers, Is.Empty, "Initialization does not persist working edits or create a restore-time write.");
		if (failedSave)
			Assert.That(owner.FirstScreen.SettingsError, Does.Contain("Write failed"));

		store.FailWrite = false;
		await owner.Renderer.Dispatcher.InvokeAsync(() => owner.FirstScreen.SaveSettingsAsync());
		await owner.WaitForSettingsAsync();

		Assert.That(owner.Profile.GetValues<Options>().Number, Is.EqualTo(41));
		Assert.That(owner.Profile.GetValues<Options>().Grid.PageSize, Is.EqualTo(29));
		Assert.That(owner.Profile.GetValues<Options>().Nested.Text, Is.EqualTo("Imported section"));
		Assert.That(store.SavedNumbers, Is.EqualTo(new[] { 41 }));
		Assert.That(owner.FirstScreen.SettingsError, Is.Null);
	}

	[Test]
	public async Task RendererOwnWriteAcknowledgementsPreserveNewerLocalValuesAndForeignApplyUpdatesBothInstances() {
		var firstEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var secondEntered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var releaseFirst = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var releaseSecond = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var store = new LifecycleStore {
			BeforeSave = async values => {
				if (values.Number == 11) {
					firstEntered.TrySetResult();
					await releaseFirst.Task;
				} else if (values.Number == 12) {
					secondEntered.TrySetResult();
					await releaseSecond.Task;
				}
			}
		};
		await using var owner = await LifecycleOwner.CreateAsync(store);
		using var release = Tools.Scope.ExecuteOnDispose(() => {
			releaseFirst.TrySetResult();
			releaseSecond.TrySetResult();
		});
		var first = owner.FirstScreen;
		var second = owner.SecondScreen;
		Task firstSave = null;
		Task secondSave = null;
		await owner.Renderer.Dispatcher.InvokeAsync(() => {
			first.OperationalDraft = "first row draft";
			second.OperationalDraft = "second row draft";
			first.Edit(11);
			firstSave = first.SaveChangeAsync();
		});
		await firstEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
		await owner.Renderer.Dispatcher.InvokeAsync(() => {
			first.Edit(12);
			secondSave = first.SaveChangeAsync();
		});
		releaseFirst.TrySetResult();
		await firstSave;
		await secondEntered.Task.WaitAsync(TimeSpan.FromSeconds(5));
		await owner.Renderer.Dispatcher.InvokeAsync(() => {
			Assert.That(first.Values.Number, Is.EqualTo(12), "An older own save acknowledgement cannot replace a newer local model.");
			Assert.That(first.Applications, Is.EqualTo(1), "Own acknowledgements do not rerun preference application.");
		});
		var edit = owner.Profile.BeginEdit();
		((Options)edit.Draft).Number = 21;
		var applying = edit.ApplyAsync();
		releaseSecond.TrySetResult();
		await Task.WhenAll(secondSave, applying);
		await owner.WaitForSettingsAsync();
		await owner.Renderer.Dispatcher.InvokeAsync(() => {
			Assert.That(first.Values.Number, Is.EqualTo(21));
			Assert.That(second.Values.Number, Is.EqualTo(21));
			Assert.That(first.OperationalDraft, Is.EqualTo("first row draft"));
			Assert.That(second.OperationalDraft, Is.EqualTo("second row draft"));
		});
		Assert.That(store.SavedNumbers, Is.EqualTo(new[] { 11, 12, 21 }));
		await owner.Renderer.Dispatcher.InvokeAsync(() => owner.Host.CloseScreenAsync(owner.First));
		Assert.That(store.SavedNumbers, Is.EqualTo(new[] { 11, 12, 21 }), "An unchanged instance cannot overwrite the accepted Options value at close.");
		await owner.Renderer.Dispatcher.InvokeAsync(() => second.Edit(99));
		await owner.DisposeRendererAsync();
		Assert.That(store.SavedNumbers, Is.EqualTo(new[] { 11, 12, 21 }), "Renderer disposal is not an accepted-save boundary.");
	}

	[Test]
	public async Task RendererVetoAndHideDoNotSaveWhileFailedAcceptedCloseKeepsOwnershipForRetry() {
		var store = new LifecycleStore();
		await using var owner = await LifecycleOwner.CreateAsync(store);
		var screen = owner.FirstScreen;
		await owner.Renderer.Dispatcher.InvokeAsync(async () => {
			await owner.Host.ShowScreenAsync(owner.First);
			screen.Edit(15);
			await owner.Host.ShowScreenAsync(owner.Second);
			Assert.That(screen.Deactivations, Is.EqualTo(1));
			Assert.That(store.SavedNumbers, Is.Empty, "Hiding a retained component does not save preferences.");
			await owner.Host.ShowScreenAsync(owner.First);
			screen.AllowLeave = false;
			Assert.That(await owner.Host.CloseScreenAsync(owner.First), Is.False);
			Assert.That(store.SavedNumbers, Is.Empty);
			screen.AllowLeave = true;
			store.FailWrite = true;
			var deactivations = screen.Deactivations;
			await Assert.ThatAsync(async () => await owner.Host.CloseScreenAsync(owner.First), Throws.InvalidOperationException);
			Assert.That(owner.Host.ActiveScreen, Is.SameAs(owner.First));
			Assert.That(owner.First.Screen, Is.SameAs(screen));
			Assert.That(screen.Host, Is.SameAs(owner.Host));
			Assert.That(screen.Deactivations, Is.EqualTo(deactivations));
			Assert.That(screen.SettingsError, Does.Contain("Write failed"));
			store.FailWrite = false;
			Assert.That(await owner.Host.CloseScreenAsync(owner.First), Is.True);
		});
		Assert.That(store.SavedNumbers, Is.EqualTo(new[] { 15 }));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task AcceptedSaveWaitsForPendingSectionApplicationBeforeCapturingItsNewBaseline(bool closeScreen) {
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var store = new LifecycleStore();
		await using var owner = await LifecycleOwner.CreateAsync(store);
		using var releaseScope = Tools.Scope.ExecuteOnDispose(() => release.TrySetResult());
		var screen = owner.FirstScreen;
		await owner.Renderer.Dispatcher.InvokeAsync(() => {
			screen.CaptureDetached = true;
			screen.OperationalDraft = "Retained operation";
			screen.Edit(41);
			screen.BeforeApply = async values => {
				if (values.Grid.PageSize != 29)
					return;
				entered.TrySetResult();
				await release.Task;
			};
			Assert.That(owner.Profile.TryInitializeSection("Grid", 1, values => ((Options)values).Grid.PageSize = 29,
				(source, target) => ((Options)target).Grid = ((Options)source).Grid), Is.True);
		});
		await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));

		Task saving = null;
		await owner.Renderer.Dispatcher.InvokeAsync(() => {
			saving = closeScreen ? owner.Host.CloseScreenAsync(owner.First) : screen.SaveSettingsAsync();
		});
		Assert.That(saving.IsCompleted, Is.False, "Accepted close and direct saves must await the pending model application.");
		Assert.That(store.SavedNumbers, Is.Empty);

		release.TrySetResult();
		await saving.WaitAsync(TimeSpan.FromSeconds(5));
		if (!closeScreen)
			await owner.WaitForSettingsAsync();
		Assert.Multiple(() => {
			Assert.That(owner.Profile.GetValues<Options>().Number, Is.EqualTo(41));
			Assert.That(owner.Profile.GetValues<Options>().Grid.PageSize, Is.EqualTo(29));
			Assert.That(store.SavedNumbers, Is.EqualTo(new[] { 41 }));
			Assert.That(screen.OperationalDraft, Is.EqualTo("Retained operation"));
		});
	}

	[Test]
	public async Task AcceptedCloseRetriesACaptureSupersededByConcurrentSectionInitialization() {
		var store = new LifecycleStore();
		await using var owner = await LifecycleOwner.CreateAsync(store);
		var screen = owner.FirstScreen;
		await owner.Renderer.Dispatcher.InvokeAsync(async () => {
			screen.CaptureDetached = true;
			screen.Edit(41);
			screen.AfterCapture = () => {
				screen.AfterCapture = null;
				// Complete the worker's import while this renderer still holds the old detached capture.
				Task.Run(() => Assert.That(owner.Profile.TryInitializeSection("Grid", 1, values => ((Options)values).Grid.PageSize = 29,
					(source, target) => ((Options)target).Grid = ((Options)source).Grid), Is.True)).GetAwaiter().GetResult();
			};
			Assert.That(await owner.Host.CloseScreenAsync(owner.First), Is.True);
		});

		Assert.Multiple(() => {
			Assert.That(owner.Profile.GetValues<Options>().Number, Is.EqualTo(41));
			Assert.That(owner.Profile.GetValues<Options>().Grid.PageSize, Is.EqualTo(29));
			Assert.That(store.SavedNumbers, Is.EqualTo(new[] { 41 }), "A rejected capture must not let accepted close discard the remaining local edit.");
		});
	}

	[Test]
	public async Task ReentrantSectionInitializationWaitsForTheActivePreferenceHookBeforeRebasing() {
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		await using var owner = await LifecycleOwner.CreateAsync(new LifecycleStore());
		using var releaseScope = Tools.Scope.ExecuteOnDispose(() => release.TrySetResult());
		var screen = owner.FirstScreen;
		var initialized = false;
		await owner.Renderer.Dispatcher.InvokeAsync(() => {
			screen.OperationalDraft = "Retained operation";
			screen.BeforeApply = async values => {
				if (values.Number != 21 || initialized)
					return;
				initialized = true;
				Assert.That(owner.Profile.TryInitializeSection("Grid", 1, root => ((Options)root).Grid.PageSize = 29,
					(source, target) => ((Options)target).Grid = ((Options)source).Grid), Is.True);
				entered.TrySetResult();
				await release.Task;
			};

			// Complete acceptance while the renderer is occupied, so the hook starts after the store's queue has drained.
			var values = owner.Profile.GetValues<Options>(out var revision);
			values.Number = 21;
			Task.Run(async () => await owner.Profile.SaveCaptureAsync(owner.Profile.Capture(values, new object(), revision, 1))).GetAwaiter().GetResult();
		});
		await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Task updates = null;
		await owner.Renderer.Dispatcher.InvokeAsync(() => {
			updates = (Task)typeof(BlazorApplicationScreen).GetField("_settingsUpdates", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(screen);
		});
		release.TrySetResult();
		await updates.WaitAsync(TimeSpan.FromSeconds(5));
		var livePageSize = 0;
		await owner.Renderer.Dispatcher.InvokeAsync(async () => {
			livePageSize = screen.Values.Grid.PageSize;
			screen.Edit(22);
			await screen.SaveChangeAsync();
		});

		Assert.Multiple(() => {
			Assert.That(livePageSize, Is.EqualTo(29), "A nested initialization must rebase the model after the outer hook assigns it.");
			Assert.That(owner.Profile.GetValues<Options>().Grid.PageSize, Is.EqualTo(29), "The next accepted capture must keep the imported section.");
			Assert.That(owner.Profile.GetValues<Options>().Number, Is.EqualTo(22));
			Assert.That(screen.OperationalDraft, Is.EqualTo("Retained operation"));
		});
	}

	[Test]
	public async Task RendererSerializesSlowPreferenceApplicationBeforeNewerAcceptedValues() {
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var applied = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		await using var owner = await LifecycleOwner.CreateAsync(new LifecycleStore());
		using var releaseScope = Tools.Scope.ExecuteOnDispose(() => release.TrySetResult());
		var screen = owner.FirstScreen;
		var started = new List<int>();
		await owner.Renderer.Dispatcher.InvokeAsync(() => {
			screen.OperationalDraft = "operation in progress";
			screen.BeforeApply = async values => {
				started.Add(values.Number);
				if (values.Number == 21) {
					entered.TrySetResult();
					await release.Task;
				}
			};
			screen.AfterApply = values => {
				if (values.Number == 22)
					applied.TrySetResult();
			};
		});
		var first = owner.Profile.BeginEdit();
		((Options)first.Draft).Number = 21;
		await first.ApplyAsync();
		await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
		var second = owner.Profile.BeginEdit();
		((Options)second.Draft).Number = 22;
		await second.ApplyAsync();
		await owner.Renderer.Dispatcher.InvokeAsync(() => Assert.That(started, Is.EqualTo(new[] { 21 })));
		release.TrySetResult();
		await applied.Task.WaitAsync(TimeSpan.FromSeconds(5));
		await owner.Renderer.Dispatcher.InvokeAsync(() => {
			Assert.That(started, Is.EqualTo(new[] { 21, 22 }));
			Assert.That(screen.Values.Number, Is.EqualTo(22));
			Assert.That(screen.OperationalDraft, Is.EqualTo("operation in progress"));
		});
	}

	// Store calls are real adapter boundaries; only browser interop and principal discovery are substituted.
	private static Options Defaults() {
		return new Options();
	}

	private static ApplicationSettingsStoredProfile Snapshot(int number) {
		return new ApplicationSettingsStoredProfile { Values = new Options { Number = number }, SectionVersions = new Dictionary<string, int> { ["Grid"] = 2 } };
	}

	private static ApplicationSettingsCatalog Catalog(IApplicationSettingsStore store) {
		return new ApplicationSettingsCatalog("app", store, Array.Empty<IApplicationBlock>(),
			new[] { new ApplicationSettingsDefinition("preferences", "Preferences", Defaults()) });
	}

	private static async Task<ApplicationSettingsProfile> ProfileAsync(IApplicationSettingsStore store) {
		var catalog = Catalog(store);
		await catalog.InitializeAsync();
		return catalog.GetProfile("preferences");
	}

	// HtmlRenderer owns components; reflection invokes the same private event handlers used by their rendered controls.
	private static Task InvokeAsync(object component, string method, params object[] arguments) {
		return (Task)component.GetType().GetMethod(method, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(component, arguments);
	}

	private static T GetField<T>(object component, string field) {
		return (T)component.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(component);
	}

	public sealed class ValueHarness : ComponentBase {
		[Parameter] public object Value { get; set; }
		[Parameter] public IDictionary<string, string> Errors { get; set; } = new Dictionary<string, string>();
		[Parameter] public Action<BlazorApplicationSettingsValueEditor> Capture { get; set; }

		protected override void BuildRenderTree(RenderTreeBuilder builder) {
			builder.OpenComponent<BlazorApplicationSettingsValueEditor>(0);
			builder.AddAttribute(1, nameof(BlazorApplicationSettingsValueEditor.Value), Value);
			builder.AddAttribute(2, nameof(BlazorApplicationSettingsValueEditor.Errors), Errors);
			builder.AddComponentReferenceCapture(3, value => Capture((BlazorApplicationSettingsValueEditor)value));
			builder.CloseComponent();
		}
	}

	public sealed class DialogHarness : ComponentBase {
		[Parameter] public ApplicationSettingsCatalog Catalog { get; set; }
		[Parameter] public Action<BlazorApplicationSettingsDialog> Capture { get; set; }

		protected override void BuildRenderTree(RenderTreeBuilder builder) {
			builder.OpenComponent<BlazorApplicationSettingsDialog>(0);
			builder.AddAttribute(1, nameof(BlazorApplicationSettingsDialog.Catalog), Catalog);
			builder.AddAttribute(2, nameof(BlazorApplicationSettingsDialog.Options), new ModalOptions());
			builder.AddComponentReferenceCapture(3, value => Capture((BlazorApplicationSettingsDialog)value));
			builder.CloseComponent();
		}
	}

	[UserEditable]
	public class Options : SettingsObject {
		[DisplayName("Refresh count")]
		public int Number { get; set; } = 10;

		[ReadOnly(true)]
		[DisplayName("Read-only preference")]
		public string ReadOnlyValue { get; set; } = "read only";

		public OptionMode Mode { get; set; }
		public NestedOptions Nested { get; set; } = new();
		public int[] Numbers { get; set; } = { 10, 20 };
		public string[] Words { get; set; } = { "first" };
		public BlazorGridSettings Grid { get; set; } = new() {
			Columns = new[] { new ApplicationGridColumnPreferences { Key = "name", Title = "Name column", Width = 150 } }
		};

		public override Result Validate() {
			return Number > 0 ? Result.Success : Result.Error("Refresh count must be positive.");
		}
	}

	public class ConstructorOptions(string caption) {
		public string Caption { get; set; } = caption;
	}

	public class PartialOptions : SettingsObject {
		public string Caption { get; set; } = "Constructor caption";
		public NestedOptions Nested { get; set; } = new();
		public List<string> Items { get; set; } = new() { "Constructor item" };
		public string[] Tags { get; set; } = { "Constructor tag" };
		public Dictionary<string, string> Roles { get; set; } = new() { ["constructor"] = "Constructor role" };
	}

	public class NestedOptions {
		[DisplayName("Nested preference")]
		public string Text { get; set; } = "default";
	}

	[UserEditable]
	public class DictionaryOptions : SettingsObject {
		public Dictionary<string, BlazorGridSettings> Grids { get; set; } = new() {
			["processes"] = new BlazorGridSettings {
				Columns = new[] { new ApplicationGridColumnPreferences { Key = "name", Title = "Process name", Width = 190 } }
			}
		};
	}

	public enum OptionMode { First, Second }

	public class SerializationProbe : SettingsObject {
		[JsonIgnore] public Action Serialized { get; set; }

		[OnSerializing]
		private void OnSerializing(StreamingContext context) {
			Serialized?.Invoke();
		}
	}

	public class UnrelatedSettings : SettingsObject;

	public class UnopenedScreen : BlazorApplicationScreen {
		public UnopenedScreen() {
			throw new InvalidOperationException("Options discovery cannot activate an operational screen.");
		}
	}

	public class BlockingValidationEditor : ComponentBase, IBlazorApplicationSettingsEditor {
		[Parameter] public object Value { get; set; }
		[Parameter] public EventCallback Changed { get; set; }

		public Task<Result> PendingValidation { get; set; }

		public int Validations { get; private set; }

		public Task<Result> ValidateAsync(CancellationToken cancellationToken = default) {
			Validations++;
			return PendingValidation?.WaitAsync(cancellationToken) ?? Task.FromResult(Result.Success);
		}
	}

	public class CustomEditor : ComponentBase, IBlazorApplicationSettingsEditor {
		[Parameter] public object Value { get; set; }
		[Parameter] public EventCallback Changed { get; set; }

		public Task<Result> ValidateAsync(CancellationToken cancellationToken = default) {
			return Task.FromResult(Result.Success);
		}
	}

	public class LifecycleScreen : BlazorApplicationScreen {
		public Options Values { get; private set; }
		public bool CaptureDetached { get; set; }
		public Action AfterCapture { get; set; }
		public string OperationalDraft { get; set; }
		public bool AllowLeave { get; set; } = true;
		public int Applications { get; private set; }
		public int Deactivations { get; private set; }
		public Func<Options, Task> BeforeApply { get; set; }
		public Action<Options> AfterApply { get; set; }

		public void Edit(int number) {
			Values.Number = number;
			MarkSettingsChanged();
		}

		public Task SaveChangeAsync() {
			return SaveSettingsChangesAsync();
		}

		protected override async Task OnSettingsChangedAsync(object values) {
			var options = (Options)values;
			if (BeforeApply != null)
				await BeforeApply(options);
			Values = options;
			Applications++;
			AfterApply?.Invoke(options);
		}

		protected override object CaptureSettings() {
			var values = CaptureDetached ? Tools.Json.ReadFromString<Options>(Tools.Json.WriteToString(Values)) : Values;
			AfterCapture?.Invoke();
			return values;
		}

		protected override Task<bool> CanDeactivateAsync(CancellationToken cancellationToken) {
			return Task.FromResult(AllowLeave);
		}

		protected override Task OnDeactivatedAsync(CancellationToken cancellationToken) {
			Deactivations++;
			return Task.CompletedTask;
		}

		protected override void BuildRenderTree(RenderTreeBuilder builder) {
			builder.AddContent(0, Values?.Number);
			builder.AddContent(1, OperationalDraft);
		}
	}

	private sealed class LifecycleOwner : IAsyncDisposable {
		private readonly ServiceProvider _services;
		private bool _rendererDisposed;

		private LifecycleOwner(ServiceProvider services, HtmlRenderer renderer) {
			_services = services;
			Renderer = renderer;
			Host = services.GetRequiredService<IBlazorApplicationScreenHost>();
		}

		public HtmlRenderer Renderer { get; }
		public IBlazorApplicationScreenHost Host { get; }
		public BlazorApplicationScreenSession First { get; private set; }
		public BlazorApplicationScreenSession Second { get; private set; }
		public ApplicationSettingsProfile Profile { get; private set; }
		public LifecycleScreen FirstScreen => (LifecycleScreen)First.Screen;
		public LifecycleScreen SecondScreen => (LifecycleScreen)Second.Screen;

		public static async Task<LifecycleOwner> CreateAsync(LifecycleStore store) {
			var services = new ServiceCollection().AddLogging().AddSingleton<IApplicationSettingsStore>(store).AddSphere10Blazor();
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
				var application = provider.GetRequiredService<IBlazorApplication>();
				await application.InitializeAsync();
				var block = owner.Host.Blocks.Single();
				var definition = block.ScreenDefinitions.Single();
				owner.Profile = application.Settings.GetProfile(block, definition.Registration);
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

		// Join the actual renderer update tails, including sections queued by an earlier asynchronous hook.
		public async Task WaitForSettingsAsync() {
			var field = typeof(BlazorApplicationScreen).GetField("_settingsUpdates", BindingFlags.Instance | BindingFlags.NonPublic);
			Task[] pending;
			bool changed;
			do {
				pending = await Renderer.Dispatcher.InvokeAsync(() => new[] { (Task)field.GetValue(FirstScreen), (Task)field.GetValue(SecondScreen) });
				await Task.WhenAll(pending).WaitAsync(TimeSpan.FromSeconds(5));
				changed = await Renderer.Dispatcher.InvokeAsync(() => !ReferenceEquals(pending[0], field.GetValue(FirstScreen))
					|| !ReferenceEquals(pending[1], field.GetValue(SecondScreen)));
			} while (changed);
		}

		public async Task DisposeRendererAsync() {
			if (_rendererDisposed)
				return;
			_rendererDisposed = true;
			await Renderer.DisposeAsync();
		}

		public async ValueTask DisposeAsync() {
			await DisposeRendererAsync();
			await _services.DisposeAsync();
		}
	}

	private sealed class LifecycleStore : IApplicationSettingsStore {
		private readonly ConcurrentDictionary<string, string> _values = new();

		public ConcurrentQueue<int> SavedNumbers { get; } = new();
		public Func<Options, Task> BeforeSave { get; init; }
		public bool FailWrite { get; set; }

		public Task<ApplicationSettingsStoredProfile> LoadAsync(string key, object defaults, bool hasExplicitStorageId, object storageId, CancellationToken cancellationToken = default) {
			return Task.Run(() => _values.TryGetValue(key, out var values)
				? new ApplicationSettingsStoredProfile { Values = Tools.Json.ReadFromString<Options>(values) } : null, cancellationToken);
		}

		public Task SaveAsync(string key, ApplicationSettingsStoredProfile snapshot, bool hasExplicitStorageId, object storageId, CancellationToken cancellationToken = default) {
			return Task.Run(async () => {
				var values = (Options)snapshot.Values;
				if (BeforeSave != null)
					await BeforeSave(values);
				if (FailWrite)
					throw new InvalidOperationException("Write failed.");
				cancellationToken.ThrowIfCancellationRequested();
				_values[key] = Tools.Json.WriteToString(values);
				SavedNumbers.Enqueue(values.Number);
			}, cancellationToken);
		}
	}

	private sealed class BrowserState(string anonymousProfile) {
		public string AnonymousProfile { get; } = anonymousProfile;
		public ConcurrentDictionary<string, string> Values { get; } = new();
		public Action AfterWrite { get; init; }
		public bool FailWrite { get; set; }
	}

	private sealed class StoreOwner : IAsyncDisposable {
		private readonly ServiceProvider _services;

		public StoreOwner(BrowserState browser, string subject) {
			Authentication = new TestAuthentication { Subject = subject };
			_services = new ServiceCollection().AddSingleton<IJSRuntime>(new BrowserRuntime(browser))
				.AddSingleton<AuthenticationStateProvider>(Authentication).BuildServiceProvider();
			Store = new BlazorApplicationSettingsStore(_services);
		}

		public TestAuthentication Authentication { get; }
		public BlazorApplicationSettingsStore Store { get; }

		public async ValueTask DisposeAsync() {
			await Store.DisposeAsync();
			await _services.DisposeAsync();
		}
	}

	private sealed class TestAuthentication : AuthenticationStateProvider {
		public string Subject { get; set; }

		public override Task<AuthenticationState> GetAuthenticationStateAsync() {
			return Task.FromResult(new AuthenticationState(new ClaimsPrincipal(Subject == null ? new ClaimsIdentity()
				: new ClaimsIdentity(new[] { new Claim(ClaimTypes.NameIdentifier, Subject) }, "test"))));
		}
	}

	private sealed class BrowserRuntime(BrowserState browser) : IJSRuntime, IJSObjectReference {
		public ValueTask<TValue> InvokeAsync<TValue>(string identifier, object[] arguments) {
			return InvokeAsync<TValue>(identifier, CancellationToken.None, arguments);
		}

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
				case "Write":
					if (browser.FailWrite)
						throw new JSException("Browser storage is unavailable.");
					browser.Values[(string)arguments[0]] = (string)arguments[1];
					browser.AfterWrite?.Invoke();
					break;
			}
			return ValueTask.FromResult(value == null ? default : (TValue)value);
		}

		public ValueTask DisposeAsync() {
			return ValueTask.CompletedTask;
		}
	}
}
