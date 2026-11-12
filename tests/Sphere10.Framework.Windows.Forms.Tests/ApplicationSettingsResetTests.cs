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
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Application;
using FormsUI = global::Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>Verifies configured Options reset scopes, confirmation and ordinary owner persistence.</summary>
[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class ApplicationSettingsResetTests {
	private bool _startedFramework;

	// Native application controls borrow the existing Framework lifetime.
	[OneTimeSetUp]
	public void StartFramework() {
		_startedFramework = !Sphere10Framework.Instance.IsStarted;
		if (_startedFramework)
			Sphere10Framework.Instance.Build().Start();
	}

	// Release only the Framework instance started by this fixture.
	[OneTimeTearDown]
	public void StopFramework() {
		if (_startedFramework)
			Sphere10Framework.Instance.EndFramework();
	}

	[Test]
	public void DesignerPolicyExposesItsDefaultAndUpdatesAnEmptyParentsButtonsImmediately() {
		RunWithMessageLoop(async owner => {
			await using var graph = new SettingsGraph(groupedSettings: false);
			using var dialog = graph.CreateDialog(owner, graph.Group);
			Assert.That(typeof(ApplicationSettingsScreenPolicy).IsDefined(typeof(FlagsAttribute), false), Is.True);
			Assert.That(dialog.Policy, Is.EqualTo(ApplicationSettingsScreenPolicy.Default));
			Assert.That(ApplicationSettingsScreenPolicy.Default.ToString(), Is.EqualTo("Default"));
			Assert.That(ApplicationSettingsScreenPolicy.Default, Is.EqualTo(ApplicationSettingsScreenPolicy.ResetDescendants
				| ApplicationSettingsScreenPolicy.IncludeNonEditableSettings | ApplicationSettingsScreenPolicy.ApplyPendingResetScope));
			var property = TypeDescriptor.GetProperties(dialog)[nameof(dialog.Policy)];
			Assert.That(property.PropertyType, Is.EqualTo(typeof(ApplicationSettingsScreenPolicy)));
			Assert.That(property.IsBrowsable, Is.True);
			Assert.That(property.ShouldSerializeValue(dialog), Is.False);
			Assert.That(property.Category, Is.EqualTo("Behavior"));
			Assert.That(Find<Button>(dialog, "_reset").Enabled, Is.True);
			dialog.Policy &= ~ApplicationSettingsScreenPolicy.ResetDescendants;
			Assert.That(Find<Button>(dialog, "_reset").Enabled, Is.False);
			Assert.That(property.ShouldSerializeValue(dialog), Is.True);
			dialog.Policy |= ApplicationSettingsScreenPolicy.ResetDescendants;
			Assert.That(Find<Button>(dialog, "_reset").Enabled, Is.True);
			dialog.ResetSelectedDefaults();
			Assert.That(Find<Button>(dialog, "_apply").Enabled, Is.True);
			dialog.Policy &= ~ApplicationSettingsScreenPolicy.ApplyPendingResetScope;
			Assert.That(Find<Button>(dialog, "_apply").Enabled, Is.False);
			dialog.Policy |= ApplicationSettingsScreenPolicy.ApplyPendingResetScope;
			Assert.That(Find<Button>(dialog, "_apply").Enabled, Is.True);
			dialog.CancelEdits();
		});
	}

	[TestCase(8)]
	[TestCase(-1)]
	public void UnknownPolicyBitsAreRejectedWithoutReplacingALegitimateUnnamedCombination(int value) {
		using var dialog = new ApplicationSettingsDialog();
		var original = ApplicationSettingsScreenPolicy.ResetDescendants | ApplicationSettingsScreenPolicy.ApplyPendingResetScope;
		dialog.Policy = original;
		Assert.That(() => dialog.Policy = (ApplicationSettingsScreenPolicy)value, Throws.InstanceOf<ArgumentException>());
		Assert.That(dialog.Policy, Is.EqualTo(original));
	}

	[TestCase("Application", 5)]
	[TestCase("Plugin", 4)]
	[TestCase("Block", 3)]
	[TestCase("Screen", 1)]
	public void ResetIncludesOnlyTheChosenTreeScopeAndCancelRestoresUnvisitedActualOwners(string level, int expected) {
		RunWithMessageLoop(async owner => {
			await using var graph = new SettingsGraph();
			var originals = graph.AllSettings.ToDictionary(settings => settings, settings => settings.Value);
			var selected = level switch {
				"Application" => (object)graph.Application,
				"Plugin" => graph.Plugin,
				"Block" => graph.Group,
				_ => graph.Editable
			};
			using var dialog = graph.CreateDialog(owner, selected);
			dialog.ResetSelectedDefaults();
			Assert.That(graph.AllSettings.Count(settings => settings.Value == 10), Is.EqualTo(expected));
			Assert.That(graph.Store.Writes, Is.Empty);
			Assert.That(graph.Notifications, Is.Zero);
			Assert.That(dialog.HasUnappliedChanges, Is.True);
			dialog.CancelEdits();
			Assert.That(graph.AllSettings.All(settings => settings.Value == originals[settings]), Is.True);
			Assert.That(graph.AllSettings.All(settings => ReferenceEquals(settings.Provider, graph.Store) && settings.ID != null), Is.True);
			Assert.That(dialog.HasUnappliedChanges, Is.False);
		});
	}

	[TestCase(ApplicationSettingsScreenPolicy.None, 1, false)]
	[TestCase(ApplicationSettingsScreenPolicy.ResetDescendants, 2, false)]
	[TestCase(ApplicationSettingsScreenPolicy.IncludeNonEditableSettings, 1, false)]
	[TestCase(ApplicationSettingsScreenPolicy.ApplyPendingResetScope, 1, true)]
	[TestCase(ApplicationSettingsScreenPolicy.ResetDescendants | ApplicationSettingsScreenPolicy.IncludeNonEditableSettings, 3, false)]
	[TestCase(ApplicationSettingsScreenPolicy.ResetDescendants | ApplicationSettingsScreenPolicy.ApplyPendingResetScope, 2, true)]
	[TestCase(ApplicationSettingsScreenPolicy.IncludeNonEditableSettings | ApplicationSettingsScreenPolicy.ApplyPendingResetScope, 1, true)]
	[TestCase(ApplicationSettingsScreenPolicy.Default, 3, true)]
	public void EveryPolicyCombinationKeepsResetAndApplyFlagsIndependent(ApplicationSettingsScreenPolicy policy, int expected, bool appliesReset) {
		RunWithMessageLoop(async owner => {
			await using var graph = new SettingsGraph();
			using var dialog = graph.CreateDialog(owner, graph.Group);
			dialog.Policy = policy;
			dialog.ResetSelectedDefaults();
			Assert.That(graph.AllSettings.Count(settings => settings.Value == 10), Is.EqualTo(expected));
			Assert.That(graph.Outside.Settings, Is.SameAs(graph.AllSettings.Single(settings => Equals(settings.ID, "outside"))));
			Assert.That(graph.Notifications, Is.Zero);

			// A grouping node without its own record can apply only the explicitly included pending reset scope.
			dialog.SelectOwner(graph.Application);
			Assert.That(Find<Button>(dialog, "_apply").Enabled, Is.EqualTo(appliesReset));
			Assert.That(await dialog.ApplySelectedAsync(), Is.True);
			Assert.That(graph.Store.Writes.Length, Is.EqualTo(appliesReset ? expected : 0));
			Assert.That(dialog.HasUnappliedChanges, Is.EqualTo(!appliesReset));
			dialog.CancelEdits();
			Assert.That(graph.AllSettings.Count(settings => settings.Value == 10), Is.EqualTo(appliesReset ? expected : 0));
		});
	}

	[Test]
	public void SharedSettingsResetOnceAndEveryAliasShowsItsPendingState() {
		RunWithMessageLoop(async owner => {
			await using var graph = new SettingsGraph();
			graph.Hidden.Settings = graph.Editable.Settings;
			var settings = (ConfiguredScreen.Preferences)graph.Editable.Settings;
			var calls = settings.ResetCalls;
			using var dialog = graph.CreateDialog(owner, graph.Group);
			dialog.ResetSelectedDefaults();
			Assert.That(settings.ResetCalls, Is.EqualTo(calls + 1));
			var aliases = Nodes(Find<TreeView>(dialog, "_navigation").Nodes).Where(node => ReferenceEquals(node.Tag, graph.Editable) || ReferenceEquals(node.Tag, graph.Hidden)).ToArray();
			Assert.That(aliases.All(node => node.Text.EndsWith(" *", StringComparison.Ordinal)), Is.True);
			Assert.That(await dialog.ApplySelectedAsync(), Is.True);
			Assert.That(graph.Store.Writes, Is.EquivalentTo(new[] { "block", "editable" }));
			Assert.That(aliases.All(node => !node.Text.EndsWith(" *", StringComparison.Ordinal)), Is.True);
		});
	}

	[TestCase(false)]
	[TestCase(true)]
	public void NativeConfirmationNamesTheScopeAndCancellingNeverResetsIt(bool accept) {
		RunWithMessageLoop(async owner => {
			await using var graph = new SettingsGraph();
			using var dialog = graph.CreateDialog(owner, graph.Group);
			Find<Button>(dialog, "_reset").PerformClick();
			using var question = await WaitForQuestionAsync(dialog);
			var text = Find<Label>(question, "_textLabel").Text;
			Assert.That(text, Does.Contain("block “Group”").And.Contain("all settings below it").And.Contain("3 settings pages").And.Contain("pending until Apply or OK"));
			Assert.That(question.Owner, Is.SameAs(dialog));
			Assert.That(((Button)question.AcceptButton).Text, Is.EqualTo("Cancel"));
			Assert.That(Find<CheckBox>(question, "_alwaysCheckBox").Visible, Is.False);
			if (accept) {
				// Preserve the real owned confirmation's rendered text and buttons for visual review.
				await Task.Yield();
				using var bitmap = new Bitmap(question.Width, question.Height);
				question.DrawToBitmap(bitmap, new Rectangle(Point.Empty, question.Size));
				var path = Path.Combine(TestContext.CurrentContext.TestDirectory, "options-reset-confirmation.png");
				bitmap.Save(path, ImageFormat.Png);
				TestContext.AddTestAttachment(path, "The actual block reset confirmation, including affected count and provisional-save notice.");
			}
			Find<Button>(question, accept ? "button2" : "button1").PerformClick();
			await WaitUntilAsync(() => question.IsDisposed && dialog.Enabled);
			Assert.That(graph.AllSettings.Count(settings => settings.Value == 10), Is.EqualTo(accept ? 3 : 0));
			Assert.That(dialog.HasUnappliedChanges, Is.EqualTo(accept));
			Assert.That(graph.Store.Writes, Is.Empty);
			dialog.CancelEdits();
		});
	}

	[Test]
	public void ConfirmationUsesItsCapturedTargetsWhenSelectionAndPoliciesChangeWhileOpen() {
		RunWithMessageLoop(async owner => {
			await using var graph = new SettingsGraph();
			using var dialog = graph.CreateDialog(owner, graph.Group);
			Find<Button>(dialog, "_reset").PerformClick();
			using var question = await WaitForQuestionAsync(dialog);
			dialog.Policy = ApplicationSettingsScreenPolicy.None;
			dialog.SelectOwner(graph.Outside);
			Find<Button>(question, "button2").PerformClick();
			await WaitUntilAsync(() => question.IsDisposed && dialog.Enabled);
			Assert.That(graph.AllSettings.Count(settings => settings.Value == 10), Is.EqualTo(3));
			Assert.That(((ConfiguredScreen.Preferences)graph.Outside.Settings).Value, Is.EqualTo(81));
			dialog.CancelEdits();
		});
	}

	[Test]
	public void PendingResetsFollowNavigationButOtherManualPagesKeepTheirSelectedApplyContract() {
		RunWithMessageLoop(async owner => {
			await using var graph = new SettingsGraph();
			using var dialog = graph.CreateDialog(owner, graph.Outside);
			SetValue(dialog, 90);
			dialog.SelectOwner(graph.Group);
			dialog.ResetSelectedDefaults();
			dialog.SelectOwner(graph.Plugin);
			Assert.That(await dialog.ApplySelectedAsync(), Is.True);
			Assert.That(graph.Store.Writes, Is.EquivalentTo(new[] { "block", "editable", "hidden" }));
			Assert.That(dialog.HasUnappliedChanges, Is.True, "The unselected manual page still needs its own Apply or Cancel.");
			dialog.CancelEdits();
			Assert.That(((ConfiguredScreen.Preferences)graph.Outside.Settings).Value, Is.EqualTo(81));
			Assert.That(((ConfiguredScreen.Preferences)graph.Editable.Settings).Value, Is.EqualTo(10));
		});
	}

	[Test]
	public void SelectedPageApplyLeavesOtherResetsPendingButOkSavesThem() {
		RunWithMessageLoop(async owner => {
			await using var graph = new SettingsGraph();
			using var dialog = graph.CreateDialog(owner, graph.Group);
			dialog.Policy &= ~ApplicationSettingsScreenPolicy.ApplyPendingResetScope;
			dialog.ResetSelectedDefaults();
			dialog.SelectOwner(graph.Hidden);
			Assert.That(await dialog.ApplySelectedAsync(), Is.True, "A reset page can be saved without a user editor.");
			Assert.That(graph.Store.Writes, Is.EqualTo(new[] { "hidden" }));
			Assert.That(dialog.HasUnappliedChanges, Is.True);
			dialog.SelectOwner(graph.Application);
			Find<Button>(dialog, "_ok").PerformClick();
			await WaitUntilAsync(() => dialog.IsDisposed || !dialog.Visible);
			Assert.That(dialog.DialogResult, Is.EqualTo(DialogResult.OK));
			Assert.That(graph.Store.Writes, Is.EquivalentTo(new[] { "block", "editable", "hidden" }));
		});
	}

	[TestCase(false)]
	[TestCase(true)]
	public void PartialSaveFailureKeepsOnlyRemainingResetRecordsPending(bool retry) {
		RunWithMessageLoop(async owner => {
			await using var graph = new SettingsGraph();
			using var dialog = graph.CreateDialog(owner, graph.Group);
			var originals = graph.AllSettings.ToDictionary(settings => (string)settings.ID, settings => settings.Value);
			dialog.ResetSelectedDefaults();
			graph.Store.RejectAfterFirstWrite = true;
			Assert.That(await dialog.ApplySelectedAsync(), Is.False);
			Assert.That(graph.Store.Writes.Length, Is.EqualTo(1));
			var accepted = graph.Store.Writes.Single();
			Assert.That(dialog.HasUnappliedChanges, Is.True);
			if (retry) {
				graph.Store.RejectAfterFirstWrite = false;
				Assert.That(await dialog.ApplySelectedAsync(), Is.True);
				Assert.That(graph.Store.Writes, Is.EquivalentTo(new[] { "block", "editable", "hidden" }), "Accepted pages are not saved again during the retry.");
			}
			dialog.CancelEdits();
			foreach (var settings in new[] { graph.Group.BlockSettings, graph.Editable.Settings, graph.Hidden.Settings }.Cast<ConfiguredScreen.Preferences>())
				Assert.That(settings.Value, Is.EqualTo(retry || Equals(settings.ID, accepted) ? 10 : originals[(string)settings.ID]));
		});
	}

	[Test]
	public void UnmarkedPendingResetPagesStillValidateBeforeSaving() {
		RunWithMessageLoop(async owner => {
			await using var graph = new SettingsGraph();
			using var dialog = graph.CreateDialog(owner, graph.Group);
			dialog.ResetSelectedDefaults();
			var hidden = (ConfiguredScreen.Preferences)graph.Hidden.Settings;
			hidden.RejectSave = true;
			Assert.That(await dialog.ApplySelectedAsync(), Is.False);
			Assert.That(graph.Store.Writes, Does.Not.Contain("hidden"));
			Assert.That(Find<RichTextBox>(dialog, "_status").Text, Does.Contain("Rejected settings"));
			hidden.RejectSave = false;
			dialog.SelectOwner(graph.Hidden);
			Assert.That(Find<Button>(dialog, "_apply").Enabled, Is.True);
			Assert.That(await dialog.ApplySelectedAsync(), Is.True);
			Assert.That(graph.Store.Writes, Is.EquivalentTo(new[] { "block", "editable", "hidden" }));
		});
	}

	[Test]
	public void MainFormTemplateConfiguresPolicyBeforeTheExistingModalRouteShowsOptions() {
		RunWithMessageLoop(async owner => {
			using var host = new PolicyMainForm();
			var opening = host.ShowScreenOptionsAsync();
			Assert.That(await opening, Is.EqualTo(DialogResult.Cancel));
			Assert.That(host.FactoryCalls, Is.EqualTo(1));
			Assert.That(host.ConfiguredPolicyWasShown, Is.True);
		});
	}

	// Native property descriptors retain the editor's ordinary dirty tracking.
	private static void SetValue(ApplicationSettingsDialog dialog, int value) {
		var descriptor = Find<PropertyGrid>(dialog, "_propertyGrid").SelectedObject;
		TypeDescriptor.GetProperties(descriptor)[nameof(ConfiguredScreen.Preferences.Value)].SetValue(descriptor, value);
	}

	// Existing controls retain their designer names during modal presentation.
	private static T Find<T>(Control owner, string name)
		where T : Control {
		return owner.Controls.Find(name, true).OfType<T>().Single();
	}

	// Inspect the actual owner tree without building a second settings hierarchy.
	private static IEnumerable<TreeNode> Nodes(TreeNodeCollection nodes) {
		foreach (TreeNode node in nodes) {
			yield return node;
			foreach (var child in Nodes(node.Nodes))
				yield return child;
		}
	}

	// Wait only for the native question owned by this Options dialog.
	private static async Task<DialogEx> WaitForQuestionAsync(Form owner) {
		DialogEx question = null;
		await WaitUntilAsync(() => (question = System.Windows.Forms.Application.OpenForms.OfType<DialogEx>().SingleOrDefault(form => ReferenceEquals(form.Owner, owner))) != null);
		return question;
	}

	// Bound every modal observation so a regression cannot leave the test desktop waiting indefinitely.
	private static async Task WaitUntilAsync(Func<bool> condition) {
		for (var attempt = 0; attempt < 300; attempt++) {
			if (condition())
				return;
			await Task.Delay(10);
		}
		throw new AssertionException("The expected Options state was not reached.");
	}

	// Keep all controls, confirmation, saves and teardown on one bounded native message loop.
	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var previous = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous));
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000), ClientSize = new Size(1000, 700) };
		using var watchdog = new System.Windows.Forms.Timer { Interval = 20000 };
		watchdog.Tick += (_, _) => {
			failure = new AssertionException("The Options reset fixture did not complete.");
			foreach (var form in System.Windows.Forms.Application.OpenForms.Cast<Form>().Reverse().ToArray())
				form.Dispose();
			System.Windows.Forms.Application.ExitThread();
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

	// Own a real application graph whose unopened registrations already carry their settings instances.
	private sealed class SettingsGraph : IAsyncDisposable {
		private readonly BlockMainForm _main;
		private readonly ApplicationBlock _outsideBlock;

		public SettingsGraph(bool groupedSettings = true) {
			Store = new RecordingStore();
			Editable = new FormsUI.ApplicationScreenDefinition(typeof(ConfiguredScreen), "Editable", stableKey: "editable", settings: Settings("editable", 61, true));
			Hidden = new FormsUI.ApplicationScreenDefinition(typeof(ConfiguredScreen), "Unmarked", stableKey: "hidden", settings: Settings("hidden", 71, false));
			Outside = new FormsUI.ApplicationScreenDefinition(typeof(ConfiguredScreen), "Outside", stableKey: "outside", settings: Settings("outside", 81, true));
			Group = new ApplicationBlock { Name = "Group", BlockSettings = groupedSettings ? Settings("block", 51, true) : null };
			Group.AddScreenDefinition(Editable);
			Group.AddScreenDefinition(Hidden);
			_outsideBlock = new ApplicationBlock { Name = "Outside group" };
			_outsideBlock.AddScreenDefinition(Outside);
			Plugin = new FormsUI.ApplicationPlugin("Plugin", new[] { Group }) { PluginSettings = Settings("plugin", 41, true) };
			_main = new BlockMainForm();
			Application = new FormsUI.Application(_main, new[] { _outsideBlock }, new[] { Plugin });
			_main.RegisterBlock(Group);
			_main.RegisterBlock(_outsideBlock);
		}

		public RecordingStore Store { get; }
		public FormsUI.Application Application { get; }
		public FormsUI.ApplicationPlugin Plugin { get; }
		public ApplicationBlock Group { get; }
		public FormsUI.ApplicationScreenDefinition Editable { get; }
		public FormsUI.ApplicationScreenDefinition Hidden { get; }
		public FormsUI.ApplicationScreenDefinition Outside { get; }
		public int Notifications { get; private set; }

		public ConfiguredScreen.Preferences[] AllSettings {
			get {
				return new[] { Plugin.PluginSettings, Group.BlockSettings, Editable.Settings, Hidden.Settings, Outside.Settings }
					.Where(settings => settings != null).Cast<ConfiguredScreen.Preferences>().Distinct().ToArray();
			}
		}

		// The ordinary modal edits the graph's actual objects; no provider load or operational screen activation is needed.
		public ApplicationSettingsDialog CreateDialog(Form owner, object selected) {
			var dialog = new ApplicationSettingsDialog(Application, selectedOwner: selected);
			dialog.Show(owner);
			return dialog;
		}

		// Release only this fixture's application and native form.
		public async ValueTask DisposeAsync() {
			await Application.DisposeAsync();
			_main.Dispose();
		}

		// Give every fixture owner a concrete identity and ordinary provider before the Options tree sees it.
		private ConfiguredScreen.Preferences Settings(string id, int value, bool editable) {
			var settings = editable ? new ConfiguredScreen.EditablePreferences() : new ConfiguredScreen.Preferences();
			settings.ID = id;
			settings.Provider = Store;
			settings.Value = value;
			settings.OnChanged += () => Notifications++;
			return settings;
		}
	}

	/// <summary>Declares native registrations without constructing their operational screens.</summary>
	public sealed class ConfiguredScreen : ApplicationScreen {

		#region Inner Types

		/// <summary>Uses attributes for defaults while exposing no automatic editor until marked.</summary>
		public class Preferences : SettingsObject {
			private int _resetCalls;

			[DefaultValue(10)]
			public int Value { get; set; }

			[Transient]
			public bool RejectSave { get; set; }

			[Transient]
			public int ResetCalls => _resetCalls;

			/// <summary>Counts actual default restoration so aliases cannot conceal repeated resets.</summary>
			public override void RestoreDefaultValues() {
				base.RestoreDefaultValues();
				_resetCalls++;
			}

			/// <summary>Allows the fixture to reject an unmarked pending page through its normal validation lifecycle.</summary>
			public override Result Validate() {
				var result = base.Validate();
				if (RejectSave)
					result.AddError("Rejected settings.");
				return result;
			}
		}

		/// <summary>Opts the same declared settings into the ordinary automatic editor.</summary>
		[UserEditable]
		public sealed class EditablePreferences : Preferences {
		}

		#endregion
	}

	// Observe only successful ordinary provider writes, including partial failure and retry.
	private sealed class RecordingStore : SyncSettingsProviderBase {
		private readonly List<string> _writes = new();

		public bool RejectAfterFirstWrite { get; set; }

		public string[] Writes => _writes.ToArray();

		// Supplied fixture settings have no earlier backing record.
		public override bool ContainsSetting(Type settingsObjectType, object id = null) => false;

		// The fixture creates no backing files to delete.
		public override void DeleteSetting(SettingsObject settings) {
		}

		// Clear the fixture's observed write history.
		public override void ClearSettings() => _writes.Clear();

		// Loading is outside this fixture's explicitly supplied owner graph.
		protected override SettingsObject LoadInternal(Type settingsObjectType, object id = null)
			=> throw new NotSupportedException("This fixture supplies its actual settings owners directly.");

		// Record only accepted writes so partial failures remain observable.
		protected override void SaveInternal(SettingsObject settings) {
			if (RejectAfterFirstWrite && _writes.Count == 1)
				throw new IOException("Fixture storage rejected this page.");
			_writes.Add((string)settings.ID);
		}
	}

	// The existing main-form template configures the flags policy before showing Options.
	private sealed class PolicyMainForm : LiteMainForm {
		public int FactoryCalls { get; private set; }
		public bool ConfiguredPolicyWasShown { get; private set; }

		protected override ApplicationSettingsDialog CreateScreenOptionsDialog(object selectedOwner) {
			FactoryCalls++;
			var dialog = new ApplicationSettingsDialog {
				Policy = ApplicationSettingsScreenPolicy.None
			};
			dialog.Shown += (_, _) => {
				ConfiguredPolicyWasShown = dialog.Policy == ApplicationSettingsScreenPolicy.None;
				dialog.DialogResult = DialogResult.Cancel;
				dialog.Close();
			};
			return dialog;
		}
	}

	#endregion
}
