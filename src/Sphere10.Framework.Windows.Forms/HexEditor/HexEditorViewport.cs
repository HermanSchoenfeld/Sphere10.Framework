// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Globalization;
using System.Text;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>
/// The non-visual state of a hex editor over a <see cref="HexEditorDocument"/>: the visible row window, caret,
/// selection, active pane and overwrite typing. Offsets are 64-bit; the caret ranges over <c>[0, Length]</c>
/// where <c>Length</c> is the end position after the last byte. Selection is the half-open range between the
/// anchor and the caret. Every state change raises <see cref="Changed"/> so a view can repaint.
/// </summary>
public class HexEditorViewport {
	/// <summary>Raised after any viewport, caret, selection or document content change.</summary>
	public event EventHandlerEx Changed;

	public const int MinBytesPerRow = 1;
	public const int MaxBytesPerRow = 256;
	public const int DefaultBytesPerRow = 16;
	public const int MinAddressDigits = 8;
	public const long DefaultMaxCopyBytes = 16L * 1024 * 1024;
	private readonly HexEditorDocument _document;
	private int _bytesPerRow;
	private int _visibleRows;
	private long _firstVisibleRow;
	private long _caretOffset;
	private int _caretNibble;
	private long _selectionAnchor;
	private long _pointerAnchor;
	private int _addressDigits;
	private long _baseAddress;
	private bool _insertMode;
	private HexEditorPane _activePane;

	public HexEditorViewport(HexEditorDocument document) {
		Guard.ArgumentNotNull(document, nameof(document));
		_document = document;
		_bytesPerRow = DefaultBytesPerRow;
		_visibleRows = 1;
		_selectionAnchor = -1;
		_pointerAnchor = -1;
		MaxCopyBytes = DefaultMaxCopyBytes;
		_document.Changed += DocumentChanged;
	}

	public HexEditorDocument Document => _document;

	/// <summary>The number of bytes shown per row (1 to 256; 8, 16 and 32 are the usual choices).</summary>
	public int BytesPerRow {
		get => _bytesPerRow;
		set {
			Guard.ArgumentInRange(value, MinBytesPerRow, MaxBytesPerRow, nameof(value));
			if (_bytesPerRow == value)
				return;
			_bytesPerRow = value;
			_firstVisibleRow = Math.Min(_firstVisibleRow, MaxFirstVisibleRow);
			EnsureCaretVisible();
			NotifyChanged();
		}
	}

	/// <summary>How many rows the view can display; the view updates this on resize.</summary>
	public int VisibleRows {
		get => _visibleRows;
		set {
			Guard.ArgumentGTE(value, 1, nameof(value));
			if (_visibleRows == value)
				return;
			_visibleRows = value;
			_firstVisibleRow = Math.Min(_firstVisibleRow, MaxFirstVisibleRow);
			NotifyChanged();
		}
	}

	/// <summary>The total rows, including the row holding the end position after the last byte.</summary>
	public long RowCount => _document.Length / _bytesPerRow + 1;

	/// <summary>The greatest <see cref="FirstVisibleRow"/> that still fills the view where possible.</summary>
	public long MaxFirstVisibleRow => Math.Max(0, RowCount - _visibleRows);

	/// <summary>The first row shown; assignment clamps to the valid range.</summary>
	public long FirstVisibleRow {
		get => _firstVisibleRow;
		set {
			var clamped = Math.Clamp(value, 0, MaxFirstVisibleRow);
			if (_firstVisibleRow == clamped)
				return;
			_firstVisibleRow = clamped;
			NotifyChanged();
		}
	}

	/// <summary>The caret byte position, from 0 to the document length (the end position).</summary>
	public long CaretOffset => _caretOffset;

	/// <summary>Which nibble of the caret byte hex typing replaces next: 0 for the high nibble, 1 for the low.</summary>
	public int CaretNibble => _caretNibble;

	/// <summary>The pane receiving keyboard input.</summary>
	public HexEditorPane ActivePane {
		get => _activePane;
		set {
			if (_activePane == value)
				return;
			_activePane = value;
			_caretNibble = 0;
			NotifyChanged();
		}
	}

	public bool HasSelection => _selectionAnchor >= 0 && _selectionAnchor != _caretOffset;

