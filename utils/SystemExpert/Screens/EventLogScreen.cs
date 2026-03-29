using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Windows.Forms;
using Sphere10.Framework;
using Sphere10.Framework.Windows.Forms;

namespace SystemExpert.Screens;

public partial class EventLogScreen : ApplicationScreen {
	private EventLogDataSource _dataSource;
	private readonly IEnumerable<ICrudGridColumn> _gridBindings;

	public EventLogScreen() {
		InitializeComponent();
		_dataSource = new EventLogDataSource("Application");
		_gridBindings = new[] {
			new CrudGridColumn<EventLogEntryInfo> {
				ColumnName = "Index",
				SortName = "Index",
				DataType = typeof(long),
				PropertyValue = e => e.Index,
				DisplayType = CrudCellDisplayType.Text,
				CanEditCell = false
			},
			new CrudGridColumn<EventLogEntryInfo> {
				ColumnName = "Level",
				SortName = "Level",
				DataType = typeof(string),
				PropertyValue = e => e.Level,
				DisplayType = CrudCellDisplayType.Text,
				CanEditCell = false
			},
			new CrudGridColumn<EventLogEntryInfo> {
				ColumnName = "Date/Time",
				SortName = "TimeGenerated",
				DataType = typeof(string),
				PropertyValue = e => e.TimeGenerated.ToString("yyyy-MM-dd HH:mm:ss"),
				DisplayType = CrudCellDisplayType.Text,
				CanEditCell = false
			},
			new CrudGridColumn<EventLogEntryInfo> {
				ColumnName = "Source",
				SortName = "Source",
				DataType = typeof(string),
				PropertyValue = e => e.Source,
				DisplayType = CrudCellDisplayType.Text,
				CanEditCell = false
			},
			new CrudGridColumn<EventLogEntryInfo> {
				ColumnName = "Message",
				SortName = "Message",
				DataType = typeof(string),
				PropertyValue = e => e.Message,
				DisplayType = CrudCellDisplayType.Text,
				ExpandsToFit = true,
				CanEditCell = false
			},
		};
		_crudGrid.GridBindings = _gridBindings;
		_crudGrid.RightClickForContextMenu = false;

		PopulateLogNames();
	}

	protected override async void OnLoad(EventArgs e) {
		base.OnLoad(e);
		await _crudGrid.SetDataSource(_dataSource);
		_crudGrid.Capabilities = DataSourceCapabilities.CanRead | DataSourceCapabilities.CanSearch | DataSourceCapabilities.CanSort | DataSourceCapabilities.CanPage;
		await _crudGrid.RefreshGrid();
	}

	private void PopulateLogNames() {
		_logComboBox.Items.Clear();
		try {
			var logNames = EventLog.GetEventLogs()
				.Select(l => l.Log)
				.Distinct()
				.OrderBy(l => l, StringComparer.OrdinalIgnoreCase)
				.ToArray();
			foreach (var name in logNames)
				_logComboBox.Items.Add(name);
		} catch {
			_logComboBox.Items.Add("Application");
			_logComboBox.Items.Add("System");
			_logComboBox.Items.Add("Security");
		}
		_logComboBox.SelectedItem = "Application";
	}

	private void _logComboBox_SelectedIndexChanged(object sender, EventArgs e) {
		if (_logComboBox.SelectedItem is string logName && logName != _dataSource.LogName) {
			_dataSource = new EventLogDataSource(logName);
			_crudGrid.SetDataSource(_dataSource);
			_crudGrid.RefreshGrid();
		}
	}

	private void _refreshButton_Click(object sender, EventArgs e) {
		_dataSource.Invalidate();
		_crudGrid.RefreshGrid();
	}
}
