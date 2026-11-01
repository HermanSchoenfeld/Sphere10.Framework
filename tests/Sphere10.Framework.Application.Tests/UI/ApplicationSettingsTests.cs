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
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Application.Tests;

[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class ApplicationSettingsTests {
	[Test]
	public async Task DetachedDefaultsPreserveColorAndDeepCopyMutableStructMembers() {
		var original = new ValueOptions {
			Color = Color.CornflowerBlue,
			Nested = new MutableValue { Items = new List<string> { "original" } }
		};
		var profile = await CreateProfileAsync(new MemoryStore(), original);

		var copy = (ValueOptions)profile.CreateDefaults();
		copy.Nested.Items.Add("copy");

		Assert.That(copy.Color, Is.EqualTo(Color.CornflowerBlue));
		Assert.That(original.Nested.Items, Is.EqualTo(new[] { "original" }));
		Assert.That(copy.Nested.Items, Is.EqualTo(new[] { "original", "copy" }));
	}

	public sealed class ValueOptions {
		public Color Color { get; set; }

		public MutableValue Nested { get; set; }
	}

	public struct MutableValue {
		public List<string> Items { get; set; }
	}

	[Test]
	public async Task CatalogIncludesUnopenedNoOptionsAndApplicationProfilesWithoutCreatingScreensOrWriting() {
		var store = new MemoryStore();
		var configured = new ApplicationScreenBuilder<UnopenedScreen>().WithStableKey("configured").UseSettings<Options>().Build();
		var noOptions = new ApplicationScreenDefinition(typeof(UnopenedScreen));
		var block = new TestApplicationBlock("Inspect", configured);
		block.AddScreenDefinition(noOptions);
		var root = new ApplicationSettingsDefinition("shared", "Sampling", new Options());
		var catalog = new ApplicationSettingsCatalog("application", store, new[] { block }, new[] { root });

		Assert.That(catalog.Profiles, Has.Length.EqualTo(3));
		Assert.That(catalog.Profiles.Count(profile => !profile.HasOptions), Is.EqualTo(1));
		Assert.That(() => catalog.GetProfile(block, configured.Registration).GetValues(), Throws.InvalidOperationException);
		await catalog.InitializeAsync();

		Assert.That(catalog.IsInitialized, Is.True);
		Assert.That(catalog.GetProfile("shared").ApplicationDefinition, Is.SameAs(root));
		Assert.That(catalog.GetProfile(block, configured.Registration).ScreenDefinition, Is.SameAs(configured));
		Assert.That(catalog.GetProfile(block, configured.Registration).GetValues<Options>().Number, Is.EqualTo(10));
		Assert.That(() => catalog.GetProfile(block, noOptions.Registration).BeginEdit(), Throws.InvalidOperationException);
		Assert.That(store.LoadCalls, Is.EqualTo(2));
		Assert.That(store.SaveCalls, Is.Zero);
	}

	[Test]
	public async Task NoOptionsCatalogNeedsNeitherAStoreNorPersistentKeysForSameTypeRegistrations() {
		var first = new ApplicationScreenDefinition(typeof(UnopenedScreen));
		var block = new TestApplicationBlock("No options", first);
		block.AddScreenDefinition(new ApplicationScreenDefinition(typeof(UnopenedScreen)));
		var catalog = new ApplicationSettingsCatalog("application", null, new[] { block });

		await catalog.InitializeAsync();

		Assert.That(catalog.Profiles, Has.Length.EqualTo(2));
		Assert.That(catalog.Profiles.All(profile => profile.ProfileKey == null), Is.True);
	}

	[Test]
	public async Task SuppliedOrdinaryObjectsKeepFrozenResetDefaultsAfterCallerAndEditorChanges() {
		var original = new PlainOptions { Number = 27, Nested = new Node { Name = "original defaults" } };
		var builder = new ApplicationScreenBuilder<UnopenedScreen>().UseSettings(original);
		original.Number = 99;
		original.Nested.Name = "caller changed";
		var definition = builder.Build();
		var block = new TestApplicationBlock("Inspect", definition);
		var store = new MemoryStore();
		var catalog = new ApplicationSettingsCatalog("application", store, new[] { block });
		((PlainOptions)definition.Settings).Number = 88;
		await catalog.InitializeAsync();
		var profile = catalog.GetProfile(block, definition.Registration);
		var editor = profile.BeginEdit();
		((PlainOptions)editor.Draft).Number = 42;
		((PlainOptions)editor.Draft).Nested.Name = "draft changed";

		editor.Reset();

		Assert.That(profile.SettingsType, Is.EqualTo(typeof(PlainOptions)));
		Assert.That(((PlainOptions)editor.Draft).Number, Is.EqualTo(27));
		Assert.That(((PlainOptions)editor.Draft).Nested.Name, Is.EqualTo("original defaults"));
		Assert.That(profile.GetValues<PlainOptions>().Number, Is.EqualTo(27));
		Assert.That(store.SaveCalls, Is.Zero);
	}

	[Test]
	public async Task OrdinarySettingsFactoriesCreateFreshResetValuesWithoutActivatingScreens() {
		var defaultNumber = 31;
		var definition = new ApplicationScreenBuilder<UnopenedScreen>()
			.UseSettings(() => new ConstructorOptions("factory argument") { Number = defaultNumber })
			.Build();
		var block = new TestApplicationBlock("Inspect", definition);
		var catalog = new ApplicationSettingsCatalog("application", new MemoryStore(), new[] { block });
		await catalog.InitializeAsync();
		var profile = catalog.GetProfile(block, definition.Registration);
		var editor = profile.BeginEdit();
		defaultNumber = 52;

		editor.Reset();
		var reset = (ConstructorOptions)editor.Draft;
		reset.Nested.Name = "edited default";
		editor.Reset();

		Assert.That(((ConstructorOptions)editor.Draft).Number, Is.EqualTo(52));
		Assert.That(((ConstructorOptions)editor.Draft).Caption, Is.EqualTo("factory argument"));
		Assert.That(((ConstructorOptions)editor.Draft).Nested.Name, Is.EqualTo("nested default"));
		Assert.That(profile.GetValues<ConstructorOptions>().Number, Is.EqualTo(31));
	}

	[Test]
	public async Task SuppliedConstructorArgumentObjectsKeepResetDefaultsAndRoundTripDetachedValues() {
		var original = new ConstructorOptions("constructor defaults") { Number = 17 };
		var definition = new ApplicationScreenBuilder<UnopenedScreen>().UseSettings(original).Build();
		original.Number = 99;
		var block = new TestApplicationBlock("Inspect", definition);
		var store = new MemoryStore();
		var catalog = new ApplicationSettingsCatalog("application", store, new[] { block });
		await catalog.InitializeAsync();
		var profile = catalog.GetProfile(block, definition.Registration);
		var editor = profile.BeginEdit();
		var draft = (ConstructorOptions)editor.Draft;
		draft.Number = 42;
		draft.Nested.Name = "accepted nested";
		draft.Items.Add(new Node { Name = "accepted item" });
		await editor.ApplyAsync();
		editor.Reset();

		Assert.That(((ConstructorOptions)editor.Draft).Number, Is.EqualTo(17));
		Assert.That(((ConstructorOptions)editor.Draft).Caption, Is.EqualTo("constructor defaults"));
		Assert.That(((ConstructorOptions)editor.Draft).Items, Is.Empty);
		Assert.That(profile.GetValues<ConstructorOptions>().Items.Single().Name, Is.EqualTo("accepted item"));

		var restartedBlock = new TestApplicationBlock("Inspect", definition.Clone());
		var restarted = new ApplicationSettingsCatalog("application", new MemoryStore(store.Records), new[] { restartedBlock });
		await restarted.InitializeAsync();
		var persisted = restarted.GetProfile(restartedBlock, definition.Registration).GetValues<ConstructorOptions>();

		Assert.That(persisted.Number, Is.EqualTo(42));
		Assert.That(persisted.Nested.Name, Is.EqualTo("accepted nested"));
		persisted.Items[0].Name = "caller mutation";
		Assert.That(profile.GetValues<ConstructorOptions>().Items[0].Name, Is.EqualTo("accepted item"));
		Assert.That(original.Items, Is.Empty);
	}

	[Test]
	public async Task OrdinaryObjectAnnotationsAndValidationProtectDetachedDraftAndFreshStoreRoundTrip() {
		var store = new MemoryStore();
		var profile = await CreateProfileAsync(store, new PlainOptions());
		var editor = profile.BeginEdit();
		var draft = (PlainOptions)editor.Draft;
		draft.Number = -1;
		Assert.That((await editor.ApplyAsync()).IsFailure, Is.True);
		draft.Number = 40;
		draft.Caption = null;
		Assert.That((await editor.ApplyAsync()).IsFailure, Is.True);
		draft.Caption = "Accepted ordinary values";
		draft.Items.Add(new Node());
		Assert.That((await editor.ApplyAsync()).IsFailure, Is.True);
		Assert.That(editor.Draft, Is.SameAs(draft));
		Assert.That(store.SaveCalls, Is.Zero);
		Assert.That(profile.GetValues<PlainOptions>().Number, Is.EqualTo(10));

		draft.Items[0].Name = "accepted item";
		draft.Nested.Name = "accepted nested";
		draft.Tags = new[] { "one", "two" };
		draft.ID = "ordinary value ID";
		draft.Provider = "ordinary value provider";
		Assert.That((await editor.ApplyAsync()).IsSuccess, Is.True);
		((PlainOptions)editor.Draft).Items[0].Name = "unapplied";
		var restarted = await CreateProfileAsync(new MemoryStore(store.Records), new PlainOptions());
		var persisted = restarted.GetValues<PlainOptions>();

		Assert.That(persisted.Number, Is.EqualTo(40));
		Assert.That(persisted.Caption, Is.EqualTo("Accepted ordinary values"));
		Assert.That(persisted.Items.Single().Name, Is.EqualTo("accepted item"));
		Assert.That(persisted.Nested.Name, Is.EqualTo("accepted nested"));
		Assert.That(persisted.Tags, Is.EqualTo(new[] { "one", "two" }));
		Assert.That(persisted.ID, Is.EqualTo("ordinary value ID"));
		Assert.That(persisted.Provider, Is.EqualTo("ordinary value provider"));
		Assert.That(restarted.SettingsType, Is.EqualTo(typeof(PlainOptions)));
		editor.Reset();
		Assert.That(((PlainOptions)editor.Draft).Number, Is.EqualTo(10));
		Assert.That(store.SaveCalls, Is.EqualTo(1));
	}

	[Test]
	public async Task DeclaredOptionsRequireAStoreAndDuplicateTypeProfilesRequireStableKeys() {
		var settings = new Options();
		var definition = new ApplicationScreenDefinition(typeof(UnopenedScreen), settings: settings);
		var block = new TestApplicationBlock("Inspect", definition);
		var catalog = new ApplicationSettingsCatalog("application", null, new[] { block });
		await Assert.ThatAsync(async () => await catalog.InitializeAsync(), Throws.InvalidOperationException);

		block.AddScreenDefinition(new ApplicationScreenDefinition(typeof(UnopenedScreen), settings: settings));
		Assert.That(() => new ApplicationSettingsCatalog("application", new MemoryStore(), new[] { block }), Throws.ArgumentException);
	}

	[Test]
	public async Task SameTypeRegistrationsHaveIndependentProfilesAndCaptionChangesKeepExplicitIdentity() {
		var settings = new Options();
		var first = new ApplicationScreenDefinition(typeof(UnopenedScreen), "CPU", stableKey: "cpu", settings: settings);
		var second = new ApplicationScreenDefinition(typeof(UnopenedScreen), "GPU", stableKey: "gpu", settings: settings);
		var block = new TestApplicationBlock("Displayed caption", first);
		block.AddScreenDefinition(second);
		var store = new MemoryStore();
		var catalog = new ApplicationSettingsCatalog("application", store, new[] { block }, blockKeySelector: _ => "stable-block");
		await catalog.InitializeAsync();
		var edit = catalog.GetProfile(block, first.Registration).BeginEdit();
		((Options)edit.Draft).Number = 42;
		await edit.ApplyAsync();

		var renamed = first.WithTitle("Renamed CPU");
		var rebuiltBlock = new TestApplicationBlock("Renamed block caption", renamed);
		rebuiltBlock.AddScreenDefinition(second.Clone());
		var restarted = new ApplicationSettingsCatalog("application", new MemoryStore(store.Records), new[] { rebuiltBlock }, blockKeySelector: _ => "stable-block");
		await restarted.InitializeAsync();

		Assert.That(restarted.GetProfile(rebuiltBlock, first.Registration).ProfileKey, Is.EqualTo(catalog.GetProfile(block, first.Registration).ProfileKey));
		Assert.That(restarted.GetProfile(rebuiltBlock, first.Registration).GetValues<Options>().Number, Is.EqualTo(42));
		Assert.That(restarted.GetProfile(rebuiltBlock, second.Registration).GetValues<Options>().Number, Is.EqualTo(10));
	}

	[Test]
	public void RootDefinitionCopiesKeepCustomEditorDefaultsAndExplicitNullStorageId() {
		var root = new ApplicationSettingsDefinition("root", "Root", new Options { Number = 27 }, settingsEditor: typeof(Node));
		var copy = root.WithStorageId(null);
		var definition = new ApplicationScreenBuilder<UnopenedScreen>().UseSettings(copy.Settings).Build();

		Assert.That(copy.SettingsEditor, Is.EqualTo(typeof(Node)));
		Assert.That(copy.HasExplicitStorageId, Is.True);
		Assert.That(copy.StorageId, Is.Null);
		Assert.That(root.HasExplicitStorageId, Is.False);
		Assert.That(((Options)definition.Clone().WithTitle("Renamed").Settings).Number, Is.EqualTo(27));
		Assert.That(((Options)copy.SettingsFactory()).Number, Is.EqualTo(27));
	}

	[TestCase(null)]
	[TestCase("original-record")]
	public async Task ExplicitLegacyRootApplyKeepsOriginalIdAndDoesNotInventMigrationSections(string storageId) {
		var root = new ApplicationSettingsDefinition("legacy", "Legacy", new Options()).WithStorageId(storageId);
		var store = new MemoryStore();
		var catalog = new ApplicationSettingsCatalog("application", store, Array.Empty<IApplicationBlock>(), new[] { root });
		await catalog.InitializeAsync();
		var profile = catalog.GetProfile("legacy");
		var editor = profile.BeginEdit();
		((Options)editor.Draft).Number = 47;

		Assert.That((await editor.ApplyAsync()).IsSuccess, Is.True);

		Assert.That(profile.SectionVersions, Is.Empty);
		Assert.That(store.Records[profile.ProfileKey].Sections, Is.Empty);
		Assert.That(profile.GetValues<Options>().Number, Is.EqualTo(47));
		Assert.That(store.LastLoadHasExplicitStorageId, Is.True);
		Assert.That(store.LastLoadStorageId, Is.EqualTo(storageId));
		Assert.That(store.LastSaveHasExplicitStorageId, Is.True);
		Assert.That(store.LastSaveStorageId, Is.EqualTo(storageId));
	}

	[Test]
	public void ApplicationBuilderCapturesRootContributionsAndDecoratorForwardsTheSameLazyCatalog() {
		var application = new SettingsBuilder()
			.WithTitle("A displayed caption")
			.WithSettingsKey("stable-application")
			.AddSettings("sampling", "Sampling", () => new ConstructorOptions("root factory") { Number = 50 })
			.AddSettings("unnamed", () => new ConstructorOptions("without explicit title"))
			.Build();
		using var owner = application;
		var decorator = new SettingsApplicationDecorator(application);

		Assert.That(application.Settings.ApplicationKey, Is.EqualTo("stable-application"));
		Assert.That(application.Settings.GetProfile("sampling").SettingsType, Is.EqualTo(typeof(ConstructorOptions)));
		Assert.That(((ConstructorOptions)application.Settings.GetProfile("sampling").CreateDefaults()).Number, Is.EqualTo(50));
		Assert.That(((ConstructorOptions)application.Settings.GetProfile("unnamed").CreateDefaults()).Caption, Is.EqualTo("without explicit title"));
		Assert.That(decorator.Settings, Is.SameAs(application.Settings));
	}

	[Test]
	public async Task DuplicateRootKeysAndInvalidDefaultsFailWithoutCreatingRecords() {
		var builder = new SettingsBuilder().AddSettings<Options>("duplicate");
		Assert.That(() => builder.AddSettings<Options>("duplicate"), Throws.ArgumentException);
		var store = new MemoryStore();
		var catalog = Catalog(store, new Options { Number = -1 });

		await Assert.ThatAsync(async () => await catalog.InitializeAsync(), Throws.InvalidOperationException);
		Assert.That(store.LoadCalls, Is.Zero);
		Assert.That(store.SaveCalls, Is.Zero);
	}

	[Test]
	public async Task DetachedCopiesPreserveNestedArrayCollectionValuesAndExcludeProviderContext() {
		var node = new Node { Name = "original" };
		var original = new Options {
			Nested = node,
			Items = new List<Node> { node },
			Array = new[] { node },
			Map = new Dictionary<string, Node>(StringComparer.OrdinalIgnoreCase) { ["Name"] = node },
			Matrix = new[,] { { node, new Node { Name = "second" } } },
			Provider = new UncopyableProvider(),
			ID = new UncopyableProvider()
		};
		var profile = await CreateProfileAsync(new MemoryStore(), original);
		var first = profile.GetValues<Options>();
		var second = profile.GetValues<Options>();

		Assert.That(first.Provider, Is.Null);
		Assert.That(first.ID, Is.Null);
		Assert.That(first.Nested, Is.Not.SameAs(node));
		Assert.That(first.Items.Single(), Is.SameAs(first.Nested));
		Assert.That(first.Array.Single(), Is.SameAs(first.Nested));
		Assert.That(first.Map["name"], Is.SameAs(first.Nested));
		Assert.That(first.Matrix[0, 0], Is.SameAs(first.Nested));
		Assert.That(first.Matrix[0, 1].Name, Is.EqualTo("second"));
		first.Nested.Name = "edited";
		first.Items.Clear();
		Assert.That(second.Nested.Name, Is.EqualTo("original"));
		Assert.That(second.Items, Has.Count.EqualTo(1));
		Assert.That(original.Nested.Name, Is.EqualTo("original"));
	}

	[Test]
	public async Task OwnerThreadCopiesDoNotInvokeJsonSerializationCallbacks() {
		var profile = await CreateProfileAsync(new MemoryStore(), new SerializationSpy());

		Assert.That(() => profile.CreateDefaults(), Throws.Nothing);
		Assert.That(() => profile.BeginEdit(), Throws.Nothing);
		Assert.That(() => profile.Capture(profile.GetValues(), new object(), profile.Revision, 1), Throws.Nothing);
	}

	[Test]
	public async Task ResetAndCancelUseDetachedAuthoritativeDefaultsWithoutWriting() {
		var calls = 0;
		var store = new MemoryStore();
		var profile = await CreateProfileAsync(store, new Options(), () => {
			calls++;
			return new Options { Number = 31, Nested = new Node { Name = "factory default" } };
		});
		var edit = profile.BeginEdit();
		((Options)edit.Draft).Nested.Name = "provisional";
		((Options)edit.Draft).Items.Add(new Node());
		edit.Cancel();
		Assert.That(((Options)edit.Draft).Nested.Name, Is.EqualTo("factory default"));
		Assert.That(((Options)edit.Draft).Items, Is.Empty);

		edit.Reset();
		Assert.That(((Options)edit.Draft).Number, Is.EqualTo(31));
		Assert.That(calls, Is.EqualTo(2));
		Assert.That(store.SaveCalls, Is.Zero);
		Assert.That(profile.GetValues<Options>().Nested.Name, Is.EqualTo("factory default"));
	}

	[Test]
	public async Task ValidationInvokesTheSettingsObjectsConstraintsBeforePersistence() {
		var store = new MemoryStore();
		var profile = await CreateProfileAsync(store, new ConstrainedOptions());
		var edit = profile.BeginEdit();
		((Options)edit.Draft).Number = -1;
		Assert.That((await edit.ApplyAsync()).IsFailure, Is.True);
		((Options)edit.Draft).Number = 101;
		Assert.That((await edit.ApplyAsync()).IsFailure, Is.True);

		Assert.That(store.SaveCalls, Is.Zero);
		Assert.That(profile.Revision, Is.Zero);
		Assert.That(profile.GetValues<Options>().Number, Is.EqualTo(10));
	}

	[Test]
	public async Task FailedApplyPreservesTheDraftAcceptedRevisionAndObservableErrorForRetry() {
		var store = new MemoryStore { SaveError = new InvalidOperationException("Storage unavailable.") };
		var profile = await CreateProfileAsync(store);
		var edit = profile.BeginEdit();
		((Options)edit.Draft).Number = 25;
		var draft = edit.Draft;
		var events = 0;
		profile.Changed += _ => events++;

		await Assert.ThatAsync(async () => await edit.ApplyAsync(), Throws.InvalidOperationException);
		Assert.That(edit.Draft, Is.SameAs(draft));
		Assert.That(((Options)edit.Draft).Number, Is.EqualTo(25));
		Assert.That(profile.GetValues<Options>().Number, Is.EqualTo(10));
		Assert.That(profile.LastSaveError, Is.SameAs(store.SaveError));
		Assert.That(profile.Revision, Is.Zero);
		Assert.That(events, Is.Zero);

		store.SaveError = null;
		Assert.That((await edit.ApplyAsync()).IsSuccess, Is.True);
		Assert.That(profile.GetValues<Options>().Number, Is.EqualTo(25));
		Assert.That(profile.Revision, Is.EqualTo(1));
		Assert.That(profile.LastSaveError, Is.Null);
		Assert.That(events, Is.EqualTo(1));
		((Options)edit.Draft).Nested.Name = "unapplied after Apply";
		edit.Cancel();
		Assert.That(((Options)edit.Draft).Nested.Name, Is.EqualTo("default"));
	}

	[Test]
	public async Task StoreReceivesExclusiveSnapshotAndPublicationWaitsForSuccessfulPersistence() {
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var store = new MemoryStore {
			BeforeSave = async snapshot => {
				((Options)snapshot.Values).Nested.Name = "temporary encryption";
				entered.TrySetResult();
				await release.Task;
				((Options)snapshot.Values).Nested.Name = "default";
			}
		};
		var profile = await CreateProfileAsync(store);
		var edit = profile.BeginEdit();
		((Options)edit.Draft).Number = 35;
		var events = new List<ApplicationSettingsChangedEventArgs>();
		profile.Changed += events.Add;
		var save = edit.ApplyAsync();
		await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
		Assert.That(profile.Revision, Is.Zero);
		Assert.That(profile.GetValues<Options>().Number, Is.EqualTo(10));
		Assert.That(((Options)edit.Draft).Nested.Name, Is.EqualTo("default"));
		Assert.That(events, Is.Empty);

		release.TrySetResult();
		await save;
		Assert.That(profile.GetValues<Options>().Number, Is.EqualTo(35));
		Assert.That(events.Single().Source, Is.SameAs(edit));
		Assert.That(events.Single().SourceVersion, Is.Zero);
		Assert.That(events.Single().Revision, Is.EqualTo(1));
	}

	[Test]
	public async Task OlderBlockedCaptureThenNewerApplyEndsWithNewerDiskAndAcceptedValues() {
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var store = new MemoryStore {
			BeforeSave = async snapshot => {
				if (((Options)snapshot.Values).Number == 11) {
					entered.TrySetResult();
					await release.Task;
				}
			}
		};
		var profile = await CreateProfileAsync(store);
		var live = profile.GetValues<Options>();
		live.Number = 11;
		var capture = profile.Capture(live, new object(), profile.Revision, 1);
		var older = profile.SaveCaptureAsync(capture);
		await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
		live.Number = 999;
		var editor = profile.BeginEdit();
		((Options)editor.Draft).Number = 22;
		var newer = editor.ApplyAsync();

		release.TrySetResult();
		Assert.That(await older, Is.True);
		Assert.That((await newer).IsSuccess, Is.True);
		Assert.That(profile.GetValues<Options>().Number, Is.EqualTo(22));
		var fresh = await CreateProfileAsync(new MemoryStore(store.Records));
		Assert.That(fresh.GetValues<Options>().Number, Is.EqualTo(22));
	}

	[Test]
	public async Task SameOwnerQueuedVersionsSurviveOwnEarlierWriteButStaleForeignCaptureCannotOverwriteApply() {
		var store = new MemoryStore();
		var profile = await CreateProfileAsync(store);
		var owner = new object();
		var first = profile.Capture(new Options { Number = 11 }, owner, 0, 1);
		var second = profile.Capture(new Options { Number = 12 }, owner, 0, 2);
		Assert.That(await profile.SaveCaptureAsync(first), Is.True);
		Assert.That(await profile.SaveCaptureAsync(second), Is.True);
		Assert.That(await profile.SaveCaptureAsync(first), Is.False);
		var stale = profile.Capture(new Options { Number = 1 }, new object(), 0, 1);
		var editor = profile.BeginEdit();
		((Options)editor.Draft).Number = 30;
		await editor.ApplyAsync();

		Assert.That(await profile.SaveCaptureAsync(stale), Is.False);
		Assert.That(profile.GetValues<Options>().Number, Is.EqualTo(30));
		Assert.That(store.SaveCalls, Is.EqualTo(3));
	}

	[Test]
	public async Task BackgroundSaveFailureKeepsItsVersionRetryableAndFlushReportsIt() {
		var store = new MemoryStore { SaveError = new InvalidOperationException("Write failed.") };
		var catalog = Catalog(store, new Options());
		await catalog.InitializeAsync();
		var profile = catalog.GetProfile("preferences");
		var capture = profile.Capture(new Options { Number = 15 }, new object(), 0, 1);
		await Assert.ThatAsync(async () => await profile.SaveCaptureAsync(capture), Throws.InvalidOperationException);
		await Assert.ThatAsync(async () => await catalog.FlushAsync(), Throws.InvalidOperationException);
		store.SaveError = null;

		Assert.That(await profile.SaveCaptureAsync(capture), Is.True);
		Assert.That(profile.GetValues<Options>().Number, Is.EqualTo(15));
		Assert.That(profile.LastSaveError, Is.Null);
		await Assert.ThatAsync(async () => await catalog.FlushAsync(), Throws.Nothing);
	}

	[Test]
	public async Task InitializationNotifiesOutsideTheStateLockAndCopiesOneFrozenSectionPerConsumer() {
		var store = new MemoryStore();
		var profile = await CreateProfileAsync(store);
		ApplicationSettingsChangedEventArgs initialized = null;
		var initializerCalls = 0;
		var readOutsideLock = false;
		profile.Changed += change => {
			if (!change.IsSectionInitialization)
				return;
			initialized = change;
			readOutsideLock = Task.Run(() => profile.GetValues()).Wait(TimeSpan.FromSeconds(2));
		};

		Assert.That(profile.TryInitializeSection("Grid", 1, values => {
			initializerCalls++;
			((Options)values).Grid.Columns = new[] { new ApplicationGridColumnPreferences { Key = "name", Title = "Name", Width = 240 } };
		}, CopyGridSection), Is.True);
		Assert.That(readOutsideLock, Is.True);
		Assert.That(store.SaveCalls, Is.Zero);
		Assert.That(initialized.Revision, Is.EqualTo(1));

		var first = new Options { Number = 21 };
		initialized.ApplyInitialization(first);
		first.Grid.Columns[0].Width = 260;
		var accepted = profile.BeginEdit();
		((Options)accepted.Draft).Grid.Columns[0].Width = 300;
		await accepted.ApplyAsync();
		var second = new Options { Number = 31 };
		initialized.ApplyInitialization(second);

		Assert.That(initializerCalls, Is.EqualTo(1), "Consumer rebasing must not execute the initializer again.");
		Assert.That(first.Number, Is.EqualTo(21));
		Assert.That(second.Number, Is.EqualTo(31));
		Assert.That(second.Grid.Columns[0].Width, Is.EqualTo(240), "The initialization snapshot remains frozen after later acceptance.");
		Assert.That(first.Grid, Is.Not.SameAs(second.Grid));
		Assert.That(profile.GetValues<Options>().Grid.Columns[0].Width, Is.EqualTo(300));
	}

	[Test]
	public async Task CapturePredatingInitializationCannotOverwriteImportedDataButRebasedCaptureCanSave() {
		var store = new MemoryStore();
		var profile = await CreateProfileAsync(store);
		var working = profile.GetValues<Options>(out var baseline);
		working.Number = 42;
		var producer = new object();
		var oldCapture = profile.Capture(working, producer, baseline, 1);
		ApplicationSettingsChangedEventArgs initialized = null;
		profile.Changed += change => initialized = change;
		profile.TryInitializeSection("Grid", 1, values => ((Options)values).Grid.PageSize = 29, CopyGridSection);

		Assert.That(await profile.SaveCaptureAsync(oldCapture), Is.False);
		Assert.That(store.SaveCalls, Is.Zero);
		Assert.That(profile.GetValues<Options>().Grid.PageSize, Is.EqualTo(29));
		initialized.ApplyInitialization(working);
		var newCapture = profile.Capture(working, producer, initialized.Revision, 2);
		Assert.That(await profile.SaveCaptureAsync(newCapture), Is.True);
		Assert.That(profile.GetValues<Options>().Number, Is.EqualTo(42));
		Assert.That(profile.GetValues<Options>().Grid.PageSize, Is.EqualTo(29));
		Assert.That(store.SaveCalls, Is.EqualTo(1));
	}

	[Test]
	public async Task InitializationPreservesAnOutstandingSaveErrorUntilTheRebasedValueIsRetried() {
		var failure = new InvalidOperationException("Failed preference save.");
		var store = new MemoryStore { SaveError = failure };
		var profile = await CreateProfileAsync(store);
		var working = profile.GetValues<Options>(out var baseline);
		working.Number = 42;
		var producer = new object();
		await Assert.ThatAsync(async () => await profile.SaveCaptureAsync(profile.Capture(working, producer, baseline, 1)), Throws.TypeOf<InvalidOperationException>());
		await profile.WaitForPendingWritesAsync();
		await Assert.ThatAsync(async () => await profile.FlushAsync(), Throws.TypeOf<InvalidOperationException>());
		ApplicationSettingsChangedEventArgs initialized = null;
		profile.Changed += change => initialized = change;
		profile.TryInitializeSection("Grid", 1, values => ((Options)values).Grid.PageSize = 29, CopyGridSection);

		Assert.That(profile.LastSaveError, Is.SameAs(failure));
		Assert.That(store.SaveCalls, Is.EqualTo(1));
		initialized.ApplyInitialization(working);
		store.SaveError = null;
		Assert.That(await profile.SaveCaptureAsync(profile.Capture(working, producer, initialized.Revision, 2)), Is.True);
		Assert.That(profile.LastSaveError, Is.Null);
		Assert.That(profile.GetValues<Options>().Number, Is.EqualTo(42));
	}

	[Test]
	public async Task MigrationPresenceSurvivesResetAndFreshStoreRestartWithoutReimportOrRestoreWrites() {
		var store = new MemoryStore();
		var profile = await CreateProfileAsync(store);
		Assert.That(profile.TryInitializeSection("processes", 1, values => ((Options)values).Grid.Columns = new[] {
			new ApplicationGridColumnPreferences { Key = "name", Title = "Name", Width = 240 }
		}, CopyGridSection), Is.True);
		Assert.That(store.SaveCalls, Is.Zero);
		var editor = profile.BeginEdit();
		editor.Reset();
		Assert.That(((Options)editor.Draft).Grid.Columns, Is.Empty);
		await editor.ApplyAsync();
		var freshStore = new MemoryStore(store.Records);
		var restarted = await CreateProfileAsync(freshStore);
		var imported = false;

		Assert.That(restarted.TryInitializeSection("processes", 1, _ => imported = true, CopyGridSection), Is.False);
		Assert.That(imported, Is.False);
		Assert.That(restarted.SectionVersions["processes"], Is.EqualTo(1));
		Assert.That(restarted.GetValues<Options>().Grid.Columns, Is.Empty);
		Assert.That(freshStore.SaveCalls, Is.Zero);
	}

	[Test]
	public async Task ApplyingUnopenedOptionsMarksPropertyAndDictionaryGridsBeforeTheirFirstLegacyImport() {
		var store = new MemoryStore();
		var profile = await CreateProfileAsync(store, new Options {
			Grids = new Dictionary<string, GridSettings> { ["processes"] = new() }
		});
		var editor = profile.BeginEdit();
		editor.Reset();
		await editor.ApplyAsync();
		Assert.That(profile.SectionVersions.Keys, Does.Contain("Grid").And.Contain("Grids[processes]"));
		Assert.That(profile.TryInitializeSection("Grids[processes]", 1, _ => throw new InvalidOperationException("Accepted live values must not reimport."),
			(source, target) => ((Options)target).Grids["processes"] = ((Options)source).Grids["processes"]), Is.False);
		var fresh = await CreateProfileAsync(new MemoryStore(store.Records));

		Assert.That(fresh.SectionVersions.Keys, Is.EquivalentTo(profile.SectionVersions.Keys));
		Assert.That(fresh.TryInitializeSection("Grid", 1, _ => throw new InvalidOperationException("Existing destination values win."), CopyGridSection), Is.False);
		Assert.That(fresh.TryInitializeSection("Grids[processes]", 1, _ => throw new InvalidOperationException("Unopened Reset must not reimport."),
			(source, target) => ((Options)target).Grids["processes"] = ((Options)source).Grids["processes"]), Is.False);
		Assert.That(fresh.GetValues<Options>().Grids["processes"].Columns, Is.Empty);
	}

	[Test]
	public async Task ApplyingUnopenedRootPropertiesPreventsLegacyImportAfterResetAndRestart() {
		var store = new MemoryStore();
		var profile = await CreateProfileAsync(store, new LoggingOptions());
		var editor = profile.BeginEdit();
		editor.Reset();
		Assert.That(store.SaveCalls, Is.Zero);

		await editor.ApplyAsync();
		Assert.That(profile.TryInitializeSection("LogOutput", 1, _ => throw new InvalidOperationException("Accepted live values must not reimport."), (_, _) => { }), Is.False);
		var fresh = await CreateProfileAsync(new MemoryStore(store.Records), new LoggingOptions());

		Assert.That(fresh.SectionVersions["LogOutput"], Is.Zero);
		Assert.That(fresh.TryInitializeSection("LogOutput", 1, _ => throw new InvalidOperationException("Accepted defaults must not import retired logging settings."), (_, _) => { }), Is.False);
	}

	[TestCase("LogOutput.Name")]
	[TestCase("Targets[primary].Name")]
	[TestCase("Targets[secondary].Name")]
	[TestCase("Optional")]
	public async Task UnopenedApplyOwnsOrdinaryNestedDictionaryAndNullSectionsAfterResetAndRestart(string sectionPath) {
		var shared = new Node { Name = "declared values" };
		var defaults = new SectionOptions {
			LogOutput = shared,
			Targets = new Dictionary<string, Node> { ["primary"] = shared, ["secondary"] = shared }
		};
		var store = new MemoryStore();
		var profile = await CreateProfileAsync(store, defaults);
		var editor = profile.BeginEdit();
		editor.Reset();
		Assert.That(store.SaveCalls, Is.Zero);
		await editor.ApplyAsync();

		Assert.That(profile.TryInitializeSection(sectionPath, 1, _ => throw new InvalidOperationException("Accepted ordinary values must not reimport."), (_, _) => { }), Is.False);
		var fresh = await CreateProfileAsync(new MemoryStore(store.Records), defaults);

		Assert.That(fresh.TryInitializeSection(sectionPath, 1, _ => throw new InvalidOperationException("Accepted defaults must survive restart."), (_, _) => { }), Is.False);
		Assert.That(fresh.SectionVersions[sectionPath], Is.Zero);
		Assert.That(fresh.GetValues<SectionOptions>().Targets["secondary"].Name, Is.EqualTo("declared values"));
	}

	[Test]
	public async Task SectionInitializationRequiresPendingWritesToComplete() {
		var entered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var release = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
		var store = new MemoryStore {
			BeforeSave = async _ => {
				entered.TrySetResult();
				await release.Task;
			}
		};
		var profile = await CreateProfileAsync(store);
		var save = profile.SaveCaptureAsync(profile.Capture(new Options(), new object(), 0, 1));
		await entered.Task.WaitAsync(TimeSpan.FromSeconds(5));
		var waiting = profile.WaitForPendingWritesAsync();
		Assert.That(waiting.IsCompleted, Is.False);
		try {
			Assert.That(() => profile.TryInitializeSection("Grid", 1, _ => { }, CopyGridSection), Throws.InvalidOperationException);
		} finally {
			release.TrySetResult();
		}
		await save;
		await waiting;
		Assert.That(profile.TryInitializeSection("Grid", 1, _ => { }, CopyGridSection), Is.True);
	}

	[Test]
	public async Task AcceptedValueAndRevisionSnapshotRemainAtomicWhileAnotherApplyWaitsToPublish() {
		using var entered = new ManualResetEventSlim();
		using var otherRead = new ManualResetEventSlim();
		using var release = new ManualResetEventSlim();
		var profile = await CreateProfileAsync(new MemoryStore(), new BlockingOptions());
		var editor = profile.BeginEdit();
		((BlockingOptions)editor.Draft).Number = 2;
		var reads = 0;
		BlockingOptions.Reading = () => {
			if (Interlocked.Increment(ref reads) == 1) {
				entered.Set();
				Assert.That(release.Wait(TimeSpan.FromSeconds(5)), Is.True);
			} else {
				otherRead.Set();
			}
		};
		using var cleanup = Tools.Scope.ExecuteOnDispose(() => {
			BlockingOptions.Reading = null;
			release.Set();
		});
		var read = Task.Run(() => {
			var values = profile.GetValues<BlockingOptions>(out var revision);
			return (values.Number, revision);
		});
		Assert.That(entered.Wait(TimeSpan.FromSeconds(5)), Is.True);
		var applying = Task.Run(() => editor.ApplyAsync());
		Assert.That(otherRead.Wait(TimeSpan.FromSeconds(5)), Is.True);
		Assert.That(applying.IsCompleted, Is.False);
		release.Set();

		Assert.That(await read, Is.EqualTo((1, 0L)));
		await applying;
		Assert.That(profile.GetValues<BlockingOptions>(out var accepted).Number, Is.EqualTo(2));
		Assert.That(accepted, Is.EqualTo(1));
	}

	[TestCase(false)]
	[TestCase(true)]
	public async Task MissingOrInvalidStoredValuesUseValidatedDefaultsWithObservableLoadErrors(bool invalid) {
		var store = new MemoryStore();
		if (invalid) {
			var profile = await CreateProfileAsync(store);
			store.Records[profile.ProfileKey] = new Record(Tools.Json.WriteToString(new Options { Number = -1 }), new Dictionary<string, int>());
		} else {
			store.LoadError = new InvalidOperationException("Unreadable settings.");
		}
		var loaded = await CreateProfileAsync(store);

		Assert.That(loaded.GetValues<Options>().Number, Is.EqualTo(10));
		Assert.That(loaded.LastLoadError, Is.Not.Null);
		Assert.That(store.SaveCalls, Is.Zero);
	}

	[Test]
	public async Task CancellationAfterCommittedStoreSaveStillPublishesThePersistedRevision() {
		using var cancellation = new CancellationTokenSource();
		var store = new MemoryStore { AfterCommit = cancellation.Cancel };
		var profile = await CreateProfileAsync(store);
		var editor = profile.BeginEdit();
		((Options)editor.Draft).Number = 55;

		Assert.That((await editor.ApplyAsync(cancellation.Token)).IsSuccess, Is.True);
		Assert.That(profile.Revision, Is.EqualTo(1));
		Assert.That(profile.GetValues<Options>().Number, Is.EqualTo(55));
	}

	[Test]
	public async Task InvalidOldValuesDoNotEraseMigrationPresenceOrReimportRetiredPreferences() {
		var store = new MemoryStore();
		var initial = await CreateProfileAsync(store);
		store.Records[initial.ProfileKey] = new Record(Tools.Json.WriteToString(new Options { Number = -1 }), new Dictionary<string, int> { ["processes"] = 1 });

		var restored = await CreateProfileAsync(new MemoryStore(store.Records));

		Assert.That(restored.LastLoadError, Is.Not.Null);
		Assert.That(restored.GetValues<Options>().Number, Is.EqualTo(10));
		Assert.That(restored.TryInitializeSection("processes", 1, _ => throw new InvalidOperationException("Retired preferences cannot be reimported."), CopyGridSection), Is.False);
	}

	// The same accessor pair supplies an isolated initialized grid to each consumer without replaying its initializer.
	private static void CopyGridSection(object source, object target) {
		((Options)target).Grid = ((Options)source).Grid;
	}

	// A fresh store instance reads independent serialized records rather than a live catalog or provider cache.
	private static async Task<ApplicationSettingsProfile> CreateProfileAsync(MemoryStore store, object settings = null, Func<object> settingsFactory = null) {
		var catalog = Catalog(store, settings ?? new Options(), settingsFactory);
		await catalog.InitializeAsync();
		return catalog.GetProfile("preferences");
	}

	private static ApplicationSettingsCatalog Catalog(MemoryStore store, object settings, Func<object> settingsFactory = null) {
		return new ApplicationSettingsCatalog("application", store, Array.Empty<IApplicationBlock>(),
			new[] { new ApplicationSettingsDefinition("preferences", "Preferences", settings, settingsFactory) });
	}

	private sealed class MemoryStore : IApplicationSettingsStore {
		public MemoryStore(ConcurrentDictionary<string, Record> records = null) {
			Records = records ?? new ConcurrentDictionary<string, Record>();
		}

		public ConcurrentDictionary<string, Record> Records { get; }
		public Func<ApplicationSettingsStoredProfile, Task> BeforeSave { get; init; }
		public Action AfterCommit { get; init; }
		public Exception LoadError { get; set; }
		public Exception SaveError { get; set; }
		public bool LastLoadHasExplicitStorageId { get; private set; }
		public object LastLoadStorageId { get; private set; }
		public bool LastSaveHasExplicitStorageId { get; private set; }
		public object LastSaveStorageId { get; private set; }
		public int LoadCalls;
		public int SaveCalls;

		public Task<ApplicationSettingsStoredProfile> LoadAsync(string key, object defaults, bool hasExplicitStorageId, object storageId, CancellationToken token = default) {
			LastLoadHasExplicitStorageId = hasExplicitStorageId;
			LastLoadStorageId = storageId;
			return Task.Run(() => {
				token.ThrowIfCancellationRequested();
				Interlocked.Increment(ref LoadCalls);
				if (LoadError != null)
					throw LoadError;
				return Records.TryGetValue(key, out var record) ? new ApplicationSettingsStoredProfile {
					Values = Tools.Json.ReadFromString(defaults.GetType(), record.Json),
					SectionVersions = new Dictionary<string, int>(record.Sections)
				} : null;
			}, token);
		}

		public Task SaveAsync(string key, ApplicationSettingsStoredProfile snapshot, bool hasExplicitStorageId, object storageId, CancellationToken token = default) {
			LastSaveHasExplicitStorageId = hasExplicitStorageId;
			LastSaveStorageId = storageId;
			return Task.Run(async () => {
				Interlocked.Increment(ref SaveCalls);
				if (BeforeSave != null)
					await BeforeSave(snapshot);
				if (SaveError != null)
					throw SaveError;
				token.ThrowIfCancellationRequested();
				Records[key] = new Record(Tools.Json.WriteToString(snapshot.Values), new Dictionary<string, int>(snapshot.SectionVersions));
				AfterCommit?.Invoke();
			}, token);
		}
	}

	private sealed record Record(string Json, Dictionary<string, int> Sections);

	public class Options : SettingsObject {
		public int Number { get; set; } = 10;
		public Node Nested { get; set; } = new() { Name = "default" };
		public List<Node> Items { get; set; } = new();
		public Node[] Array { get; set; } = System.Array.Empty<Node>();
		public Dictionary<string, Node> Map { get; set; } = new();
		public Node[,] Matrix { get; set; } = new Node[0, 0];
		public GridSettings Grid { get; set; } = new();
		public Dictionary<string, GridSettings> Grids { get; set; } = new();

		public override Result Validate() {
			return Number >= 0 ? Result.Success : Result.Error("Number cannot be negative.");
		}
	}

	public sealed class GridSettings {
		public int PageSize { get; set; } = 100;

		public ApplicationGridColumnPreferences[] Columns { get; set; } = System.Array.Empty<ApplicationGridColumnPreferences>();
	}

	public class Node {
		public string Name { get; set; }
	}

	public class SerializationSpy : SettingsObject {
		public int Number { get; set; }

		[OnSerializing]
		private void RejectSerialization(StreamingContext context) {
			throw new InvalidOperationException("Capturing UI values must not invoke serialization.");
		}
	}

	public class BlockingOptions : SettingsObject {
		private int _number = 1;

		public static Action Reading { get; set; }

		public int Number {
			get {
				Reading?.Invoke();
				return _number;
			}
			set => _number = value;
		}
	}

	public sealed class ConstrainedOptions : Options {
		public override Result Validate() {
			return Number <= 100 ? base.Validate() : Result.Error("Too large.");
		}
	}

	public sealed class LoggingOptions {
		public Node LogOutput { get; set; } = new() { Name = "logging defaults" };
	}

	public sealed class SectionOptions {
		public Node LogOutput { get; set; }

		public Dictionary<string, Node> Targets { get; set; } = new();

		public Node Optional { get; set; }
	}

	public sealed class ConstructorOptions(string caption) {
		public string Caption { get; set; } = caption;

		public int Number { get; set; }

		public Node Nested { get; set; } = new() { Name = "nested default" };

		public List<Node> Items { get; set; } = new();
	}

	public sealed class PlainOptions : IValidatableObject {
		[System.ComponentModel.DataAnnotations.Range(0, 100)]
		public int Number { get; set; } = 10;

		[Required]
		public string Caption { get; set; } = "Plain settings";

		public Node Nested { get; set; } = new() { Name = "plain default" };

		public List<Node> Items { get; set; } = new();

		public string[] Tags { get; set; } = System.Array.Empty<string>();

		public string ID { get; set; }

		public string Provider { get; set; }

		public IEnumerable<ValidationResult> Validate(ValidationContext context) {
			if (Items.Any(item => string.IsNullOrWhiteSpace(item.Name)))
				yield return new ValidationResult("Each item needs a name.", new[] { nameof(Items) });
		}
	}

	private sealed class SettingsBuilder : ApplicationBuilderBase<SettingsBuilder, SettingsApplication, ApplicationPlugin, ApplicationPluginBuilder, SettingsApplication> {
		public SettingsBuilder() : base("Settings") {
		}

		protected override SettingsApplication CreateApplication(ApplicationPlugin[] plugins, Action<SettingsApplication> configureApplication) {
			var application = new SettingsApplication(SettingsKey, SettingsDefinitions);
			configureApplication?.Invoke(application);
			return application;
		}
	}

	private sealed class SettingsApplication(string key, ApplicationSettingsDefinition[] definitions) : AsyncApplicationBase(null, key, definitions) {
		public override IApplicationPlugin[] LoadedPlugins => System.Array.Empty<IApplicationPlugin>();

		public override IApplicationScreenHost ScreenHost => null;
	}

	private sealed class SettingsApplicationDecorator(SettingsApplication application) : ApplicationDecorator<SettingsApplication>(application);

	public sealed class UnopenedScreen : IApplicationScreen {
		public UnopenedScreen() {
			throw new InvalidOperationException("Catalog discovery cannot construct operational screens.");
		}

		public IApplicationScreenHost Host { get; set; }
		public IApplicationBlock Block { get; set; }
	}

	private sealed class UncopyableProvider : BaseSettingsProvider {
		public string RuntimeState {
			get => throw new InvalidOperationException("Provider state must not be traversed.");
			set => throw new InvalidOperationException("Provider state must not be copied.");
		}

		public override bool ContainsSetting(Type type, object id = null) {
			throw new NotSupportedException();
		}

		public override void DeleteSetting(SettingsObject values) {
			throw new NotSupportedException();
		}

		public override void ClearSettings() {
			throw new NotSupportedException();
		}

		protected override SettingsObject LoadInternal(Type type, object id = null) {
			throw new NotSupportedException();
		}

		protected override void SaveInternal(SettingsObject values) {
			throw new NotSupportedException();
		}
	}
}
