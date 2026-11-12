// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;
using System.Windows.Forms;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

partial class UrlIDTestScreen {
	private TableLayoutPanel _layout;
	private Label _heading;
	private Label _introduction;
	private FlowLayoutPanel _commands;
	private Label _status;
	private Label _startLabel;
	private NumericUpDown _start;
	private Label _countLabel;
	private NumericUpDown _count;
	private ComboBox _format;
	private Button _generateButton;
	private Button _clearButton;
	private TextBoxEx _textBoxEx;

	// Keep the complete static demonstration editable on the native design surface.
	private void InitializeComponent() {
		_layout = new TableLayoutPanel();
		_heading = new Label();
		_introduction = new Label();
		_commands = new FlowLayoutPanel();
		_status = new Label();
		_startLabel = new Label();
		_start = new NumericUpDown();
		_countLabel = new Label();
		_count = new NumericUpDown();
		_format = new ComboBox();
		_generateButton = new Button();
		_clearButton = new Button();
		_textBoxEx = new TextBoxEx();
		_layout.SuspendLayout();
		_commands.SuspendLayout();
		SuspendLayout();

		_layout.Name = "_layout";
		_heading.Name = "_heading";
		_introduction.Name = "_introduction";
		_commands.Name = "_commands";
		_status.Name = "_status";
		_startLabel.Name = "_startLabel";
		_start.Name = "_start";
		_countLabel.Name = "_countLabel";
		_count.Name = "_count";
		_format.Name = "_format";

		_layout.Dock = DockStyle.Fill;
		_layout.Padding = new Padding(16);
		_layout.ColumnCount = 1;
		_layout.RowCount = 5;
		_layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_heading.Text = "URL-safe identifiers";
		_heading.AutoSize = true;
		_heading.Font = new Font("Segoe UI", 17F, FontStyle.Bold);
		_heading.ForeColor = Color.FromArgb(34, 61, 91);
		_heading.Margin = new Padding(0, 0, 0, 8);
		_introduction.Text = "Generate deterministic URL-safe identifiers for a numeric range. Choose Base62 or either hexadecimal byte order.";
		_introduction.AutoSize = true;
		_introduction.Dock = DockStyle.Fill;
		_introduction.Margin = new Padding(0, 0, 0, 12);
		_commands.Dock = DockStyle.Fill;
		_commands.AutoSize = true;
		_commands.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_commands.Margin = new Padding(0, 0, 0, 10);
		_status.AutoSize = true;
		_status.Dock = DockStyle.Fill;
		_status.Margin = new Padding(0, 10, 0, 0);
		_status.Text = "Ready.";
		_generateButton.Text = "Generate range";
		_generateButton.Name = "_generateButton";
		_generateButton.AutoSize = true;
		_generateButton.MinimumSize = new Size(96, 30);
		_generateButton.Click += _generateButton_Click;
		_commands.Controls.Add(_generateButton);
		_clearButton.Text = "Clear";
		_clearButton.Name = "_clearButton";
		_clearButton.AutoSize = true;
		_clearButton.MinimumSize = new Size(96, 30);
		_clearButton.Click += ClearClicked;
		_commands.Controls.Add(_clearButton);
		_startLabel.Text = "First value";
		_startLabel.AutoSize = true;
		_startLabel.Padding = new Padding(0, 8, 0, 0);
		_start.Maximum = uint.MaxValue;
		_start.Value = 1;
		_start.Width = 110;
		_countLabel.Text = "Count";
		_countLabel.AutoSize = true;
		_countLabel.Padding = new Padding(0, 8, 0, 0);
		_count.Minimum = 1;
		_count.Maximum = 10000;
		_count.Value = 20;
		_count.Width = 80;
		_format.DropDownStyle = ComboBoxStyle.DropDownList;
		_format.Items.AddRange(new object[] { "Base62", "Hex little endian", "Hex big endian" });
		_format.SelectedIndex = 0;
		_format.Width = 150;
		_commands.Controls.Add(_startLabel);
		_commands.Controls.Add(_start);
		_commands.Controls.Add(_countLabel);
		_commands.Controls.Add(_count);
		_commands.Controls.Add(_format);
		_start.ValueChanged += PreferencesChanged;
		_count.ValueChanged += PreferencesChanged;
		_format.SelectedIndexChanged += PreferencesChanged;
		_textBoxEx.Dock = DockStyle.Fill;
		_textBoxEx.Multiline = true;
		_textBoxEx.ReadOnly = true;
		_textBoxEx.ScrollBars = ScrollBars.Both;
		_textBoxEx.WordWrap = false;
		_textBoxEx.Name = "_textBoxEx";

		_layout.Controls.Add(_heading, 0, 0);
		_layout.Controls.Add(_introduction, 0, 1);
		_layout.Controls.Add(_commands, 0, 2);
		_layout.Controls.Add(_textBoxEx, 0, 3);
		_layout.Controls.Add(_status, 0, 4);
		Controls.Add(_layout);
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		Font = SystemFonts.MessageBoxFont;
		Size = new Size(1000, 700);
		MinimumSize = new Size(680, 440);
		Name = "UrlIDTestScreen";
		_commands.ResumeLayout(false);
		_commands.PerformLayout();
		_layout.ResumeLayout(false);
		_layout.PerformLayout();
		ResumeLayout(false);
	}
}
