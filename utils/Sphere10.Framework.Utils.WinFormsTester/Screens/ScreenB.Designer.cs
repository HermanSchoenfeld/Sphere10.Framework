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

partial class ScreenB {
	private TableLayoutPanel _layout;
	private Label _heading;
	private Label _introduction;
	private Panel _content;
	private TableLayoutPanel _columns;
	private TaskPane _taskPane;
	private Expando _expando1;
	private Expando _expando2;
	private Label _firstHelp;
	private Label _secondHelp;
	private ListMerger _listMerger1;
	private Button _reset;

	// The full navigation sample remains editable on its concrete Designer surface.
	private void InitializeComponent() {
		_layout = new TableLayoutPanel();
		_heading = new Label();
		_introduction = new Label();
		_content = new Panel();
		_columns = new TableLayoutPanel();
		_taskPane = new TaskPane();
		_expando1 = new Expando();
		_expando2 = new Expando();
		_firstHelp = new Label();
		_secondHelp = new Label();
		_listMerger1 = new ListMerger();
		_reset = new Button();
		_layout.SuspendLayout();
		_content.SuspendLayout();
		SuspendLayout();

		((ISupportInitialize)_taskPane).BeginInit();
		((ISupportInitialize)_expando1).BeginInit();
		((ISupportInitialize)_expando2).BeginInit();
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
		_heading.Text = "Expandable Panels And List Merging";
		_heading.AutoSize = true;
		_heading.Font = new Font("Segoe UI", 17F, FontStyle.Bold);
		_heading.Margin = new Padding(0, 0, 0, 12);
		_introduction.Text = "Expand the two guidance panels, then move sample skills between the available and selected lists. Reset restores the demonstration membership.";
		_introduction.AutoSize = true;
		_introduction.Dock = DockStyle.Fill;
		_introduction.Margin = new Padding(0, 0, 0, 16);
		_content.Dock = DockStyle.Fill;
		_columns.Dock = DockStyle.Fill;
		_columns.ColumnCount = 2;
		_columns.RowCount = 2;
		_columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35));
		_columns.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 65));
		_columns.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_columns.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
		_columns.Controls.Add(_reset, 1, 0);
		_columns.Controls.Add(_taskPane, 0, 1);
		_columns.Controls.Add(_listMerger1, 1, 1);
		_taskPane.Dock = DockStyle.Fill;
		_taskPane.AutoScroll = true;
		_taskPane.Expandos.AddRange(new[] { _expando1, _expando2 });
		_expando1.Name = "expando1";
		_expando1.Text = "What to try";
		_expando1.ExpandedHeight = 150;
		_expando1.Items.Add(_firstHelp);
		_expando2.Name = "expando2";
		_expando2.Text = "Instance ownership";
		_expando2.ExpandedHeight = 150;
		_expando2.Items.Add(_secondHelp);
		_firstHelp.AutoSize = true;
		_firstHelp.Dock = DockStyle.Top;
		_firstHelp.MaximumSize = new Size(260, 0);
		_firstHelp.Padding = new Padding(10);
		_firstHelp.Text = "Select one or several items. Move or copy them using the existing ListMerger commands, then compare both lists.";
		_secondHelp.AutoSize = true;
		_secondHelp.Dock = DockStyle.Top;
		_secondHelp.MaximumSize = new Size(260, 0);
		_secondHelp.Padding = new Padding(10);
		_secondHelp.Text = "Open this navigation sample again to create a separate instance. Its lists and collapsed panels remain independent.";
		_listMerger1.Name = "listMerger1";
		_listMerger1.Dock = DockStyle.Fill;
		_listMerger1.LeftHeader = "Available skills";
		_listMerger1.RightHeader = "Selected skills";
		_reset.Text = "Reset sample lists";
		_reset.AutoSize = true;
		_reset.MinimumSize = new Size(130, 32);
		_reset.Click += ResetClicked;
		_content.Controls.Add(_columns);
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		Font = SystemFonts.MessageBoxFont;
		Name = "ScreenB";
		Size = new Size(1000, 720);
		Controls.Add(_layout);
		((ISupportInitialize)_taskPane).EndInit();
		((ISupportInitialize)_expando1).EndInit();
		((ISupportInitialize)_expando2).EndInit();
		_content.ResumeLayout(false);
		_content.PerformLayout();
		_layout.ResumeLayout(false);
		_layout.PerformLayout();
		ResumeLayout(false);
	}
}
