// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;

namespace Sphere10.Framework.Windows.Forms.SourceGrid;

/// <summary>
/// This class implements a RowsSimpleBase class using a DataView bound mode for row count.
/// </summary>
public class DataGridRows : RowsSimpleBase {
	private AutoSizeMode _autoSizeMode = AutoSizeMode.Default;

	private int _headerHeight;

	private Dictionary<int, int> _rowHeights = new Dictionary<int, int>();

	public DataGridRows(DataGrid grid)
		: base(grid) {
		_headerHeight = grid.DefaultHeight;
	}

	public new DataGrid Grid {
		get => (DataGrid)base.Grid;
	}

	/// <summary>
	/// Gets the number of row of the current DataView. Usually this value is automatically calculated and cannot be changed manually.
	/// </summary>
	public override int Count {
		get {
			if (Grid.DataSource == null)
				return Grid.FixedRows;
			if (Grid.DataSource.AllowNew)
				return Grid.DataSource.Count + Grid.FixedRows + 1;
			return Grid.DataSource.Count + Grid.FixedRows;
		}
	}

	public AutoSizeMode AutoSizeMode {
		get => _autoSizeMode;
		set => _autoSizeMode = value;
	}

	/// <summary>
	/// Gets or sets the header height (row 0)
	/// </summary>
	public int HeaderHeight {
		get => _headerHeight;
		set {
			if (_headerHeight != value) {
				_headerHeight = value;
				PerformLayout();
			}
		}
	}

	/// <summary>
	/// Returns the grid index for the specified DataSource index
	/// </summary>
	/// <returns></returns>
	public int DataSourceIndexToGridRowIndex(int dataSourceIndex) => dataSourceIndex + Grid.FixedRows;

	/// <summary>
	/// Returns the index for a given item row. -1 if not valid.
	/// </summary>
	/// <param name="row"></param>
	/// <returns></returns>
	public int DataSourceRowToIndex(object row) {
		if (Grid.DataSource != null)
			return Grid.DataSource.IndexOf(row);

		return -1;
	}

	public override AutoSizeMode GetAutoSizeMode(int row) => _autoSizeMode;

	public void ResetRowHeigth() => _rowHeights.Clear();

	public void RowDeleted(object row) {
		if (row != null && _rowHeights.ContainsKey(row.GetHashCode()))
			_rowHeights.Remove(row.GetHashCode());
	}

	public override int GetHeight(int row) {
		if (row == 0)
			return HeaderHeight;
		var objectRow = IndexToDataSourceRow(row);
		if (objectRow != null && _rowHeights.ContainsKey(objectRow.GetHashCode()))
			return _rowHeights[objectRow.GetHashCode()];
		return base.GetHeight(row);
	}

	public override void SetHeight(int row, int height) {
		if (row == 0)
			HeaderHeight = height;
		else if (IndexToDataSourceRow(row) != null) {
			var hash = IndexToDataSourceRow(row).GetHashCode();
			if (!_rowHeights.ContainsKey(hash) || _rowHeights[hash] != height) {
				_rowHeights[hash] = height;
				PerformLayout();
			}
		} else
			base.SetHeight(row, height);
	}

	/// <summary>
	/// Returns the DataRowView object for a given grid row index. Return null if not applicable, for example if the DataSource is null or if the row index requested is a FixedRows
	/// </summary>
	/// <param name="gridRowIndex"></param>
	/// <returns></returns>
	public object IndexToDataSourceRow(int gridRowIndex) {
		var dataIndex = IndexToDataSourceIndex(gridRowIndex);

		//Verifico che l'indice sia valido, perchè potrei essere in un caso in cui le righe non combaciano (magari a seguito di un refresh non ancora completo)
		if (Grid.DataSource != null && dataIndex >= 0 && dataIndex < Grid.DataSource.Count)
			return Grid.DataSource[dataIndex];
		return null;
	}

	/// <summary>
	/// Returns the DataSource index for the specified grid row index.
	/// </summary>
	/// <param name="gridRowIndex"></param>
	/// <returns></returns>
	public int IndexToDataSourceIndex(int gridRowIndex) {
		var dataIndex = gridRowIndex - Grid.FixedRows;
		return dataIndex;
	}
}
