using System;
using System.Collections.Generic;
using System.ServiceProcess;
using System.Windows.Forms;
using Sphere10.Framework;
using Sphere10.Framework.Windows.Forms;

namespace SystemExpert.Screens;

public partial class ServicesScreen : ApplicationScreen {
	private readonly ServiceDataSource _dataSource;
	private readonly IEnumerable<ICrudGridColumn> _gridBindings;
	private Timer _autoRefreshTimer;
	private int _refreshIntervalSeconds = 10;
	private ContextMenuStrip _serviceContextMenu;

	public ServicesScreen() {
		InitializeComponent();
		_dataSource = new ServiceDataSource();
		_gridBindings = new[] {
			new CrudGridColumn<ServiceInfo> {
				ColumnName = "Name",
				SortName = "Name",
				DataType = typeof(string),
				PropertyValue = s => s.Name,
				DisplayType = CrudCellDisplayType.Text,
				CanEditCell = false
			},
			new CrudGridColumn<ServiceInfo> {
				ColumnName = "Display Name",
				SortName = "DisplayName",
				DataType = typeof(string),
				PropertyValue = s => s.DisplayName,
				DisplayType = CrudCellDisplayType.Text,
				ExpandsToFit = true,
				CanEditCell = false
			},
			new CrudGridColumn<ServiceInfo> {
				ColumnName = "Status",
				SortName = "Status",
				DataType = typeof(string),
				PropertyValue = s => s.Status,
				DisplayType = CrudCellDisplayType.Text,
				CanEditCell = false
			},
			new CrudGridColumn<ServiceInfo> {
				ColumnName = "Startup Type",
				SortName = "StartupType",
				DataType = typeof(string),
				PropertyValue = s => s.StartupType,
				DisplayType = CrudCellDisplayType.Text,
				CanEditCell = false
			},
			new CrudGridColumn<ServiceInfo> {
				ColumnName = "Log On As",
				SortName = "Account",
				DataType = typeof(string),
				PropertyValue = s => s.Account,
				DisplayType = CrudCellDisplayType.Text,
				CanEditCell = false
			},
		};
		_crudGrid.GridBindings = _gridBindings;
		_crudGrid.RightClickForContextMenu = false;
		_serviceContextMenu = BuildServiceContextMenu();
		_crudGrid.ContextMenuStrip = _serviceContextMenu;

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

	#region Service Actions

	private ContextMenuStrip BuildServiceContextMenu() {
		var contextMenu = new ContextMenuStrip();

		contextMenu.Items.Add(new ToolStripMenuItem("Start", null, (s, e) => ExecuteServiceAction(sc => {
			sc.Start();
			sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(15));
		})));
		contextMenu.Items.Add(new ToolStripMenuItem("Stop", null, (s, e) => ExecuteServiceAction(sc => {
			sc.Stop();
			sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(15));
		})));
		contextMenu.Items.Add(new ToolStripMenuItem("Restart", null, (s, e) => ExecuteServiceAction(sc => {
			sc.Stop();
			sc.WaitForStatus(ServiceControllerStatus.Stopped, TimeSpan.FromSeconds(15));
			sc.Start();
			sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(15));
		})));
		contextMenu.Items.Add(new ToolStripSeparator());
		contextMenu.Items.Add(new ToolStripMenuItem("Pause", null, (s, e) => ExecuteServiceAction(sc => {
			sc.Pause();
			sc.WaitForStatus(ServiceControllerStatus.Paused, TimeSpan.FromSeconds(15));
		})));
		contextMenu.Items.Add(new ToolStripMenuItem("Resume", null, (s, e) => ExecuteServiceAction(sc => {
			sc.Continue();
			sc.WaitForStatus(ServiceControllerStatus.Running, TimeSpan.FromSeconds(15));
		})));

		contextMenu.Opening += (s, e) => {
			if (_crudGrid.SelectedEntity is not ServiceInfo)
				e.Cancel = true;
		};

		return contextMenu;
	}

	private void ExecuteServiceAction(Action<ServiceController> action) {
		if (_crudGrid.SelectedEntity is not ServiceInfo serviceInfo)
			return;

		try {
			using var sc = new ServiceController(serviceInfo.Name);
			action(sc);
			DoRefresh();
		} catch (InvalidOperationException ex) {
			MessageBox.Show(this, $"Cannot perform action on service '{serviceInfo.DisplayName}':\n{ex.InnerException?.Message ?? ex.Message}",
				"Service Action Failed", MessageBoxButtons.OK, MessageBoxIcon.Warning);
		} catch (System.ServiceProcess.TimeoutException) {
			MessageBox.Show(this, $"Timed out waiting for service '{serviceInfo.DisplayName}' to change state.",
				"Timeout", MessageBoxButtons.OK, MessageBoxIcon.Warning);
			DoRefresh();
		} catch (Exception ex) {
			MessageBox.Show(this, $"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
	}

	#endregion
}
