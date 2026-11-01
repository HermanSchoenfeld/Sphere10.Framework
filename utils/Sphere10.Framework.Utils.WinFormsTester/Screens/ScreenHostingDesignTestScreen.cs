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

/// <summary>Retains the lifecycle demonstration and optionally hosts typed inspector preferences and grids.</summary>
public partial class ScreenHostingDesignTestScreen : ScreenHostingTestScreen {
	/// <summary>Creates the ordinary Design registration without declared preference behavior.</summary>
	public ScreenHostingDesignTestScreen()
		: this("Design") {
	}

	/// <summary>Lets independent registrations supply their own construction-time workspace profile.</summary>
	public ScreenHostingDesignTestScreen(string profile)
		: base(profile, ScreenActivationMode.MultiInstance) {
	}

	/// <summary>Adds the typed preference demonstration while preserving the registration's original constructor argument.</summary>
	public ScreenHostingDesignTestScreen(string profile, string standaloneGridKey)
		: this(profile) {
		InitializePreferencesDemo(standaloneGridKey);
	}
}
