// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Globalization;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

/// <summary>Showcases the hex editor over a file, a small in-memory sample and a 512 MiB synthetic stream that is never loaded.</summary>
public partial class HexEditorTestScreen : ApplicationScreen {
	public const long SyntheticLength = 512L * 1024 * 1024;
	private SyntheticByteStream _syntheticStream;
	private string _documentDescription;

	public HexEditorTestScreen() {
		InitializeComponent();
		Disposables.Add(_heading.Font);
		_editor.DocumentChanged += UpdateStatistics;
		_editor.ViewChanged += UpdateStatistics;
		UpdateStatistics();
	}

	/// <summary>The hosted editor, for tests that drive the real screen.</summary>
	public HexEditorControl Editor => _editor;

	/// <summary>Opens a writable in-memory sample of a few hundred bytes.</summary>
	public void OpenSample() {
		_syntheticStream = null;
		_editor.OpenStream(new MemoryStream(CreateSampleBytes()));
		_documentDescription = "In-memory sample (MemoryStream)";
		UpdateStatistics();
	}

	/// <summary>Opens the 512 MiB synthetic stream; its bytes are computed on demand so no buffer of that size exists.</summary>
	public void OpenSynthetic() {
		_syntheticStream = new SyntheticByteStream(SyntheticLength);
		_editor.OpenStream(_syntheticStream);
		_documentDescription = $"{SyntheticLength / (1024 * 1024)} MiB synthetic stream (SyntheticByteStream, computed on demand)";
		UpdateStatistics();
	}

	/// <summary>Opens a file in place; edits are written back on Save.</summary>
	public void OpenFile(string path, bool readOnly) {
		_syntheticStream = null;
		_editor.OpenFile(path, readOnly);
		_documentDescription = $"File {Path.GetFileName(path)} ({(readOnly ? "read-only" : "writable")})";
		UpdateStatistics();
	}

	/// <summary>A few hundred bytes mixing readable text, a little-endian record and binary filler.</summary>
	public static byte[] CreateSampleBytes() {
		using var stream = new MemoryStream();
		using (var writer = new BinaryWriter(stream, Encoding.ASCII, true)) {
			writer.Write(Encoding.ASCII.GetBytes("Sphere10 Framework hex editor sample.\r\n"));
			writer.Write(Encoding.ASCII.GetBytes("Inspect the record below in the data inspector: "));
			writer.Write((short)-2);
			writer.Write((ushort)0xBEEF);
			writer.Write(0x12345678);
			writer.Write(3.5f);
			writer.Write(1.0 / 3.0);
			writer.Write(long.MaxValue);
			writer.Write(Encoding.UTF8.GetBytes("\r\nUTF-8: café ✓ π\r\n"));
			for (var index = 0; index < 256; index++)
				writer.Write((byte)index);
			writer.Write(Encoding.ASCII.GetBytes("\r\nEnd of sample."));
		}
		return stream.ToArray();
	}

	/// <summary>Opens the sample bytes the first time the screen is shown; later refreshes keep the current document.</summary>
	protected override void CopyModelToUI() {
		base.CopyModelToUI();
		if (!_editor.HasDocument)
			OpenSample();
	}

	// Each command replaces the document; the editor disposes the previous owned document.
	private void OpenFileButtonClicked(object sender, EventArgs e) {
		using var dialog = new OpenFileDialog { Title = "Open file in hex editor", Filter = "All files (*.*)|*.*", CheckFileExists = true };
		if (dialog.ShowDialog(this) == DialogResult.OK)
			OpenFile(dialog.FileName, _readOnlyCheckBox.Checked);
	}

	private void OpenSyntheticButtonClicked(object sender, EventArgs e) => OpenSynthetic();

	private void OpenSampleButtonClicked(object sender, EventArgs e) => OpenSample();

	private void ReadOnlyCheckBoxChanged(object sender, EventArgs e) => _editor.ReadOnly = _readOnlyCheckBox.Checked;

	// Live evidence that browsing reads only the visible window: stream and cache counters beside the managed heap.
	private void UpdateStatistics() {
		if (IsDisposed || Disposing)
			return;
		var document = _editor.Document;
		if (document == null) {
			_statisticsLabel.Text = "No document open.";
			return;
		}
		var cache = document.Cache;
		var culture = CultureInfo.CurrentCulture;
		var text = new StringBuilder();
		text.Append("Document: ").Append(_documentDescription).Append("  |  Length: ").Append(document.Length.ToString("N0", culture)).Append(" bytes");
		text.Append("  |  Cache: ").Append(cache.CachedBlockCount.ToString("N0", culture)).Append(" blocks, ").Append((cache.CachedBytes / 1024).ToString("N0", culture)).Append(" KiB of ").Append((cache.MaxCachedBytes / 1024).ToString("N0", culture)).Append(" KiB budget");
		text.Append("  |  Fetched: ").Append(cache.BlockFetchCount.ToString("N0", culture)).Append(" blocks, ").Append((cache.BytesFetched / 1024).ToString("N0", culture)).Append(" KiB");
		if (_syntheticStream != null)
			text.Append("  |  Stream reads: ").Append(_syntheticStream.ReadCallCount.ToString("N0", culture)).Append(" calls, ").Append((_syntheticStream.BytesRead / 1024).ToString("N0", culture)).Append(" KiB");
		text.Append("  |  Managed heap: ").Append((GC.GetTotalMemory(false) / (1024 * 1024)).ToString("N0", culture)).Append(" MiB");
		_statisticsLabel.Text = text.ToString();
	}
}
