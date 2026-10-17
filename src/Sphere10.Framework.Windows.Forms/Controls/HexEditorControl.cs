// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Edits an owned byte buffer through a bounded hexadecimal and ASCII viewport.</summary>
public partial class HexEditorControl : UserControl {
	public event EventHandlerEx BytesChanged;
	private const int BytesPerRow = 16;
	private const int BytesPerPage = 4096;
	private readonly int _ownerThreadId;
	private byte[] _bytes = Array.Empty<byte>();
	private byte[] _originalBytes = Array.Empty<byte>();
	private int _pageStart;
	private int _selectionStart;
	private int _selectionLength;
	private bool _readOnly;
	private bool _updating = true;
	private bool _pushingValue;
	private bool _pushRejected;

	public HexEditorControl() {
		_ownerThreadId = Environment.CurrentManagedThreadId;
		InitializeComponent();
		_updating = false;
		RefreshPage();
	}

	/// <summary>Gets a copy of the edited bytes. Assignment copies the input and establishes a new unchanged baseline.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public byte[] Bytes {
		get { EnsureOwnerThread(); return (byte[])_bytes.Clone(); }
		set {
			EnsureOwnerThread();
			var input = value ?? Array.Empty<byte>();
			var changed = !_bytes.AsSpan().SequenceEqual(input);
			_bytes = (byte[])input.Clone();
			_originalBytes = (byte[])input.Clone();
			_selectionStart = _selectionLength = _pageStart = 0;
			RefreshPage();
			if (changed)
				BytesChanged?.Invoke();
		}
	}

	[Category("Behavior"), DefaultValue(false)]
	public bool ReadOnly {
		get => _readOnly;
		set { EnsureOwnerThread(); _readOnly = value; _grid.ReadOnly = value; UpdateCommandState(); }
	}

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public bool HasChanges { get { EnsureOwnerThread(); return !_bytes.AsSpan().SequenceEqual(_originalBytes); } }

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public int Length => _bytes.Length;

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public int SelectionStart => _selectionStart;

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public int SelectionLength => _selectionLength;

	/// <summary>Completes a pending native cell edit, returning false if its value is invalid.</summary>
	public bool CommitEdit() {
		EnsureOwnerThread();
		if (!_grid.IsCurrentCellInEditMode)
			return true;
		// DataGridView.EndEdit does not run CellValidating. Validate its live editing text before native commit can push the value.
		if (_grid.EditingControl is TextBox textBox && !TryReadByte(textBox.Text, out _)) {
			_grid.CurrentRow.ErrorText = "Enter exactly two hexadecimal digits (00–FF), or press Escape to cancel.";
			return false;
		}
		_pushRejected = false;
		return _grid.EndEdit() && !_pushRejected;
	}

	public void SelectRange(int start, int length) {
		EnsureOwnerThread();
		ValidateRange(start, length);
		_selectionStart = start;
		_selectionLength = length;
		_pageStart = start / BytesPerPage * BytesPerPage;
		RefreshPage();
	}

	public void SetByte(int offset, byte value) {
		EnsureEditable();
		Guard.ArgumentInRange(offset, 0, _bytes.Length - 1, nameof(offset));
		if (_bytes[offset] == value)
			return;
		_bytes[offset] = value;
		NotifyBufferChanged();
	}

	public void InsertBytes(int offset, byte[] bytes) {
		Guard.ArgumentNotNull(bytes, nameof(bytes));
		ReplaceBytes(offset, 0, bytes);
	}

	public void DeleteBytes(int offset, int count) => ReplaceBytes(offset, count, Array.Empty<byte>());

	/// <summary>Replaces a range with the supplied bytes. Offsets and lengths are validated before any mutation.</summary>
	public void ReplaceBytes(int offset, int count, byte[] bytes) {
		EnsureEditable();
		Guard.ArgumentNotNull(bytes, nameof(bytes));
		ValidateRange(offset, count);
		Guard.Argument(bytes.Length <= int.MaxValue - (_bytes.Length - count), nameof(bytes), "The resulting buffer is too large.");
		if (count == bytes.Length && _bytes.AsSpan(offset, count).SequenceEqual(bytes))
			return;
		var updated = new byte[_bytes.Length - count + bytes.Length];
		_bytes.AsSpan(0, offset).CopyTo(updated);
		bytes.AsSpan().CopyTo(updated.AsSpan(offset));
		_bytes.AsSpan(offset + count).CopyTo(updated.AsSpan(offset + bytes.Length));
		_bytes = updated;
		_selectionStart = offset;
		_selectionLength = bytes.Length;
		_pageStart = offset / BytesPerPage * BytesPerPage;
		NotifyBufferChanged();
	}

	/// <summary>Decodes hexadecimal pairs with optional whitespace and replaces the selected range.</summary>
	public void PasteHex(string text) {
		EnsureEditable();
		var bytes = ParseHex(text);
		Guard.Argument(bytes.Length != 0, nameof(text), "Enter at least one hexadecimal byte pair.");
		ReplaceBytes(_selectionStart, _selectionLength, bytes);
	}

	public string GetSelectedHex() {
		EnsureOwnerThread();
		return HexEncoding.Encode(_bytes.AsSpan(_selectionStart, _selectionLength).ToArray()).ToUpperInvariant();
	}

	/// <summary>Finds an exact byte sequence, optionally wrapping to the beginning. Returns -1 when absent.</summary>
	public int FindBytes(byte[] pattern, int start = 0, bool wrap = true) {
		EnsureOwnerThread();
		Guard.ArgumentNotNull(pattern, nameof(pattern));
		Guard.Argument(pattern.Length != 0, nameof(pattern), "The search pattern cannot be empty.");
		Guard.ArgumentInRange(start, 0, _bytes.Length, nameof(start));
		var found = _bytes.AsSpan(start).IndexOf(pattern);
		if (found >= 0)
			return start + found;
		return wrap && start > 0 ? _bytes.AsSpan(0, (int)Math.Min(_bytes.Length, (long)start + pattern.Length - 1)).IndexOf(pattern) : -1;
	}

	public void RevertChanges() {
		EnsureEditable();
		if (!HasChanges)
			return;
		_bytes = (byte[])_originalBytes.Clone();
		_selectionStart = _selectionLength = _pageStart = 0;
		NotifyBufferChanged();
	}

	private void EnsureOwnerThread() {
		Guard.Ensure(Environment.CurrentManagedThreadId == _ownerThreadId, "Use the hex editor on its creating UI thread.");
		ObjectDisposedException.ThrowIf(IsDisposed || Disposing, this);
	}

	private void EnsureEditable() {
		EnsureOwnerThread();
		Guard.Ensure(!_readOnly, "This hex editor is read-only.");
	}

	private void ValidateRange(int offset, int count) {
		Guard.ArgumentInRange(offset, 0, _bytes.Length, nameof(offset));
		Guard.Argument(count >= 0 && count <= _bytes.Length - offset, nameof(count), "The byte range is outside the buffer.");
	}

	private static byte[] ParseHex(string text) {
		Guard.ArgumentNotNull(text, nameof(text));
		return HexEncoding.Decode(string.Concat(text.Where(character => !char.IsWhiteSpace(character))));
	}

	private void NotifyBufferChanged() {
		if (!_pushingValue)
			RefreshPage();
		else {
			_grid.Invalidate();
			UpdateCommandState();
		}
		BytesChanged?.Invoke();
	}

	private void RefreshPage() {
		if (_grid == null)
			return;
		var previous = _updating;
		using var update = Tools.Scope.ExecuteOnDispose(() => _updating = previous);
		_updating = true;
		_pageStart = Math.Min(_pageStart, _bytes.Length / BytesPerPage * BytesPerPage);
		_grid.RowCount = Math.Min(BytesPerPage / BytesPerRow, (_bytes.Length - _pageStart) / BytesPerRow + 1);
		_grid.ClearSelection();
		if (_selectionStart >= _pageStart && _selectionStart < _pageStart + BytesPerPage) {
			var relative = _selectionStart - _pageStart;
			_grid.CurrentCell = _grid.Rows[relative / BytesPerRow].Cells[relative % BytesPerRow + 1];
			_grid.ClearSelection();
		}
		for (var offset = Math.Max(_selectionStart, _pageStart); offset < Math.Min((long)_selectionStart + _selectionLength, (long)_pageStart + BytesPerPage); offset++) {
			var relative = offset - _pageStart;
			_grid.Rows[relative / BytesPerRow].Cells[relative % BytesPerRow + 1].Selected = true;
		}
		_position.Maximum = _bytes.Length;
		_position.Value = _selectionStart;
		_grid.Invalidate();
		UpdateCommandState();
	}

	private void UpdateCommandState() {
		if (_grid == null)
			return;
		_insert.Enabled = _paste.Enabled = !_readOnly;
		_delete.Enabled = !_readOnly && _selectionLength != 0;
		_revert.Enabled = !_readOnly && HasChanges;
		_copy.Enabled = _selectionLength != 0;
		_first.Enabled = _previous.Enabled = _pageStart != 0;
		_next.Enabled = _last.Enabled = (long)_pageStart + BytesPerPage <= _bytes.Length;
		var pageCount = (long)_bytes.Length / BytesPerPage + 1;
		_page.Text = $"Page {_pageStart / BytesPerPage + 1:N0} / {pageCount:N0}";
		_status.Text = $"{_bytes.Length:N0} bytes  |  Selected: 0x{_selectionStart:X8}, {_selectionLength:N0} bytes" + (HasChanges ? "  |  Modified" : string.Empty);
	}

	private void GridCellValueNeeded(object sender, DataGridViewCellValueEventArgs e) {
		var rowOffset = _pageStart + e.RowIndex * BytesPerRow;
		if (e.ColumnIndex == 0) {
			e.Value = rowOffset.ToString("X8", CultureInfo.InvariantCulture);
			return;
		}
		if (e.ColumnIndex == BytesPerRow + 1) {
			var length = Math.Min(BytesPerRow, _bytes.Length - rowOffset);
			var text = new char[Math.Max(0, length)];
			for (var index = 0; index < text.Length; index++) {
				var value = _bytes[rowOffset + index];
				text[index] = value >= 32 && value <= 126 ? (char)value : '.';
			}
			e.Value = new string(text);
			return;
		}
		var offset = rowOffset + e.ColumnIndex - 1;
		e.Value = offset < _bytes.Length ? _bytes[offset].ToString("X2", CultureInfo.InvariantCulture) : string.Empty;
	}

	private void GridCellBeginEdit(object sender, DataGridViewCellCancelEventArgs e) =>
		e.Cancel = _readOnly || e.ColumnIndex < 1 || e.ColumnIndex > BytesPerRow || (long)_pageStart + e.RowIndex * BytesPerRow + e.ColumnIndex - 1 > _bytes.Length;

	private void GridCellValidating(object sender, DataGridViewCellValidatingEventArgs e) {
		if (!_grid.IsCurrentCellInEditMode || e.ColumnIndex < 1 || e.ColumnIndex > BytesPerRow)
			return;
		if (TryReadByte(e.FormattedValue, out _)) {
			_grid.Rows[e.RowIndex].ErrorText = string.Empty;
			return;
		}
		e.Cancel = true;
		_grid.Rows[e.RowIndex].ErrorText = "Enter exactly two hexadecimal digits (00–FF), or press Escape to cancel.";
	}

	private void GridCellValuePushed(object sender, DataGridViewCellValueEventArgs e) {
		if (_updating || e.ColumnIndex < 1 || e.ColumnIndex > BytesPerRow)
			return;
		var offset = _pageStart + e.RowIndex * BytesPerRow + e.ColumnIndex - 1;
		if (!TryReadByte(e.Value, out var value)) {
			_pushRejected = true;
			_grid.Rows[e.RowIndex].ErrorText = "Enter exactly two hexadecimal digits (00–FF), or press Escape to cancel.";
			return;
		}
		using var edit = Tools.Scope.ExecuteOnDispose(() => _pushingValue = false);
		_pushingValue = true;
		if (offset == _bytes.Length)
			InsertBytes(offset, new[] { value });
		else
			SetByte(offset, value);
	}

	private static bool TryReadByte(object input, out byte value) {
		value = 0;
		var text = Convert.ToString(input, CultureInfo.InvariantCulture)?.Trim();
		return text?.Length == 2 && byte.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
	}

	private void GridCellEndEdit(object sender, DataGridViewCellEventArgs e) {
		if (!_grid.IsHandleCreated || IsDisposed || Disposing)
			return;
		// Rebuild append rows after native cell commit has finished, never from inside CellValuePushed.
		_grid.BeginInvoke(new Action(() => {
			if (!IsDisposed && !Disposing && !_grid.IsCurrentCellInEditMode)
				RefreshPage();
		}));
	}

	private void GridDataError(object sender, DataGridViewDataErrorEventArgs e) {
		e.ThrowException = false;
		e.Cancel = true;
		if (e.RowIndex >= 0 && e.RowIndex < _grid.RowCount)
			_grid.Rows[e.RowIndex].ErrorText = "Enter exactly two hexadecimal digits (00–FF), or press Escape to cancel.";
	}

	private void GridSelectionChanged(object sender, EventArgs e) {
		if (_updating || IsDisposed || Disposing)
			return;
		var selected = _grid.SelectedCells.Cast<DataGridViewCell>().Where(cell => cell.ColumnIndex >= 1 && cell.ColumnIndex <= BytesPerRow)
			.Select(cell => _pageStart + cell.RowIndex * BytesPerRow + cell.ColumnIndex - 1).Where(offset => offset < _bytes.Length).ToArray();
		if (selected.Length != 0) {
			_selectionStart = selected.Min();
			_selectionLength = selected.Max() - _selectionStart + 1;
		} else if (_grid.CurrentCell is { ColumnIndex: >= 1 and <= BytesPerRow } current) {
			_selectionStart = Math.Min(_bytes.Length, _pageStart + current.RowIndex * BytesPerRow + current.ColumnIndex - 1);
			_selectionLength = _selectionStart < _bytes.Length ? 1 : 0;
		}
		var previous = _updating;
		using var update = Tools.Scope.ExecuteOnDispose(() => _updating = previous);
		_updating = true;
		_position.Value = _selectionStart;
		UpdateCommandState();
	}

	private void PositionValueChanged(object sender, EventArgs e) {
		if (!_updating)
			SelectRange((int)_position.Value, _position.Value < _bytes.Length ? 1 : 0);
	}

	private void FirstClicked(object sender, EventArgs e) => SelectRange(0, _bytes.Length > 0 ? 1 : 0);
	private void PreviousClicked(object sender, EventArgs e) => SelectRange(Math.Max(0, _pageStart - BytesPerPage), _bytes.Length > 0 ? 1 : 0);
	private void NextClicked(object sender, EventArgs e) => SelectRange((int)Math.Min(_bytes.Length, (long)_pageStart + BytesPerPage), (long)_pageStart + BytesPerPage < _bytes.Length ? 1 : 0);
	private void LastClicked(object sender, EventArgs e) => SelectRange(_bytes.Length / BytesPerPage * BytesPerPage, _bytes.Length % BytesPerPage > 0 ? 1 : 0);
	private void RevertClicked(object sender, EventArgs e) => RevertChanges();

	private async void InsertClicked(object sender, EventArgs e) => await RunCommandAsync(async () => {
		var result = await EnterTextDialog.ShowAsync(this, "Insert Bytes", "Enter hexadecimal byte pairs, separated by spaces if desired.", "00");
		if (result.Accepted) {
			var bytes = ParseHex(result.UserInput);
			Guard.Argument(bytes.Length != 0, nameof(result.UserInput), "Enter at least one hexadecimal byte pair.");
			InsertBytes(_selectionStart, bytes);
		}
	});

	private async void DeleteClicked(object sender, EventArgs e) => await RunCommandAsync(() => {
		DeleteBytes(_selectionStart, _selectionLength);
		return Task.CompletedTask;
	});

	private async void PasteClicked(object sender, EventArgs e) => await RunCommandAsync(() => {
		PasteHex(Clipboard.GetText());
		return Task.CompletedTask;
	});

	private async void CopyClicked(object sender, EventArgs e) => await RunCommandAsync(() => {
		if (_selectionLength > 0)
			Clipboard.SetText(GetSelectedHex());
		return Task.CompletedTask;
	});

	private async void FindClicked(object sender, EventArgs e) => await RunCommandAsync(() => {
		var pattern = ParseHex(_findText.Text);
		var found = FindBytes(pattern, Math.Min(_bytes.Length, _selectionStart + Math.Max(1, _selectionLength)));
		if (found < 0)
			_status.Text = "Byte sequence not found.";
		else
			SelectRange(found, pattern.Length);
		return Task.CompletedTask;
	});

	private async Task RunCommandAsync(Func<Task> action) {
		try {
			if (CommitEdit())
				await action();
		} catch (Exception error) {
			if (!IsDisposed && !Disposing)
				await DialogEx.ShowAsync(this, SystemIconType.Error, "Hex Editor", error.Message, "OK");
		}
	}

	private void GridKeyDown(object sender, KeyEventArgs e) {
		if (_grid.IsCurrentCellInEditMode)
			return;
		if (e.Control && e.KeyCode == Keys.C) {
			e.Handled = e.SuppressKeyPress = true;
			CopyClicked(sender, e);
		} else if (!_readOnly && e.Control && e.KeyCode == Keys.V) {
			e.Handled = e.SuppressKeyPress = true;
			PasteClicked(sender, e);
		} else if (!_readOnly && e.KeyCode == Keys.Delete) {
			e.Handled = e.SuppressKeyPress = true;
			DeleteClicked(sender, e);
		} else if (e.Control && e.KeyCode == Keys.A) {
			e.Handled = e.SuppressKeyPress = true;
			SelectRange(0, _bytes.Length);
		}
	}
}
