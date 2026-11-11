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
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>
/// A professional hex editor over any seekable <see cref="Stream"/>: a virtual hex/text view with a bounded block
/// cache, overwrite editing, Go To, streaming Find, clipboard copies, a status bar and a data inspector. Bind
/// content with <see cref="OpenFile"/>, <see cref="OpenStream"/> or <see cref="SetDocument"/>; nothing beyond the
/// visible window is ever loaded into memory.
/// </summary>
public partial class HexEditorControl : ApplicationControl {
	/// <summary>Raised after the bound document is replaced or closed.</summary>
	public event EventHandlerEx DocumentChanged;

	/// <summary>Raised after any caret, selection, scroll or content change of the bound document.</summary>
	public event EventHandlerEx ViewChanged;

	private static readonly int[] _bytesPerRowChoices = { 8, 16, 32 };
	private readonly int _ownerThreadId;
	private readonly HexEditorDataInspector _inspector;
	private HexEditorDocument _document;
	private HexEditorViewport _viewport;
	private bool _ownsDocument;
	private int _bytesPerRow;
	private long _baseAddress;
	private bool _insertMode;
	private bool _readOnly;
	private bool _showDataInspector;
	private HexEditorSearch _lastSearch;
	private CancellationTokenSource _searchCancellation;
	private bool _syncingBytesPerRow;

	public HexEditorControl() {
		_ownerThreadId = Environment.CurrentManagedThreadId;
		_inspector = new HexEditorDataInspector();
		_bytesPerRow = HexEditorViewport.DefaultBytesPerRow;
		_showDataInspector = true;
		InitializeComponent();
		SyncBytesPerRowComboBox();
		UpdatePresentation();
	}

