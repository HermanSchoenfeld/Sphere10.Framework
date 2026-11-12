// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.Linq;
using System.Text;
using NUnit.Framework;
using Sphere10.Framework.Utils.WinFormsTester;

namespace Sphere10.Framework.Windows.Forms.Tests;

/// <summary>Exercises caret, selection, scrolling, go-to and overwrite typing on the view model without a message loop.</summary>
[TestFixture]
[Parallelizable(ParallelScope.Children)]
public class HexEditorViewportTests {
	[Test]
	public void CaretMovementClampsToTheDocumentAndScrollsTheWindowMinimally() {
		using var document = new HexEditorDocument(new MemoryStream(new byte[100]));
		var viewport = new HexEditorViewport(document) { BytesPerRow = 16, VisibleRows = 3 };
		Assert.That(viewport.RowCount, Is.EqualTo(7), "The end position after the last byte has its own row.");
		Assert.That(viewport.MaxFirstVisibleRow, Is.EqualTo(4));
		viewport.MoveCaret(20, false);
		Assert.That(viewport.CaretOffset, Is.EqualTo(20));
		Assert.That(viewport.FirstVisibleRow, Is.Zero);
		viewport.MoveRows(3, false);
		Assert.That(viewport.CaretOffset, Is.EqualTo(68));
		Assert.That(viewport.FirstVisibleRow, Is.EqualTo(2), "The window scrolls just far enough to show the caret row.");
		viewport.MoveToRowEnd(false);
		Assert.That(viewport.CaretOffset, Is.EqualTo(79));
		viewport.MoveToRowStart(false);
		Assert.That(viewport.CaretOffset, Is.EqualTo(64));
		viewport.MoveToDocumentEnd(false);
		Assert.That(viewport.CaretOffset, Is.EqualTo(100));
		Assert.That(viewport.FirstVisibleRow, Is.EqualTo(4));
		viewport.MoveCaret(5, false);
		Assert.That(viewport.CaretOffset, Is.EqualTo(100), "The caret saturates at the end position.");
		viewport.MoveToRowEnd(false);
		Assert.That(viewport.CaretOffset, Is.EqualTo(100), "The final row ends at the end position.");
		viewport.MoveToDocumentStart(false);
		Assert.That(viewport.CaretOffset, Is.Zero);
		Assert.That(viewport.FirstVisibleRow, Is.Zero);
		viewport.MoveCaret(-1, false);
		Assert.That(viewport.CaretOffset, Is.Zero);
		viewport.MovePages(1, false);
		Assert.That(viewport.FirstVisibleRow, Is.EqualTo(3));
		Assert.That(viewport.CaretOffset, Is.EqualTo(48));
		viewport.MovePages(-5, false);
		Assert.That(viewport.FirstVisibleRow, Is.Zero);
		Assert.That(viewport.CaretOffset, Is.Zero);
		viewport.MoveCaret(long.MaxValue, false);
		Assert.That(viewport.CaretOffset, Is.EqualTo(100), "64-bit deltas saturate instead of overflowing.");
	}

	[Test]
	public void KeyboardSelectionIsTheHalfOpenRangeBetweenAnchorAndCaret() {
		using var document = new HexEditorDocument(new MemoryStream(new byte[100]));
		var viewport = new HexEditorViewport(document) { VisibleRows = 4 };
		viewport.MoveCaretTo(10, false);
		Assert.That(viewport.HasSelection, Is.False);
		Assert.That(viewport.SelectionStart, Is.EqualTo(10), "Without a selection the start is the caret.");
		Assert.That(viewport.SelectionLength, Is.Zero);
		viewport.MoveCaret(5, true);
		Assert.That((viewport.SelectionStart, viewport.SelectionLength, viewport.SelectionEnd), Is.EqualTo((10L, 5L, 15L)));
		Assert.That(viewport.IsSelected(14), Is.True);
		Assert.That(viewport.IsSelected(15), Is.False);
		viewport.MoveCaret(-8, true);
		Assert.That((viewport.SelectionStart, viewport.SelectionLength), Is.EqualTo((7L, 3L)));
		viewport.MoveCaret(3, true);
		Assert.That(viewport.HasSelection, Is.False, "Returning to the anchor selects nothing.");
		viewport.MoveCaret(2, true);
		viewport.MoveCaret(1, false);
		Assert.That(viewport.HasSelection, Is.False, "Unshifted movement clears the selection.");
		Assert.That(viewport.CaretOffset, Is.EqualTo(13));
		viewport.Select(20, 5);
		Assert.That((viewport.SelectionStart, viewport.SelectionLength, viewport.CaretOffset), Is.EqualTo((20L, 5L, 20L)));
		viewport.Select(20, 0);
		Assert.That(viewport.HasSelection, Is.False);
		Assert.That(() => viewport.Select(96, 5), Throws.TypeOf<ArgumentOutOfRangeException>());
		viewport.SelectAll();
		Assert.That((viewport.SelectionStart, viewport.SelectionLength, viewport.CaretOffset), Is.EqualTo((0L, 100L, 100L)));
		viewport.ClearSelection();
		Assert.That(viewport.HasSelection, Is.False);
		Assert.That(viewport.CaretOffset, Is.EqualTo(100));
	}

