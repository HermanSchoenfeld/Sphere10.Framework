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
/// A generic binded list interface. See BoundDataView or BoundList for a concreate implementation.
/// It is used as an abstraction layer for list objects (typically a list can be a DataView or a IList class)
/// Can be used to bind a list control (like SourceGrid).
/// </summary>
public interface IBoundList {
	event System.ComponentModel.ListChangedEventHandler ListChanged;

	event EventHandler ListCleared;

	/// <summary>Reports deleted items at the heterogeneous native binding boundary.</summary>
	event EventHandler<ItemDeletedEventArgs<object>> ItemDeleted;

	bool AllowDelete {
		get; set;
	}

	bool AllowEdit {
		get; set;
	}

	bool AllowNew {
		get; set;
	}

	bool AllowSort {
		get; set;
	}

	int Count {
		get;
	}

	object EditedObject {
		get;
	}

	object this[int index] { get; }

	void ApplySort(System.ComponentModel.ListSortDescriptionCollection sorts);

	int BeginAddNew();

	void BeginEdit(int index);

	void EndEdit(bool cancel);

	System.ComponentModel.PropertyDescriptorCollection GetItemProperties();

	System.ComponentModel.PropertyDescriptor GetItemProperty(string name, StringComparison comparison);

	object GetItemValue(int index, System.ComponentModel.PropertyDescriptor property);

	int IndexOf(object item);

	void RemoveAt(int index);

	void SetEditValue(System.ComponentModel.PropertyDescriptor property, object value);
}
