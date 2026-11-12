// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework.Application.UI;

namespace Sphere10.Framework.Utils.WinFormsTester.Screens;

/// <summary>Demonstrates the host's retained single-instance empty fallback.</summary>
public class ScreenHostingEmptyTestScreen : ScreenHostingTestScreen {
	/// <summary>Creates this registration's inherited editable lifecycle surface.</summary>
	public ScreenHostingEmptyTestScreen()
		: base("Cached empty workspace", ScreenActivationMode.SingleInstance) {
	}
}
