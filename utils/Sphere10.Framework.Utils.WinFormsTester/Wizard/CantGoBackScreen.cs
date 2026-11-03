// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Threading.Tasks;

namespace Sphere10.Framework.Utils.WinFormsTester.Wizard;

/// <summary>Demonstrates a forward-only page without changing the retained wizard model.</summary>
public partial class CantGoBackScreen : DemoWizardScreenBase {
	/// <summary>Creates the complete designable page without activating a wizard.</summary>
	public CantGoBackScreen() {
		InitializeComponent();
		Disposables.Add(_heading.Font);
	}

	/// <summary>Restores the model and hides the previous-page action for this step.</summary>
	public override async Task OnPresent() {
		await base.OnPresent();
		if (Wizard != null)
			Wizard.HidePrevious = true;
	}
}
