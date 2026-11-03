// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Utils.WinFormsTester.Screens;

/// <summary>Demonstrates a permanent notes workspace whose lifetime remains controlled by the host.</summary>
public class ScreenHostingPermanentTestScreen : ScreenHostingTestScreen {
	/// <summary>Creates the inherited surface with the existing permanent activation contract.</summary>
	public ScreenHostingPermanentTestScreen()
		: base("Permanent Notes", ScreenActivationMode.PermanentSingleton) {
	}
}