	[Test]
	public void PointerGesturesSelectInclusivelyInEitherDirectionAndShiftExtends() {
		using var document = new HexEditorDocument(new MemoryStream(new byte[100]));
		var viewport = new HexEditorViewport(document) { VisibleRows = 4 };
		viewport.BeginPointerSelection(HexEditorPane.Hex, 10, 1, false);
		Assert.That((viewport.CaretOffset, viewport.CaretNibble, viewport.ActivePane), Is.EqualTo((10L, 1, HexEditorPane.Hex)));
		viewport.ContinuePointerSelection(13);
		Assert.That((viewport.SelectionStart, viewport.SelectionLength, viewport.CaretOffset), Is.EqualTo((10L, 4L, 14L)), "Dragging forward includes the byte under the pointer.");
		viewport.ContinuePointerSelection(5);
		Assert.That((viewport.SelectionStart, viewport.SelectionLength, viewport.CaretOffset), Is.EqualTo((5L, 6L, 5L)), "Dragging backward includes the origin byte.");
		viewport.ContinuePointerSelection(10);
		Assert.That((viewport.SelectionStart, viewport.SelectionLength), Is.EqualTo((10L, 1L)));
		viewport.EndPointerSelection();
		viewport.ContinuePointerSelection(50);
		Assert.That(viewport.SelectionLength, Is.EqualTo(1), "Moves after the gesture ends are ignored.");
		viewport.BeginPointerSelection(HexEditorPane.Text, 30, 0, true);
		Assert.That((viewport.SelectionStart, viewport.SelectionLength, viewport.ActivePane), Is.EqualTo((10L, 20L, HexEditorPane.Text)), "Shift-click extends from the existing anchor.");
		viewport.BeginPointerSelection(HexEditorPane.Hex, 200, 1, false);
		Assert.That((viewport.CaretOffset, viewport.CaretNibble, viewport.HasSelection), Is.EqualTo((100L, 0, false)), "Pointer offsets clamp to the end position, which has no nibble.");
		Assert.That(() => viewport.BeginPointerSelection(HexEditorPane.Hex, 0, 2, false), Throws.TypeOf<ArgumentOutOfRangeException>());
	}

	[Test]
	public void GoToClampsToTheLastByteClearsTheSelectionAndCentersTheRow() {
		using var document = new HexEditorDocument(new MemoryStream(new byte[100]));
		var viewport = new HexEditorViewport(document) { VisibleRows = 3 };
		viewport.Select(0, 10);
		viewport.GoTo(50);
		Assert.That(viewport.CaretOffset, Is.EqualTo(50));
		Assert.That(viewport.HasSelection, Is.False);
		Assert.That(viewport.FirstVisibleRow, Is.EqualTo(2), "The target row is centered where possible.");
		viewport.GoTo(1000);
		Assert.That(viewport.CaretOffset, Is.EqualTo(99), "Offsets beyond the end clamp to the last byte.");
		Assert.That(viewport.FirstVisibleRow, Is.EqualTo(viewport.MaxFirstVisibleRow));
		viewport.GoTo(-5);
		Assert.That(viewport.CaretOffset, Is.Zero);
		Assert.That(viewport.FirstVisibleRow, Is.Zero);
		using var empty = new HexEditorDocument(new MemoryStream());
		var emptyViewport = new HexEditorViewport(empty);
		emptyViewport.GoTo(5);
		Assert.That(emptyViewport.CaretOffset, Is.Zero);
		Assert.That(emptyViewport.RowCount, Is.EqualTo(1));
	}

