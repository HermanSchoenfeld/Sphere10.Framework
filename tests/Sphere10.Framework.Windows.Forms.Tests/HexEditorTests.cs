// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using FormsApplication = System.Windows.Forms.Application;

namespace Sphere10.Framework.Windows.Forms.Tests;

[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class HexEditorTests {
	[Test]
	public void BufferOwnershipRangeEditsAndRevertPreserveTheOriginalAndReportOnlyChanges() {
		using var editor = new HexEditorControl();
		var input = new byte[] { 0, 1, 2, 3 };
		var changes = 0;
		editor.BytesChanged += () => changes++;
		editor.Bytes = input;
		input[0] = 99;
		var copy = editor.Bytes;
		copy[1] = 99;
		Assert.That(editor.Bytes, Is.EqualTo(new byte[] { 0, 1, 2, 3 }));
		Assert.That(editor.HasChanges, Is.False);
		editor.SetByte(0, 0);
		Assert.That(changes, Is.EqualTo(1));
		editor.SetByte(1, 0x7F);
		editor.InsertBytes(2, new byte[] { 0xAA, 0xBB });
		Assert.That(editor.Bytes, Is.EqualTo(new byte[] { 0, 0x7F, 0xAA, 0xBB, 2, 3 }));
		editor.SelectRange(1, 3);
		editor.PasteHex("DE AD\r\nBE EF");
		Assert.That(editor.Bytes, Is.EqualTo(new byte[] { 0, 0xDE, 0xAD, 0xBE, 0xEF, 2, 3 }));
		Assert.That(editor.GetSelectedHex(), Is.EqualTo("DEADBEEF"));
		editor.DeleteBytes(1, 4);
		Assert.That(editor.Bytes, Is.EqualTo(new byte[] { 0, 2, 3 }));
		Assert.That(editor.HasChanges, Is.True);
		Assert.That(changes, Is.EqualTo(5));
		editor.RevertChanges();
		Assert.That(editor.Bytes, Is.EqualTo(new byte[] { 0, 1, 2, 3 }));
		Assert.That(editor.HasChanges, Is.False);
		Assert.That(changes, Is.EqualTo(6));
		editor.Bytes = editor.Bytes;
		Assert.That(changes, Is.EqualTo(6));
	}

	[TestCase("0")]
	[TestCase("GG")]
	[TestCase("01 Q0")]
	public void InvalidHexAndRangesDoNotPartiallyMutateTheBuffer(string invalid) {
		using var editor = new HexEditorControl { Bytes = new byte[] { 1, 2, 3 } };
		editor.SelectRange(1, 1);
		Assert.That(() => editor.PasteHex(invalid), Throws.TypeOf<FormatException>());
		Assert.That(() => editor.DeleteBytes(2, 2), Throws.ArgumentException);
		Assert.That(() => editor.InsertBytes(4, new byte[] { 9 }), Throws.TypeOf<ArgumentOutOfRangeException>());
		Assert.That(editor.Bytes, Is.EqualTo(new byte[] { 1, 2, 3 }));
		Assert.That(editor.HasChanges, Is.False);
	}

	[Test]
	public void TheViewportStaysBoundedAndSearchAndSelectionWorkAcrossPages() {
		using var editor = new HexEditorControl();
		var bytes = new byte[16 * 1024 * 1024];
		bytes[4095] = 0xAA;
		bytes[4096] = 0xBB;
		bytes[^2] = 0xAA;
		bytes[^1] = 0xBB;
		editor.Bytes = bytes;
		var grid = Find<DataGridView>(editor, "_grid");
		Assert.That(grid.VirtualMode, Is.True);
		Assert.That(grid.RowCount, Is.LessThanOrEqualTo(256));
		Assert.That(grid.Columns, Has.Count.EqualTo(18));
		Assert.That(editor.FindBytes(new byte[] { 0xAA, 0xBB }), Is.EqualTo(4095));
		Assert.That(editor.FindBytes(new byte[] { 0xAA, 0xBB }, 4096), Is.EqualTo(bytes.Length - 2));
		Assert.That(editor.FindBytes(new byte[] { 0xAA, 0xBB }, bytes.Length), Is.EqualTo(4095));
		Assert.That(editor.FindBytes(new byte[] { 0xAA, 0xBB }, bytes.Length, false), Is.EqualTo(-1));
		editor.SelectRange(bytes.Length - 2, 2);
		Assert.That(editor.GetSelectedHex(), Is.EqualTo("AABB"));
		Assert.That(grid.Rows[0].Cells[0].Value, Is.EqualTo((bytes.Length - 4096).ToString("X8")));
		Assert.That(grid.RowCount, Is.LessThanOrEqualTo(256));
	}

	[Test]
	public void ReadonlyModeKeepsFindAndCopyWhileRejectingEveryMutation() {
		using var editor = new HexEditorControl { Bytes = new byte[] { 0x41, 0x42 }, ReadOnly = true };
		editor.SelectRange(0, 2);
		Assert.That(editor.GetSelectedHex(), Is.EqualTo("4142"));
		Assert.That(editor.FindBytes(new byte[] { 0x42 }), Is.EqualTo(1));
		Assert.That(() => editor.SetByte(0, 1), Throws.InvalidOperationException);
		Assert.That(() => editor.InsertBytes(0, new byte[] { 1 }), Throws.InvalidOperationException);
		Assert.That(() => editor.DeleteBytes(0, 1), Throws.InvalidOperationException);
		Assert.That(() => editor.PasteHex("01"), Throws.InvalidOperationException);
		Assert.That(() => editor.RevertChanges(), Throws.InvalidOperationException);
		Assert.That(Find<DataGridView>(editor, "_grid").ReadOnly, Is.True);
		Assert.That(Find<Button>(editor, "_copy").Enabled, Is.True);
		Assert.That(Find<Button>(editor, "_paste").Enabled, Is.False);
		Assert.That(Find<Button>(editor, "_delete").Enabled, Is.False);
		Assert.That(editor.Bytes, Is.EqualTo(new byte[] { 0x41, 0x42 }));
	}

	[Test]
	public void NativeCellEditsValidateHexAndAppendWithoutReentrantGridChanges() => RunWithMessageLoop(async owner => {
		using var editor = new HexEditorControl { Dock = DockStyle.Fill, Bytes = new byte[] { 1, 2 } };
		owner.Controls.Add(editor);
		var grid = Find<DataGridView>(editor, "_grid");
		grid.CurrentCell = grid.Rows[0].Cells[1];
		Assert.That(grid.BeginEdit(true), Is.True);
		((TextBox)grid.EditingControl).Text = "GG";
		Assert.That(editor.CommitEdit(), Is.False);
		Assert.That(editor.HasChanges, Is.False);
		Assert.That(grid.IsCurrentCellInEditMode, Is.True);
		((TextBox)grid.EditingControl).Text = "F";
		Assert.That(editor.CommitEdit(), Is.False);
		Assert.That(editor.HasChanges, Is.False);
		((TextBox)grid.EditingControl).Text = "7F";
		Assert.That(editor.CommitEdit(), Is.True);
		Assert.That(editor.Bytes, Is.EqualTo(new byte[] { 0x7F, 2 }));
		await Task.Yield();
		grid.CurrentCell = grid.Rows[0].Cells[3];
		Assert.That(grid.BeginEdit(true), Is.True);
		((TextBox)grid.EditingControl).Text = "41";
		Assert.That(editor.CommitEdit(), Is.True);
		await Task.Yield();
		Assert.That(editor.Bytes, Is.EqualTo(new byte[] { 0x7F, 2, 0x41 }));
		Assert.That(grid.Rows[0].Cells[17].Value, Is.EqualTo("..A"));
	});

	[TestCase(false)]
	[TestCase(true)]
	public void ADialogReturnsOnlyAcceptedBytesAndNeverChangesItsCallersArray(bool accept) => RunWithMessageLoop(async owner => {
		var input = new byte[] { 1, 2 };
		var pending = HexEditorDialog.ShowAsync(owner, "Edit Bytes", input);
		var dialog = await WaitForDialog(owner);
		Find<HexEditorControl>(dialog, "_editor").SetByte(0, 99);
		Find<Button>(dialog, accept ? "_accept" : "_cancel").PerformClick();
		var result = await pending;
		Assert.That(input, Is.EqualTo(new byte[] { 1, 2 }));
		Assert.That(result, accept ? Is.EqualTo(new byte[] { 99, 2 }) : Is.Null);
		Assert.That(result, Is.Not.SameAs(input));
		Assert.That(dialog.IsDisposed, Is.True);
	});

	[TestCase(false)]
	[TestCase(true)]
	public void DialogCancellationJoinsNativeModalCleanupAndLeavesItsOwnerUsable(bool cancelBeforeShowing) => RunWithMessageLoop(async owner => {
		using var cancellation = new CancellationTokenSource();
		if (cancelBeforeShowing)
			cancellation.Cancel();
		var pending = HexEditorDialog.ShowAsync(owner, "Edit Bytes", new byte[] { 1 }, cancellationToken: cancellation.Token);
		if (!cancelBeforeShowing)
			await WaitForDialog(owner);
		cancellation.Cancel();
		await Assert.ThatAsync(() => pending, Throws.InstanceOf<OperationCanceledException>());
		Assert.That(owner.OwnedForms.OfType<HexEditorDialog>(), Is.Empty);
		Assert.That(owner.IsDisposed, Is.False);
		Assert.That(owner.Visible, Is.True);
	});

	private static T Find<T>(Control control, string name)
		where T : Control => (T)control.Controls.Find(name, true).Single();

	private static async Task<HexEditorDialog> WaitForDialog(Form owner) {
		for (var index = 0; index < 500; index++) {
			var dialog = owner.OwnedForms.OfType<HexEditorDialog>().FirstOrDefault(form => form.Visible);
			if (dialog != null)
				return dialog;
			await Task.Delay(10);
		}
		throw new AssertionException("The owned hex editor dialog did not appear.");
	}

	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var completed = false;
		var finished = false;
		var previousContext = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previousContext));
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000), ClientSize = new Size(900, 650) };
		using var watchdog = new System.Windows.Forms.Timer { Interval = 20000 };
		void Finish() {
			if (finished)
				return;
			finished = true;
			watchdog.Stop();
			using var closeOwner = Tools.Scope.ExecuteOnDispose(owner.Close);
			foreach (var dialog in owner.OwnedForms.OfType<HexEditorDialog>().ToArray())
				dialog.Dispose();
		}
		watchdog.Tick += (_, _) => {
			failure = new AssertionException("The hex editor test timed out.");
			Finish();
		};
		owner.Shown += async (_, _) => {
			using var finish = Tools.Scope.ExecuteOnDispose(Finish);
			try {
				await test(owner);
				completed = true;
			} catch (Exception error) {
				failure = error;
			}
		};
		watchdog.Start();
		FormsApplication.Run(owner);
		Assert.That(failure, Is.Null, failure?.ToString());
		Assert.That(completed, Is.True);
	}
}
