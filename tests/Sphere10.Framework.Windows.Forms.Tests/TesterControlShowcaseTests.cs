// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Utils.WinFormsTester;
using Sphere10.Framework.Windows.Forms.AppointmentBook;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>Exercises the control showcase's real corrected workflows and resource boundaries.</summary>
[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class TesterControlShowcaseTests {
	[Test]
	public void AppointmentsCanMoveThroughTheUnallocatedColumnWithoutReservingAResource() {
		var source = new MyAppointmentBookDataSource();
		var resource = source.NewColumn("Room A", null);
		var unallocated = new UnallocatedColumn();
		var start = source.StartTime.AddHours(9);
		var end = start.AddHours(1);
		var appointment = source.NewAppointment(resource, "sample", start, end, "Planning", "", "", "", Color.SteelBlue, null);

		Assert.That(source.IsTimeAvailable(unallocated, appointment, start, end), Is.True);
		source.Reschedule(appointment, resource, unallocated, start, end);
		Assert.That(source.GetAppointments(resource), Is.Empty);
		Assert.That(source.GetAppointments(unallocated), Is.EqualTo(new[] { appointment }));

		// An unallocated appointment does not prevent another item from using the original resource.
		Assert.That(source.IsTimeAvailable(resource, null, start, end), Is.True);
		source.Reschedule(appointment, unallocated, resource, start, end);
		Assert.That(source.GetAppointments(unallocated), Is.Empty);
		Assert.That(source.GetAppointments(resource), Is.EqualTo(new[] { appointment }));
	}

	[Test]
	public void PasswordPolicyRequiresUppercaseIndependentlyOfLowercase() {
		using var screen = new PasswordDialogTestScreen();
		var policy = typeof(PasswordDialogTestScreen).GetMethod("PolicyValidator", BindingFlags.Instance | BindingFlags.NonPublic);
		var missingUppercase = ((IEnumerable<string>)policy.Invoke(screen, new object[] { "abcde1" })).ToArray();
		var valid = ((IEnumerable<string>)policy.Invoke(screen, new object[] { "Abcde1" })).ToArray();
		Assert.That(missingUppercase, Has.Some.Contains("uppercase"));
		Assert.That(valid, Is.Empty);
	}

	[TestCase(false)]
	[TestCase(true)]
	public void AcceptedPasswordDialogReportsItsOutcomeWithoutPrintingTheSecret(bool customPolicy) {
		RunWithMessageLoop(async owner => {
			using var screen = new PasswordDialogTestScreen { Dock = DockStyle.Fill };
			owner.Controls.Add(screen);
			await Task.Yield();
			Find<Button>(screen, customPolicy ? "_customButton" : "_standardButton").PerformClick();
			await WaitUntilAsync(() => System.Windows.Forms.Application.OpenForms.OfType<PasswordDialog>().Any());
			var dialog = System.Windows.Forms.Application.OpenForms.OfType<PasswordDialog>().Single();
			const string secret = "NeverPrintThis7";
			Find<TextBox>(dialog, "_passwordTextBox").Text = secret;
			Find<TextBox>(dialog, "_repeatTextBox").Text = secret;
			Find<Button>(dialog, "_okButton").PerformClick();
			var output = Find<TextBox>(screen, "_outputTextBox");
			await WaitUntilAsync(() => output.Text.Contains("OK", StringComparison.Ordinal));
			Assert.That(output.Text, Does.Not.Contain(secret));
			Assert.That(System.Windows.Forms.Application.OpenForms.OfType<PasswordDialog>(), Is.Empty);
		});
	}

	[Test]
	public void FlagReadoutUsesCommittedCheckValuesAndThePresetActions() {
		RunWithMessageLoop(async owner => {
			using var screen = new FlagsCheckedBoxListScreen { Dock = DockStyle.Fill };
			owner.Controls.Add(screen);
			await Task.Yield();
			var flags = Find<FlagsCheckedListBox>(screen, "_flagsCheckedListBox");
			var summary = Find<Label>(screen, "_selectionSummary");
			flags.SetItemChecked(1, true);
			await WaitUntilAsync(() => summary.Text.Contains("Mask: 1 ", StringComparison.Ordinal));
			Assert.That(Convert.ToInt64(flags.SelectedEnum), Is.EqualTo(1));
			Find<Button>(screen, "_allButton").PerformClick();
			await WaitUntilAsync(() => Convert.ToInt64(flags.SelectedEnum) == 7);
			Assert.That(summary.Text, Does.Contain("Mask: 7"));
			Find<Button>(screen, "_noneButton").PerformClick();
			await WaitUntilAsync(() => Convert.ToInt64(flags.SelectedEnum) == 0);
			Assert.That(summary.Text, Does.Contain("Mask: 0"));
		});
	}

	[Test]
	public void DecayConstructionAndNormalDisplayNeverInstallGlobalHooks() {
		RunWithMessageLoop(async owner => {
			using var screen = new DecayGaugeScreen { Dock = DockStyle.Fill };
			var timer = Field<System.Windows.Forms.Timer>(screen, "_timer");
			Assert.That(timer.Enabled, Is.False);
			Assert.That(Field<IMouseHook>(screen, "_mouseHook"), Is.Null);
			Assert.That(Field<IKeyboardHook>(screen, "_keyboardHook"), Is.Null);
			owner.Controls.Add(screen);
			await Task.Yield();
			Assert.That(timer.Enabled, Is.True);
			Assert.That(Field<IMouseHook>(screen, "_mouseHook"), Is.Null);
			Assert.That(Field<IKeyboardHook>(screen, "_keyboardHook"), Is.Null);

			// A zero decay coefficient makes the reset level and event increment independent of elapsed time.
			Find<NumericUpDown>(screen, "_decayCoefficient").Value = 0;
			Find<Button>(screen, "_resetButton").PerformClick();
			Assert.That(screen.DecayGauge.Level, Is.EqualTo(30));
			Find<Button>(screen, "_triggerEventButton").PerformClick();
			Assert.That(screen.DecayGauge.Level, Is.EqualTo(38));
			screen.Visible = false;
			Assert.That(timer.Enabled, Is.False);
			screen.Dispose();
			Assert.That(Field<IMouseHook>(screen, "_mouseHook"), Is.Null);
			Assert.That(Field<IKeyboardHook>(screen, "_keyboardHook"), Is.Null);
		});
	}

	[Test]
	public void PlaceholderClearRestoresEmptyEditorsAndTheExtenderRemainsOwned() {
		RunWithMessageLoop(async owner => {
			var screen = new PlaceHolderTestScreen { Dock = DockStyle.Fill };
			owner.Controls.Add(screen);
			await Task.Yield();
			var extender = Field<PlaceHolderTextExtender>(screen, "_cueTextExtender1");
			var textbox = Find<TextBox>(screen, "textBox1");
			Assert.That(extender.GetPlaceHolderText(textbox), Is.EqualTo("Enter a plain text value"));
			textbox.Text = "A temporary draft";
			Find<Button>(screen, "_clearInputs").PerformClick();
			Assert.That(textbox.Text, Is.Empty);
			var disposed = false;
			extender.Disposed += (_, _) => disposed = true;
			screen.Dispose();
			Assert.That(disposed, Is.True);
			Assert.That(textbox.IsDisposed, Is.True);
		});
	}

	[Test]
	public void DropdownReopeningBorrowsItsContentAndScreenDisposalReleasesIt() {
		RunWithMessageLoop(async owner => {
			var screen = new CustomComboBoxScreen { Dock = DockStyle.Fill };
			owner.Controls.Add(screen);
			await Task.Yield();
			var combo = Find<CustomComboBox>(screen, "customComboBox1");
			var contents = combo.DropDownControl;
			combo.ShowDropDown();
			await Task.Yield();
			combo.HideDropDown();
			Assert.That(contents.IsDisposed, Is.False);
			combo.ShowDropDown();
			await Task.Yield();
			Assert.That(combo.DropDownControl, Is.SameAs(contents));
			combo.HideDropDown();
			screen.Dispose();
			Assert.That(contents.IsDisposed, Is.True);
			Assert.That(Field<UserControl1>(screen, "_editorPopup").IsDisposed, Is.True);
			Assert.That(Field<RichTextBox>(screen, "_textPopup").IsDisposed, Is.True);
		});
	}

	[TestCase(DockStyle.None)]
	[TestCase(DockStyle.Fill)]
	public void DropdownDisposesEachHostAndItsPopupWithoutTakingOwnershipOfContent(DockStyle dock) {
		RunWithMessageLoop(async owner => {
			using var content = new TextBox {
				Multiline = true,
				Size = new Size(300, 150),
				MinimumSize = new Size(180, 80),
				MaximumSize = new Size(600, 400),
				Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right,
				Dock = dock
			};
			var originalDock = content.Dock;
			var originalAnchor = content.Anchor;
			var combo = new CustomComboBox(content) { Width = 240, DropDownSizeMode = CustomComboBox.SizeMode.UseInitialControlSize };
			var closedEvents = 0;
			combo.DropDownClosed += (_, _) => closedEvents++;
			owner.Controls.Add(combo);
			await Task.Yield();
			var popup = Field<PopupControl>(combo, "_popupCtrl");
			combo.ShowDropDown();
			var firstHost = Field<ToolStripControlHost>(popup, "_host");
			var firstSurface = firstHost.Control;
			var dropDown = Field<PopupDropDown>(popup, "_dropDown");
			Assert.That(popup.Control, Is.SameAs(content));
			Assert.That(content.Dock, Is.EqualTo(originalDock));
			Assert.That(content.Anchor, Is.EqualTo(originalAnchor));
			Assert.That(content.Size, Is.EqualTo(firstSurface.ClientSize));
			combo.HideDropDown();
			Assert.That(combo.IsDroppedDown, Is.False);
			Assert.That(closedEvents, Is.EqualTo(1));
			Assert.That(firstHost.IsDisposed, Is.True);
			Assert.That(firstSurface.IsDisposed, Is.True);
			Assert.That(content.IsDisposed, Is.False);
			Assert.That(content.Parent, Is.Null);
			Assert.That(dropDown.IsDisposed, Is.False);
			combo.HideDropDown();
			Assert.That(closedEvents, Is.EqualTo(1));

			combo.ShowDropDown();
			var secondHost = Field<ToolStripControlHost>(popup, "_host");
			var secondSurface = secondHost.Control;
			Assert.That(secondHost, Is.Not.SameAs(firstHost));
			Assert.That(popup.Control, Is.SameAs(content));

			// Native dismissal and an explicit repeated hide share the same host cleanup boundary.
			dropDown.Close();
			combo.HideDropDown();
			Assert.That(secondHost.IsDisposed, Is.True);
			Assert.That(secondSurface.IsDisposed, Is.True);
			Assert.That(closedEvents, Is.EqualTo(2));
			Assert.That(content.IsDisposed, Is.False);
			combo.ShowDropDown();
			var finalHost = Field<ToolStripControlHost>(popup, "_host");
			var finalSurface = finalHost.Control;
			combo.Dispose();
			Assert.That(finalHost.IsDisposed, Is.True);
			Assert.That(finalSurface.IsDisposed, Is.True);
			Assert.That(dropDown.IsDisposed, Is.True);
			Assert.That(content.IsDisposed, Is.False);
			Assert.That(content.Parent, Is.Null);
			Assert.That(content.Dock, Is.EqualTo(originalDock));
			Assert.That(content.Anchor, Is.EqualTo(originalAnchor));
			Assert.That(() => combo.Dispose(), Throws.Nothing);
		});
	}

	[Test]
	public void DropdownConsumerCallbackFailureStillDisposesTheComboAndItsOwnedPopup() {
		RunWithMessageLoop(async owner => {
			using var content = new TextBox { Multiline = true, Size = new Size(300, 150) };
			var combo = new CustomComboBox(content) { Width = 240 };
			owner.Controls.Add(combo);
			await Task.Yield();
			combo.ShowDropDown();
			var popup = Field<PopupControl>(combo, "_popupCtrl");
			var host = Field<ToolStripControlHost>(popup, "_host");
			var dropDown = Field<PopupDropDown>(popup, "_dropDown");
			EventHandler failOnDetach = (_, _) => {
				if (content.Parent == null)
					throw new InvalidOperationException("A consumer failed during popup detachment.");
			};
			content.ParentChanged += failOnDetach;
			try {
				Assert.That(() => combo.Dispose(), Throws.TypeOf<InvalidOperationException>());
			} finally {
				content.ParentChanged -= failOnDetach;
			}
			Assert.That(combo.IsDisposed, Is.True);
			Assert.That(host.IsDisposed, Is.True);
			Assert.That(dropDown.IsDisposed, Is.True);
			Assert.That(content.IsDisposed, Is.False);
		});
	}

	[Test]
	public void ValidationUsesWorkerSnapshotsAndPublishesItsCompletedResultOnTheUiThread() {
		RunWithMessageLoop(async owner => {
			var previous = Control.CheckForIllegalCrossThreadCalls;
			using var restore = Tools.Scope.ExecuteOnDispose(() => Control.CheckForIllegalCrossThreadCalls = previous);
			Control.CheckForIllegalCrossThreadCalls = true;
			using var screen = new ValidationIndicatorTestScreen { Dock = DockStyle.Fill };
			owner.Controls.Add(screen);
			await Task.Yield();
			var indicator = Find<ValidationIndicator>(screen, "inputValidation");
			var completed = new TaskCompletionSource<int>(TaskCreationOptions.RunContinuationsAsynchronously);
			indicator.ValidationStateChanged += (_, _, current) => {
				if (current == ValidationState.Valid)
					completed.TrySetResult(Environment.CurrentManagedThreadId);
			};
			var thread = Environment.CurrentManagedThreadId;
			Find<TextBox>(screen, "validationInput").Text = "Herman";
			Assert.That(await completed.Task.WaitAsync(TimeSpan.FromSeconds(5)), Is.EqualTo(thread));
			Assert.That(Find<TextBox>(screen, "_outputTextBox").Text, Does.Contain("Validating → Valid"));
		});
	}

	[Test]
	public void NativeTabsRetainTheCompositeAndOrdinaryDraftsIndependently() {
		RunWithMessageLoop(async owner => {
			using var screen = new TabControlTestScreen { Dock = DockStyle.Fill };
			owner.Controls.Add(screen);
			await Task.Yield();
			var tabs = Find<TabControl>(screen, "exampleTabs");
			var customMode = Find<RadioButton>(screen, "_customMode");
			var compositeDraft = Find<TextBox>(screen, "_draftInput");
			Assert.That(compositeDraft.ReadOnly, Is.True);
			customMode.Checked = true;
			Assert.That(compositeDraft.ReadOnly, Is.False);
			compositeDraft.Text = "Composite draft";
			tabs.SelectedIndex = 1;
			Find<TextBox>(screen, "_textBox1").Text = "Ordinary tab draft";
			tabs.SelectedIndex = 0;
			Assert.That(customMode.Checked, Is.True);
			Assert.That(compositeDraft.Text, Is.EqualTo("Composite draft"));
			tabs.SelectedIndex = 1;
			Assert.That(Find<TextBox>(screen, "_textBox1").Text, Is.EqualTo("Ordinary tab draft"));
		});
	}

	[TestCase(848)]
	[TestCase(1200)]
	public void NativeListMergerFitsBothListsInsideTheAvailableScreenWidth(int width) {
		RunWithMessageLoop(async owner => {
			owner.ClientSize = new Size(width, 720);
			using var screen = new MiscTestScreen { Dock = DockStyle.Fill };
			owner.Controls.Add(screen);
			await Task.Yield();
			var merger = Find<ListMerger>(screen, "_listMerger1");
			var rightList = Find<ListBox>(merger, "_rightListBox");
			var rightHeader = Find<Label>(merger, "_rightHeaderLabel");
			var mergerBounds = merger.RectangleToScreen(merger.ClientRectangle);
			Assert.That(screen.RectangleToScreen(screen.ClientRectangle).Contains(mergerBounds), Is.True);
			Assert.That(mergerBounds.Contains(rightList.RectangleToScreen(rightList.ClientRectangle)), Is.True);
			Assert.That(mergerBounds.Contains(rightHeader.RectangleToScreen(rightHeader.ClientRectangle)), Is.True);
			Assert.That(merger.LeftItems, Has.Length.EqualTo(3));
		});
	}

	// Named native controls exercise the real Designer-owned surface without production test accessors.
	private static T Find<T>(Control parent, string name)
		where T : Control {
		return (T)parent.Controls.Find(name, true).Single();
	}

	// Reflection is limited to ownership and private policy state that has no public testing seam.
	private static T Field<T>(object owner, string name) {
		return (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic).GetValue(owner);
	}

	// Wait for a semantic outcome while pumping the owning native dispatcher.
	private static async Task WaitUntilAsync(Func<bool> completed) {
		var timeout = Stopwatch.StartNew();
		while (!completed() && timeout.Elapsed < TimeSpan.FromSeconds(5))
			await Task.Delay(10);
		Assert.That(completed(), Is.True, "The native workflow did not complete.");
	}

	// Each case owns a real UI dispatcher, closes stray dialogs and restores the caller's context.
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
		using var watchdog = new System.Windows.Forms.Timer { Interval = 15000 };
		watchdog.Tick += (_, _) => {
			failure = new AssertionException("The control showcase test timed out.");
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
}
