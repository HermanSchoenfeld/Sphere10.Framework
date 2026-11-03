// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.ComponentModel;

/// <summary>
/// Event arguments for item deletion event.
/// </summary>
/// <typeparam name="T">The type of the deleted item.</typeparam>
public class ItemDeletedEventArgs<T> : EventArgs {
	/// <summary>Creates a deletion notification containing the removed item.</summary>
	public ItemDeletedEventArgs(T item) {
		Item = item;
	}

	/// <summary>Gets or replaces the item passed to subsequent deletion handlers.</summary>
	public T Item { get; set; }
}
