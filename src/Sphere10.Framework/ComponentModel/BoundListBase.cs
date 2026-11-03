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
/// An abstract class used for data binding. This class can be used as a base implementation of the IBoundList interface.
/// You can use the concreate classes BoundList or BoundDataView or a custom class.
/// To implement you own bound list class simply derive from this class and implement the abstract methods.
/// </summary>
[Serializable]
public abstract class BoundListBase<T> : IBoundList {
	public event System.ComponentModel.ListChangedEventHandler ListChanged;

	public event EventHandler ListCleared;

	/// <summary>Reports deleted items with the collection's declared item type.</summary>
	public event EventHandler<ItemDeletedEventArgs<T>> ItemDeleted;

	private event EventHandler<ItemDeletedEventArgs<object>> _bindingItemDeleted;

	// Native binding controls consume heterogeneous items through the existing nongeneric interface.
	event EventHandler<ItemDeletedEventArgs<object>> IBoundList.ItemDeleted {
		add {
			_bindingItemDeleted += value;
		}
		remove {
			_bindingItemDeleted -= value;
		}
	}

	private int _editIndex;
	private T _editItem;
	private bool _adding = false;
	private Dictionary<PropertyDescriptor, object> _previousValues = new Dictionary<PropertyDescriptor, object>();
	private bool _allowEdit = false;
	private bool _allowNew = false;
	private bool _allowDelete = false;
	private bool _allowSort = false;
	private List<T> _addedItems = new List<T>();
	private List<T> _removedItems = new List<T>();
	private List<T> _editedItems = new List<T>();

	/// <summary>
	/// Gets the current edited object
	/// </summary>
	public object EditedObject {
		get {
			return _editItem;
		}
	}

	public bool AllowEdit {
		get {
			return _allowEdit;
		}
		set {
			_allowEdit = value;
		}
	}

	public bool AllowNew {
		get {
			return _allowNew;
		}
		set {
			_allowNew = value;
		}
	}

	public bool AllowDelete {
		get {
			return _allowDelete;
		}
		set {
			_allowDelete = value;
		}
	}

	/// <summary>
	/// Gets or sets if the sort is enabled. Usually is enabled only if the IList is an instance of List class
	/// </summary>
	public bool AllowSort {
		get {
			return _allowSort;
		}
		set {
			_allowSort = value;
		}
	}

	public List<T> AddedItems {
		get {
			return _addedItems;
		}
	}

	public List<T> RemovedItems {
		get {
			return _removedItems;
		}
	}

	public List<T> EditedItems {
		get {
			return _editedItems;
		}
	}

	/// <summary>
	/// Return the item at the specified position.
	/// </summary>
	/// <param name="index"></param>
	/// <returns></returns>
	public abstract object this[int index] { get; }

	/// <summary>
	/// Return the row count of the list
	/// </summary>
	public abstract int Count {
		get;
	}

	public int BeginAddNew() {
		if (_editItem != null)
			throw new ApplicationException("There is already a row in editing state, call EndEdit first");

		// Create the appended item and begin its edit lifecycle.
		_editItem = OnAddNew();
		_editIndex = Count - 1;
		_adding = true;
		OnListChanged(new System.ComponentModel.ListChangedEventArgs(System.ComponentModel.ListChangedType.ItemAdded, _editIndex));

		return _editIndex;
	}

	public void BeginEdit(int index) {
		if (_editItem != null)
			throw new ApplicationException("There is already a row in editing state, call EndEdit first");

		// Retain the item and index for subsequent edit operations.
		_editItem = (T)this[index];
		_editIndex = index;
	}

	public void EndEdit(bool cancel) {
		if (_editItem == null)
			return;

		//Cancel edit
		if (cancel) {
			//Delete added value
			if (_adding)
				RemoveAt(_editIndex);
			else {
				//restore edited values
				foreach (var editedValue in _previousValues)
					editedValue.Key.SetValue(_editItem, editedValue.Value);
			}
		} else {
			//Apply edit
			if (_adding)
				_addedItems.Add(_editItem);
			else {
				//I consider the item edited only if not already edited or if is not a new item
				if (_editedItems.Contains(_editItem) == false && _addedItems.Contains(_editItem) == false)
					_editedItems.Add(_editItem);
			}
		}

		// Release the edit state before notifying the bound controls.
		_editItem = default(T);
		_adding = false;
		_editIndex = -1;
		_previousValues.Clear();
		OnListChanged(new System.ComponentModel.ListChangedEventArgs(System.ComponentModel.ListChangedType.Reset, -1));
	}

