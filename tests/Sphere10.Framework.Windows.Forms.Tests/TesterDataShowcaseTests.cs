// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Microsoft.Extensions.DependencyInjection;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.ObjectSpaces;
using Sphere10.Framework.Utils.WinFormsTester;
using Sphere10.Framework.Utils.WinFormsTester.Screens;
using FormsUI = global::Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>Exercises real native data workflows, metadata hydration and worker cancellation.</summary>
[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class TesterDataShowcaseTests {
	private bool _startedFramework;

	[OneTimeSetUp]
	public void StartFramework() {
		_startedFramework = !Sphere10Framework.Instance.IsStarted;
		if (_startedFramework)
			Sphere10Framework.Instance.Build().Start();
	}

	[OneTimeTearDown]
	public void StopFramework() {
		if (_startedFramework)
			Sphere10Framework.Instance.EndFramework();
	}

	[Test]
	public void ObjectSpaceSettingsDeclarationPreservesItsLegacyRecordTypeWithoutRegisteringTheCompatibilityBase() {
		Assert.That(ApplicationHelper.GetScreenSettingsType(typeof(ObjectSpaceScreen)), Is.EqualTo(typeof(ObjectSpaceScreen.ObjectSpaceScreenSettings)));
		Assert.That(typeof(ObjectSpaceScreen.ScreenSettings).FullName, Is.EqualTo("Sphere10.Framework.Utils.WinFormsTester.ObjectSpaceScreen+ScreenSettings"));
		Assert.That(typeof(ObjectSpaceScreenBase).IsAbstract, Is.False);
		Assert.That(typeof(ObjectSpaceScreenBase).IsGenericType, Is.False);
		using var block = new ApplicationBlockBuilder()
			.WithName("ObjectSpace")
			.AddScreen<ObjectSpaceScreen>("ObjectSpace")
			.Build();
		Assert.That(block.ScreenDefinitions.Select(definition => definition.ScreenType), Is.EqualTo(new[] { typeof(ObjectSpaceScreen) }));
		Assert.That(block.ScreenDefinitions.Single().Settings, Is.TypeOf<ObjectSpaceScreen.ObjectSpaceScreenSettings>());
	}

	[TestCaseSource(nameof(TypedSettingsScreenTypes))]
	public void TypedScreenSettingsRemainHiddenFromDesignerSerialization(Type screenType) {
		var settingsType = ApplicationHelper.GetScreenSettingsType(screenType);
		var descriptor = TypeDescriptor.GetProperties(screenType)[nameof(ApplicationScreen.ScreenSettings)];
		Assert.That(descriptor, Is.Not.Null);
		Assert.That(typeof(SettingsObject).IsAssignableFrom(descriptor.PropertyType), Is.True);
		Assert.That(descriptor.PropertyType.IsAssignableFrom(settingsType ?? typeof(SettingsObject)), Is.True);
		Assert.That(descriptor.IsBrowsable, Is.False);
		Assert.That(((DesignerSerializationVisibilityAttribute)descriptor.Attributes[typeof(DesignerSerializationVisibilityAttribute)]).Visibility,
			Is.EqualTo(DesignerSerializationVisibility.Hidden));
	}

	[TestCase(typeof(EmailTestScreen), "_smtpServerTextBox", "mail.scalar.example")]
	[TestCase(typeof(ParagraphBuilderScreen), "_formatSentence", false)]
	[TestCase(typeof(UrlIDTestScreen), "_count", 37)]
	[TestCase(typeof(WAMSTestScreen), "_heightControl", 7)]
	public void ScalarPreferencesSurviveAcceptedCloseThroughAFreshProvider(Type screenType, string controlName, object acceptedValue) {
		RunWithMessageLoop(async owner => {
			var directory = Tools.FileSystem.GetTempEmptyDirectory();
			using var cleanup = Tools.Scope.DeleteDirOnDispose(directory);
			UserSettings.TryGetProvider(out var previous);
			using var restore = Tools.Scope.ExecuteOnDispose(() => UserSettings.Provider = previous);
			var files = new LegacyPathProvider(directory);
			UserSettings.Provider = files;
			await using (var services = CreatePreferenceApplication(files, screenType)) {
				var application = services.GetRequiredService<FormsUI.IApplication>();
				await application.InitializeAsync();
				var main = (PreferenceMainForm)services.GetRequiredService<IMainForm>();
				main.Show(owner);
				var screen = main.ActiveScreen;
				var input = screen.Controls.Find(controlName, true).Single();
				var value = ScalarInputProperty(input);
				value.SetValue(input, Convert.ChangeType(acceptedValue, value.PropertyType));
				Assert.That(files.Writes, Is.Zero, "Editing a scalar must wait for accepted persistence.");
				Assert.That(await main.ScreenHost.CloseScreenAsync(screen), Is.True);
				Assert.That(screen.IsDisposed, Is.True);
				Assert.That(files.Writes, Is.EqualTo(1), "A scalar edit must mark its own binding dirty without a grid capture.");
			}

			var fresh = new LegacyPathProvider(directory);
			UserSettings.Provider = fresh;
			await using var restarted = CreatePreferenceApplication(fresh, screenType);
			var restartedApplication = restarted.GetRequiredService<FormsUI.IApplication>();
			await restartedApplication.InitializeAsync();
			var restartedMain = (PreferenceMainForm)restarted.GetRequiredService<IMainForm>();
			restartedMain.Show(owner);
			var reopened = restartedMain.ActiveScreen;
			var restoredInput = reopened.Controls.Find(controlName, true).Single();
			var restoredProperty = ScalarInputProperty(restoredInput);
			Assert.That(restoredProperty.GetValue(restoredInput), Is.EqualTo(Convert.ChangeType(acceptedValue, restoredProperty.PropertyType)));
			Assert.That(await restartedMain.ScreenHost.CloseScreenAsync(reopened), Is.True);
			Assert.That(fresh.Writes, Is.Zero, "Restoration and an unchanged scalar close must not save again.");
		});
	}

	[TestCase(false)]
	[TestCase(true)]
	public void ObservableListSequenceReportsRangeChangesAndFinalMembership(bool includeAccess) {
		RunWithMessageLoop(async owner => {
			using var screen = new ObservableCollectionsTestScreen { Dock = DockStyle.Fill };
			owner.Controls.Add(screen);
			await Task.Yield();
			Find<CheckBox>(screen, "_includeAccess").Checked = includeAccess;

			Find<Button>(screen, "_listTestButton").PerformClick();
			var output = Find<TextBoxEx>(screen, "textBoxEx1").Text;
			Assert.That(output, Does.Contain("Removed 5 at index 1"));
			Assert.That(output, Does.Contain("padding 1, padding 7"));
			Assert.That(output, Does.Contain("After Clear: 0 items."));
			Assert.That(output.Contains("Access:", StringComparison.Ordinal), Is.EqualTo(includeAccess));
			Assert.That(Find<Label>(screen, "showcaseStatus").Text, Does.StartWith("List sequence complete."));

			Find<Button>(screen, "_clearButton").PerformClick();
			Assert.That(Find<TextBoxEx>(screen, "textBoxEx1").Text, Is.Empty);
		});
	}

	[TestCase(false)]
	[TestCase(true)]
	public void ObservableDictionarySequenceReportsRemovedKeysAndFinalMembership(bool includeAccess) {
		RunWithMessageLoop(async owner => {
			using var screen = new ObservableCollectionsTestScreen { Dock = DockStyle.Fill };
			owner.Controls.Add(screen);
			await Task.Yield();
			Find<CheckBox>(screen, "_includeAccess").Checked = includeAccess;

			Find<Button>(screen, "_dictionaryTestButton").PerformClick();
			var output = Find<TextBoxEx>(screen, "textBoxEx1").Text;
			Assert.That(output, Does.Contain("Removed keys: string 1"));
			Assert.That(output, Does.Contain("Remove missing key: False"));
			Assert.That(output, Does.Contain("string 6 = -6"));
			Assert.That(output, Does.Contain("After Clear: 0 items."));
			Assert.That(output.Contains("Access:", StringComparison.Ordinal), Is.EqualTo(includeAccess));
			Assert.That(Find<Label>(screen, "showcaseStatus").Text, Does.StartWith("Dictionary sequence complete."));
		});
	}

	[TestCase(false)]
	[TestCase(true)]
	public void LegacySnapshotPathImportsOnlyWhenAbsentAndBlankOptionsSurviveNewInstancesAndRestart(bool reset) {
		RunWithMessageLoop(async owner => {
			var directory = Tools.FileSystem.GetTempEmptyDirectory();
			using var cleanup = Tools.Scope.DeleteDirOnDispose(directory);
			UserSettings.TryGetProvider(out var previous);
			using var restore = Tools.Scope.ExecuteOnDispose(() => UserSettings.Provider = previous);
			var legacy = new LegacyPathProvider(directory);
			var old = ((ISettingsProvider)legacy).Get<ObjectSpaceScreen.ScreenSettings>();
			old.FilePath = "previous-session.s10space";
			old.Save();
			legacy.Writes = 0;
			UserSettings.Provider = legacy;

			await using (var services = CreatePreferenceApplication(legacy)) {
				var application = services.GetRequiredService<FormsUI.IApplication>();
				await application.InitializeAsync();
				var main = (PreferenceMainForm)services.GetRequiredService<IMainForm>();
				main.Show(owner);
				var block = application.Blocks.Single();
				var definition = block.ScreenDefinitions.Single();
				var first = (ObjectSpaceScreen)main.ActiveScreen;
				await Field<Task>(first, "_preferencesLoading");
				Assert.That(Find<PathSelectorControl>(first, "_objectSpacePathControl").Path, Is.EqualTo(old.FilePath));
				Assert.That(legacy.LegacyReads, Is.EqualTo(1));
				Assert.That(legacy.LegacyReadThread, Is.Not.EqualTo(Environment.CurrentManagedThreadId));
				Assert.That(legacy.Writes, Is.Zero, "Restoration must not write either the old setting or the new root record.");
				using var dialog = new ApplicationSettingsDialog(application, selectedOwner: definition);
				dialog.Show(owner);
				if (reset)
					dialog.ResetSelectedDefaults();
				else
					((ObjectSpaceScreen.ObjectSpaceScreenSettings)definition.Settings).SnapshotPath = string.Empty;
				Assert.That(await dialog.ApplySelectedAsync(), Is.True);
				dialog.Close();
				await WaitUntilAsync(() => Find<PathSelectorControl>(first, "_objectSpacePathControl").Path == string.Empty);
				Assert.That(await main.ScreenHost.CloseScreenAsync(first), Is.True);
				var second = (ObjectSpaceScreen)await main.ScreenHost.OpenScreenAsync(block, definition);
				await Field<Task>(second, "_preferencesLoading");
				Assert.That(Find<PathSelectorControl>(second, "_objectSpacePathControl").Path, Is.Empty);
				Assert.That(legacy.LegacyReads, Is.EqualTo(1), "An intentionally blank accepted section must not consult its retired record.");
			}

			var fresh = new LegacyPathProvider(directory);
			UserSettings.Provider = fresh;
			await using var restarted = CreatePreferenceApplication(fresh);
			var restartedApplication = restarted.GetRequiredService<FormsUI.IApplication>();
			await restartedApplication.InitializeAsync();
			var restartedMain = (PreferenceMainForm)restarted.GetRequiredService<IMainForm>();
			restartedMain.Show(owner);
			var reopened = (ObjectSpaceScreen)restartedMain.ActiveScreen;
			await Field<Task>(reopened, "_preferencesLoading");
			Assert.That(Find<PathSelectorControl>(reopened, "_objectSpacePathControl").Path, Is.Empty);
			Assert.That(fresh.LegacyReads, Is.Zero);
			Assert.That(fresh.Writes, Is.Zero, "Restart and unchanged screen creation must not save preferences.");
		});
	}

	[TestCase(typeof(BloomFilterAnalysisScreen))]
	[TestCase(typeof(MerkleTreeTestScreen))]
	[TestCase(typeof(ObjectSpaceScreen))]
	[TestCase(typeof(ObservableCollectionsTestScreen))]
	[TestCase(typeof(SourceGridGalleryScreen))]
	[TestCase(typeof(TransactionalCollectionScreen))]
	[TestCase(typeof(VisualGridTestScreen))]
	[TestCase(typeof(EmailTestScreen))]
	[TestCase(typeof(ImageResizeScreen))]
	[TestCase(typeof(LoggingTestScreen))]
	[TestCase(typeof(ParagraphBuilderScreen))]
	[TestCase(typeof(ScheduleTestScreen))]
	[TestCase(typeof(SettingsTest))]
	[TestCase(typeof(UrlIDTestScreen))]
	[TestCase(typeof(WAMSTestScreen))]
	[TestCase(typeof(ScreenHostingDesignTestScreen))]
	public void AdHocHostedScreensKeepTheirDefaultPresentationWithoutDeclaredTypedSettings(Type screenType) {
		RunWithMessageLoop(async owner => {
			var directory = Tools.FileSystem.GetTempEmptyDirectory();
			using var cleanup = Tools.Scope.DeleteDirOnDispose(directory);
			UserSettings.TryGetProvider(out var previous);
			using var restore = Tools.Scope.ExecuteOnDispose(() => UserSettings.Provider = previous);
			var provider = new LegacyPathProvider(directory);
			UserSettings.Provider = provider;
			using var host = new FormsUI.ApplicationScreenHost { Dock = DockStyle.Fill };
			owner.Controls.Add(host);
			using var screen = screenType == typeof(ScreenHostingDesignTestScreen)
				? new ScreenHostingDesignTestScreen("Ad hoc inspector", includePreferences: true)
				: (ApplicationScreen)Activator.CreateInstance(screenType);
			Assert.That(host.ShowScreen(screen), Is.True);
			await Task.Yield();
			if (screen is VisualGridTestScreen grid)
				await grid.Initialization;
			if (screen.GetType().GetField("_preferencesLoading", BindingFlags.Instance | BindingFlags.NonPublic)?.GetValue(screen) is Task preferences)
				await preferences;
			Assert.That(screen.Host, Is.SameAs(host));
			Assert.That(screen.Visible, Is.True);

			// Exercise harmless native changes that previously mistook a host for an attached settings profile.
			switch (screen) {
				case BloomFilterAnalysisScreen:
					Find<NumericUpDown>(screen, "_expectedItems").Value++;
					break;
				case MerkleTreeTestScreen:
					Find<NumericUpDown>(screen, "_leafCount").Value++;
					break;
				case ObjectSpaceScreen:
					Find<NumericUpDown>(screen, "sampleAccounts").Value = 2;
					Find<PathSelectorControl>(screen, "_objectSpacePathControl").Path = "unopened-demo.s10space";
					Assert.That(Field<ObjectSpace>(screen, "_space"), Is.Null);
					break;
				case ObservableCollectionsTestScreen:
					Find<CheckBox>(screen, "_includeAccess").Checked = true;
					break;
				case SourceGridGalleryScreen:
					Find<TabControl>(screen, "sourceGridExamples").SelectedIndex = 1;
					break;
				case TransactionalCollectionScreen:
					Find<IntBox>(screen, "_itemsIntBox").Value = 12;
					break;
				case EmailTestScreen:
					Find<CheckBox>(screen, "_sslCheckBox").Checked = true;
					break;
				case ImageResizeScreen:
					Find<NumericUpDown>(screen, "_widthNumeric").Value = 32;
					break;
				case ParagraphBuilderScreen:
					Find<CheckBox>(screen, "_formatSentence").Checked = !Find<CheckBox>(screen, "_formatSentence").Checked;
					break;
				case ScheduleTestScreen:
					Find<Button>(screen, "AsyncTestButton").PerformClick();
					Assert.That(Field<Scheduler>(screen, "_asynchronousScheduler"), Is.Not.Null);
					Assert.That(Field<Scheduler>(screen, "_asynchronousScheduler").Status, Is.EqualTo(SchedulerStatus.Off));
					Assert.That(Field<Scheduler>(screen, "_synchronousScheduler"), Is.Null);
					break;
				case SettingsTest:
					Find<CheckBox>(screen, "_globalScope").Checked = true;
					break;
				case UrlIDTestScreen:
					Find<NumericUpDown>(screen, "_count").Value = 3;
					break;
				case WAMSTestScreen:
					Find<NumericUpDown>(screen, "_heightControl").Value = 3;
					break;
				case ScreenHostingDesignTestScreen:
					foreach (var name in new[] { "PreferencesGrid", "DetailsGrid", "StandaloneGrid" })
						Assert.That(Find<VisualGrid>(screen, name).VisibleEntities, Is.Not.Empty, name);
					Assert.That(Find<Label>(screen, "PreferenceSummary").Text, Does.Not.Contain("could not be loaded"));
					Assert.That(Find<LogOutputSelectorControl>(screen, "PreferenceLogOutput").IsLogging, Is.False);
					break;
			}

			await screen.SaveSettingsAsync();
			Assert.That(await host.CloseScreenAsync(screen), Is.True);
			Assert.That(screen.IsDisposed, Is.True);
			Assert.That(provider.Writes, Is.Zero, "Ad hoc presentation must not create a substitute typed settings record.");
		});
	}

	[TestCase(0)]
	[TestCase(1)]
	public void ObjectSpaceCreatesEditsDeletesAndReopensItsDefinitionFromMetadata(int byteOrder) {
		RunWithMessageLoop(async owner => {
			using var screen = new ObjectSpaceScreen { Dock = DockStyle.Fill };
			owner.Controls.Add(screen);
			await Task.Yield();
			Find<NumericUpDown>(screen, "sampleAccounts").Value = 2;
			Find<ComboBox>(screen, "storeEndianness").SelectedIndex = byteOrder;
			Find<Button>(screen, "_buildButton").PerformClick();
			var store = Field<ObjectSpace>(screen, "_space");
			Assert.That(store, Is.Not.Null, Find<TextBox>(screen, "objectSpaceLog").Text);
			Assert.That(store.Count<ObjectSpaceScreen.Account>(), Is.EqualTo(2));
			Assert.That(store.Count<ObjectSpaceScreen.Identity>(), Is.EqualTo(2));
			Find<TextBox>(screen, "accountName").Text = "Test account";
			Find<NumericUpDown>(screen, "accountQuantity").Value = 12.5M;
			Find<Button>(screen, "_addButton").PerformClick();
			Assert.That(store.Get((ObjectSpaceScreen.Account account) => account.Name, "Test account").Quantity, Is.EqualTo(12.5M));

			var rows = Find<DataGridView>(screen, "accountsGrid");
			SelectAccount(rows, "Test account");
			Find<TextBox>(screen, "accountName").Text = "Renamed account";
			Find<NumericUpDown>(screen, "accountQuantity").Value = 99M;
			Find<Button>(screen, "_updateButton").PerformClick();
			Assert.That(store.TryGet((ObjectSpaceScreen.Account account) => account.Name, "Test account", out _), Is.False);
			Assert.That(store.Get((ObjectSpaceScreen.Account account) => account.Name, "Renamed account").Quantity, Is.EqualTo(99M));
			Find<Button>(screen, "_reloadButton").PerformClick();
			var reopened = Field<ObjectSpace>(screen, "_space");
			Assert.That(reopened, Is.Not.SameAs(store));
			Assert.That(reopened.Count<ObjectSpaceScreen.Account>(), Is.EqualTo(3));
			Assert.That(reopened.Get((ObjectSpaceScreen.Account account) => account.Name, "Renamed account").Quantity, Is.EqualTo(99M));
			Assert.That(reopened.Traits.HasFlag(ObjectSpaceTraits.MetaData), Is.True);
			Assert.That(reopened.Traits.HasFlag(ObjectSpaceTraits.BigEndian), Is.EqualTo(byteOrder == 1));
			SelectAccount(rows, "Renamed account");
			Find<Button>(screen, "_deleteButton").PerformClick();
			Assert.That(reopened.Count<ObjectSpaceScreen.Account>(), Is.EqualTo(2));
			Assert.That(reopened.Count<ObjectSpaceScreen.Identity>(), Is.EqualTo(2));
			Find<Button>(screen, "_closeButton").PerformClick();
			Assert.That(Field<ObjectSpace>(screen, "_space"), Is.Null);
			Assert.That(Field<MemoryStream>(screen, "_rawStream"), Is.Null);
		});
	}

	[Test]
	public void BloomSizingReportsBitsAndRoundedBytesFromTheActualMathApi() {
		RunWithMessageLoop(async owner => {
			using var screen = new BloomFilterAnalysisScreen { Dock = DockStyle.Fill };
			owner.Controls.Add(screen);
			await Task.Yield();
			Find<NumericUpDown>(screen, "_expectedItems").Value = 100;
			Find<NumericUpDown>(screen, "_targetError").Value = 0.01M;
			Find<NumericUpDown>(screen, "_hashRounds").Value = 4;
			Find<Button>(screen, "_estimateButton").PerformClick();
			var row = Find<TextBox>(screen, "_standardTextBox").Lines[1].Split('\t');
			var bits = BloomFilterMath.EstimateBloomFilterLength(0.01M, 100, 4);
			Assert.That(long.Parse(row[3], NumberStyles.Number, CultureInfo.CurrentCulture), Is.EqualTo(bits));
			Assert.That(long.Parse(row[4], NumberStyles.Number, CultureInfo.CurrentCulture), Is.EqualTo((bits + 7) / 8));
			Find<Button>(screen, "_metricsTableButton").PerformClick();
			Assert.That(Find<TextBox>(screen, "_standardTextBox").Lines.Count(line => line.Contains('\t')), Is.EqualTo(10));
		});
	}

	[Test]
	public void MerkleComparisonAndAppendBatchesReportMatchingActualRoots() {
		RunWithMessageLoop(async owner => {
			using var screen = new MerkleTreeTestScreen { Dock = DockStyle.Fill };
			owner.Controls.Add(screen);
			await Task.Yield();
			Find<NumericUpDown>(screen, "_leafCount").Value = 7;
			Find<Button>(screen, "_perfectMerkleButton").PerformClick();
			await WaitUntilAsync(() => Field<CancellationTokenSource>(screen, "_comparisonCancellation") == null);
			Assert.That(Find<TextBox>(screen, "_outputTextBox").Text, Does.Contain("Roots match: True"));
			Find<Button>(screen, "_treePerfTestsButton").PerformClick();
			await WaitUntilAsync(() => Field<CancellationTokenSource>(screen, "_comparisonCancellation") == null);
			Assert.That(Find<TextBox>(screen, "_outputTextBox").Text, Does.Contain("Batch 5: 167 leaves; roots match: True"));
			Assert.That(Find<TextBox>(screen, "_outputTextBox").Text, Does.Not.Contain("match: False"));
		});
	}

	[Test]
	public void CancellingMerkleWorkRestoresItsNativeCommands() {
		RunWithMessageLoop(async owner => {
			using var screen = new MerkleTreeTestScreen { Dock = DockStyle.Fill };
			owner.Controls.Add(screen);
			await Task.Yield();
			Find<NumericUpDown>(screen, "_leafCount").Value = 20000;
			Find<Button>(screen, "_treePerfTestsButton").PerformClick();
			Assert.That(Find<Button>(screen, "_cancelButton").Enabled, Is.True);
			Find<Button>(screen, "_cancelButton").PerformClick();
			await WaitUntilAsync(() => Field<CancellationTokenSource>(screen, "_comparisonCancellation") == null);
			Assert.That(Find<Label>(screen, "showcaseStatus").Text, Is.EqualTo("Comparison cancelled."));
			Assert.That(Find<Button>(screen, "_treePerfTestsButton").Enabled, Is.True);
			Assert.That(Find<Button>(screen, "_cancelButton").Enabled, Is.False);
		});
	}

	[Test]
	public void HostedVisualGridBindsBothCompleteSchemasAndLoadsEditableAndLiveRows() {
		RunWithMessageLoop(async owner => {
			var directory = Tools.FileSystem.GetTempEmptyDirectory();
			using var cleanup = Tools.Scope.DeleteDirOnDispose(directory);
			await using var services = CreatePreferenceApplication(
				new DirectoryFileSettingsProvider(directory),
				typeof(VisualGridTestScreen),
				new VisualGridTestScreen.VisualGridShowcaseSettings()
			);
			var application = services.GetRequiredService<FormsUI.IApplication>();
			await application.InitializeAsync();
			var main = (PreferenceMainForm)services.GetRequiredService<IMainForm>();
			main.Show(owner);
			await Task.Yield();
			var screen = (VisualGridTestScreen)main.ActiveScreen;
			await screen.Initialization;
			var records = Find<VisualGrid>(screen, "_crudGrid");
			var live = Find<VisualGrid>(screen, "_liveGrid");
			Assert.That(records.GridBindings.Count(), Is.EqualTo(12), "The command column is retained alongside all eleven employee projections.");
			Assert.That(records.Preferences, Is.Not.Null);
			Assert.That(live.Preferences, Is.Not.Null);
			Assert.That(records.VisibleEntities, Is.Not.Empty);
			Assert.That(live.VisibleEntities, Is.Not.Empty);
			Find<Button>(screen, "_selectFirstEntityButton").PerformClick();
			Assert.That(records.SelectedEntity, Is.Not.Null);

			var block = application.Blocks.Single();
			var settings = (VisualGridTestScreen.VisualGridShowcaseSettings)block.ScreenDefinitions.Single().Settings;
			settings.RecordsGrid.PageMode = VisualGridPageMode.Paged;
			settings.RecordsGrid.PageSize = 7;
			await settings.SaveAsync();
			await WaitUntilAsync(() => records.Preferences.PageSize == 7 && records.VisibleEntities.Count() == 7);
			Assert.That(records.GridBindings.Single(column => column.PreferenceKey == "EditCommand").DisplayType, Is.EqualTo(CrudCellDisplayType.EditCommand));
		});
	}

	[Test]
	public void TransactionWorkbenchVerifiesCommitRollbackAndReopenedContents() {
		RunWithMessageLoop(async owner => {
			using var screen = new TransactionalCollectionScreen { Dock = DockStyle.Fill };
			owner.Controls.Add(screen);
			await Task.Yield();
			Find<IntBox>(screen, "_itemsIntBox").Value = 12;
			Find<IntBox>(screen, "_batchIntBox").Value = 5;
			Find<Button>(screen, "_rollbackButton").PerformClick();
			await WaitUntilAsync(() => Field<CancellationTokenSource>(screen, "_operationCancellation") == null);
			Assert.That(Find<TextBox>(screen, "_outputTextBox").Text, Does.Contain("verified exactly [1, 3]"));
			Find<Button>(screen, "_listAppendTestButton").PerformClick();
			await WaitUntilAsync(() => Field<CancellationTokenSource>(screen, "_operationCancellation") == null);
			Assert.That(Find<TextBox>(screen, "_outputTextBox").Text, Does.Contain("Verified count: 12"));
			Find<CheckBox>(screen, "_commitCheckBox").Checked = true;
			Find<Button>(screen, "_dictionaryAppend").PerformClick();
			await WaitUntilAsync(() => Field<CancellationTokenSource>(screen, "_operationCancellation") == null);
			Assert.That(Find<TextBox>(screen, "_outputTextBox").Text, Does.Contain("Transactional dictionary count: 12"));
			Assert.That(Find<Label>(screen, "showcaseStatus").Text, Does.StartWith("Operation complete"));
			Find<IntBox>(screen, "_itemsIntBox").Value = null;
			Find<Button>(screen, "_listAppendTestButton").PerformClick();
			Assert.That(Find<Label>(screen, "showcaseStatus").Text, Does.Contain("Items must be a positive whole number"));
			Assert.That(Field<CancellationTokenSource>(screen, "_operationCancellation"), Is.Null);
		});
	}

	[Test]
	public void CancellingCollectionWorkReleasesItsWorkerAndRestoresCommands() {
		RunWithMessageLoop(async owner => {
			using var screen = new TransactionalCollectionScreen { Dock = DockStyle.Fill };
			owner.Controls.Add(screen);
			await Task.Yield();
			var previousDirectories = Directory.GetDirectories(Path.GetTempPath(), "Sphere10.CollectionDemo-*").ToHashSet(StringComparer.OrdinalIgnoreCase);
			Find<IntBox>(screen, "_itemsIntBox").Value = 100000;
			Find<Button>(screen, "_dictionaryAppend").PerformClick();
			await WaitUntilAsync(() => Directory.GetDirectories(Path.GetTempPath(), "Sphere10.CollectionDemo-*")
				.Any(path => !previousDirectories.Contains(path) && File.Exists(Path.Combine(path, "data.s10")))
				|| Field<CancellationTokenSource>(screen, "_operationCancellation") == null);
			Assert.That(Field<CancellationTokenSource>(screen, "_operationCancellation"), Is.Not.Null, Find<Label>(screen, "showcaseStatus").Text);
			var ownedDirectories = Directory.GetDirectories(Path.GetTempPath(), "Sphere10.CollectionDemo-*").Where(path => !previousDirectories.Contains(path)).ToArray();
			Find<Button>(screen, "_cancelButton").PerformClick();
			await WaitUntilAsync(() => Field<CancellationTokenSource>(screen, "_operationCancellation") == null);
			Assert.That(Find<Label>(screen, "showcaseStatus").Text, Does.StartWith("Operation cancelled"));
			Assert.That(Find<Button>(screen, "_dictionaryAppend").Enabled, Is.True);
			Assert.That(ownedDirectories, Is.Not.Empty);
			Assert.That(ownedDirectories.Any(Directory.Exists), Is.False, "Cancellation must release the file handle and remove its owned temporary directory.");
		});
	}

	// Compose a real native application/catalog around one declared demonstration registration.
	private static ServiceProvider CreatePreferenceApplication(ISettingsProvider provider, Type screenType = null, SettingsObject settings = null) {
		var block = new ApplicationBlock { Name = "ObjectSpace legacy path" };
		var definition = new FormsUI.ApplicationScreenDefinition(screenType ?? typeof(ObjectSpaceScreen), "Demonstration", settings: settings);
		block.AddScreenDefinition(definition);
		block.DefaultScreenDefinition = definition;
		var services = new ServiceCollection();
		services.AddSingleton(new Local<ISettingsProvider>(provider));
		services.AddMainForm<PreferenceMainForm>();
		services.AddApplicationBlock(block);
		services.AddApplicationSettings("TesterObjectSpaceLegacyPath");
		return services.BuildServiceProvider();
	}

	// Metadata discovery never constructs the operational controls or invokes their settings getters.
	private static IEnumerable<Type> TypedSettingsScreenTypes() {
		return typeof(EmailTestScreen).Assembly.GetExportedTypes()
			.Where(type => typeof(ApplicationScreen).IsAssignableFrom(type)
				&& type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Any(property => property.Name == nameof(ApplicationScreen.ScreenSettings) && property.PropertyType != typeof(object)))
			.OrderBy(type => type.FullName);
	}

	// These scalar-only demonstrations intentionally have no grid binding that could mask a missing dirty notification.
	private static PropertyDescriptor ScalarInputProperty(Control input) {
		var propertyName = input switch {
			CheckBox => nameof(CheckBox.Checked),
			NumericUpDown => nameof(NumericUpDown.Value),
			TextBox => nameof(TextBox.Text),
			_ => throw new InvalidOperationException("Unexpected scalar preference control.")
		};
		return TypeDescriptor.GetProperties(input)[propertyName];
	}

	// Select the actual row before invoking the screen's tracked-record command.
	private static void SelectAccount(DataGridView grid, string name) {
		grid.ClearSelection();
		var row = grid.Rows.Cast<DataGridViewRow>().Single(item => Equals(item.Cells[0].Value, name));
		grid.CurrentCell = row.Cells[0];
		row.Selected = true;
	}

	// Exercise the real Designer-owned controls without adding production test accessors.
	private static T Find<T>(Control parent, string name)
		where T : Control {
		return (T)parent.Controls.Find(name, true).Single();
	}

	// Observe owned session/task state that has no public UI contract.
	private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);

	// Pump the native dispatcher until an observable operation has finished.
	private static async Task WaitUntilAsync(Func<bool> completed) {
		var clock = Stopwatch.StartNew();
		while (!completed() && clock.Elapsed < TimeSpan.FromSeconds(10))
			await Task.Delay(10);
		Assert.That(completed(), Is.True, "The data showcase did not complete its native operation.");
	}

	// Each test owns a dispatcher and bounded watchdog, preserving its caller's context.
	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var previous = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous));
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form {
			ShowInTaskbar = false,
			StartPosition = FormStartPosition.Manual,
			Location = new Point(-20000, -20000),
			ClientSize = new Size(1100, 800)
		};
		using var watchdog = new System.Windows.Forms.Timer { Interval = 20000 };
		watchdog.Tick += (_, _) => {
			failure = new AssertionException("The data showcase test timed out.");
			System.Windows.Forms.Application.ExitThread();
		};
		owner.Shown += async (_, _) => {
			try {
				await test(owner);
			} catch (Exception error) {
				failure = error;
			} finally {
				watchdog.Stop();
				foreach (var dialog in System.Windows.Forms.Application.OpenForms.Cast<Form>().Where(form => form != owner).ToArray())
					dialog.Dispose();
				owner.Close();
			}
		};
		watchdog.Start();
		System.Windows.Forms.Application.Run(owner);
		Assert.That(failure, Is.Null, failure?.ToString());
	}

	/// <summary>A native test owner that keeps its errors observable and does not perform application startup twice.</summary>
	public class PreferenceMainForm : BlockMainForm {
		/// <summary>Creates an offscreen test window.</summary>
		public PreferenceMainForm() {
			ShowInTaskbar = false;
			StartPosition = FormStartPosition.Manual;
			Location = new Point(-20000, -20000);
		}

		// The test explicitly owns application initialization and native presentation.
		protected override void OnLoad(EventArgs e) {
		}
	}

	// Observe actual file-backed legacy reads and saves without changing the framework's provider behavior.
	private sealed class LegacyPathProvider : DirectoryFileSettingsProvider {
		internal LegacyPathProvider(string directory)
			: base(directory) {
		}

		internal int LegacyReads {
			get; private set;
		}

		internal int LegacyReadThread {
			get; private set;
		}

		internal int Writes {
			get; set;
		}

		// Track the IO owner only for the retired ObjectSpace path record.
		protected override SettingsObject LoadInternal(Type type, object id) {
			if (type == typeof(ObjectSpaceScreen.ScreenSettings)) {
				LegacyReads++;
				LegacyReadThread = Environment.CurrentManagedThreadId;
			}
			return base.LoadInternal(type, id);
		}

		// Count real accepted writes, including the new application profile envelope.
		protected override void SaveInternal(SettingsObject settings) {
			base.SaveInternal(settings);
			Writes++;
		}
	}
}
