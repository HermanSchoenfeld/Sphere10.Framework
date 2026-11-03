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

/// <summary>Combines real expandable groups with instance-owned ListMerger membership.</summary>
public partial class ScreenB : WinFormsApplicationScreen {
	/// <summary>Creates the static surface without priming runtime sample lists.</summary>
	public ScreenB() {
		InitializeComponent();
		Disposables.Add(_heading.Font);
	}

	// Prime demonstration membership only when the native surface first loads.
	protected override void InitializeUIPrimingData() {
		base.InitializeUIPrimingData();
		ResetLists();
	}

	// Reset is an explicit user action; it does not replace this screen instance.
	private void ResetClicked(object sender, EventArgs args) {
		ResetLists();
	}

	// Preserve the original two independent list populations using useful sample names.
	private void ResetLists() {
		_listMerger1.LeftItems = new object[] { "Analysis", "Development", "Documentation", "Testing" };
		_listMerger1.RightItems = new object[] { "Planning", "Review" };
	}
}
