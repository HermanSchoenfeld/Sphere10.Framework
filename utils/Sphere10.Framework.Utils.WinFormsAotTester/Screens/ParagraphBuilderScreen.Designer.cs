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

partial class ParagraphBuilderScreen {
	private TableLayoutPanel _layout;
	private Label _heading;
	private Label _introduction;
	private FlowLayoutPanel _commands;
	private Label _status;
	private TextBox _sentenceTextBox;
	private CheckBox _formatSentence;
	private Button _appendSentenceButton;
	private Button _appendBreakButton;
	private Button _clearButton;
	private Button _copyButton;
	private TextBox _paragraphTextBox;

	// Keep the complete static demonstration editable on the native design surface.
	private void InitializeComponent() {
		_layout = new TableLayoutPanel();
		_heading = new Label();
		_introduction = new Label();
		_commands = new FlowLayoutPanel();
		_status = new Label();
		_sentenceTextBox = new TextBox();
		_formatSentence = new CheckBox();
		_appendSentenceButton = new Button();
		_appendBreakButton = new Button();
		_clearButton = new Button();
		_copyButton = new Button();
		_paragraphTextBox = new TextBox();
		_layout.SuspendLayout();
		_commands.SuspendLayout();
		SuspendLayout();

		_layout.Name = "_layout";
		_heading.Name = "_heading";
		_introduction.Name = "_introduction";
		_commands.Name = "_commands";
		_status.Name = "_status";
		_formatSentence.Name = "_formatSentence";

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
		_heading.Text = "Paragraph builder";
		_heading.AutoSize = true;
		_heading.Font = new Font("Segoe UI", 17F, FontStyle.Bold);
		_heading.ForeColor = Color.FromArgb(34, 61, 91);
		_heading.Margin = new Padding(0, 0, 0, 8);
		_introduction.Text = "Append sentences with optional punctuation formatting, insert paragraph breaks, then inspect or copy the complete text.";
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
		_appendSentenceButton.Text = "Append sentence";
		_appendSentenceButton.Name = "_appendSentenceButton";
		_appendSentenceButton.AutoSize = true;
		_appendSentenceButton.MinimumSize = new Size(96, 30);
		_appendSentenceButton.Click += _appendSentenceButton_Click;
		_commands.Controls.Add(_appendSentenceButton);
		_appendBreakButton.Text = "Paragraph break";
		_appendBreakButton.Name = "_appendBreakButton";
		_appendBreakButton.AutoSize = true;
		_appendBreakButton.MinimumSize = new Size(96, 30);
		_appendBreakButton.Click += _appendBreakButton_Click;
		_commands.Controls.Add(_appendBreakButton);
		_clearButton.Text = "Clear paragraph";
		_clearButton.Name = "_clearButton";
		_clearButton.AutoSize = true;
		_clearButton.MinimumSize = new Size(96, 30);
		_clearButton.Click += ClearClicked;
		_commands.Controls.Add(_clearButton);
		_copyButton.Text = "Copy paragraph";
		_copyButton.Name = "_copyButton";
		_copyButton.AutoSize = true;
		_copyButton.MinimumSize = new Size(96, 30);
		_copyButton.Click += CopyClicked;
		_commands.Controls.Add(_copyButton);
		_sentenceTextBox.Width = 320;
		_sentenceTextBox.Text = "the framework joins sentences consistently";
		_sentenceTextBox.Name = "_sentenceTextBox";
		_formatSentence.Text = "Format punctuation";
		_formatSentence.Checked = true;
		_formatSentence.AutoSize = true;
		_commands.Controls.Add(_sentenceTextBox);
		_commands.Controls.Add(_formatSentence);
		_formatSentence.CheckedChanged += PreferencesChanged;
		_paragraphTextBox.Dock = DockStyle.Fill;
		_paragraphTextBox.Multiline = true;
		_paragraphTextBox.ReadOnly = true;
		_paragraphTextBox.ScrollBars = ScrollBars.Both;
		_paragraphTextBox.WordWrap = false;
		_paragraphTextBox.Name = "_paragraphTextBox";
		_paragraphTextBox.WordWrap = true;

		_layout.Controls.Add(_heading, 0, 0);
		_layout.Controls.Add(_introduction, 0, 1);
		_layout.Controls.Add(_commands, 0, 2);
		_layout.Controls.Add(_paragraphTextBox, 0, 3);
		_layout.Controls.Add(_status, 0, 4);
		Controls.Add(_layout);
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		Font = SystemFonts.MessageBoxFont;
		Size = new Size(1000, 700);
		MinimumSize = new Size(680, 440);
		Name = "ParagraphBuilderScreen";
		_commands.ResumeLayout(false);
		_commands.PerformLayout();
		_layout.ResumeLayout(false);
		_layout.PerformLayout();
		ResumeLayout(false);
	}
}