	[Test]
	public void ScrollingClampsAndNotifiesOnlyWhenTheWindowMoves() {
		using var document = new HexEditorDocument(new MemoryStream(new byte[100]));
		var viewport = new HexEditorViewport(document) { VisibleRows = 3 };
		var notifications = 0;
		viewport.Changed += () => notifications++;
		viewport.ScrollTo(100);
		Assert.That(viewport.FirstVisibleRow, Is.EqualTo(4));
		viewport.ScrollTo(4);
		Assert.That(notifications, Is.EqualTo(1), "An unchanged window raises nothing.");
		viewport.ScrollBy(-10);
		Assert.That(viewport.FirstVisibleRow, Is.Zero);
		viewport.VisibleRows = 100;
		Assert.That(viewport.MaxFirstVisibleRow, Is.Zero);
		viewport.ScrollTo(3);
		Assert.That(viewport.FirstVisibleRow, Is.Zero, "A window taller than the document cannot scroll.");
		viewport.VisibleRows = 2;
		viewport.ScrollTo(5);
		viewport.BytesPerRow = 32;
		Assert.That(viewport.RowCount, Is.EqualTo(4));
		Assert.That(viewport.MaxFirstVisibleRow, Is.EqualTo(2));
		Assert.That(viewport.FirstVisibleRow, Is.Zero, "Changing the row width keeps the caret row visible.");
		viewport.MoveCaretTo(99, false);
		viewport.BytesPerRow = 8;
		Assert.That(viewport.FirstVisibleRow, Is.EqualTo(viewport.RowOf(99) - 1), "The caret row stays in the window after relayout.");
		document.WriteByte(0, 1);
		Assert.That(notifications, Is.GreaterThan(4), "Document edits are relayed to the view.");
		Assert.That(() => viewport.BytesPerRow = 0, Throws.TypeOf<ArgumentOutOfRangeException>());
		Assert.That(() => viewport.VisibleRows = 0, Throws.TypeOf<ArgumentOutOfRangeException>());
	}

	[Test]
	public void HexAndTextTypingOverwriteBytesThroughTheDocumentAndAdvanceTheCaret() {
		using var document = new HexEditorDocument(new MemoryStream(new byte[] { 0x00, 0x00, 0x41 }));
		var viewport = new HexEditorViewport(document);
		Assert.That(viewport.CanEditAtCaret, Is.True);
		Assert.That(viewport.TypeHexDigit('a'), Is.True);
		Assert.That((viewport.CaretOffset, viewport.CaretNibble), Is.EqualTo((0L, 1)), "The high nibble is replaced first.");
		Assert.That(document.TryReadByte(0, out var high) && high == 0xA0, Is.True);
		Assert.That(viewport.TypeHexDigit('g'), Is.False);
		Assert.That(viewport.TypeHexDigit('5'), Is.True);
		Assert.That((viewport.CaretOffset, viewport.CaretNibble), Is.EqualTo((1L, 0)), "The low nibble completes the byte and advances.");
		Assert.That(document.TryReadByte(0, out var full) && full == 0xA5, Is.True);
		Assert.That(document.IsModified(0), Is.True);
		viewport.ActivePane = HexEditorPane.Text;
		Assert.That(viewport.TypeCharacter('Z'), Is.True);
		Assert.That(document.TryReadByte(1, out var text) && text == 0x5A, Is.True);
		Assert.That(viewport.TypeCharacter('Ā'), Is.False, "Characters above 0xFF have no single-byte code.");
		Assert.That(viewport.TypeCharacter('x'), Is.True);
		Assert.That(viewport.CaretOffset, Is.EqualTo(3));
		Assert.That(viewport.CanEditAtCaret, Is.False, "The end position holds no byte to overwrite.");
		Assert.That(viewport.TypeCharacter('y'), Is.False);
		Assert.That(viewport.TypeHexDigit('1'), Is.False);
		viewport.Select(0, 2);
		viewport.ActivePane = HexEditorPane.Hex;
		viewport.TypeHexDigit('0');
		Assert.That(viewport.HasSelection, Is.False, "Typing clears the selection.");
		Assert.That(document.ModifiedByteCount, Is.EqualTo(3));
		using var readOnly = new HexEditorDocument(new MemoryStream(new byte[] { 1 }, false));
		var readOnlyViewport = new HexEditorViewport(readOnly);
		Assert.That(readOnlyViewport.CanEditAtCaret, Is.False);
		Assert.That(readOnlyViewport.TypeHexDigit('F'), Is.False);
		Assert.That(readOnly.HasChanges, Is.False);
	}

