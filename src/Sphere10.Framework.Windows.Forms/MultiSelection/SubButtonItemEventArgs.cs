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

/// <summary>Identifies the alternative action selected on a composite selection button.</summary>
public class SubButtonItemEventArgs : EventArgs {
	/// <summary>Creates the notification for the selected alternative action.</summary>
	public SubButtonItemEventArgs(SubButtonItem item) {
		ButtonItem = item;
	}

	/// <summary>Gets or sets the selected item, or null when the button has no alternative items.</summary>
	public SubButtonItem ButtonItem {
		get; set;
	}
}
