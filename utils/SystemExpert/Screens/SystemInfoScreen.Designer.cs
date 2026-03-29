using Sphere10.Framework.Windows.Forms;

namespace SystemExpert.Screens {
	partial class SystemInfoScreen {
		private System.ComponentModel.IContainer components = null;

		protected override void Dispose(bool disposing) {
			if (disposing) {
				_refreshTimer?.Dispose();
				_cpuCounter?.Dispose();
				components?.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		private void InitializeComponent() {
			var _mainPanel = new System.Windows.Forms.Panel();
			var _infoPanel = new System.Windows.Forms.TableLayoutPanel();
			var _gaugePanel = new System.Windows.Forms.TableLayoutPanel();
			var _diskGroup = new System.Windows.Forms.GroupBox();

			_osLabel = new System.Windows.Forms.Label();
			_machineLabel = new System.Windows.Forms.Label();
			_cpuLabel = new System.Windows.Forms.Label();
			_ramLabel = new System.Windows.Forms.Label();
			_uptimeLabel = new System.Windows.Forms.Label();
			_cpuUsageLabel = new System.Windows.Forms.Label();
			_memUsageLabel = new System.Windows.Forms.Label();
			_cpuBar = new System.Windows.Forms.ProgressBar();
			_memBar = new System.Windows.Forms.ProgressBar();
			_diskListView = new System.Windows.Forms.ListView();

			_mainPanel.SuspendLayout();
			_infoPanel.SuspendLayout();
			_gaugePanel.SuspendLayout();
			_diskGroup.SuspendLayout();
			SuspendLayout();

			// 
			// _infoPanel — static system information
			// 
			_infoPanel.Dock = System.Windows.Forms.DockStyle.Top;
			_infoPanel.ColumnCount = 1;
			_infoPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			_infoPanel.RowCount = 4;
			_infoPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
			_infoPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
			_infoPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
			_infoPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
			_infoPanel.Padding = new System.Windows.Forms.Padding(8, 8, 8, 4);
			_infoPanel.AutoSize = true;
			_infoPanel.Controls.Add(_osLabel, 0, 0);
			_infoPanel.Controls.Add(_machineLabel, 0, 1);
			_infoPanel.Controls.Add(_cpuLabel, 0, 2);
			_infoPanel.Controls.Add(_ramLabel, 0, 3);

			_osLabel.AutoSize = true;
			_osLabel.Padding = new System.Windows.Forms.Padding(0, 2, 0, 2);
			_osLabel.Text = "OS:";

			_machineLabel.AutoSize = true;
			_machineLabel.Padding = new System.Windows.Forms.Padding(0, 2, 0, 2);
			_machineLabel.Text = "Machine:";

			_cpuLabel.AutoSize = true;
			_cpuLabel.Padding = new System.Windows.Forms.Padding(0, 2, 0, 2);
			_cpuLabel.Text = "CPU:";

			_ramLabel.AutoSize = true;
			_ramLabel.Padding = new System.Windows.Forms.Padding(0, 2, 0, 2);
			_ramLabel.Text = "RAM:";

			// 
			// _gaugePanel — CPU and Memory gauges
			// 
			_gaugePanel.Dock = System.Windows.Forms.DockStyle.Top;
			_gaugePanel.ColumnCount = 2;
			_gaugePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 200F));
			_gaugePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			_gaugePanel.RowCount = 3;
			_gaugePanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
			_gaugePanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
			_gaugePanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
			_gaugePanel.Padding = new System.Windows.Forms.Padding(8, 4, 8, 4);
			_gaugePanel.AutoSize = true;

			_uptimeLabel.AutoSize = true;
			_uptimeLabel.Padding = new System.Windows.Forms.Padding(0, 4, 0, 8);
			_uptimeLabel.Text = "Uptime:";
			_gaugePanel.SetColumnSpan(_uptimeLabel, 2);
			_gaugePanel.Controls.Add(_uptimeLabel, 0, 0);

			_cpuUsageLabel.AutoSize = true;
			_cpuUsageLabel.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
			_cpuUsageLabel.Text = "CPU: 0%";
			_cpuBar.Dock = System.Windows.Forms.DockStyle.Fill;
			_cpuBar.Height = 22;
			_cpuBar.Minimum = 0;
			_cpuBar.Maximum = 100;
			_gaugePanel.Controls.Add(_cpuUsageLabel, 0, 1);
			_gaugePanel.Controls.Add(_cpuBar, 1, 1);

			_memUsageLabel.AutoSize = true;
			_memUsageLabel.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
			_memUsageLabel.Text = "Memory: 0%";
			_memBar.Dock = System.Windows.Forms.DockStyle.Fill;
			_memBar.Height = 22;
			_memBar.Minimum = 0;
			_memBar.Maximum = 100;
			_gaugePanel.Controls.Add(_memUsageLabel, 0, 2);
			_gaugePanel.Controls.Add(_memBar, 1, 2);

			// 
			// _diskGroup — Disk information
			// 
			_diskGroup.Dock = System.Windows.Forms.DockStyle.Fill;
			_diskGroup.Text = "Disk Drives";
			_diskGroup.Padding = new System.Windows.Forms.Padding(8, 8, 8, 8);

			_diskListView.Dock = System.Windows.Forms.DockStyle.Fill;
			_diskListView.View = System.Windows.Forms.View.Details;
			_diskListView.FullRowSelect = true;
			_diskListView.GridLines = true;
			_diskListView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			_diskListView.Columns.Add("Drive", 70);
			_diskListView.Columns.Add("Label", 140);
			_diskListView.Columns.Add("Format", 70);
			_diskListView.Columns.Add("Total", 100);
			_diskListView.Columns.Add("Free", 100);
			_diskListView.Columns.Add("Used %", 80);
			_diskGroup.Controls.Add(_diskListView);

			// 
			// _mainPanel
			// 
			_mainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			_mainPanel.AutoScroll = true;
			_mainPanel.Controls.Add(_diskGroup);
			_mainPanel.Controls.Add(_gaugePanel);
			_mainPanel.Controls.Add(_infoPanel);

			// 
			// SystemInfoScreen
			// 
			AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			Controls.Add(_mainPanel);
			Name = "SystemInfoScreen";
			Size = new System.Drawing.Size(900, 665);

			_mainPanel.ResumeLayout(false);
			_mainPanel.PerformLayout();
			_infoPanel.ResumeLayout(false);
			_infoPanel.PerformLayout();
			_gaugePanel.ResumeLayout(false);
			_gaugePanel.PerformLayout();
			_diskGroup.ResumeLayout(false);
			ResumeLayout(false);
			PerformLayout();
		}

		#endregion
	}
}
