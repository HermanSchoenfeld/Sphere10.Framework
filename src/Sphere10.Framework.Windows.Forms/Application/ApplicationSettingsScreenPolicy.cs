// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Controls the reset targets and pending-reset application behavior of the native Options dialog.</summary>
[Flags]
public enum ApplicationSettingsScreenPolicy {

	/// <summary>Resets only the selected editable page and applies only the selected page.</summary>
	None = 0,

	/// <summary>Includes descendants of the selected Options node when resetting defaults.</summary>
	ResetDescendants = 1,

	/// <summary>Includes settings without an automatic or explicit editor in the chosen reset scope.</summary>
	IncludeNonEditableSettings = 2,

	/// <summary>Apply saves every pending reset across navigation; OK always saves pending resets.</summary>
	ApplyPendingResetScope = 4,

	/// <summary>Resets the full selected subtree and includes pending resets when applying.</summary>
	Default = ResetDescendants | IncludeNonEditableSettings | ApplyPendingResetScope
}
