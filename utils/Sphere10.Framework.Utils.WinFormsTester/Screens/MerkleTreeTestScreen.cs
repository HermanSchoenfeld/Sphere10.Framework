// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using Sphere10.Framework.Application;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

/// <summary>Shows real Merkle structures, partition arithmetic and cancellable implementation comparisons.</summary>
public partial class MerkleTreeTestScreen : ApplicationScreen {
	private CancellationTokenSource _comparisonCancellation;

	/// <summary>Creates a bounded workbench without allocating a tree or starting work.</summary>
	public MerkleTreeTestScreen() {
		InitializeComponent();
		Disposables.Add(_heading.Font);
		Disposables.Add(_outputTextBox.Font);
	}

	/// <summary>The preferences declared by this screen type and restored for its exact registration.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public override MerkleTreeScreenSettings ScreenSettings => (MerkleTreeScreenSettings)base.ScreenSettings;

	// Apply restored calculation choices after profile attachment.

	// Accepted Options updates change the next comparison, never its current tree data.
	protected override void CopyModelToUI() {
		base.CopyModelToUI();
		ApplyPreferences();
	}

	// The background operation owns only local trees and checks cancellation before publishing.
	protected override void OnDestroyScreen() {
		_comparisonCancellation?.Cancel();
		base.OnDestroyScreen();
	}

	// Readable concatenation makes the alphabet's parent/child structure visible.
	private void AlphabetClicked(object sender, EventArgs args) {
		ShowReadableTree(Enumerable.Range('A', 26).Select(value => ((char)value).ToString()).ToArray(), "Alphabet");
	}

	// Keep the integer example useful without printing every previous complete tree.
	private void IntegersClicked(object sender, EventArgs args) {
		ShowReadableTree(Enumerable.Range(1, 32).Select(value => value.ToString()).ToArray(), "Integers 1–32");
	}

	// Exercise the original partition multiplication with explicit expected-result verification.
	private void PartitionsClicked(object sender, EventArgs args) {
		using var output = new StringWriter();
		output.WriteLine("POWER-OF-TWO PARTITION MULTIPLICATION");
		for (var value = 0; value < 64; value++) {
			var result = Pow2.Mul(Pow2.CalculatePow2Partition(value), Pow2.CalculatePow2Partition(value + 1));
			var expected = Pow2.CalculatePow2Partition(value * (value + 1));
			output.WriteLine($"{value} × {value + 1} = {value * (value + 1)}: [{result.ToDelimittedString(", ")}] — {(result.SequenceEqual(expected) ? "PASS" : "FAIL")}");
		}
		_outputTextBox.Text = output.ToString();
		_status.Text = "Verified 64 adjacent-integer products against their direct power-of-two partition.";
	}

	// Retain the finite reciprocal-series demonstration with readable precision.
	private void InversePowersClicked(object sender, EventArgs args) {
		using var output = new StringWriter();
		var sum = 0M;
		for (var power = 0; power < 64; power++) {
			var term = 1M / (1UL << power);
			sum += term;
			output.WriteLine($"1 / 2^{power} = {term}; cumulative sum = {sum}");
		}
		_outputTextBox.Text = output.ToString();
		_status.Text = "Inverse powers complete. Decimal precision limits the smallest displayed terms.";
	}

	// Native command adapters keep the owned task and its error reporting on the UI dispatcher.
	private async void CompareClicked(object sender, EventArgs args) {
		await CompareAsync(false);
	}

	// Append comparison preserves the original batch/root recalculation scenario.
	private async void AppendClicked(object sender, EventArgs args) {
		await CompareAsync(true);
	}

	// Cancel the existing operation without starting a second worker.
	private void CancelClicked(object sender, EventArgs args) {
		_comparisonCancellation?.Cancel();
	}

	// Clear only output, leaving the comparison choices intact.
	private void ClearClicked(object sender, EventArgs args) {
		_outputTextBox.Clear();
		_status.Text = "Results cleared.";
	}

	// Capture input once; worker code never reads native controls.
	private async Task CompareAsync(bool appendBatches) {
		if (_comparisonCancellation != null)
			return;
		var count = (int)_leafCount.Value;
		using var cancellation = new CancellationTokenSource();
		_comparisonCancellation = cancellation;
		SetBusy(true);
		_status.Text = $"Comparing {count:N0} SHA-256 leaves…";
		try {
			var result = await Task.Run(() => CompareTrees(count, appendBatches, cancellation.Token), cancellation.Token);
			if (!IsDisposed && !Disposing) {
				_outputTextBox.Text = result;
				_status.Text = "Comparison complete. Matching roots validate the same input sequence; timings are illustrative.";
			}
		} catch (OperationCanceledException) when (cancellation.IsCancellationRequested) {
			if (!IsDisposed && !Disposing)
				_status.Text = "Comparison cancelled.";
		} catch (Exception error) {
			SystemLog.Exception(error, "The Merkle comparison failed.");
			if (!IsDisposed && !Disposing)
				_status.Text = "Could not complete the comparison: " + error.Message;
		} finally {
			_comparisonCancellation = null;
			if (!IsDisposed && !Disposing)
				SetBusy(false);
		}
	}

