// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>
/// A standalone hex editor window hosting <see cref="HexEditorControl"/> with File, Edit and View menus. Open a
/// file or any seekable stream; edits are written in place on Save and the window asks before discarding them.
/// </summary>
public partial class HexEditorForm : Form {
	public const string DefaultTitle = "Hex Editor";
	private string _documentTitle;
	private bool _closeApproved;

	public HexEditorForm() {
		InitializeComponent();
		_editor.DocumentChanged += EditorDocumentChanged;
		_editor.ViewChanged += UpdateTitle;
		UpdateTitle();
		UpdateMenuState();
	}

	/// <summary>The hosted editor.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public HexEditorControl Editor => _editor;

	/// <summary>The caption describing the open document, shown in the window title.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public string DocumentTitle => _documentTitle;

	/// <summary>Shows a modal editor over a stream and returns when the window closes. Token cancellation closes it and throws.</summary>
	public static async Task ShowAsync(IWin32Window owner, Stream stream, string title, bool ownsStream = true, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(stream, nameof(stream));
		cancellationToken.ThrowIfCancellationRequested();
		using var form = new HexEditorForm();
		form.OpenStream(stream, title, ownsStream);
		await ShowModalAsync(owner, form, cancellationToken);
	}

	/// <summary>Shows a modal editor over a copy of the bytes. Returns the saved copy, or null when the window closes without saving; the input is never changed. Token cancellation closes it and throws.</summary>
	public static async Task<byte[]> ShowBytesAsync(IWin32Window owner, string title, byte[] bytes, bool readOnly = false, CancellationToken cancellationToken = default) {
		Guard.ArgumentNotNull(bytes, nameof(bytes));
		cancellationToken.ThrowIfCancellationRequested();
		using var copy = new MemoryStream((byte[])bytes.Clone(), 0, bytes.Length, !readOnly, true);
		using var form = new HexEditorForm();
		form.Editor.ReadOnly = readOnly;
		form.OpenStream(copy, title, false);
		var saved = false;
		form.Editor.Document.Saved += () => saved = true;
		await ShowModalAsync(owner, form, cancellationToken);
		return saved ? copy.ToArray() : null;
	}

	/// <summary>Shows a modal editor over a file and returns when the window closes. Token cancellation closes it and throws.</summary>
	public static async Task ShowFileAsync(IWin32Window owner, string path, bool readOnly = false, CancellationToken cancellationToken = default) {
		cancellationToken.ThrowIfCancellationRequested();
		using var form = new HexEditorForm();
		form.OpenFile(path, readOnly);
		await ShowModalAsync(owner, form, cancellationToken);
	}

	/// <summary>Opens a file, replacing the current document without prompting.</summary>
	public void OpenFile(string path, bool readOnly = false) {
		_editor.OpenFile(path, readOnly);
		_documentTitle = Path.GetFileName(path);
		UpdateTitle();
	}

	/// <summary>Opens a seekable stream under a caption, replacing the current document without prompting.</summary>
	public void OpenStream(Stream stream, string title, bool ownsStream = true) {
		_editor.OpenStream(stream, ownsStream);
		_documentTitle = string.IsNullOrWhiteSpace(title) ? "Stream" : title;
		UpdateTitle();
	}

	/// <summary>Closes the document without prompting.</summary>
	public void CloseDocument() {
		_editor.CloseDocument();
		_documentTitle = null;
		UpdateTitle();
	}

	/// <summary>Offers to save pending edits; returns false when the user cancels.</summary>
	public async Task<bool> ConfirmDiscardChangesAsync() {
		if (!_editor.HasChanges)
			return true;
		var answer = await DialogEx.ShowAsync(this, SystemIconType.Question, DefaultTitle, $"Save changes to '{_documentTitle}'?", "&Save", "&Discard", "Cancel");
		if (IsDisposed)
			return false;
		switch (answer) {
			case DialogExResult.Button1:
				_editor.Save();
				return true;
			case DialogExResult.Button2:
				return true;
			default:
				return false;
		}
	}

	// Closing with pending edits asks first; the native event cannot await, so the decision closes the form again.
	protected override void OnFormClosing(FormClosingEventArgs e) {
		base.OnFormClosing(e);
		if (e.Cancel || _closeApproved || !_editor.HasChanges)
			return;
		e.Cancel = true;
		RequestClose();
	}

	private async void RequestClose() {
		var approved = await Tools.Exceptions.ExecuteCapturingExceptionAsync<Exception>(async () => {
			if (await ConfirmDiscardChangesAsync() && !IsDisposed) {
				_closeApproved = true;
				Close();
			}
		});
		if (approved != null && !IsDisposed)
			await DialogEx.ShowAsync(this, SystemIconType.Error, DefaultTitle, approved.Message, "OK");
	}

