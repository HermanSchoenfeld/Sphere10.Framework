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
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using NUnit.Framework;
using Sphere10.Framework.Utils.WinFormsTester;
using FormsApplication = System.Windows.Forms.Application;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>Drives the real hex editor control, view and form on an STA message loop.</summary>
[TestFixture]
[NonParallelizable]
[Apartment(ApartmentState.STA)]
public class HexEditorControlTests {
	[Test]
	public void ConstructionWithoutServicesBindsStreamsAndMirrorsConfigurationIntoTheViewport() {
		using var editor = new HexEditorControl();
		Assert.That(editor.HasDocument, Is.False);
		Assert.That(FindItem<ToolStripStatusLabel>(editor, "_offsetStatusLabel").Text, Is.EqualTo("No document"));
		Assert.That(FindItem<ToolStripButton>(editor, "_goToButton").Enabled, Is.False);
		editor.OpenStream(new MemoryStream(HexEditorTestScreen.CreateSampleBytes()));
		Assert.That(editor.HasDocument, Is.True);
		Assert.That(editor.Viewport.BytesPerRow, Is.EqualTo(16));
		Assert.That(editor.View.Viewport, Is.SameAs(editor.Viewport));
		editor.BytesPerRow = 32;
		Assert.That(editor.Viewport.BytesPerRow, Is.EqualTo(32));
		Assert.That(FindItem<ToolStripComboBox>(editor, "_bytesPerRowComboBox").SelectedIndex, Is.EqualTo(2));
		editor.ReadOnly = true;
		Assert.That(editor.Document.IsReadOnly, Is.True);
		Assert.That(FindItem<ToolStripStatusLabel>(editor, "_modeStatusLabel").Text, Is.EqualTo("Read-only"));
		editor.ReadOnly = false;
		Assert.That(FindItem<ToolStripStatusLabel>(editor, "_modeStatusLabel").Text, Is.EqualTo("Overwrite"));
		Assert.That(FindItem<ToolStripStatusLabel>(editor, "_lengthStatusLabel").Text, Does.StartWith("Length: "));
		editor.ShowDataInspector = false;
		Assert.That(Find<Panel>(editor, "_inspectorPanel").Visible, Is.False);
		Assert.That(() => editor.BytesPerRow = 0, Throws.TypeOf<ArgumentOutOfRangeException>());
		Assert.That(() => editor.FindNextAsync(), Throws.InvalidOperationException, "There is no previous search.");
	}

