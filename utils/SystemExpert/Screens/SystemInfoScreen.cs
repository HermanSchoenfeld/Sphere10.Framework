using System;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Management;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;
using Sphere10.Framework.Windows.Forms;

namespace SystemExpert.Screens;

public partial class SystemInfoScreen : ApplicationScreen {
	private Timer _refreshTimer;
	private PerformanceCounter _cpuCounter;
	private Label _osLabel;
	private Label _machineLabel;
	private Label _cpuLabel;
	private Label _ramLabel;
	private Label _uptimeLabel;
	private Label _cpuUsageLabel;
	private Label _memUsageLabel;
	private ProgressBar _cpuBar;
	private ProgressBar _memBar;
	private ListView _diskListView;

	public SystemInfoScreen() {
		InitializeComponent();
	}

	protected override void OnLoad(EventArgs e) {
		base.OnLoad(e);
		try {
			_cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total");
			_cpuCounter.NextValue(); // prime the counter
		} catch {
			_cpuCounter = null;
		}
		PopulateStaticInfo();
		RefreshDynamicInfo();
		_refreshTimer = new Timer { Interval = 2000 };
		_refreshTimer.Tick += (s, _) => RefreshDynamicInfo();
		_refreshTimer.Start();
	}

	private void PopulateStaticInfo() {
		_osLabel.Text = $"OS: {RuntimeInformation.OSDescription}";
		_machineLabel.Text = $"Machine: {Environment.MachineName}  |  User: {Environment.UserName}  |  Processors: {Environment.ProcessorCount}";

		var cpuName = "Unknown";
		try {
			using var searcher = new ManagementObjectSearcher("SELECT Name FROM Win32_Processor");
			foreach (var obj in searcher.Get()) {
				cpuName = obj["Name"]?.ToString()?.Trim() ?? cpuName;
				break;
			}
		} catch { /* WMI unavailable */ }
		_cpuLabel.Text = $"CPU: {cpuName}";

		var totalRamBytes = GC.GetGCMemoryInfo().TotalAvailableMemoryBytes;
		_ramLabel.Text = $"Installed RAM: {totalRamBytes / (1024.0 * 1024 * 1024):N1} GB";
	}

	private void RefreshDynamicInfo() {
		try {
			// CPU usage
			float cpuPercent = 0;
			if (_cpuCounter != null) {
				try { cpuPercent = _cpuCounter.NextValue(); } catch { cpuPercent = 0; }
			}
			_cpuBar.Value = Math.Clamp((int)cpuPercent, 0, 100);
			_cpuUsageLabel.Text = $"CPU: {cpuPercent:N1}%";

			// Memory usage
			var gcInfo = GC.GetGCMemoryInfo();
			var totalBytes = gcInfo.TotalAvailableMemoryBytes;
			var usedBytes = totalBytes - GetAvailablePhysicalMemory();
			var memPercent = totalBytes > 0 ? (double)usedBytes / totalBytes * 100.0 : 0;
			_memBar.Value = Math.Clamp((int)memPercent, 0, 100);
			_memUsageLabel.Text = $"Memory: {usedBytes / (1024.0 * 1024 * 1024):N1} / {totalBytes / (1024.0 * 1024 * 1024):N1} GB ({memPercent:N1}%)";

			// Uptime
			var uptime = TimeSpan.FromMilliseconds(Environment.TickCount64);
			_uptimeLabel.Text = $"Uptime: {(int)uptime.TotalDays}d {uptime.Hours}h {uptime.Minutes}m {uptime.Seconds}s";

			// Disks
			RefreshDisks();
		} catch {
			// defensive
		}
	}

	private void RefreshDisks() {
		_diskListView.Items.Clear();
		foreach (var drive in DriveInfo.GetDrives().Where(d => d.IsReady)) {
			var totalGb = drive.TotalSize / (1024.0 * 1024 * 1024);
			var freeGb = drive.AvailableFreeSpace / (1024.0 * 1024 * 1024);
			var usedGb = totalGb - freeGb;
			var usedPercent = totalGb > 0 ? usedGb / totalGb * 100.0 : 0;
			var item = new ListViewItem(new[] {
				drive.Name,
				drive.VolumeLabel,
				drive.DriveFormat,
				$"{totalGb:N1} GB",
				$"{freeGb:N1} GB",
				$"{usedPercent:N1}%"
			});
			_diskListView.Items.Add(item);
		}
	}

	private static long GetAvailablePhysicalMemory() {
		try {
			using var searcher = new ManagementObjectSearcher("SELECT FreePhysicalMemory FROM Win32_OperatingSystem");
			foreach (var obj in searcher.Get()) {
				var freeKb = Convert.ToInt64(obj["FreePhysicalMemory"]);
				return freeKb * 1024;
			}
		} catch { /* fallback */ }
		return 0;
	}
}
