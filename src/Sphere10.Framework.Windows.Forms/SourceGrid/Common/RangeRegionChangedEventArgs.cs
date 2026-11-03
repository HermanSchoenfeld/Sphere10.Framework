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
/// Represents the event arguments used when changing a RangeRegion class (like the selection class).
/// </summary>
public class RangeRegionChangedEventArgs : EventArgs {

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="addedRange"></param>
	/// <param name="removedRange"></param>
	public RangeRegionChangedEventArgs(CellRange addedRange, CellRange removedRange) {
		if (addedRange.IsEmpty() == false)
			AddedRange = new RangeRegion(addedRange);
		if (removedRange.IsEmpty() == false)
			RemovedRange = new RangeRegion(removedRange);
	}

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="addedRange">Use null if the added range is empty</param>
	/// <param name="removedRange">Use null if the removed range is empty</param>
	public RangeRegionChangedEventArgs(RangeRegion addedRange, RangeRegion removedRange) {
		AddedRange = addedRange;
		RemovedRange = removedRange;
	}

	/// <summary>
	/// Null if the added range is empty
	/// </summary>
	public RangeRegion AddedRange { get; }

	/// <summary>
	/// Null if the removed range is empty
	/// </summary>
	public RangeRegion RemovedRange { get; }
}
