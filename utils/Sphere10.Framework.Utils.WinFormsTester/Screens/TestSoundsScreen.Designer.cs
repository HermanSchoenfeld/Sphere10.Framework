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

partial class TestSoundsScreen {
	private TableLayoutPanel _layout;
	private Label _heading;
	private Label _introduction;
	private FlowLayoutPanel _commands;
	private Label _status;
	private Button _leftDown;
	private Button _leftUp;
	private Button _rightDown;
	private Button _rightUp;
	private Button _systemPlayerButton;
	private Button _stopButton;
	private TextBox _output;

	// Keep the complete static demonstration editable on the native design surface.
	private void InitializeComponent() {
		_layout = new TableLayoutPanel();
		_heading = new Label();
		_introduction = new Label();
		_commands = new FlowLayoutPanel();
		_status = new Label();
		_leftDown = new Button();
		_leftUp = new Button();
		_rightDown = new Button();
		_rightUp = new Button();
		_systemPlayerButton = new Button();
		_stopButton = new Button();
		_output = new TextBox();
		_layout.SuspendLayout();
		_commands.SuspendLayout();
		SuspendLayout();

		_layout.Name = "_layout";
		_heading.Name = "_heading";
		_introduction.Name = "_introduction";
		_commands.Name = "_commands";
		_status.Name = "_status";

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
		_heading.Text = "Sound playback";
		_heading.AutoSize = true;
		_heading.Font = new Font("Segoe UI", 17F, FontStyle.Bold);
		_heading.ForeColor = Color.FromArgb(34, 61, 91);
		_heading.Margin = new Padding(0, 0, 0, 8);
		_introduction.Text = "Compare four embedded mouse sounds through DirectSound and SoundPlayer. Playback begins only when you press a button.";
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
		_leftDown.Text = "Left button down";
		_leftDown.Name = "_leftDown";
		_leftDown.AutoSize = true;
		_leftDown.MinimumSize = new Size(96, 30);
		_leftDown.Click += button1_Click;
		_commands.Controls.Add(_leftDown);
		_leftUp.Text = "Left button up";
		_leftUp.Name = "_leftUp";
		_leftUp.AutoSize = true;
		_leftUp.MinimumSize = new Size(96, 30);
		_leftUp.Click += button2_Click;
		_commands.Controls.Add(_leftUp);
		_rightDown.Text = "Right button down";
		_rightDown.Name = "_rightDown";
		_rightDown.AutoSize = true;
		_rightDown.MinimumSize = new Size(96, 30);
		_rightDown.Click += button3_Click;
		_commands.Controls.Add(_rightDown);
		_rightUp.Text = "Right button up";
		_rightUp.Name = "_rightUp";
		_rightUp.AutoSize = true;
		_rightUp.MinimumSize = new Size(96, 30);
		_rightUp.Click += button4_Click;
		_commands.Controls.Add(_rightUp);
		_systemPlayerButton.Text = "SoundPlayer example";
		_systemPlayerButton.Name = "_systemPlayerButton";
		_systemPlayerButton.AutoSize = true;
		_systemPlayerButton.MinimumSize = new Size(96, 30);
		_systemPlayerButton.Click += _systemPlayerButton_Click;
		_commands.Controls.Add(_systemPlayerButton);
		_stopButton.Text = "Stop playback";
		_stopButton.Name = "_stopButton";
		_stopButton.AutoSize = true;
		_stopButton.MinimumSize = new Size(96, 30);
		_stopButton.Click += StopClicked;
		_commands.Controls.Add(_stopButton);
		_output.Dock = DockStyle.Fill;
		_output.Multiline = true;
		_output.ReadOnly = true;
		_output.ScrollBars = ScrollBars.Both;
		_output.WordWrap = false;
		_output.Name = "_output";

		_layout.Controls.Add(_heading, 0, 0);
		_layout.Controls.Add(_introduction, 0, 1);
		_layout.Controls.Add(_commands, 0, 2);
		_layout.Controls.Add(_output, 0, 3);
		_layout.Controls.Add(_status, 0, 4);
		Controls.Add(_layout);
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		Font = SystemFonts.MessageBoxFont;
		Size = new Size(1000, 700);
		MinimumSize = new Size(680, 440);
		Name = "TestSoundsScreen";
		_commands.ResumeLayout(false);
		_commands.PerformLayout();
		_layout.ResumeLayout(false);
		_layout.PerformLayout();
		ResumeLayout(false);
	}
}
