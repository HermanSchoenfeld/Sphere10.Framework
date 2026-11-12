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

partial class ScreenA {
	private TableLayoutPanel _layout;
	private Label _heading;
	private Label _introduction;
	private Panel _content;
	private MonthCalendar _monthCalendar1;
	private RadioButton _radioButton1;
	private Label _selection;
	private ToolStrip _toolStrip1;
	private ToolStripLabel _toolStripLabel1;
	private ToolStripButton _toolStripButton1;

	// The full navigation sample remains editable on its concrete Designer surface.
	private void InitializeComponent() {
		_layout = new TableLayoutPanel();
		_heading = new Label();
		_introduction = new Label();
		_content = new Panel();
		_monthCalendar1 = new MonthCalendar();
		_radioButton1 = new RadioButton();
		_selection = new Label();
		_toolStrip1 = new ToolStrip();
		_toolStripLabel1 = new ToolStripLabel();
		_toolStripButton1 = new ToolStripButton();
		_layout.SuspendLayout();
		_content.SuspendLayout();
		SuspendLayout();

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
		_heading.Text = "Calendar And Contextual Toolbar";
		_heading.AutoSize = true;
		_heading.Font = new Font("Segoe UI", 17F, FontStyle.Bold);
		_heading.Margin = new Padding(0, 0, 0, 12);
		_introduction.Text = "Choose dates and compare the contextual toolbar while switching screens or undocking this instance. Its selection stays with this screen.";
		_introduction.AutoSize = true;
		_introduction.Dock = DockStyle.Fill;
		_introduction.Margin = new Padding(0, 0, 0, 16);
		_content.Dock = DockStyle.Fill;
		_monthCalendar1.Name = "monthCalendar1";
		_monthCalendar1.Location = new Point(12, 12);
		_monthCalendar1.DateChanged += SelectionChanged;
		_radioButton1.Name = "radioButton1";
		_radioButton1.Text = "Selection confirmed by this screen’s toolbar";
		_radioButton1.AutoSize = true;
		_radioButton1.Location = new Point(12, 200);
		_radioButton1.CheckedChanged += SelectionChanged;
		_selection.AutoSize = true;
		_selection.Location = new Point(12, 240);
		_selection.Text = "Choose a date range, then use Confirm selection in the toolbar.";
		_toolStrip1.Name = "toolStrip1";
		_toolStrip1.GripStyle = ToolStripGripStyle.Hidden;
		_toolStrip1.Items.AddRange(new ToolStripItem[] { _toolStripLabel1, _toolStripButton1 });
		_toolStripLabel1.Text = "Calendar tools";
		_toolStripButton1.Text = "Confirm selection";
		_toolStripButton1.DisplayStyle = ToolStripItemDisplayStyle.Text;
		_toolStripButton1.Click += ToolStripButton1_Click;
		_content.Controls.Add(_monthCalendar1);
		_content.Controls.Add(_radioButton1);
		_content.Controls.Add(_selection);
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		Font = SystemFonts.MessageBoxFont;
		Name = "ScreenA";
		Size = new Size(1000, 720);
		Controls.Add(_layout);
		Controls.Add(_toolStrip1);
		ScreenToolBar = _toolStrip1;
		_content.ResumeLayout(false);
		_content.PerformLayout();
		_layout.ResumeLayout(false);
		_layout.PerformLayout();
		ResumeLayout(false);
	}
}
