// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester.Screens;

public class ScreenHostingSettingsTestScreen : ScreenHostingTestScreen {
	public ScreenHostingSettingsTestScreen()
		: base("Settings", ScreenActivationMode.SingleInstance) {
	}
}
