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

public class IndexRangeEventArgs : EventArgs {
	private int _startIndex;
	private int _count;

	public IndexRangeEventArgs(int startIndex, int count) {
		_startIndex = startIndex;
		_count = count;
	}

	public int StartIndex {
		get {
			return _startIndex;
		}
	}

	public int Count {
		get {
			return _count;
		}
	}
}
