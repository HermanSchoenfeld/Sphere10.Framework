using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Windows.Forms;
using Sphere10.Framework;
using Sphere10.Framework.Windows;
using Sphere10.Framework.Windows.Forms;

namespace SystemExpert.Screens;

public partial class ProcessesScreen : ApplicationScreen {
 private readonly ProcessInfoDataSource _dataSource;
	private readonly IEnumerable<ICrudGridColumn> _gridBindings;
	private Timer _autoRefreshTimer;
	private int _refreshIntervalSeconds = 5;
	private ContextMenuStrip _processContextMenu;

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
		_crudGrid.RightClickForContextMenu = false;
		_processContextMenu = BuildProcessContextMenu();
		_crudGrid.ContextMenuStrip = _processContextMenu;

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

	#region Process Message Sending

	private ContextMenuStrip BuildProcessContextMenu() {
		var contextMenu = new ContextMenuStrip();
		var sendMessageItem = new ToolStripMenuItem("Send Message");

		sendMessageItem.DropDownItems.AddRange(new ToolStripItem[] {
			new ToolStripMenuItem("WM_QUERYENDSESSION", null, (s, e) => SendWindowMessage((uint)WinAPI.USER32.WM.QUERYENDSESSION, IntPtr.Zero, (IntPtr)ProcessSignaler.ENDSESSION_CLOSEAPP)),
			new ToolStripMenuItem("WM_ENDSESSION", null, (s, e) => SendWindowMessage((uint)WinAPI.USER32.WM.ENDSESSION, (IntPtr)1, (IntPtr)ProcessSignaler.ENDSESSION_CLOSEAPP)),
			new ToolStripMenuItem("WM_CLOSE", null, (s, e) => SendWindowMessage((uint)WinAPI.USER32.WM.CLOSE, IntPtr.Zero, IntPtr.Zero, synchronous: false)),
			new ToolStripMenuItem("WM_QUIT", null, (s, e) => SendWindowMessage((uint)WinAPI.USER32.WM.QUIT, IntPtr.Zero, IntPtr.Zero, synchronous: false)),
			new ToolStripMenuItem("WM_DESTROY", null, (s, e) => SendWindowMessage((uint)WinAPI.USER32.WM.DESTROY, IntPtr.Zero, IntPtr.Zero)),
			new ToolStripSeparator(),
			new ToolStripMenuItem("Close Main Window", null, (s, e) => ExecuteOnSelectedProcess(p => p.CloseMainWindow())),
			new ToolStripMenuItem("Kill Process", null, (s, e) => ExecuteOnSelectedProcess(p => p.Kill())),
			new ToolStripMenuItem("Kill Process Tree", null, (s, e) => ExecuteOnSelectedProcess(p => p.Kill(true))),
			new ToolStripSeparator(),
			new ToolStripMenuItem("CTRL+C Signal", null, (s, e) => SendConsoleSignal(WinAPI.KERNEL32.CTRL_C_EVENT)),
			new ToolStripMenuItem("CTRL+BREAK Signal", null, (s, e) => SendConsoleSignal(WinAPI.KERNEL32.CTRL_BREAK_EVENT)),
		});

		contextMenu.Items.Add(sendMessageItem);
		contextMenu.Opening += (s, e) => {
			if (_crudGrid.SelectedEntity is not ProcessInfo)
				e.Cancel = true;
		};

		return contextMenu;
	}

	private void ExecuteOnSelectedProcess(Action<Process> action) {
		if (_crudGrid.SelectedEntity is not ProcessInfo processInfo)
			return;
		try {
			using var process = Process.GetProcessById(processInfo.PID);
			action(process);
		} catch (ArgumentException) {
			MessageBox.Show(this, $"Process (PID {processInfo.PID}) has exited.", "Process Not Found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
		} catch (Exception ex) {
			MessageBox.Show(this, $"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
	}

	private void SendWindowMessage(uint msg, IntPtr wParam, IntPtr lParam, bool synchronous = true) {
		ExecuteOnSelectedProcess(process => {
			var hwnd = process.MainWindowHandle;
			if (hwnd == IntPtr.Zero) {
				MessageBox.Show(this, $"Process '{process.ProcessName}' (PID {process.Id}) has no main window.",
					"No Window Handle", MessageBoxButtons.OK, MessageBoxIcon.Warning);
				return;
			}
			Tools.WinTool.Processes.SendWindowMessage(hwnd, msg, wParam, lParam, synchronous);
		});
	}

	private void SendConsoleSignal(uint signal) {
		if (_crudGrid.SelectedEntity is not ProcessInfo processInfo)
			return;
		try {
			Tools.WinTool.Processes.SendConsoleSignal(processInfo.PID, signal);
		} catch (Exception ex) {
			MessageBox.Show(this, $"Error sending console signal: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
		}
	}

	#endregion
}
