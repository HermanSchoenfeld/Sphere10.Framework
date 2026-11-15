// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Edits one provisional attachment. Closing or cancelling discards all changes.</summary>
internal sealed class AttachmentPickerDialog : FormEx {
	private readonly Label _fileName;
	private readonly Label _status;
	private readonly PictureBox _preview;
	private readonly Button _remove;

	public AttachmentPickerDialog(AttachmentSelection selection) {
		Selection = selection;
		Text = "Attachment";
		Name = "AttachmentPickerDialog";
		AutoScaleMode = AutoScaleMode.Dpi;
		AutoScaleDimensions = new SizeF(96, 96);
		ClientSize = new Size(470, 300);
		StartPosition = FormStartPosition.CenterParent;
		FormBorderStyle = FormBorderStyle.FixedDialog;
		MaximizeBox = false;
		MinimizeBox = false;
		ShowInTaskbar = false;
		KeyPreview = true;
		AllowDrop = true;
		var layout = new TableLayoutPanel { Dock = DockStyle.Fill, Padding = new Padding(16), ColumnCount = 1, RowCount = 4 };
		layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
		layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
		layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_fileName = new Label { AutoSize = true, Dock = DockStyle.Fill, TextAlign = ContentAlignment.MiddleLeft, Padding = new Padding(0, 0, 0, 8) };
		_preview = new PictureBox { Dock = DockStyle.Fill, SizeMode = PictureBoxSizeMode.Zoom, BackColor = SystemColors.Window, BorderStyle = BorderStyle.FixedSingle, TabStop = false };
		_status = new Label { AutoSize = true, Dock = DockStyle.Fill, Padding = new Padding(0, 8, 0, 8) };
		var actions = new FlowLayoutPanel { AutoSize = true, Dock = DockStyle.Fill, WrapContents = false, Margin = Padding.Empty };
		var select = new Button { Name = "SelectAttachment", Text = "Select File", AutoSize = true };
		var paste = new Button { Name = "PasteAttachment", Text = "Paste", AutoSize = true };
		_remove = new Button { Name = "RemoveAttachment", Text = "Remove", AutoSize = true };
		var ok = new Button { Name = "AcceptAttachment", Text = "OK", DialogResult = DialogResult.OK, AutoSize = true };
		var cancel = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel, AutoSize = true };
		actions.Controls.AddRange([select, paste, _remove, ok, cancel]);
		layout.Controls.Add(_fileName, 0, 0);
		layout.Controls.Add(_preview, 0, 1);
		layout.Controls.Add(_status, 0, 2);
		layout.Controls.Add(actions, 0, 3);
		Controls.Add(layout);
		AcceptButton = ok;
		CancelButton = cancel;
		select.Click += (_, _) => SelectFile();
		paste.Click += (_, _) => ReadSelection(AttachmentSelection.ReadClipboard);
		_remove.Click += (_, _) => { Selection = null; ShowSelection(); };
		DragEnter += (_, e) => e.Effect = e.Data?.GetData(DataFormats.FileDrop) is string[] { Length: 1 } ? DragDropEffects.Copy : DragDropEffects.None;
		DragDrop += (_, e) => {
			if (e.Data?.GetData(DataFormats.FileDrop) is string[] { Length: 1 } files)
				ReadSelection(() => AttachmentSelection.ReadFile(files[0]));
		};
		ShowSelection();
	}

	public AttachmentSelection Selection { get; private set; }

	protected override void OnLoad(EventArgs e) {
		ClientSize = new Size(LogicalToDeviceUnits(520), LogicalToDeviceUnits(320));
		base.OnLoad(e);
	}

	protected override void OnDpiChanged(DpiChangedEventArgs e) {
		base.OnDpiChanged(e);
		ClientSize = new Size(LogicalToDeviceUnits(520), LogicalToDeviceUnits(320));
	}

	protected override bool ProcessCmdKey(ref Message msg, Keys keyData) {
		if (keyData != (Keys.Control | Keys.V))
			return base.ProcessCmdKey(ref msg, keyData);
		ReadSelection(AttachmentSelection.ReadClipboard);
		return true;
	}

	protected override void Dispose(bool disposing) {
		if (disposing) {
			var image = _preview.Image;
			_preview.Image = null;
			image?.Dispose();
		}
		base.Dispose(disposing);
	}

	private void SelectFile() {
		using var picker = new OpenFileDialog { Title = "Select Attachment", Filter = "All files (*.*)|*.*", CheckFileExists = true, Multiselect = false };
		if (picker.ShowDialog(this) == DialogResult.OK)
			ReadSelection(() => AttachmentSelection.ReadFile(picker.FileName));
	}

	private void ReadSelection(Func<AttachmentSelection> read) {
		try {
			Selection = read();
			ShowSelection();
		} catch (Exception error) when (error is IOException or UnauthorizedAccessException or ExternalException or ArgumentException or InvalidOperationException) {
			_status.ForeColor = Color.Firebrick;
			_status.Text = error.Message;
		}
	}

	private void ShowSelection() {
		_fileName.Text = Selection?.FileName ?? "No Attachment";
		_status.ForeColor = SystemColors.ControlText;
		_status.Text = Selection == null ? "Select a file, paste with Ctrl+V, or drop a file here." : Tools.Memory.GetBytesReadable(Selection.Length);
		_remove.Enabled = Selection != null;
		var previous = _preview.Image;
		_preview.Image = null;
		previous?.Dispose();
		if (Selection == null)
			return;
		try {
			using var stream = new MemoryStream(Selection.CopyBytes());
			using var image = Image.FromStream(stream);
			_preview.Image = new Bitmap(image);
		} catch (ArgumentException) {
			// Non-image files are valid attachments and retain their name and size.
		}
	}
}
