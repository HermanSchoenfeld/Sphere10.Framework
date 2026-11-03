// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;


/// <summary>Verifies owned temporary compression round trips without blocking the native dispatcher.</summary>
public partial class EncryptedCompressionTestScreen : ApplicationScreen {
	private CancellationTokenSource _operationCancellation;

	/// <summary>Creates file choices without opening files or starting compression.</summary>
	public EncryptedCompressionTestScreen() {
		InitializeComponent();
		Disposables.Add(_heading.Font);
	}

	protected override void OnDestroyScreen() {
		_operationCancellation?.Cancel();
		base.OnDestroyScreen();
	}

	// Capture native drafts before starting the bounded worker operation.
	private async void _testButton_Click(object sender, EventArgs e) {
		await RunRoundTripsAsync();
	}

	// Capture the path and session-only password before entering worker code.
	private async Task RunRoundTripsAsync() {
		if (_operationCancellation != null)
			return;
		var path = fileSelectorControl1.Path;
		var password = _passwordTextBox.Text;
		if (!File.Exists(path) || string.IsNullOrEmpty(password)) {
			_status.Text = "Choose an existing input file, and enter a password.";
			return;
		}
		using var cancellation = new CancellationTokenSource();
		_operationCancellation = cancellation;
		_testButton.Enabled = false;
		_cancelButton.Enabled = true;
		try {
			_status.Text = "Running compression and content verification…";
			var output = await Task.Run(() => RunRoundTrips(path, password, cancellation.Token));
			if (!IsDisposed && !Disposing) {
				textBox1.Text = output;
				_status.Text = "Completed. Temporary round-trip files have been removed.";
			}
		} catch (OperationCanceledException) when (cancellation.IsCancellationRequested) {
			if (!IsDisposed && !Disposing)
				_status.Text = "Cancelled. Temporary files have been removed.";
		} catch (Exception error) {
			if (!IsDisposed && !Disposing)
				_status.Text = "Round trip failed: " + error.Message;
		} finally {
			_operationCancellation = null;
			if (!IsDisposed && !Disposing) {
				_testButton.Enabled = true;
				_cancelButton.Enabled = false;
			}
		}
	}

	private void CancelClicked(object sender, EventArgs e) {
		_operationCancellation?.Cancel();
		_status.Text = "Cancelling after the current file operation…";
	}

	// Temporary files remain owned by this worker even if compression, validation or cancellation fails.
	private static string RunRoundTrips(string path, string password, CancellationToken cancellationToken) {
		var compressed = Path.GetTempFileName();
		string restored = null;
		try {
			restored = Path.GetTempFileName();
			var originalLength = new FileInfo(path).Length;
			var output = new StringBuilder();
			cancellationToken.ThrowIfCancellationRequested();
			Tools.FileSystem.CompressFile(path, compressed, password);
			cancellationToken.ThrowIfCancellationRequested();
			Tools.FileSystem.DecompressFile(compressed, restored, password);
			cancellationToken.ThrowIfCancellationRequested();
			var same = ComparerFactory.Default.GetEqualityComparer<byte[]>().Equals(
				Tools.FileSystem.CalculateContentHash(path, CHF.SHA2_256), Tools.FileSystem.CalculateContentHash(restored, CHF.SHA2_256));
			if (!same)
				throw new InvalidDataException("Restored content does not match the original.");
			var compressedLength = new FileInfo(compressed).Length;
			var ratio = originalLength == 0 ? "n/a (empty input)" : ((double)compressedLength / originalLength).ToString("P2");
			output.AppendLine($"Framework: {originalLength:N0} bytes → {compressedLength:N0} bytes; ratio {ratio}; content verified.");
			return output.ToString();
		} finally {
			try {
				File.Delete(compressed);
			} finally {
				if (restored != null)
					File.Delete(restored);
			}
		}
	}
}
