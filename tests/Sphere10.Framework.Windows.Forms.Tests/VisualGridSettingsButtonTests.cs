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

			grid.Preferences = Defaults();
			Assert.That(button.Visible, Is.True);
			Assert.That(button.Enabled, Is.True);
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
	public void AutomaticOptionsEditingRequiresTheClassMarkerButExplicitEditorsStillEditOrdinaryObjects() {
		RunWithMessageLoop(async owner => {
			var customCalls = 0;
			var roots = new ApplicationSettingsDefinition[] {
				new("unmarked", "Unmarked", new OrdinarySettings()),
				new("marked", "Marked", new EditableSettings()),
				new WinFormsApplicationSettingsDefinition("custom", "Custom", new OrdinarySettings(), context => {
					Assert.That(context.Settings, Is.TypeOf<OrdinarySettings>());
					customCalls++;
					return new DefaultCrudEntityEditor();
				})
			};
			var store = new MemoryStore();
			var catalog = new ApplicationSettingsCatalog("settings-button-tests", store, Array.Empty<IApplicationBlock>(), roots);
			await catalog.InitializeAsync();
			using var dialog = new WinFormsApplicationSettingsDialog(catalog);
			dialog.Show(owner);
			Assert.That(catalog.GetProfile("unmarked").HasOptions, Is.True);
			Assert.That(Find<Panel>(dialog, "_editorHost").Controls.OfType<DefaultCrudEntityEditor>(), Is.Empty);
			Assert.That(Find<Button>(dialog, "_apply").Enabled, Is.False);
			Assert.That(Find<Button>(dialog, "_reset").Enabled, Is.False);
			Assert.That(await dialog.ApplySelectedAsync(), Is.True);
			Assert.That(store.Saves, Is.Zero);

			dialog.SelectProfile(catalog.GetProfile("marked"));
			Assert.That(Find<Panel>(dialog, "_editorHost").Controls.OfType<DefaultCrudEntityEditor>().Count(), Is.EqualTo(1));
			Assert.That(Find<Button>(dialog, "_apply").Enabled, Is.True);
			dialog.SelectProfile(catalog.GetProfile("custom"));
			Assert.That(customCalls, Is.EqualTo(1));
			var editor = Find<Panel>(dialog, "_editorHost").Controls.OfType<DefaultCrudEntityEditor>().Single();
			((OrdinarySettings)editor.GetEntityWithChanges()).Number = 37;
			Assert.That(await dialog.ApplySelectedAsync(), Is.True);
			Assert.That(catalog.GetProfile("custom").GetValues<OrdinarySettings>().Number, Is.EqualTo(37));
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

	// Native modal presentation remains asynchronous so the owned dispatcher can drive its real controls.
	private static async Task<VisualGridSettingsDialog> WaitForDialogAsync() {
		await WaitUntilAsync(() => System.Windows.Forms.Application.OpenForms.OfType<VisualGridSettingsDialog>().Any());
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

	public class OrdinarySettings {
		public int Number { get; set; } = 10;
	}

	[UserEditable]
	public sealed class EditableSettings : OrdinarySettings {
	}

	private sealed class MemoryStore : IApplicationSettingsStore {
		public int Saves { get; private set; }

		public Task<ApplicationSettingsStoredProfile> LoadAsync(string key, object defaults, bool explicitId, object id, CancellationToken token = default) {
			return Task.FromResult<ApplicationSettingsStoredProfile>(null);
		}

		public Task SaveAsync(string key, ApplicationSettingsStoredProfile snapshot, bool explicitId, object id, CancellationToken token = default) {
			Saves++;
			return Task.CompletedTask;
		}
	}
}
