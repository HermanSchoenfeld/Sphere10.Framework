// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class VisualGridSettingsButtonTests {
	[Test]
	public void SettingsPresenceControlsVisibilityAndTheTraitOnlyControlsEnabledState() {
		RunWithMessageLoop(async owner => {
			using var grid = await CreateGridAsync(owner);
			var button = Find<Button>(grid, "_settingsButton");
			Assert.That(button.Visible, Is.False);
			Assert.That(grid.AllowsSettingsChange, Is.True);
			Assert.That(grid.ShowGridSettings, Is.True);

			grid.Preferences = Defaults();
			Assert.That(button.Visible, Is.True);
			Assert.That(button.Enabled, Is.True);
			grid.ShowGridSettings = false;
			Assert.That(button.Visible, Is.False);
			grid.ShowGridSettings = true;
			Assert.That(button.Visible, Is.True);
			grid.Traits = VisualGridTraits.None;
			Assert.That(button.Visible, Is.True);
			Assert.That(button.Enabled, Is.False);
			Assert.That(await grid.ShowSettingsAsync(), Is.False);
			grid.AllowsSettingsChange = true;
			Assert.That(grid.Traits.HasFlag(VisualGridTraits.AllowsSettingsChange), Is.True);
			Assert.That(button.Enabled, Is.True);
			grid.AllowsSettingsChange = false;
			Assert.That(grid.Traits, Is.EqualTo(VisualGridTraits.None));
			Assert.That(button.Visible, Is.True);
			grid.Preferences = null;
			Assert.That(button.Visible, Is.False);
		});
	}

	[Test]
	public void GridButtonEditsTheBoundValuesAndCancelOrInvalidInputDoesNotPublish() {
		RunWithMessageLoop(async owner => {
			using var grid = await CreateGridAsync(owner);
			grid.Preferences = Defaults();
			var changed = 0;
			grid.PreferencesChanged += _ => changed++;
			var button = Find<Button>(grid, "_settingsButton");
			button.PerformClick();
			using (var cancelled = await WaitForDialogAsync()) {
				cancelled.Settings.PageSize = 41;
				Find<Button>(cancelled, "_cancel").PerformClick();
			}
			Assert.That(grid.CaptureGridPreferences().PageSize, Is.EqualTo(12));
			Assert.That(changed, Is.Zero);

			button.PerformClick();
			using (var accepted = await WaitForDialogAsync()) {
				Capture(accepted, "visual-grid-settings-dialog.png");
				accepted.Settings.PageSize = 0;
				Find<Button>(accepted, "_ok").PerformClick();
				Assert.That(accepted.Visible, Is.True);
				Assert.That(Find<Label>(accepted, "_error").Text, Does.Contain("positive"));
				Assert.That(changed, Is.Zero);
				accepted.Settings.PageSize = 23;
				accepted.Settings.Columns[0].Width = 184;
				Find<Button>(accepted, "_ok").PerformClick();
			}
			await WaitUntilAsync(() => changed == 1);
			await grid.RefreshGrid();

			Assert.That(grid.CaptureGridPreferences().PageSize, Is.EqualTo(23));
			Assert.That(grid.CaptureGridPreferences().Columns[0].Width, Is.EqualTo(184));
			Assert.That(grid.VisibleEntities.Count(), Is.EqualTo(23));
			Assert.That(changed, Is.EqualTo(1), "The accepted editor uses the existing bound-settings event once.");
			Capture(owner, "visual-grid-settings-button.png");
		});
	}

	[Test]
	public void NestedColumnChoicesPaintWithoutReplacingTheSettingsObject() {
		RunWithMessageLoop(async owner => {
			using var grid = await CreateGridAsync(owner);
			grid.Preferences = Defaults();
			var editing = grid.ShowSettingsAsync();
			using var dialog = await WaitForDialogAsync();
			var properties = Find<PropertyGrid>(dialog, "_propertyGrid");
			var column = ColumnItem(properties);
			var original = dialog.Settings.Columns[0];
			PaintStandardValueDropdown(properties, column);
			Assert.That(dialog.Settings.Columns[0], Is.SameAs(original));
			Find<Button>(dialog, "_cancel").PerformClick();
			Assert.That(await editing, Is.False);
		});
	}

	[Test]
	public void AcceptedNativeColumnWidthChangesSurviveRefresh([Values(false, true)] bool prepared, [Values(false, true)] bool pendingText) {
		RunWithMessageLoop(async owner => {
			using var grid = await CreateGridAsync(owner);
			grid.PrepareReadOnlyCellsInBackground = prepared;
			grid.Preferences = Defaults();
			var editing = grid.ShowSettingsAsync();
			using var dialog = await WaitForDialogAsync();
			var properties = Find<PropertyGrid>(dialog, "_propertyGrid");
			var column = ColumnItem(properties);
			column.Expanded = true;
			var width = column.GridItems.Cast<GridItem>().Single(item => item.PropertyDescriptor?.Name == "Width");
			if (pendingText) {
				properties.SelectedGridItem = width;
				properties.Focus();
				Assert.That(properties.ContainsFocus, Is.True);
				var view = properties.Controls.Cast<Control>().Single(control => control.GetType().Name == "PropertyGridView");
				view.Focus();
				Assert.That(view.ContainsFocus, Is.True);
				var edit = (TextBox)view.GetType().GetProperty("EditTextBox", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(view)!;
				Assert.That(edit.Focus(), Is.True, $"Text editor Visible={edit.Visible}, Enabled={edit.Enabled}, Bounds={edit.Bounds}; property readonly={width.PropertyDescriptor!.IsReadOnly}.");
				edit.Text = "184";
				Assert.That(dialog.Settings.Columns[0].Width, Is.Null, "Text remains pending until the user leaves the native editor.");
				Assert.That(Find<Button>(dialog, "_ok").Focus(), Is.True);
			} else
				CommitText(properties, width, "184");
			Assert.That(dialog.Settings.Columns[0].Width, Is.EqualTo(184));
			Find<Button>(dialog, "_ok").PerformClick();
			Assert.That(await editing, Is.True);

			var native = Find<SourceGrid.Grid>(grid, "_grid");
			var expectedWidth = (int)Math.Round(184 * grid.DeviceDpi / 96D);
			Assert.That(native.Columns[0].Width, Is.EqualTo(expectedWidth));
			await grid.RefreshGrid();
			Assert.That(native.Columns[0].Width, Is.EqualTo(expectedWidth));
		});
	}

	[TestCase(false)]
	[TestCase(true)]
	public void StretchChoiceChangesTheNativeLayoutAndManualWidthsTakePrecedence(bool prepared) {
		RunWithMessageLoop(async owner => {
			using var grid = await CreateGridAsync(owner);
			grid.PrepareReadOnlyCellsInBackground = prepared;
			grid.Preferences = Defaults();
			var native = Find<SourceGrid.Grid>(grid, "_grid");
			var naturalWidth = native.Columns[0].Width;
			Assert.That(grid.CaptureGridPreferences().Columns[0].ExpandsToFit, Is.False);

			var editing = grid.ShowSettingsAsync();
			using (var dialog = await WaitForDialogAsync()) {
				var properties = Find<PropertyGrid>(dialog, "_propertyGrid");
				var column = ColumnItem(properties);
				column.Expanded = true;
				CommitText(properties, column.GridItems.Cast<GridItem>().Single(item => item.PropertyDescriptor?.Name == "ExpandsToFit"), "True");
				Capture(dialog, "visual-grid-stretch-settings.png");
				Find<Button>(dialog, "_ok").PerformClick();
			}
			Assert.That(await editing, Is.True);
			Assert.That(native.Columns[0].Width, Is.GreaterThan(naturalWidth));
			Assert.That(native.Columns[0].Width, Is.EqualTo(native.DisplayRectangle.Width));
			await grid.RefreshGrid();
			Assert.That(native.Columns[0].Width, Is.EqualTo(native.DisplayRectangle.Width));
			Assert.That(grid.CaptureGridPreferences().Columns[0].ExpandsToFit, Is.True);

			editing = grid.ShowSettingsAsync();
			using (var dialog = await WaitForDialogAsync()) {
				var properties = Find<PropertyGrid>(dialog, "_propertyGrid");
				var column = ColumnItem(properties);
				column.Expanded = true;
				CommitText(properties, column.GridItems.Cast<GridItem>().Single(item => item.PropertyDescriptor?.Name == "Width"), "184");
				Find<Button>(dialog, "_ok").PerformClick();
			}
			Assert.That(await editing, Is.True);
			Assert.That(native.Columns[0].Width, Is.EqualTo((int)Math.Round(184 * grid.DeviceDpi / 96D)));
			Assert.That(grid.CaptureGridPreferences().Columns[0].ExpandsToFit, Is.True, "A manual width suppresses stretch without discarding the saved choice.");

			var reset = grid.CaptureGridPreferences();
			reset.Columns[0].Width = null;
			reset.Columns[0].ExpandsToFit = false;
			grid.ApplyGridPreferences(reset);
			await grid.RefreshGrid();
			Assert.That(native.Columns[0].Width, Is.EqualTo(naturalWidth));
		});
	}

	[Test]
	public void AutomaticOptionsEditingRequiresTheClassMarkerButExplicitEditorsStillEditOrdinaryObjects() {
		RunWithMessageLoop(async owner => {
			var customCalls = 0;
			var unmarked = new WinFormsApplicationScreenDefinition(typeof(OrdinarySettingsScreen), "Unmarked", stableKey: "unmarked");
			var marked = new WinFormsApplicationScreenDefinition(typeof(EditableSettingsScreen), "Marked", stableKey: "marked");
			var custom = new WinFormsApplicationScreenDefinition(typeof(OrdinarySettingsScreen), "Custom", stableKey: "custom", settingsEditor: context => {
					Assert.That(context.Settings, Is.TypeOf<OrdinarySettings>());
					customCalls++;
					return new DefaultCrudEntityEditor();
				});
			var block = new WinFormsApplicationBlock { Name = "Options" };
			foreach (var definition in new[] { unmarked, marked, custom })
				block.AddScreenDefinition(definition);
			var store = new MemoryStore();
			using var main = new BlockMainForm();
			await using var application = new WinFormsApplication(main, new[] { block });
			foreach (var definition in block.ScreenDefinitions)
				definition.Settings.Provider = store;
			main.RegisterBlock(block);
			using var dialog = new WinFormsApplicationSettingsDialog(application);
			dialog.Show(owner);
			dialog.SelectOwner(unmarked);
			Assert.That(unmarked.Settings, Is.Not.Null);
			Assert.That(Find<Panel>(dialog, "_editorHost").Controls.OfType<DefaultCrudEntityEditor>(), Is.Empty);
			Assert.That(Find<Button>(dialog, "_apply").Enabled, Is.False);
			Assert.That(Find<Button>(dialog, "_reset").Enabled, Is.False);
			Assert.That(await dialog.ApplySelectedAsync(), Is.True);
			Assert.That(store.Saves, Is.Zero);

			dialog.SelectOwner(marked);
			Assert.That(Find<Panel>(dialog, "_editorHost").Controls.OfType<DefaultCrudEntityEditor>().Count(), Is.EqualTo(1));
			Assert.That(Find<Button>(dialog, "_apply").Enabled, Is.True);
			dialog.SelectOwner(custom);
			Assert.That(customCalls, Is.EqualTo(1));
			var editor = Find<Panel>(dialog, "_editorHost").Controls.OfType<DefaultCrudEntityEditor>().Single();
			((OrdinarySettings)editor.GetEntityWithChanges()).Number = 37;
			Assert.That(await dialog.ApplySelectedAsync(), Is.True);
			Assert.That(((OrdinarySettings)custom.Settings).Number, Is.EqualTo(37));
			Assert.That(store.Saves, Is.EqualTo(1));
			dialog.Close();
		});
	}

	// Each grid is owner-bound but does not resolve a standalone provider or write a separate preference record.
	private static async Task<VisualGrid> CreateGridAsync(Form owner) {
		var grid = new VisualGrid {
			Dock = DockStyle.Fill,
			PersistSettings = false,
			GridBindings = new[] {
				new VisualGridColumn<string> { ColumnName = "Record", PreferenceKey = "record", DataType = typeof(string), PropertyValue = value => value }
			}
		};
		owner.Controls.Add(grid);
		await grid.SetDataSource(new ListDataSource<string>(new ExtendedList<string>(Enumerable.Range(1, 80).Select(number => "Record " + number).ToArray())),
			DataSourceCapabilities.CanRead | DataSourceCapabilities.CanPage);
		await grid.RefreshGrid();
		return grid;
	}

	// The grid and its Options section use this same existing grid settings type.
	private static VisualGridSettings Defaults() {
		return new VisualGridSettings {
			PageMode = VisualGridPageMode.Paged,
			PageSize = 12,
			Columns = new[] { new ApplicationGridColumnPreferences { Key = "record", Title = "Record" } }
		};
	}

	// Expand the real settings collection rather than editing the dialog's model behind its controls.
	private static GridItem ColumnItem(PropertyGrid properties) {
		var root = properties.SelectedGridItem;
		while (root.Parent != null)
			root = root.Parent;
		var columns = root.GridItems.Cast<GridItem>().SelectMany(item => item.GridItemType == GridItemType.Category
			? item.GridItems.Cast<GridItem>() : new[] { item }).Single(item => item.PropertyDescriptor?.Name == "Columns");
		columns.Expanded = true;
		return columns.GridItems.Cast<GridItem>().Single(item => item.PropertyDescriptor?.Name == "[0]");
	}

	// Commit through the same native text path used by the property editor.
	private static void CommitText(PropertyGrid properties, GridItem item, string text) {
		properties.SelectedGridItem = item;
		var view = properties.Controls.Cast<Control>().Single(control => control.GetType().Name == "PropertyGridView");
		Assert.That(view.GetType().GetMethod("CommitText", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(view, new object[] { text }), Is.True);
	}

	// Exercise the native dropdown's paint path directly so a failing converter cannot open a modal error prompt.
	private static void PaintStandardValueDropdown(PropertyGrid properties, GridItem item) {
		properties.SelectedGridItem = item;
		var converter = item.PropertyDescriptor!.Converter;
		var captions = converter.GetStandardValues()!.Cast<object>().Select(value => converter.ConvertToString(value)).ToArray();
		Assert.That(captions, Is.EqualTo(new[] { "Record", "(none)", "(Create new)" }));
		var paint = item.GetType().GetMethod("PaintValue", BindingFlags.Instance | BindingFlags.Public)!;
		var flags = Enum.ToObject(paint.GetParameters()[3].ParameterType, 0);
		using var bitmap = new Bitmap(300, 28);
		using var graphics = Graphics.FromImage(bitmap);
		var bounds = new Rectangle(Point.Empty, bitmap.Size);
		foreach (var caption in captions)
			Assert.That(() => paint.Invoke(item, new object[] { graphics, bounds, bounds, flags, caption }), Throws.Nothing);
	}

	// Native modal presentation remains asynchronous so the owned dispatcher can drive its real controls.
	private static async Task<VisualGridSettingsDialog> WaitForDialogAsync() {
		await WaitUntilAsync(() => System.Windows.Forms.Application.OpenForms.OfType<VisualGridSettingsDialog>().Any(dialog => dialog.Visible && dialog.CanFocus));
		return System.Windows.Forms.Application.OpenForms.OfType<VisualGridSettingsDialog>().Single();
	}

	// Bound observable conditions avoid ending a fixture before its native acceptance continuation completes.
	private static async Task WaitUntilAsync(Func<bool> condition) {
		var deadline = DateTime.UtcNow.AddSeconds(5);
		while (!condition()) {
			Assert.That(DateTime.UtcNow, Is.LessThan(deadline), "The native settings transition did not complete.");
			await Task.Delay(10);
		}
	}

	// Capture owned HWND rendering rather than the unrelated desktop foreground window.
	private static void Capture(Form form, string name) {
		var directory = Environment.GetEnvironmentVariable("SPHERE10_UI_CAPTURE_DIRECTORY");
		if (string.IsNullOrWhiteSpace(directory))
			return;
		Directory.CreateDirectory(directory);
		using var bitmap = new Bitmap(form.Width, form.Height);
		form.DrawToBitmap(bitmap, new Rectangle(Point.Empty, form.Size));
		bitmap.Save(Path.Combine(directory, name), ImageFormat.Png);
	}

	// Native controls retain designer names while the public grid family uses its new VisualGrid name.
	private static T Find<T>(Control owner, string name)
		where T : Control {
		return (T)owner.Controls.Find(name, true).Single();
	}

	// All controls, asynchronous dialogs and cleanup execute on one live Forms message loop.
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
			ClientSize = new Size(850, 420)
		};
		using var watchdog = new System.Windows.Forms.Timer { Interval = 20000 };
		watchdog.Tick += (_, _) => {
			failure = new AssertionException("The visual-grid settings test did not complete.");
			foreach (var dialog in System.Windows.Forms.Application.OpenForms.OfType<VisualGridSettingsDialog>().ToArray())
				dialog.Close();
			owner.Close();
		};
		owner.Shown += async (_, _) => {
			using var close = Tools.Scope.ExecuteOnDispose(owner.Close);
			failure = await Tools.Exceptions.ExecuteCapturingExceptionAsync(() => test(owner));
		};
		watchdog.Start();
		System.Windows.Forms.Application.Run(owner);
		Assert.That(failure, Is.Null, failure?.ToString());
	}

	public class OrdinarySettings : SettingsObject {
		public int Number { get; set; } = 10;
	}

	public sealed class OrdinarySettingsScreen : WinFormsApplicationScreen {
		public override OrdinarySettings ScreenSettings {
			get {
				return (OrdinarySettings)base.ScreenSettings;
			}
		}
	}

	public sealed class EditableSettingsScreen : WinFormsApplicationScreen {
		public override EditableSettings ScreenSettings {
			get {
				return (EditableSettings)base.ScreenSettings;
			}
		}
	}

	[UserEditable]
	public sealed class EditableSettings : OrdinarySettings {
	}

	// Supplies isolated provider storage for this fixture without changing its lifecycle contract.
	private sealed class MemoryStore : SyncSettingsProviderBase {
		// Counts successful fixture writes.
		public int Saves { get; private set; }

		// Check whether this fixture has a persisted record.
		public override bool ContainsSetting(Type settingsObjectType, object id = null) {
			return false;
		}

		// Remove this fixture record.
		public override void DeleteSetting(SettingsObject settings) {
		}

		// Clear this fixture storage.
		public override void ClearSettings() {
		}

		// Read fixture storage without bypassing the base provider lifecycle.
		protected override SettingsObject LoadInternal(Type settingsObjectType, object id = null) {
			throw new InvalidOperationException("This provider has no saved settings.");
		}

		// Persist fixture values after the base provider prepares them.
		protected override void SaveInternal(SettingsObject settings) {
			Saves++;
		}
	}
}