	public void RemoveAt(int index) {
		// Remove the item from the underlying collection.
		var item = (T)this[index];
		OnRemoveAt(index);

		// Update the pending changes without reporting a new item as deleted.
		if (_addedItems.Contains(item))
			_addedItems.Remove(item);
		else {
			if (_editedItems.Contains(item))
				_editedItems.Remove(item);

			_removedItems.Add(item);
		}

		// Notify consumers after the collection and change tracking agree.
		OnItemDeleted(new ItemDeletedEventArgs<T>(item));
		OnListChanged(new System.ComponentModel.ListChangedEventArgs(System.ComponentModel.ListChangedType.ItemDeleted, index));
	}

	/// <summary>
	/// Clear all list items
	/// </summary>
	public void Clear() {
		// Clear the retained change tracking and active edit state.
		_editedItems.Clear();
		_addedItems.Clear();
		_editedItems.Clear();
		_editItem = default(T);
		_adding = false;
		_editIndex = -1;
		_previousValues.Clear();

		// Refresh controls after the reset has completed.
		OnListCleared(EventArgs.Empty);
		OnListChanged(new System.ComponentModel.ListChangedEventArgs(System.ComponentModel.ListChangedType.Reset, -1));
	}

	public object GetItemValue(int index, System.ComponentModel.PropertyDescriptor property) {
		return property.GetValue(this[index]);
	}

	public void SetEditValue(System.ComponentModel.PropertyDescriptor property, object value) {
		if (_editItem == null)
			throw new ApplicationException("There isn't a row in editing state, call BeginAddNew or BeginEdit first");

		//Save the previous value to enable the restore if the user cancel the editing
		if (_previousValues.ContainsKey(property) == false)
			_previousValues.Add(property, property.GetValue(_editItem));

		// Apply the change and refresh the affected bound property.
		property.SetValue(_editItem, value);
		OnListChanged(new System.ComponentModel.ListChangedEventArgs(System.ComponentModel.ListChangedType.ItemChanged, _editIndex, property));
	}

	/// <summary>
	/// Get an item property by name
	/// </summary>
	/// <returns></returns>
	public System.ComponentModel.PropertyDescriptor GetItemProperty(string name, StringComparison comparison) {
		foreach (System.ComponentModel.PropertyDescriptor prop in GetItemProperties()) {
			if (prop.Name.Equals(name, comparison))
				return prop;
		}

		return null;
	}

	public System.ComponentModel.PropertyDescriptorCollection GetItemProperties() {
		return System.ComponentModel.TypeDescriptor.GetProperties(typeof(T));
	}

	/// <summary>
	/// Return the index of the specified item
	/// </summary>
	/// <param name="item"></param>
	/// <returns></returns>
	public abstract int IndexOf(object item);

	/// <summary>
	/// Sort the list
	/// </summary>
	/// <param name="sorts"></param>
	public abstract void ApplySort(System.ComponentModel.ListSortDescriptionCollection sorts);

	protected virtual void OnListChanged(System.ComponentModel.ListChangedEventArgs args) {
		if (ListChanged != null)
			ListChanged(this, args);
	}

	protected virtual void OnListCleared(EventArgs args) {
		if (ListCleared != null)
			ListCleared(this, args);
	}

	/// <summary>Notifies typed subscribers before forwarding the current item to native binding subscribers.</summary>
	protected virtual void OnItemDeleted(ItemDeletedEventArgs<T> args) {
		ItemDeleted?.Invoke(this, args);
		if (_bindingItemDeleted == null)
			return;

		// Preserve valid item replacements across the typed and heterogeneous notification channels.
		var bindingArgs = new ItemDeletedEventArgs<object>(args.Item);
		_bindingItemDeleted(this, bindingArgs);
		args.Item = (T)bindingArgs.Item;
	}

	// Abstract methods
	/// <summary>
	/// Create a new item (row) add it at the end of the list and return the new item.
	/// </summary>
	/// <returns></returns>
	protected abstract T OnAddNew();

	/// <summary>
	/// Remove the item at the specified position.
	/// </summary>
	/// <param name="index"></param>
	protected abstract void OnRemoveAt(int index);

	/// <summary>
	/// Remove all items.
	/// </summary>
	protected abstract void OnClear();
}
