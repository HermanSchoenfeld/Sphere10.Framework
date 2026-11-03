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

partial class ConnectionPanelTestScreen {
	private TableLayoutPanel _layout;
	private Label _heading;
	private Label _introduction;
	private FlowLayoutPanel _commands;
	private Label _status;
	private DatabaseConnectionPanel _databaseConnectionPanel;
	private Button _testConnectionButton;
	private Button _saveButton;
	private Button _cancelButton;
	private LoadingCircle _loadingCircle;
	private TableLayoutPanel _content;
	private TextBox _summary;

	// Keep the complete static demonstration editable on the native design surface.
	private void InitializeComponent() {
		_layout = new TableLayoutPanel();
		_heading = new Label();
		_introduction = new Label();
		_commands = new FlowLayoutPanel();
		_status = new Label();
		_databaseConnectionPanel = new DatabaseConnectionPanel();
		_testConnectionButton = new Button();
		_saveButton = new Button();
		_cancelButton = new Button();
		_loadingCircle = new LoadingCircle();
		_content = new TableLayoutPanel();
		_summary = new TextBox();
		_layout.SuspendLayout();
		_commands.SuspendLayout();
		SuspendLayout();

		_layout.Name = "_layout";
		_heading.Name = "_heading";
		_introduction.Name = "_introduction";
		_commands.Name = "_commands";
		_status.Name = "_status";
		_databaseConnectionPanel.Name = "_databaseConnectionPanel";
		_loadingCircle.Name = "_loadingCircle";
		_content.Name = "_content";

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
		_heading.Text = "Database connection panel";
		_heading.AutoSize = true;
		_heading.Font = new Font("Segoe UI", 17F, FontStyle.Bold);
		_heading.ForeColor = Color.FromArgb(34, 61, 91);
		_heading.Margin = new Padding(0, 0, 0, 8);
		_introduction.Text = "Configure a provider, test its connection explicitly, then accept or restore the local editor draft. No database is contacted when this screen opens.";
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
		_testConnectionButton.Text = "Test connection";
		_testConnectionButton.Name = "_testConnectionButton";
		_testConnectionButton.AutoSize = true;
		_testConnectionButton.MinimumSize = new Size(96, 30);
		_testConnectionButton.Click += _testConnectionButton_Click;
		_commands.Controls.Add(_testConnectionButton);
		_saveButton.Text = "Accept draft";
		_saveButton.Name = "_saveButton";
		_saveButton.AutoSize = true;
		_saveButton.MinimumSize = new Size(96, 30);
		_saveButton.Click += AcceptClicked;
		_commands.Controls.Add(_saveButton);
		_cancelButton.Text = "Restore draft";
		_cancelButton.Name = "_cancelButton";
		_cancelButton.AutoSize = true;
		_cancelButton.MinimumSize = new Size(96, 30);
		_cancelButton.Click += RestoreClicked;
		_commands.Controls.Add(_cancelButton);
		_content.Dock = DockStyle.Fill;
		_content.ColumnCount = 1;
		_content.RowCount = 2;
		_content.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
		_content.RowStyles.Add(new RowStyle(SizeType.Absolute, 260));
		_content.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
		_databaseConnectionPanel.Dock = DockStyle.Fill;
		_content.Controls.Add(_databaseConnectionPanel, 0, 0);
		_content.Controls.Add(_summary, 0, 1);
		_loadingCircle.Size = new Size(28, 28);
		_loadingCircle.Visible = false;
		_commands.Controls.Add(_loadingCircle);
		_summary.Dock = DockStyle.Fill;
		_summary.Multiline = true;
		_summary.ReadOnly = true;
		_summary.ScrollBars = ScrollBars.Both;
		_summary.WordWrap = false;
		_summary.Name = "_summary";

		_layout.Controls.Add(_heading, 0, 0);
		_layout.Controls.Add(_introduction, 0, 1);
		_layout.Controls.Add(_commands, 0, 2);
		_layout.Controls.Add(_content, 0, 3);
		_layout.Controls.Add(_status, 0, 4);
		Controls.Add(_layout);
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		Font = SystemFonts.MessageBoxFont;
		Size = new Size(1000, 700);
		MinimumSize = new Size(680, 440);
		Name = "ConnectionPanelTestScreen";
		_commands.ResumeLayout(false);
		_commands.PerformLayout();
		_layout.ResumeLayout(false);
		_layout.PerformLayout();
		ResumeLayout(false);
	}
}
