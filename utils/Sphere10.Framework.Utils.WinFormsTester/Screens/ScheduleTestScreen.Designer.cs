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

partial class ScheduleTestScreen {
	private TableLayoutPanel _layout;
	private Label _heading;
	private Label _introduction;
	private FlowLayoutPanel _commands;
	private Label _status;
	private Button _job1Button;
	private Button TestButton;
	private Button SyncStartButton;
	private Button SyncStopButton;
	private Button AsyncTestButton;
	private Button AsyncStartButton;
	private Button AsyncStopButton;
	private Button SyncSerializeButton;
	private Button XmlSerializeButton;
	private Button _clearButton;
	private TextBox textBox1;

	// Keep the complete static demonstration editable on the native design surface.
	private void InitializeComponent() {
		_layout = new TableLayoutPanel();
		_heading = new Label();
		_introduction = new Label();
		_commands = new FlowLayoutPanel();
		_status = new Label();
		_job1Button = new Button();
		TestButton = new Button();
		SyncStartButton = new Button();
		SyncStopButton = new Button();
		AsyncTestButton = new Button();
		AsyncStartButton = new Button();
		AsyncStopButton = new Button();
		SyncSerializeButton = new Button();
		XmlSerializeButton = new Button();
		_clearButton = new Button();
		textBox1 = new TextBox();
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
		_heading.Text = "Owned job scheduling";
		_heading.AutoSize = true;
		_heading.Font = new Font("Segoe UI", 17F, FontStyle.Bold);
		_heading.ForeColor = Color.FromArgb(34, 61, 91);
		_heading.Margin = new Padding(0, 0, 0, 8);
		_introduction.Text = "Create bounded synchronous or asynchronous job schedules, start/stop them, and compare surrogate/XML persistence. This screen owns its schedulers.";
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
		_job1Button.Text = "Start sample pair";
		_job1Button.Name = "_job1Button";
		_job1Button.AutoSize = true;
		_job1Button.MinimumSize = new Size(96, 30);
		_job1Button.Click += _job1Button_Click;
		_commands.Controls.Add(_job1Button);
		TestButton.Text = "Create synchronous";
		TestButton.Name = "TestButton";
		TestButton.AutoSize = true;
		TestButton.MinimumSize = new Size(96, 30);
		TestButton.Click += TestButton_Click;
		_commands.Controls.Add(TestButton);
		SyncStartButton.Text = "Start sync";
		SyncStartButton.Name = "SyncStartButton";
		SyncStartButton.AutoSize = true;
		SyncStartButton.MinimumSize = new Size(96, 30);
		SyncStartButton.Click += SyncStartButton_Click;
		_commands.Controls.Add(SyncStartButton);
		SyncStopButton.Text = "Stop sync";
		SyncStopButton.Name = "SyncStopButton";
		SyncStopButton.AutoSize = true;
		SyncStopButton.MinimumSize = new Size(96, 30);
		SyncStopButton.Click += SyncStopButton_Click;
		_commands.Controls.Add(SyncStopButton);
		AsyncTestButton.Text = "Create asynchronous";
		AsyncTestButton.Name = "AsyncTestButton";
		AsyncTestButton.AutoSize = true;
		AsyncTestButton.MinimumSize = new Size(96, 30);
		AsyncTestButton.Click += AsyncTestButton_Click;
		_commands.Controls.Add(AsyncTestButton);
		AsyncStartButton.Text = "Start async";
		AsyncStartButton.Name = "AsyncStartButton";
		AsyncStartButton.AutoSize = true;
		AsyncStartButton.MinimumSize = new Size(96, 30);
		AsyncStartButton.Click += AsyncStartButton_Click;
		_commands.Controls.Add(AsyncStartButton);
		AsyncStopButton.Text = "Stop async";
		AsyncStopButton.Name = "AsyncStopButton";
		AsyncStopButton.AutoSize = true;
		AsyncStopButton.MinimumSize = new Size(96, 30);
		AsyncStopButton.Click += AsyncStopButton_Click;
		_commands.Controls.Add(AsyncStopButton);
		SyncSerializeButton.Text = "Surrogate round trip";
		SyncSerializeButton.Name = "SyncSerializeButton";
		SyncSerializeButton.AutoSize = true;
		SyncSerializeButton.MinimumSize = new Size(96, 30);
		SyncSerializeButton.Click += SyncSerializeButton_Click;
		_commands.Controls.Add(SyncSerializeButton);
		XmlSerializeButton.Text = "XML round trip";
		XmlSerializeButton.Name = "XmlSerializeButton";
		XmlSerializeButton.AutoSize = true;
		XmlSerializeButton.MinimumSize = new Size(96, 30);
		XmlSerializeButton.Click += XmlSerializeButton_Click;
		_commands.Controls.Add(XmlSerializeButton);
		_clearButton.Text = "Clear log";
		_clearButton.Name = "_clearButton";
		_clearButton.AutoSize = true;
		_clearButton.MinimumSize = new Size(96, 30);
		_clearButton.Click += ClearClicked;
		_commands.Controls.Add(_clearButton);
		textBox1.Dock = DockStyle.Fill;
		textBox1.Multiline = true;
		textBox1.ReadOnly = true;
		textBox1.ScrollBars = ScrollBars.Both;
		textBox1.WordWrap = false;
		textBox1.Name = "textBox1";

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
		Name = "ScheduleTestScreen";
		_commands.ResumeLayout(false);
		_commands.PerformLayout();
		_layout.ResumeLayout(false);
		_layout.PerformLayout();
		ResumeLayout(false);
	}
}
