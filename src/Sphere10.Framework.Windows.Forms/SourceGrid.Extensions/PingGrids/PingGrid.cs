// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Threading.Tasks;
using Sphere10.Framework.Windows.Forms;
using Sphere10.Framework.Windows.Forms.SourceGrid.Cells;
using Sphere10.Framework.Windows.Forms.SourceGrid.Selection;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Extensions.PingGrids;

[System.ComponentModel.ToolboxItem(true)]
public class PingGrid : GridVirtual {
	private IPingData _boundList;

	private bool _endEditingRowOnValidate = true;

	private bool _deleteRowsWithDeleteKey = true;

	private bool _cancelEditingWithEscapeKey = true;

	private string _deleteQuestionMessage = "Are you sure to delete all the selected rows?";

	public PingGrid() {
		// Configure row binding, editing and selection for the data source.
		FixedRows = 1;
		FixedColumns = 0;
		Controller.AddController(new PingGridCellController());
		this.DataSource = new EmptyPingSource();
		SelectionMode = GridSelectionMode.Row;
	}

	/// <summary>
	/// Sorting is always enabled
	/// </summary>
	public override bool EnableSort {
		get {
			return true;
		}
		set {
		}
	}

	/// <summary>
	/// Gets or sets the IBoundList used for data binding.
	/// It can be any class that implements the IBoundList interface, usually can be BoundList
	///  (that can be used to bind to a generic List) or BoundDataView (that can be used to bind to a DataView).
	/// </summary>
	public IPingData DataSource {
		get {
			return _boundList;
		}
		set {
			Unbind();
			if (value == null)
				_boundList = new EmptyPingSource();
			else
				_boundList = value;
			if (_boundList != null)
				Bind();
		}
	}

	/// <summary>
	/// Gets the rows information as a PingGridRows object.
	/// </summary>
	public new PingGridRows Rows {
		get {
			return (PingGridRows)base.Rows;
		}
	}

	/// <summary>
	/// Gets the columns informations as a PingGridColumns object.
	/// </summary>
	public new PingGridColumns Columns {
		get {
			return (PingGridColumns)base.Columns;
		}
	}

	/// <summary>
	/// Gets or sets the selected DataRowView.
	/// </summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	[Obsolete]
	public object[] SelectedDataRows {
		get {
			if (_boundList == null)
				return new System.Data.DataRowView[0];

			// Count only selected rows that map to source items.
			var selectedRows = Selection.GetSelectionRegion().GetRowsIndex();
			var count = 0;
			for (var index = 0; index < selectedRows.Length; index++) {
				var row = Rows.IndexToDataSourceRow(selectedRows[index]);
				if (row != null)
					count++;
			}

			// Materialize the selected items without a trailing placeholder row.
			var dataRows = new object[count];
			var rowIndex = 0;
			for (var i = 0; i < selectedRows.Length; i++) {
				var row = Rows.IndexToDataSourceRow(selectedRows[i]);
				if (row != null) {
					dataRows[rowIndex] = row;
					rowIndex++;
				}
			}
			return dataRows;
		}
		set {
			Selection.ResetSelection(false);

			if (_boundList != null && value != null) {
				for (var index = 0; index < value.Length; index++) {
					for (var r = FixedRows; r < Rows.Count; r++) {
						var row = Rows.IndexToDataSourceRow(r);

						if (object.ReferenceEquals(row, value[index])) {
							Selection.SelectRow(r, true);
							break;
						}
					}
				}
			}
		}
	}

	/// <summary>
	/// Gets or sets a property to force an End Editing when the control loose the focus
	/// </summary>
	[System.ComponentModel.DefaultValue(true)]
	[Obsolete]
	public bool EndEditingRowOnValidate {
		get {
			return _endEditingRowOnValidate;
		}
		set {
			_endEditingRowOnValidate = value;
		}
	}

	/// <summary>
	/// Gets or sets if enable the delete of the selected rows when pressing Delete key.
	/// </summary>
	[System.ComponentModel.DefaultValue(true)]
	public bool DeleteRowsWithDeleteKey {
		get {
			return _deleteRowsWithDeleteKey;
		}
		set {
			_deleteRowsWithDeleteKey = value;
		}
	}