	/// <summary>The first selected offset, or the caret when nothing is selected.</summary>
	public long SelectionStart => HasSelection ? Math.Min(_selectionAnchor, _caretOffset) : _caretOffset;

	/// <summary>The number of selected bytes, or zero.</summary>
	public long SelectionLength => HasSelection ? Math.Abs(_caretOffset - _selectionAnchor) : 0;

	/// <summary>The exclusive end of the selection.</summary>
	public long SelectionEnd => SelectionStart + SelectionLength;

	/// <summary>The configured offset column width in hex digits, or 0 to size it from the document length.</summary>
	public int AddressDigits {
		get => _addressDigits;
		set {
			Guard.ArgumentInRange(value, 0, 16, nameof(value));
			if (_addressDigits == value)
				return;
			_addressDigits = value;
			NotifyChanged();
		}
	}

	/// <summary>The address shown for document offset zero, so a memory region or file slice displays its real virtual address or file offset. Offsets in the API stay document-relative.</summary>
	public long BaseAddress {
		get => _baseAddress;
		set {
			Guard.ArgumentNotNegative(value, nameof(value));
			if (_baseAddress == value)
				return;
			_baseAddress = value;
			NotifyChanged();
		}
	}

	/// <summary>The offset column width actually used: the configured width, or at least eight digits widened to fit the last displayed address.</summary>
	public int EffectiveAddressDigits {
		get {
			if (_addressDigits > 0)
				return _addressDigits;
			var digits = Math.Max(1, (_baseAddress + _document.Length).ToString("X", CultureInfo.InvariantCulture).Length);
			return Math.Max(MinAddressDigits, digits + digits % 2);
		}
	}

	/// <summary>The largest selection that clipboard copies accept.</summary>
	public long MaxCopyBytes { get; set; }

	/// <summary>Whether the caret byte exists and the document accepts edits.</summary>
	public bool CanEditAtCaret => !_document.IsReadOnly && _caretOffset < _document.Length;

	/// <summary>Reads one row's bytes, returning how many exist (less than <see cref="BytesPerRow"/> on the last row).</summary>
	public int ReadRow(long row, Span<byte> buffer) {
		Guard.ArgumentInRange(row, 0, RowCount - 1, nameof(row));
		Guard.Argument(buffer.Length >= _bytesPerRow, nameof(buffer), "The buffer must hold a full row.");
		var rowStart = row * _bytesPerRow;
		var count = (int)Math.Min(_bytesPerRow, _document.Length - rowStart);
		return count <= 0 ? 0 : _document.Read(rowStart, buffer.Slice(0, count));
	}

	/// <summary>Whether an offset lies in the selection.</summary>
	public bool IsSelected(long offset) => HasSelection && offset >= SelectionStart && offset < SelectionEnd;

	/// <summary>The row containing an offset.</summary>
	public long RowOf(long offset) => offset / _bytesPerRow;

	/// <summary>Formats a document offset as its displayed address (<see cref="BaseAddress"/> plus the offset) with the effective address width.</summary>
	public string FormatOffset(long offset)
		=> (_baseAddress + offset).ToString("X" + EffectiveAddressDigits.ToString(CultureInfo.InvariantCulture), CultureInfo.InvariantCulture);

	/// <summary>Parses a hexadecimal (optionally 0x-prefixed) or decimal offset text.</summary>
	public static bool TryParseOffset(string text, bool hexadecimal, out long offset) {
		offset = 0;
		var trimmed = text?.Trim();
		if (string.IsNullOrEmpty(trimmed))
			return false;
		if (hexadecimal) {
			if (trimmed.StartsWith("0x", StringComparison.OrdinalIgnoreCase))
				trimmed = trimmed.Substring(2);
			return long.TryParse(trimmed, NumberStyles.AllowHexSpecifier, CultureInfo.InvariantCulture, out offset) && offset >= 0;
		}
		return long.TryParse(trimmed, NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out offset) && offset >= 0;
	}

	/// <summary>Places the caret, extending the selection from the previous caret when requested or clearing it otherwise.</summary>
	public void MoveCaretTo(long offset, bool extendSelection) {
		var clamped = Math.Clamp(offset, 0, _document.Length);
		if (extendSelection) {
			if (_selectionAnchor < 0)
				_selectionAnchor = _caretOffset;
		} else {
			_selectionAnchor = -1;
		}
		_caretOffset = clamped;
		_caretNibble = 0;
		EnsureCaretVisibleCore();
		NotifyChanged();
	}

