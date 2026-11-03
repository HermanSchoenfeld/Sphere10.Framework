// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Utils.WinFormsTester.Wizard;

/// <summary>Displays the final values from the current wizard session.</summary>
public partial class ConfirmScreen : DemoWizardScreenBase {
	/// <summary>Creates the complete designable page without activating a wizard.</summary>
	public ConfirmScreen() {
		InitializeComponent();
		Disposables.Add(_heading.Font);
	}

	// Restore the retained model when this page is presented again.
	protected override void CopyModelToUI() {
		if (Wizard == null)
			return;
		_label3.Text = Model.Name;
		_label4.Text = Model.Age?.ToString() ?? string.Empty;
	}
}