	[Test]
	public void TypedEditsUpdateStatusAndInspectorAndSaveWritesTheStream() => RunWithMessageLoop(async owner => {
		var stream = new MemoryStream(new byte[] { 0x00, 0x10, 0x20, 0x30, 0x40, 0x50, 0x60, 0x70, 0x80 });
		using var editor = new HexEditorControl { Dock = DockStyle.Fill };
		owner.Controls.Add(editor);
		editor.OpenStream(stream, false);
		await Task.Yield();
		var states = 0;
		editor.StateChanged += () => states++;
		var documents = 0;
		editor.DocumentChanged += () => documents++;
		var viewport = editor.Viewport;
		var inspector = Find<ListView>(editor, "_inspectorListView");
		Assert.That(inspector.Items.Count, Is.EqualTo(12));
		Assert.That(inspector.Items[0].SubItems[1].Text, Is.EqualTo("0"), "Int8 of the caret byte.");
		Assert.That(inspector.Items[4].SubItems[1].Text, Is.EqualTo(0x30201000.ToString()), "Int32 little endian.");
		viewport.TypeHexDigit('F');
		viewport.TypeHexDigit('F');
		Assert.That(editor.HasChanges, Is.True);
		Assert.That(states, Is.EqualTo(2), "Each committed nibble is one document change.");
		Assert.That(FindItem<ToolStripStatusLabel>(editor, "_changesStatusLabel").Text, Is.EqualTo("Modified: 1 bytes"));
		Assert.That(FindItem<ToolStripStatusLabel>(editor, "_offsetStatusLabel").Text, Does.StartWith("Offset: 00000001"));
		Assert.That(inspector.Items[0].SubItems[1].Text, Is.EqualTo("16"), "The inspector follows the caret to byte 1.");
		viewport.MoveCaretTo(0, false);
		Assert.That(inspector.Items[0].SubItems[1].Text, Is.EqualTo("-1"), "The edited byte is inspected as signed -1.");
		editor.BigEndian = true;
		Assert.That(inspector.Items[4].SubItems[1].Text, Is.EqualTo(unchecked((int)0xFF102030).ToString()), "Int32 big endian.");
		Assert.That(FindItem<ToolStripButton>(editor, "_saveButton").Enabled, Is.True);
		Assert.That(stream.ToArray()[0], Is.Zero, "Nothing is written before Save.");
		editor.Save();
		Assert.That(stream.ToArray()[0], Is.EqualTo(0xFF));
		Assert.That(editor.HasChanges, Is.False);
		Assert.That(FindItem<ToolStripStatusLabel>(editor, "_changesStatusLabel").Text, Is.Empty);
		Assert.That(FindItem<ToolStripStatusLabel>(editor, "_messageStatusLabel").Text, Is.EqualTo("Saved"));
		Assert.That(states, Is.EqualTo(3));
		viewport.Select(2, 3);
		Assert.That(FindItem<ToolStripStatusLabel>(editor, "_selectionStatusLabel").Text, Does.Contain("(3 bytes)"));
		Assert.That(FindItem<ToolStripButton>(editor, "_copyHexButton").Enabled, Is.True);
		Assert.That(documents, Is.Zero);
		editor.CloseDocument();
		Assert.That(documents, Is.EqualTo(1));
		Assert.That(editor.Viewport, Is.Null);
		Assert.That(stream.CanRead, Is.True, "A borrowed stream survives closing.");
	});

	[Test]
	public void FindSelectsMatchesInBothDirectionsOnTheLiveControl() => RunWithMessageLoop(async owner => {
		using var editor = new HexEditorControl { Dock = DockStyle.Fill };
		owner.Controls.Add(editor);
		editor.OpenStream(new SyntheticByteStream(8L * 1024 * 1024));
		await Task.Yield();
		var header = HexEditorSearch.ForText(SyntheticByteStream.BlockHeader(100), Encoding.ASCII);
		var found = await editor.FindAsync(header, false);
		Assert.That(found, Is.EqualTo(100L * SyntheticByteStream.BlockSize));
		Assert.That((editor.Viewport.SelectionStart, editor.Viewport.SelectionLength), Is.EqualTo((found, 16L)));
		Assert.That(editor.Viewport.FirstVisibleRow, Is.LessThanOrEqualTo(editor.Viewport.RowOf(found)));
		Assert.That(FindItem<ToolStripStatusLabel>(editor, "_messageStatusLabel").Text, Is.EqualTo("Found at 0x64000"));
		Assert.That(await editor.FindAsync(header, false), Is.EqualTo(-1), "A unique header is not found again past itself.");
		Assert.That(FindItem<ToolStripStatusLabel>(editor, "_messageStatusLabel").Text, Is.EqualTo("Not found"));
		var anyHeader = HexEditorSearch.ForHex("42 4C 4B 20");
		Assert.That(await editor.FindAsync(anyHeader, true), Is.EqualTo(99L * SyntheticByteStream.BlockSize), "Backward search starts before the current selection.");
		Assert.That(await editor.FindAsync(anyHeader, false), Is.EqualTo(100L * SyntheticByteStream.BlockSize), "Forward search starts after the current selection.");
		Assert.That(editor.IsSearching, Is.False);
		Assert.That(editor.Document.Cache.CachedBytes, Is.LessThanOrEqualTo(editor.Document.Cache.MaxCachedBytes));
	});