	/// <summary>Moves the caret by a signed number of bytes, saturating at the document bounds.</summary>
	public void MoveCaret(long delta, bool extendSelection) => MoveCaretTo(Offset(_caretOffset, delta), extendSelection);

	/// <summary>Moves the caret by whole rows.</summary>
	public void MoveRows(long rows, bool extendSelection) => MoveCaret(rows * _bytesPerRow, extendSelection);

	/// <summary>Scrolls the view and moves the caret by whole pages.</summary>
	public void MovePages(int pages, bool extendSelection) {
		var rows = (long)pages * _visibleRows;
		_firstVisibleRow = Math.Clamp(_firstVisibleRow + rows, 0, MaxFirstVisibleRow);
		MoveRows(rows, extendSelection);
	}

	public void MoveToRowStart(bool extendSelection) => MoveCaretTo(_caretOffset - _caretOffset % _bytesPerRow, extendSelection);

	/// <summary>Moves to the last byte of the caret's row, or the end position on the final row.</summary>
	public void MoveToRowEnd(bool extendSelection) {
		var rowStart = _caretOffset - _caretOffset % _bytesPerRow;
		MoveCaretTo(Math.Min(_document.Length, rowStart + _bytesPerRow - 1), extendSelection);
	}

	public void MoveToDocumentStart(bool extendSelection) => MoveCaretTo(0, extendSelection);

	public void MoveToDocumentEnd(bool extendSelection) => MoveCaretTo(_document.Length, extendSelection);

	/// <summary>Jumps to a byte, clamping to the last byte, clearing the selection and centering the row in the view.</summary>
	public void GoTo(long offset) {
		var target = Math.Clamp(offset, 0, Math.Max(0, _document.Length - 1));
		_selectionAnchor = -1;
		_caretOffset = target;
		_caretNibble = 0;
		_firstVisibleRow = Math.Clamp(RowOf(target) - _visibleRows / 2, 0, MaxFirstVisibleRow);
		NotifyChanged();
	}

	/// <summary>Scrolls so that a row is the first visible row, clamping to the valid range.</summary>
	public void ScrollTo(long row) => FirstVisibleRow = row;

	/// <summary>Scrolls by a signed number of rows.</summary>
	public void ScrollBy(long rows) => FirstVisibleRow = Offset(_firstVisibleRow, rows);

	/// <summary>Scrolls the minimum distance needed to show the caret row.</summary>
	public void EnsureCaretVisible() {
		if (EnsureCaretVisibleCore())
			NotifyChanged();
	}

	public void SelectAll() {
		if (_document.Length == 0)
			return;
		_selectionAnchor = 0;
		_caretOffset = _document.Length;
		_caretNibble = 0;
		NotifyChanged();
	}

	/// <summary>Selects a range, placing the caret at its start so the data inspector reflects the first selected byte.</summary>
	public void Select(long start, long length) {
		Guard.ArgumentInRange(start, 0, _document.Length, nameof(start));
		Guard.ArgumentInRange(length, 0, _document.Length - start, nameof(length));
		_selectionAnchor = length == 0 ? -1 : start + length;
		_caretOffset = start;
		_caretNibble = 0;
		if (_caretOffset < _firstVisibleRow * _bytesPerRow || RowOf(_caretOffset) >= _firstVisibleRow + _visibleRows)
			_firstVisibleRow = Math.Clamp(RowOf(_caretOffset) - _visibleRows / 2, 0, MaxFirstVisibleRow);
		NotifyChanged();
	}

	public void ClearSelection() {
		if (_selectionAnchor < 0)
			return;
		_selectionAnchor = -1;
		NotifyChanged();
	}

	/// <summary>Starts a pointer gesture on a byte: activates the pane, places the caret and remembers the drag origin.</summary>
	public void BeginPointerSelection(HexEditorPane pane, long offset, int nibble, bool extendSelection) {
		Guard.ArgumentInRange(nibble, 0, 1, nameof(nibble));
		var clamped = Math.Clamp(offset, 0, _document.Length);
		_activePane = pane;
		if (extendSelection) {
			if (_selectionAnchor < 0)
				_selectionAnchor = _caretOffset;
		} else {
			_selectionAnchor = -1;
		}
		_pointerAnchor = clamped;
		_caretOffset = clamped;
		_caretNibble = pane == HexEditorPane.Hex && clamped < _document.Length ? nibble : 0;
		EnsureCaretVisibleCore();
		NotifyChanged();
	}

