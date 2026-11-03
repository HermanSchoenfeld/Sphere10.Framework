// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>Controls which acceptance keys close an editor dropdown.</summary>
[Flags]
public enum DropDownFlags {
	None = 0,

	/// <summary>
	/// Close the DropDownForm whe the user press the escape key, return DialogResult.Cancel
	/// </summary>
	CloseOnEscape = 1,

	/// <summary>
	/// Close the DropDownForm whe the user press the enter key, return DialogResult.OK
	/// </summary>
	CloseOnEnter = 2
}