	[Test]
	public void ReplacingTheDocumentDisposesOwnedDocumentsAndKeepsBorrowedOnes() {
		using var borrowed = new HexEditorDocument(new MemoryStream(new byte[16]));
		var editor = new HexEditorControl();
		editor.Document = borrowed;
		Assert.That(editor.Document, Is.SameAs(borrowed));
		editor.OpenStream(new MemoryStream(new byte[32]));
		var owned = editor.Document;
		Assert.That(borrowed.Read(0, new byte[1]), Is.EqualTo(1), "Borrowed documents are not disposed when replaced.");
		editor.OpenStream(new MemoryStream(new byte[8]));
		Assert.That(() => owned.Read(0, new byte[1]), Throws.TypeOf<ObjectDisposedException>(), "Owned documents are disposed when replaced.");
		var last = editor.Document;
		editor.Dispose();
		Assert.That(() => last.Read(0, new byte[1]), Throws.TypeOf<ObjectDisposedException>(), "Disposing the control disposes its owned document.");
	}

	[Test]
	public void HitTestingMapsPointerPositionsToBytesAndPanes() => RunWithMessageLoop(async owner => {
		using var editor = new HexEditorControl { Dock = DockStyle.Fill };
		owner.Controls.Add(editor);
		editor.OpenStream(new MemoryStream(new byte[200]));
		await Task.Yield();
		var view = editor.View;
		Assert.That(view.HitTest(new Point(5, 0)), Is.Null, "The header is not a byte.");
		var hexCell = view.GetCellBounds(17, HexEditorPane.Hex);
		Assert.That(hexCell.IsEmpty, Is.False);
		var highNibble = view.HitTest(new Point(hexCell.Left + 1, hexCell.Top + hexCell.Height / 2));
		Assert.That((highNibble.Pane, highNibble.Offset, highNibble.Nibble), Is.EqualTo((HexEditorPane.Hex, 17L, 0)));
		var lowNibble = view.HitTest(new Point(hexCell.Right - 1, hexCell.Top + 1));
		Assert.That((lowNibble.Pane, lowNibble.Offset, lowNibble.Nibble), Is.EqualTo((HexEditorPane.Hex, 17L, 1)));
		var textCell = view.GetCellBounds(35, HexEditorPane.Text);
		var text = view.HitTest(new Point(textCell.Left + textCell.Width / 2, textCell.Top + 1));
		Assert.That((text.Pane, text.Offset, text.Nibble), Is.EqualTo((HexEditorPane.Text, 35L, 0)));
		var offsetColumn = view.HitTest(new Point(2, hexCell.Top + 1));
		Assert.That((offsetColumn.Pane, offsetColumn.Offset), Is.EqualTo((HexEditorPane.Hex, 16L)), "The offset column resolves to the row's first byte.");
		var belowContent = view.HitTest(new Point(hexCell.Left, view.ClientSize.Height - 1));
		Assert.That(belowContent.Offset, Is.LessThanOrEqualTo(200), "Rows beyond the document clamp to the end position.");
		editor.BytesPerRow = 8;
		Assert.That(view.GetCellBounds(9, HexEditorPane.Hex).Top, Is.EqualTo(view.GetCellBounds(1, HexEditorPane.Hex).Top + view.RowHeight));
		Assert.That(view.GetCellBounds(10_000, HexEditorPane.Hex).IsEmpty, Is.True, "Rows outside the window have no bounds.");
	});

	[Test]
	public void TheFormTracksTheDocumentTitleAndModificationState() => RunWithMessageLoop(async owner => {
		var stream = new MemoryStream(new byte[] { 1, 2, 3 });
		var pending = HexEditorForm.ShowAsync(owner, stream, "Sample", false);
		var form = await WaitForForm(owner);
		Assert.That(form.Text, Is.EqualTo("Sample - Hex Editor"));
		Assert.That(form.Editor.Document.Length, Is.EqualTo(3));
		form.Editor.Viewport.TypeHexDigit('A');
		form.Editor.Viewport.TypeHexDigit('B');
		Assert.That(form.Text, Is.EqualTo("Sample* - Hex Editor"));
		form.Editor.Save();
		Assert.That(form.Text, Is.EqualTo("Sample - Hex Editor"));
		Assert.That(stream.ToArray()[0], Is.EqualTo(0xAB));
		form.Close();
		await pending;
		Assert.That(form.IsDisposed, Is.True);
		Assert.That(stream.CanRead, Is.True, "The caller keeps a borrowed stream.");
	});

