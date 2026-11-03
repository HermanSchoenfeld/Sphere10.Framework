// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework.Application.UI;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester.Screens;

/// <summary>Demonstrates a retained single-instance lifecycle workspace.</summary>
public class ScreenHostingSettingsTestScreen : ScreenHostingTestScreen {
	/// <summary>Creates this registration's inherited editable lifecycle surface.</summary>
	public ScreenHostingSettingsTestScreen()
		: base("Settings", ScreenActivationMode.SingleInstance) {
	}
}
