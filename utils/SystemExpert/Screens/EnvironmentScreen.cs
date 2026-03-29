using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Sphere10.Framework;
using Sphere10.Framework.Windows.Forms;

namespace SystemExpert.Screens;

public partial class EnvironmentScreen : ApplicationScreen {
	private readonly EnvironmentVariableDataSource _dataSource;
	private readonly IEnumerable<ICrudGridColumn> _gridBindings;

	public EnvironmentScreen() {
		InitializeComponent();
		_dataSource = new EnvironmentVariableDataSource();
		_gridBindings = new[] {
			new CrudGridColumn<EnvironmentVariableInfo> {
				ColumnName = "Name",
				SortName = "Name",
				DataType = typeof(string),
				PropertyValue = v => v.Name,
				DisplayType = CrudCellDisplayType.Text,
				CanEditCell = false
			},
			new CrudGridColumn<EnvironmentVariableInfo> {
				ColumnName = "Value",
				SortName = "Value",
				DataType = typeof(string),
				PropertyValue = v => v.Value,
				DisplayType = CrudCellDisplayType.Text,
				ExpandsToFit = true,
				CanEditCell = false
			},
			new CrudGridColumn<EnvironmentVariableInfo> {
				ColumnName = "Scope",
				SortName = "Scope",
				DataType = typeof(string),
				PropertyValue = v => v.Scope,
				DisplayType = CrudCellDisplayType.Text,
				CanEditCell = false
			},
		};
		_crudGrid.GridBindings = _gridBindings;
		_crudGrid.RightClickForContextMenu = false;
	}

	protected override async void OnLoad(EventArgs e) {
		base.OnLoad(e);
		await _crudGrid.SetDataSource(_dataSource);
		_crudGrid.Capabilities = DataSourceCapabilities.CanRead | DataSourceCapabilities.CanSearch | DataSourceCapabilities.CanSort | DataSourceCapabilities.CanPage;
		await _crudGrid.RefreshGrid();
	}

	private void _refreshButton_Click(object sender, EventArgs e) {
		_dataSource.Invalidate();
		_crudGrid.RefreshGrid();
	}
}
