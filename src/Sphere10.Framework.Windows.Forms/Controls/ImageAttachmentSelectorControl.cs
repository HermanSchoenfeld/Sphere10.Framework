// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>
/// Lets the user attach one file: any file chosen or dropped on the control (images are previewed), or the image on the clipboard.
/// <see cref="TryGetAttachment"/> returns the attachment's name and bytes; <see cref="TryGetSelectedImage"/> returns it as an image.
/// </summary>
public partial class ImageAttachmentSelectorControl : UserControlEx {
	public const string ClipboardFileName = "Clipboard.png";

	public ImageAttachmentSelectorControl() {
		InitializeComponent();
	}

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public bool HasSelectedImage => !_noneRadioButton.Checked;

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public string Filename {
		get => _fileSelectorControl.Path;
		set => _fileSelectorControl.Path = value;
	}

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public ImageAttachmentSource AttachmentSource {
		get {
			var retval = ImageAttachmentSource.None;
			if (_filenameRadioButton.Checked)
				retval = ImageAttachmentSource.File;
			else if (_clipboardRadioButton.Checked)
				retval = ImageAttachmentSource.Clipboard;
			else if (_noneRadioButton.Checked)
				retval = ImageAttachmentSource.None;

			return retval;
		}
		set {
			switch (value) {
				case ImageAttachmentSource.Clipboard:
					_clipboardRadioButton.Checked = true;
					break;
				case ImageAttachmentSource.File:
					_filenameRadioButton.Checked = true;
					break;
				case ImageAttachmentSource.None:
					_noneRadioButton.Checked = true;
					break;
			}
		}
	}

	/// <summary>
	/// The selected attachment: the chosen file's name and bytes, or the clipboard image as <see cref="ClipboardFileName"/> in PNG. False
	/// for None, a missing or unreadable file and an empty clipboard.
	/// </summary>
	public bool TryGetAttachment(out string fileName, out byte[] data) {
		fileName = null;
		data = null;
		switch (AttachmentSource) {
			case ImageAttachmentSource.Clipboard:
				if (!Clipboard.ContainsImage())
					return false;
				using (var image = Clipboard.GetImage()) {
					if (image == null)
						return false;
					using var stream = new MemoryStream();
					image.Save(stream, ImageFormat.Png);
					data = stream.ToArray();
				}
				fileName = ClipboardFileName;
				return true;
			case ImageAttachmentSource.File:
				if (string.IsNullOrWhiteSpace(Filename) || !File.Exists(Filename))
					return false;
				try {
					data = File.ReadAllBytes(Filename);
				} catch (Exception error) when (error is IOException or UnauthorizedAccessException) {
					return false;
				}
				fileName = Path.GetFileName(Filename);
				return true;
			default:
				return false;
		}
	}

	/// <summary>The selected attachment as an image, owned by the caller; false when nothing is selected or it is not an image.</summary>
	public bool TryGetSelectedImage(out Image selectedImage) {
		selectedImage = null;
		if (!HasSelectedImage || !TryGetAttachment(out _, out var data))
			return false;
		selectedImage = TryReadImage(data);
		return selectedImage != null;
	}

	// The path box spans from the File option to the preview at the right; a designer anchor would not survive a DPI change. On a
	// design surface the designer's own geometry stays as edited.
	protected override void OnLayout(LayoutEventArgs e) {
		base.OnLayout(e);
		if (DesignMode || Tools.Runtime.IsDesignMode)
			return;
		var gap = LogicalToDeviceUnits(6);
		_pictureBox.Bounds = new Rectangle(ClientSize.Width - _pictureBox.Width, 0, _pictureBox.Width, ClientSize.Height);
		var left = _filenameRadioButton.Right + gap;
		_fileSelectorControl.Bounds = new Rectangle(left, _fileSelectorControl.Top, Math.Max(0, _pictureBox.Left - gap - left), _fileSelectorControl.Height);
	}

	// The preview is read from the bytes, so the file is never left locked.
	private static Image TryReadImage(byte[] data) {
		try {
			using var stream = new MemoryStream(data);
			using var image = Image.FromStream(stream);
			return new Bitmap(image);
		} catch (ArgumentException) {
			return null;
		}
	}

	private void UpdateImageBox() {
		var previous = _pictureBox.Image;
		_pictureBox.Image = TryGetSelectedImage(out var image) ? image : null;
		previous?.Dispose();
	}

	// Only a single dropped file is an attachment; text and several files are refused.
	private static bool TryGetDroppedFile(DragEventArgs e, out string path) {
		path = null;
		if (e.Data?.GetData(DataFormats.FileDrop) is not string[] { Length: 1 } files || !File.Exists(files[0]))
			return false;
		path = files[0];
		return true;
	}

	private void FilenameRadioButton_CheckedChanged(object sender, EventArgs args) {
		_fileSelectorControl.Enabled = _filenameRadioButton.Checked;
		if (_filenameRadioButton.Checked)
			UpdateImageBox();
	}

	private void ClipboardRadioButton_CheckedChanged(object sender, EventArgs args) {
		if (_clipboardRadioButton.Checked)
			UpdateImageBox();
	}

	private void NoneRadioButton_CheckedChanged(object sender, EventArgs args) {
		if (_noneRadioButton.Checked)
			UpdateImageBox();
	}

	private void FileSelectorControl_PathChanged() {
		if (_filenameRadioButton.Checked)
			UpdateImageBox();
	}

	private void ImageAttachmentSelectorControl_DragEnter(object sender, DragEventArgs e) => e.Effect = TryGetDroppedFile(e, out _) ? DragDropEffects.Copy : DragDropEffects.None;

	private void ImageAttachmentSelectorControl_DragDrop(object sender, DragEventArgs e) {
		if (!TryGetDroppedFile(e, out var path))
			return;
		_filenameRadioButton.Checked = true;
		Filename = path;
	}

	#region Inner classes/enums

	public enum ImageAttachmentSource {
		File,
		Clipboard,
		None
	}

	#endregion
}
