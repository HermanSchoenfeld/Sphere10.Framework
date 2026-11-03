// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.ComponentModel;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Controls whether the header menu's default restoration also saves the grid configuration.</summary>
public enum VisualGridDefaultsRestorePolicy {

	/// <summary>Changes the current view until Save configuration is explicitly selected.</summary>
	[Description("Restore the current view only")]
	RestoreOnly,

	/// <summary>Restores declared defaults and saves them through the owning screen.</summary>
	[Description("Restore and save the configuration")]
	RestoreAndSave
}
