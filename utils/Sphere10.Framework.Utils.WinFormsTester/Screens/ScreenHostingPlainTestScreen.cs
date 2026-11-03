// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester.Screens;

/// <summary>A content-only screen for checking docking without reserved empty command rows.</summary>
public partial class ScreenHostingPlainTestScreen : ApplicationScreen {
	/// <summary>Creates a genuinely bar-free editable screen with ordinary instance-owned notes.</summary>
	public ScreenHostingPlainTestScreen() {
		InitializeComponent();
		Disposables.Add(_heading.Font);
		Title = "Plain screen";
		ActivationMode = ScreenActivationMode.MultiInstance;
	}
}