	/// <summary>Extends a pointer gesture to the byte under the pointer, selecting the bytes between it and the origin inclusively.</summary>
	public void ContinuePointerSelection(long offset) {
		if (_pointerAnchor < 0)
			return;
		var clamped = Math.Clamp(offset, 0, _document.Length);
		if (clamped >= _pointerAnchor) {
			_selectionAnchor = _pointerAnchor;
			_caretOffset = Math.Min(_document.Length, clamped + 1);
		} else {
			_selectionAnchor = Math.Min(_document.Length, _pointerAnchor + 1);
			_caretOffset = clamped;
		}
		if (_selectionAnchor == _caretOffset)
			_selectionAnchor = -1;
		_caretNibble = 0;
		EnsureCaretVisibleCore();
		NotifyChanged();
	}

	/// <summary>Ends a pointer gesture.</summary>
	public void EndPointerSelection() => _pointerAnchor = -1;

	/// <summary>Whether typing and pasting insert new bytes at the caret instead of overwriting existing ones. Insert mode also types at the end position.</summary>
	public bool InsertMode {
		get => _insertMode;
		set {
			if (_insertMode == value)
				return;
			_insertMode = value;
			_caretNibble = 0;
			NotifyChanged();
		}
	}

	/// <summary>Types a hex digit at the caret: the high nibble inserts a new byte in insert mode or overwrites otherwise; the low nibble completes the byte and advances. Returns false when not editable.</summary>
	public bool TypeHexDigit(char character) {
		if (!TryParseHexDigit(character, out var digit) || _document.IsReadOnly)
			return false;
		if (_caretNibble == 0) {
			if (_insertMode)
				_document.Insert(_caretOffset, stackalloc byte[] { (byte)(digit << 4) });
			else if (_caretOffset < _document.Length) {
				_document.TryReadByte(_caretOffset, out var current);
				_document.WriteByte(_caretOffset, (byte)((digit << 4) | (current & 0x0F)));
			} else
				return false;
			_caretNibble = 1;
		} else {
			_document.TryReadByte(_caretOffset, out var current);
			_document.WriteByte(_caretOffset, (byte)((current & 0xF0) | digit));
			_caretOffset++;
			_caretNibble = 0;
		}
		_selectionAnchor = -1;
		EnsureCaretVisibleCore();
		NotifyChanged();
		return true;
	}

	/// <summary>Types a character code (0x00 to 0xFF) at the caret, inserting or overwriting by mode, and advances. Returns false when not editable.</summary>
	public bool TypeCharacter(char character) {
		if (character > 0xFF || _document.IsReadOnly || !_insertMode && _caretOffset >= _document.Length)
			return false;
		if (_insertMode)
			_document.Insert(_caretOffset, stackalloc byte[] { (byte)character });
		else
			_document.WriteByte(_caretOffset, (byte)character);
		_caretOffset++;
		_caretNibble = 0;
		_selectionAnchor = -1;
		EnsureCaretVisibleCore();
		NotifyChanged();
		return true;
	}

	/// <summary>Puts bytes at the caret: inserted in insert mode, otherwise overwriting (appending beyond the end). A selection is replaced first. Returns false when not editable.</summary>
	public bool InsertBytes(ReadOnlySpan<byte> bytes) {
		if (_document.IsReadOnly)
			return false;
		if (HasSelection) {
			var start = SelectionStart;
			_document.Delete(start, SelectionLength);
			_caretOffset = start;
		}
		if (_insertMode || HasSelection)
			_document.Insert(_caretOffset, bytes);
		else
			_document.Overwrite(_caretOffset, bytes);
		_caretOffset += bytes.Length;
		_caretNibble = 0;
		_selectionAnchor = -1;
		EnsureCaretVisibleCore();
		NotifyChanged();
		return true;
	}

	/// <summary>Deletes the selection, or the caret byte, shrinking the document. Returns false when nothing was deleted.</summary>
	public bool DeleteForward() {
		if (_document.IsReadOnly)
			return false;
		if (HasSelection)
			return DeleteSelection();
		if (_caretOffset >= _document.Length)
			return false;
		_document.Delete(_caretOffset, 1);
		_caretNibble = 0;
		EnsureCaretVisibleCore();
		NotifyChanged();
		return true;
	}

