// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Windows.Forms.SourceGrid;
using FormsApplication = System.Windows.Forms.Application;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class CrudGridAutoPageSizeTests {
	private ISettingsProvider _previousProvider;
	private string _directory;
	private CountingProvider _provider;

	[SetUp]
	public void SetUp() {
		_previousProvider = (ISettingsProvider)typeof(UserSettings).GetField("_provider", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
		_directory = Path.Combine(Path.GetTempPath(), $"Sphere10-CrudGrid-{Guid.NewGuid():N}");
		_provider = new CountingProvider(_directory);
		UserSettings.Provider = new CachedSettingsProvider(_provider);
	}

	[TearDown]
	public void TearDown() {
		UserSettings.Provider = _previousProvider;
		if (Directory.Exists(_directory))
			Directory.Delete(_directory, true);
	}

	[Test]
	public void FooterSwitchFitsTheFirstPageAndRestoresTheManualSize() => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner);
		var source = await BindAsync(crud);
		Find<NumericUpDown>(crud, "_pageSizeUpDown").Value = 37;
		await CompleteRefreshAsync(crud);
		Find<IntBox>(crud, "_pageNumberBox").Value = 3;
		await CompleteRefreshAsync(crud);
		Assert.That(source.Requests.Last().Page, Is.EqualTo(2));
		var mode = Find<ComboBox>(crud, "_pageModeComboBox");
		mode.SelectedIndex = (int)CrudGridPageMode.Auto;
		await CompleteRefreshAsync(crud);
		Assert.That(crud.AutoPageSize, Is.True);
		Assert.That(source.Requests.Last().Page, Is.Zero);
		Assert.That(Find<Control>(crud, "_manualPageSizePanel").Visible, Is.False);
		Assert.That(Find<Control>(crud, "_pageSizeLabel").Visible, Is.False);
		Assert.That(Find<Control>(crud, "_pageSizeUpDown").Visible, Is.False);
		Assert.That(Find<Control>(crud, "_pageNavigationPanel").Visible, Is.True);
		Assert.That(Find<Control>(crud, "_nextPageButton").Enabled, Is.True);
		Assert.That(Find<Label>(crud, "_totalRecordsLabel").Visible, Is.True);
		Assert.That(Find<Label>(crud, "_totalRecordsLabel").Text, Is.EqualTo("240"));
		AssertCompleteRows(crud);
		mode.SelectedIndex = (int)CrudGridPageMode.Paged;
		await CompleteRefreshAsync(crud);
		Assert.That(crud.AutoPageSize, Is.False);
		Assert.That(crud.VisibleEntities.Count(), Is.EqualTo(37));
		Assert.That(Find<Control>(crud, "_manualPageSizePanel").Visible, Is.True);
		Assert.That(Find<Control>(crud, "_pageSizeUpDown").Enabled, Is.True);
		Assert.That(Find<NumericUpDown>(crud, "_pageSizeUpDown").Value, Is.EqualTo(37), "Manual mode restores the remembered input.");
		Assert.That(Find<Control>(crud, "_pageNavigationPanel").Visible, Is.True);
		Assert.That(Find<Control>(crud, "_nextPageButton").Enabled, Is.True);
	});

	[Test]
	public void AutomaticSizingKeepsAllPagerButtonsAndDirectPageEntryWorking() => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner, "Auto-navigation");
		crud.AutoPageSize = true;
		var source = await BindAsync(crud);
		var capacity = source.Requests.Last().Length;
		Find<Button>(crud, "_nextPageButton").PerformClick();
		await CompleteRefreshAsync(crud);
		Assert.That(source.Requests.Last().Page, Is.EqualTo(1));
		Assert.That(crud.VisibleEntities.Cast<string>().First(), Is.EqualTo($"Address {capacity:D4}"));
		Find<Button>(crud, "_lastPageButton").PerformClick();
		await CompleteRefreshAsync(crud);
		var lastPage = (240 - 1) / capacity;
		Assert.That(source.Requests.Last().Page, Is.EqualTo(lastPage));
		Assert.That(crud.VisibleEntities.Cast<string>().Last(), Is.EqualTo("Address 0239"));
		Find<Button>(crud, "_previousPageButton").PerformClick();
		await CompleteRefreshAsync(crud);
		Assert.That(source.Requests.Last().Page, Is.EqualTo(lastPage - 1));
		Find<Button>(crud, "_firstPageButton").PerformClick();
		await CompleteRefreshAsync(crud);
		Assert.That(source.Requests.Last().Page, Is.Zero);
		Find<IntBox>(crud, "_pageNumberBox").Value = 3;
		await CompleteRefreshAsync(crud);
		Assert.That(source.Requests.Last().Page, Is.EqualTo(2));
		Assert.That(source.Requests.Last().Length, Is.EqualTo(capacity));
		Assert.That(Find<NumericUpDown>(crud, "_pageSizeUpDown").Visible, Is.False);
		AssertCompleteRows(crud);
	});

	[Test]
	public void AutomaticPageSelectionSurvivesAFreshUserSettingsProvider() => RunWithMessageLoop(async owner => {
		using (var first = NewGrid(owner, "Auto-page-persistence")) {
			first.AutoPageSize = true;
			await BindAsync(first);
			Find<IntBox>(first, "_pageNumberBox").Value = 3;
			await CompleteRefreshAsync(first);
		}
		UserSettings.Provider = new CachedSettingsProvider(new DirectoryFileSettingsProvider(_directory));
		using var restored = NewGrid(owner, "Auto-page-persistence");
		var source = await BindAsync(restored);
		Assert.That(restored.AutoPageSize, Is.True);
		Assert.That(source.Requests.Last().Page, Is.EqualTo(2));
		Assert.That(Find<IntBox>(restored, "_pageNumberBox").Value, Is.EqualTo(3));
	});

	[TestCase(CrudGridPageMode.Paged)]
	[TestCase(CrudGridPageMode.Auto)]
	[TestCase(CrudGridPageMode.Single)]
	public void SelectedModePersistsAcrossSessionsAndRetainsTheManualSize(CrudGridPageMode mode) => RunWithMessageLoop(async owner => {
		using (var first = NewGrid(owner, "Mode-persistence")) {
			await BindAsync(first);
			Find<NumericUpDown>(first, "_pageSizeUpDown").Value = 41;
			await CompleteRefreshAsync(first);
			Find<ComboBox>(first, "_pageModeComboBox").SelectedIndex = (int)mode;
			await CompleteRefreshAsync(first);
		}
		UserSettings.Provider = new CachedSettingsProvider(new DirectoryFileSettingsProvider(_directory));
		using var restored = NewGrid(owner, "Mode-persistence");
		var source = await BindAsync(restored);
		Assert.That(restored.PageMode, Is.EqualTo(mode));
		Assert.That(UserSettings.Get<CrudGridSettings>("Mode-persistence").PageMode, Is.EqualTo(mode));
		Assert.That(UserSettings.Get<CrudGridSettings>("Mode-persistence").PageSize, Is.EqualTo(41));
		Assert.That(source.Requests.Last().Page, Is.Zero);
		if (mode == CrudGridPageMode.Single)
			Assert.That(restored.VisibleEntities.Count(), Is.EqualTo(240));
		restored.PageMode = CrudGridPageMode.Paged;
		await CompleteRefreshAsync(restored);
		Assert.That(restored.VisibleEntities.Count(), Is.EqualTo(41));
		Assert.That(Find<NumericUpDown>(restored, "_pageSizeUpDown").Value, Is.EqualTo(41));
	});

	[TestCase(false, null, CrudGridPageMode.Paged)]
	[TestCase(true, null, CrudGridPageMode.Auto)]
	[TestCase(false, 999, CrudGridPageMode.Paged)]
	[TestCase(true, 999, CrudGridPageMode.Auto)]
	public void OldOrInvalidModeSettingsFallBackToTheLegacyAutoPreference(bool automatic, int? storedMode, CrudGridPageMode expected) => RunWithMessageLoop(async owner => {
		var settings = UserSettings.Get<CrudGridSettings>("Legacy-mode");
		settings.AutoPageSize = automatic;
		settings.PageMode = storedMode.HasValue ? (CrudGridPageMode)storedMode.Value : null;
		settings.PageSize = 23;
		settings.Save();
		UserSettings.Provider = new CachedSettingsProvider(new DirectoryFileSettingsProvider(_directory));
		using var crud = NewGrid(owner, "Legacy-mode");
		await BindAsync(crud);
		Assert.That(crud.PageMode, Is.EqualTo(expected));
		Assert.That(Find<ComboBox>(crud, "_pageModeComboBox").SelectedIndex, Is.EqualTo((int)expected));
		Assert.That(_provider.SaveCount, Is.EqualTo(1), "Migration reads must not overwrite cached preferences.");
		crud.AutoPageSize = false;
		await CompleteRefreshAsync(crud);
		Assert.That(crud.PageMode, Is.EqualTo(CrudGridPageMode.Paged));
		Assert.That(crud.VisibleEntities.Count(), Is.EqualTo(23));
	});

	[Test]
	public void ModeDropdownSwitchesToOneScrollablePageAndBackWithoutChangingWidths() => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner);
		var source = await BindAsync(crud);
		Find<NumericUpDown>(crud, "_pageSizeUpDown").Value = 25;
		await CompleteRefreshAsync(crud);
		Find<IntBox>(crud, "_pageNumberBox").Value = 3;
		await CompleteRefreshAsync(crud);
		var grid = Find<Grid>(crud, "_grid");
		var widths = grid.Columns.Cast<ColumnInfo>().Select(column => column.Width).ToArray();
		var mode = Find<ComboBox>(crud, "_pageModeComboBox");
		mode.SelectedIndex = (int)CrudGridPageMode.Single;
		await CompleteRefreshAsync(crud);
		Assert.That(crud.PageMode, Is.EqualTo(CrudGridPageMode.Single));
		Assert.That(source.Requests.Last().Length, Is.EqualTo(int.MaxValue));
		Assert.That(source.Requests.Last().Page, Is.Zero);
		Assert.That(crud.VisibleEntities.Count(), Is.EqualTo(240));
		Assert.That(grid.Rows.Count, Is.EqualTo(241), "Single must bind every matching row plus its header.");
		Assert.That(grid.VScrollBarVisible, Is.True);
		Assert.That(Find<Control>(crud, "_pageNavigationPanel").Visible, Is.False);
		Assert.That(Find<Control>(crud, "_manualPageSizePanel").Visible, Is.False);
		Assert.That(grid.Columns.Cast<ColumnInfo>().Select(column => column.Width), Is.EqualTo(widths));
		mode.SelectedIndex = (int)CrudGridPageMode.Auto;
		await CompleteRefreshAsync(crud);
		Assert.That(Find<Control>(crud, "_pageNavigationPanel").Visible, Is.True);
		AssertCompleteRows(crud);
		mode.SelectedIndex = (int)CrudGridPageMode.Paged;
		await CompleteRefreshAsync(crud);
		Assert.That(crud.VisibleEntities.Count(), Is.EqualTo(25));
		Assert.That(source.Requests.Last().Page, Is.Zero);
		Assert.That(Find<NumericUpDown>(crud, "_pageSizeUpDown").Value, Is.EqualTo(25));
		Assert.That(grid.Columns.Cast<ColumnInfo>().Select(column => column.Width), Is.EqualTo(widths));
	});

	[Test]
	public void ConsumerMigrationIsDetachedAndLaterExplicitSingleSurvivesAFreshProvider() => RunWithMessageLoop(async owner => {
		const string key = "Consumer-migration";
		var settings = UserSettings.Get<CrudGridSettings>(key);
		settings.PageMode = CrudGridPageMode.Single;
		settings.PageSize = 37;
		settings.SortColumnName = "Address";
		settings.SortDirection = SortDirection.Descending;
		settings.Save();
		using (var crud = new UpgradedGrid { Dock = DockStyle.Fill, SettingsKey = key }) {
			owner.Controls.Add(crud);
			await BindAsync(crud);
			Assert.That(crud.PageMode, Is.EqualTo(CrudGridPageMode.Auto));
			Assert.That(crud.VisibleEntities.Cast<string>().First(), Is.EqualTo("Address 0239"));
			Assert.That(settings.ConsumerVersion, Is.Zero, "Restoring must not mutate the provider's cached payload.");
			Assert.That(settings.PageMode, Is.EqualTo(CrudGridPageMode.Single));
			Assert.That(_provider.SaveCount, Is.EqualTo(1), "Restoring must not write preferences.");
			crud.PageMode = CrudGridPageMode.Single;
			await CompleteRefreshAsync(crud);
		}
		UserSettings.Provider = new CachedSettingsProvider(new DirectoryFileSettingsProvider(_directory));
		using var restored = new UpgradedGrid { Dock = DockStyle.Fill, SettingsKey = key };
		owner.Controls.Add(restored);
		await BindAsync(restored);
		var saved = UserSettings.Get<CrudGridSettings>(key);
		Assert.That(saved.ConsumerVersion, Is.EqualTo(1));
		Assert.That(saved.PageSize, Is.EqualTo(37));
		Assert.That(saved.SortDirection, Is.EqualTo(SortDirection.Descending));
		Assert.That(restored.PageMode, Is.EqualTo(CrudGridPageMode.Single));
	});

	[Test]
	public void SingleModeKeepsSortingAndSearchingOverTheCompleteResult() => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner);
		crud.PageMode = CrudGridPageMode.Single;
		var source = await BindAsync(crud);
		await ClickHeaderAsync(crud);
		await ClickHeaderAsync(crud);
		Assert.That(crud.VisibleEntities.Cast<string>().First(), Is.EqualTo("Address 0239"));
		Find<SearchTextBox>(crud, "_searchTextBox").Text = "Address 001";
		await CompleteRefreshAsync(crud);
		Assert.That(source.Requests.Last().Length, Is.EqualTo(int.MaxValue));
		Assert.That(source.Requests.Last().Page, Is.Zero);
		Assert.That(crud.VisibleEntities.Cast<string>(), Is.EqualTo(Enumerable.Range(10, 10).Reverse().Select(index => $"Address {index:D4}")));
		Assert.That(Find<Label>(crud, "_totalRecordsLabel").Text, Is.EqualTo("10"));
	});

	[TestCase(CrudGridPageMode.Paged)]
	[TestCase(CrudGridPageMode.Auto)]
	[TestCase(CrudGridPageMode.Single)]
	public void NonpageableSourcesKeepTheirExistingCompleteResultBehavior(CrudGridPageMode mode) => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner);
		crud.PageMode = mode;
		crud.GridBindings = new[] { new CrudGridColumn<string> { ColumnName = "Address", DataType = typeof(string), PropertyValue = value => value } };
		var source = new RecordingSource(240);
		await crud.SetDataSource(source, DataSourceCapabilities.ComboBoxDefault);
		await CompleteRefreshAsync(crud);
		Assert.That(source.Requests, Is.Not.Empty);
		Assert.That(source.Requests.Last().Length, Is.EqualTo(int.MaxValue));
		Assert.That(source.Requests.Last().Page, Is.Zero);
		Assert.That(crud.VisibleEntities.Count(), Is.EqualTo(240));
		Assert.That(Find<ComboBox>(crud, "_pageModeComboBox").Visible, Is.False);
		Assert.That(crud.Capabilities.HasFlag(DataSourceCapabilities.CanPage), Is.False);
		Assert.That(crud.Capabilities, Is.EqualTo(DataSourceCapabilities.ComboBoxDefault));
	});

	[Test]
	public void ChangingToSingleDuringAnOldReadDiscardsThePagedResult() => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner);
		var source = new DelayedRecordingSource(240);
		await BindAsync(crud, source);
		source.DelayNextRead();
		var pending = crud.RefreshGrid();
		await source.ReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
		crud.PageMode = CrudGridPageMode.Single;
		source.ContinueRead.SetResult();
		await pending;
		await CompleteRefreshAsync(crud);
		Assert.That(source.Requests.Last().Length, Is.EqualTo(int.MaxValue));
		Assert.That(source.Requests.Last().Page, Is.Zero);
		Assert.That(crud.VisibleEntities.Count(), Is.EqualTo(240));
	});

	[Test]
	public void AutoPageSizeCompatibilityAliasSelectsAutoOrPagedAndInvalidModesAreRejected() {
		using var crud = new CrudGrid();
		crud.AutoPageSize = true;
		Assert.That(crud.PageMode, Is.EqualTo(CrudGridPageMode.Auto));
		crud.PageMode = CrudGridPageMode.Single;
		Assert.That(crud.AutoPageSize, Is.False);
		crud.AutoPageSize = false;
		Assert.That(crud.PageMode, Is.EqualTo(CrudGridPageMode.Paged));
		Assert.That(() => crud.PageMode = (CrudGridPageMode)999, Throws.ArgumentException);
	}

	[TestCase(CrudGridPageMode.Paged)]
	[TestCase(CrudGridPageMode.Auto)]
	[TestCase(CrudGridPageMode.Single)]
	public void FooterPagingControlsAlignRightWithModeBeforePageSizeAndNavigator(CrudGridPageMode mode) => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner);
		crud.PageMode = mode;
		await BindAsync(crud);
		var footer = Find<Panel>(crud, "_bottomPanel");
		var total = Find<Control>(crud, "_totalRecordsPanel");
		var paging = Find<Control>(crud, "_pagingControlsPanel");
		var manual = Find<Control>(crud, "_manualPageSizePanel");
		var navigator = Find<Control>(crud, "_pageNavigationPanel");
		var modeSelector = Find<ComboBox>(crud, "_pageModeComboBox");
		Assert.That(total.Left, Is.EqualTo(0));
		Assert.That(paging.Right, Is.EqualTo(footer.ClientSize.Width));
		Assert.That(modeSelector.Items.Cast<string>(), Is.EqualTo(new[] { "Paged", "Auto", "Single" }));
		Assert.That(modeSelector.SelectedIndex, Is.EqualTo((int)mode));
		Assert.That(manual.Visible, Is.EqualTo(mode == CrudGridPageMode.Paged));
		Assert.That(navigator.Visible, Is.EqualTo(mode != CrudGridPageMode.Single));
		if (mode == CrudGridPageMode.Single)
			Assert.That(modeSelector.Right + modeSelector.Margin.Right, Is.EqualTo(paging.ClientSize.Width));
		else {
			Assert.That(navigator.Right, Is.EqualTo(paging.ClientSize.Width));
			if (mode == CrudGridPageMode.Paged) {
				Assert.That(modeSelector.Right, Is.LessThanOrEqualTo(manual.Left));
				Assert.That(manual.Right, Is.LessThanOrEqualTo(navigator.Left));
			} else
				Assert.That(modeSelector.Right, Is.LessThanOrEqualTo(navigator.Left));
		}
	});

	[TestCase(false)]
	[TestCase(true)]
	public void PagingAndRefreshKeepColumnWidthsAcrossDifferentLengthValues(bool stretch) => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner);
		crud.PersistSettings = false;
		crud.GridBindings = new[] {
			new CrudGridColumn<string> { ColumnName = "Value", DataType = typeof(string), PropertyValue = value => value, ExpandsToFit = stretch }
		};
		var items = new[] { "Short", new string('W', 200), "Tiny" };
		await crud.SetDataSource(new ListDataSource<string>(new ExtendedList<string>(items)));
		Find<NumericUpDown>(crud, "_pageSizeUpDown").Value = 1;
		await CompleteRefreshAsync(crud);
		var grid = Find<Grid>(crud, "_grid");
		var initialWidth = grid.Columns[0].Width;
		Find<Button>(crud, "_nextPageButton").PerformClick();
		await CompleteRefreshAsync(crud);
		Assert.That(crud.VisibleEntities.Single(), Is.EqualTo(items[1]));
		Assert.That(grid.Columns[0].Width, Is.EqualTo(initialWidth), "Page content must not change the established width.");
		grid.Columns[0].Width = 193;
		Find<Button>(crud, "_lastPageButton").PerformClick();
		await CompleteRefreshAsync(crud);
		Assert.That(grid.Columns[0].Width, Is.EqualTo(193), "An explicit user width must survive paging.");
		await CompleteRefreshAsync(crud);
		Assert.That(grid.Columns[0].Width, Is.EqualTo(193), "An explicit user width must survive refresh.");
	});

	[Test]
	public void AllFourPagerButtonsUpdateRowsAndTheDisplayedPage() => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner, "Pager-buttons");
		var source = await BindAsync(crud);
		Find<NumericUpDown>(crud, "_pageSizeUpDown").Value = 20;
		await CompleteRefreshAsync(crud);
		Find<Button>(crud, "_nextPageButton").PerformClick();
		await CompleteRefreshAsync(crud);
		Assert.That(source.Requests.Last().Page, Is.EqualTo(1), "Next must request the second page.");
		Assert.That(Find<IntBox>(crud, "_pageNumberBox").Value, Is.EqualTo(2));
		Assert.That(crud.VisibleEntities.Cast<string>().First(), Is.EqualTo("Address 0020"));
		Find<Button>(crud, "_lastPageButton").PerformClick();
		await CompleteRefreshAsync(crud);
		Assert.That(source.Requests.Last().Page, Is.EqualTo(11), "Last must request the final page.");
		Assert.That(Find<IntBox>(crud, "_pageNumberBox").Value, Is.EqualTo(12));
		Assert.That(crud.VisibleEntities.Cast<string>().Last(), Is.EqualTo("Address 0239"));
		Find<Button>(crud, "_previousPageButton").PerformClick();
		await CompleteRefreshAsync(crud);
		Assert.That(source.Requests.Last().Page, Is.EqualTo(10), "Previous must request the preceding page.");
		Assert.That(Find<IntBox>(crud, "_pageNumberBox").Value, Is.EqualTo(11));
		Assert.That(crud.VisibleEntities.Cast<string>().First(), Is.EqualTo("Address 0200"));
		Find<Button>(crud, "_firstPageButton").PerformClick();
		await CompleteRefreshAsync(crud);
		Assert.That(source.Requests.Last().Page, Is.Zero, "First must request the first page.");
		Assert.That(Find<IntBox>(crud, "_pageNumberBox").Value, Is.EqualTo(1));
		Assert.That(crud.VisibleEntities.Cast<string>().First(), Is.EqualTo("Address 0000"));
	});

	[Test]
	public void SearchingFromALaterPageRequestsAndDisplaysTheFirstFilteredPage() => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner, "Search-reset");
		var source = await BindAsync(crud);
		Find<NumericUpDown>(crud, "_pageSizeUpDown").Value = 20;
		await CompleteRefreshAsync(crud);
		Find<IntBox>(crud, "_pageNumberBox").Value = 3;
		await CompleteRefreshAsync(crud);
		Assert.That(crud.VisibleEntities.Cast<string>().First(), Is.EqualTo("Address 0040"));
		Find<SearchTextBox>(crud, "_searchTextBox").Text = "Address 0015";
		await CompleteRefreshAsync(crud);
		Assert.That(source.Requests.Last().Page, Is.Zero);
		Assert.That(Find<IntBox>(crud, "_pageNumberBox").Value, Is.EqualTo(1));
		Assert.That(crud.VisibleEntities.Cast<string>().Single(), Is.EqualTo("Address 0015"));
		Assert.That(Find<Label>(crud, "_totalRecordsLabel").Text, Is.EqualTo("1"));
	});

	[TestCase(319, 8.25f, CrudGridPageMode.Paged)]
	[TestCase(319, 18f, CrudGridPageMode.Paged)]
	[TestCase(520, 8.25f, CrudGridPageMode.Paged)]
	[TestCase(520, 18f, CrudGridPageMode.Paged)]
	[TestCase(319, 18f, CrudGridPageMode.Auto)]
	[TestCase(520, 18f, CrudGridPageMode.Auto)]
	[TestCase(319, 18f, CrudGridPageMode.Single)]
	[TestCase(520, 18f, CrudGridPageMode.Single)]
	public void FooterGroupsNeverOverlapOrClip(int width, float fontSize, CrudGridPageMode mode) => RunWithMessageLoop(async owner => {
		owner.ClientSize = new Size(width, 450);
		using var font = new Font("Segoe UI", fontSize);
		using var crud = NewGrid(owner);
		crud.Font = font;
		crud.PageMode = mode;
		await BindAsync(crud);
		var footer = Find<Panel>(crud, "_bottomPanel");
		Assert.That(footer.Parent.ClientRectangle.Contains(footer.Bounds), Is.True);
		var groups = footer.Controls.Cast<Control>().Where(control => control.Visible).ToArray();
		AssertChildrenFit(footer);
		for (var index = 0; index < groups.Length; index++)
			foreach (var other in groups.Skip(index + 1))
				Assert.That(groups[index].Bounds.IntersectsWith(other.Bounds), Is.False, $"{groups[index].Name} overlaps {other.Name}.");
	});

	[TestCase(CrudGridPageMode.Paged, 8.25f)]
	[TestCase(CrudGridPageMode.Auto, 8.25f)]
	[TestCase(CrudGridPageMode.Single, 8.25f)]
	[TestCase(CrudGridPageMode.Paged, 18f)]
	[TestCase(CrudGridPageMode.Auto, 18f)]
	[TestCase(CrudGridPageMode.Single, 18f)]
	public void FooterReturnsToOneCompactLineAfterTheViewportWidens(CrudGridPageMode mode, float fontSize) => RunWithMessageLoop(async owner => {
		owner.ClientSize = new Size(319, 600);
		using var font = new Font("Segoe UI", fontSize);
		using var crud = NewGrid(owner);
		crud.Font = font;
		crud.PageMode = mode;
		await BindAsync(crud);
		AssertChildrenFit(Find<Panel>(crud, "_bottomPanel"));
		owner.ClientSize = new Size(1400, 600);
		await CompleteRefreshAsync(crud);
		var footer = Find<TableLayoutPanel>(crud, "_bottomPanel");
		var modeSelector = Find<ComboBox>(crud, "_pageModeComboBox");
		var rowHeight = Math.Max(modeSelector.PreferredHeight, Math.Max(Find<NumericUpDown>(crud, "_pageSizeUpDown").PreferredHeight, Find<IntBox>(crud, "_pageNumberBox").PreferredHeight));
		Assert.That(footer.RowCount, Is.EqualTo(1));
		Assert.That(footer.Height, Is.LessThanOrEqualTo(rowHeight + 8), "A normal-width footer must occupy only its control height and small vertical margins.");
		Assert.That(footer.GetCellPosition(Find<Control>(crud, "_pagingControlsPanel")).Row, Is.Zero);
		AssertChildrenFit(footer);
	});

	private static void AssertChildrenFit(Control parent) {
		foreach (var child in parent.Controls.Cast<Control>().Where(control => control.Visible)) {
			Assert.That(parent.ClientRectangle.Contains(child.Bounds), Is.True, $"{child.Name} must fit inside {parent.Name} ({parent.ClientSize}).");
			AssertChildrenFit(child);
		}
	}

	[TestCase(319, 8.25f)]
	[TestCase(319, 18f)]
	[TestCase(1000, 8.25f)]
	[TestCase(1000, 18f)]
	public void SettledFooterLayoutDoesNotInvalidatePagingFlowAgain(int width, float fontSize) => RunWithMessageLoop(async owner => {
		owner.ClientSize = new Size(width, 600);
		using var font = new Font("Segoe UI", fontSize);
		using var crud = NewGrid(owner);
		crud.Font = font;
		await BindAsync(crud);
		var footer = Find<TableLayoutPanel>(crud, "_bottomPanel");
		var paging = Find<FlowLayoutPanel>(crud, "_pagingControlsPanel");
		var wrapLayouts = 0;
		paging.Layout += (_, args) => {
			if (args.AffectedProperty == nameof(FlowLayoutPanel.WrapContents))
				wrapLayouts++;
		};
		var bounds = footer.Bounds;
		var rows = footer.RowCount;
		for (var index = 0; index < 10; index++)
			footer.PerformLayout();
		Assert.That(wrapLayouts, Is.Zero, "A settled footer must not keep requesting child flow layouts while its parent lays out.");
		Assert.That(footer.Bounds, Is.EqualTo(bounds));
		Assert.That(footer.RowCount, Is.EqualTo(rows));
		AssertChildrenFit(footer);
	});

	[Test]
	public void AutoModeResizesWithoutRepeatedReadsOrPreferenceWrites() => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner, "Auto-resize");
		crud.AutoPageSize = true;
		var source = await BindAsync(crud);
		var originalCount = crud.VisibleEntities.Count();
		owner.ClientSize = new Size(850, 600);
		await CompleteRefreshAsync(crud);
		Assert.That(crud.VisibleEntities.Count(), Is.GreaterThan(originalCount));
		Assert.That(Find<NumericUpDown>(crud, "_pageSizeUpDown").Visible, Is.False);
		AssertCompleteRows(crud);
		var reads = source.Requests.Count;
		var writes = _provider.SaveCount;
		crud.PerformLayout();
		await Task.Delay(550);
		Assert.That(source.Requests.Count, Is.EqualTo(reads), "Settled layout must not continuously reread its source.");
		Assert.That(_provider.SaveCount, Is.EqualTo(writes), "Computed page capacity is not a user preference write.");
	});

	[Test]
	public void PreferencesSurviveAFreshProviderAndKeepManualPagingAndSort() => RunWithMessageLoop(async owner => {
		using (var first = NewGrid(owner, "Persistent-grid")) {
			await BindAsync(first);
			Find<NumericUpDown>(first, "_pageSizeUpDown").Value = 25;
			await CompleteRefreshAsync(first);
			await ClickHeaderAsync(first);
			await ClickHeaderAsync(first);
			Find<IntBox>(first, "_pageNumberBox").Value = 3;
			await CompleteRefreshAsync(first);
		}
		UserSettings.Provider = new CachedSettingsProvider(new DirectoryFileSettingsProvider(_directory));
		using var restored = NewGrid(owner, "Persistent-grid");
		var source = await BindAsync(restored);
		Assert.That(restored.AutoPageSize, Is.False);
		Assert.That(Find<NumericUpDown>(restored, "_pageSizeUpDown").Value, Is.EqualTo(25));
		Assert.That(source.Requests.Last().Page, Is.EqualTo(2));
		Assert.That(source.Requests.Last().Sort, Is.EqualTo("Address"));
		Assert.That(source.Requests.Last().Direction, Is.EqualTo(SortDirection.Descending));
		Assert.That(restored.VisibleEntities.Cast<string>().First(), Is.EqualTo("Address 0189"));
	});

	[Test]
	public void HeaderClicksCycleThroughAscendingDescendingAndNaturalOrderWithoutChangingPageOrWidths() => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner, "Three-state-sort");
		crud.GridBindings = new[] {
			new CrudGridColumn<SortRow> { ColumnName = "Number", SortName = nameof(SortRow.Number), DataType = typeof(int), PropertyValue = row => row.Number }
		};
		var source = new SnapshotDataSource<SortRow> { DefaultSortProperty = nameof(SortRow.Number), DefaultSortDirection = SortDirection.Descending };
		source.ReplaceSnapshot(new[] { 6, 1, 5, 2, 4, 3 }.Select(number => new SortRow { Number = number }).ToArray());
		await crud.SetDataSource(source);
		await CompleteRefreshAsync(crud);
		Find<NumericUpDown>(crud, "_pageSizeUpDown").Value = 2;
		await CompleteRefreshAsync(crud);
		Find<IntBox>(crud, "_pageNumberBox").Value = 2;
		await CompleteRefreshAsync(crud);
		var grid = Find<Grid>(crud, "_grid");
		grid.Columns[0].Width = 193;
		Assert.That(crud.VisibleEntities.Cast<SortRow>().Select(row => row.Number), Is.EqualTo(new[] { 4, 3 }), "Initial null sorting must still use the source's configured default.");
		var directions = new[] { SortDirection.Ascending, SortDirection.Descending, SortDirection.None, SortDirection.Ascending };
		var orders = new[] { new[] { 3, 4 }, new[] { 4, 3 }, new[] { 5, 2 }, new[] { 3, 4 } };
		var styles = new[] { SourceGrid.Drawing.HeaderSortStyle.Ascending, SourceGrid.Drawing.HeaderSortStyle.Descending, SourceGrid.Drawing.HeaderSortStyle.None, SourceGrid.Drawing.HeaderSortStyle.Ascending };
		for (var index = 0; index < directions.Length; index++) {
			await ClickHeaderAsync(crud);
			Assert.That(crud.VisibleEntities.Cast<SortRow>().Select(row => row.Number), Is.EqualTo(orders[index]));
			Assert.That(Find<IntBox>(crud, "_pageNumberBox").Value, Is.EqualTo(2));
			Assert.That(grid.Columns[0].Width, Is.EqualTo(193));
			var sortable = (SourceGrid.Cells.Models.ISortableHeader)grid[0, 0].Model.FindModel(typeof(SourceGrid.Cells.Models.ISortableHeader));
			Assert.That(sortable.GetSortStatus(new CellContext(grid, new Position(0, 0))).Style, Is.EqualTo(styles[index]));
			Assert.That(UserSettings.Get<CrudGridSettings>("Three-state-sort").SortDirection, Is.EqualTo(directions[index]));
		}
	});

	[Test]
	public void ClearingSortSurvivesAFreshProviderWithoutReapplyingDefaultSorting() => RunWithMessageLoop(async owner => {
		using (var first = NewGrid(owner, "Natural-sort-grid")) {
			await BindAsync(first);
			await ClickHeaderAsync(first);
			await ClickHeaderAsync(first);
			await ClickHeaderAsync(first);
			Find<IntBox>(first, "_pageNumberBox").Value = 2;
			await CompleteRefreshAsync(first);
		}
		UserSettings.Provider = new CachedSettingsProvider(new DirectoryFileSettingsProvider(_directory));
		using var restored = NewGrid(owner, "Natural-sort-grid");
		var source = await BindAsync(restored);
		Assert.That(source.Requests.Last().Sort, Is.Null);
		Assert.That(source.Requests.Last().Direction, Is.EqualTo(SortDirection.None));
		Assert.That(source.Requests.Last().Page, Is.EqualTo(1));
		var grid = Find<Grid>(restored, "_grid");
		var sortable = (SourceGrid.Cells.Models.ISortableHeader)grid[0, 0].Model.FindModel(typeof(SourceGrid.Cells.Models.ISortableHeader));
		Assert.That(sortable.GetSortStatus(new CellContext(grid, new Position(0, 0))).Style, Is.EqualTo(SourceGrid.Drawing.HeaderSortStyle.None));
	});

	[TestCase(false)]
	[TestCase(true)]
	public void AnUnchangedInstanceCannotOverwriteAnotherInstancesAcceptedPreferences(bool automatic) => RunWithMessageLoop(async owner => {
		using (var first = NewGrid(owner, "Shared-grid")) {
			await BindAsync(first);
			var unchanged = NewGrid(owner, "Shared-grid");
			await BindAsync(unchanged);
			Find<NumericUpDown>(first, "_pageSizeUpDown").Value = 25;
			await CompleteRefreshAsync(first);
			Find<IntBox>(first, "_pageNumberBox").Value = 3;
			await CompleteRefreshAsync(first);
			first.AutoPageSize = automatic;
			await CompleteRefreshAsync(first);
			var writes = _provider.SaveCount;
			unchanged.Dispose();
			Assert.That(_provider.SaveCount, Is.EqualTo(writes), "An unchanged control must not save its stale view of shared preferences.");
		}
		UserSettings.Provider = new CachedSettingsProvider(new DirectoryFileSettingsProvider(_directory));
		using var restored = NewGrid(owner, "Shared-grid");
		var source = await BindAsync(restored);
		Assert.That(restored.AutoPageSize, Is.EqualTo(automatic));
		Assert.That(Find<NumericUpDown>(restored, "_pageSizeUpDown").Value, Is.EqualTo(25));
		Assert.That(UserSettings.Get<CrudGridSettings>("Shared-grid").PageSize, Is.EqualTo(25), "Fitted capacity must not replace the manual preference.");
		Assert.That(source.Requests.Last().Page, Is.EqualTo(automatic ? 0 : 2));
	});

	[Test]
	public void RestoringMissingColumnsDoesNotMutateTheSharedSettingsObject() => RunWithMessageLoop(async owner => {
		var settings = UserSettings.Get<CrudGridSettings>("Shared-columns");
		settings.SortColumnName = "Address";
		settings.SortDirection = SortDirection.Descending;
		settings.Save();
		using (var other = NewGrid(owner, "Shared-columns")) {
			other.GridBindings = new[] {
				new CrudGridColumn<string> { ColumnName = "Other", SortName = "Other", DataType = typeof(string), PropertyValue = value => value }
			};
			await other.SetDataSource(new RecordingSource(240));
			await CompleteRefreshAsync(other);
			Assert.That(settings.SortColumnName, Is.EqualTo("Address"));
			Assert.That(settings.SortDirection, Is.EqualTo(SortDirection.Descending));
		}
		Assert.That(_provider.SaveCount, Is.EqualTo(1), "Restore and disposal must not write normalized defaults over another column layout.");
	});

	[Test]
	public void DisposalRetriesAnAcceptedPreferenceWriteThatFailed() => RunWithMessageLoop(async owner => {
		using (var crud = NewGrid(owner, "Retry-grid")) {
			await BindAsync(crud);
			_provider.FailNextSave = true;
			crud.AutoPageSize = true;
			await CompleteRefreshAsync(crud);
			Assert.That(_provider.SaveCount, Is.EqualTo(1));
		}
		Assert.That(_provider.SaveCount, Is.EqualTo(2));
		UserSettings.Provider = new CachedSettingsProvider(new DirectoryFileSettingsProvider(_directory));
		Assert.That(UserSettings.Get<CrudGridSettings>("Retry-grid").AutoPageSize, Is.True);
	});

	[Test]
	public void PersistedAutoModeRetainsItsManualSizeForLaterUse() => RunWithMessageLoop(async owner => {
		using (var first = NewGrid(owner, "Automatic-grid")) {
			await BindAsync(first);
			Find<NumericUpDown>(first, "_pageSizeUpDown").Value = 41;
			await CompleteRefreshAsync(first);
			first.AutoPageSize = true;
			await CompleteRefreshAsync(first);
		}
		UserSettings.Provider = new CachedSettingsProvider(new DirectoryFileSettingsProvider(_directory));
		using var restored = NewGrid(owner, "Automatic-grid");
		var source = await BindAsync(restored);
		Assert.That(restored.AutoPageSize, Is.True);
		Assert.That(source.Requests.Last().Page, Is.Zero);
		Assert.That(Find<NumericUpDown>(restored, "_pageSizeUpDown").Visible, Is.False);
		Assert.That(UserSettings.Get<CrudGridSettings>("Automatic-grid").PageSize, Is.EqualTo(41));
		AssertCompleteRows(restored);
		restored.AutoPageSize = false;
		await CompleteRefreshAsync(restored);
		Assert.That(restored.VisibleEntities.Count(), Is.EqualTo(41));
	});

	[Test]
	public void DelayedOldReadCannotOverwriteNewPageOrSorting() => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner, "In-flight-grid");
		var source = new DelayedRecordingSource(240);
		await BindAsync(crud, source);
		Find<NumericUpDown>(crud, "_pageSizeUpDown").Value = 25;
		await CompleteRefreshAsync(crud);
		var previousRows = crud.VisibleEntities.ToArray();
		source.DelayNextRead();
		var pending = crud.RefreshGrid();
		await source.ReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
		var firstSort = ClickHeaderAsync(crud);
		var secondSort = ClickHeaderAsync(crud);
		Find<IntBox>(crud, "_pageNumberBox").Value = 3;
		Assert.That(crud.VisibleEntities, Is.EqualTo(previousRows), "An in-flight read must retain the displayed row mapping.");
		source.ContinueRead.SetResult();
		await Task.WhenAll(pending, firstSort, secondSort);
		await CompleteRefreshAsync(crud);
		Assert.That(source.Requests.Last().Page, Is.EqualTo(2));
		Assert.That(source.Requests.Last().Sort, Is.EqualTo("Address"));
		Assert.That(source.Requests.Last().Direction, Is.EqualTo(SortDirection.Descending));
		Assert.That(Find<IntBox>(crud, "_pageNumberBox").Value, Is.EqualTo(3));
		Assert.That(crud.VisibleEntities.Cast<string>().First(), Is.EqualTo("Address 0189"));
		UserSettings.Provider = new CachedSettingsProvider(new DirectoryFileSettingsProvider(_directory));
		var saved = UserSettings.Get<CrudGridSettings>("In-flight-grid");
		Assert.That(saved.PageNumber, Is.EqualTo(2));
		Assert.That(saved.SortDirection, Is.EqualTo(SortDirection.Descending));
	});

	[TestCase(-100, -1, "Removed column", 999, 1)]
	[TestCase(100000, int.MaxValue, "Address", 999, 9999)]
	public void InvalidStoredOptionsAreNormalizedBeforeTheSourceIsRead(int pageSize, int pageNumber, string sort, int direction, int expectedPageSize) => RunWithMessageLoop(async owner => {
		var settings = UserSettings.Get<CrudGridSettings>("Invalid-grid");
		settings.PageSize = pageSize;
		settings.PageNumber = pageNumber;
		settings.SortColumnName = sort;
		settings.SortDirection = (SortDirection)direction;
		settings.Save();
		UserSettings.Provider = new CachedSettingsProvider(new DirectoryFileSettingsProvider(_directory));
		using var crud = NewGrid(owner, "Invalid-grid");
		var source = await BindAsync(crud, 0);
		Assert.That(source.Requests.Last().Length, Is.EqualTo(expectedPageSize));
		Assert.That(source.Requests.Last().Page, Is.Zero);
		Assert.That(source.Requests.Last().Direction, Is.EqualTo(SortDirection.Ascending));
		Assert.That(crud.VisibleEntities, Is.Empty);
	});

	[Test]
	public void ConsumerDefaultsAreNotOverwrittenOnFirstUseAndOptOutNeverWrites() => RunWithMessageLoop(async owner => {
		using (var crud = NewGrid(owner, "Default-grid")) {
			crud.AutoPageSize = true;
			await BindAsync(crud);
			Assert.That(crud.AutoPageSize, Is.True);
		}
		Assert.That(Directory.Exists(_directory), Is.False, "Defaults alone must not create preference files.");
		using (var transient = NewGrid(owner, "Transient-grid")) {
			transient.PersistSettings = false;
			await BindAsync(transient);
			Find<NumericUpDown>(transient, "_pageSizeUpDown").Value = 17;
			await CompleteRefreshAsync(transient);
			transient.AutoPageSize = true;
			await CompleteRefreshAsync(transient);
		}
		Assert.That(Directory.Exists(_directory), Is.False);
	});

	[Test]
	public void OwnerAndNamedChildKeysSurviveMovingTheScreenBetweenForms() => RunWithMessageLoop(async owner => {
		using (var screen = new PreferenceScreen { Dock = DockStyle.Fill }) {
			owner.Controls.Add(screen);
			using var crud = new CrudGrid { Name = "Events", Dock = DockStyle.Fill };
			screen.Controls.Add(crud);
			await BindAsync(crud);
			Find<NumericUpDown>(crud, "_pageSizeUpDown").Value = 19;
			await CompleteRefreshAsync(crud);
			using var detached = new Form { Name = "Detached window", ClientSize = new Size(600, 350) };
			detached.Controls.Add(screen);
			Find<NumericUpDown>(crud, "_pageSizeUpDown").Value = 23;
			await CompleteRefreshAsync(crud);
			owner.Controls.Add(screen);
		}
		UserSettings.Provider = new CachedSettingsProvider(new DirectoryFileSettingsProvider(_directory));
		using var restoredScreen = new PreferenceScreen { Dock = DockStyle.Fill };
		owner.Controls.Add(restoredScreen);
		using var restored = new CrudGrid { Name = "Events", Dock = DockStyle.Fill };
		restoredScreen.Controls.Add(restored);
		await BindAsync(restored);
		Assert.That(restored.VisibleEntities.Count(), Is.EqualTo(23));
		Assert.That(Directory.GetFiles(_directory, "*.setting").Length, Is.EqualTo(1), "Undocking must not create a second identity.");
	});

	private static CrudGrid NewGrid(Form owner, string key = "") {
		var crud = new CrudGrid { Dock = DockStyle.Fill, SettingsKey = key };
		owner.Controls.Add(crud);
		return crud;
	}

	private static async Task<RecordingSource> BindAsync(CrudGrid crud, int count = 240) {
		return await BindAsync(crud, new RecordingSource(count));
	}

	private static async Task<RecordingSource> BindAsync(CrudGrid crud, RecordingSource source) {
		crud.GridBindings = new[] {
			new CrudGridColumn<string> { ColumnName = "Address", SortName = "Address", DataType = typeof(string), PropertyValue = value => value, ExpandsToFit = true }
		};
		await crud.SetDataSource(source);
		await CompleteRefreshAsync(crud);
		return source;
	}

	private static async Task CompleteRefreshAsync(CrudGrid crud) {
		await crud.RefreshGrid();
		await Task.Delay(300);
		await crud.RefreshGrid();
	}

	private static async Task ClickHeaderAsync(CrudGrid crud) {
		var grid = Find<Grid>(crud, "_grid");
		var position = new Position(0, 0);
		var bounds = grid.PositionToRectangle(position);
		grid[0, 0].Controller.OnMouseUp(new CellContext(grid, position),
			new MouseEventArgs(MouseButtons.Left, 1, bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2, 0));
		await CompleteRefreshAsync(crud);
	}

	private static void AssertCompleteRows(CrudGrid crud) {
		var grid = Find<Grid>(crud, "_grid");
		Assert.That(crud.VisibleEntities.Count(), Is.GreaterThanOrEqualTo(1));
		Assert.That(grid.VScrollBarVisible, Is.False);
		Assert.That(grid.Rows.Sum(row => row.Height), Is.LessThanOrEqualTo(grid.DisplayRectangle.Height));
	}

	private static T Find<T>(Control control, string name)
		where T : Control => (T)control.Controls.Find(name, true).Single();

	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var previousContext = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restoreContext = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previousContext));
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000), ClientSize = new Size(850, 400) };
		using var watchdog = new System.Windows.Forms.Timer { Interval = 20000 };
		watchdog.Tick += (_, _) => {
			failure = new AssertionException("The CRUD grid auto-page test timed out.");
			owner.Close();
		};
		owner.Shown += async (_, _) => {
			using var close = Tools.Scope.ExecuteOnDispose(owner.Close);
			try {
				await test(owner);
			} catch (Exception error) {
				failure = error;
			}
		};
		watchdog.Start();
		FormsApplication.Run(owner);
		Assert.That(failure, Is.Null, failure?.ToString());
	}

	private sealed class UpgradedGrid : CrudGrid {
		protected override void UpgradeGridSettings(CrudGridSettings settings) {
			if (settings.ConsumerVersion >= 1)
				return;
			if (settings.PageMode == CrudGridPageMode.Single)
				settings.PageMode = CrudGridPageMode.Auto;
			settings.ConsumerVersion = 1;
		}
	}

	private sealed class CountingProvider : DirectoryFileSettingsProvider {
		public CountingProvider(string directory)
			: base(directory) {
		}

		public int SaveCount { get; private set; }

		public bool FailNextSave { get; set; }

		public override void SaveSetting(SettingsObject settings) {
			SaveCount++;
			if (FailNextSave) {
				FailNextSave = false;
				throw new IOException("Simulated optional preference write failure.");
			}
			base.SaveSetting(settings);
		}
	}

	private sealed class PreferenceScreen : UserControl {
	}

	private sealed class SortRow {
		public int Number { get; set; }
	}

	private class RecordingSource : ListDataSource<string> {
		private readonly string[] _items;

		public RecordingSource(int count)
			: base(new ExtendedList<string>()) {
			_items = Enumerable.Range(0, count).Select(index => $"Address {index:D4}").ToArray();
		}

		public List<(int Length, int Page, string Sort, SortDirection Direction)> Requests { get; } = new();

		public override Task<DataSourceItems<string>> ReadRangeAsync(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection) {
			Requests.Add((pageLength, page, sortProperty, sortDirection));
			var query = sortDirection == SortDirection.Descending ? _items.Reverse() : _items.AsEnumerable();
			if (!string.IsNullOrWhiteSpace(searchTerm))
				query = query.Where(item => item.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
			var filtered = query.ToArray();
			var lastPage = Math.Max(0, (filtered.Length - 1) / pageLength);
			page = Math.Min(page, lastPage);
			return Task.FromResult(new DataSourceItems<string> { Items = filtered.Skip(page * pageLength).Take(pageLength).ToArray(), Page = page, TotalCount = filtered.Length });
		}
	}

	private sealed class DelayedRecordingSource : RecordingSource {
		private bool _delayNext;

		public DelayedRecordingSource(int count)
			: base(count) {
		}

		public TaskCompletionSource ReadStarted { get; private set; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public TaskCompletionSource ContinueRead { get; private set; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public void DelayNextRead() {
			ReadStarted = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			ContinueRead = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			_delayNext = true;
		}

		public override async Task<DataSourceItems<string>> ReadRangeAsync(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection) {
			var result = await base.ReadRangeAsync(searchTerm, pageLength, page, sortProperty, sortDirection);
			if (_delayNext) {
				_delayNext = false;
				ReadStarted.SetResult();
				await ContinueRead.Task;
			}
			return result;
		}
	}
}
