// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Extensions.PingGrids;

/// <summary>Adapts a list to PingGrid's read and property-sort contract.</summary>
public class ListPingSource<T> : List<T>, IPingData {
	/// <summary>Gets or sets whether the grid may reorder the supplied items.</summary>
	public bool AllowSort { get; set; } = true;

	/// <summary>Sorts by the requested property using the framework's projection comparer.</summary>
	public void ApplySort(string propertyName, bool ascending) {
		Guard.ArgumentNotNullOrEmpty(propertyName, nameof(propertyName));
		if (!AllowSort)
			return;

		IComparer<T> comparer = new ProjectionComparer<T, object>(item => ReflectionPropertyResolver.SharedInstance.ReadValue(item, propertyName));
		if (!ascending)
			comparer = new InvertedComparer<T>(comparer);
		Sort(comparer);
	}

	/// <summary>Reads a property through the existing native-grid resolver.</summary>
	public object GetItemValue(int index, string propertyName) {
		Guard.ArgumentNotNullOrEmpty(propertyName, nameof(propertyName));
		return ReflectionPropertyResolver.SharedInstance.ReadValue(this[index], propertyName);
	}
}
