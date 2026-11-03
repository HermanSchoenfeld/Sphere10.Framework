// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Threading;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

/// <summary>Demonstrates manual states and completed asynchronous validation without worker-thread UI access.</summary>
public partial class ValidationIndicatorTestScreen : ApplicationScreen {
	private string _validationInput = string.Empty;

	/// <summary>Creates both validation paths without starting a validation request.</summary>
	public ValidationIndicatorTestScreen() {
		InitializeComponent();
		Disposables.Add(_heading.Font);
	}

	// Manual state buttons expose the same real control state machine independently of validation.
	private void ValidButton_Click(object sender, EventArgs args) {
		_validationIndicator1.State = ValidationState.Valid;
	}

	// An error has its own visible state and tooltip semantics.
	private void ErrorButton_Click(object sender, EventArgs args) {
		_validationIndicator1.State = ValidationState.Error;
	}

	// The pending state uses the control's actual loading indicator.
	private void ValidatingButton_Click(object sender, EventArgs args) {
		_validationIndicator1.State = ValidationState.Validating;
	}

	// Disabled state clears the current validation presentation.
	private void DisabledButton_Click(object sender, EventArgs args) {
		_validationIndicator1.State = ValidationState.Disabled;
	}

	// Capture text on its native owner before the control invokes PerformValidation on a worker.
	private void TextBox_TextChanged(object sender, EventArgs args) {
		Volatile.Write(ref _validationInput, _textBox.Text);
		_validationIndicator2.RunValidation();
	}

	// The callback reads only the immutable snapshot and writes the supplied result object.
	private void ValidationIndicator2_PerformValidation(ValidationIndicator control, ValidationIndicatorEvent args) {
		var input = Volatile.Read(ref _validationInput);
		Thread.Sleep(250);
		args.ValidationResult = string.Equals(input, "Herman", StringComparison.Ordinal);
		args.ValidationMessage = args.ValidationResult == true ? "The input matches Herman." : "Enter Herman to satisfy this demonstration rule.";
	}

	// State completion returns to the native dispatcher before touching the output.
	private void ValidationIndicator1_ValidationStateChanged(ValidationIndicator control, ValidationState previous, ValidationState current) {
		RecordState(control, previous, current);
	}

	// The input example shares the same completed-state reporting path.
	private void ValidationIndicator2_ValidationStateChanged(ValidationIndicator control, ValidationState previous, ValidationState current) {
		RecordState(control, previous, current);
	}

	// Ignore delivery to a screen whose native lifetime has ended.
	private void RecordState(ValidationIndicator control, ValidationState previous, ValidationState current) {
		if (!IsDisposed && !Disposing)
			_outputTextBox.AppendText($"{control.Name}: {previous} → {current}" + Environment.NewLine);
	}

	// Availability is separate from the last validation result.
	private void EnableDisableButton_Click(object sender, EventArgs args) {
		_validationIndicator2.Enabled = !_validationIndicator2.Enabled;
		_enableDisableButton.Text = _validationIndicator2.Enabled ? "Disable validator" : "Enable validator";
	}
}
