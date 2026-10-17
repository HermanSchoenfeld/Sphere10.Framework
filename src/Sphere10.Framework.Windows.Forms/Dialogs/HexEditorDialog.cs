// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Hosts a byte editor over its own copy; only accepted bytes are returned to the caller.</summary>
public partial class HexEditorDialog : Form {
	public HexEditorDialog() => InitializeComponent();

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public byte[] Bytes { get => _editor.Bytes; set => _editor.Bytes = value; }

	[Category("Behavior"), DefaultValue(false)]
	public bool ReadOnly {
		get => _editor.ReadOnly;
		set { _editor.ReadOnly = value; _accept.Text = value ? "&Close" : "&OK"; }
	}

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public bool HasChanges => _editor.HasChanges;

	/// <summary>Returns a fresh byte array on OK and null on user cancellation. Token cancellation closes the owned modal dialog and throws.</summary>
	public static async Task<byte[]> ShowAsync(IWin32Window owner, string title, byte[] bytes, bool readOnly = false, CancellationToken cancellationToken = default) {
		cancellationToken.ThrowIfCancellationRequested();
		using var dialog = new HexEditorDialog { Text = title ?? "Hex Editor", Bytes = bytes, ReadOnly = readOnly };
		var showing = dialog.ShowDialogAsync(owner);
		try {
			var result = await showing.WaitAsync(cancellationToken);
			return result == DialogResult.OK ? dialog.Bytes : null;
		} catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) {
			dialog.Dispose();
			try { await showing; } catch (ObjectDisposedException) { /* Cancellation may precede the posted native show. */ }
			throw;
		}
	}

	private void AcceptClicked(object sender, EventArgs e) {
		if (!_editor.CommitEdit())
			return;
		DialogResult = DialogResult.OK;
		Close();
	}
}