	/// <summary>
	/// Gets or sets if enable the Cancel Editing feature when pressing escape key
	/// </summary>
	[System.ComponentModel.DefaultValue(true)]
	public bool CancelEditingWithEscapeKey {
		get {
			return _cancelEditingWithEscapeKey;
		}
		set {
			_cancelEditingWithEscapeKey = value;
		}
	}

	/// <summary>
	/// Message showed with the DeleteSelectedRowsAsync method. Set to null to not show any message.
	/// </summary>
	public string DeleteQuestionMessage {
		get {
			return _deleteQuestionMessage;
		}
		set {
			_deleteQuestionMessage = value;
		}
	}

	/// <summary>
	/// Gets a specified Cell by its row and column.
	/// </summary>
	/// <param name="row"></param>
	/// <param name="column"></param>
	/// <returns></returns>
	public override ICellVirtual GetCell(int row, int column) {
		if (_boundList == null)
			return null;
		if (column >= Columns.Count)
			return null;

		if (row < FixedRows)
			return Columns[column].HeaderCell;
		return Columns[column].GetDataCell(row);
	}

	/// <summary>
	/// AutoSize the columns based on the visible range and autosize the rows based on it's contents.
	/// </summary>
	public override void AutoSizeCells() {
		Columns.AutoSizeView();
		for (var index = 0; index < Rows.Count; index++)
			Rows.AutoSizeRow(index);
	}

	/// <summary>
	/// Check if the specified row is the active row (focused), return false if it is not the active row. Then call the BeginEdit on the associated DataRowView. Add a row to the DataView if required. Returns true if the method sucesfully call the BeginEdit and set the EditingRow property.
	/// </summary>
	/// <param name="gridRow"></param>
	/// <returns></returns>
	[Obsolete]
	public bool BeginEditRow(int gridRow) {
		/*
		if (mEditingRow != null && mEditingRow.Value == gridRow)
			return true;

		EndEditingRow(false); //Terminate the old edit if present

		if (DataSource != null)
		{
			int dataIndex = Rows.IndexToDataSourceIndex(gridRow);

			// add this here to check if we have permission for edition
			if (!DataSource.AllowEdit)
				return false;

			if (dataIndex == DataSource.Count && DataSource.AllowNew) //Last Row
			{
				DataSource.BeginAddNew();
			}
			else if (dataIndex < DataSource.Count)
			{
				DataSource.BeginEdit(dataIndex);
			}
		}

		mEditingRow = gridRow;
		 */
		return true;
	}

	/// <summary>
	/// Calls the CancelEdit or the EndEdit on the editing Row and set to null the editing row.
	/// </summary>
	/// <param name="cancel"></param>
	[Obsolete]
	public void EndEditingRow(bool cancel) {
		//if (mBoundList != null)
		//	mBoundList.EndEdit(cancel);
		//mEditingRow = null;
	}

	/// <summary>
	/// Delete all the selected rows.
	/// </summary>
	/// <returns>Returns true if one or more row is deleted otherwise false.</returns>
	[Obsolete]
	public virtual async Task<bool> DeleteSelectedRowsAsync() {
		// Ask once before applying the deletion to the selected source rows.
		if (string.IsNullOrEmpty(_deleteQuestionMessage) ||
			await DialogEx.ShowAsync(
				this,
				SystemIconType.Question,
				_deleteQuestionMessage,
				System.Windows.Forms.Application.ProductName,
				System.Windows.Forms.MessageBoxButtons.YesNo,
				System.Windows.Forms.MessageBoxIcon.Question
			) == System.Windows.Forms.DialogResult.Yes
		) {
			foreach (var gridRow in Selection.GetSelectionRegion().GetRowsIndex()) {
				var dataIndex = Rows.IndexToDataSourceIndex(gridRow);
				//if (dataIndex < DataSource.Count)
				//	DataSource.RemoveAt(dataIndex);
			}

			return true;
		}

		return false;
	}

