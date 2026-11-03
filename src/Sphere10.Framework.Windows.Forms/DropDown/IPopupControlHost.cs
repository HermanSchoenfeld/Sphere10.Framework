// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Drawing;
using System.Security.Permissions;
using System.Windows.Forms;
using static Sphere10.Framework.Windows.WinAPI.USER32;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Provides the show and hide operations for a control that owns a popup.</summary>
public interface IPopupControlHost {

	// Methods
	/// <summary>
	/// Displays drop-down area of combo box, if not already shown.
	/// </summary>
	void ShowDropDown();

	/// <summary>
	/// Hides drop-down area of combo box, if shown.
	/// </summary>
	void HideDropDown();
}
