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

partial class TabControlTestScreen {
	private TableLayoutPanel _layout;
	private Label _heading;
	private Label _introduction;
	private TabControl _tabControl1;
	private TabPage _tabPage1;
	private TabPage _tabPage2;
	private TabControlTestControl _tabControlTestControl1;
	private GroupBox _groupBox1;
	private RadioButton _radioButton1;
	private TextBox _textBox1;
	private Label _label1;

	// Static layout and named event wiring remain editable in the native Designer.
	private void InitializeComponent() {
		_layout = new TableLayoutPanel();
		_layout.Name = "_layout";
		_heading = new Label();
		_heading.Name = "_heading";
		_introduction = new Label();
		_introduction.Name = "_introduction";
		_tabControl1 = new TabControl();
		_tabPage1 = new TabPage();
		_tabPage1.Name = "_tabPage1";
		_tabPage2 = new TabPage();
		_tabPage2.Name = "_tabPage2";
		_tabControlTestControl1 = new TabControlTestControl();
		_tabControlTestControl1.Name = "_tabControlTestControl1";
		_groupBox1 = new GroupBox();
		_groupBox1.Name = "_groupBox1";
		_radioButton1 = new RadioButton();
		_radioButton1.Name = "_radioButton1";
		_textBox1 = new TextBox();
		_textBox1.Name = "_textBox1";
		_label1 = new Label();
		_label1.Name = "_label1";
		_layout.SuspendLayout();
		SuspendLayout();

		// The heading and instructions remain with this screen when it is detached.
		_layout.Dock = DockStyle.Fill;
		_layout.Padding = new Padding(20);
		_layout.ColumnCount = 1;
		_layout.RowCount = 3;
		_layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_heading.AutoSize = true;
		_heading.Dock = DockStyle.Fill;
		_heading.Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 17F, FontStyle.Bold);
		_heading.Margin = new Padding(0, 0, 0, 10);
		_heading.Text = "Native Tab Controls";
		_introduction.AutoSize = true;
		_introduction.Dock = DockStyle.Fill;
		_introduction.Margin = new Padding(0, 0, 0, 18);
		_introduction.Text = "Compare a composite user control with ordinary grouped inputs. Both pages retain their own draft when you change the selected tab.";
		_layout.Controls.Add(_heading, 0, 0);
		_layout.Controls.Add(_introduction, 0, 1);

		_tabControl1.Name = "exampleTabs";
		_tabControl1.Dock = DockStyle.Fill;
		_tabPage1.Text = "Nested user control";
		_tabPage2.Text = "Native grouped inputs";
		_tabControlTestControl1.Dock = DockStyle.Fill;
		_tabPage1.Controls.Add(_tabControlTestControl1);
		_groupBox1.Text = "Settings inside a tab";
		_groupBox1.Dock = DockStyle.Top;
		_groupBox1.Height = 180;
		_groupBox1.Padding = new Padding(20);
		_radioButton1.Text = "Enable example option";
		_radioButton1.AutoSize = true;
		_radioButton1.Location = new Point(20, 80);
		_textBox1.Text = "An independent tab draft";
		_textBox1.Location = new Point(20, 120);
		_textBox1.Width = 380;
		_label1.Text = "Switching tabs retains each page’s controls and values.";
		_label1.AutoSize = true;
		_label1.Location = new Point(20, 35);
		_groupBox1.Controls.AddRange(new Control[] { _label1, _radioButton1, _textBox1 });
		_tabPage2.Controls.Add(_groupBox1);
		_tabControl1.TabPages.AddRange(new[] { _tabPage1, _tabPage2 });

		// Content-sized command rows keep their complete buttons visible at smaller widths.
		_layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
		_layout.Controls.Add(_tabControl1, 0, 2);
		Font = SystemFonts.MessageBoxFont;
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		BackColor = SystemColors.Window;
		Name = "TabControlTestScreen";
		Text = "Native Tab Controls";
		Size = new Size(1000, 720);
		Controls.Add(_layout);
		_layout.ResumeLayout(false);
		_layout.PerformLayout();
		ResumeLayout(false);
	}
}