	// All implementations receive identical deterministic hashes; no global hasher registrations are changed.
	private static string CompareTrees(int count, bool appendBatches, CancellationToken cancellationToken) {
		var items = new byte[count][];
		for (var index = 0; index < count; index++) {
			cancellationToken.ThrowIfCancellationRequested();
			items[index] = Hashers.Hash(CHF.SHA2_256, Encoding.UTF8.GetBytes($"Demonstration leaf {index}"));
		}
		var clock = Stopwatch.StartNew();
		var simple = new SimpleMerkleTree(CHF.SHA2_256, items);
		var simpleRoot = simple.Root;
		var simpleDuration = clock.Elapsed;
		clock.Restart();
		var flat = new FlatMerkleTree(CHF.SHA2_256, items);
		var flatRoot = flat.Root;
		var flatDuration = clock.Elapsed;
		var output = new StringBuilder();
		output.AppendLine($"INITIAL {count:N0} LEAVES");
		output.AppendLine($"Simple: {simpleRoot.ToHexString()} ({simpleDuration.TotalMilliseconds:N3} ms)");
		output.AppendLine($"Flat:   {flatRoot.ToHexString()} ({flatDuration.TotalMilliseconds:N3} ms)");
		output.AppendLine($"Roots match: {ByteArrayEqualityComparer.Instance.Equals(simpleRoot, flatRoot)}");

		if (appendBatches) {
			for (var batch = 0; batch < 5; batch++) {
				cancellationToken.ThrowIfCancellationRequested();
				var additions = Enumerable.Range(0, 32)
					.Select(index => Hashers.Hash(CHF.SHA2_256, Encoding.UTF8.GetBytes($"Batch {batch}, leaf {index}")))
					.ToArray();
				clock.Restart();
				simple.Leafs.AddRange(additions);
				simpleRoot = simple.Root;
				simpleDuration = clock.Elapsed;
				clock.Restart();
				flat.Leafs.AddRange(additions);
				flatRoot = flat.Root;
				flatDuration = clock.Elapsed;
				output.AppendLine();
				output.AppendLine($"Batch {batch + 1}: {simple.Size.LeafCount:N0} leaves; roots match: {ByteArrayEqualityComparer.Instance.Equals(simpleRoot, flatRoot)}");
				output.AppendLine($"Simple {simpleDuration.TotalMilliseconds:N3} ms; flat {flatDuration.TotalMilliseconds:N3} ms; root {flatRoot.ToHexString()}");
			}
		}
		cancellationToken.ThrowIfCancellationRequested();
		return output.ToString();
	}

	// Render the actual node hierarchy for the final state of a small readable tree.
	private void ShowReadableTree(string[] values, string description) {
		var tree = new SimpleMerkleTree(CHF.ConcatBytes);
		tree.Leafs.AddRange(values.Select(Encoding.ASCII.GetBytes));
		using var output = new StringWriter();
		output.WriteLine($"{description}: {tree.Size.LeafCount} leaves, height {tree.Size.Height}");
		output.PrintTree(
			tree.GetRootNode(),
			node => Encoding.ASCII.GetString(node.Hash),
			node => {
				if (MerkleMath.IsLeaf(node.Coordinate))
					return Enumerable.Empty<MerkleNode>();
				var children = tree.GetChildren(node.Coordinate);
				return new[] { children.Left }.Concat(children.Right != null ? new[] { children.Right } : Enumerable.Empty<MerkleNode>());
			}
		);
		_outputTextBox.Text = output.ToString();
		_status.Text = "Readable tree rendered. Concatenation is used only to expose the structure in this example.";
	}

	// Prevent overlapping calculations while retaining a usable cancellation action.
	private void SetBusy(bool busy) {
		foreach (Control control in _commands.Controls)
			control.Enabled = !busy;
		_cancelButton.Enabled = busy;
		_leafCount.Enabled = !busy;
	}

	// Only deliberate changes update the ordinary profile values.
	private void PreferencesEdited(object sender, EventArgs args) {
		if (!HasSettings || Updating)
			return;
		ScreenSettings.ComparisonLeaves = (int)_leafCount.Value;
		MarkSettingsChanged();
	}

	// Standalone/designer instances use their existing bounded defaults.
	private void ApplyPreferences() {
		if (!HasSettings || IsDisposed || Disposing)
			return;
		using (EnterUpdateScope(FinishedUpdateBehaviour.DoNothing))
			_leafCount.Value = Math.Clamp(ScreenSettings.ComparisonLeaves, 1, 20000);
	}

	#region Inner Types

	/// <summary>The bounded input size of the Merkle comparison, without its generated trees or hashes.</summary>
	[UserEditable]
	public class MerkleTreeScreenSettings : SettingsObject {
		/// <summary>The number of identical leaves supplied to both implementations.</summary>
		[DefaultValue(256)]
		[DisplayName("Comparison leaves"), Range(1, 20000)]
		public int ComparisonLeaves {
			get; set;
		} = 256;
	}
	#endregion
}
