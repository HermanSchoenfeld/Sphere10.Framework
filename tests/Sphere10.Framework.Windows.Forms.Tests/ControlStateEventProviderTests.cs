// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Tools;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class ControlStateEventProviderTests {
	[TestCase("Text")]
	[TestCase("RichText")]
	[TestCase("Masked")]
	[TestCase("Combo")]
	[TestCase("Check")]
	[TestCase("Radio")]
	[TestCase("Numeric")]
	[TestCase("List")]
	[TestCase("Track")]
	[TestCase("Scroll")]
	[TestCase("Calendar")]
	[TestCase("Date")]
	[TestCase("Domain")]
	public void CommonInputAttachmentsDetachAndCanBeReusedWithoutDuplicates(string kind) {
		var first = CreateInput(kind);
		var second = CreateInput(kind);
		using var firstControl = first.Control;
		using var secondControl = second.Control;

		// RichTextBox publishes text edits through its native RichEdit window.
		firstControl.CreateControl();
		secondControl.CreateControl();

		var provider = new CommonControlStateEventProvider();
		var changes = 0;
		provider.StateChanged += () => changes++;
		provider.SetControl(first.Control);
		first.Change(1);
		Assert.That(changes, Is.EqualTo(1));

		provider.SetControl(second.Control);
		first.Change(2);
		Assert.That(changes, Is.EqualTo(1), "The previous borrowed input must no longer be observed.");
		second.Change(1);
		Assert.That(changes, Is.EqualTo(2));
		provider.SetControl(second.Control);
		second.Change(2);
		Assert.That(changes, Is.EqualTo(3));

		provider.Clear();
		provider.Clear();
		second.Change(3);
		Assert.That(changes, Is.EqualTo(3));
		Assert.That(provider.Control, Is.Null);
		Assert.That(firstControl.IsDisposed || secondControl.IsDisposed, Is.False);
	}

	[Test]
	public void CustomGenericNumericInputPublishesParsedValuesOnceAndDetaches() {
		using var input = new LongBox();
		input.CreateControl();
		var manager = new ControlStateEventProviderManager();
		var provider = manager.GetControlStateProvider(input.GetType());
		var values = new List<long?>();
		provider.StateChanged += () => values.Add(input.Value);
		provider.SetControl(input);

		// TextChanged precedes parsing; the state observer must receive the completed numeric value instead.
		input.Text = "41";
		Assert.That(values, Is.EqualTo(new long?[] { 41 }));
		input.Value = 42;
		Assert.That(values, Is.EqualTo(new long?[] { 41, 42 }));
		provider.SetControl(input);
		input.Text = "43";
		Assert.That(values, Is.EqualTo(new long?[] { 41, 42, 43 }));
		input.Text = "not a number";
		Assert.That(input.Value, Is.EqualTo(43));
		Assert.That(values, Has.Count.EqualTo(3));

		provider.Clear();
		input.Text = "44";
		Assert.That(input.Value, Is.EqualTo(44));
		Assert.That(values, Has.Count.EqualTo(3));
		Assert.That(input.IsDisposed, Is.False);
	}

	[Test]
	public void ContainerObserversFollowReparentingAndStopFollowingNewChildrenAfterClear() {
		using var first = new Panel();
		using var second = new Panel();
		using var editor = new TextBox { Parent = first };
		var firstProvider = new ContainerControlStateEventProvider();
		var secondProvider = new ContainerControlStateEventProvider();
		var firstChanges = 0;
		var secondChanges = 0;
		firstProvider.StateChanged += () => firstChanges++;
		secondProvider.StateChanged += () => secondChanges++;
		firstProvider.SetControl(first);
		secondProvider.SetControl(second);
		editor.Text = "First";
		Assert.That((firstChanges, secondChanges), Is.EqualTo((1, 0)));

		editor.Parent = second;
		editor.Text = "Second";
		Assert.That((firstChanges, secondChanges), Is.EqualTo((1, 1)));
		firstProvider.Clear();
		secondProvider.Clear();
		using var later = new TextBox { Parent = first, Text = "Not observed" };
		editor.Parent = first;
		editor.Text = "Still not observed";
		Assert.That((firstChanges, secondChanges), Is.EqualTo((1, 1)));
		Assert.That(editor.IsDisposed, Is.False);
	}

	[Test]
	public void EditableComboTracksTextAndDistinctChoicesWithIdenticalCaptions() {
		using var input = new ComboBox();
		input.Items.AddRange(new object[] { new CaptionItem(), new CaptionItem() });
		var provider = new CommonControlStateEventProvider();
		provider.SetControl(input);
		var changes = 0;
		provider.StateChanged += () => changes++;
		input.SelectedIndex = 0;
		Assert.That(changes, Is.EqualTo(1));
		input.SelectedIndex = 1;
		Assert.That(changes, Is.EqualTo(2));
		input.Text = "An editable value";
		Assert.That(changes, Is.EqualTo(3));
		provider.Clear();
	}

	[Test]
	public void SplitObserverReleasesBothPanelsAndTheirBorrowedInputs() {
		using var split = new SplitContainer();
		using var first = new TextBox { Parent = split.Panel1 };
		using var second = new TextBox { Parent = split.Panel2 };
		var provider = new SplitContainerControlStateEventProvider();
		var changes = 0;
		provider.StateChanged += () => changes++;
		provider.SetControl(split);
		first.Text = "One";
		second.Text = "Two";
		Assert.That(changes, Is.EqualTo(2));
		provider.Clear();
		first.Text = "No longer";
		second.Text = "Observed";
		Assert.That(changes, Is.EqualTo(2));
		Assert.That(split.IsDisposed, Is.False);
	}

	[Test]
	public void ClearedContainerProviderDoesNotRetainItsFormerBorrowedTree() {
		var provider = new ContainerControlStateEventProvider();
		var weak = CreateAndClearBorrowedTree(provider);
		GC.Collect();
		GC.WaitForPendingFinalizers();
		GC.Collect();
		Assert.That(weak.IsAlive, Is.False);
		GC.KeepAlive(provider);
	}

	[Test]
	public void DisposingABorrowedInputAutomaticallyClearsItsProvider() {
		var provider = new CommonControlStateEventProvider();
		var changes = 0;
		provider.StateChanged += () => changes++;
		var editor = new TextBox();
		provider.SetControl(editor);
		editor.Text = "Live";
		editor.Dispose();
		Assert.That(provider.Control, Is.Null);
		provider.Clear();
		Assert.That(changes, Is.EqualTo(1));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void OwnerNotificationsCopyThenCallHookThenPublishExactlyOnce(bool form) {
		using var probe = CreateProbe(form);
		probe.InitializeLoaded();
		probe.State.ResetCounts();
		probe.StateChanged += probe.State.Event;
		probe.Editor.Text = "Edited";
		Assert.That(probe.State.Model, Is.EqualTo("Edited"));
		Assert.That(probe.State.Order, Is.EqualTo(new[] { "copy", "hook", "event" }));
	}

	[TestCase(false, FinishedUpdateBehaviour.DoNothing)]
	[TestCase(true, FinishedUpdateBehaviour.DoNothing)]
	[TestCase(false, FinishedUpdateBehaviour.Default)]
	[TestCase(true, FinishedUpdateBehaviour.Default)]
	[TestCase(false, FinishedUpdateBehaviour.ForceCopyUIToModel)]
	[TestCase(true, FinishedUpdateBehaviour.ForceCopyUIToModel)]
	[TestCase(false, FinishedUpdateBehaviour.CopyModelToUI)]
	[TestCase(true, FinishedUpdateBehaviour.CopyModelToUI)]
	[TestCase(false, FinishedUpdateBehaviour.ForceCopyUIToModel | FinishedUpdateBehaviour.NotifyStateChanged)]
	[TestCase(true, FinishedUpdateBehaviour.ForceCopyUIToModel | FinishedUpdateBehaviour.NotifyStateChanged)]
	[TestCase(false, FinishedUpdateBehaviour.ForceCopyUIToModel | FinishedUpdateBehaviour.CopyModelToUI | FinishedUpdateBehaviour.NotifyStateChanged)]
	[TestCase(true, FinishedUpdateBehaviour.ForceCopyUIToModel | FinishedUpdateBehaviour.CopyModelToUI | FinishedUpdateBehaviour.NotifyStateChanged)]
	public void CompletionFlagsHonorCopiesAndNotificationWithoutReentrantCopies(bool form, FinishedUpdateBehaviour behaviour) {
		using var probe = CreateProbe(form);
		probe.InitializeLoaded();
		probe.State.ResetCounts();
		probe.StateChanged += probe.State.Event;
		using (Tools.Scope.EnterUpdateScope(probe, behaviour)) {
			probe.Editor.Text = "Edited";
			probe.State.Model = "Model";
			Assert.That(probe.State.Events, Is.Zero);
		}

		var notify = behaviour.HasFlag(FinishedUpdateBehaviour.NotifyStateChanged);
		Assert.That(probe.Updating, Is.False);
		Assert.That(probe.State.Events, Is.EqualTo(notify ? 1 : 0));
		Assert.That(probe.State.ModelCopies, Is.EqualTo(notify || behaviour.HasFlag(FinishedUpdateBehaviour.ForceCopyUIToModel) ? 1 : 0));
		Assert.That(probe.State.UICopies, Is.EqualTo(behaviour.HasFlag(FinishedUpdateBehaviour.CopyModelToUI) ? 1 : 0));
		if (notify)
			Assert.That(probe.State.Order.TakeLast(2), Is.EqualTo(new[] { "hook", "event" }));
	}

	[TestCase(false)]
	[TestCase(true)]
	public void InnerScopeDoesNotCompleteTheOuterAcquisition(bool form) {
		using var probe = CreateProbe(form);
		probe.InitializeLoaded();
		probe.State.ResetCounts();
		probe.StateChanged += probe.State.Event;
		using (Tools.Scope.EnterUpdateScope(probe)) {
			using (Tools.Scope.EnterUpdateScope(probe, FinishedUpdateBehaviour.DoNothing))
				probe.Editor.Text = "First";
			Assert.That(probe.Updating, Is.True);
			Assert.That(probe.State.Events, Is.Zero);
			probe.Editor.Text = "Last";
		}
		Assert.That(probe.Updating, Is.False);
		Assert.That(probe.State.ModelCopies, Is.EqualTo(1));
		Assert.That(probe.State.Model, Is.EqualTo("Last"));
		Assert.That(probe.State.Events, Is.EqualTo(1));
	}

	[TestCase(false, false)]
	[TestCase(false, true)]
	[TestCase(true, false)]
	[TestCase(true, true)]
	public void FailedCompletionCopyRestoresTheUpdateGate(bool form, bool copyToUI) {
		using var probe = CreateProbe(form);
		probe.InitializeLoaded();
		probe.State.ResetCounts();
		probe.State.ThrowModelCopy = !copyToUI;
		probe.State.ThrowUICopy = copyToUI;
		var scope = Tools.Scope.EnterUpdateScope(probe, copyToUI ? FinishedUpdateBehaviour.CopyModelToUI : FinishedUpdateBehaviour.ForceCopyUIToModel);
		probe.Editor.Text = "Changed";
		Assert.That(scope.Dispose, Throws.TypeOf<InvalidOperationException>());
		Assert.That(probe.Updating, Is.False);
	}

	[Test]
	public void RepeatedLoadDoesNotPrimeAnAlreadyLoadedUserControl() {
		using var probe = new UserProbe();
		probe.InitializeLoaded();
		probe.State.Model = "New model waiting for an explicit refresh";
		probe.InitializeLoaded();
		Assert.That(probe.State.UICopies, Is.EqualTo(1));
		Assert.That(probe.Editor.Text, Is.Empty);
	}

	[Test]
	public void SemanticOwnerCanOptOutOfBroadChildObservation() {
		using var probe = new SemanticProbe();
		probe.InitializeLoaded();
		probe.State.ResetCounts();
		probe.StateChanged += probe.State.Event;
		probe.Editor.Text = "Uncommitted operation";
		Assert.That(probe.State.Events, Is.Zero);
		probe.Commit();
		Assert.That(probe.State.Events, Is.EqualTo(1));
		Assert.That(probe.State.Model, Is.EqualTo("Uncommitted operation"));
	}

	[Test]
	public void CheckedNotificationsReadCommittedValuesAndStopAfterClearOrDisposal() {
		RunWithMessageLoop(async owner => {
			using var choices = new CheckedListBox { Parent = owner };
			choices.Items.AddRange(new object[] { "First", "Second" });
			var provider = new CommonControlStateEventProvider();
			var values = new List<CheckState[]>();
			provider.SetControl(choices);
			provider.StateChanged += () => values.Add(new[] { choices.GetItemCheckState(0), choices.GetItemCheckState(1) });
			choices.SetItemChecked(0, true);
			choices.SetItemCheckState(1, CheckState.Indeterminate);
			await Task.Yield();
			Assert.That(values, Has.Count.EqualTo(1));
			Assert.That(values[0], Is.EqualTo(new[] { CheckState.Checked, CheckState.Indeterminate }));

			choices.SetItemChecked(0, false);
			provider.Clear();
			await Task.Yield();
			Assert.That(values, Has.Count.EqualTo(1));
			provider.SetControl(choices);
			choices.SetItemChecked(0, true);
			choices.Dispose();
			await Task.Yield();
			Assert.That(provider.Control, Is.Null);
			Assert.That(values, Has.Count.EqualTo(1));
		});
	}

	[Test]
	public void APreviousCheckedNotificationCannotConsumeTheSameControlsNewAttachment() {
		RunWithMessageLoop(async owner => {
			using var choices = new CheckedListBox { Parent = owner };
			choices.Items.AddRange(new object[] { "First", "Second" });
			var provider = new CommonControlStateEventProvider();
			var changes = 0;
			provider.StateChanged += () => changes++;
			provider.SetControl(choices);
			choices.SetItemChecked(0, true);
			provider.Clear();
			provider.SetControl(choices);

			// The old post precedes this marker; only the new attachment's later post may publish its edit.
			var changesBetweenPosts = -1;
			owner.BeginInvoke(() => changesBetweenPosts = changes);
			choices.SetItemChecked(1, true);
			await Task.Yield();

			Assert.That(changesBetweenPosts, Is.Zero);
			Assert.That(changes, Is.EqualTo(1));
			Assert.That(choices.CheckedItems, Has.Count.EqualTo(2));
			provider.Clear();
		});
	}

	[Test]
	public void CancelledNativeCheckDoesNotManufactureAStateChange() {
		RunWithMessageLoop(async owner => {
			using var choices = new CheckedListBox { Parent = owner };
			choices.Items.Add("First");
			var provider = new CommonControlStateEventProvider();
			provider.SetControl(choices);
			var changes = 0;
			provider.StateChanged += () => changes++;
			choices.ItemCheck += (_, args) => args.NewValue = args.CurrentValue;
			choices.SetItemChecked(0, true);
			await Task.Yield();
			Assert.That(choices.GetItemChecked(0), Is.False);
			Assert.That(changes, Is.Zero);
			provider.Clear();
		});
	}

	[TestCase(false, FinishedUpdateBehaviour.DoNothing)]
	[TestCase(true, FinishedUpdateBehaviour.DoNothing)]
	[TestCase(false, FinishedUpdateBehaviour.Default)]
	[TestCase(true, FinishedUpdateBehaviour.Default)]
	[TestCase(false, FinishedUpdateBehaviour.ForceCopyUIToModel | FinishedUpdateBehaviour.NotifyStateChanged)]
	[TestCase(true, FinishedUpdateBehaviour.ForceCopyUIToModel | FinishedUpdateBehaviour.NotifyStateChanged)]
	public void ScopedChecksFinishWithCommittedStateAndNeverLeakAPostedNotification(bool form, FinishedUpdateBehaviour behaviour) {
		RunWithMessageLoop(async owner => {
			using var probe = CreateProbe(form);
			if (probe.Root is Form childForm)
				childForm.TopLevel = false;
			owner.Controls.Add(probe.Root);
			probe.Root.Show();
			probe.InitializeLoaded();
			probe.State.ResetCounts();
			probe.StateChanged += probe.State.Event;
			using (Tools.Scope.EnterUpdateScope(probe, behaviour)) {
				probe.Choices.SetItemChecked(0, true);
				Assert.That(probe.State.Events, Is.Zero);
			}
			await Task.Yield();
			var notify = behaviour.HasFlag(FinishedUpdateBehaviour.NotifyStateChanged);
			Assert.That(probe.State.Events, Is.EqualTo(notify ? 1 : 0));
			Assert.That(probe.State.ModelCopies, Is.EqualTo(notify ? 1 : 0));
			Assert.That(probe.State.ModelChecked, Is.EqualTo(notify));
		});
	}

	[Test]
	public void NestedFormAndUserControlScopesKeepTheirOwnCompletionPolicy() {
		RunWithMessageLoop(async owner => {
			using var form = new FormProbe { TopLevel = false };
			owner.Controls.Add(form);
			form.Show();
			using var child = new UserProbe { Parent = form };
			child.InitializeLoaded();
			form.State.ResetCounts();
			child.State.ResetCounts();
			form.StateChanged += form.State.Event;
			child.StateChanged += child.State.Event;
			using (form.EnterUpdateScope(FinishedUpdateBehaviour.DoNothing)) {
				using (child.EnterUpdateScope())
					child.Choices.SetItemChecked(0, true);
				Assert.That(child.State.ModelChecked, Is.True);
				Assert.That(child.State.Events, Is.EqualTo(1));
				Assert.That(form.State.Events, Is.Zero);
			}
			await Task.Yield();
			Assert.That(form.State.Events, Is.Zero);
			Assert.That(child.State.Events, Is.EqualTo(1));
		});
	}

	// Factories return metadata-free native inputs; no controls are constructed during NUnit discovery.
	private static (Control Control, Action<int> Change) CreateInput(string kind) {
		switch (kind) {
			case "Text": {
				var control = new TextBox();
				return (control, value => control.Text = value.ToString());
			}
			case "RichText": {
				var control = new RichTextBox();
				return (control, value => control.Text = value.ToString());
			}
			case "Masked": {
				var control = new MaskedTextBox { Mask = "000" };
				return (control, value => control.Text = value.ToString("D3"));
			}
			case "Combo": {
				var control = new ComboBox();
				control.Items.AddRange(new object[] { "Zero", "One", "Two", "Three" });
				return (control, value => control.SelectedIndex = value % 4);
			}
			case "Check": {
				var control = new CheckBox { ThreeState = true };
				return (control, value => control.CheckState = (CheckState)(value % 3));
			}
			case "Radio": {
				var control = new RadioButton();
				return (control, value => control.Checked = value % 2 != 0);
			}
			case "Numeric": {
				var control = new NumericUpDown();
				return (control, value => control.Value = value);
			}
			case "List": {
				var control = new ListBox();
				control.Items.AddRange(new object[] { "Zero", "One", "Two", "Three" });
				return (control, value => control.SelectedIndex = value % 4);
			}
			case "Track": {
				var control = new TrackBar();
				return (control, value => control.Value = value);
			}
			case "Scroll": {
				var control = new HScrollBar();
				return (control, value => control.Value = value);
			}
			case "Calendar": {
				var control = new MonthCalendar();
				return (control, value => control.SetDate(new DateTime(2020, 1, value + 1)));
			}
			case "Date": {
				var control = new DateTimePicker { Value = new DateTime(2020, 1, 1) };
				return (control, value => control.Value = new DateTime(2020, 1, value + 1));
			}
			case "Domain": {
				var control = new DomainUpDown();
				control.Items.AddRange(new object[] { "Zero", "One", "Two", "Three" });
				return (control, value => control.SelectedIndex = value % 4);
			}
			default:
				throw new ArgumentOutOfRangeException(nameof(kind));
		}
	}

	// A separate stack frame makes the borrowed tree collectible while the cleared provider remains live.
	[MethodImpl(MethodImplOptions.NoInlining)]
	private static WeakReference CreateAndClearBorrowedTree(ContainerControlStateEventProvider provider) {
		var container = new Panel();
		container.Controls.Add(new TextBox());
		provider.SetControl(container);
		provider.Clear();
		return new WeakReference(container);
	}

	private static IProbe CreateProbe(bool form) => form ? new FormProbe() : new UserProbe();

	// Every deferred native observation runs on one owned WinForms dispatcher.
	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var previous = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previous));
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new System.Drawing.Point(-20000, -20000) };
		using var watchdog = new System.Windows.Forms.Timer { Interval = 20000 };
		watchdog.Tick += (_, _) => {
			failure = new AssertionException("The native state observation did not complete.");
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
		Assert.That(failure, Is.Null, failure?.ToString());
	}

	// Two native ancestry families exercise the same public update contract.
	private interface IProbe : IUpdatable {
		event EventHandlerEx StateChanged;
		Control Root { get; }
		TextBox Editor { get; }
		CheckedListBox Choices { get; }
		ProbeState State { get; }
		void InitializeLoaded();
	}

	// Model copies record their observable order without inspecting production fields.
	private sealed class ProbeState {
		private readonly List<string> _order = new();
		public string Model { get; set; } = string.Empty;
		public bool ModelChecked { get; set; }
		public int ModelCopies { get; private set; }
		public int UICopies { get; private set; }
		public int Events { get; private set; }
		public bool ThrowModelCopy { get; set; }
		public bool ThrowUICopy { get; set; }
		public string[] Order => _order.ToArray();

		public void Capture(TextBox editor, CheckedListBox choices) {
			ModelCopies++;
			_order.Add("copy");
			if (ThrowModelCopy)
				throw new InvalidOperationException("Simulated model copy failure.");
			Model = editor.Text;
			ModelChecked = choices.GetItemChecked(0);
		}

		public void Apply(TextBox editor, CheckedListBox choices) {
			UICopies++;
			_order.Add("ui");
			if (ThrowUICopy)
				throw new InvalidOperationException("Simulated UI copy failure.");
			editor.Text = Model;
			choices.SetItemChecked(0, ModelChecked);
		}

		public void Hook() => _order.Add("hook");

		public void Event() {
			Events++;
			_order.Add("event");
		}

		public void ResetCounts() {
			_order.Clear();
			ModelCopies = 0;
			UICopies = 0;
			Events = 0;
		}
	}

	private class UserProbe : UserControlEx, IProbe {
		public UserProbe() {
			Editor = new TextBox { Parent = this };
			Choices = new CheckedListBox { Parent = this };
			Choices.Items.Add("Choice");
		}
		public Control Root => this;
		public TextBox Editor { get; }
		public CheckedListBox Choices { get; }
		public ProbeState State { get; } = new();
		public void InitializeLoaded() => OnLoad(EventArgs.Empty);
		protected override void CopyUIToModel() => State.Capture(Editor, Choices);
		protected override void CopyModelToUI() => State.Apply(Editor, Choices);
		protected override void OnStateChanged() => State.Hook();
	}

	private sealed class FormProbe : FormEx, IProbe {
		public FormProbe() {
			Editor = new TextBox { Parent = this };
			Choices = new CheckedListBox { Parent = this };
			Choices.Items.Add("Choice");
		}
		public Control Root => this;
		public TextBox Editor { get; }
		public CheckedListBox Choices { get; }
		public ProbeState State { get; } = new();
		public void InitializeLoaded() => OnLoad(EventArgs.Empty);
		protected override void CopyUIToModel() => State.Capture(Editor, Choices);
		protected override void CopyModelToUI() => State.Apply(Editor, Choices);
		protected override void OnStateChanged() => State.Hook();
	}

	private sealed class SemanticProbe : UserProbe {
		public void Commit() => NotifyStateChanged();
		protected override bool ShouldObserveChildStateChanges(Control child) => false;
	}

	// A consumer-owned numeric specialization must retain the generic base's semantic event contract.
	private sealed class LongBox : NumericBoxBase<long> {
		protected override bool CanParse(string text) => long.TryParse(text, out _);

		protected override long? DoParse(string text) => long.TryParse(text, out var value) ? value : null;

		protected override string ToText(long value) => value.ToString();
	}

	// Equal presentation text must not collapse different selected values.
	private sealed class CaptionItem {
		public override string ToString() => "Shared caption";
	}
}
