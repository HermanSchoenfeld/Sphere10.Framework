// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
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
using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Windows.Forms.SourceGrid;
using Sphere10.Framework.Windows.Forms.SourceGrid.Cells;
using FormsApplication = System.Windows.Forms.Application;
using EditorControlBase = Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Editors.EditorControlBase;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class VisualGridPreferencesTests {
	private ISettingsProvider _previousProvider;
	private string _directory;
	private ProbeProvider _provider;

	[SetUp]
	public void SetUp() {
		_previousProvider = (ISettingsProvider)typeof(UserSettings).GetField("_provider", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
		_directory = Path.Combine(TestContext.CurrentContext.TestDirectory, "grid-preferences-" + Guid.NewGuid().ToString("N"));
		_provider = new ProbeProvider(_directory);
		UserSettings.Provider = new CachedSettingsProvider(_provider);
	}

	[TearDown]
	public void TearDown() {
		_provider.ReleaseSave.Set();
		UserSettings.Provider = _previousProvider;
	}

	[TestCase(false)]
	[TestCase(true)]
	public void RenamedGridImportsOriginalJsonAndXmlRecordsAndPrefersCurrentRecords(bool xml) {
		RunWithMessageLoop(async owner => {
			var files = new DirectoryFileSettingsProvider(_directory);
			var legacyType = typeof(VisualGrid).Assembly.GetType("Sphere10.Framework.Windows.Forms.CrudGridSettings", throwOnError: true);
			var legacy = (SettingsObject)Activator.CreateInstance(legacyType);
			legacy.ID = "Renamed";
			legacyType.GetProperty("PageSize").SetValue(legacy, 23);
			var mode = legacyType.GetProperty("PageMode");
			mode.SetValue(legacy, Enum.Parse(Nullable.GetUnderlyingType(mode.PropertyType), "Auto"));
			legacyType.GetProperty("Columns").SetValue(legacy, new[] {
				new ApplicationGridColumnPreferences { Key = "name", Title = "Name", Width = 183 }
			});
			Directory.CreateDirectory(_directory);
			var originalFile = Path.Combine(_directory, "CrudGridSettings_Renamed.setting");
			if (xml)
				Tools.Xml.DeepWriteToFile(originalFile, legacy);
			else
				files.SaveSetting(legacy);

			using var grid = CreateGrid(owner, "Renamed");
			var imported = await grid.ReadLegacyPreferencesAsync(files);
			Assert.That(imported.PageSize, Is.EqualTo(23));
			Assert.That(imported.PageMode, Is.EqualTo(VisualGridPageMode.Auto));
			Assert.That(imported.Columns.Single().Width, Is.EqualTo(183));
			Assert.That(imported.Provider, Is.Null);
			Assert.That(File.Exists(originalFile), Is.True);

			files.SaveSetting(new VisualGridSettings { ID = "Renamed", PageSize = 41 });
			Assert.That((await grid.ReadLegacyPreferencesAsync(files)).PageSize, Is.EqualTo(41));
			Assert.That(File.Exists(originalFile), Is.True, "Import must preserve the old record.");
		});
	}

	[Test]
	public void ExistingGridSettingsValidatePagingSortingAndColumnValues() {
		Assert.That(Preferences(20, width: 145).Validate().IsSuccess, Is.True);
	}

	[TestCase(0)]
	[TestCase(1)]
	[TestCase(2)]
	[TestCase(3)]
	[TestCase(4)]
	[TestCase(5)]
	[TestCase(6)]
	[TestCase(7)]
	[TestCase(8)]
	[TestCase(9)]
	[TestCase(10)]
	[TestCase(11)]
	public void ExistingGridSettingsRejectInvalidOptionsValues(int invalidValue) {
		var settings = Preferences(20, width: 145);
		switch (invalidValue) {
			case 0:
				settings.PageSize = 0;
				break;
			case 1:
				settings.PageNumber = -1;
				break;
			case 2:
				settings.PageMode = (VisualGridPageMode)99;
				break;
			case 3:
				settings.SortDirection = (SortDirection)99;
				break;
			case 4:
				settings.Columns = null;
				break;
			case 5:
				settings.Columns = new ApplicationGridColumnPreferences[] { null };
				break;
			case 6:
				settings.Columns[0].Key = string.Empty;
				break;
			case 7:
				settings.Columns = new[] { settings.Columns[0], settings.Columns[0] };
				break;
			case 8:
				settings.Columns[0].Width = 0;
				break;
			case 9:
				settings.Columns[0].Width = double.NaN;
				break;
			case 10:
				settings.Columns[0].Width = double.PositiveInfinity;
				break;
			case 11:
				settings.Columns[0].Width = 100001;
				break;
		}
		Assert.That(settings.Validate().IsSuccess, Is.False);
	}

	[Test]
	public void OwnedValuesRestoreBeforeTheFirstQueryAndOnlyCompletedActionsNotify() {
		RunWithMessageLoop(async owner => {
			var order = new List<string>();
			using var crud = CreateGrid(owner, "Owned", grid: new HookGrid(order));
			crud.Preferences = Preferences(pageSize: 2, page: 1, width: 180);
			var changes = new List<VisualGridSettings>();
			crud.PreferencesChanged += values => {
				order.Add("event");
				changes.Add(values);
			};
			var source = new ProbeSource();
			await crud.SetDataSource(source);
			await crud.RefreshGrid();
			Assert.That(source.Requests.First(), Is.EqualTo((2, 1, "Name", SortDirection.Descending)));
			Assert.That(changes, Is.Empty, "Restore and binding are not accepted user edits.");
			Assert.That(_provider.ReadCount, Is.Zero);
			Assert.That(_provider.SaveCount, Is.Zero);

			var grid = Find<Grid>(crud, "_grid");
			grid.Columns[0].Width = 193;
			Assert.That(changes, Is.Empty, "Pixel geometry changes must not publish or serialize preferences.");
			CompleteResize(grid);
			Assert.That(changes, Has.Count.EqualTo(1));
			Assert.That(order, Is.EqualTo(new[] { "hook", "event" }));
			Assert.That(changes[0].Columns.Single(column => column.Key == "name").Width, Is.EqualTo(193 * 96D / crud.DeviceDpi));
			changes[0].Columns[0].Width = 1;
			Assert.That(crud.CaptureGridPreferences().Columns.Single(column => column.Key == "name").Width, Is.EqualTo(193 * 96D / crud.DeviceDpi));
			Assert.That(_provider.SaveCount, Is.Zero, "The screen owner is the only persistence authority.");
		});
	}

	[Test]
	public void ManualWidthsRoundTripByStableKeyWhileOtherColumnsKeepStretching() {
		RunWithMessageLoop(async owner => {
			double logical;
			using (var first = CreateGrid(owner, "Widths")) {
				await first.SetDataSource(new ProbeSource());
				await first.RefreshGrid();
				var grid = Find<Grid>(first, "_grid");
				grid.Columns[0].Width = 193;
				CompleteResize(grid);
				await first.FlushPreferencesAsync();
				logical = first.CaptureGridPreferences().Columns.Single(column => column.Key == "name").Width.Value;
				var other = grid.Columns[1].Width;
				var writes = _provider.SaveCount;
				owner.Width += 300;
				await first.RefreshGrid();
				Assert.That(grid.Columns[0].Width, Is.EqualTo(193));
				Assert.That(grid.Columns[1].Width, Is.GreaterThan(other));
				Assert.That(first.CaptureGridPreferences().Columns.Single(column => column.Key == "group").Width, Is.Null);
				Assert.That(_provider.SaveCount, Is.EqualTo(writes), "Viewport stretching never becomes a manual preference.");
			}

			UserSettings.Provider = new CachedSettingsProvider(new DirectoryFileSettingsProvider(_directory));
			using var restored = CreateGrid(owner, "Widths", "Renamed caption");
			await restored.SetDataSource(new ProbeSource());
			await restored.RefreshGrid();
			Assert.That(Find<Grid>(restored, "_grid").Columns[0].Width, Is.EqualTo((int)Math.Round(logical * restored.DeviceDpi / 96D)));
			Assert.That(restored.CaptureGridPreferences().Columns.Single(column => column.Key == "name").Title, Is.EqualTo("Renamed caption"));
			var reset = restored.CaptureGridPreferences();
			foreach (var column in reset.Columns)
				column.Width = null;
			restored.ApplyGridPreferences(reset);
			await restored.RefreshGrid();
			Assert.That(restored.CaptureGridPreferences().Columns.All(column => column.Width == null), Is.True);
		});
	}

	[Test]
	public void LegacyReadUsesItsCapturedProviderAndNeverCreatesOrMutatesARecord() {
		RunWithMessageLoop(async owner => {
			var captured = UserSettings.Provider;
			var saved = new VisualGridSettings { ID = "Legacy", Provider = captured, PageMode = VisualGridPageMode.Single, PageSize = 7 };
			captured.SaveSetting(saved);
			var writes = _provider.SaveCount;
			using var crud = new UpgradedGrid { SettingsKey = "Legacy", Dock = DockStyle.Fill };
			owner.Controls.Add(crud);
			UserSettings.Provider = new CachedSettingsProvider(new DirectoryFileSettingsProvider(Path.Combine(_directory, "other")));
			var imported = await crud.ReadLegacyPreferencesAsync(captured);
			Assert.That(imported.ConsumerVersion, Is.EqualTo(1));
			Assert.That(imported.PageMode, Is.EqualTo(VisualGridPageMode.Auto));
			Assert.That(crud.UpgradeThread, Is.EqualTo(Environment.CurrentManagedThreadId));
			Assert.That(saved.PageMode, Is.EqualTo(VisualGridPageMode.Single));
			Assert.That(saved.ConsumerVersion, Is.Zero, "Consumer upgrades cannot edit the provider's cached root.");
			Assert.That(_provider.SaveCount, Is.EqualTo(writes));
			crud.SettingsKey = "Absent";
			Assert.That(await crud.ReadLegacyPreferencesAsync(captured), Is.Null);
			Assert.That(captured.ContainsSetting(typeof(VisualGridSettings), "Absent"), Is.False);
		});
	}

	[Test]
	public void OrderedDetachedSavesUseTheOriginalProviderWithoutBlockingTheUi() {
		RunWithMessageLoop(async owner => {
			using var crud = CreateGrid(owner, "Queued");
			await crud.SetDataSource(new ProbeSource());
			await crud.RefreshGrid();
			_provider.BlockNextSave = true;
			var thread = Environment.CurrentManagedThreadId;
			Find<NumericUpDown>(crud, "_pageSizeUpDown").Value = 3;
			await _provider.SaveStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));
			using var timer = new System.Windows.Forms.Timer { Interval = 10 };
			var ticked = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
			timer.Tick += (_, _) => ticked.TrySetResult();
			timer.Start();
			await ticked.Task.WaitAsync(TimeSpan.FromSeconds(5));
			UserSettings.Provider = new CachedSettingsProvider(new DirectoryFileSettingsProvider(Path.Combine(_directory, "other")));
			Find<NumericUpDown>(crud, "_pageSizeUpDown").Value = 4;
			_provider.ReleaseSave.Set();
			await crud.FlushPreferencesAsync();
			Assert.That(_provider.SavedSizes, Is.EqualTo(new[] { 3, 4 }));
			Assert.That(_provider.SaveThreads, Has.None.EqualTo(thread));
			var fresh = new DirectoryFileSettingsProvider(_directory);
			Assert.That(((VisualGridSettings)fresh.LoadSetting(typeof(VisualGridSettings), "Queued")).PageSize, Is.EqualTo(4));
			Assert.That(UserSettings.Provider.ContainsSetting(typeof(VisualGridSettings), "Queued"), Is.False);
		});
	}

	[Test]
	public void FailedStandaloneSaveRemainsObservableAndCanBeRetried() {
		RunWithMessageLoop(async owner => {
			using var crud = CreateGrid(owner, "Retry");
			await crud.SetDataSource(new ProbeSource());
			await crud.RefreshGrid();
			_provider.FailNextSave = true;
			Find<NumericUpDown>(crud, "_pageSizeUpDown").Value = 3;
			await Assert.ThatAsync(() => crud.FlushPreferencesAsync(), Throws.InstanceOf<InvalidOperationException>());
			Assert.That(crud.PreferenceSaveError, Is.TypeOf<IOException>());
			Assert.That(UserSettings.Provider.ContainsSetting(typeof(VisualGridSettings), "Retry"), Is.False);
			await crud.FlushPreferencesAsync();
			Assert.That(crud.PreferenceSaveError, Is.Null);
			Assert.That(((VisualGridSettings)new DirectoryFileSettingsProvider(_directory).LoadSetting(typeof(VisualGridSettings), "Retry")).PageSize, Is.EqualTo(3));
		});
	}

	[Test]
	public void OwnedComputedColumnsRequireStableKeysWhileStandaloneColumnsRemainCompatible() {
		RunWithMessageLoop(owner => {
			using var crud = CreateGrid(owner, "Computed");
			var column = new VisualGridColumn<Row> { ColumnName = "Projection", PropertyValue = row => row.Name };
			crud.GridBindings = new[] { column };
			Assert.That(() => crud.Preferences = Preferences(2), Throws.ArgumentException);
			column.PreferenceKey = "projection";
			crud.Preferences = Preferences(2);
			Assert.That(crud.CaptureGridPreferences().Columns.Any(value => value.Key == "projection"), Is.True);
			Assert.That(_provider.ReadCount, Is.Zero);
			Assert.That(_provider.SaveCount, Is.Zero);
			return Task.CompletedTask;
		});
	}

	[Test]
	public void PreviousIdentitySaveFailureCannotMarkTheNewIdentityDirtyOrRetargetItsSnapshot() {
		RunWithMessageLoop(async owner => {
			using var crud = CreateGrid(owner, "Old identity");
			await crud.SetDataSource(new ProbeSource());
			await crud.RefreshGrid();
			_provider.BlockNextSave = true;
			_provider.FailNextSave = true;
			Find<NumericUpDown>(crud, "_pageSizeUpDown").Value = 3;
			await _provider.SaveStarted.Task.WaitAsync(TimeSpan.FromSeconds(5));

			// Finish the independently coalesced data query while the old preference write remains blocked.
			await crud.RefreshGrid();
			crud.SettingsKey = "New identity";
			await crud.RefreshGrid();
			_provider.ReleaseSave.Set();
			await crud.FlushPreferencesAsync();
			Assert.That(crud.PreferenceSaveError, Is.Null);
			Assert.That(_provider.SaveCount, Is.EqualTo(1));
			Assert.That(UserSettings.Provider.ContainsSetting(typeof(VisualGridSettings), "New identity"), Is.False);
			Find<NumericUpDown>(crud, "_pageSizeUpDown").Value = 4;
			await crud.FlushPreferencesAsync();
			Assert.That(((VisualGridSettings)new DirectoryFileSettingsProvider(_directory).LoadSetting(typeof(VisualGridSettings), "New identity")).PageSize, Is.EqualTo(4));
			Assert.That(UserSettings.Provider.ContainsSetting(typeof(VisualGridSettings), "Old identity"), Is.False);
		});
	}

	[Test]
	public void ApplyingPreferencesPreservesTheLiveCellDraftAndDefersItsQuery() {
		RunWithMessageLoop(async owner => {
			using var crud = CreateGrid(owner, "Draft");
			crud.Preferences = Preferences(pageSize: 2);
			crud.AllowCellEditing = true;
			var source = new ProbeSource();
			await crud.SetDataSource(source);
			await crud.RefreshGrid();
			var grid = Find<Grid>(crud, "_grid");
			var context = new CellContext(grid, new Position(1, 0));
			crud.SelectedEntity = crud.VisibleEntities.First();
			typeof(GridVirtual).GetMethod("OnKeyDown", BindingFlags.Instance | BindingFlags.NonPublic)
				.Invoke(grid, new object[] { new KeyEventArgs(Keys.F2) });
			Assert.That(context.IsEditing(), Is.True);
			var editor = ((EditorControlBase)context.Cell.Editor).Control;
			editor.Text = "Unapplied draft";
			var reads = source.Requests.Count;
			crud.ApplyGridPreferences(Preferences(pageSize: 4, width: 185));
			await crud.RefreshGrid();
			Assert.That(context.IsEditing(), Is.True);
			Assert.That(editor.Text, Is.EqualTo("Unapplied draft"));
			Assert.That(source.Requests, Has.Count.EqualTo(reads));
			context.EndEdit(true);
			await crud.RefreshGrid();
			Assert.That(source.Requests.Last().Length, Is.EqualTo(4));
			Assert.That(source.Rows.All(row => row.Name != "Unapplied draft"), Is.True);
		});
	}

	private static VisualGridSettings Preferences(int pageSize, int page = 0, double? width = null) {
		return new VisualGridSettings {
			ConsumerVersion = 1,
			PageMode = VisualGridPageMode.Paged,
			PageSize = pageSize,
			PageNumber = page,
			SortColumnName = nameof(Row.Name),
			SortDirection = SortDirection.Descending,
			Columns = new[] {
				new ApplicationGridColumnPreferences { Key = "name", Title = "Name", Width = width },
				new ApplicationGridColumnPreferences { Key = "group", Title = "Group" }
			}
		};
	}

	private static VisualGrid CreateGrid(Form owner, string key, string caption = "Name", VisualGrid grid = null) {
		var crud = grid ?? new VisualGrid();
		crud.SettingsKey = key;
		crud.Dock = DockStyle.Fill;
		owner.Controls.Add(crud);
		crud.GridBindings = new[] {
			new VisualGridColumn<Row> {
				PreferenceKey = "name",
				ColumnName = caption,
				PropertyName = nameof(Row.Name),
				SortName = nameof(Row.Name),
				DataType = typeof(string),
				PropertyValue = row => row.Name,
				SetPropertyValue = (row, value) => row.Name = (string)value,
				CanEditCell = true,
				ExpandsToFit = true
			},
			new VisualGridColumn<Row> {
				PreferenceKey = "group",
				ColumnName = "Group",
				PropertyName = nameof(Row.Group),
				SortName = nameof(Row.Group),
				DataType = typeof(string),
				PropertyValue = row => row.Group,
				CanEditCell = false,
				ExpandsToFit = true
			}
		};
		return crud;
	}

	private static T Find<T>(Control control, string name) where T : Control {
		return (T)control.Controls.Find(name, true).Single();
	}

	private static void CompleteResize(Grid grid) {
		typeof(Control).GetMethod("OnMouseUp", BindingFlags.Instance | BindingFlags.NonPublic)
			.Invoke(grid, new object[] { new MouseEventArgs(MouseButtons.Left, 1, -1, -1, 0) });
	}

	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var previous = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous));
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000), ClientSize = new Size(850, 450) };
		using var watchdog = new System.Windows.Forms.Timer { Interval = 20000 };
		watchdog.Tick += (_, _) => {
			failure = new AssertionException("The grid preference test timed out.");
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

	private class ProbeSource : ListDataSource<Row> {
		public ProbeSource() : base(new ExtendedList<Row>()) {
			Rows = Enumerable.Range(0, 20).Select(index => new Row { Name = "Name " + index.ToString("D2"), Group = "Group" }).ToArray();
		}

		public Row[] Rows { get; }

		public List<(int Length, int Page, string Sort, SortDirection Direction)> Requests { get; } = new();

		public override Task<DataSourceItems<Row>> ReadRangeAsync(string searchTerm, int pageLength, int page, string sortProperty, SortDirection sortDirection) {
			Requests.Add((pageLength, page, sortProperty, sortDirection));
			var rows = sortDirection == SortDirection.Descending ? Rows.Reverse().ToArray() : Rows;
			page = Math.Min(page, (rows.Length - 1) / pageLength);
			return Task.FromResult(new DataSourceItems<Row> { Items = rows.Skip(page * pageLength).Take(pageLength).ToArray(), Page = page, TotalCount = rows.Length });
		}
	}

	private sealed class Row {
		public string Name { get; set; }

		public string Group { get; set; }
	}

	private sealed class HookGrid(List<string> order) : VisualGrid {
		protected override void OnPreferencesChanged(VisualGridSettings preferences) {
			order.Add("hook");
			base.OnPreferencesChanged(preferences);
		}
	}

	private sealed class UpgradedGrid : VisualGrid {
		public int UpgradeThread { get; private set; }

		protected override void UpgradeGridSettings(VisualGridSettings settings) {
			UpgradeThread = Environment.CurrentManagedThreadId;
			settings.ConsumerVersion = 1;
			settings.PageMode = VisualGridPageMode.Auto;
		}
	}

	private sealed class ProbeProvider : DirectoryFileSettingsProvider {
		public ProbeProvider(string directory) : base(directory) {
		}

		public int ReadCount { get; private set; }

		public int SaveCount { get; private set; }

		public bool BlockNextSave { get; set; }

		public bool FailNextSave { get; set; }

		public TaskCompletionSource SaveStarted { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);

		public ManualResetEventSlim ReleaseSave { get; } = new(false);

		public List<int> SavedSizes { get; } = new();

		public List<int> SaveThreads { get; } = new();

		public override bool ContainsSetting(Type type, object id = null) {
			ReadCount++;
			return base.ContainsSetting(type, id);
		}

		public override void SaveSetting(SettingsObject settings) {
			SaveCount++;
			SaveThreads.Add(Environment.CurrentManagedThreadId);
			SavedSizes.Add(((VisualGridSettings)settings).PageSize);
			if (BlockNextSave) {
				BlockNextSave = false;
				SaveStarted.TrySetResult();
				if (!ReleaseSave.Wait(TimeSpan.FromSeconds(10)))
					throw new TimeoutException("The test did not release its blocked save.");
			}
			if (FailNextSave) {
				FailNextSave = false;
				throw new IOException("Simulated preference write failure.");
			}
			base.SaveSetting(settings);
		}
	}
}
