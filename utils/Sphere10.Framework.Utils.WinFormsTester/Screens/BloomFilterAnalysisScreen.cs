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
using System.Globalization;
using System.Text;
using System.Windows.Forms;
using Sphere10.Framework.Application;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

/// <summary>Compares the actual Bloom-filter sizing API using explicit bit and byte units.</summary>
public partial class BloomFilterAnalysisScreen : ApplicationScreen {
	/// <summary>Creates the sizing workbench without allocating a filter or reading settings.</summary>
	public BloomFilterAnalysisScreen() {
		InitializeComponent();
		Disposables.Add(_heading.Font);
		Disposables.Add(_standardTextBox.Font);
	}

	/// <summary>The preferences declared by this screen type and restored for its exact registration.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public override BloomFilterScreenSettings ScreenSettings {
		get {
			return (BloomFilterScreenSettings)base.ScreenSettings;
		}
	}

	// Options updates the same inputs without starting calculations or writing preferences.
	protected override void CopyModelToUI() {
		base.CopyModelToUI();
		ApplyPreferences();
	}

	// Describe one requested filter through the public maths implementation.
	private void EstimateClicked(object sender, EventArgs args) {
		var output = new StringBuilder();
		output.AppendLine("Expected items\tHash rounds\tTarget error\tFilter bits\tRounded bytes\tPredicted error");
		AppendEstimate(output, (long)_expectedItems.Value, _targetError.Value, (int)_hashRounds.Value);
		_standardTextBox.Text = output.ToString();
		_status.Text = "Estimate complete. Byte counts round up; no estimated storage was allocated.";
	}

	// Retain the comparison table within the current API's supported bit range.
	private void MetricsClicked(object sender, EventArgs args) {
		var output = new StringBuilder();
		output.AppendLine("Expected items\tHash rounds\tTarget error\tFilter bits\tRounded bytes\tPredicted error");
		var count = (long)_expectedItems.Value;
		foreach (var items in new[] { Math.Max(1, count / 10), count, Math.Min(1000000, count * 10) }) {
			foreach (var rounds in new[] { 3, 4, 5 })
				AppendEstimate(output, items, _targetError.Value, rounds);
		}
		_standardTextBox.Text = output.ToString();
		_status.Text = "Compared three item budgets and three hash-round counts. Unsupported rows are explained in the results.";
	}

	// Keep clearing separate from the user's retained calculation choices.
	private void ClearClicked(object sender, EventArgs args) {
		_standardTextBox.Clear();
		_status.Text = "Results cleared. Calculation choices are unchanged.";
	}

	// Only deliberate edits mark the ordinary registration values dirty.
	private void PreferencesEdited(object sender, EventArgs args) {
		if (!HasSettings || Updating)
			return;
		var settings = ScreenSettings;
		settings.ExpectedItems = (int)_expectedItems.Value;
		settings.TargetError = _targetError.Value;
		settings.HashRounds = (int)_hashRounds.Value;
		MarkSettingsChanged();
	}

	// Standalone and designer instances retain the useful Designer defaults.
	private void ApplyPreferences() {
		if (!HasSettings || IsDisposed || Disposing)
			return;
		var settings = ScreenSettings;
		using (EnterUpdateScope(FinishedUpdateBehaviour.DoNothing)) {
			_expectedItems.Value = Math.Clamp(settings.ExpectedItems, 1, 1000000);
			_targetError.Value = Math.Clamp(settings.TargetError, 0.0001M, 0.5M);
			_hashRounds.Value = Math.Clamp(settings.HashRounds, 1, 16);
		}
	}

	// EstimateBloomFilterLength returns bits and currently narrows its result to Int32.
	private static void AppendEstimate(StringBuilder output, long items, decimal error, int rounds) {
		var expectedBits = -rounds * (double)items / Math.Log(1 - Math.Pow((double)error, 1d / rounds));
		if (!double.IsFinite(expectedBits) || expectedBits > int.MaxValue) {
			output.AppendLine($"{items:N0}\t{rounds}\t{error:P2}\tOutside the current Int32 bit-length range");
			return;
		}

		var bits = BloomFilterMath.EstimateBloomFilterLength(error, items, rounds);
		var bytes = (bits + 7) / 8;
		var predicted = BloomFilterMath.EstimateBloomFilterError(bits, items, rounds);
		output.AppendLine(string.Format(CultureInfo.CurrentCulture, "{0:N0}\t{1}\t{2:P2}\t{3:N0}\t{4:N0}\t{5:P4}", items, rounds, error, bits, bytes, predicted));
	}

	#region Inner Types

	/// <summary>Sizing choices for the Bloom-filter workbench, independent of generated results.</summary>
	[UserEditable]
	public class BloomFilterScreenSettings : SettingsObject {
		/// <summary>The anticipated number of distinct items.</summary>
		[DefaultValue(100000)]
		[DisplayName("Expected items"), Range(1, 1000000)]
		public int ExpectedItems {
			get; set;
		} = 100000;

		/// <summary>The target false-positive probability.</summary>
		[DefaultValue(typeof(decimal), "0.01")]
		[DisplayName("Target error"), Range(typeof(decimal), "0.0001", "0.5")]
		public decimal TargetError {
			get; set;
		} = 0.01M;

		/// <summary>The number of independent hash rounds.</summary>
		[DefaultValue(4)]
		[DisplayName("Hash rounds"), Range(1, 16)]
		public int HashRounds {
			get; set;
		} = 4;
	}
	#endregion
}