	[Test]
	public void AddressWidthGrowsWithTheDocumentAndCanBeFixed() {
		using var small = new HexEditorDocument(new MemoryStream(new byte[100]));
		var smallViewport = new HexEditorViewport(small);
		Assert.That(smallViewport.EffectiveAddressDigits, Is.EqualTo(8));
		Assert.That(smallViewport.FormatOffset(255), Is.EqualTo("000000FF"));
		using var large = new HexEditorDocument(new SyntheticByteStream((1L << 32) + 1));
		var largeViewport = new HexEditorViewport(large);
		Assert.That(largeViewport.EffectiveAddressDigits, Is.EqualTo(10), "Widths grow in even steps beyond eight digits.");
		Assert.That(largeViewport.FormatOffset(1L << 32), Is.EqualTo("0100000000"));
		largeViewport.AddressDigits = 12;
		Assert.That(largeViewport.EffectiveAddressDigits, Is.EqualTo(12));
		Assert.That(largeViewport.RowCount, Is.EqualTo((1L << 28) + 1));
		largeViewport.MoveToDocumentEnd(false);
		Assert.That(largeViewport.CaretOffset, Is.EqualTo((1L << 32) + 1));
		Assert.That(() => largeViewport.AddressDigits = 17, Throws.TypeOf<ArgumentOutOfRangeException>());
	}

	[Test]
	public void InsertModeTypesNewBytesAndDeleteKeysShrinkTheDocument() {
		using var document = new HexEditorDocument(new MemoryStream());
		var viewport = new HexEditorViewport(document);
		Assert.That(viewport.TypeHexDigit('A'), Is.False, "Overwrite mode has nothing to type over in an empty document.");
		viewport.InsertMode = true;
		Assert.That(viewport.TypeHexDigit('A') && viewport.TypeHexDigit('B'), Is.True);
		Assert.That(viewport.TypeHexDigit('1') && viewport.TypeHexDigit('2'), Is.True);
		viewport.ActivePane = HexEditorPane.Text;
		Assert.That(viewport.TypeCharacter('Z'), Is.True);
		Assert.That(document.Length, Is.EqualTo(3));
		Assert.That(viewport.CaretOffset, Is.EqualTo(3));
		var content = new byte[3];
		document.Read(0, content);
		Assert.That(content, Is.EqualTo(new byte[] { 0xAB, 0x12, (byte)'Z' }));

		viewport.MoveCaretTo(1, false);
		viewport.InsertBytes(new byte[] { 0xEE, 0xFF });
		Assert.That(document.Length, Is.EqualTo(5));
		Assert.That(viewport.CaretOffset, Is.EqualTo(3));
		Assert.That(viewport.DeleteBackward(), Is.True);
		Assert.That(viewport.CaretOffset, Is.EqualTo(2));
		viewport.Select(0, 2);
		Assert.That(viewport.DeleteForward(), Is.True);
		Assert.That(viewport.HasSelection, Is.False);
		content = new byte[document.Length];
		document.Read(0, content);
		Assert.That(content, Is.EqualTo(new byte[] { 0x12, (byte)'Z' }));

		viewport.InsertMode = false;
		viewport.MoveToDocumentEnd(false);
		Assert.That(viewport.TypeCharacter('Q'), Is.False, "Overwrite mode does not append.");
		viewport.MoveCaretTo(0, false);
		viewport.InsertBytes(new byte[] { 1, 2, 3 });
		content = new byte[document.Length];
		document.Read(0, content);
		Assert.That(content, Is.EqualTo(new byte[] { 1, 2, 3 }), "Overwrite-mode paste replaces in place and appends the remainder.");
		Assert.That(viewport.DeleteForward(), Is.False, "Nothing follows the caret at the end.");
		document.Delete(0, document.Length);
		Assert.That(viewport.CaretOffset, Is.Zero, "External shrinking clamps the caret.");
	}