	[TestCase(false)]
	[TestCase(true)]
	public void FormCancellationJoinsNativeModalCleanupAndLeavesItsOwnerUsable(bool cancelBeforeShowing) => RunWithMessageLoop(async owner => {
		using var cancellation = new CancellationTokenSource();
		if (cancelBeforeShowing)
			cancellation.Cancel();
		var pending = HexEditorForm.ShowAsync(owner, new MemoryStream(new byte[] { 1 }), "Sample", true, cancellation.Token);
		if (!cancelBeforeShowing) {
			var form = await WaitForForm(owner);
			form.Editor.Viewport.TypeHexDigit('F');
		}
		cancellation.Cancel();
		await Assert.ThatAsync(() => pending, Throws.InstanceOf<OperationCanceledException>());
		Assert.That(owner.OwnedForms.OfType<HexEditorForm>(), Is.Empty, "Pending edits do not block cancellation.");
		Assert.That(owner.IsDisposed, Is.False);
		Assert.That(owner.Visible, Is.True);
	});

	[Test]
	public void TheDemoScreenOpensTheSampleOnFirstShowAndTheSyntheticStreamOnDemand() => RunWithMessageLoop(async owner => {
		using var screen = new HexEditorTestScreen { Dock = DockStyle.Fill };
		Assert.That(screen.Editor.HasDocument, Is.False, "Construction opens nothing.");
		owner.Controls.Add(screen);
		await Task.Yield();
		Assert.That(screen.Editor.HasDocument, Is.True);
		Assert.That(screen.Editor.Document.Length, Is.EqualTo(HexEditorTestScreen.CreateSampleBytes().Length));
		screen.OpenSynthetic();
		Assert.That(screen.Editor.Document.Length, Is.EqualTo(HexEditorTestScreen.SyntheticLength));
		screen.Editor.Viewport.GoTo(HexEditorTestScreen.SyntheticLength / 2);
		Assert.That(screen.Editor.Document.Cache.CachedBytes, Is.LessThanOrEqualTo(screen.Editor.Document.Cache.MaxCachedBytes));
		Assert.That(Find<Label>(screen, "_statisticsLabel").Text, Does.Contain("synthetic").And.Contain("Stream reads"));
	});

	private static T Find<T>(Control control, string name)
		where T : Control {
		return (T)control.Controls.Find(name, true).Single();
	}

	private static T FindItem<T>(Control control, string name)
		where T : ToolStripItem {
		return Descendants(control).OfType<ToolStrip>().SelectMany(strip => strip.Items.OfType<T>()).Single(item => item.Name == name);
	}

	private static IEnumerable<Control> Descendants(Control control) {
		yield return control;
		foreach (Control child in control.Controls)
			foreach (var descendant in Descendants(child))
				yield return descendant;
	}

	private static async Task<HexEditorForm> WaitForForm(Form owner) {
		for (var index = 0; index < 500; index++) {
			var form = owner.OwnedForms.OfType<HexEditorForm>().FirstOrDefault(candidate => candidate.Visible);
			if (form != null)
				return form;
			await Task.Delay(10);
		}
		throw new AssertionException("The owned hex editor form did not appear.");
	}

	private static void RunWithMessageLoop(Func<Form, Task> test) {
		Exception failure = null;
		var completed = false;
		var finished = false;
		var previousContext = SynchronizationContext.Current;
		using var context = new WindowsFormsSynchronizationContext();
		using var restore = Tools.Scope.ExecuteOnDispose(() => SynchronizationContext.SetSynchronizationContext(previousContext));
		SynchronizationContext.SetSynchronizationContext(context);
		using var owner = new Form { ShowInTaskbar = false, StartPosition = FormStartPosition.Manual, Location = new Point(-20000, -20000), ClientSize = new Size(1000, 700) };
		using var watchdog = new System.Windows.Forms.Timer { Interval = 30000 };
		void Finish() {
			if (finished)
				return;
			finished = true;
			watchdog.Stop();
			using var closeOwner = Tools.Scope.ExecuteOnDispose(owner.Close);
			foreach (var form in owner.OwnedForms.OfType<HexEditorForm>().ToArray())
				form.Dispose();
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
