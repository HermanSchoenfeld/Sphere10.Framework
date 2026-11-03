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

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>Verifies automatic and explicit editors for screen-owned settings.</summary>
[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class ApplicationSettingsEditorSelectionTests {

	[Test]
	public void AutomaticOptionsEditingRequiresTheClassMarkerAndExplicitEditorsCanEditUnmarkedSettings() {
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
			failure = new AssertionException("The screen Options editor test did not complete.");
			foreach (var dialog in System.Windows.Forms.Application.OpenForms.OfType<WinFormsApplicationSettingsDialog>().ToArray())
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
	public sealed class OrdinarySettingsScreen : WinFormsApplicationScreen {
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public override OrdinarySettings ScreenSettings {
			get {
				return (OrdinarySettings)base.ScreenSettings;
			}
		}
	}

	/// <summary>Declares settings that opt into the automatic editor.</summary>
	public sealed class EditableSettingsScreen : WinFormsApplicationScreen {
		[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
		public override EditableSettings ScreenSettings {
			get {
				return (EditableSettings)base.ScreenSettings;
			}
		}
	}

	/// <summary>Enables the ordinary property editor through its class marker.</summary>
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
	#endregion
}
