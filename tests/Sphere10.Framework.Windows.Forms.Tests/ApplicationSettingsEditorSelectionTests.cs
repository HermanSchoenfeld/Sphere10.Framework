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
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Application;
using Sphere10.Framework.Application.UI;
using FormsUI = global::Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>Verifies automatic and explicit editors for screen-owned settings.</summary>
[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class ApplicationSettingsEditorSelectionTests {

	/// <summary>Applies settings visibility to the real native descriptors without hiding ordinary nested values.</summary>
	[Test]
	public void ReusableEditorFiltersRuntimeValuesAndTracksTheActualSettingsObject() {
		RunWithMessageLoop(owner => {
			var settings = new EditableSettings { Provider = new MemoryStore() };
			using var editor = new ApplicationSettingsEditor();
			editor.SetEntity(DataSourceCapabilities.CanRead | DataSourceCapabilities.CanUpdate, settings, false);
			var propertyGrid = editor.Controls.OfType<PropertyGrid>().Single();
			var properties = TypeDescriptor.GetProperties(propertyGrid.SelectedObject);
			Assert.That(properties[nameof(SettingsObject.ID)], Is.Null);
			Assert.That(properties[nameof(SettingsObject.Provider)], Is.Null);
			Assert.That(properties[nameof(EditableSettings.SessionToken)], Is.Null);
			Assert.That(editor.GetEntityWithChanges(), Is.SameAs(settings));

			// Ownership names on ordinary nested records remain editable values.
			var details = properties[nameof(EditableSettings.Details)];
			var nested = details.Converter.GetProperties(details.GetValue(propertyGrid.SelectedObject));
			Assert.That(nested[nameof(EditorDetails.ID)], Is.Not.Null);
			Assert.That(nested[nameof(EditorDetails.Provider)], Is.Not.Null);
			Assert.That(nested[nameof(EditorDetails.TransientValue)], Is.Null);

			// Existing descriptor edit and undo behavior still owns the actual values.
			properties[nameof(OrdinarySettings.Number)].SetValue(propertyGrid.SelectedObject, 37);
			Assert.That(settings.Number, Is.EqualTo(37));
			Assert.That(editor.HasChanges, Is.True);
			editor.UndoChanges();
			Assert.That(settings.Number, Is.EqualTo(10));
			Assert.That(editor.HasChanges, Is.False);
			Assert.That(((MemoryStore)settings.Provider).Saves, Is.Zero);
			return Task.CompletedTask;
		});
	}

	[Test]
	public void AutomaticOptionsEditingRequiresTheClassMarkerAndExplicitEditorsCanEditUnmarkedSettings() {
		RunWithMessageLoop(async owner => {
			var customCalls = 0;
			var unmarked = new FormsUI.ApplicationScreenDefinition(typeof(OrdinarySettingsScreen), "Unmarked", stableKey: "unmarked");
			var marked = new FormsUI.ApplicationScreenDefinition(typeof(EditableSettingsScreen), "Marked", stableKey: "marked");
			var custom = new FormsUI.ApplicationScreenDefinition(typeof(OrdinarySettingsScreen), "Custom", stableKey: "custom", settingsEditor: context => {
					Assert.That(context.Settings, Is.TypeOf<OrdinarySettings>());
					customCalls++;
					return new DefaultCrudEntityEditor();
				});
			var block = new ApplicationBlock { Name = "Options" };
			foreach (var definition in new[] { unmarked, marked, custom })
				block.AddScreenDefinition(definition);
			var store = new MemoryStore();
			using var main = new BlockMainForm();
			await using var application = new FormsUI.Application(main, new[] { block });
			foreach (var definition in block.ScreenDefinitions)
				definition.Settings.Provider = store;
			main.RegisterBlock(block);
			using var dialog = new ApplicationSettingsDialog(application);
			dialog.Show(owner);
			dialog.SelectOwner(unmarked);
			Assert.That(unmarked.Settings, Is.Not.Null);
			Assert.That(Find<Panel>(dialog, "_editorHost").Controls.OfType<DefaultCrudEntityEditor>(), Is.Empty);
			Assert.That(Find<Button>(dialog, "_apply").Enabled, Is.False);
			Assert.That(Find<Button>(dialog, "_reset").Enabled, Is.True, "Reset targets are configured independently of editor availability.");
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

	/// <summary>Screen Options opens the settings root's own nested values, while deeper values and ordinary entity editors stay collapsed.</summary>
	[Test]
	public void OptionsEditorsExpandOnlyTheSettingsFirstLevel() {
		RunWithMessageLoop(async owner => {
			var marked = new FormsUI.ApplicationScreenDefinition(typeof(EditableSettingsScreen), "Marked", stableKey: "marked");
			var block = new ApplicationBlock { Name = "Options" };
			block.AddScreenDefinition(marked);
			using var main = new BlockMainForm();
			await using var application = new FormsUI.Application(main, new[] { block });
			marked.Settings.Provider = new MemoryStore();
			main.RegisterBlock(block);

			// The initial page is bound before the dialog's native controls exist.
			using (var dialog = new ApplicationSettingsDialog(application, selectedOwner: marked)) {
				dialog.Show(owner);
				var details = FindFirstLevelItem(Find<Panel>(dialog, "_editorHost").Controls.OfType<ApplicationSettingsEditor>().Single(), nameof(EditableSettings.Details));
				var origin = details.GridItems.Cast<GridItem>().Single(item => item.PropertyDescriptor?.Name == nameof(EditorDetails.Origin));
				Assert.That(details.Expanded, Is.True);
				Assert.That(origin.Expandable, Is.True);
				Assert.That(origin.Expanded, Is.False);
				dialog.Close();
			}

			// An editor that is already shown expands a newly bound entity.
			var settings = new EditableSettings { Provider = new MemoryStore() };
			using var shown = new ApplicationSettingsEditor { Dock = DockStyle.Left };
			owner.Controls.Add(shown);
			shown.SetEntity(DataSourceCapabilities.CanRead | DataSourceCapabilities.CanUpdate, settings, false);
			Assert.That(FindFirstLevelItem(shown, nameof(EditableSettings.Details)).Expanded, Is.True);

			// Other entity editors keep the native collapsed default.
			using var ordinary = new DefaultCrudEntityEditor { Dock = DockStyle.Right };
			owner.Controls.Add(ordinary);
			ordinary.SetEntity(DataSourceCapabilities.CanRead | DataSourceCapabilities.CanUpdate, settings, false);
			Assert.That(FindFirstLevelItem(ordinary, nameof(EditableSettings.Details)).Expanded, Is.False);
		});
	}

	// Native controls retain designer names while the public grid family uses its new VisualGrid name.
	private static T Find<T>(Control owner, string name)
		where T : Control {
		return (T)owner.Controls.Find(name, true).Single();
	}

	// Read a property row directly below the native grid's root, looking through category groups.
	private static GridItem FindFirstLevelItem(DefaultCrudEntityEditor editor, string name) {
		var root = editor.Controls.OfType<PropertyGrid>().Single().SelectedGridItem;
		while (root.Parent != null)
			root = root.Parent;
		return root.GridItems.Cast<GridItem>()
			.SelectMany(item => item.GridItemType == GridItemType.Category ? item.GridItems.Cast<GridItem>() : new[] { item })
			.Single(item => item.PropertyDescriptor?.Name == name);
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
			failure = new AssertionException("The screen Options editor test did not complete.");
			foreach (var dialog in System.Windows.Forms.Application.OpenForms.OfType<ApplicationSettingsDialog>().ToArray())
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

	#region Inner Types

	/// <summary>Provides unmarked settings for explicit editor selection.</summary>
	public class OrdinarySettings : SettingsObject {
		[DefaultValue(10)]
		public int Number { get; set; }
	}

	/// <summary>Declares settings without opting into the automatic editor.</summary>
	public sealed class OrdinarySettingsScreen : ApplicationScreen {
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public override OrdinarySettings ScreenSettings => (OrdinarySettings)base.ScreenSettings;
	}

	/// <summary>Declares settings that opt into the automatic editor.</summary>
	public sealed class EditableSettingsScreen : ApplicationScreen {
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public override EditableSettings ScreenSettings => (EditableSettings)base.ScreenSettings;
	}

	/// <summary>Enables the ordinary property editor through its class marker.</summary>
	[UserEditable]
	public sealed class EditableSettings : OrdinarySettings {

		/// <summary>Ordinary nested values that share names with settings ownership members.</summary>
		public EditorDetails Details { get; set; } = new();

		/// <summary>Runtime-only values do not appear in the settings editor.</summary>
		[Transient]
		public string SessionToken { get; set; }
	}

	/// <summary>Exercises nested value filtering independently of SettingsObject ownership.</summary>
	public sealed class EditorDetails {

		/// <summary>A user value rather than the settings root's persistent identity.</summary>
		public int ID { get; set; } = 7;

		/// <summary>A user value rather than a settings persistence provider.</summary>
		public string Provider { get; set; } = "Sample";

		/// <summary>A second-level expandable value that Screen Options leaves collapsed.</summary>
		public Point Origin { get; set; } = new(3, 4);

		/// <summary>A nested runtime value excluded by the same settings visibility rules.</summary>
		[Transient]
		public string TransientValue { get; set; }
	}

	// Supplies isolated provider storage for this fixture without changing its lifecycle contract.
	private sealed class MemoryStore : SyncSettingsProviderBase {
		// Counts successful fixture writes.
		public int Saves { get; private set; }

		// Check whether this fixture has a persisted record.
		public override bool ContainsSetting(Type settingsObjectType, object id = null) => false;

		// Remove this fixture record.
		public override void DeleteSetting(SettingsObject settings) {
		}

		// Clear this fixture storage.
		public override void ClearSettings() {
		}

		// Read fixture storage without bypassing the base provider lifecycle.
		protected override SettingsObject LoadInternal(Type settingsObjectType, object id = null)
			=> throw new InvalidOperationException("This provider has no saved settings.");

		// Persist fixture values after the base provider prepares them.
		protected override void SaveInternal(SettingsObject settings) => Saves++;
	}
	#endregion
}
