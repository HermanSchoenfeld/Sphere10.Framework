// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;
using System.Windows.Forms;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

partial class ExpandoTesterScreen {
	private TableLayoutPanel _layout;
	private Label _heading;
	private Label _introduction;
	private SplitContainer _body;
	private TaskPane _taskPane1;
	private Expando _expando1;
	private Expando _expando2;
	private Expando _expando3;
	private TaskItem _taskItem1;
	private TaskItem _taskItem2;
	private TaskItem _taskItem3;
	private TaskItem _taskItem5;
	private CheckBox _checkBox1;
	private TreeView _treeView1;
	private TextBox _outputTextBox;
	private FlowLayoutPanel _actions;
	private Button _expandAll;
	private Button _collapseAll;

	// Static layout and named event wiring remain editable in the native Designer.
	private void InitializeComponent() {
		_layout = new TableLayoutPanel();
		_layout.Name = "_layout";
		_heading = new Label();
		_heading.Name = "_heading";
		_introduction = new Label();
		_introduction.Name = "_introduction";
		_body = new SplitContainer();
		_body.Name = "_body";
		_taskPane1 = new TaskPane();
		_taskPane1.Name = "_taskPane1";
		_expando1 = new Expando();
		_expando1.Name = "_expando1";
		_expando2 = new Expando();
		_expando2.Name = "_expando2";
		_expando3 = new Expando();
		_expando3.Name = "_expando3";
		_taskItem1 = new TaskItem();
		_taskItem1.Name = "_taskItem1";
		_taskItem2 = new TaskItem();
		_taskItem2.Name = "_taskItem2";
		_taskItem3 = new TaskItem();
		_taskItem3.Name = "_taskItem3";
		_taskItem5 = new TaskItem();
		_taskItem5.Name = "_taskItem5";
		_checkBox1 = new CheckBox();
		_checkBox1.Name = "_checkBox1";
		_treeView1 = new TreeView();
		_treeView1.Name = "_treeView1";
		_outputTextBox = new TextBox();
		_actions = new FlowLayoutPanel();
		_actions.Name = "_actions";
		_expandAll = new Button();
		_expandAll.Name = "_expandAll";
		_collapseAll = new Button();
		_collapseAll.Name = "_collapseAll";
		_layout.SuspendLayout();
		SuspendLayout();

		// The heading and instructions remain with this screen when it is detached.
		_layout.Dock = DockStyle.Fill;
		_layout.Padding = new Padding(20);
		_layout.ColumnCount = 1;
		_layout.RowCount = 4;
		_layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_heading.AutoSize = true;
		_heading.Dock = DockStyle.Fill;
		_heading.Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 17F, FontStyle.Bold);
		_heading.Margin = new Padding(0, 0, 0, 10);
		_heading.Text = "Expandable Panels";
		_introduction.AutoSize = true;
		_introduction.Dock = DockStyle.Fill;
		_introduction.Margin = new Padding(0, 0, 0, 18);
		_introduction.Text = "Explore animated ExplorerBar groups containing commands, checkboxes and a tree. Expand or collapse groups and inspect the resulting interactions.";
		_layout.Controls.Add(_heading, 0, 0);
		_layout.Controls.Add(_introduction, 0, 1);

		_body.Dock = DockStyle.Fill;
		_body.SplitterDistance = 340;
		_body.Panel1.Controls.Add(_taskPane1);
		_body.Panel2.Controls.Add(_outputTextBox);
		_taskPane1.Dock = DockStyle.Fill;
		_taskPane1.AutoScroll = true;
		_taskPane1.Expandos.AddRange(new[] { _expando1, _expando2, _expando3 });
		_expando1.Text = "Common actions";
		_expando1.Width = 300;
		_expando1.ExpandedHeight = 190;
		_expando1.AutoLayout = true;
		_expando1.Animate = true;
		_expando2.Text = "Filters and navigation";
		_expando2.Width = 300;
		_expando2.ExpandedHeight = 190;
		_expando2.AutoLayout = true;
		_expando2.Animate = true;
		_expando3.Text = "Additional information";
		_expando3.Width = 300;
		_expando3.ExpandedHeight = 190;
		_expando3.AutoLayout = true;
		_expando3.Animate = true;
		_taskItem1.Text = "Create a new document";
		_taskItem1.Height = 28;
		_taskItem1.Width = 270;
		_taskItem1.Click += TaskClicked;
		_taskItem2.Text = "Review recent activity";
		_taskItem2.Height = 28;
		_taskItem2.Width = 270;
		_taskItem2.Click += TaskClicked;
		_taskItem3.Text = "Open selected category";
		_taskItem3.Height = 28;
		_taskItem3.Width = 270;
		_taskItem3.Click += TaskClicked;
		_taskItem5.Text = "Refresh demonstration";
		_taskItem5.Height = 28;
		_taskItem5.Width = 270;
		_taskItem5.Click += TaskClicked;
		_checkBox1.Text = "Include archived categories";
		_checkBox1.AutoSize = true;
		_checkBox1.CheckedChanged += FilterChanged;
		_treeView1.Size = new Size(270, 140);
		_treeView1.Nodes.Add("Projects", "Projects").Nodes.Add("Current work");
		_treeView1.Nodes.Add("Archive", "Archive");
		_treeView1.AfterSelect += CategorySelected;
		_expando1.Items.AddRange(new Control[] { _taskItem1, _taskItem2 });
		_expando2.Items.AddRange(new Control[] { _checkBox1, _taskItem3, _treeView1 });
		_expando2.ExpandedHeight = 270;
		_expando3.Items.Add(_taskItem5);
		_outputTextBox.Name = "_outputTextBox";
		_outputTextBox.Dock = DockStyle.Fill;
		_outputTextBox.Multiline = true;
		_outputTextBox.ReadOnly = true;
		_outputTextBox.ScrollBars = ScrollBars.Both;
		_outputTextBox.WordWrap = false;
		_outputTextBox.BackColor = SystemColors.Window;
		_expandAll.AutoSize = true;
		_expandAll.MinimumSize = new Size(110, 34);
		_expandAll.Text = "Expand all";
		_expandAll.UseVisualStyleBackColor = true;
		_expandAll.Click += ExpandAllClicked;
		_collapseAll.AutoSize = true;
		_collapseAll.MinimumSize = new Size(110, 34);
		_collapseAll.Text = "Collapse all";
		_collapseAll.UseVisualStyleBackColor = true;
		_collapseAll.Click += CollapseAllClicked;
		_actions.AutoSize = true;
		_actions.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_actions.Dock = DockStyle.Fill;
		_actions.Margin = Padding.Empty;
		_actions.Controls.AddRange(new Control[] { _expandAll, _collapseAll });

		// Content-sized command rows keep their complete buttons visible at smaller widths.
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.Controls.Add(_actions, 0, 2);
		_layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
		_layout.Controls.Add(_body, 0, 3);
		Font = SystemFonts.MessageBoxFont;
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		BackColor = SystemColors.Window;
		Name = "ExpandoTesterScreen";
		Text = "Expandable Panels";
		Size = new Size(1000, 720);
		Controls.Add(_layout);
		_layout.ResumeLayout(false);
		_layout.PerformLayout();
		ResumeLayout(false);
	}
}
