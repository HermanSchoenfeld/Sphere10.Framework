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

partial class EncryptedCompressionTestScreen {
	private TableLayoutPanel _layout;
	private Label _heading;
	private Label _introduction;
	private FlowLayoutPanel _commands;
	private Label _status;
	private Button _testButton;
	private Button _cancelButton;
	private PathSelectorControl fileSelectorControl1;
	private TextBox textBox1;
	private TextBox _passwordTextBox;
	private FlowLayoutPanel _fileFields;
	private Label _fileLabel;
	private FlowLayoutPanel _passwordFields;
	private Label _passwordLabel;

	// Keep the complete static demonstration editable on the native design surface.
	private void InitializeComponent() {
		_layout = new TableLayoutPanel();
		_heading = new Label();
		_introduction = new Label();
		_commands = new FlowLayoutPanel();
		_status = new Label();
		_testButton = new Button();
		_cancelButton = new Button();
		fileSelectorControl1 = new PathSelectorControl();
		textBox1 = new TextBox();
		_passwordTextBox = new TextBox();
		_fileFields = new FlowLayoutPanel();
		_fileLabel = new Label();
		_passwordFields = new FlowLayoutPanel();
		_passwordLabel = new Label();
		_layout.SuspendLayout();
		_commands.SuspendLayout();
		SuspendLayout();

		_layout.Name = "_layout";
		_heading.Name = "_heading";
		_introduction.Name = "_introduction";
		_commands.Name = "_commands";
		_status.Name = "_status";
		_passwordTextBox.Name = "_passwordTextBox";

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
		_heading.Text = "Encrypted compression";
		_heading.AutoSize = true;
		_heading.Font = new Font("Segoe UI", 17F, FontStyle.Bold);
		_heading.ForeColor = Color.FromArgb(34, 61, 91);
		_heading.Margin = new Padding(0, 0, 0, 8);
		_introduction.Text = "Choose a file and explicitly verify a compressed round trip. Work runs off the UI thread; cancellation is checked between file operations.";
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
		_testButton.Text = "Verify round trip";
		_testButton.Name = "_testButton";
		_testButton.AutoSize = true;
		_testButton.MinimumSize = new Size(96, 30);
		_testButton.Click += _testButton_Click;
		_commands.Controls.Add(_testButton);
		_cancelButton.Text = "Cancel";
		_cancelButton.Name = "_cancelButton";
		_cancelButton.AutoSize = true;
		_cancelButton.MinimumSize = new Size(96, 30);
		_cancelButton.Click += CancelClicked;
		_commands.Controls.Add(_cancelButton);
		_fileFields.Name = "_fileFields";
		_fileFields.AutoSize = true;
		_fileFields.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_fileFields.WrapContents = false;
		_fileFields.Margin = new Padding(0);
		_fileLabel.Name = "_fileLabel";
		_fileLabel.Text = "Input file";
		_fileLabel.AutoSize = true;
		_fileLabel.Padding = new Padding(0, 6, 0, 0);
		fileSelectorControl1.Width = 280;
		fileSelectorControl1.Name = "fileSelectorControl1";
		_fileFields.Controls.Add(_fileLabel);
		_fileFields.Controls.Add(fileSelectorControl1);
		_commands.Controls.Add(_fileFields);
		_passwordFields.Name = "_passwordFields";
		_passwordFields.AutoSize = true;
		_passwordFields.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_passwordFields.WrapContents = false;
		_passwordFields.Margin = new Padding(0);
		_passwordLabel.Name = "_passwordLabel";
		_passwordLabel.Text = "Password";
		_passwordLabel.AutoSize = true;
		_passwordLabel.Padding = new Padding(0, 6, 0, 0);
		_passwordTextBox.Width = 160;
		_passwordTextBox.UseSystemPasswordChar = true;
		_passwordTextBox.PlaceholderText = "Session password";
		_passwordFields.Controls.Add(_passwordLabel);
		_passwordFields.Controls.Add(_passwordTextBox);
		_commands.Controls.Add(_passwordFields);
		textBox1.Dock = DockStyle.Fill;
		textBox1.Multiline = true;
		textBox1.ReadOnly = true;
		textBox1.ScrollBars = ScrollBars.Both;
		textBox1.WordWrap = false;
		textBox1.Name = "textBox1";
		_cancelButton.Enabled = false;

		_layout.Controls.Add(_heading, 0, 0);
		_layout.Controls.Add(_introduction, 0, 1);
		_layout.Controls.Add(_commands, 0, 2);
		_layout.Controls.Add(textBox1, 0, 3);
		_layout.Controls.Add(_status, 0, 4);
		Controls.Add(_layout);
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		Font = SystemFonts.MessageBoxFont;
		Size = new Size(1000, 700);
		MinimumSize = new Size(680, 440);
		Name = "EncryptedCompressionTestScreen";
		_commands.ResumeLayout(false);
		_commands.PerformLayout();
		_layout.ResumeLayout(false);
		_layout.PerformLayout();
		ResumeLayout(false);
	}
}
