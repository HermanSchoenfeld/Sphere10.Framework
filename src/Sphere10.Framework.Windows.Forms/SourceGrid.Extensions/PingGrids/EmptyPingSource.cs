// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Extensions.PingGrids;

/// <summary>Provides the empty data source used before a PingGrid receives items.</summary>
public class EmptyPingSource : IPingData {
	/// <summary>Gets the number of items in this empty source.</summary>
	public int Count => 0;

	/// <summary>Empty sources do not offer sorting.</summary>
	public bool AllowSort {
		get {
			return false;
		}
		set {
		}
	}

	/// <summary>Rejects reads because this source has no valid item index.</summary>
	public object GetItemValue(int index, string propertyName) {
		throw new ArgumentOutOfRangeException(nameof(index));
	}

	/// <summary>Sorting an empty source leaves it empty.</summary>
	public void ApplySort(string propertyName, bool ascending) {
	}
}
