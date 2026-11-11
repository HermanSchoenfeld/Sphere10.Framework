// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms.SourceGrid;

/// <summary>
/// EventArgs used by the FocusColumnEnter
/// </summary>
public class ColumnEventArgs : EventArgs {
	private int _column;

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="column"></param>
	public ColumnEventArgs(int column) => _column = column;

	/// <summary>
	/// Column
	/// </summary>
	public int Column {
		get => _column;
		set => _column = value;
	}
}
