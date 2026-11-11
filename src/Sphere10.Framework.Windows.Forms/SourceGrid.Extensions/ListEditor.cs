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
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms.SourceGrid;

/// <summary>
/// Edits a list through reflected property columns with add, remove, refresh and row-movement commands.
/// </summary>
public partial class ListEditor : UserControlEx {
	/// <summary>Raised after a complete list edit.</summary>
	public event EventHandler ListChanged;

	private IList _list;

	private Type _itemType;

	private Cells.Editors.EditorBase[] _editors;

	private readonly List<Cells.Editors.EditorBase> _ownedEditors = new();

	private System.Reflection.PropertyInfo[] _properties;

	/// <summary>Creates the grid and its list-editing commands.</summary>
	public ListEditor() {
		// This call is required by the Windows.Forms Form Designer.
		InitializeComponent();
	}

	/// <summary>Gets or sets the mutable list, including generic lists exposed through their runtime IList contract.</summary>
	public IList List {
		get => _list;
		set => _list = value;
	}

	/// <summary>Gets the displayed grid for selection, presentation and export configuration.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Grid Grid => _grid;

	/// <summary>Gets or sets the item schema used to create property editors.</summary>
	public Type ItemType {
		get => _itemType;
		set {
			_itemType = value;
			LoadEditors();
		}
	}

	/// <summary>Gets or sets the editors associated with the displayed properties.</summary>
	[Browsable(false)]
	public Cells.Editors.EditorBase[] Editors {
		get => _editors;
		set {
			if (ReferenceEquals(_editors, value))
				return;
			CancelCurrentEdit();
			DetachBorrowedEditors();
			ReleaseOwnedEditors(value);
			_editors = value;
		}
	}

	/// <summary>Gets or sets the reflected properties displayed as columns.</summary>
	[Browsable(false)]
	public System.Reflection.PropertyInfo[] Properties {
		get => _properties;
		set => _properties = value;
	}

	/// <summary>Gets or sets whether the add command is visible.</summary>
	public bool EnableAdd {
		get => _addButton.Visible;
		set => _addButton.Visible = value;
	}

	/// <summary>Gets or sets whether the remove command is visible.</summary>
	public bool EnableRemove {
		get => _removeButton.Visible;
		set => _removeButton.Visible = value;
	}

	/// <summary>Gets or sets whether the refresh command is visible.</summary>
	public bool EnableRefresh {
		get => _refreshButton.Visible;
		set => _refreshButton.Visible = value;
	}

	/// <summary>Gets or sets whether row-movement commands are visible.</summary>
	public bool EnableMove {
		get => _moveUpButton.Visible;
		set {
			_moveUpButton.Visible = value;
			_moveDownButton.Visible = value;
		}
	}

	/// <summary>Rebuilds the grid from the current schema and list membership.</summary>
	public void LoadList() {
		// End any in-place edit before rebuilding the list's cells.
		using var update = EnterUpdateScope(FinishedUpdateBehaviour.DoNothing);
		CancelCurrentEdit();
		if (_itemType == null)
			throw new ApplicationException("ItemType is null");
		if (_list == null)
			_list = new List<object>();

		if (_properties.Length != _editors.Length)
			throw new ApplicationException("Properteis.Length != Editors.Length");

		// Size the grid from the reflected schema and current list membership.
		_grid.FixedRows = 1;
		_grid.FixedColumns = 0;
		_grid.Redim(_list.Count + _grid.FixedRows, _properties.Length + _grid.FixedColumns);

		//HeaderCell
		//grid[0,0] = new Cells.Header();
		for (var index = 0; index < _properties.Length; index++) {
			var header = new Cells.ColumnHeader(_properties[index].Name);
			_grid[0, index + _grid.FixedColumns] = header;

			header.AutomaticSortEnabled = false;
			//				//If the column type support the IComparable then I can use the value to sort the column, otherwise I use the string representation for sort.
			//				if (typeof(IComparable).IsAssignableFrom(m_Properties[i].PropertyType))
			//					l_Header.Comparer = new ValueCellComparer();
			//				else
			//					l_Header.Comparer = new DisplayStringCellComparer();
		}

		// Bind each list item before sizing the completed grid.
		for (var r = 0; r < _list.Count; r++)
			PopulateRow(r + _grid.FixedRows, _list[r]);

		_grid.AutoStretchColumnsToFitWidth = true;
		_grid.AutoSizeCells();
		UpdateCommandState(_grid.Selection.ActivePosition.Row);
	}

	/// <summary>Reports complete list edits and row selection rather than intermediate child-grid notifications.</summary>
	protected override bool ShouldObserveChildStateChanges(Control child) => false;