	/// <summary>The bound document. Assignment borrows the document; use <see cref="SetDocument"/> to transfer ownership.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public HexEditorDocument Document {
		get => _document;
		set => SetDocument(value, false);
	}

	/// <summary>The view model for the bound document, or null when nothing is open.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public HexEditorViewport Viewport => _viewport;

	/// <summary>The painting surface.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public HexEditorView View => _view;

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public bool HasDocument => _document != null;

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public bool HasChanges => _document != null && _document.HasChanges;

	/// <summary>Whether a background search is in progress.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public bool IsSearching => _searchCancellation != null;

	/// <summary>The most recent search, reused by Find Next and Find Previous.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public HexEditorSearch LastSearch => _lastSearch;

	/// <summary>The number of bytes per row (1 to 256). The toolbar offers 8, 16 and 32.</summary>
	[Category("Appearance"), DefaultValue(HexEditorViewport.DefaultBytesPerRow), Description("The number of bytes shown per row.")]
	public int BytesPerRow {
		get => _bytesPerRow;
		set {
			Guard.ArgumentInRange(value, HexEditorViewport.MinBytesPerRow, HexEditorViewport.MaxBytesPerRow, nameof(value));
			if (_bytesPerRow == value)
				return;
			_bytesPerRow = value;
			if (_viewport != null)
				_viewport.BytesPerRow = value;
			SyncBytesPerRowComboBox();
		}
	}

	/// <summary>The address displayed for the first byte, so a memory region or file slice shows its real virtual address or file offset. Retained across documents.</summary>
	[Category("Appearance"), DefaultValue(0L), Description("The address displayed for the first byte of the document.")]
	public long BaseAddress {
		get => _baseAddress;
		set {
			Guard.ArgumentNotNegative(value, nameof(value));
			_baseAddress = value;
			if (_viewport != null)
				_viewport.BaseAddress = value;
		}
	}

	/// <summary>Rejects edits regardless of the stream. A non-writable stream is read-only even when this is false.</summary>
	[Category("Behavior"), DefaultValue(false), Description("Whether editing is disabled even for writable streams.")]
	public bool ReadOnly {
		get => _readOnly;
		set {
			_readOnly = value;
			if (_document != null && _document.CanWrite)
				_document.IsReadOnly = value;
			UpdatePresentation();
		}
	}

	[Category("Appearance"), DefaultValue(true), Description("Whether the data inspector panel is shown.")]
	public bool ShowDataInspector {
		get => _showDataInspector;
		set {
			_showDataInspector = value;
			_inspectorPanel.Visible = value;
			_inspectorButton.Checked = value;
			UpdateInspector();
		}
	}

	[Category("Appearance"), DefaultValue(true), Description("Whether the command toolbar is shown.")]
	public bool ShowToolBar {
		get => _toolStrip.Visible;
		set => _toolStrip.Visible = value;
	}

	[Category("Appearance"), DefaultValue(true), Description("Whether the status bar is shown.")]
	public bool ShowStatusBar {
		get => _statusStrip.Visible;
		set => _statusStrip.Visible = value;
	}

	/// <summary>Whether the data inspector decodes multi-byte values most-significant byte first.</summary>
	[Category("Behavior"), DefaultValue(false), Description("Whether the data inspector decodes values as big endian.")]
	public bool BigEndian {
		get => _inspector.IsBigEndian;
		set {
			_inspector.IsBigEndian = value;
			_bigEndianCheckBox.Checked = value;
			UpdateInspector();
		}
	}

	/// <summary>Opens a file for in-place editing, or read-only, replacing any current document.</summary>
	public void OpenFile(string path, bool readOnly = false) {
		EnsureOwnerThread();
		SetDocument(HexEditorDocument.OpenFile(path, readOnly || _readOnly), true);
	}

	/// <summary>Opens a seekable stream, replacing any current document.</summary>
	public void OpenStream(Stream stream, bool ownsStream = true) {
		EnsureOwnerThread();
		SetDocument(new HexEditorDocument(stream, ownsStream), true);
	}

	/// <summary>Binds a document, disposing a previously owned one. Pass null to close.</summary>
	public void SetDocument(HexEditorDocument document, bool ownsDocument) {
		EnsureOwnerThread();
		if (ReferenceEquals(document, _document))
			return;
		CancelSearch();
		ReleaseDocument();
		_document = document;
		_ownsDocument = ownsDocument;
		if (_document != null) {
			if (_readOnly && _document.CanWrite)
				_document.IsReadOnly = true;
			_viewport = new HexEditorViewport(_document) { BytesPerRow = _bytesPerRow, BaseAddress = _baseAddress, InsertMode = _insertMode };
			_viewport.Changed += ViewportChanged;
			_document.Changed += DocumentContentChanged;
		}
		_view.Viewport = _viewport;
		_messageStatusLabel.Text = string.Empty;
		UpdatePresentation();
		DocumentChanged?.Invoke();
		NotifyStateChanged();
	}

	/// <summary>Closes the current document, disposing it when owned.</summary>
	public void CloseDocument() => SetDocument(null, false);

	/// <summary>Writes pending edits to the stream.</summary>
	public void Save() {
		EnsureDocument();
		_document.Save();
		_messageStatusLabel.Text = "Saved";
	}

	/// <summary>Discards pending edits.</summary>
	public void Revert() {
		EnsureDocument();
		_document.Revert();
	}

	/// <summary>Moves the caret to an offset, clamping to the document.</summary>
	public void GoTo(long offset) {
		EnsureDocument();
		_viewport.GoTo(offset);
		_view.Focus();
	}

	public void SelectAll() {
		EnsureDocument();
		_viewport.SelectAll();
	}

	/// <summary>Whether typing and pasting insert bytes at the caret instead of overwriting. Retained across documents.</summary>
	[Category("Behavior"), DefaultValue(false), Description("Whether typing and pasting insert new bytes instead of overwriting.")]
	public bool InsertMode {
		get => _insertMode;
		set {
			_insertMode = value;
			if (_viewport != null)
				_viewport.InsertMode = value;
			_insertModeButton.Checked = value;
			UpdatePresentation();
		}
	}

	/// <summary>Puts clipboard content at the caret as hexadecimal pairs when the hex pane is active, otherwise as UTF-8 text. Inserts or overwrites by <see cref="InsertMode"/>.</summary>
	public void Paste() {
		EnsureDocument();
		Guard.Ensure(!_document.IsReadOnly, "The document is read-only.");
		var text = Clipboard.ContainsText() ? Clipboard.GetText() : string.Empty;
		Guard.Ensure(!string.IsNullOrWhiteSpace(text), "The clipboard holds no text to paste.");
		var bytes = _viewport.ActivePane == HexEditorPane.Hex ? HexEditorSearch.ForHex(text).Pattern : Encoding.UTF8.GetBytes(text);
		_viewport.InsertBytes(bytes);
		_view.Focus();
	}

	/// <summary>Deletes the selection or the caret byte.</summary>
	public void Delete() {
		EnsureDocument();
		_viewport.DeleteForward();
	}

	/// <summary>Copies the selection to the clipboard as space-separated hexadecimal pairs.</summary>
	public void CopySelectionAsHex() {
		EnsureDocument();
		Clipboard.SetText(_viewport.GetSelectionHex());
	}

	/// <summary>Copies the selection to the clipboard as printable ASCII text.</summary>
	public void CopySelectionAsText() {
		EnsureDocument();
		Clipboard.SetText(_viewport.GetSelectionText());
	}

	/// <summary>Asks for a displayed address and jumps to it; addresses include <see cref="BaseAddress"/>.</summary>
	public async Task GoToAsync() {
		EnsureDocument();
		var baseAddress = _viewport.BaseAddress;
		using var dialog = new HexEditorGoToDialog { MaximumOffset = baseAddress + Math.Max(0, _document.Length - 1), Offset = baseAddress + _viewport.CaretOffset };
		if (await dialog.ShowDialogAsync(this) == DialogResult.OK && !IsDisposed && _document != null)
			GoTo(Math.Max(0, dialog.Offset - baseAddress));
	}

	/// <summary>Asks for a byte or text pattern and direction, then searches from the caret.</summary>
	public async Task FindAsync() {
		EnsureDocument();
		using var dialog = new HexEditorFindDialog();
		if (await dialog.ShowDialogAsync(this) != DialogResult.OK || IsDisposed || _document == null)
			return;
		_lastSearch = dialog.Search;
		UpdateCommandState();
		await FindAsync(_lastSearch, dialog.SearchBackward);
	}

	/// <summary>Repeats the last search forward from the current position.</summary>
	public Task FindNextAsync() {
		Guard.Ensure(_lastSearch != null, "There is no previous search to repeat.");
		return FindAsync(_lastSearch, false);
	}

	/// <summary>Repeats the last search backward from the current position.</summary>
	public Task FindPreviousAsync() {
		Guard.Ensure(_lastSearch != null, "There is no previous search to repeat.");
		return FindAsync(_lastSearch, true);
	}

	/// <summary>Searches on a worker thread from the caret or selection and selects the match. Returns its offset or -1.</summary>
	public async Task<long> FindAsync(HexEditorSearch search, bool backward) {
		EnsureOwnerThread();
		Guard.ArgumentNotNull(search, nameof(search));
		EnsureDocument();
		Guard.Ensure(_searchCancellation == null, "A search is already in progress.");
		var document = _document;
		var start = backward
			? (_viewport.HasSelection ? _viewport.SelectionStart - 1 : _viewport.CaretOffset - 1)
			: (_viewport.HasSelection ? _viewport.SelectionStart + 1 : _viewport.CaretOffset);
		using var cancellation = new CancellationTokenSource();
		_searchCancellation = cancellation;
		_messageStatusLabel.Text = "Searching...";
		UpdateCommandState();
		try {
			var result = await Task.Run(() => search.Find(document, start, !backward, cancellation.Token), cancellation.Token);
			if (IsDisposed || !ReferenceEquals(document, _document))
				return -1;
			if (result >= 0) {
				_viewport.Select(result, search.Pattern.Length);
				_messageStatusLabel.Text = $"Found at 0x{result:X}";
				_view.Focus();
			} else {
				_messageStatusLabel.Text = "Not found";
			}
			return result;
		} catch (OperationCanceledException) {
			return -1;
		} finally {
			if (ReferenceEquals(_searchCancellation, cancellation))
				_searchCancellation = null;
			if (!IsDisposed)
				UpdateCommandState();
		}
	}

	/// <summary>Keyboard shortcuts for the editor commands while focus is inside this control.</summary>
	protected override bool ProcessCmdKey(ref Message msg, Keys keyData) {
		if (_document != null) {
			switch (keyData) {
				case Keys.Control | Keys.G:
					RunCommand(GoToAsync);
					return true;
				case Keys.Control | Keys.F:
					RunCommand(FindAsync);
					return true;
				case Keys.F3 when _lastSearch != null:
					RunCommand(FindNextAsync);
					return true;
				case Keys.Shift | Keys.F3 when _lastSearch != null:
					RunCommand(FindPreviousAsync);
					return true;
				case Keys.Control | Keys.S when !_document.IsReadOnly:
					RunCommand(Save);
					return true;
				case Keys.Control | Keys.V when _view.Focused && !_document.IsReadOnly:
					RunCommand(Paste);
					return true;
				case Keys.Control | Keys.C when _view.Focused && _viewport.HasSelection:
					RunCommand(_viewport.ActivePane == HexEditorPane.Hex ? CopySelectionAsHex : CopySelectionAsText);
					return true;
				case Keys.Control | Keys.Shift | Keys.C when _view.Focused && _viewport.HasSelection:
					RunCommand(_viewport.ActivePane == HexEditorPane.Hex ? CopySelectionAsText : CopySelectionAsHex);
					return true;
			}
		}
		return base.ProcessCmdKey(ref msg, keyData);
	}

	/// <summary>Document content defines this control's state; child widgets do not publish edits.</summary>
	protected override bool ShouldObserveChildStateChanges(Control child) => false;

	protected override void CopyModelToUI() {
		base.CopyModelToUI();
		UpdatePresentation();
	}

	private void SaveButtonClicked(object sender, EventArgs e) => RunCommand(Save);

	private void RevertButtonClicked(object sender, EventArgs e) => RunCommand(Revert);

	private void GoToButtonClicked(object sender, EventArgs e) => RunCommand(GoToAsync);

	private void FindButtonClicked(object sender, EventArgs e) => RunCommand(FindAsync);

	private void FindNextButtonClicked(object sender, EventArgs e) => RunCommand(FindNextAsync);

	private void FindPreviousButtonClicked(object sender, EventArgs e) => RunCommand(FindPreviousAsync);

	private void CopyHexButtonClicked(object sender, EventArgs e) => RunCommand(CopySelectionAsHex);

	private void CopyTextButtonClicked(object sender, EventArgs e) => RunCommand(CopySelectionAsText);

	private void PasteButtonClicked(object sender, EventArgs e) => RunCommand(Paste);

	private void InsertModeButtonClicked(object sender, EventArgs e) => InsertMode = _insertModeButton.Checked;

	private void SelectAllButtonClicked(object sender, EventArgs e) => RunCommand(SelectAll);

	private void BytesPerRowComboBoxSelectionChanged(object sender, EventArgs e) {
		if (_syncingBytesPerRow || _bytesPerRowComboBox.SelectedIndex < 0)
			return;
		BytesPerRow = _bytesPerRowChoices[_bytesPerRowComboBox.SelectedIndex];
	}

	private void InspectorButtonClicked(object sender, EventArgs e) => ShowDataInspector = _inspectorButton.Checked;

	private void BigEndianCheckBoxChanged(object sender, EventArgs e) => BigEndian = _bigEndianCheckBox.Checked;

	private void ViewportChanged() {
		// The view toggles insert mode with the Insert key; keep the control's property and button in step.
		if (_viewport != null && _insertMode != _viewport.InsertMode) {
			_insertMode = _viewport.InsertMode;
			_insertModeButton.Checked = _insertMode;
		}
		if (IsDisposed || Disposing)
			return;
		UpdateStatus();
		UpdateInspector();
		UpdateCommandState();
		ViewChanged?.Invoke();
	}

	// Content edits are the semantic state this control publishes to the framework's state observers.
	private void DocumentContentChanged() {
		if (IsDisposed || Disposing)
			return;
		NotifyStateChanged();
	}

	private void RunCommand(Action action) {
		RunCommand(() => {
			action();
			return Task.CompletedTask;
		});
	}

	// Command failures are reported to the user instead of faulting the message loop.
	private async void RunCommand(Func<Task> action) {
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync<Exception>(action);
		if (error != null && !IsDisposed && !Disposing)
			await DialogEx.ShowAsync(this, SystemIconType.Error, "Hex Editor", error.Message, "OK");
	}

	private void UpdatePresentation() {
		UpdateStatus();
		UpdateInspector();
		UpdateCommandState();
	}

	private void UpdateStatus() {
		if (_viewport == null) {
			_offsetStatusLabel.Text = "No document";
			_selectionStatusLabel.Text = string.Empty;
			_lengthStatusLabel.Text = string.Empty;
			_changesStatusLabel.Text = string.Empty;
			_modeStatusLabel.Text = string.Empty;
			return;
		}
		_offsetStatusLabel.Text = $"Offset: {_viewport.FormatOffset(_viewport.CaretOffset)} ({_viewport.CaretOffset.ToString("N0", CultureInfo.CurrentCulture)})";
		_selectionStatusLabel.Text = _viewport.HasSelection
			? $"Selection: {_viewport.FormatOffset(_viewport.SelectionStart)} - {_viewport.FormatOffset(_viewport.SelectionEnd - 1)} ({_viewport.SelectionLength.ToString("N0", CultureInfo.CurrentCulture)} bytes)"
			: "No selection";
		_lengthStatusLabel.Text = $"Length: {_document.Length.ToString("N0", CultureInfo.CurrentCulture)} bytes";
		_changesStatusLabel.Text = _document.HasChanges ? $"Modified: {_document.ModifiedByteCount.ToString("N0", CultureInfo.CurrentCulture)} bytes" : string.Empty;
		_modeStatusLabel.Text = _document.IsReadOnly ? "Read-only" : _viewport.InsertMode ? "Insert" : "Overwrite";
	}

	// Inspector rows are reused once created; only changed values are written back to the list.
	private void UpdateInspector() {
		if (!_showDataInspector)
			return;
		var entries = _viewport != null ? _inspector.Inspect(_document, _viewport.CaretOffset).Entries : Array.Empty<HexEditorInspectionEntry>();
		_inspectorListView.BeginUpdate();
		try {
			if (_inspectorListView.Items.Count != entries.Length) {
				_inspectorListView.Items.Clear();
				foreach (var entry in entries)
					_inspectorListView.Items.Add(new ListViewItem(new[] { entry.Name, entry.Value }));
				return;
			}
			for (var index = 0; index < entries.Length; index++) {
				var valueItem = _inspectorListView.Items[index].SubItems[1];
				if (valueItem.Text != entries[index].Value)
					valueItem.Text = entries[index].Value;
			}
		} finally {
			_inspectorListView.EndUpdate();
		}
	}

	private void UpdateCommandState() {
		var open = _document != null;
		var editable = open && !_document.IsReadOnly;
		var searching = _searchCancellation != null;
		_saveButton.Enabled = editable && _document.HasChanges;
		_revertButton.Enabled = editable && _document.HasChanges;
		_goToButton.Enabled = open;
		_findButton.Enabled = open && !searching;
		_findNextButton.Enabled = open && !searching && _lastSearch != null;
		_findPreviousButton.Enabled = open && !searching && _lastSearch != null;
		_copyHexButton.Enabled = open && _viewport.HasSelection;
		_copyTextButton.Enabled = open && _viewport.HasSelection;
		_selectAllButton.Enabled = open && _document.Length > 0;
		_pasteButton.Enabled = editable;
		_insertModeButton.Enabled = editable;
		_bytesPerRowComboBox.Enabled = true;
	}

	private void SyncBytesPerRowComboBox() {
		_syncingBytesPerRow = true;
		try {
			_bytesPerRowComboBox.SelectedIndex = Array.IndexOf(_bytesPerRowChoices, _bytesPerRow);
		} finally {
			_syncingBytesPerRow = false;
		}
	}

	private void CancelSearch() => _searchCancellation?.Cancel();

	// Called when replacing the document and from disposal; owned documents are disposed here.
	private void ReleaseDocument() {
		if (_viewport != null) {
			_viewport.Changed -= ViewportChanged;
			_viewport = null;
		}
		if (_document != null) {
			_document.Changed -= DocumentContentChanged;
			if (_ownsDocument)
				_document.Dispose();
			_document = null;
		}
	}

	private void EnsureDocument() {
		EnsureOwnerThread();
		Guard.Ensure(_document != null, "No document is open.");
	}

	private void EnsureOwnerThread() {
		Guard.Ensure(Environment.CurrentManagedThreadId == _ownerThreadId, "Use the hex editor on its creating UI thread.");
		ObjectDisposedException.ThrowIf(IsDisposed || Disposing, this);
	}
}
