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
public class VisualGridAutoPageSizeTests {
	private ISettingsProvider _previousProvider;
	private string _directory;
	private CountingProvider _provider;

	[SetUp]
	public void SetUp() {
		_previousProvider = (ISettingsProvider)typeof(UserSettings).GetField("_provider", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
		_directory = Path.Combine(Path.GetTempPath(), $"Sphere10-VisualGrid-{Guid.NewGuid():N}");
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
		var mode = ModeSelector(crud);
		mode.SelectedItem = VisualGridPageMode.Auto;
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
		mode.SelectedItem = VisualGridPageMode.Paged;
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
		using var crud = NewGrid(owner);
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
	public void CapturedAutoPageSelectionCanBeAppliedToAnotherGrid() {
		RunWithMessageLoop(async owner => {
			VisualGridSettings values;
			using (var first = NewGrid(owner)) {
				first.AutoPageSize = true;
				await BindAsync(first);
				Find<IntBox>(first, "_pageNumberBox").Value = 3;
				await CompleteRefreshAsync(first);
				values = first.CaptureGridPreferences();
			}
			using var restored = NewGrid(owner);
			restored.ApplyGridPreferences(values);
			var source = await BindAsync(restored);
			Assert.That(restored.AutoPageSize, Is.True);
			Assert.That(source.Requests.Last().Page, Is.EqualTo(2));
			Assert.That(Find<IntBox>(restored, "_pageNumberBox").Value, Is.EqualTo(3));
		});
	}

	[TestCase(VisualGridPageMode.Paged)]
	[TestCase(VisualGridPageMode.Auto)]
	[TestCase(VisualGridPageMode.Single)]
	public void OwnerValuesRetainTheSelectedModeAndManualSize(VisualGridPageMode mode) {
		RunWithMessageLoop(async owner => {
			VisualGridSettings values;
			using (var first = NewGrid(owner)) {
				await BindAsync(first);
				Find<NumericUpDown>(first, "_pageSizeUpDown").Value = 41;
				await CompleteRefreshAsync(first);
				ModeSelector(first).SelectedItem = mode;
				await CompleteRefreshAsync(first);
				values = first.CaptureGridPreferences();
			}
			using var restored = NewGrid(owner);
			restored.ApplyGridPreferences(values);
			var source = await BindAsync(restored);
			Assert.That(restored.PageMode, Is.EqualTo(mode));
			Assert.That(values.PageMode, Is.EqualTo(mode));
			Assert.That(values.PageSize, Is.EqualTo(41));
			Assert.That(source.Requests.Last().Page, Is.Zero);
			if (mode == VisualGridPageMode.Single)
				Assert.That(restored.VisibleEntities.Count(), Is.EqualTo(240));
			restored.PageMode = VisualGridPageMode.Paged;
			await CompleteRefreshAsync(restored);
			Assert.That(restored.VisibleEntities.Count(), Is.EqualTo(41));
			Assert.That(Find<NumericUpDown>(restored, "_pageSizeUpDown").Value, Is.EqualTo(41));
		});
	}

	[TestCase(false, null, VisualGridPageMode.Paged)]
	[TestCase(true, null, VisualGridPageMode.Auto)]
	[TestCase(false, 999, VisualGridPageMode.Paged)]
	[TestCase(true, 999, VisualGridPageMode.Auto)]
	public void OldOrInvalidSuppliedModesFallBackToTheLegacyAutoPreference(bool automatic, int? storedMode, VisualGridPageMode expected) {
		RunWithMessageLoop(async owner => {
			var settings = new VisualGridSettings();
			settings.AutoPageSize = automatic;
			settings.PageMode = storedMode.HasValue ? (VisualGridPageMode)storedMode.Value : null;
			settings.PageSize = 23;
			using var crud = NewGrid(owner);
			crud.ApplyGridPreferences(settings);
			await BindAsync(crud);
			Assert.That(crud.PageMode, Is.EqualTo(expected));
			Assert.That(ModeSelector(crud).SelectedItem, Is.EqualTo(expected));
			Assert.That(_provider.SaveCount, Is.Zero, "Applying values does not write settings.");
			crud.AutoPageSize = false;
			await CompleteRefreshAsync(crud);
			Assert.That(crud.PageMode, Is.EqualTo(VisualGridPageMode.Paged));
			Assert.That(crud.VisibleEntities.Count(), Is.EqualTo(23));
		});
	}

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
		var mode = ModeSelector(crud);
		mode.SelectedItem = VisualGridPageMode.Single;
		await CompleteRefreshAsync(crud);
		Assert.That(crud.PageMode, Is.EqualTo(VisualGridPageMode.Single));
		Assert.That(source.Requests.Last().Length, Is.EqualTo(int.MaxValue));
		Assert.That(source.Requests.Last().Page, Is.Zero);
		Assert.That(crud.VisibleEntities.Count(), Is.EqualTo(240));
		Assert.That(grid.Rows.Count, Is.EqualTo(241), "Single must bind every matching row plus its header.");
		Assert.That(grid.VScrollBarVisible, Is.True);
		Assert.That(Find<Control>(crud, "_pageNavigationPanel").Visible, Is.False);
		Assert.That(Find<Control>(crud, "_manualPageSizePanel").Visible, Is.False);
		Assert.That(grid.Columns.Cast<ColumnInfo>().Select(column => column.Width), Is.EqualTo(widths));
		mode.SelectedItem = VisualGridPageMode.Auto;
		await CompleteRefreshAsync(crud);
		Assert.That(Find<Control>(crud, "_pageNavigationPanel").Visible, Is.True);
		AssertCompleteRows(crud);
		mode.SelectedItem = VisualGridPageMode.Paged;
		await CompleteRefreshAsync(crud);
		Assert.That(crud.VisibleEntities.Count(), Is.EqualTo(25));
		Assert.That(source.Requests.Last().Page, Is.Zero);
		Assert.That(Find<NumericUpDown>(crud, "_pageSizeUpDown").Value, Is.EqualTo(25));
		Assert.That(grid.Columns.Cast<ColumnInfo>().Select(column => column.Width), Is.EqualTo(widths));
	});