	[Test]
	public void BaseAddressShiftsDisplayedAddressesWhileOffsetsStayDocumentRelative() {
		using var document = new HexEditorDocument(new MemoryStream(new byte[0x100]));
		var viewport = new HexEditorViewport(document);
		var notifications = 0;
		viewport.Changed += () => notifications++;
		viewport.BaseAddress = 0x7FF6_1234_0000;
		Assert.That(notifications, Is.EqualTo(1));
		Assert.That(viewport.EffectiveAddressDigits, Is.EqualTo(12), "The width fits the last displayed address, not the document length.");
		Assert.That(viewport.FormatOffset(0), Is.EqualTo("7FF612340000"));
		Assert.That(viewport.FormatOffset(0x10), Is.EqualTo("7FF612340010"));
		viewport.GoTo(0x10);
		Assert.That(viewport.CaretOffset, Is.EqualTo(0x10), "Navigation and reads keep using document offsets.");
		viewport.BaseAddress = 0x7FF6_1234_0000;
		Assert.That(notifications, Is.EqualTo(2), "Assigning the same base is not a change.");
		Assert.That(() => viewport.BaseAddress = -1, Throws.TypeOf<ArgumentOutOfRangeException>());
	}

	[Test]
	public void SelectionCopiesFormatHexAndTextAndRespectTheCopyLimit() {
		var bytes = Encoding.ASCII.GetBytes("Hello, World!").Concat(new byte[] { 0x00, 0x7F, 0xFF }).ToArray();
		using var document = new HexEditorDocument(new MemoryStream(bytes));
		var viewport = new HexEditorViewport(document);
		Assert.That(() => viewport.GetSelectionBytes(), Throws.InvalidOperationException, "Nothing is selected.");
		viewport.Select(0, 5);
		Assert.That(viewport.GetSelectionHex(), Is.EqualTo("48 65 6C 6C 6F"));
		Assert.That(viewport.GetSelectionText(), Is.EqualTo("Hello"));
		viewport.Select(12, 4);
		Assert.That(viewport.GetSelectionText(), Is.EqualTo("!..."));
		Assert.That(viewport.GetSelectionHex(), Is.EqualTo("21 00 7F FF"));
		document.WriteByte(13, 0x41);
		Assert.That(viewport.GetSelectionText(), Is.EqualTo("!A.."), "Copies observe pending edits.");
		viewport.MaxCopyBytes = 3;
		Assert.That(() => viewport.GetSelectionBytes(), Throws.InvalidOperationException);
		viewport.Select(0, 3);
		Assert.That(viewport.GetSelectionBytes(), Is.EqualTo(new byte[] { 0x48, 0x65, 0x6C }));
	}

	[TestCase("0x1F", true, true, 31L)]
	[TestCase("1f", true, true, 31L)]
	[TestCase(" FF ", true, true, 255L)]
	[TestCase("1,024", false, true, 1024L)]
	[TestCase("42", false, true, 42L)]
	[TestCase("-1", false, false, 0L)]
	[TestCase("zz", true, false, 0L)]
	[TestCase("", true, false, 0L)]
	[TestCase(null, false, false, 0L)]
	public void OffsetTextParsesAsHexadecimalOrDecimal(string text, bool hexadecimal, bool expectedParsed, long expectedOffset) {
		var parsed = HexEditorViewport.TryParseOffset(text, hexadecimal, out var offset);
		Assert.That(parsed, Is.EqualTo(expectedParsed));
		if (parsed)
			Assert.That(offset, Is.EqualTo(expectedOffset));
	}

	[Test]
	public void RowsAreReadThroughTheDocumentWithShortFinalRows() {
		using var document = new HexEditorDocument(new MemoryStream(Enumerable.Range(0, 20).Select(index => (byte)index).ToArray()));
		var viewport = new HexEditorViewport(document);
		var row = new byte[16];
		Assert.That(viewport.ReadRow(0, row), Is.EqualTo(16));
		Assert.That(row[15], Is.EqualTo(15));
		Assert.That(viewport.ReadRow(1, row), Is.EqualTo(4));
		Assert.That(row[3], Is.EqualTo(19));
		Assert.That(() => viewport.ReadRow(2, row), Throws.TypeOf<ArgumentOutOfRangeException>());
		Assert.That(() => viewport.ReadRow(0, new byte[8]), Throws.ArgumentException);
		Assert.That(HexEditorViewport.ToDisplayCharacter(0x41), Is.EqualTo('A'));
		Assert.That(HexEditorViewport.ToDisplayCharacter(0x1F), Is.EqualTo('.'));
		Assert.That(HexEditorViewport.ToDisplayCharacter(0x7F), Is.EqualTo('.'));
	}
}
