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

partial class SettingsTest {
	private TableLayoutPanel _layout;
	private Label _heading;
	private Label _introduction;
	private FlowLayoutPanel _commands;
	private Label _status;
	private Button _loadButton;
	private Button _saveButton;
	private CheckBox _globalScope;
	private Label _languageLabel;
	private TextBox _language;
	private Label _counterLabel;
	private NumericUpDown _counter;
	private TextBox _secret;
	private FlowLayoutPanel _secretFields;
	private Label _secretLabel;
	private TextBox _outputTextBox;

	// Keep the complete static demonstration editable on the native design surface.
	private void InitializeComponent() {
		_layout = new TableLayoutPanel();
		_heading = new Label();
		_introduction = new Label();
		_commands = new FlowLayoutPanel();
		_status = new Label();
		_loadButton = new Button();
		_saveButton = new Button();
		_globalScope = new CheckBox();
		_languageLabel = new Label();
		_language = new TextBox();
		_counterLabel = new Label();
		_counter = new NumericUpDown();
		_secret = new TextBox();
		_secretFields = new FlowLayoutPanel();
		_secretLabel = new Label();
		_outputTextBox = new TextBox();
		_layout.SuspendLayout();
		_commands.SuspendLayout();
		SuspendLayout();

		_layout.Name = "_layout";
		_heading.Name = "_heading";
		_introduction.Name = "_introduction";
		_commands.Name = "_commands";
		_status.Name = "_status";
		_globalScope.Name = "_globalScope";
		_languageLabel.Name = "_languageLabel";
		_language.Name = "_language";
		_counterLabel.Name = "_counterLabel";
		_counter.Name = "_counter";
		_secret.Name = "_secret";

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
		_heading.Text = "Screen and standalone settings";
		_heading.AutoSize = true;
		_heading.Font = new Font("Segoe UI", 17F, FontStyle.Bold);
		_heading.ForeColor = Color.FromArgb(34, 61, 91);
		_heading.Margin = new Padding(0, 0, 0, 8);
		_introduction.Text = "This screen declares its settings with [Settings(typeof(...))]. Use Screen Options to change its default scope. The controls below edit a separate standalone user/global record.";
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
		_loadButton.Text = "Load selected scope";
		_loadButton.Name = "_loadButton";
		_loadButton.AutoSize = true;
		_loadButton.MinimumSize = new Size(96, 30);
		_loadButton.Click += LoadClicked;
		_commands.Controls.Add(_loadButton);
		_saveButton.Text = "Save selected scope";
		_saveButton.Name = "_saveButton";
		_saveButton.AutoSize = true;
		_saveButton.MinimumSize = new Size(96, 30);
		_saveButton.Click += SaveClicked;
		_commands.Controls.Add(_saveButton);
		_globalScope.Text = "Global scope";
		_globalScope.AutoSize = true;
		_languageLabel.Text = "Language";
		_languageLabel.AutoSize = true;
		_languageLabel.Padding = new Padding(0, 8, 0, 0);
		_language.Text = "en";
		_language.Width = 70;
		_counterLabel.Text = "Counter";
		_counterLabel.AutoSize = true;
		_counterLabel.Padding = new Padding(0, 8, 0, 0);
		_counter.Maximum = int.MaxValue;
		_counter.Width = 90;
		_secret.UseSystemPasswordChar = true;
		_secret.PlaceholderText = "Optional encrypted demo value";
		_secret.Width = 220;
		_secretFields.Name = "_secretFields";
		_secretFields.AutoSize = true;
		_secretFields.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_secretFields.WrapContents = false;
		_secretFields.Margin = new Padding(0);
		_secretLabel.Name = "_secretLabel";
		_secretLabel.Text = "Encrypted value";
		_secretLabel.AutoSize = true;
		_secretLabel.Padding = new Padding(0, 6, 0, 0);
		_commands.Controls.Add(_globalScope);
		_commands.Controls.Add(_languageLabel);
		_commands.Controls.Add(_language);
		_commands.Controls.Add(_counterLabel);
		_commands.Controls.Add(_counter);
		_secretFields.Controls.Add(_secretLabel);
		_secretFields.Controls.Add(_secret);
		_commands.Controls.Add(_secretFields);
		_globalScope.CheckedChanged += ScopeChanged;
		_outputTextBox.Dock = DockStyle.Fill;
		_outputTextBox.Multiline = true;
		_outputTextBox.ReadOnly = true;
		_outputTextBox.ScrollBars = ScrollBars.Both;
		_outputTextBox.WordWrap = false;
		_outputTextBox.Name = "_outputTextBox";

		_layout.Controls.Add(_heading, 0, 0);
		_layout.Controls.Add(_introduction, 0, 1);
		_layout.Controls.Add(_commands, 0, 2);
		_layout.Controls.Add(_outputTextBox, 0, 3);
		_layout.Controls.Add(_status, 0, 4);
		Controls.Add(_layout);
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		Font = SystemFonts.MessageBoxFont;
		Size = new Size(1000, 700);
		MinimumSize = new Size(680, 440);
		Name = "SettingsTest";
		_commands.ResumeLayout(false);
		_commands.PerformLayout();
		_layout.ResumeLayout(false);
		_layout.PerformLayout();
		ResumeLayout(false);
	}
}
