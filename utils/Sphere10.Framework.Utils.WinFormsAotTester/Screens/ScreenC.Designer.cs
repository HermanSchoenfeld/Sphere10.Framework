// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

partial class ScreenC {
	private TableLayoutPanel _layout;
	private Label _heading;
	private Label _introduction;
	private Panel _content;
	private TaskPane _taskPane1;
	private Expando _expando1;
	private Expando _expando2;
	private Expando _expando3;
	private DataGridView _dataGridView1;
	private DataGridView _dataGridView2;
	private DataGridView _dataGridView3;

	// The full navigation sample remains editable on its concrete Designer surface.
	private void InitializeComponent() {
		_layout = new TableLayoutPanel();
		_heading = new Label();
		_introduction = new Label();
		_content = new Panel();
		_taskPane1 = new TaskPane();
		_expando1 = new Expando();
		_expando2 = new Expando();
		_expando3 = new Expando();
		_dataGridView1 = new DataGridView();
		_dataGridView2 = new DataGridView();
		_dataGridView3 = new DataGridView();
		_layout.SuspendLayout();
		_content.SuspendLayout();
		SuspendLayout();

		((ISupportInitialize)_taskPane1).BeginInit();
		((ISupportInitialize)_expando1).BeginInit();
		((ISupportInitialize)_expando2).BeginInit();
		((ISupportInitialize)_expando3).BeginInit();
		((ISupportInitialize)_dataGridView1).BeginInit();
		((ISupportInitialize)_dataGridView2).BeginInit();
		((ISupportInitialize)_dataGridView3).BeginInit();
		_layout.Dock = DockStyle.Fill;
		_layout.Padding = new Padding(20);
		_layout.ColumnCount = 1;
		_layout.RowCount = 3;
		_layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
		_layout.Controls.Add(_heading, 0, 0);
		_layout.Controls.Add(_introduction, 0, 1);
		_layout.Controls.Add(_content, 0, 2);
		_heading.Text = "Task Pane Grids";
		_heading.AutoSize = true;
		_heading.Font = new Font("Segoe UI", 17F, FontStyle.Bold);
		_heading.Margin = new Padding(0, 0, 0, 12);
		_introduction.Text = "Each expandable group owns a separate native grid. Inspect realistic sample rows, collapse groups and resize or undock the screen.";
		_introduction.AutoSize = true;
		_introduction.Dock = DockStyle.Fill;
		_introduction.Margin = new Padding(0, 0, 0, 16);
		_content.Dock = DockStyle.Fill;
		_taskPane1.Name = "taskPane1";
		_taskPane1.Dock = DockStyle.Fill;
		_taskPane1.AutoScroll = true;
		_taskPane1.Expandos.AddRange(new[] { _expando1, _expando2, _expando3 });
		_expando1.Name = "expando1";
		_expando1.Text = "Project milestones";
		_expando1.ExpandedHeight = 220;
		_expando1.Items.Add(_dataGridView1);
		_expando2.Name = "expando2";
		_expando2.Text = "Resource allocation";
		_expando2.ExpandedHeight = 220;
		_expando2.Items.Add(_dataGridView2);
		_expando3.Name = "expando3";
		_expando3.Text = "Review queue";
		_expando3.ExpandedHeight = 220;
		_expando3.Items.Add(_dataGridView3);
		_dataGridView1.Name = "dataGridView1";
		_dataGridView1.Dock = DockStyle.Fill;
		_dataGridView1.ReadOnly = true;
		_dataGridView1.AllowUserToAddRows = false;
		_dataGridView1.AllowUserToDeleteRows = false;
		_dataGridView1.RowHeadersVisible = false;
		_dataGridView1.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
		_dataGridView1.BackgroundColor = SystemColors.Window;
		_dataGridView2.Name = "dataGridView2";
		_dataGridView2.Dock = DockStyle.Fill;
		_dataGridView2.ReadOnly = true;
		_dataGridView2.AllowUserToAddRows = false;
		_dataGridView2.AllowUserToDeleteRows = false;
		_dataGridView2.RowHeadersVisible = false;
		_dataGridView2.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
		_dataGridView2.BackgroundColor = SystemColors.Window;
		_dataGridView3.Name = "dataGridView3";
		_dataGridView3.Dock = DockStyle.Fill;
		_dataGridView3.ReadOnly = true;
		_dataGridView3.AllowUserToAddRows = false;
		_dataGridView3.AllowUserToDeleteRows = false;
		_dataGridView3.RowHeadersVisible = false;
		_dataGridView3.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
		_dataGridView3.BackgroundColor = SystemColors.Window;
		_content.Controls.Add(_taskPane1);
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		Font = SystemFonts.MessageBoxFont;
		Name = "ScreenC";
		Size = new Size(1000, 720);
		Controls.Add(_layout);
		((ISupportInitialize)_taskPane1).EndInit();
		((ISupportInitialize)_expando1).EndInit();
		((ISupportInitialize)_expando2).EndInit();
		((ISupportInitialize)_expando3).EndInit();
		((ISupportInitialize)_dataGridView1).EndInit();
		((ISupportInitialize)_dataGridView2).EndInit();
		((ISupportInitialize)_dataGridView3).EndInit();
		_content.ResumeLayout(false);
		_content.PerformLayout();
		_layout.ResumeLayout(false);
		_layout.PerformLayout();
		ResumeLayout(false);
	}
}
