// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

/// <summary>Connects a retained lock choice to an editor and shows the resulting state events.</summary>
public partial class PadLockTestScreen : ApplicationScreen {
	/// <summary>Creates the active and disabled lock examples.</summary>
	public PadLockTestScreen() {
		InitializeComponent();
		Disposables.Add(_heading.Font);
	}

	// The lock's completed event controls the editor; the icon itself is not an authorization mechanism.
	private void PadLockButton1_PadLockStateChanged(PadLockButton control, PadLockButton.PadLockState state) {
		_textBox2.ReadOnly = state == PadLockButton.PadLockState.Locked;
		_lockSummary.Text = _textBox2.ReadOnly ? "Locked: the protected value is read-only." : "Unlocked: the protected value is editable.";
		_outputTextBox.AppendText($"{control.Name}: {state}" + Environment.NewLine);
	}
}