	/// <summary>
	/// Clean up any resources being used.
	/// </summary>
	protected override void Dispose(bool disposing) {
		if (disposing) {
			_grid.Selection.FocusRowLeaving -= SelectionFocusRowLeaving;
			_grid.Selection.FocusRowEntered -= SelectionFocusRowEntered;
			CancelCurrentEdit();
			DetachBorrowedEditors();
			ReleaseOwnedEditors();
			if (_components != null)
				_components.Dispose();
		}
		base.Dispose(disposing);
	}

	/// <summary>Notifies list subscribers and the containing control's state observers.</summary>
	protected virtual void OnListChanged(EventArgs args) {
		if (ListChanged != null)
			ListChanged(this, args);
		NotifyStateChanged();
	}

	// Creates a schema item through the shared activator and appends its row.
	private async void AddClicked(object sender, System.EventArgs args) {
		try {
			using var update = EnterUpdateScope(FinishedUpdateBehaviour.NotifyStateChanged);
			var row = _grid.RowsCount;
			_grid.Rows.Insert(row);
			var item = Tools.Object.Create(_itemType);
			_list.Add(item);
			PopulateRow(row, item);
			UpdateCommandState(_grid.Selection.ActivePosition.Row);

			OnListChanged(EventArgs.Empty);
		} catch (Exception exception) {
			await ExceptionDialog.ShowAsync(this, "Error", exception);
		}
	}

	// Removes the selected data row and its underlying item.
	private async void RemoveClicked(object sender, System.EventArgs args) {
		try {
			using var update = EnterUpdateScope(FinishedUpdateBehaviour.NotifyStateChanged);
			if (_grid.Selection.ActivePosition.IsEmpty() == false && _grid.Selection.ActivePosition.Row >= _grid.FixedRows) {
				_list.Remove(_grid.Rows[_grid.Selection.ActivePosition.Row].Tag);
				_grid.Rows.Remove(_grid.Selection.ActivePosition.Row);
				UpdateCommandState(_grid.Selection.ActivePosition.Row);

				OnListChanged(EventArgs.Empty);
			}
		} catch (Exception exception) {
			await ExceptionDialog.ShowAsync(this, "Error", exception);
		}
	}

	// Reloads the grid and presents any data-source failure.
	private async void RefreshListClicked(object sender, System.EventArgs args) {
		try {
			LoadList();
		} catch (Exception exception) {
			await ExceptionDialog.ShowAsync(this, "Error", exception);
		}
	}

	// Moves the selected item and its grid row one position upward.
	private async void MoveUpClicked(object sender, System.EventArgs args) {
		try {
			using var update = EnterUpdateScope(FinishedUpdateBehaviour.NotifyStateChanged);
			if (_grid.Selection.ActivePosition.IsEmpty() == false && _grid.Selection.ActivePosition.Row > _grid.FixedRows) {
				var tag = _grid.Rows[_grid.Selection.ActivePosition.Row].Tag;
				var gridRow = _grid.Selection.ActivePosition.Row;
				var listIndex = _list.IndexOf(tag);
				_list.Remove(tag);
				_list.Insert(listIndex - 1, tag);
				_grid.Rows.Move(gridRow, gridRow - 1);

				_grid.Selection.FocusRow(gridRow - 1);
				UpdateCommandState(_grid.Selection.ActivePosition.Row);

				OnListChanged(EventArgs.Empty);
			}
		} catch (Exception exception) {
			await ExceptionDialog.ShowAsync(this, "Error", exception);
		}
	}

	// Moves the selected item and its grid row one position downward.
	private async void MoveDownClicked(object sender, System.EventArgs args) {
		try {
			using var update = EnterUpdateScope(FinishedUpdateBehaviour.NotifyStateChanged);
			if (_grid.Selection.ActivePosition.IsEmpty() == false &&
				_grid.Selection.ActivePosition.Row >= _grid.FixedRows &&
				_grid.Selection.ActivePosition.Row < _grid.Rows.Count - 1) {
				var tag = _grid.Rows[_grid.Selection.ActivePosition.Row].Tag;
				var gridRow = _grid.Selection.ActivePosition.Row;
				var listIndex = _list.IndexOf(tag);
				_list.Remove(tag);
				_list.Insert(listIndex + 1, tag);
				_grid.Rows.Move(gridRow, gridRow + 1);

				_grid.Selection.FocusRow(gridRow + 1);
				UpdateCommandState(_grid.Selection.ActivePosition.Row);

				OnListChanged(EventArgs.Empty);
			}
		} catch (Exception exception) {
			await ExceptionDialog.ShowAsync(this, "Error", exception);
		}
	}

	// Reports completed cell edits through the list-level notification.
	private void GridValueChanged(object sender, EventArgs args) => OnListChanged(args);

