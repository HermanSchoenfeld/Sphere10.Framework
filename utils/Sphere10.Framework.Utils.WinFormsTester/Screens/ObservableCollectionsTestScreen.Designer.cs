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

partial class ObservableCollectionsTestScreen {
	private TableLayoutPanel _layout;
	private Label _heading;
	private Label _introduction;
	private FlowLayoutPanel _inputs;
	private FlowLayoutPanel _commands;
	private TextBoxEx _textBoxEx1;
	private Label _status;
	private CheckBox _includeAccess;
	private Button _listTestButton;
	private Button _dictionaryTestButton;
	private Button _clearButton;

	// The complete static demonstration layout remains editable on the designer surface.
	private void InitializeComponent() {
		_layout = new TableLayoutPanel();
		_heading = new Label();
		_introduction = new Label();
		_inputs = new FlowLayoutPanel();
		_commands = new FlowLayoutPanel();
		_textBoxEx1 = new TextBoxEx();
		_status = new Label();
		_includeAccess = new CheckBox();
		_listTestButton = new Button();
		_dictionaryTestButton = new Button();
		_clearButton = new Button();
		_layout.SuspendLayout();
		_inputs.SuspendLayout();
		_commands.SuspendLayout();
		SuspendLayout();

		_layout.Dock = DockStyle.Fill;
		_layout.ColumnCount = 1;
		_layout.RowCount = 6;
		_layout.Padding = new Padding(20);
		_layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.Controls.Add(_heading, 0, 0);
		_layout.Controls.Add(_introduction, 0, 1);
		_layout.Controls.Add(_inputs, 0, 2);
		_layout.Controls.Add(_commands, 0, 3);
		_layout.Controls.Add(_textBoxEx1, 0, 4);
		_layout.Controls.Add(_status, 0, 5);

		_heading.Text = "Observable Collections";
		_heading.AutoSize = true;
		_heading.Font = new Font("Segoe UI", 17F, FontStyle.Bold);
		_heading.Margin = new Padding(0, 0, 0, 12);
		_introduction.Text = "Run the list or dictionary sequence, then inspect the current membership and event order. Range operations remain distinct from individual mutations.";
		_introduction.AutoSize = true;
		_introduction.Dock = DockStyle.Fill;
		_introduction.Margin = new Padding(0, 0, 0, 16);

		_inputs.AutoSize = true;
		_inputs.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_inputs.Dock = DockStyle.Fill;
		_inputs.Margin = new Padding(0, 0, 0, 10);

		_commands.AutoSize = true;
		_commands.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_commands.Dock = DockStyle.Fill;
		_commands.Margin = new Padding(0, 0, 0, 10);

		_includeAccess.Name = "_includeAccess";
		_includeAccess.Text = "Include access notifications";
		_includeAccess.AutoSize = true;
		_includeAccess.Margin = new Padding(0, 3, 12, 3);
		_inputs.Controls.Add(_includeAccess);
		_includeAccess.CheckedChanged += PreferencesEdited;

		_listTestButton.Name = "_listTestButton";
		_listTestButton.Text = "Run list sequence";
		_listTestButton.AutoSize = true;
		_listTestButton.MinimumSize = new Size(110, 32);
		_listTestButton.Margin = new Padding(0, 0, 8, 0);
		_listTestButton.Click += ListClicked;
		_commands.Controls.Add(_listTestButton);

		_dictionaryTestButton.Name = "_dictionaryTestButton";
		_dictionaryTestButton.Text = "Run dictionary sequence";
		_dictionaryTestButton.AutoSize = true;
		_dictionaryTestButton.MinimumSize = new Size(110, 32);
		_dictionaryTestButton.Margin = new Padding(0, 0, 8, 0);
		_dictionaryTestButton.Click += DictionaryClicked;
		_commands.Controls.Add(_dictionaryTestButton);

		_clearButton.Name = "_clearButton";
		_clearButton.Text = "Clear results";
		_clearButton.AutoSize = true;
		_clearButton.MinimumSize = new Size(110, 32);
		_clearButton.Margin = new Padding(0, 0, 8, 0);
		_clearButton.Click += ClearClicked;
		_commands.Controls.Add(_clearButton);

		_textBoxEx1.Name = "textBoxEx1";
		_textBoxEx1.Dock = DockStyle.Fill;
		_textBoxEx1.Multiline = true;
		_textBoxEx1.ReadOnly = true;
		_textBoxEx1.ScrollBars = ScrollBars.Both;
		_textBoxEx1.WordWrap = false;
		_textBoxEx1.BackColor = SystemColors.Window;
		_textBoxEx1.Font = new Font("Consolas", 9F);
		_textBoxEx1.Margin = new Padding(0, 0, 0, 10);
		_status.Name = "showcaseStatus";
		_status.AutoSize = true;
		_status.Dock = DockStyle.Fill;
		_status.Text = "Ready. Choose an example to see its results.";

		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		Font = SystemFonts.MessageBoxFont;
		BackColor = SystemColors.Window;
		Name = "ObservableCollectionsTestScreen";
		Size = new Size(1000, 720);
		Controls.Add(_layout);
		_commands.ResumeLayout(false);
		_commands.PerformLayout();
		_inputs.ResumeLayout(false);
		_inputs.PerformLayout();
		_layout.ResumeLayout(false);
		_layout.PerformLayout();
		ResumeLayout(false);
	}
}
