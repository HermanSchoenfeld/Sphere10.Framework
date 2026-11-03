// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

/// <summary>An isolated native screen for demonstrating the actual host and detached-window controls.</summary>
public partial class ControlCatalogHostedScreen : WinFormsApplicationScreen {
	public ControlCatalogHostedScreen() {
		InitializeComponent();
		Title = "Native screen example";
	}
}