	[Test]
	public void SingleModeKeepsSortingAndSearchingOverTheCompleteResult() => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner);
		crud.PageMode = VisualGridPageMode.Single;
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

	[TestCase(VisualGridPageMode.Paged)]
	[TestCase(VisualGridPageMode.Auto)]
	[TestCase(VisualGridPageMode.Single)]
	public void NonpageableSourcesKeepTheirExistingCompleteResultBehavior(VisualGridPageMode mode) => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner);
		crud.PageMode = mode;
		crud.GridBindings = new[] { new VisualGridColumn<string> { ColumnName = "Address", DataType = typeof(string), PropertyValue = value => value } };
		var source = new RecordingSource(240);
		await crud.SetDataSource(source, DataSourceCapabilities.ComboBoxDefault);
		await CompleteRefreshAsync(crud);
		Assert.That(source.Requests, Is.Not.Empty);
		Assert.That(source.Requests.Last().Length, Is.EqualTo(int.MaxValue));
		Assert.That(source.Requests.Last().Page, Is.Zero);
		Assert.That(crud.VisibleEntities.Count(), Is.EqualTo(240));
		Assert.That(ModeSelector(crud).Visible, Is.False);
		Assert.That(crud.Capabilities.HasFlag(DataSourceCapabilities.CanPage), Is.False);
		Assert.That(crud.Capabilities, Is.EqualTo(DataSourceCapabilities.ComboBoxDefault | DataSourceCapabilities.CanNotify), "A CRUD list notifies after its own mutations; that is independent of the requested mask.");
	});

	[Test]
	public void ChangingToSingleDuringAnOldReadDiscardsThePagedResult() => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner);
		var source = new DelayedRecordingSource(240);
		await BindAsync(crud, source);
		source.DelayNextRead();
		var pending = crud.RefreshGrid();
		await source.ReadStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
		crud.PageMode = VisualGridPageMode.Single;
		source.ContinueRead.SetResult();
		await pending;
		await CompleteRefreshAsync(crud);
		Assert.That(source.Requests.Last().Length, Is.EqualTo(int.MaxValue));
		Assert.That(source.Requests.Last().Page, Is.Zero);
		Assert.That(crud.VisibleEntities.Count(), Is.EqualTo(240));
	});

	[Test]
	public void AutoPageSizeCompatibilityAliasSelectsAutoOrPagedAndInvalidModesAreRejected() {
		using var crud = new VisualGrid();
		crud.AutoPageSize = true;
		Assert.That(crud.PageMode, Is.EqualTo(VisualGridPageMode.Auto));
		crud.PageMode = VisualGridPageMode.Single;
		Assert.That(crud.AutoPageSize, Is.False);
		crud.AutoPageSize = false;
		Assert.That(crud.PageMode, Is.EqualTo(VisualGridPageMode.Paged));
		Assert.That(() => crud.PageMode = (VisualGridPageMode)999, Throws.ArgumentException);
	}

	[TestCase(VisualGridPageMode.Paged)]
	[TestCase(VisualGridPageMode.Auto)]
	[TestCase(VisualGridPageMode.Single)]
	public void FooterPagingControlsAlignRightWithModeBeforePageSizeAndNavigator(VisualGridPageMode mode) => RunWithMessageLoop(async owner => {
		using var crud = NewGrid(owner);
		crud.PageMode = mode;
		await BindAsync(crud);
		var footer = Find<Panel>(crud, "_bottomPanel");
		var total = Find<Control>(crud, "_totalRecordsPanel");
		var paging = Find<Control>(crud, "_pagingControlsPanel");
		var manual = Find<Control>(crud, "_manualPageSizePanel");
		var navigator = Find<Control>(crud, "_pageNavigationPanel");
		var modeSelector = Find<ToolStrip>(crud, "_pageModeToolStrip");
		var modeButton = ModeSelector(crud);
		Assert.That(total.Left, Is.EqualTo(0));
		Assert.That(paging.Right, Is.EqualTo(footer.ClientSize.Width));
		Assert.That(modeButton.DropDownItems.Cast<ToolStripMenuItem>().Select(item => item.Text), Is.EqualTo(new[] { "Single", "Auto", "Manual" }));
		Assert.That(modeButton.SelectedItem, Is.EqualTo(mode));
		Assert.That(manual.Visible, Is.EqualTo(mode == VisualGridPageMode.Paged));
		Assert.That(navigator.Visible, Is.EqualTo(mode != VisualGridPageMode.Single));
		if (mode == VisualGridPageMode.Single)
			Assert.That(modeSelector.Right + modeSelector.Margin.Right, Is.EqualTo(paging.ClientSize.Width));
		else {
			Assert.That(navigator.Right, Is.EqualTo(paging.ClientSize.Width));
			if (mode == VisualGridPageMode.Paged) {
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
		crud.GridBindings = new[] {
			new VisualGridColumn<string> { ColumnName = "Value", DataType = typeof(string), PropertyValue = value => value, ExpandsToFit = stretch }
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
		using var crud = NewGrid(owner);
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
		using var crud = NewGrid(owner);
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

	[TestCase(319, 8.25f, VisualGridPageMode.Paged)]
	[TestCase(319, 18f, VisualGridPageMode.Paged)]
	[TestCase(520, 8.25f, VisualGridPageMode.Paged)]
	[TestCase(520, 18f, VisualGridPageMode.Paged)]
	[TestCase(319, 18f, VisualGridPageMode.Auto)]
	[TestCase(520, 18f, VisualGridPageMode.Auto)]
	[TestCase(319, 18f, VisualGridPageMode.Single)]
	[TestCase(520, 18f, VisualGridPageMode.Single)]
	public void FooterGroupsNeverOverlapOrClip(int width, float fontSize, VisualGridPageMode mode) => RunWithMessageLoop(async owner => {
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

	[TestCase(VisualGridPageMode.Paged, 8.25f)]
	[TestCase(VisualGridPageMode.Auto, 8.25f)]
	[TestCase(VisualGridPageMode.Single, 8.25f)]
	[TestCase(VisualGridPageMode.Paged, 18f)]
	[TestCase(VisualGridPageMode.Auto, 18f)]
	[TestCase(VisualGridPageMode.Single, 18f)]
	public void FooterReturnsToOneCompactLineAfterTheViewportWidens(VisualGridPageMode mode, float fontSize) => RunWithMessageLoop(async owner => {
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
		var modeSelector = Find<ToolStrip>(crud, "_pageModeToolStrip");
		var rowHeight = Math.Max(modeSelector.Height, Math.Max(Find<NumericUpDown>(crud, "_pageSizeUpDown").PreferredHeight, Find<IntBox>(crud, "_pageNumberBox").PreferredHeight));
		Assert.That(footer.RowCount, Is.EqualTo(1));
		Assert.That(footer.Height, Is.LessThanOrEqualTo(rowHeight + 8), "A normal-width footer must occupy only its control height and small vertical margins.");
		Assert.That(footer.GetCellPosition(Find<Control>(crud, "_pagingControlsPanel")).Row, Is.Zero);
		AssertChildrenFit(footer);
	});

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
		using var crud = NewGrid(owner);
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
	public void CapturedPreferencesKeepManualPagingAndSort() {
		RunWithMessageLoop(async owner => {
			VisualGridSettings values;
			using (var first = NewGrid(owner)) {
				await BindAsync(first);
				Find<NumericUpDown>(first, "_pageSizeUpDown").Value = 25;
				await CompleteRefreshAsync(first);
				await ClickHeaderAsync(first);
				await ClickHeaderAsync(first);
				Find<IntBox>(first, "_pageNumberBox").Value = 3;
				await CompleteRefreshAsync(first);
				values = first.CaptureGridPreferences();
			}
			using var restored = NewGrid(owner);
			restored.ApplyGridPreferences(values);
			var source = await BindAsync(restored);
			Assert.That(restored.AutoPageSize, Is.False);
			Assert.That(Find<NumericUpDown>(restored, "_pageSizeUpDown").Value, Is.EqualTo(25));
			Assert.That(source.Requests.Last().Page, Is.EqualTo(2));
			Assert.That(source.Requests.Last().Sort, Is.EqualTo("Address"));
			Assert.That(source.Requests.Last().Direction, Is.EqualTo(SortDirection.Descending));
			Assert.That(restored.VisibleEntities.Cast<string>().First(), Is.EqualTo("Address 0189"));
		});
	}

	[Test]
	public void HeaderClicksCycleThroughAscendingDescendingAndNaturalOrderWithoutChangingPageOrWidths() {
		RunWithMessageLoop(async owner => {
			using var crud = NewGrid(owner);
			crud.GridBindings = new[] {
				new VisualGridColumn<SortRow> { ColumnName = "Number", SortName = nameof(SortRow.Number), DataType = typeof(int), PropertyValue = row => row.Number }
			};
			var source = new ArrayDataSource<SortRow> { DefaultSortProperty = nameof(SortRow.Number), DefaultSortDirection = SortDirection.Descending };
			source.ReplaceData(new[] { 6, 1, 5, 2, 4, 3 }.Select(number => new SortRow { Number = number }).ToArray());
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
			var styles = new[] { Sphere10.Framework.Windows.Forms.Drawing.HeaderSortStyle.Ascending, Sphere10.Framework.Windows.Forms.Drawing.HeaderSortStyle.Descending, Sphere10.Framework.Windows.Forms.Drawing.HeaderSortStyle.None, Sphere10.Framework.Windows.Forms.Drawing.HeaderSortStyle.Ascending };
			for (var index = 0; index < directions.Length; index++) {
				await ClickHeaderAsync(crud);
				Assert.That(crud.VisibleEntities.Cast<SortRow>().Select(row => row.Number), Is.EqualTo(orders[index]));
				Assert.That(Find<IntBox>(crud, "_pageNumberBox").Value, Is.EqualTo(2));
				Assert.That(grid.Columns[0].Width, Is.EqualTo(193));
				var sortable = (SourceGrid.Cells.Models.ISortableHeader)grid[0, 0].Model.FindModel(typeof(SourceGrid.Cells.Models.ISortableHeader));
				Assert.That(sortable.GetSortStatus(new CellContext(grid, new Position(0, 0))).Style, Is.EqualTo(styles[index]));
				Assert.That(crud.CaptureGridPreferences().SortDirection, Is.EqualTo(directions[index]));
			}
		});
	}

	[Test]
	public void CapturedClearedSortDoesNotReapplyDefaultSorting() {
		RunWithMessageLoop(async owner => {
			VisualGridSettings values;
			using (var first = NewGrid(owner)) {
				await BindAsync(first);
				await ClickHeaderAsync(first);
				await ClickHeaderAsync(first);
				await ClickHeaderAsync(first);
				Find<IntBox>(first, "_pageNumberBox").Value = 2;
				await CompleteRefreshAsync(first);
				values = first.CaptureGridPreferences();
			}
			using var restored = NewGrid(owner);
			restored.ApplyGridPreferences(values);
			var source = await BindAsync(restored);
			Assert.That(source.Requests.Last().Sort, Is.Null);
			Assert.That(source.Requests.Last().Direction, Is.EqualTo(SortDirection.None));
			Assert.That(source.Requests.Last().Page, Is.EqualTo(1));
			var grid = Find<Grid>(restored, "_grid");
			var sortable = (SourceGrid.Cells.Models.ISortableHeader)grid[0, 0].Model.FindModel(typeof(SourceGrid.Cells.Models.ISortableHeader));
			Assert.That(sortable.GetSortStatus(new CellContext(grid, new Position(0, 0))).Style, Is.EqualTo(Sphere10.Framework.Windows.Forms.Drawing.HeaderSortStyle.None));
		});
	}

	[Test]
	public void ApplyingValuesForMissingColumnsDoesNotMutateTheSuppliedObject() {
		RunWithMessageLoop(async owner => {
			var settings = new VisualGridSettings();
			settings.SortColumnName = "Address";
			settings.SortDirection = SortDirection.Descending;
			using (var other = NewGrid(owner)) {
				other.Preferences = settings;
				other.GridBindings = new[] {
					new VisualGridColumn<string> { ColumnName = "Other", SortName = "Other", DataType = typeof(string), PropertyValue = value => value }
				};
				await other.SetDataSource(new RecordingSource(240));
				await CompleteRefreshAsync(other);
				Assert.That(settings.SortColumnName, Is.EqualTo("Address"));
				Assert.That(settings.SortDirection, Is.EqualTo(SortDirection.Descending));
			}
			Assert.That(_provider.SaveCount, Is.Zero, "Applying values and disposal must not write settings.");
		});
	}

	[Test]
	public void CapturedAutoModeRetainsItsManualSizeForLaterUse() {
		RunWithMessageLoop(async owner => {
			VisualGridSettings values;
			using (var first = NewGrid(owner)) {
				await BindAsync(first);
				Find<NumericUpDown>(first, "_pageSizeUpDown").Value = 41;
				await CompleteRefreshAsync(first);
				first.AutoPageSize = true;
				await CompleteRefreshAsync(first);
				values = first.CaptureGridPreferences();
			}
			using var restored = NewGrid(owner);
			restored.ApplyGridPreferences(values);
			var source = await BindAsync(restored);
			Assert.That(restored.AutoPageSize, Is.True);
			Assert.That(source.Requests.Last().Page, Is.Zero);
			Assert.That(Find<NumericUpDown>(restored, "_pageSizeUpDown").Visible, Is.False);
			Assert.That(values.PageSize, Is.EqualTo(41));
			AssertCompleteRows(restored);
			restored.AutoPageSize = false;
			await CompleteRefreshAsync(restored);
			Assert.That(restored.VisibleEntities.Count(), Is.EqualTo(41));
		});
	}

	[Test]
	public void DelayedOldReadCannotOverwriteNewPageOrSorting() {
		RunWithMessageLoop(async owner => {
			using var crud = NewGrid(owner);
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
			var saved = crud.CaptureGridPreferences();
			Assert.That(saved.PageNumber, Is.EqualTo(2));
			Assert.That(saved.SortDirection, Is.EqualTo(SortDirection.Descending));
		});
	}

	[TestCase(-100, -1, "Removed column", 999, 1)]
	[TestCase(100000, int.MaxValue, "Address", 999, 9999)]
	public void SuppliedOptionsAreNormalizedBeforeTheSourceIsRead(int pageSize, int pageNumber, string sort, int direction, int expectedPageSize) {
		RunWithMessageLoop(async owner => {
			var settings = new VisualGridSettings();
			settings.PageSize = pageSize;
			settings.PageNumber = pageNumber;
			settings.SortColumnName = sort;
			settings.SortDirection = (SortDirection)direction;
			using var crud = NewGrid(owner);
			crud.ApplyGridPreferences(settings);
			var source = await BindAsync(crud, 0);
			Assert.That(source.Requests.Last().Length, Is.EqualTo(expectedPageSize));
			Assert.That(source.Requests.Last().Page, Is.Zero);
			Assert.That(source.Requests.Last().Direction, Is.EqualTo(SortDirection.Ascending));
			Assert.That(crud.VisibleEntities, Is.Empty);
		});
	}

	[Test]
	public void GridDefaultsAndOperationalChangesDoNotWriteSettings() {
		RunWithMessageLoop(async owner => {
			using (var crud = NewGrid(owner)) {
				crud.AutoPageSize = true;
				await BindAsync(crud);
				Assert.That(crud.AutoPageSize, Is.True);
			}
			Assert.That(Directory.Exists(_directory), Is.False, "Defaults alone must not create preference files.");
			using (var transient = NewGrid(owner)) {
				await BindAsync(transient);
				Find<NumericUpDown>(transient, "_pageSizeUpDown").Value = 17;
				await CompleteRefreshAsync(transient);
				transient.AutoPageSize = true;
				await CompleteRefreshAsync(transient);
			}
			Assert.That(Directory.Exists(_directory), Is.False);
		});
	}

	private static ToolStripChoiceSelector ModeSelector(VisualGrid grid) => Find<ToolStrip>(grid, "_pageModeToolStrip").Items.OfType<ToolStripChoiceSelector>().Single();

	private static void AssertChildrenFit(Control parent) {
		foreach (var child in parent.Controls.Cast<Control>().Where(control => control.Visible)) {
			Assert.That(parent.ClientRectangle.Contains(child.Bounds), Is.True, $"{child.Name} must fit inside {parent.Name} ({parent.ClientSize}).");
			AssertChildrenFit(child);
		}
	}

	private static VisualGrid NewGrid(Form owner) {
		var crud = new VisualGrid { Dock = DockStyle.Fill };
		owner.Controls.Add(crud);
		return crud;
	}

	private static async Task<RecordingSource> BindAsync(VisualGrid crud, int count = 240) => await BindAsync(crud, new RecordingSource(count));

	private static async Task<RecordingSource> BindAsync(VisualGrid crud, RecordingSource source) {
		crud.GridBindings = new[] {
			new VisualGridColumn<string> { ColumnName = "Address", SortName = "Address", DataType = typeof(string), PropertyValue = value => value, ExpandsToFit = true }
		};
		await crud.SetDataSource(source);
		await CompleteRefreshAsync(crud);
		return source;
	}

	private static async Task CompleteRefreshAsync(VisualGrid crud) {
		await crud.RefreshGrid();
		await Task.Delay(300);
		await crud.RefreshGrid();
	}

	private static async Task ClickHeaderAsync(VisualGrid crud) {
		var grid = Find<Grid>(crud, "_grid");
		var position = new Position(0, 0);
		var bounds = grid.PositionToRectangle(position);
		grid[0, 0].Controller.OnMouseUp(new CellContext(grid, position),
			new MouseEventArgs(MouseButtons.Left, 1, bounds.Left + bounds.Width / 2, bounds.Top + bounds.Height / 2, 0));
		await CompleteRefreshAsync(crud);
	}

	private static void AssertCompleteRows(VisualGrid crud) {
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

	// Observes any accidental control-owned writes while grid operations run.
	private sealed class CountingProvider : DirectoryFileSettingsProvider {
		public CountingProvider(string directory)
			: base(directory) {
		}

		public int SaveCount { get; private set; }

		public override void SaveSetting(SettingsObject settings) {
			SaveCount++;
			base.SaveSetting(settings);
		}
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