	/// <summary>Deletes the selection, or the byte before the caret, shrinking the document. Returns false when nothing was deleted.</summary>
	public bool DeleteBackward() {
		if (_document.IsReadOnly)
			return false;
		if (HasSelection)
			return DeleteSelection();
		if (_caretOffset == 0)
			return false;
		_caretOffset--;
		_document.Delete(_caretOffset, 1);
		_caretNibble = 0;
		EnsureCaretVisibleCore();
		NotifyChanged();
		return true;
	}

	/// <summary>Reads the selected bytes, bypassing the block cache. Rejects selections above <see cref="MaxCopyBytes"/>.</summary>
	public byte[] GetSelectionBytes() {
		Guard.Ensure(HasSelection, "Nothing is selected.");
		Guard.Ensure(SelectionLength <= MaxCopyBytes, $"The selection exceeds the {MaxCopyBytes:N0} byte copy limit.");
		var bytes = new byte[SelectionLength];
		var read = _document.ReadUncached(SelectionStart, bytes);
		return read == bytes.Length ? bytes : bytes.AsSpan(0, read).ToArray();
	}

	/// <summary>The selection as space-separated uppercase hexadecimal pairs.</summary>
	public string GetSelectionHex() {
		var bytes = GetSelectionBytes();
		var text = new StringBuilder(bytes.Length * 3);
		for (var index = 0; index < bytes.Length; index++) {
			if (index > 0)
				text.Append(' ');
			text.Append(bytes[index].ToString("X2", CultureInfo.InvariantCulture));
		}
		return text.ToString();
	}

	/// <summary>The selection decoded as printable ASCII, with other bytes shown as '.'.</summary>
	public string GetSelectionText() {
		var bytes = GetSelectionBytes();
		var text = new char[bytes.Length];
		for (var index = 0; index < bytes.Length; index++)
			text[index] = ToDisplayCharacter(bytes[index]);
		return new string(text);
	}

	/// <summary>The character shown for a byte in the text pane.</summary>
	public static char ToDisplayCharacter(byte value) => value >= 0x20 && value <= 0x7E ? (char)value : '.';

	private bool DeleteSelection() {
		var start = SelectionStart;
		_document.Delete(start, SelectionLength);
		_caretOffset = start;
		_caretNibble = 0;
		_selectionAnchor = -1;
		EnsureCaretVisibleCore();
		NotifyChanged();
		return true;
	}

	protected virtual void OnChanged() {
	}

	protected void NotifyChanged() {
		OnChanged();
		Changed?.Invoke();
	}

	// Scrolls so the caret row is inside the window, reporting whether the window moved.
	private bool EnsureCaretVisibleCore() {
		var row = RowOf(_caretOffset);
		var previous = _firstVisibleRow;
		if (row < _firstVisibleRow)
			_firstVisibleRow = row;
		else if (row >= _firstVisibleRow + _visibleRows)
			_firstVisibleRow = row - _visibleRows + 1;
		_firstVisibleRow = Math.Clamp(_firstVisibleRow, 0, MaxFirstVisibleRow);
		return _firstVisibleRow != previous;
	}

	// An edit elsewhere (the document API, another view) can shrink the content below the caret or selection.
	private void DocumentChanged() {
		_caretOffset = Math.Clamp(_caretOffset, 0, _document.Length);
		if (_selectionAnchor > _document.Length)
			_selectionAnchor = _document.Length;
		_firstVisibleRow = Math.Clamp(_firstVisibleRow, 0, MaxFirstVisibleRow);
		NotifyChanged();
	}

	// Saturating addition keeps 64-bit navigation safe near the bounds.
	private static long Offset(long value, long delta) {
		if (delta > 0 && value > long.MaxValue - delta)
			return long.MaxValue;
		if (delta < 0 && value < long.MinValue - delta)
			return long.MinValue;
		return value + delta;
	}

	private static bool TryParseHexDigit(char character, out int digit) {
		digit = character switch {
			>= '0' and <= '9' => character - '0',
			>= 'a' and <= 'f' => character - 'a' + 10,
			>= 'A' and <= 'F' => character - 'A' + 10,
			_ => -1
		};
		return digit >= 0;
	}
}
