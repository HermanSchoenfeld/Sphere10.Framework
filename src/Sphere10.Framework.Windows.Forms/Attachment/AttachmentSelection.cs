// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>An owned snapshot shared by attachment editors; it never retains a file handle or clipboard reference.</summary>
internal sealed class AttachmentSelection {
	private readonly byte[] _data;

	public AttachmentSelection(string fileName, byte[] data) {
		Guard.ArgumentNotNull(data, nameof(data));
		Guard.Argument(!string.IsNullOrWhiteSpace(fileName), nameof(fileName), "A file name is required.");
		FileName = Path.GetFileName(fileName);
		_data = (byte[])data.Clone();
	}

	public string FileName { get; }

	public long Length => _data.LongLength;

	public byte[] CopyBytes() => (byte[])_data.Clone();

	public static AttachmentSelection ReadFile(string path) => new(Path.GetFileName(path), File.ReadAllBytes(path));

	public static AttachmentSelection ReadClipboard() {
		if (Clipboard.ContainsFileDropList()) {
			var files = Clipboard.GetFileDropList();
			Guard.Ensure(files.Count == 1, "Copy one file at a time.");
			return ReadFile(files[0]);
		}
		using var image = Clipboard.GetImage();
		Guard.Ensure(image != null, "Copy a file or image, then choose Paste.");
		using var stream = new MemoryStream();
		image.Save(stream, ImageFormat.Png);
		return new AttachmentSelection(ImageAttachmentSelectorControl.ClipboardFileName, stream.ToArray());
	}
}