	private static async Task ShowModalAsync(IWin32Window owner, HexEditorForm form, CancellationToken cancellationToken) {
		var showing = form.ShowDialogAsync(owner);
		try {
			await showing.WaitAsync(cancellationToken);
		} catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
			form._closeApproved = true;
			form.Dispose();
			// Cancellation may precede the posted native show.
			_ = await Tools.Exceptions.ExecuteCapturingExceptionAsync<ObjectDisposedException>(() => showing);
			throw;
		}
	}

	private void OpenMenuItemClicked(object sender, EventArgs e) => RunCommand(() => OpenWithDialogAsync(false));

	private void OpenReadOnlyMenuItemClicked(object sender, EventArgs e) => RunCommand(() => OpenWithDialogAsync(true));

	private void SaveMenuItemClicked(object sender, EventArgs e) => RunCommand(_editor.Save);

	private void CloseDocumentMenuItemClicked(object sender, EventArgs e) {
		RunCommand(async () => {
			if (await ConfirmDiscardChangesAsync())
				CloseDocument();
		});
	}

	private void ExitMenuItemClicked(object sender, EventArgs e) => Close();

	private void GoToMenuItemClicked(object sender, EventArgs e) => RunCommand(_editor.GoToAsync);

	private void FindMenuItemClicked(object sender, EventArgs e) => RunCommand(_editor.FindAsync);

	private void FindNextMenuItemClicked(object sender, EventArgs e) => RunCommand(_editor.FindNextAsync);

	private void FindPreviousMenuItemClicked(object sender, EventArgs e) => RunCommand(_editor.FindPreviousAsync);

	private void CopyHexMenuItemClicked(object sender, EventArgs e) => RunCommand(_editor.CopySelectionAsHex);

	private void CopyTextMenuItemClicked(object sender, EventArgs e) => RunCommand(_editor.CopySelectionAsText);

	private void SelectAllMenuItemClicked(object sender, EventArgs e) => RunCommand(_editor.SelectAll);

	private void PasteMenuItemClicked(object sender, EventArgs e) => RunCommand(_editor.Paste);

	private void DeleteMenuItemClicked(object sender, EventArgs e) => RunCommand(_editor.Delete);

	private void InsertModeMenuItemClicked(object sender, EventArgs e) => _editor.InsertMode = _insertModeMenuItem.Checked;

	private void BytesPerRowMenuItemClicked(object sender, EventArgs e) {
		if (ReferenceEquals(sender, _bytesPerRow8MenuItem))
			_editor.BytesPerRow = 8;
		else if (ReferenceEquals(sender, _bytesPerRow16MenuItem))
			_editor.BytesPerRow = 16;
		else
			_editor.BytesPerRow = 32;
		UpdateMenuState();
	}

	private void DataInspectorMenuItemClicked(object sender, EventArgs e) => _editor.ShowDataInspector = _dataInspectorMenuItem.Checked;

	private void ToolBarMenuItemClicked(object sender, EventArgs e) => _editor.ShowToolBar = _toolBarMenuItem.Checked;

	private void StatusBarMenuItemClicked(object sender, EventArgs e) => _editor.ShowStatusBar = _statusBarMenuItem.Checked;

	private void EditMenuOpening(object sender, EventArgs e) => UpdateMenuState();

	// The file picker is a synchronous common dialog; the save prompt before it is asynchronous.
	private async Task OpenWithDialogAsync(bool readOnly) {
		if (!await ConfirmDiscardChangesAsync() || IsDisposed)
			return;
		using var dialog = new OpenFileDialog { Title = readOnly ? "Open File Read-Only" : "Open File", Filter = "All files (*.*)|*.*", CheckFileExists = true };
		if (dialog.ShowDialog(this) != DialogResult.OK)
			return;
		OpenFile(dialog.FileName, readOnly);
	}

	private void RunCommand(Action action) {
		RunCommand(() => {
			action();
			return Task.CompletedTask;
		});
	}

	private async void RunCommand(Func<Task> action) {
		var error = await Tools.Exceptions.ExecuteCapturingExceptionAsync<Exception>(action);
		if (error != null && !IsDisposed && !Disposing)
			await DialogEx.ShowAsync(this, SystemIconType.Error, DefaultTitle, error.Message, "OK");
	}

	private void EditorDocumentChanged() {
		UpdateTitle();
		UpdateMenuState();
	}

	private void UpdateTitle() {
		if (IsDisposed || Disposing)
			return;
		var modified = _editor.HasChanges ? "*" : string.Empty;
		Text = _documentTitle == null ? DefaultTitle : $"{_documentTitle}{modified} - {DefaultTitle}";
		UpdateMenuState();
	}

	private void UpdateMenuState() {
		var open = _editor.HasDocument;
		var editable = open && !_editor.Document.IsReadOnly;
		_saveMenuItem.Enabled = editable && _editor.HasChanges;
		_closeDocumentMenuItem.Enabled = open;
		_goToMenuItem.Enabled = open;
		_findMenuItem.Enabled = open && !_editor.IsSearching;
		_findNextMenuItem.Enabled = open && !_editor.IsSearching && _editor.LastSearch != null;
		_findPreviousMenuItem.Enabled = _findNextMenuItem.Enabled;
		_copyHexMenuItem.Enabled = open && _editor.Viewport.HasSelection;
		_copyTextMenuItem.Enabled = _copyHexMenuItem.Enabled;
		_selectAllMenuItem.Enabled = open && _editor.Document.Length > 0;
		_pasteMenuItem.Enabled = editable;
		_deleteMenuItem.Enabled = editable && (_editor.Viewport.HasSelection || _editor.Viewport.CaretOffset < _editor.Document.Length);
		_insertModeMenuItem.Enabled = editable;
		_insertModeMenuItem.Checked = _editor.InsertMode;
		_bytesPerRow8MenuItem.Checked = _editor.BytesPerRow == 8;
		_bytesPerRow16MenuItem.Checked = _editor.BytesPerRow == 16;
		_bytesPerRow32MenuItem.Checked = _editor.BytesPerRow == 32;
		_dataInspectorMenuItem.Checked = _editor.ShowDataInspector;
		_toolBarMenuItem.Checked = _editor.ShowToolBar;
		_statusBarMenuItem.Checked = _editor.ShowStatusBar;
	}
}
