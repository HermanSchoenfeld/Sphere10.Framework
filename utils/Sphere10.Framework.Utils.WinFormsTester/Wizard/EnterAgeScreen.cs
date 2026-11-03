// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Threading.Tasks;

namespace Sphere10.Framework.Utils.WinFormsTester.Wizard;

/// <summary>Collects a validated non-negative age while retaining backward navigation.</summary>
public partial class EnterAgeScreen : DemoWizardScreenBase {
	/// <summary>Creates the complete designable page without activating a wizard.</summary>
	public EnterAgeScreen() {
		InitializeComponent();
		Disposables.Add(_heading.Font);
	}

	/// <summary>Validates the current native inputs before advancing.</summary>
	public override Task<Result> Validate() {
		var validation = int.TryParse(_textBox1.Text, out var age) && age >= 0
			? Result.Success
			: Result.Error("Enter your age as a non-negative whole number.");
		return Task.FromResult(validation);
	}

	/// <summary>Retains validated values in the current wizard model.</summary>
	public override Task OnNext() {
		CopyUIToModel();
		return Task.CompletedTask;
	}

	// Copy validated input into this wizard session.
	protected override void CopyUIToModel() {
		Model.Age = int.Parse(_textBox1.Text);
	}

	// Restore the retained model when this page is presented again.
	protected override void CopyModelToUI() {
		if (Wizard != null)
			_textBox1.Text = Model.Age?.ToString() ?? string.Empty;
	}
}
