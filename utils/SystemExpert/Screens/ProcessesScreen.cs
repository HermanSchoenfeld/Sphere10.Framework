using System;
using System.Collections.Generic;
using System.Windows.Forms;
using Sphere10.Framework;
using Sphere10.Framework.Windows.Forms;

namespace SystemExpert.Screens;

public partial class ProcessesScreen : ApplicationScreen {
 private readonly ProcessInfoDataSource _dataSource;
	private readonly IEnumerable<ICrudGridColumn> _gridBindings;
	private Timer _autoRefreshTimer;
	private int _refreshIntervalSeconds = 5;

	public ProcessesScreen() {
		InitializeComponent();
      _dataSource = new ProcessInfoDataSource();
		_gridBindings = new[] {
			new CrudGridColumn<ProcessInfo> {
				ColumnName = "PID",
				SortName = "PID",
				DataType = typeof(int),
				PropertyValue = p => p.PID,
				DisplayType = CrudCellDisplayType.Text,
				CanEditCell = false
			},
			new CrudGridColumn<ProcessInfo> {
				ColumnName = "Name",
				SortName = "Name",
				DataType = typeof(string),
				PropertyValue = p => p.Name,
				DisplayType = CrudCellDisplayType.Text,
				ExpandsToFit = true,
				CanEditCell = false
			},
			new CrudGridColumn<ProcessInfo> {
				ColumnName = "Memory (MB)",
				SortName = "MemoryMB",
				DataType = typeof(string),
				PropertyValue = p => p.MemoryMB.ToString("N1"),
				DisplayType = CrudCellDisplayType.Text,
				CanEditCell = false
			},
			new CrudGridColumn<ProcessInfo> {
				ColumnName = "Threads",
				SortName = "ThreadCount",
				DataType = typeof(int),
				PropertyValue = p => p.ThreadCount,
				DisplayType = CrudCellDisplayType.Text,
				CanEditCell = false
			},
			new CrudGridColumn<ProcessInfo> {
				ColumnName = "Priority",
				SortName = "Priority",
				DataType = typeof(string),
				PropertyValue = p => p.Priority,
				DisplayType = CrudCellDisplayType.Text,
				CanEditCell = false
			},
			new CrudGridColumn<ProcessInfo> {
				ColumnName = "Responding",
				SortName = "Responding",
				DataType = typeof(bool),
				PropertyValue = p => p.Responding,
				DisplayType = CrudCellDisplayType.Boolean,
				CanEditCell = false
			},
		};
		_crudGrid.GridBindings = _gridBindings;

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