	/// <summary>
	/// Method used to create the rows object, in this class of type PingGridRows.
	/// </summary>
	protected override RowsBase CreateRowsObject() {
		return new PingGridRows(this);
	}

	/// <summary>
	/// Method used to create the columns object, in this class of type DataGridColumns.
	/// </summary>
	protected override ColumnsBase CreateColumnsObject() {
		return new PingGridColumns(this);
	}

	protected override SelectionBase CreateSelectionObject() {
		var selection = base.CreateSelectionObject();

		selection.EnableMultiSelection = true;
		selection.FocusStyle = SourceGrid.FocusStyle.RemoveFocusCellOnLeave;
		selection.FocusRowLeaving += new RowCancelEventHandler(SelectionFocusRowLeaving);

		return selection;
	}

	protected override void OnSortingRangeRows(SortRangeRowsEventArgs args) {
		base.OnSortingRangeRows(args);

		if (DataSource == null || DataSource.AllowSort == false)
			return;
		var property = this.Columns[args.KeyColumn].PropertyName;
		DataSource.ApplySort(property, args.Ascending);

		// force redraw
		this.Invalidate();
	}

	/*[Obsolete]
							protected override void OnKeyDown(System.Windows.Forms.KeyEventArgs e)
							{
								base.OnKeyDown(e);

								if (e.KeyCode == System.Windows.Forms.Keys.Delete &&
									mBoundList != null &&
									mBoundList.AllowDelete &&
									e.Handled == false &&
									mDeleteRowsWithDeleteKey)
								{
									object[] rows = SelectedDataRows;
									if (rows != null && rows.Length > 0)
										DeleteSelectedRows();

									e.Handled = true;
								}
								else if (e.KeyCode == System.Windows.Forms.Keys.Escape &&
										 e.Handled == false &&
										 mCancelEditingWithEscapeKey)
								{
									EndEditingRow(true);

									e.Handled = true;
								}
							}*/

	protected override void OnValidating(CancelEventArgs args) {
		base.OnValidating(args);

		try {
			//if (EndEditingRowOnValidate)
			//EndEditingRow(false);
		} catch (Exception exception) {
			OnUserException(new ExceptionEventArgs(exception));
		}
	}

	protected override void Dispose(bool disposing) {
		base.Dispose(disposing);
	}

	protected virtual void Bind() {
		//if (Columns.Count == 0)
		//	CreateColumns();
		//InvalidateDataGridColumns();

		//mBoundList.ListChanged += new ListChangedEventHandler(mBoundList_ListChanged);
		//mBoundList.ItemDeleted += new ItemDeletedEventHandler(mBoundList_ItemDeleted);
		//mBoundList.ListCleared += new EventHandler(mBoundList_ListCleared);
		Rows.RowsChanged();
		//Rows.ResetRowHeigth();
	}

	protected virtual void Unbind() {
		if (_boundList != null) {
			//mBoundList.ListChanged -= new ListChangedEventHandler(mBoundList_ListChanged);
			//mBoundList.ItemDeleted -= new ItemDeletedEventHandler(mBoundList_ItemDeleted);
			//mBoundList.ListCleared -= new EventHandler(mBoundList_ListCleared);
		}

		Rows.RowsChanged();
	}

	protected virtual void BoundListChanged(object sender, ListChangedEventArgs args) {
		if (base.IsSuspended() == true)
			return;
		Rows.RowsChanged();
		Invalidate(true);
	}

	[Obsolete]
	private void BoundListCleared(object sender, EventArgs args) {
		Rows.ResetRowHeigth();
	}

	[Obsolete]
	private void BoundListItemDeleted(object sender, Sphere10.Framework.ComponentModel.ItemDeletedEventArgs<object> args) {
		Rows.RowDeleted(args.Item);
	}

	private void SelectionFocusRowLeaving(object sender, RowCancelEventArgs args) {
		try {
			//EndEditingRow(false);
		} catch (Exception exception) {
			OnUserException(new ExceptionEventArgs(new EndEditingException(exception)));

			args.Cancel = true;
		}
	}

	[Obsolete]
	private void InvalidateDataGridColumns() {
		foreach (PingGridColumn column in Columns)
			column.Invalidate();
	}
}
