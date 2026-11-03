// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Sphere10.Framework.ComponentModel;

/// <summary>
/// A class derived from BoundListBase that can be used to bind a list control (like SourceGrid) to a generic IList class.
/// If the IList is an instance of List class then also the Sort is supported.
/// Implement the IBoundList interface used for data binding.
/// </summary>
/// <typeparam name="T"></typeparam>
[Serializable]
public class BoundList<T> : BoundListBase<T> {
	private IList<T> _list;

	/// <summary>Creates a binding wrapper over the supplied list and its supported editing operations.</summary>
	public BoundList(IList<T> list) {
		_list = list;

		AllowNew = true;
		AllowDelete = true;
		AllowEdit = true;
		AllowSort = _list is List<T>;
	}

	/// <summary>Returns the item at the requested list index.</summary>
	public override object this[int index] {
		get {
			return _list[index];
		}
	}

	/// <summary>Gets the current number of items in the wrapped list.</summary>
	public override int Count {
		get {
			return _list.Count;
		}
	}

	/// <summary>Locates an item in the wrapped list.</summary>
	public override int IndexOf(object item) {
		return _list.IndexOf((T)item);
	}

	/// <summary>Sorts by the configured descriptors and raises one binding reset notification.</summary>
	public override void ApplySort(ListSortDescriptionCollection sorts) {
		var sortableList = _list as List<T>;
		if (sortableList == null)
			throw new ApplicationException("Sort not supported, the list must be an instance of List<T>.");

		// Compare each descriptor in declaration order, continuing to the next key when both values are null.
		var comparers = new List<IComparer<T>>(sorts.Count);
		foreach (ListSortDescription sort in sorts) {
			IComparer<T> comparer = new ProjectionComparer<T, IComparable>(item => sort.PropertyDescriptor.GetValue(item) as IComparable);

			// Swap the compared objects if the sort direction is Descending.
			if (sort.SortDirection == ListSortDirection.Descending)
				comparer = new InvertedComparer<T>(comparer);
			comparers.Add(comparer);
		}
		sortableList.Sort(comparers.Count == 0 ? IdempotentComparer<T>.Instance : CompositeComparer<T>.From(comparers));

		// Notify bound controls after the complete descriptor ordering has been applied.
		OnListChanged(new ListChangedEventArgs(ListChangedType.Reset, -1));
	}

	/// <summary>Creates a default item through the Framework activator and appends it to the list.</summary>
	protected override T OnAddNew() {
		var editItem = (T)Tools.Object.Create(typeof(T));
		_list.Add(editItem);
		return editItem;
	}

	/// <summary>Removes the requested item from the wrapped list.</summary>
	protected override void OnRemoveAt(int index) {
		_list.RemoveAt(index);
	}

	/// <summary>Removes every item from the wrapped list.</summary>
	protected override void OnClear() {
		_list.Clear();
	}
}
