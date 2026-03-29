using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Sphere10.Framework;
using Sphere10.Framework.Windows.Forms;

namespace SystemExpert.Screens;

public partial class NetworkScreen : ApplicationScreen {
	private readonly NetworkConnectionDataSource _dataSource;
	private readonly IEnumerable<ICrudGridColumn> _gridBindings;
	private Timer _autoRefreshTimer;
	private int _refreshIntervalSeconds = 5;

	public NetworkScreen() {
		InitializeComponent();
		_dataSource = new NetworkConnectionDataSource();
		_gridBindings = new[] {
			new CrudGridColumn<NetworkConnectionInfo> {
				ColumnName = "Protocol",
				SortName = "Protocol",
				DataType = typeof(string),
				PropertyValue = c => c.Protocol,
				DisplayType = CrudCellDisplayType.Text,
				CanEditCell = false
			},
			new CrudGridColumn<NetworkConnectionInfo> {
				ColumnName = "Local Address",
				SortName = "LocalAddress",
				DataType = typeof(string),
				PropertyValue = c => c.LocalAddress,
				DisplayType = CrudCellDisplayType.Text,
				CanEditCell = false
			},
			new CrudGridColumn<NetworkConnectionInfo> {
				ColumnName = "Local Port",
				SortName = "LocalPort",
				DataType = typeof(int),
				PropertyValue = c => c.LocalPort,
				DisplayType = CrudCellDisplayType.Text,
				CanEditCell = false
			},
			new CrudGridColumn<NetworkConnectionInfo> {
				ColumnName = "Remote Address",
				SortName = "RemoteAddress",
				DataType = typeof(string),
				PropertyValue = c => c.RemoteAddress,
				DisplayType = CrudCellDisplayType.Text,
				ExpandsToFit = true,
				CanEditCell = false
			},
			new CrudGridColumn<NetworkConnectionInfo> {
				ColumnName = "Remote Port",
				SortName = "RemotePort",
				DataType = typeof(int),
				PropertyValue = c => c.RemotePort,
				DisplayType = CrudCellDisplayType.Text,
				CanEditCell = false
			},
			new CrudGridColumn<NetworkConnectionInfo> {
				ColumnName = "State",
				SortName = "State",
				DataType = typeof(string),
				PropertyValue = c => c.State,
				DisplayType = CrudCellDisplayType.Text,
				CanEditCell = false
			},
		};
		_crudGrid.GridBindings = _gridBindings;
		_crudGrid.RightClickForContextMenu = false;

		_autoRefreshTimer = new Timer { Interval = (int)TimeSpan.FromSeconds(_refreshIntervalSeconds).TotalMilliseconds };
		_autoRefreshTimer.Tick += (s, e) => DoRefresh();

		UpdateAutoRefreshUI();
	}

	protected override async void OnLoad(EventArgs e) {
		base.OnLoad(e);
		await _crudGrid.SetDataSource(_dataSource);
		_crudGrid.Capabilities = DataSourceCapabilities.CanRead | DataSourceCapabilities.CanSearch | DataSourceCapabilities.CanSort | DataSourceCapabilities.CanPage;
		await _crudGrid.RefreshGrid();
	}

	private void DoRefresh() {
		_dataSource.Invalidate();
		_crudGrid.RefreshGrid();
	}

	private void _refreshButton_Click(object sender, EventArgs e) {
		DoRefresh();
	}

	private void _autoRefreshCheckBox_CheckedChanged(object sender, EventArgs e) {
		UpdateAutoRefreshUI();
	}

	private void _intervalTextBox_TextChanged(object sender, EventArgs e) {
		if (int.TryParse(_intervalTextBox.Text, out var seconds) && seconds > 0) {
			_refreshIntervalSeconds = seconds;
			_autoRefreshTimer.Interval = _refreshIntervalSeconds * 1000;
		}
	}

	private void UpdateAutoRefreshUI() {
		var autoRefresh = _autoRefreshCheckBox.Checked;
		_refreshButton.Visible = !autoRefresh;
		_intervalLabel.Visible = autoRefresh;
		_intervalTextBox.Visible = autoRefresh;
		_secondsLabel.Visible = autoRefresh;

		if (autoRefresh) {
			_autoRefreshTimer.Interval = (int)TimeSpan.FromSeconds(_refreshIntervalSeconds).TotalMilliseconds;
			_autoRefreshTimer.Start();
		} else {
			_autoRefreshTimer.Stop();
		}
	}
}
