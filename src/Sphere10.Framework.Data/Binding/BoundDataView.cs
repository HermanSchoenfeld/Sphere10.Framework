// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using Sphere10.Framework.ComponentModel;

namespace Sphere10.Framework.Data;

/// <summary>
/// A class to support list binding to a DataView object.
/// Implement the IBoundList.
/// </summary>
[Serializable]
public class BoundDataView : SyncDisposable, IBoundList {
	public event System.ComponentModel.ListChangedEventHandler ListChanged;

	public event EventHandler ListCleared;

	/// <summary>Reports the deleted data row rather than its transient DataRowView wrapper.</summary>
	public event EventHandler<ItemDeletedEventArgs<System.Data.DataRow>> ItemDeleted;

	private event EventHandler<ItemDeletedEventArgs<object>> _bindingItemDeleted;

	// Native binding controls retain their heterogeneous notification boundary.
	event EventHandler<ItemDeletedEventArgs<object>> IBoundList.ItemDeleted {
		add {
			_bindingItemDeleted += value;
		}
		remove {
			_bindingItemDeleted -= value;
		}
	}

	private readonly System.Data.DataView _dataView;
	private readonly System.Data.DataTable _dataTable;
	private System.Data.DataRowView _editingRow;
	private bool _allowEdit = true;
	private bool _allowNew = true;
	private bool _allowDelete = true;
	private bool _allowSort = true;

	public BoundDataView(System.Data.DataView dataView) {
		_dataView = dataView;

		_dataView.ListChanged += new System.ComponentModel.ListChangedEventHandler(DataViewListChanged);
		// Save it for destructor
		_dataTable = _dataView.Table;
		if (_dataTable != null) {
			_dataTable.TableCleared += new System.Data.DataTableClearEventHandler(TableTableCleared);
			_dataTable.RowDeleted += new System.Data.DataRowChangeEventHandler(TableRowDeleted);
		}
	}

	public System.Data.DataView DataView {
		get {
			return _dataView;
		}
	}

	public System.Data.DataTable DataTable {
		get {
			return _dataTable;
		}
	}

	/// <summary>
	/// Gets the current edited object
	/// </summary>
	public virtual object EditedObject {
		get {
			return _editingRow;
		}
	}

	public virtual object this[int index] {
		get {
			if (index > _dataView.Table.Rows.Count)
				throw new ArgumentException(string.Format(
					"Data table does not have row with given index. It has only {0} number of rows," +
					"you requested to return row number {1}",
					_dataView.Table.Rows.Count,
					index));
			try {
				return _dataView[index];
			} catch (InvalidOperationException) {
				// we sometimes get this error. Don't know why. Maybe it is multithreading issue
				// It says that internal index is corrupt, number 13.
				// Return null in this case, works fine afaik
				return null;
			}
		}
	}

	public virtual int Count {
		get {
			return _dataView.Count;
		}
	}

	public virtual bool AllowEdit {
		get {
			return _dataView.AllowEdit && _allowEdit;
		}
		set {
			_allowEdit = value;
		}
	}

	public virtual bool AllowNew {
		get {
			return _dataView.AllowNew && _allowNew;
		}
		set {
			_allowNew = value;
		}
	}

	public virtual bool AllowDelete {
		get {
			return _dataView.AllowDelete && _allowDelete;
		}
		set {
			_allowDelete = value;
		}
	}

	public virtual bool AllowSort {
		get {
			return _allowSort;
		}
		set {
			_allowSort = value;
		}
	}

	public virtual int BeginAddNew() {
		if (_editingRow != null)
			throw new ApplicationException("There is already a row in editing state, call EndEdit first");

		_editingRow = _dataView.AddNew();

		_editingRow.BeginEdit();

		var list = (System.Collections.IList)_dataView;

		return list.IndexOf(_editingRow);
	}

	public virtual void BeginEdit(int index) {
		if (_editingRow != null)
			throw new ApplicationException("There is already a row in editing state, call EndEdit first");

		_editingRow = _dataView[index];

		_editingRow.BeginEdit();
	}

