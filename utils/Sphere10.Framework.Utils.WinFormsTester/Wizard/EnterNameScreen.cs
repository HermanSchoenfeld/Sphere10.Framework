// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Threading.Tasks;

namespace Sphere10.Framework.Utils.WinFormsTester.Wizard;

/// <summary>Collects a validated name and explicit confirmation for the wizard model.</summary>
public partial class EnterNameScreen : DemoWizardScreenBase {
	/// <summary>Creates the complete designable page without activating a wizard.</summary>
	public EnterNameScreen() {
		InitializeComponent();
		Disposables.Add(_heading.Font);
	}

	/// <summary>Validates the current native inputs before advancing.</summary>
	public override Task<Result> Validate() {
		var validation = Result.Default;
		if (string.IsNullOrWhiteSpace(_textBox1.Text))
			validation.AddError("Enter your name.");
		if (!_checkBox1.Checked)
			validation.AddError("Check the confirmation box to proceed.");
		return Task.FromResult(validation);
	}

	/// <summary>Retains validated values in the current wizard model.</summary>
	public override Task OnNext() {
		CopyUIToModel();
		return Task.CompletedTask;
	}

	// Copy validated input into this wizard session.
	protected override void CopyUIToModel() => Model.Name = _textBox1.Text.Trim();

	// Restore the retained model when this page is presented again.
	protected override void CopyModelToUI() {
		if (Wizard != null)
			_textBox1.Text = Model.Name;
	}
}