	// Disables row commands while focus leaves the current row.
	private void SelectionFocusRowLeaving(object sender, RowCancelEventArgs args) => UpdateCommandState(-1);

	// Updates commands and state observers for the newly focused row.
	private void SelectionFocusRowEntered(object sender, RowEventArgs args) {
		UpdateCommandState(args.Row);
		NotifyStateChanged();
	}

	// Creates owned editors for the browsable properties of the configured schema.
	private void LoadEditors() {
		CancelCurrentEdit();
		DetachBorrowedEditors();
		ReleaseOwnedEditors();
		if (_itemType != null) {
			_properties = _itemType.GetProperties();

			var browsableProperties = new List<PropertyInfo>();
			//remove not Browsable attribute
			for (var index = 0; index < _properties.Length; index++) {
				var attributes = TypeDescriptor.GetProperties(_itemType)[_properties[index].Name].Attributes;

				// Checks to see if the value of the BrowsableAttribute is Yes.
				if (attributes[typeof(BrowsableAttribute)].Equals(BrowsableAttribute.Yes))
					browsableProperties.Add(_properties[index]);
			}

			_editors = new Cells.Editors.EditorBase[browsableProperties.Count];
			_properties = browsableProperties.ToArray();

			for (var index = 0; index < _properties.Length; index++) {
				var property = _properties[index];
				_editors[index] = Cells.Editors.Factory.Create(property.PropertyType);
				if (_editors[index] != null) {
					_ownedEditors.Add(_editors[index]);
					_editors[index].EnableEdit = property.CanWrite;
				}
			}
		} else
			_editors = null;
	}

	// Binds an item's properties to one grid row.
	private void PopulateRow(int gridRow, object item) {
		for (var c = 0; c < _properties.Length; c++)
			PopulateCell(gridRow, c + _grid.FixedColumns, _properties[c], _editors[c], item);

		_grid.Rows[gridRow].Tag = item;
	}

	// Connects the property value, editor, binding controller and change notification.
	private void PopulateCell(int gridRow, int gridColumn, System.Reflection.PropertyInfo propertyInfo, Cells.Editors.EditorBase editor, object item) {
		_grid[gridRow, gridColumn] = new Cells.Cell(propertyInfo.GetValue(item, null));
		_grid[gridRow, gridColumn].Editor = editor;

		var bind = new Cells.Controllers.BindProperty(propertyInfo, item);
		_grid[gridRow, gridColumn].AddController(bind);

		var customEvents = new Cells.Controllers.CustomEvents();
		customEvents.ValueChanged += new EventHandler(GridValueChanged);
		_grid[gridRow, gridColumn].AddController(customEvents);
	}

	/// <summary>Cancels this grid's current edits before rebuilding or releasing its editors.</summary>
	private void CancelCurrentEdit() {
		if (_editors == null)
			return;
		foreach (var editor in new HashSet<Cells.Editors.EditorBase>(_editors, ReferenceEqualityComparer.Instance))
			if (editor != null && editor.IsEditing && ReferenceEquals(editor.EditCellContext.Grid, _grid))
				editor.EditCellContext.EndEdit(true);
	}

	/// <summary>Detaches borrowed editing surfaces so disposing the grid cannot dispose their controls.</summary>
	private void DetachBorrowedEditors() {
		if (_editors == null)
			return;
		foreach (var editor in new HashSet<Cells.Editors.EditorBase>(_editors, ReferenceEqualityComparer.Instance))
			if (editor is Cells.Editors.EditorControlBase controlEditor && ReferenceEquals(controlEditor.Grid, _grid) && !_ownedEditors.Exists(owned => ReferenceEquals(owned, editor)))
				controlEditor.DetachControl();
	}

	/// <summary>Disposes only factory-created editors that are absent from a replacement array.</summary>
	private void ReleaseOwnedEditors(Cells.Editors.EditorBase[] retained = null) {
		for (var index = _ownedEditors.Count - 1; index >= 0; index--) {
			var editor = _ownedEditors[index];
			if (retained != null && Array.Exists(retained, item => ReferenceEquals(item, editor)))
				continue;
			_ownedEditors.RemoveAt(index);
			editor.Dispose();
		}
	}

	/// <summary>Reflects first/last-row movement boundaries and the presence of a removable data row.</summary>
	private void UpdateCommandState(int gridRow) {
		var hasDataRow = gridRow >= _grid.FixedRows && gridRow < _grid.Rows.Count;
		_removeButton.Enabled = hasDataRow;
		_moveUpButton.Enabled = hasDataRow && gridRow > _grid.FixedRows;
		_moveDownButton.Enabled = hasDataRow && gridRow < _grid.Rows.Count - 1;
	}
}
