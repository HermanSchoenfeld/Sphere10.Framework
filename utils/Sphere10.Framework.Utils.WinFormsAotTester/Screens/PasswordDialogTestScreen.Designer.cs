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

partial class PasswordDialogTestScreen {
	private TableLayoutPanel _layout;
	private Label _heading;
	private Label _introduction;
	private FlowLayoutPanel _actions;
	private Button _standardButton;
	private Button _customButton;
	private Button _logonButton;
	private Label _policySummary;
	private TextBox _outputTextBox;

	// Static layout and named event wiring remain editable in the native Designer.
	private void InitializeComponent() {
		_layout = new TableLayoutPanel();
		_layout.Name = "_layout";
		_heading = new Label();
		_heading.Name = "_heading";
		_introduction = new Label();
		_introduction.Name = "_introduction";
		_actions = new FlowLayoutPanel();
		_actions.Name = "_actions";
		_standardButton = new Button();
		_standardButton.Name = "_standardButton";
		_customButton = new Button();
		_customButton.Name = "_customButton";
		_logonButton = new Button();
		_logonButton.Name = "_logonButton";
		_policySummary = new Label();
		_policySummary.Name = "_policySummary";
		_outputTextBox = new TextBox();
		_layout.SuspendLayout();
		SuspendLayout();

		// The heading and instructions remain with this screen when it is detached.
		_layout.Dock = DockStyle.Fill;
		_layout.Padding = new Padding(20);
		_layout.ColumnCount = 1;
		_layout.RowCount = 5;
		_layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_heading.AutoSize = true;
		_heading.Dock = DockStyle.Fill;
		_heading.Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 17F, FontStyle.Bold);
		_heading.Margin = new Padding(0, 0, 0, 10);
		_heading.Text = "Password And Sign-in Dialogs";
		_introduction.AutoSize = true;
		_introduction.Dock = DockStyle.Fill;
		_introduction.Margin = new Padding(0, 0, 0, 18);
		_introduction.Text = "Explore standard, validated and asynchronous authentication dialogs. Only outcomes are shown here; entered passwords are never printed or stored.";
		_layout.Controls.Add(_heading, 0, 0);
		_layout.Controls.Add(_introduction, 0, 1);

		_standardButton.AutoSize = true;
		_standardButton.MinimumSize = new Size(110, 34);
		_standardButton.Text = "Standard password";
		_standardButton.UseVisualStyleBackColor = true;
		_standardButton.Click += StandardButton_Click;
		_customButton.AutoSize = true;
		_customButton.MinimumSize = new Size(110, 34);
		_customButton.Text = "Validated password";
		_customButton.UseVisualStyleBackColor = true;
		_customButton.Click += CustomButton_Click;
		_logonButton.AutoSize = true;
		_logonButton.MinimumSize = new Size(110, 34);
		_logonButton.Text = "Simulated sign in";
		_logonButton.UseVisualStyleBackColor = true;
		_logonButton.Click += LogonButton_Click;
		_actions.AutoSize = true;
		_actions.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_actions.Dock = DockStyle.Fill;
		_actions.Margin = Padding.Empty;
		_actions.Controls.AddRange(new Control[] { _standardButton, _customButton, _logonButton });
		_policySummary.AutoSize = true;
		_policySummary.Dock = DockStyle.Fill;
		_policySummary.Margin = new Padding(0, 8, 0, 8);
		_policySummary.Text = "Custom policy: at least 5 characters, one uppercase letter, one lowercase letter and one digit; no whitespace. Local sign in accepts matching username/password; an empty username simulates an unavailable server.";
		_outputTextBox.Name = "_outputTextBox";
		_outputTextBox.Dock = DockStyle.Fill;
		_outputTextBox.Multiline = true;
		_outputTextBox.ReadOnly = true;
		_outputTextBox.ScrollBars = ScrollBars.Both;
		_outputTextBox.WordWrap = false;
		_outputTextBox.BackColor = SystemColors.Window;

		// Content-sized command rows keep their complete buttons visible at smaller widths.
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.Controls.Add(_actions, 0, 2);
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.Controls.Add(_policySummary, 0, 3);
		_layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
		_layout.Controls.Add(_outputTextBox, 0, 4);
		Font = SystemFonts.MessageBoxFont;
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		BackColor = SystemColors.Window;
		Name = "PasswordDialogTestScreen";
		Text = "Password And Sign-in Dialogs";
		Size = new Size(1000, 720);
		Controls.Add(_layout);
		_layout.ResumeLayout(false);
		_layout.PerformLayout();
		ResumeLayout(false);
	}
}
