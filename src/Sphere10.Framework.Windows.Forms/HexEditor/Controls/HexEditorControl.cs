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
public partial class HexEditorControl : UserControlEx {
	public event EventHandlerEx BytesChanged;

	private const int _bytesPerRow = 16;
	private const int _bytesPerPage = 4096;
	private readonly int _ownerThreadId;
	private byte[] _bytes = Array.Empty<byte>();
	private byte[] _originalBytes = Array.Empty<byte>();
	private int _pageStart;
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
		get {
			EnsureOwnerThread();
			return (byte[])_bytes.Clone();
		}
		set {
			EnsureOwnerThread();
			var input = value ?? Array.Empty<byte>();
			var changed = !_bytes.AsSpan().SequenceEqual(input);
			_bytes = (byte[])input.Clone();
			_originalBytes = (byte[])input.Clone();
			SelectionStart = SelectionLength = _pageStart = 0;
			RefreshPage();
			if (changed) {
				BytesChanged?.Invoke();
				NotifyStateChanged();
			}
		}
	}

	[Category("Behavior"), DefaultValue(false)]
	public bool ReadOnly {
		get {
			return field;
		}
		set {
			EnsureOwnerThread();
			field = value;
			_grid.ReadOnly = value;
			UpdateCommandState();
		}
	}

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public bool HasChanges {
		get {
			EnsureOwnerThread();
			return !_bytes.AsSpan().SequenceEqual(_originalBytes);
		}
	}

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public int Length => _bytes.Length;

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public int SelectionStart { get; private set; }

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public int SelectionLength { get; private set; }

	/// <summary>Reports committed buffers and ranges rather than native editing drafts and viewport rebuilding.</summary>
	protected override bool ShouldObserveChildStateChanges(Control child) {
		return false;
	}

	private void GridCellValueNeeded(object sender, DataGridViewCellValueEventArgs args) {
		var rowOffset = _pageStart + args.RowIndex * _bytesPerRow;
		if (args.ColumnIndex == 0) {
			args.Value = rowOffset.ToString("X8", CultureInfo.InvariantCulture);
			return;
		}
		if (args.ColumnIndex == _bytesPerRow + 1) {
			var length = Math.Min(_bytesPerRow, _bytes.Length - rowOffset);
			var text = new char[Math.Max(0, length)];
			for (var index = 0; index < text.Length; index++) {
				var value = _bytes[rowOffset + index];
				text[index] = value >= 32 && value <= 126 ? (char)value : '.';
			}
			args.Value = new string(text);
			return;
		}
		var offset = rowOffset + args.ColumnIndex - 1;
		args.Value = offset < _bytes.Length ? _bytes[offset].ToString("X2", CultureInfo.InvariantCulture) : string.Empty;
	}

	private void GridCellBeginEdit(object sender, DataGridViewCellCancelEventArgs args) {
		args.Cancel = ReadOnly || args.ColumnIndex < 1 || args.ColumnIndex > _bytesPerRow || (long)_pageStart + args.RowIndex * _bytesPerRow + args.ColumnIndex - 1 > _bytes.Length;
	}

	private void GridCellValidating(object sender, DataGridViewCellValidatingEventArgs args) {
		if (!_grid.IsCurrentCellInEditMode || args.ColumnIndex < 1 || args.ColumnIndex > _bytesPerRow)
			return;
		if (TryReadByte(args.FormattedValue, out _)) {
			_grid.Rows[args.RowIndex].ErrorText = string.Empty;
			return;
		}
		args.Cancel = true;
		_grid.Rows[args.RowIndex].ErrorText = "Enter exactly two hexadecimal digits (00–FF), or press Escape to cancel.";
	}

	private void GridCellValuePushed(object sender, DataGridViewCellValueEventArgs args) {
		if (_updating || args.ColumnIndex < 1 || args.ColumnIndex > _bytesPerRow)
			return;
		var offset = _pageStart + args.RowIndex * _bytesPerRow + args.ColumnIndex - 1;
		if (!TryReadByte(args.Value, out var value)) {
			_pushRejected = true;
			_grid.Rows[args.RowIndex].ErrorText = "Enter exactly two hexadecimal digits (00–FF), or press Escape to cancel.";
			return;
		}
		using var edit = Tools.Scope.ExecuteOnDispose(() => _pushingValue = false);
		_pushingValue = true;
		if (offset == _bytes.Length)
			InsertBytes(offset, new[] { value });
		else
			SetByte(offset, value);
	}

	public void SetByte(int offset, byte value) {
		EnsureEditable();
		Guard.ArgumentInRange(offset, 0, _bytes.Length - 1, nameof(offset));
		if (_bytes[offset] == value)
			return;
		_bytes[offset] = value;
		NotifyBufferChanged();
	}

	private void GridCellEndEdit(object sender, DataGridViewCellEventArgs args) {
		if (!_grid.IsHandleCreated || IsDisposed || Disposing)
			return;
		// Rebuild append rows after native cell commit has finished, never from inside CellValuePushed.
		_grid.BeginInvoke(new Action(() => {
			if (!IsDisposed && !Disposing && !_grid.IsCurrentCellInEditMode)
				RefreshPage();
		}));
	}

	private void GridDataError(object sender, DataGridViewDataErrorEventArgs args) {
		args.ThrowException = false;
		args.Cancel = true;
		if (args.RowIndex >= 0 && args.RowIndex < _grid.RowCount)
			_grid.Rows[args.RowIndex].ErrorText = "Enter exactly two hexadecimal digits (00–FF), or press Escape to cancel.";
	}

	private void GridSelectionChanged(object sender, EventArgs args) {
		if (_updating || IsDisposed || Disposing)
			return;
		var previousStart = SelectionStart;
		var previousLength = SelectionLength;
		var selected = _grid.SelectedCells.Cast<DataGridViewCell>().Where(cell => cell.ColumnIndex >= 1 && cell.ColumnIndex <= _bytesPerRow)
			.Select(cell => _pageStart + cell.RowIndex * _bytesPerRow + cell.ColumnIndex - 1).Where(offset => offset < _bytes.Length).ToArray();
		if (selected.Length != 0) {
			SelectionStart = selected.Min();
			SelectionLength = selected.Max() - SelectionStart + 1;
		} else if (_grid.CurrentCell is { ColumnIndex: >= 1 and <= _bytesPerRow } current) {
			SelectionStart = Math.Min(_bytes.Length, _pageStart + current.RowIndex * _bytesPerRow + current.ColumnIndex - 1);
			SelectionLength = SelectionStart < _bytes.Length ? 1 : 0;
		}
		var previous = _updating;
		using var update = Tools.Scope.ExecuteOnDispose(() => _updating = previous);
		_updating = true;
		_position.Value = SelectionStart;
		UpdateCommandState();
		if (previousStart != SelectionStart || previousLength != SelectionLength)
			NotifyStateChanged();
	}

	private void PositionValueChanged(object sender, EventArgs args) {
		if (!_updating)
			SelectRange((int)_position.Value, _position.Value < _bytes.Length ? 1 : 0);
	}

	private void FirstClicked(object sender, EventArgs args) {
		SelectRange(0, _bytes.Length > 0 ? 1 : 0);
	}

	private void PreviousClicked(object sender, EventArgs args) {
		SelectRange(Math.Max(0, _pageStart - _bytesPerPage), _bytes.Length > 0 ? 1 : 0);
	}

	private void NextClicked(object sender, EventArgs args) {
		SelectRange((int)Math.Min(_bytes.Length, (long)_pageStart + _bytesPerPage), (long)_pageStart + _bytesPerPage < _bytes.Length ? 1 : 0);
	}

	private void LastClicked(object sender, EventArgs args) {
		SelectRange(_bytes.Length / _bytesPerPage * _bytesPerPage, _bytes.Length % _bytesPerPage > 0 ? 1 : 0);
	}

	private void RevertClicked(object sender, EventArgs args) {
		RevertChanges();
	}

	public void RevertChanges() {
		EnsureEditable();
		if (!HasChanges)
			return;
		_bytes = (byte[])_originalBytes.Clone();
		SelectionStart = SelectionLength = _pageStart = 0;
		NotifyBufferChanged();
	}

	private async void InsertClicked(object sender, EventArgs args) {
		await RunCommandAsync(async () => {
			var result = await EnterTextDialog.ShowAsync(this, "Insert Bytes", "Enter hexadecimal byte pairs, separated by spaces if desired.", "00");
			if (result.Accepted) {
				var bytes = ParseHex(result.UserInput);
				Guard.Argument(bytes.Length != 0, nameof(result.UserInput), "Enter at least one hexadecimal byte pair.");
				InsertBytes(SelectionStart, bytes);
			}
		});
	}

	public void InsertBytes(int offset, byte[] bytes) {
		Guard.ArgumentNotNull(bytes, nameof(bytes));
		ReplaceBytes(offset, 0, bytes);
	}

	private async void FindClicked(object sender, EventArgs args) {
		await RunCommandAsync(() => {
			var pattern = ParseHex(_findText.Text);
			var found = FindBytes(pattern, Math.Min(_bytes.Length, SelectionStart + Math.Max(1, SelectionLength)));
			if (found < 0)
				_status.Text = "Byte sequence not found.";
			else
				SelectRange(found, pattern.Length);
			return Task.CompletedTask;
		});
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

	private void GridKeyDown(object sender, KeyEventArgs args) {
		if (_grid.IsCurrentCellInEditMode)
			return;
		if (args.Control && args.KeyCode == Keys.C) {
			args.Handled = args.SuppressKeyPress = true;
			CopyClicked(sender, args);
		} else if (!ReadOnly && args.Control && args.KeyCode == Keys.V) {
			args.Handled = args.SuppressKeyPress = true;
			PasteClicked(sender, args);
		} else if (!ReadOnly && args.KeyCode == Keys.Delete) {
			args.Handled = args.SuppressKeyPress = true;
			DeleteClicked(sender, args);
		} else if (args.Control && args.KeyCode == Keys.A) {
			args.Handled = args.SuppressKeyPress = true;
			SelectRange(0, _bytes.Length);
		}
	}

	public void SelectRange(int start, int length) {
		EnsureOwnerThread();
		ValidateRange(start, length);
		var changed = SelectionStart != start || SelectionLength != length;
		SelectionStart = start;
		SelectionLength = length;
		_pageStart = start / _bytesPerPage * _bytesPerPage;
		RefreshPage();
		if (changed)
			NotifyStateChanged();
	}

	private async void DeleteClicked(object sender, EventArgs args) {
		await RunCommandAsync(() => {
			DeleteBytes(SelectionStart, SelectionLength);
			return Task.CompletedTask;
		});
	}

	public void DeleteBytes(int offset, int count) {
		ReplaceBytes(offset, count, Array.Empty<byte>());
	}

	private async void PasteClicked(object sender, EventArgs args) {
		await RunCommandAsync(() => {
			PasteHex(Clipboard.GetText());
			return Task.CompletedTask;
		});
	}

	/// <summary>Decodes hexadecimal pairs with optional whitespace and replaces the selected range.</summary>
	public void PasteHex(string text) {
		EnsureEditable();
		var bytes = ParseHex(text);
		Guard.Argument(bytes.Length != 0, nameof(text), "Enter at least one hexadecimal byte pair.");
		ReplaceBytes(SelectionStart, SelectionLength, bytes);
	}

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
		SelectionStart = offset;
		SelectionLength = bytes.Length;
		_pageStart = offset / _bytesPerPage * _bytesPerPage;
		NotifyBufferChanged();
	}

	private void EnsureEditable() {
		EnsureOwnerThread();
		Guard.Ensure(!ReadOnly, "This hex editor is read-only.");
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
		NotifyStateChanged();
	}

	private void RefreshPage() {
		if (_grid == null)
			return;
		var previous = _updating;
		using var update = Tools.Scope.ExecuteOnDispose(() => _updating = previous);
		_updating = true;
		_pageStart = Math.Min(_pageStart, _bytes.Length / _bytesPerPage * _bytesPerPage);
		_grid.RowCount = Math.Min(_bytesPerPage / _bytesPerRow, (_bytes.Length - _pageStart) / _bytesPerRow + 1);
		_grid.ClearSelection();
		if (SelectionStart >= _pageStart && SelectionStart < _pageStart + _bytesPerPage) {
			var relative = SelectionStart - _pageStart;
			_grid.CurrentCell = _grid.Rows[relative / _bytesPerRow].Cells[relative % _bytesPerRow + 1];
			_grid.ClearSelection();
		}
		for (var offset = Math.Max(SelectionStart, _pageStart); offset < Math.Min((long)SelectionStart + SelectionLength, (long)_pageStart + _bytesPerPage); offset++) {
			var relative = offset - _pageStart;
			_grid.Rows[relative / _bytesPerRow].Cells[relative % _bytesPerRow + 1].Selected = true;
		}
		_position.Maximum = _bytes.Length;
		_position.Value = SelectionStart;
		_grid.Invalidate();
		UpdateCommandState();
	}

	private void UpdateCommandState() {
		if (_grid == null)
			return;
		_insert.Enabled = _paste.Enabled = !ReadOnly;
		_delete.Enabled = !ReadOnly && SelectionLength != 0;
		_revert.Enabled = !ReadOnly && HasChanges;
		_copy.Enabled = SelectionLength != 0;
		_first.Enabled = _previous.Enabled = _pageStart != 0;
		_next.Enabled = _last.Enabled = (long)_pageStart + _bytesPerPage <= _bytes.Length;
		var pageCount = (long)_bytes.Length / _bytesPerPage + 1;
		_page.Text = $"Page {_pageStart / _bytesPerPage + 1:N0} / {pageCount:N0}";
		_status.Text = $"{_bytes.Length:N0} bytes  |  Selected: 0x{SelectionStart:X8}, {SelectionLength:N0} bytes" + (HasChanges ? "  |  Modified" : string.Empty);
	}

	private async void CopyClicked(object sender, EventArgs args) {
		await RunCommandAsync(() => {
			if (SelectionLength > 0)
				Clipboard.SetText(GetSelectedHex());
			return Task.CompletedTask;
		});
	}

	public string GetSelectedHex() {
		EnsureOwnerThread();
		return HexEncoding.Encode(_bytes.AsSpan(SelectionStart, SelectionLength).ToArray()).ToUpperInvariant();
	}

	private async Task RunCommandAsync(Func<Task> action) {
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync<Exception>(async () => {
			if (CommitEdit())
				await action();
		});
		if (error != null) {
			if (!IsDisposed && !Disposing)
				await DialogEx.ShowAsync(this, SystemIconType.Error, "Hex Editor", error.Message, "OK");
		}
	}

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

	private void EnsureOwnerThread() {
		Guard.Ensure(Environment.CurrentManagedThreadId == _ownerThreadId, "Use the hex editor on its creating UI thread.");
		ObjectDisposedException.ThrowIf(IsDisposed || Disposing, this);
	}

	private static bool TryReadByte(object input, out byte value) {
		value = 0;
		var text = Convert.ToString(input, CultureInfo.InvariantCulture)?.Trim();
		return text?.Length == 2 && byte.TryParse(text, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out value);
	}
}
