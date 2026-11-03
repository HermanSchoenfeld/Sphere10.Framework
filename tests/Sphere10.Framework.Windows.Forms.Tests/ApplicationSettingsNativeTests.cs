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
using System.ComponentModel.DataAnnotations;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using FormsUI = global::Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>Verifies actual native settings identity, persistence, Options and model-refresh lifecycle behavior.</summary>
[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class ApplicationSettingsNativeTests {
	private bool _startedFramework;

	// Start the existing Framework owner before native controls resolve their services.
	[OneTimeSetUp]
	public void StartFramework() {
		_startedFramework = !Sphere10Framework.Instance.IsStarted;
		if (_startedFramework)
			Sphere10Framework.Instance.Build().Start();
	}

	// End only the Framework lifetime started by this fixture.
	[OneTimeTearDown]
	public void StopFramework() {
		if (_startedFramework)
			Sphere10Framework.Instance.EndFramework();
	}

	// A cached provider must not substitute a different root for the supplied owner settings instance.
	[Test]
	public void ActualSettingsLoadBeforeActivationAndRetainSuppliedIdentity() {
		RunWithMessageLoop(async owner => {
			var store = new CachedSettingsProvider(new MemoryStore());
			using var block = CreateBlock("First");
			var definition = block.ScreenDefinitions.Single();
			var supplied = (ProbeScreen.Preferences)definition.Settings;
			var id = Tools.UI.GetScreenSettingsId("NativeActualSettings", block, definition);
			ISettingsProvider provider = store;
			var earlier = new ProbeScreen.Preferences { ID = id, Provider = provider, Name = "Restored before activation" };
			await earlier.SaveAsync();
			await using var services = CreateApplication(store, block);
			var application = services.GetRequiredService<FormsUI.IApplication>();
			await application.InitializeAsync();
			var main = (QuietMainForm)services.GetRequiredService<IMainForm>();
			main.Show(owner);
			var screen = (ProbeScreen)main.ActiveScreen;

			Assert.That(application.Settings, Is.SameAs(provider));
			Assert.That(definition.Settings, Is.SameAs(supplied));
			Assert.That(screen.ScreenSettings, Is.SameAs(supplied));
			Assert.That(screen.FirstName, Is.EqualTo("Restored before activation"));
			Assert.That(supplied.ID, Is.EqualTo(id));
			Assert.That(supplied.Provider, Is.SameAs(provider));
			Assert.That(supplied.Items, Is.EqualTo(new[] { "Initial" }));
		});
	}

	// Actual block changes refresh their screens on the UI thread, and closed screens unsubscribe.
	[Test]
	public void OwnerNotificationsUseTheExistingRefreshLifecycleAndStopAfterClose() {
		RunWithMessageLoop(async owner => {
			using var firstBlock = CreateBlock("First");
			using var otherBlock = CreateBlock("Other");
			firstBlock.BlockSettings = new ProbeScreen.Preferences { Name = "Block before" };
			otherBlock.BlockSettings = new ProbeScreen.Preferences { ID = "other", Name = "Other before" };
			await using var services = CreateApplication(new MemoryStore(), firstBlock, otherBlock);
			var application = services.GetRequiredService<FormsUI.IApplication>();
			await application.InitializeAsync();
			var main = (QuietMainForm)services.GetRequiredService<IMainForm>();
			main.ScreenMode = ScreenMode.MultiView;
			main.Show(owner);
			var first = (ProbeScreen)main.ActiveScreen;
			var other = (ProbeScreen)await main.ScreenHost.OpenScreenAsync(otherBlock, otherBlock.ScreenDefinitions.Single());
			var firstCopies = first.ModelCopies;
			var otherCopies = other.ModelCopies;
			var uiThread = Environment.CurrentManagedThreadId;

			// Publish from a worker, exercising native dispatch instead of a test-only direct refresh.
			((ProbeScreen.Preferences)firstBlock.BlockSettings).Name = "Block after";
			await Task.Run(firstBlock.BlockSettings.Save);
			await WaitUntilAsync(() => first.ModelCopies > firstCopies);
			Assert.That(first.BlockName, Is.EqualTo("Block after"));
			Assert.That(first.LastCopyThread, Is.EqualTo(uiThread));
			Assert.That(other.ModelCopies, Is.EqualTo(otherCopies));

			// A queued or later notification cannot revive a disposed screen.
			Assert.That(await main.ScreenHost.CloseScreenAsync(first), Is.True);
			var closedCopies = first.ModelCopies;
			await firstBlock.BlockSettings.SaveAsync();
			await Task.Yield();
			Assert.That(first.ModelCopies, Is.EqualTo(closedCopies));
		});
	}

	// A worker save during handle recreation must defer presentation until the UI creates the replacement handle.
	[Test]
	public void BackgroundSaveDuringHandleRecreationNeverRefreshesTheScreenOnTheWorker() {
		RunWithMessageLoop(async owner => {
			using var block = CreateBlock("Handle recreation");
			await using var services = CreateApplication(new MemoryStore(), block);
			var application = services.GetRequiredService<FormsUI.IApplication>();
			await application.InitializeAsync();
			var main = (QuietMainForm)services.GetRequiredService<IMainForm>();
			main.Show(owner);
			var screen = (ProbeScreen)main.ActiveScreen;
			var copies = screen.ModelCopies;
			var uiThread = Environment.CurrentManagedThreadId;
			await screen.RecreateNativeHandleAsync(async () => {
				Assert.That(screen.IsHandleCreated, Is.False);
				screen.ScreenSettings.Name = "Saved during recreation";
				// Save after destruction returns, leaving the UI free to dispatch the worker notification.
				await Task.Run(screen.ScreenSettings.Save).WaitAsync(TimeSpan.FromSeconds(5));
				Assert.That(screen.ModelCopies, Is.EqualTo(copies));
			});
			await WaitUntilAsync(() => screen.DisplayedName == "Saved during recreation");
			Assert.That(screen.LastCopyThread, Is.EqualTo(uiThread));
		});
	}

	/// <summary>Options identifies the application through its configured product information rather than its CLR type.</summary>
	[TestCase("System Expert", "System Expert")]
	[TestCase(null, "Application")]
	[TestCase("", "Application")]
	[TestCase("  ", "Application")]
	public void OptionsRootUsesTheConfiguredProductName(string productName, string expectedCaption) {
		RunWithMessageLoop(async owner => {
			using var block = CreateBlock("Diagnostics");
			await using var services = CreateApplication(new MemoryStore(), block);
			var application = services.GetRequiredService<FormsUI.IApplication>();
			var productServices = new ServiceCollection();
			if (productName != null)
				productServices.AddSingleton<IProductInformationProvider>(new CaptionProductInformationProvider(productName));
			await using var provider = productServices.BuildServiceProvider();
			using var dialog = new ApplicationSettingsDialog(application, provider);
			dialog.Show(owner);

			Assert.That(Find<TreeView>(dialog).Nodes[0].Text, Is.EqualTo(expectedCaption));
			Assert.That(Find<TreeView>(dialog).Nodes[0].Text, Is.Not.EqualTo(application.GetType().Name));
		});
	}

	// Menu and programmatic openings follow the visible screen, independently of browsed navigation.
	[TestCase(false)]
	[TestCase(true)]
	public void OptionsInitiallySelectTheActiveScreensSettings(bool openFromMenu) {
		RunWithMessageLoop(async owner => {
			using var firstBlock = CreateBlock("First");
			using var activeBlock = CreateBlock("Active");
			await using var services = CreateApplication(new MemoryStore(), firstBlock, activeBlock);
			var application = services.GetRequiredService<FormsUI.IApplication>();
			await application.InitializeAsync();
			var main = (QuietMainForm)services.GetRequiredService<IMainForm>();
			main.Show(owner);
			var definition = activeBlock.ScreenDefinitions.Single();
			var screen = await main.ScreenHost.OpenScreenAsync(activeBlock, definition);
			main.ScreenHost.SelectBlock(firstBlock);
			Assert.That(main.ActiveScreen, Is.SameAs(screen));
			Assert.That(main.SelectedBlock, Is.SameAs(firstBlock));

			// Exercise the actual modal dialog from both supported opening paths.
			Task<DialogResult> opening = null;
			if (openFromMenu)
				Find<MenuStrip>(main).Items.OfType<ToolStripMenuItem>().SelectMany(item => item.DropDownItems.OfType<ToolStripMenuItem>())
					.Single(item => item.Name == "_screenOptionsMenuItem").PerformClick();
			else
				opening = main.ShowScreenOptionsAsync();
			await WaitUntilAsync(() => System.Windows.Forms.Application.OpenForms.OfType<ApplicationSettingsDialog>().Any());
			var dialog = System.Windows.Forms.Application.OpenForms.OfType<ApplicationSettingsDialog>().Single();
			try {
				Assert.That(dialog.SelectedOwner, Is.SameAs(definition));
				Assert.That(Find<DefaultCrudEntityEditor>(dialog).GetEntityWithChanges(), Is.SameAs(screen.ScreenSettings));
			} finally {
				dialog.DialogResult = DialogResult.Cancel;
				dialog.Close();
			}
			if (opening != null)
				Assert.That(await opening, Is.EqualTo(DialogResult.Cancel));
		});
	}

	// Explicit owners remain authoritative, and screens without preferences retain the ordinary fallback.
	[Test]
	public void OptionsKeepExplicitSelectionAndTheNoSettingsFallback() {
		RunWithMessageLoop(async owner => {
			using var firstBlock = CreateBlock("First");
			using var activeBlock = CreateBlock("Active");
			var plain = new FormsUI.ApplicationScreenDefinition(typeof(PlainScreen), "Plain");
			activeBlock.AddScreenDefinition(plain);
			await using var services = CreateApplication(new MemoryStore(), firstBlock, activeBlock);
			var application = services.GetRequiredService<FormsUI.IApplication>();
			await application.InitializeAsync();
			var main = (QuietMainForm)services.GetRequiredService<IMainForm>();
			main.Show(owner);
			await main.ScreenHost.OpenScreenAsync(activeBlock, activeBlock.ScreenDefinitions.First());
			using (var explicitSelection = new ApplicationSettingsDialog(application, selectedOwner: firstBlock.ScreenDefinitions.Single())) {
				explicitSelection.Show(owner);
				Assert.That(explicitSelection.SelectedOwner, Is.SameAs(firstBlock.ScreenDefinitions.Single()));
			}

			await main.ScreenHost.OpenScreenAsync(activeBlock, plain);
			using var fallback = new ApplicationSettingsDialog(application);
			fallback.Show(owner);
			Assert.That(fallback.SelectedOwner, Is.SameAs(firstBlock.ScreenDefinitions.Single()));
		});
	}

	// Options lists actual settings-bearing registrations without activating their operational screens.
	[TestCase(false)]
	[TestCase(true)]
	public void OptionsHideScreensWithoutSettingsAndKeepUnopenedAndUnmarkedSettings(bool selectScreenWithoutSettings) {
		RunWithMessageLoop(async owner => {
			using var block = new ApplicationBlock { Name = "Filtered Options" };
			var plain = new FormsUI.ApplicationScreenDefinition(typeof(PlainScreen), "No settings", stableKey: "plain");
			var unopened = new FormsUI.ApplicationScreenDefinition(typeof(UnopenedSettingsScreen), "Unopened settings");
			var unmarked = new FormsUI.ApplicationScreenDefinition(typeof(PlainScreen), "Stored settings", stableKey: "stored", settings: new UnmarkedPreferences());
			block.AddScreenDefinition(plain);
			block.AddScreenDefinition(unopened);
			block.AddScreenDefinition(unmarked);
			block.DefaultScreenDefinition = plain;
			var store = new MemoryStore();
			await using var services = CreateApplication(store, block);
			var application = services.GetRequiredService<FormsUI.IApplication>();
			await application.InitializeAsync();
			using var dialog = new ApplicationSettingsDialog(application, selectedOwner: selectScreenWithoutSettings ? plain : null);
			dialog.Show(owner);

			var screenNodes = Find<TreeView>(dialog).Nodes[0].Nodes[0].Nodes.Cast<TreeNode>().ToArray();
			Assert.That(screenNodes.Select(node => node.Tag), Is.EqualTo(new[] { unopened, unmarked }));
			Assert.That(dialog.SelectedOwner, Is.SameAs(unopened));
			Assert.That(Find<DefaultCrudEntityEditor>(dialog).GetEntityWithChanges(), Is.SameAs(unopened.Settings));
			Assert.That(unopened.Settings, Is.TypeOf<UnopenedSettingsScreen.Preferences>());
			Assert.That(store.Writes, Is.Zero, "Opening Options must not save or activate an unopened settings screen.");
		});
	}

	// Apply saves the actual object and Cancel restores only subsequent dialog-local edits.
	[Test]
	public void OptionsEditActualObjectsAndCancelKeepsAnEarlierSuccessfulApply() {
		RunWithMessageLoop(async owner => {
			var store = new MemoryStore();
			using var block = CreateBlock("Options");
			await using var services = CreateApplication(store, block);
			var application = services.GetRequiredService<FormsUI.IApplication>();
			await application.InitializeAsync();
			var definition = block.ScreenDefinitions.Single();
			var actual = (ProbeScreen.Preferences)definition.Settings;
			var notifications = 0;
			actual.OnChanged += () => notifications++;
			using var dialog = new ApplicationSettingsDialog(application, selectedOwner: definition);
			dialog.Show(owner);
			Assert.That(Find<DefaultCrudEntityEditor>(dialog).GetEntityWithChanges(), Is.SameAs(actual));

			// Provisional edits affect this actual object, without persisting or publishing yet.
			SetName(dialog, "Accepted");
			Assert.That(actual.Name, Is.EqualTo("Accepted"));
			Assert.That(store.Writes, Is.Zero);
			Assert.That(notifications, Is.Zero);
			Assert.That(await dialog.ApplySelectedAsync(), Is.True);
			SetName(dialog, "Discarded");
			dialog.CancelEdits();
			dialog.Close();

			Assert.That(actual.Name, Is.EqualTo("Accepted"));
			Assert.That(definition.Settings, Is.SameAs(actual));
			Assert.That(store.Writes, Is.EqualTo(1));
			Assert.That(notifications, Is.EqualTo(1));
		});
	}

	// Repeated local restores replace nested values and getter-only list membership without appending duplicates.
	[TestCase(false)]
	[TestCase(true)]
	public void CancelAndResetRestoreNestedCollectionsWithoutReplacingTheActualRoot(bool guidIdentity) {
		RunWithMessageLoop(async owner => {
			using var block = CreateBlock("Collections");
			object id = guidIdentity ? Guid.NewGuid() : 7;
			block.ScreenDefinitions.Single().Settings.ID = id;
			await using var services = CreateApplication(new MemoryStore(), block);
			var application = services.GetRequiredService<FormsUI.IApplication>();
			await application.InitializeAsync();
			var definition = block.ScreenDefinitions.Single();
			var actual = (ProbeScreen.Preferences)definition.Settings;
			for (var attempt = 0; attempt < 2; attempt++) {
				using var dialog = new ApplicationSettingsDialog(application, selectedOwner: definition);
				dialog.Show(owner);
				SetName(dialog, "Temporary");
				actual.Items.Add("Temporary");
				actual.Nested.Values.Add("Temporary");
				dialog.CancelEdits();
				dialog.Close();
				Assert.That(actual.ID, Is.SameAs(id));
				Assert.That(actual.Items, Is.EqualTo(new[] { "Initial" }));
				Assert.That(actual.Nested.Values, Is.EqualTo(new[] { "Nested" }));
			}

			// Reset uses declared attributes and the settings object's existing nested-default override.
			using var reset = new ApplicationSettingsDialog(application, selectedOwner: definition);
			reset.Show(owner);
			SetName(reset, "Temporary");
			actual.Grid.PageSize = 37;
			reset.ResetSelectedDefaults();
			Assert.That(actual.Name, Is.EqualTo("Default"));
			Assert.That(actual.Grid.PageSize, Is.EqualTo(12));
			Assert.That(actual.Items, Is.EqualTo(new[] { "Initial" }));
			Assert.That(definition.Settings, Is.SameAs(actual));
			reset.CancelEdits();
			reset.Close();
		});
	}

	// Failed validation or storage does not notify consumers; the same actual object remains editable and retryable.
	[Test]
	public void InvalidAndFailedSavesRemainRetryableWithoutPublishingNotifications() {
		RunWithMessageLoop(async owner => {
			var store = new MemoryStore();
			using var block = CreateBlock("Validation");
			await using var services = CreateApplication(store, block);
			var application = services.GetRequiredService<FormsUI.IApplication>();
			await application.InitializeAsync();
			var actual = block.ScreenDefinitions.Single().Settings;
			var notifications = 0;
			actual.OnChanged += () => notifications++;
			using var dialog = new ApplicationSettingsDialog(application, selectedOwner: actual);
			dialog.Show(owner);
			SetName(dialog, string.Empty);
			Assert.That(await dialog.ApplySelectedAsync(), Is.False);
			SetName(dialog, "Retry");
			store.FailSave = true;
			Assert.That(await dialog.ApplySelectedAsync(), Is.False);
			Assert.That(store.Writes, Is.Zero);
			Assert.That(notifications, Is.Zero);
			store.FailSave = false;
			Assert.That(await dialog.ApplySelectedAsync(), Is.True);
			Assert.That(store.Writes, Is.EqualTo(1));
			Assert.That(notifications, Is.EqualTo(1));
			dialog.Close();
		});
	}

	// Grid and Options read the same nested actual settings value after a successful save.
	[Test]
	public void SavedSettingsImmediatelyUpdateTheBoundGridThroughOnChanged() {
		RunWithMessageLoop(async owner => {
			using var block = CreateBlock("Grid");
			await using var services = CreateApplication(new MemoryStore(), block);
			var application = services.GetRequiredService<FormsUI.IApplication>();
			await application.InitializeAsync();
			var main = (QuietMainForm)services.GetRequiredService<IMainForm>();
			main.Show(owner);
			var screen = (ProbeScreen)main.ActiveScreen;
			await screen.BindGridAsync();
			var actual = screen.ScreenSettings;
			actual.Grid.PageSize = 7;
			await actual.SaveAsync();
			await WaitUntilAsync(() => screen.Grid.VisibleEntities.Count() == 7);

			Assert.That(screen.Grid.Preferences.PageSize, Is.EqualTo(actual.Grid.PageSize));
		});
	}

	// Saving a screen scalar must neither accept nor reset the grid's temporary presentation.
	[Test]
	public void OrdinaryGridChangesRemainTemporaryAcrossScreenSavesAndClose() {
		RunWithMessageLoop(async owner => {
			var store = new MemoryStore();
			using var block = CreateBlock("Temporary grid");
			await using var services = CreateApplication(store, block);
			var application = services.GetRequiredService<FormsUI.IApplication>();
			await application.InitializeAsync();
			var main = (QuietMainForm)services.GetRequiredService<IMainForm>();
			main.Show(owner);
			var screen = (ProbeScreen)main.ActiveScreen;
			await screen.BindGridAsync();
			var actual = screen.ScreenSettings;
			var temporary = screen.Grid.CaptureGridPreferences();
			temporary.PageMode = VisualGridPageMode.Single;
			temporary.Columns[0].Width = 243;
			screen.Grid.ApplyGridPreferences(temporary);
			await screen.Grid.RefreshGrid();

			screen.EditName("Accepted scalar");
			await screen.SaveSettingsAsync();
			Assert.That(store.Writes, Is.EqualTo(1));
			Assert.That(actual.Grid.PageMode, Is.EqualTo(VisualGridPageMode.Paged));
			Assert.That(actual.Grid.Columns[0].Width, Is.Null);
			Assert.That(screen.Grid.PageMode, Is.EqualTo(VisualGridPageMode.Single));
			Assert.That(screen.Grid.Preferences.Columns[0].Width, Is.EqualTo(243));

			Assert.That(await main.ScreenHost.CloseScreenAsync(screen), Is.True);
			Assert.That(store.Writes, Is.EqualTo(1), "Closing does not accept temporary grid changes.");
			var restored = (ProbeScreen.Preferences)await store.LoadSettingAsync(typeof(ProbeScreen.Preferences), actual.ID);
			Assert.That(restored.Name, Is.EqualTo("Accepted scalar"));
			Assert.That(restored.Grid.PageMode, Is.EqualTo(VisualGridPageMode.Paged));
			Assert.That(restored.Grid.Columns[0].Width, Is.Null);
		});
	}

	// The native menu awaits storage while later user operations remain outside the accepted snapshot.
	[Test]
	public void ExplicitGridSavePreservesNewerTemporaryChangesDuringStorage() {
		RunWithMessageLoop(async owner => {
			var store = new PendingStore();
			using var block = CreateBlock("Pending grid save");
			await using var services = CreateApplication(store, block);
			var application = services.GetRequiredService<FormsUI.IApplication>();
			await application.InitializeAsync();
			var main = (QuietMainForm)services.GetRequiredService<IMainForm>();
			main.Show(owner);
			var screen = (ProbeScreen)main.ActiveScreen;
			await screen.BindGridAsync();
			screen.Grid.PageMode = VisualGridPageMode.Single;
			using var menu = new VisualGridConfigurationContextMenu();
			menu.Show(screen.Grid, Point.Empty);
			((ToolStripMenuItem)menu.Items[0]).PerformClick();
			menu.Close();
			await store.FirstWriteStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

			// An operation after Save was selected is still only a live view choice.
			screen.Grid.PageMode = VisualGridPageMode.Auto;
			store.ReleaseFirstWrite.TrySetResult();
			await WaitUntilAsync(() => store.SavedNames.Length == 1);
			Assert.That(screen.ScreenSettings.Grid.PageMode, Is.EqualTo(VisualGridPageMode.Single));
			Assert.That(screen.Grid.PageMode, Is.EqualTo(VisualGridPageMode.Auto));
			await screen.SaveSettingsAsync();
			Assert.That(store.SavedNames, Has.Length.EqualTo(1));
		});
	}

	// An explicit failed write does not publish accepted changes, and the same command can be retried.
	[Test]
	public void ExplicitGridSaveFailureCanBeRetriedWithoutAcceptingIncidentalChanges() {
		RunWithMessageLoop(async owner => {
			var store = new MemoryStore();
			using var block = CreateBlock("Rejected grid save");
			await using var services = CreateApplication(store, block);
			var application = services.GetRequiredService<FormsUI.IApplication>();
			await application.InitializeAsync();
			var main = (QuietMainForm)services.GetRequiredService<IMainForm>();
			main.Show(owner);
			var screen = (ProbeScreen)main.ActiveScreen;
			await screen.BindGridAsync();
			var notifications = 0;
			screen.ScreenSettings.OnChanged += () => notifications++;
			screen.Grid.PageMode = VisualGridPageMode.Single;
			store.FailSave = true;
			await Assert.ThatAsync(() => SaveGridConfigurationAsync(screen), Throws.TypeOf<IOException>());
			Assert.That(store.Writes, Is.Zero);
			Assert.That(notifications, Is.Zero);

			screen.Grid.PageMode = VisualGridPageMode.Auto;
			Assert.That(screen.ScreenSettings.Grid.PageMode, Is.EqualTo(VisualGridPageMode.Single), "Only the explicit selection was captured.");
			store.FailSave = false;
			screen.EditName("Retry pending accepted values");
			await screen.SaveSettingsAsync();
			Assert.That(store.Writes, Is.EqualTo(1));
			Assert.That(notifications, Is.EqualTo(1));
			Assert.That(screen.ScreenSettings.Grid.PageMode, Is.EqualTo(VisualGridPageMode.Single));
			Assert.That(screen.Grid.PageMode, Is.EqualTo(VisualGridPageMode.Auto), "Retrying the pending write does not reset later temporary choices.");

			await SaveGridConfigurationAsync(screen);
			Assert.That(store.Writes, Is.EqualTo(2));
			Assert.That(notifications, Is.EqualTo(2));
			Assert.That(screen.ScreenSettings.Grid.PageMode, Is.EqualTo(VisualGridPageMode.Auto));
		});
	}

	// A failed accepted-close write leaves its screen available for an explicit retry.
	[Test]
	public void AcceptedCloseRetainsItsScreenWhenSettingsCannotBeSaved() {
		RunWithMessageLoop(async owner => {
			var store = new MemoryStore();
			using var block = CreateBlock("Close");
			await using var services = CreateApplication(store, block);
			var application = services.GetRequiredService<FormsUI.IApplication>();
			await application.InitializeAsync();
			var main = (QuietMainForm)services.GetRequiredService<IMainForm>();
			main.Show(owner);
			var screen = (ProbeScreen)main.ActiveScreen;
			screen.EditName("Save on close");
			store.FailSave = true;
			await Assert.ThatAsync(async () => await main.ScreenHost.CloseScreenAsync(screen), Throws.TypeOf<IOException>());
			Assert.That(main.ScreenHost.Screens, Does.Contain(screen));
			Assert.That(screen.IsDisposed, Is.False);
			store.FailSave = false;
			Assert.That(await main.ScreenHost.CloseScreenAsync(screen), Is.True);
			Assert.That(store.Writes, Is.EqualTo(1));
		});
	}

	// A write acknowledgement must not consume a newer edit made while that write was pending.
	[Test]
	public void ASecondEditDuringAPendingSaveRemainsDirtyForTheNextSave() {
		RunWithMessageLoop(async owner => {
			var store = new PendingStore();
			using var block = CreateBlock("Pending save");
			await using var services = CreateApplication(store, block);
			var application = services.GetRequiredService<FormsUI.IApplication>();
			await application.InitializeAsync();
			var main = (QuietMainForm)services.GetRequiredService<IMainForm>();
			main.Show(owner);
			var screen = (ProbeScreen)main.ActiveScreen;

			// The provider has captured the first value but has not acknowledged it yet.
			screen.EditName("First edit");
			var saving = screen.SaveSettingsAsync();
			await store.FirstWriteStarted.Task.WaitAsync(TimeSpan.FromSeconds(3));
			screen.EditName("Second edit");
			store.ReleaseFirstWrite.TrySetResult();
			await saving;
			await screen.SaveSettingsAsync();

			Assert.That(store.SavedNames, Is.EqualTo(new[] { "First edit", "Second edit" }));
			Assert.That(screen.ScreenSettings.Name, Is.EqualTo("Second edit"));
		});
	}

	/// <summary>Rejected optional reads restore configured defaults on the same owner without writing a replacement record.</summary>
	[TestCase("invalid")]
	[TestCase("annotation")]
	[TestCase("partial")]
	[TestCase("cancelled")]
	public void InvalidOrInterruptedReadsRetainTheActualConfiguredSettings(string failure) {
		RunWithMessageLoop(async owner => {
			using var cancellation = new CancellationTokenSource();
			var storage = new RejectedLoadStore(failure, cancellation);
			var provider = new CachedSettingsProvider(storage);
			var settings = new AnnotatedPreferences { Name = "Configured default" };
			settings.Nested.Values = new List<string> { "Configured nested" };
			using var block = new ApplicationBlock { Name = "Rejected load" };
			var definition = new FormsUI.ApplicationScreenDefinition(typeof(ProbeScreen), "Probe", stableKey: "probe", settings: settings);
			block.AddScreenDefinition(definition);
			block.DefaultScreenDefinition = definition;
			var id = Tools.UI.GetScreenSettingsId("NativeActualSettings", block, definition);
			await using var services = CreateApplication(provider, block);
			var application = services.GetRequiredService<FormsUI.IApplication>();

			if (failure == "cancelled")
				await Assert.ThatAsync(() => application.InitializeAsync(cancellation.Token), Throws.InstanceOf<OperationCanceledException>());
			else
				await application.InitializeAsync(cancellation.Token);

			Assert.That(definition.Settings, Is.SameAs(settings));
			Assert.That(settings.Name, Is.EqualTo("Configured default"));
			Assert.That(settings.Interval, Is.EqualTo(12));
			Assert.That(settings.Items, Is.EqualTo(new[] { "Initial" }));
			Assert.That(settings.Nested.Values, Is.EqualTo(new[] { "Configured nested" }));
			Assert.That(settings.ID, Is.EqualTo(id));
			Assert.That(settings.Provider, Is.SameAs(provider));
			Assert.That(storage.Writes, Is.Zero);
		});
	}

	// Screens declare their settings statically; Options must not activate operational controls.
	[Test]
	public void SettingsClassEditorReceivesTheActualOwnerAndRegistrationEditorsTakePrecedence() {
		RunWithMessageLoop(async owner => {
			using var block = new ApplicationBlock { Name = "Editors" };
			var actual = new AttributedPreferences();
			var attributed = new FormsUI.ApplicationScreenDefinition(typeof(PlainScreen), "Attributed", stableKey: "attributed", settings: actual);
			var explicitEditor = new FormsUI.ApplicationScreenDefinition(typeof(PlainScreen), "Explicit", stableKey: "explicit",
				settings: new AttributedPreferences(), settingsEditor: _ => new DefaultCrudEntityEditor());
			block.AddScreenDefinition(attributed);
			block.AddScreenDefinition(explicitEditor);
			await using var services = CreateApplication(new MemoryStore(), block);
			var application = services.GetRequiredService<FormsUI.IApplication>();
			await application.InitializeAsync();
			var main = (QuietMainForm)services.GetRequiredService<IMainForm>();
			var count = main.ScreenHost.Screens.Length;
			using var dialog = new ApplicationSettingsDialog(application, selectedOwner: attributed);
			dialog.Show(owner);
			Assert.That(Find<DeclaredEditor>(dialog).Context.Settings, Is.SameAs(actual));
			dialog.SelectOwner(explicitEditor);
			Assert.That(Find<DefaultCrudEntityEditor>(dialog), Is.TypeOf<DefaultCrudEntityEditor>());
			Assert.That(main.ScreenHost.Screens.Length, Is.EqualTo(count));
			dialog.Close();
		});
	}

	// Await the production save operation without presenting an error dialog in the failure test.
	private static Task SaveGridConfigurationAsync(ProbeScreen screen) {
		return (Task)typeof(ApplicationScreen).GetMethod("SaveGridConfigurationAsync", BindingFlags.Instance | BindingFlags.NonPublic)
			.Invoke(screen, new object[] { screen.Grid, CancellationToken.None });
	}

	// Keep application startup composition inside the existing DI path.
	private static ServiceProvider CreateApplication(ISettingsProvider store, params FormsUI.IApplicationBlock[] blocks) {
		var services = new ServiceCollection();
		services.AddSingleton(new Local<ISettingsProvider>(store));
		services.AddMainForm<QuietMainForm>();
		foreach (var block in blocks)
			services.AddApplicationBlock(block);
		services.AddApplicationSettings("NativeActualSettings");
		return services.BuildServiceProvider();
	}

	// Declare a real typed default registration without creating its screen.
	private static ApplicationBlock CreateBlock(string name) {
		var block = new ApplicationBlock { Name = name };
		var definition = new FormsUI.ApplicationScreenDefinition(typeof(ProbeScreen), "Probe", stableKey: "probe");
		block.AddScreenDefinition(definition);
		block.DefaultScreenDefinition = definition;
		return block;
	}

	// Use the real property descriptor path to retain normal editor change detection.
	private static void SetName(ApplicationSettingsDialog dialog, string value) {
		var descriptor = Find<PropertyGrid>(dialog).SelectedObject;
		TypeDescriptor.GetProperties(descriptor)[nameof(ProbeScreen.Preferences.Name)].SetValue(descriptor, value);
	}

	// Find the actual native editor control rather than constructing a substitute.
	private static T Find<T>(Control owner)
		where T : Control {
		return owner.Controls.Cast<Control>().SelectMany(Descendants).OfType<T>().Single();
	}

	// Traverse the owned native control tree for fixture assertions.
	private static IEnumerable<Control> Descendants(Control owner) {
		yield return owner;
		foreach (Control child in owner.Controls)
			foreach (var descendant in Descendants(child))
				yield return descendant;
	}

	// Await actual message-loop progress with a bounded failure instead of blocking the UI thread.
	private static async Task WaitUntilAsync(Func<bool> condition) {
		var deadline = DateTime.UtcNow.AddSeconds(5);
		while (!condition()) {
			Assert.That(DateTime.UtcNow, Is.LessThan(deadline), "Native settings did not finish updating.");
			await Task.Delay(10);
		}
	}

	// Own a real native pump while asynchronous settings operations return to its context.
	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var previous = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous));
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000) };
		using var watchdog = new System.Windows.Forms.Timer { Interval = 30000 };
		watchdog.Tick += (_, _) => {
			failure = new AssertionException("Native settings test timed out.");
			owner.Close();
		};
		owner.Shown += async (_, _) => {
			try {
				await test(owner);
			} catch (Exception error) {
				failure = error;
			} finally {
				owner.Close();
			}
		};
		watchdog.Start();
		System.Windows.Forms.Application.Run(owner);
		if (failure != null)
			ExceptionDispatchInfo.Capture(failure).Throw();
	}

	#region Inner Types

	/// <summary>Supplies the product identity selected by application composition.</summary>
	private sealed class CaptionProductInformationProvider : IProductInformationProvider {
		/// <summary>Retains the configured product name for the Options heading.</summary>
		public CaptionProductInformationProvider(string productName) {
			ProductInformation = new ProductInformation { ProductName = productName };
		}

		/// <inheritdoc />
		public ProductInformation ProductInformation { get; }
	}

	// Omit product startup dialogs while retaining the actual native screen host.
	public class QuietMainForm : BlockMainForm {
		// Skip product startup dialogs while retaining the native host used by this fixture.
		protected override void OnLoad(EventArgs e) {
		}
	}

	// Capture the real settings and model-to-UI lifecycle observed by hosted screens.
	public class ProbeScreen : ApplicationScreen {
		public string FirstName { get; private set; }

		public string BlockName { get; private set; }

		public string DisplayedName { get; private set; }

		public int ModelCopies { get; private set; }

		public int LastCopyThread { get; private set; }

		public VisualGrid Grid { get; private set; }

		public override Preferences ScreenSettings {
			get {
				return (Preferences)base.ScreenSettings;
			}
		}

		// Capture the value available before the first real data query.
		protected override void InitializeUIPrimingData() {
			FirstName = ScreenSettings?.Name;
			base.InitializeUIPrimingData();
		}

		// Observe actual owner values through the existing refresh lifecycle.
		protected override void CopyModelToUI() {
			base.CopyModelToUI();
			ModelCopies++;
			DisplayedName = ScreenSettings?.Name;
			LastCopyThread = Environment.CurrentManagedThreadId;
			if (Block?.BlockSettings is Preferences)
				BlockName = GetBlockSettings<Preferences>().Name;
		}

		// Release the native destruction call before awaiting a worker, then restore the handle on the UI thread.
		public async Task RecreateNativeHandleAsync(Func<Task> whileHandleAbsent) {
			DestroyHandle();
			try {
				await whileHandleAbsent();
			} finally {
				CreateHandle();
			}
		}

		// Bind an actual visual grid using the screen's ordinary settings property.
		public async Task BindGridAsync() {
			Grid = new VisualGrid {
				Dock = DockStyle.Fill,
				GridBindings = new[] { new VisualGridColumn<string> { ColumnName = "Record", PreferenceKey = "record", DataType = typeof(string), PropertyValue = value => value } }
			};
			Controls.Add(Grid);
			await BindGridPreferencesAsync(Grid, nameof(Preferences.Grid));
			await Grid.SetDataSource(new ListDataSource<string>(new ExtendedList<string>(Enumerable.Range(0, 40).Select(value => value.ToString()).ToArray())),
				DataSourceCapabilities.CanRead | DataSourceCapabilities.CanPage);
		}

		// Deliberate screen edits use the existing accepted-close persistence boundary.
		public void EditName(string value) {
			ScreenSettings.Name = value;
			MarkSettingsChanged();
		}

		#region Inner Types

		/// <summary>Actual screen-owned values used by the native lifecycle and Options tests.</summary>
		[UserEditable]
		public class Preferences : SettingsObject {
			[DefaultValue("Default")]
			public string Name { get; set; } = "Default";

			public List<string> Items { get; } = new() { "Initial" };

			public NestedValues Nested { get; set; } = new();

			public VisualGridSettings Grid { get; set; } = new() { PageMode = VisualGridPageMode.Paged, PageSize = 12 };

			// Reset nested grid defaults through the same existing settings lifecycle as scalar attributes.
			public override void RestoreDefaultValues() {
				base.RestoreDefaultValues();
				if (Grid != null)
					Grid.PageSize = 12;
			}

			// Reject empty names through the settings object's existing validation contract.
			public override Result Validate() {
				return string.IsNullOrWhiteSpace(Name) ? Result.Error("A name is required.") : Result.Success;
			}
		}

		/// <summary>Nested mutable values used to check repeated local backup restoration.</summary>
		public class NestedValues {
			public List<string> Values { get; set; } = new() { "Nested" };
		}

		#endregion
	}

	// No operational settings initialization is needed to enumerate this registration in Options.
	public class PlainScreen : ApplicationScreen {
	}

	/// <summary>Declares settings by attribute and rejects any attempt to activate the operational screen.</summary>
	[Settings(typeof(UnopenedSettingsScreen.Preferences))]
	public class UnopenedSettingsScreen : ApplicationScreen {
		/// <summary>Options discovery must only read the class declaration.</summary>
		public UnopenedSettingsScreen() {
			throw new InvalidOperationException("Options must not activate an unopened screen.");
		}

		#region Inner Types

		/// <summary>The screen's editable values discovered without constructing its control.</summary>
		[UserEditable]
		public class Preferences : SettingsObject {
			/// <summary>A default value supplied by the existing settings activation lifecycle.</summary>
			[DefaultValue(10)]
			public int Number { get; set; }
		}

		#endregion
	}

	/// <summary>Persisted settings remain visible even without an automatic or custom editor.</summary>
	public class UnmarkedPreferences : SettingsObject {
	}

	/// <summary>Declares a custom class editor through the standard component-model attribute.</summary>
	[Editor(typeof(DeclaredEditor), typeof(ICrudEntityEditor<object>))]
	public class AttributedPreferences : SettingsObject {
		public string Name { get; set; } = "Attributed";
	}

	// Observe the actual object and services supplied to a settings-class editor.
	public sealed class DeclaredEditor : DefaultCrudEntityEditor {
		public DeclaredEditor(ApplicationSettingsEditorContext context) {
			Context = context;
		}

		public ApplicationSettingsEditorContext Context { get; }
	}

	// Isolated storage exercises the actual directional base and normal SettingsObject save lifecycle.
	private sealed class PendingStore : AsyncSettingsProviderBase {
		private readonly List<string> _savedNames = new();

		public TaskCompletionSource FirstWriteStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public TaskCompletionSource ReleaseFirstWrite { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public string[] SavedNames {
			get {
				return _savedNames.ToArray();
			}
		}

		public override bool EncryptSettings {
			get {
				return false;
			}
		}

		public override Task<bool> ContainsSettingAsync(Type settingsObjectType, object id = null, CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();
			return Task.FromResult(false);
		}

		protected override Task<SettingsObject> LoadInternalAsync(Type settingsObjectType, object id, CancellationToken cancellationToken) {
			throw new NotSupportedException("This fixture starts with no stored settings.");
		}

		protected override async Task SaveInternalAsync(SettingsObject settings, CancellationToken cancellationToken) {
			var name = ((ProbeScreen.Preferences)settings).Name;
			if (_savedNames.Count == 0) {
				FirstWriteStarted.TrySetResult();
				await ReleaseFirstWrite.Task.WaitAsync(cancellationToken);
			}
			_savedNames.Add(name);
		}

		public override Task DeleteSettingAsync(SettingsObject settings, CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();
			return Task.CompletedTask;
		}

		public override Task ClearSettingsAsync(CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();
			_savedNames.Clear();
			return Task.CompletedTask;
		}
	}

	/// <summary>Adds an annotation constraint while retaining the screen's declared base settings schema.</summary>
	public sealed class AnnotatedPreferences : ProbeScreen.Preferences {
		[DefaultValue(12), System.ComponentModel.DataAnnotations.Range(1, 60)]
		public int Interval { get; set; }
	}

	/// <summary>Mutates the supplied owner exactly as a partially read, invalid or cancelled backing store can.</summary>
	private sealed class RejectedLoadStore(string failure, CancellationTokenSource cancellation) : AsyncSettingsProviderBase {
		public int Writes { get; private set; }

		public override bool EncryptSettings {
			get {
				return false;
			}
		}

		public override Task<bool> ContainsSettingAsync(Type settingsObjectType, object id = null, CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();
			return Task.FromResult(true);
		}

		protected override Task<SettingsObject> LoadInternalAsync(Type settingsObjectType, object id, CancellationToken cancellationToken) {
			throw new NotSupportedException("This fixture deliberately populates the actual owner.");
		}

		protected override Task<SettingsObject> LoadInternalAsync(SettingsObject defaults, CancellationToken cancellationToken) {
			var payload = failure switch {
				"invalid" => "{\"Name\":\"\",\"Items\":[\"Changed\"],\"id\":\"Changed\"}",
				"annotation" => "{\"Name\":\"Changed\",\"Interval\":0,\"Items\":[\"Changed\"]}",
				"partial" => "{\"Name\":\"Changed\",\"Items\":[\"Changed\"],\"Nested\":{\"Values\":[\"Changed\"]},\"Interval\":\"invalid\"}",
				_ => "{\"Name\":\"Changed\",\"Items\":[\"Changed\"]}"
			};
			using var reader = new JsonTextReader(new StringReader(payload));
			new SettingsObjectJsonSerializer().Populate(reader, defaults);
			if (failure == "cancelled")
				cancellation.Cancel();
			cancellationToken.ThrowIfCancellationRequested();
			return Task.FromResult(defaults);
		}

		protected override Task SaveInternalAsync(SettingsObject settings, CancellationToken cancellationToken) {
			cancellationToken.ThrowIfCancellationRequested();
			Writes++;
			return Task.CompletedTask;
		}

		public override Task DeleteSettingAsync(SettingsObject settings, CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();
			return Task.CompletedTask;
		}

		public override Task ClearSettingsAsync(CancellationToken cancellationToken = default) {
			cancellationToken.ThrowIfCancellationRequested();
			return Task.CompletedTask;
		}
	}

	// Isolated storage exercises the actual directional base and normal SettingsObject save lifecycle.
	private sealed class MemoryStore : SyncSettingsProviderBase {
		private readonly record struct SettingKey(Type Type, object Id);

		private readonly ConcurrentDictionary<SettingKey, string> _records = new();
		private int _writes;

		public bool FailSave { get; set; }

		public int Writes {
			get {
				return _writes;
			}
		}

		// Test persistence through the same type and identity used by the actual setting.
		public override bool ContainsSetting(Type settingsObjectType, object id = null) {
			return _records.ContainsKey(new SettingKey(settingsObjectType, id));
		}

		// Restore serialized values while the provider base manages context and encryption.
		protected override SettingsObject LoadInternal(Type settingsObjectType, object id = null) {
			using var reader = new JsonTextReader(new StringReader(_records[new SettingKey(settingsObjectType, id)]));
			return (SettingsObject)new SettingsObjectJsonSerializer().Deserialize(reader, settingsObjectType);
		}

		// Record only successful writes so failed saves cannot look accepted.
		protected override void SaveInternal(SettingsObject settings) {
			if (FailSave)
				throw new IOException("Fixture storage rejected the write.");
			using var writer = new StringWriter();
			new SettingsObjectJsonSerializer().Serialize(writer, settings);
			_records[new SettingKey(settings.GetType(), settings.ID)] = writer.ToString();
			Interlocked.Increment(ref _writes);
		}

		// Remove the fixture's real stored value.
		public override void DeleteSetting(SettingsObject settings) {
			_records.TryRemove(new SettingKey(settings.GetType(), settings.ID), out _);
		}

		// Clear the fixture storage without introducing an application-specific registry.
		public override void ClearSettings() {
			_records.Clear();
		}
	}

	#endregion
}
