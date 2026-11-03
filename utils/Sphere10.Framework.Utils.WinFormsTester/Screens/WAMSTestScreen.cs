// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Sphere10.Framework.Application;
using Sphere10.Framework.CryptoEx;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

/// <summary>Exercises the existing WAMS algorithms with bounded, owned background workloads.</summary>
public partial class WAMSTestScreen : WinFormsApplicationScreen {
	private CancellationTokenSource _operationCancellation;

	/// <summary>Creates parameter choices without generating keys or replacing process hash providers.</summary>
	public WAMSTestScreen() {
		InitializeComponent();
		Disposables.Add(_heading.Font);
		_hashAlgControl.EnumType = typeof(CHF);
		_hashAlgControl.SelectedEnum = CHF.SHA2_256;
	}

	/// <summary>The preferences declared by this screen type and restored for its exact registration.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public override WamsScreenSettings ScreenSettings {
		get {
			return (WamsScreenSettings)base.ScreenSettings;
		}
	}

	protected override void CopyModelToUI() {
		base.CopyModelToUI();
		if (!HasSettings || _operationCancellation != null)
			return;
		var values = ScreenSettings;
		using (EnterUpdateScope(FinishedUpdateBehaviour.DoNothing)) {
			_heightControl.Value = Math.Clamp(values.TreeHeight, 1, 16);
			_winternitzControl.Value = Math.Clamp(values.Winternitz, 1, 16);
			_seconds.Value = Math.Clamp(values.BenchmarkSeconds, 1, 30);
			_hashAlgControl.SelectedEnum = values.HashFunction;
		}
	}

	protected override void OnDestroyScreen() {
		_operationCancellation?.Cancel();
		base.OnDestroyScreen();
	}

	protected override void CopyUIToModel() {
		base.CopyUIToModel();
		if (Updating || !HasSettings || _hashAlgControl.SelectedEnum == null)
			return;
		var values = ScreenSettings;
		var changed = values.TreeHeight != decimal.ToInt32(_heightControl.Value)
			|| values.Winternitz != decimal.ToInt32(_winternitzControl.Value)
			|| values.BenchmarkSeconds != decimal.ToInt32(_seconds.Value)
			|| values.HashFunction != (CHF)_hashAlgControl.SelectedEnum;
		if (!changed)
			return;
		values.TreeHeight = decimal.ToInt32(_heightControl.Value);
		values.Winternitz = decimal.ToInt32(_winternitzControl.Value);
		values.BenchmarkSeconds = decimal.ToInt32(_seconds.Value);
		values.HashFunction = (CHF)_hashAlgControl.SelectedEnum;
		MarkSettingsChanged();
	}

	private void PreferencesChanged(object sender, EventArgs e) {
		CopyUIToModel();
	}

	private async void _keyMatchingTestsButton_Click(object sender, EventArgs e) {
		await RunAsync(0);
	}

	private async void _verificationSpeedButton_Click(object sender, EventArgs e) {
		await RunAsync(1);
	}

	private async void _miscButton_Click(object sender, EventArgs e) {
		await RunAsync(2);
	}

	private async void _miscButton2_Click(object sender, EventArgs e) {
		await RunAsync(3);
	}

	// Every operation captures configuration once and owns its cancellation source through completion.
	private async Task RunAsync(int mode) {
		if (_operationCancellation != null)
			return;
		var height = decimal.ToInt32(_heightControl.Value);
		var winternitz = decimal.ToInt32(_winternitzControl.Value);
		var hash = (CHF)_hashAlgControl.SelectedEnum;
		var seconds = decimal.ToInt32(_seconds.Value);
		var fullMatrix = _fullMatrix.Checked;
		using var cancellation = new CancellationTokenSource();
		_operationCancellation = cancellation;
		SetBusy(true);
		try {
			_status.Text = "Preparing keys and running the selected workload…";
			var output = await Task.Run(() => RunWorkload(mode, height, winternitz, hash, seconds, fullMatrix, cancellation.Token));
			if (!IsDisposed && !Disposing) {
				_outputTextBox.Text = output;
				_status.Text = "Completed. Rates describe this local demonstration run.";
			}
		} catch (OperationCanceledException) when (cancellation.IsCancellationRequested) {
			if (!IsDisposed && !Disposing)
				_status.Text = "Cancelled after the current cryptographic operation.";
		} catch (Exception error) {
			if (!IsDisposed && !Disposing)
				_status.Text = "Cryptographic workload failed: " + error.Message;
		} finally {
			_operationCancellation = null;
			if (!IsDisposed && !Disposing)
				SetBusy(false);
			if (!IsDisposed && !Disposing)
				Refresh();
		}
	}

	// Bounded loops replace the old unsynchronized indefinite workers; algorithms and digest contracts are unchanged.
	private static string RunWorkload(int mode, int height, int winternitz, CHF hash, int seconds, bool fullMatrix, CancellationToken cancellationToken) {
		if (mode == 3) {
			var output = new StringBuilder("Scheme\tHash\tWinternitz\tHeight\tPublic bytes\tSignature bytes\tSign/s\tVerify/s" + Environment.NewLine);
			foreach (var algorithm in fullMatrix ? new[] { CHF.Blake2b_128_Fast, CHF.Blake2b_160_Fast, CHF.Blake2b_256_Fast } : new[] { hash })
			foreach (var w in fullMatrix ? new[] { 2, 4, 8 } : new[] { winternitz })
			foreach (var h in fullMatrix ? new[] { 0, 8, 16 } : new[] { height })
			foreach (var sharp in new[] { false, true }) {
				cancellationToken.ThrowIfCancellationRequested();
				AMS scheme = sharp ? new WAMSSharp(h, w, algorithm) : new WAMS(h, w, algorithm);
				var key = scheme.GeneratePrivateKey();
				var publicKey = scheme.DerivePublicKeyForBatch(key, 0);
				var digest = Hashers.Hash(algorithm, Encoding.UTF8.GetBytes("WAMS parameter comparison"));
				var signature = scheme.SignDigest(key, digest, 0, 0);
				Guard.Ensure(scheme.VerifyDigest(signature, digest, publicKey), "Generated signature failed verification.");
				var watch = Stopwatch.StartNew();
				for (var index = 0; index < 20; index++) {
					cancellationToken.ThrowIfCancellationRequested();
					scheme.SignDigest(key, digest, 0, 0);
				}
				var signRate = 20 / Math.Max(watch.Elapsed.TotalSeconds, double.Epsilon);
				watch.Restart();
				for (var index = 0; index < 20; index++) {
					cancellationToken.ThrowIfCancellationRequested();
					scheme.VerifyDigest(signature, digest, publicKey);
				}
				var verifyRate = 20 / Math.Max(watch.Elapsed.TotalSeconds, double.Epsilon);
				output.AppendLine($"{(sharp ? "WAMS#" : "WAMS")}\t{algorithm}\t{w}\t{h}\t{publicKey.RawBytes.Length}\t{signature.Length}\t{signRate:F1}\t{verifyRate:F1}");
			}
			return output.ToString();
		}

		var wams = new WAMSSharp(height, winternitz, hash);
		var privateKey = wams.GeneratePrivateKey();
		cancellationToken.ThrowIfCancellationRequested();
		var publicValue = wams.DerivePublicKeyForBatch(privateKey, 0);
		var messageDigest = Hashers.Hash(hash, Encoding.UTF8.GetBytes("WAMS signature demonstration"));
		var signed = wams.SignDigest(privateKey, messageDigest, 0, 0);
		if (mode == 2) {
			var altered = messageDigest.ToArray();
			altered[0] ^= 1;
			return $"Valid signature accepted: {wams.VerifyDigest(signed, messageDigest, publicValue)}" + Environment.NewLine
				+ $"Altered digest rejected: {!wams.VerifyDigest(signed, altered, publicValue)}" + Environment.NewLine
				+ $"Public key: {publicValue.RawBytes.Length:N0} bytes; signature: {signed.Length:N0} bytes.";
		}
		var nonMatching = publicValue.RawBytes.ToArray();
		nonMatching[0] ^= 1;
		var elapsed = Stopwatch.StartNew();
		long count = 0;
		var workers = Math.Max(1, Environment.ProcessorCount);
		// Retain the original parallel throughput measurement, but join every worker within one bounded operation.
		Parallel.For(0, workers, new ParallelOptions { MaxDegreeOfParallelism = workers, CancellationToken = cancellationToken }, _ => {
			while (elapsed.Elapsed < TimeSpan.FromSeconds(seconds)) {
				cancellationToken.ThrowIfCancellationRequested();
				if (mode == 0)
					wams.IsPublicKey(privateKey, nonMatching);
				else
					Guard.Ensure(wams.VerifyDigest(signed, messageDigest, publicValue), "Signature verification failed.");
				Interlocked.Increment(ref count);
			}
		});
		return $"{count:N0} {(mode == 0 ? "non-matching key checks" : "signature verifications")} in {elapsed.Elapsed.TotalSeconds:F2} seconds." + Environment.NewLine
			+ $"Rate: {count / elapsed.Elapsed.TotalSeconds:N1} operations/second across {workers} parallel worker(s).";
	}

	private void CancelClicked(object sender, EventArgs e) {
		_operationCancellation?.Cancel();
		_status.Text = "Cancellation requested; the current cryptographic operation will finish first.";
	}

	private void SetBusy(bool busy) {
		foreach (var button in new[] { _keyMatchingTestsButton, _verificationSpeedButton, _miscButton, _miscButton2, _registerStandardHashers, _regFastButton })
			button.Enabled = !busy;
		_cancelButton.Enabled = busy;
	}

	// These explicit process-wide registration examples are retained; opening the screen never invokes them.
	private void _registerStandardHashers_Click(object sender, EventArgs e) {
		Hashers.RegisterDefaultAlgorithms();
		_status.Text = "Standard hash providers registered for this process.";
	}

	private void _regFastButton_Click(object sender, EventArgs e) {
		Hashers.Register(CHF.Blake2b_512_Fast, () => new Blake2bFastAdapter(64));
		Hashers.Register(CHF.Blake2b_384_Fast, () => new Blake2bFastAdapter(48));
		Hashers.Register(CHF.Blake2b_256_Fast, () => new Blake2bFastAdapter(32));
		Hashers.Register(CHF.Blake2b_128_Fast, () => new Blake2bFastAdapter(16));
		Hashers.Register(CHF.Blake2b_224_Fast, () => new Blake2bFastAdapter(28));
		Hashers.Register(CHF.Blake2b_160_Fast, () => new Blake2bFastAdapter(20));
		Hashers.Register(CHF.Blake2s_256_Fast, () => new Blake2sFastAdapter(32));
		Hashers.Register(CHF.Blake2s_224_Fast, () => new Blake2sFastAdapter(28));
		Hashers.Register(CHF.Blake2s_160_Fast, () => new Blake2sFastAdapter(20));
		Hashers.Register(CHF.Blake2s_128_Fast, () => new Blake2sFastAdapter(16));
		_status.Text = "Fast Blake2 providers registered for this process.";
	}

	#region Inner Types

	/// <summary>Configuration preferences for the WAMS signature laboratory.</summary>
	[UserEditable]
	public class WamsScreenSettings : SettingsObject {
		/// <summary>The Merkle tree height; larger trees require substantially more preparation.</summary>
		[DefaultValue(1)]
		[Range(1, 16)]
		public int TreeHeight { get; set; } = 1;

		/// <summary>The Winternitz parameter.</summary>
		[DefaultValue(8)]
		[Range(1, 16)]
		public int Winternitz { get; set; } = 8;

		/// <summary>The existing Framework hash algorithm.</summary>
		[DefaultValue(CHF.SHA2_256)]
		public CHF HashFunction { get; set; } = CHF.SHA2_256;

		/// <summary>The bounded matching/verification duration.</summary>
		[DefaultValue(3)]
		[Range(1, 30)]
		public int BenchmarkSeconds { get; set; } = 3;
	}
	#endregion
}