	public virtual void EndEdit(bool cancel) {
		if (_editingRow == null)
			return;

		if (cancel)
			_editingRow.CancelEdit();
		else
			_editingRow.EndEdit();

		_editingRow = null;

		//when CancelEdit the DataView doesn't automatically call a ListChanged event, so I will call it manually
		if (cancel)
			OnListChanged(new System.ComponentModel.ListChangedEventArgs(System.ComponentModel.ListChangedType.Reset, -1));
	}

	public virtual int IndexOf(object item) {
		var list = (System.Collections.IList)_dataView;

		return list.IndexOf(item);
	}

	public virtual void RemoveAt(int index) {
		_dataView[index].Delete();
	}

	public System.ComponentModel.PropertyDescriptor GetItemProperty(string name, StringComparison comparison) {
		foreach (System.ComponentModel.PropertyDescriptor prop in GetItemProperties()) {
			if (prop.Name.Equals(name, comparison))
				return prop;
		}

		return null;
	}

	public virtual System.ComponentModel.PropertyDescriptorCollection GetItemProperties() {
		//Removed for mono compatibility
		//return System.Windows.Forms.ListBindingHelper.GetListItemProperties(_dataView);

		if (_dataView == null)
			return new System.ComponentModel.PropertyDescriptorCollection(null);
		return ((System.ComponentModel.ITypedList)_dataView).GetItemProperties(null);
	}

	public virtual object GetItemValue(int index, System.ComponentModel.PropertyDescriptor property) {
		var dataVal = property.GetValue(_dataView[index]);

		//Convert DbNull to null
		if (System.DBNull.Value == dataVal)
			return null;
		return dataVal;
	}

	public virtual void SetEditValue(System.ComponentModel.PropertyDescriptor property, object value) {
		//Convert the null value to DbNull
		if (value == null)
			value = System.DBNull.Value;

		if (_editingRow == null)
			throw new ApplicationException("There isn't a row in editing state, call BeginAddNew or BeginEdit first");

		property.SetValue(_editingRow, value);
	}

	public virtual void ApplySort(System.ComponentModel.ListSortDescriptionCollection sorts) {
		var listView = (System.ComponentModel.IBindingListView)_dataView;

		if (sorts != null && sorts.Count > 0)
			listView.ApplySort(sorts);
		else
			listView.RemoveSort();
	}

	// Detach borrowed data sources on disposal; managed event subscriptions do not belong in a finalizer.
	protected override void DisposeInternal() {
		_dataView.ListChanged -= new System.ComponentModel.ListChangedEventHandler(DataViewListChanged);
		if (_dataTable != null) {
			_dataTable.TableCleared -= new System.Data.DataTableClearEventHandler(TableTableCleared);
			_dataTable.RowDeleted -= new System.Data.DataRowChangeEventHandler(TableRowDeleted);
		}
	}

	void DataViewListChanged(object sender, System.ComponentModel.ListChangedEventArgs args) {
		OnListChanged(args);
	}

	protected virtual void OnListChanged(System.ComponentModel.ListChangedEventArgs args) {
		if (ListChanged != null)
			ListChanged(this, args);
	}

	void TableTableCleared(object sender, System.Data.DataTableClearEventArgs args) {
		OnListCleared(EventArgs.Empty);
	}

	protected virtual void OnListCleared(EventArgs args) {
		if (ListCleared != null)
			ListCleared(this, args);
	}

	void TableRowDeleted(object sender, System.Data.DataRowChangeEventArgs args) {
		OnItemDeleted(new ItemDeletedEventArgs<System.Data.DataRow>(args.Row));
	}

	/// <summary>Notifies typed row subscribers before the heterogeneous native binding subscribers.</summary>
	protected virtual void OnItemDeleted(ItemDeletedEventArgs<System.Data.DataRow> args) {
		ItemDeleted?.Invoke(this, args);
		if (_bindingItemDeleted == null)
			return;

		// Preserve valid row replacements for overrides that observe the argument after base dispatch.
		var bindingArgs = new ItemDeletedEventArgs<object>(args.Item);
		_bindingItemDeleted(this, bindingArgs);
		args.Item = (System.Data.DataRow)bindingArgs.Item;
	}
}
