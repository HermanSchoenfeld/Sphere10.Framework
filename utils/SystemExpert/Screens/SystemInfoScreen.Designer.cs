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
			_mainPanel = new System.Windows.Forms.Panel();
			_diskGroup = new System.Windows.Forms.GroupBox();
			_diskListView = new System.Windows.Forms.ListView();
			_gaugePanel = new System.Windows.Forms.TableLayoutPanel();
			_uptimeLabel = new System.Windows.Forms.Label();
			_cpuUsageLabel = new System.Windows.Forms.Label();
			_cpuBar = new System.Windows.Forms.ProgressBar();
			_memUsageLabel = new System.Windows.Forms.Label();
			_memBar = new System.Windows.Forms.ProgressBar();
			_infoPanel = new System.Windows.Forms.TableLayoutPanel();
			_osLabel = new System.Windows.Forms.Label();
			_machineLabel = new System.Windows.Forms.Label();
			_cpuLabel = new System.Windows.Forms.Label();
			_ramLabel = new System.Windows.Forms.Label();
			_mainPanel.SuspendLayout();
			_diskGroup.SuspendLayout();
			_gaugePanel.SuspendLayout();
			_infoPanel.SuspendLayout();
			SuspendLayout();
			// 
			// _mainPanel
			// 
			_mainPanel.AutoScroll = true;
			_mainPanel.Controls.Add(_diskGroup);
			_mainPanel.Controls.Add(_gaugePanel);
			_mainPanel.Controls.Add(_infoPanel);
			_mainPanel.Dock = System.Windows.Forms.DockStyle.Fill;
			_mainPanel.Location = new System.Drawing.Point(0, 0);
			_mainPanel.Name = "_mainPanel";
			_mainPanel.Size = new System.Drawing.Size(900, 665);
			_mainPanel.TabIndex = 0;
			// 
			// _diskGroup
			// 
			_diskGroup.Controls.Add(_diskListView);
			_diskGroup.Dock = System.Windows.Forms.DockStyle.Fill;
			_diskGroup.Location = new System.Drawing.Point(0, 179);
			_diskGroup.Name = "_diskGroup";
			_diskGroup.Padding = new System.Windows.Forms.Padding(8);
			_diskGroup.Size = new System.Drawing.Size(900, 486);
			_diskGroup.TabIndex = 0;
			_diskGroup.TabStop = false;
			_diskGroup.Text = "Disk Drives";
			// 
			// _diskListView
			// 
			_diskListView.Dock = System.Windows.Forms.DockStyle.Fill;
			_diskListView.FullRowSelect = true;
			_diskListView.GridLines = true;
			_diskListView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
			_diskListView.Location = new System.Drawing.Point(8, 24);
			_diskListView.Name = "_diskListView";
			_diskListView.Size = new System.Drawing.Size(884, 454);
			_diskListView.TabIndex = 0;
			_diskListView.UseCompatibleStateImageBehavior = false;
			_diskListView.View = System.Windows.Forms.View.Details;
			// 
			// _gaugePanel
			// 
			_gaugePanel.AutoSize = true;
			_gaugePanel.ColumnCount = 2;
			_gaugePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 200F));
			_gaugePanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			_gaugePanel.Controls.Add(_uptimeLabel, 0, 0);
			_gaugePanel.Controls.Add(_cpuUsageLabel, 0, 1);
			_gaugePanel.Controls.Add(_cpuBar, 1, 1);
			_gaugePanel.Controls.Add(_memUsageLabel, 0, 2);
			_gaugePanel.Controls.Add(_memBar, 1, 2);
			_gaugePanel.Dock = System.Windows.Forms.DockStyle.Top;
			_gaugePanel.Location = new System.Drawing.Point(0, 88);
			_gaugePanel.Name = "_gaugePanel";
			_gaugePanel.Padding = new System.Windows.Forms.Padding(8, 4, 8, 4);
			_gaugePanel.RowCount = 3;
			_gaugePanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			_gaugePanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			_gaugePanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			_gaugePanel.Size = new System.Drawing.Size(900, 91);
			_gaugePanel.TabIndex = 1;
			// 
			// _uptimeLabel
			// 
			_uptimeLabel.AutoSize = true;
			_gaugePanel.SetColumnSpan(_uptimeLabel, 2);
			_uptimeLabel.Location = new System.Drawing.Point(11, 4);
			_uptimeLabel.Name = "_uptimeLabel";
			_uptimeLabel.Padding = new System.Windows.Forms.Padding(0, 4, 0, 8);
			_uptimeLabel.Size = new System.Drawing.Size(49, 27);
			_uptimeLabel.TabIndex = 0;
			_uptimeLabel.Text = "Uptime:";
			// 
			// _cpuUsageLabel
			// 
			_cpuUsageLabel.AutoSize = true;
			_cpuUsageLabel.Location = new System.Drawing.Point(11, 31);
			_cpuUsageLabel.Name = "_cpuUsageLabel";
			_cpuUsageLabel.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
			_cpuUsageLabel.Size = new System.Drawing.Size(52, 23);
			_cpuUsageLabel.TabIndex = 1;
			_cpuUsageLabel.Text = "CPU: 0%";
			// 
			// _cpuBar
			// 
			_cpuBar.Dock = System.Windows.Forms.DockStyle.Fill;
			_cpuBar.Location = new System.Drawing.Point(211, 34);
			_cpuBar.Name = "_cpuBar";
			_cpuBar.Size = new System.Drawing.Size(678, 22);
			_cpuBar.TabIndex = 2;
			// 
			// _memUsageLabel
			// 
			_memUsageLabel.AutoSize = true;
			_memUsageLabel.Location = new System.Drawing.Point(11, 59);
			_memUsageLabel.Name = "_memUsageLabel";
			_memUsageLabel.Padding = new System.Windows.Forms.Padding(0, 4, 0, 4);
			_memUsageLabel.Size = new System.Drawing.Size(74, 23);
			_memUsageLabel.TabIndex = 3;
			_memUsageLabel.Text = "Memory: 0%";
			// 
			// _memBar
			// 
			_memBar.Dock = System.Windows.Forms.DockStyle.Fill;
			_memBar.Location = new System.Drawing.Point(211, 62);
			_memBar.Name = "_memBar";
			_memBar.Size = new System.Drawing.Size(678, 22);
			_memBar.TabIndex = 4;
			// 
			// _infoPanel
			// 
			_infoPanel.AutoSize = true;
			_infoPanel.ColumnCount = 1;
			_infoPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
			_infoPanel.Controls.Add(_osLabel, 0, 0);
			_infoPanel.Controls.Add(_machineLabel, 0, 1);
			_infoPanel.Controls.Add(_cpuLabel, 0, 2);
			_infoPanel.Controls.Add(_ramLabel, 0, 3);
			_infoPanel.Dock = System.Windows.Forms.DockStyle.Top;
			_infoPanel.Location = new System.Drawing.Point(0, 0);
			_infoPanel.Name = "_infoPanel";
			_infoPanel.Padding = new System.Windows.Forms.Padding(8, 8, 8, 4);
			_infoPanel.RowCount = 4;
			_infoPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			_infoPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			_infoPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			_infoPanel.RowStyles.Add(new System.Windows.Forms.RowStyle());
			_infoPanel.Size = new System.Drawing.Size(900, 88);
			_infoPanel.TabIndex = 2;
			// 
			// _osLabel
			// 
			_osLabel.AutoSize = true;
			_osLabel.Location = new System.Drawing.Point(11, 8);
			_osLabel.Name = "_osLabel";
			_osLabel.Padding = new System.Windows.Forms.Padding(0, 2, 0, 2);
			_osLabel.Size = new System.Drawing.Size(25, 19);
			_osLabel.TabIndex = 0;
			_osLabel.Text = "OS:";
			// 
			// _machineLabel
			// 
			_machineLabel.AutoSize = true;
			_machineLabel.Location = new System.Drawing.Point(11, 27);
			_machineLabel.Name = "_machineLabel";
			_machineLabel.Padding = new System.Windows.Forms.Padding(0, 2, 0, 2);
			_machineLabel.Size = new System.Drawing.Size(56, 19);
			_machineLabel.TabIndex = 1;
			_machineLabel.Text = "Machine:";
			// 
			// _cpuLabel
			// 
			_cpuLabel.AutoSize = true;
			_cpuLabel.Location = new System.Drawing.Point(11, 46);
			_cpuLabel.Name = "_cpuLabel";
			_cpuLabel.Padding = new System.Windows.Forms.Padding(0, 2, 0, 2);
			_cpuLabel.Size = new System.Drawing.Size(33, 19);
			_cpuLabel.TabIndex = 2;
			_cpuLabel.Text = "CPU:";
			// 
			// _ramLabel
			// 
			_ramLabel.AutoSize = true;
			_ramLabel.Location = new System.Drawing.Point(11, 65);
			_ramLabel.Name = "_ramLabel";
			_ramLabel.Padding = new System.Windows.Forms.Padding(0, 2, 0, 2);
			_ramLabel.Size = new System.Drawing.Size(36, 19);
			_ramLabel.TabIndex = 3;
			_ramLabel.Text = "RAM:";
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
			_diskGroup.ResumeLayout(false);
			_gaugePanel.ResumeLayout(false);
			_gaugePanel.PerformLayout();
			_infoPanel.ResumeLayout(false);
			_infoPanel.PerformLayout();
			ResumeLayout(false);
		}

		#endregion

		private System.Windows.Forms.Panel _mainPanel;
		private System.Windows.Forms.GroupBox _diskGroup;
		private System.Windows.Forms.TableLayoutPanel _gaugePanel;
		private System.Windows.Forms.TableLayoutPanel _infoPanel;
	}
}
