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

public class SelectionChangeEventArgs : EventArgs {
	private CellRange _range;
	private SelectionChangeEventType _type;

	public SelectionChangeEventArgs(SelectionChangeEventType type, CellRange range) {
		_type = type;
		_range = range;
	}

	public CellRange Range {
		get {
			return _range;
		}
	}

	public SelectionChangeEventType EventType {
		get {
			return _type;
		}
	}
}
