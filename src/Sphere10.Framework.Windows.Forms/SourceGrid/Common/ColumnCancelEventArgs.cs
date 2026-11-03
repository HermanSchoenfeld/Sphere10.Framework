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
/// EventArgs used by the FocusColumnLeaving
/// </summary>
public class ColumnCancelEventArgs : ColumnEventArgs {
	private bool _cancel;
	private int _proposedColumn;

	/// <summary>
	/// Constructor
	/// </summary>
	public ColumnCancelEventArgs(int currentFocusedColumn, int proposedFocusedColumn)
		: base(currentFocusedColumn) {
		_proposedColumn = proposedFocusedColumn;
	}

	/// <summary>
	/// Column
	/// </summary>
	public bool Cancel {
		get {
			return _cancel;
		}
		set {
			_cancel = value;
		}
	}

	/// <summary>
	/// Proposed column
	/// </summary>
	public int ProposedColumn {
		get {
			return _proposedColumn;
		}
	}
}
