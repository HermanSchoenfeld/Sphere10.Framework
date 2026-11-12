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

partial class HexEditorTestScreen {
	private TableLayoutPanel _layout;
	private Label _heading;
	private Label _introduction;
	private FlowLayoutPanel _commands;
	private Button _openFileButton;
	private Button _openSyntheticButton;
	private Button _openSampleButton;
	private CheckBox _readOnlyCheckBox;
	private Label _statisticsLabel;
	private HexEditorControl _editor;

	// Static layout and named event wiring remain editable in the native Designer.
	private void InitializeComponent() {
		_layout = new TableLayoutPanel();
		_heading = new Label();
		_introduction = new Label();
		_commands = new FlowLayoutPanel();
		_openFileButton = new Button();
		_openSyntheticButton = new Button();
		_openSampleButton = new Button();
		_readOnlyCheckBox = new CheckBox();
		_statisticsLabel = new Label();
		_editor = new HexEditorControl();
		_layout.SuspendLayout();
		_commands.SuspendLayout();
		SuspendLayout();
		// The heading and instructions remain with this screen when it is detached.
		_layout.Dock = DockStyle.Fill;
		_layout.Padding = new Padding(20);
		_layout.ColumnCount = 1;
		_layout.RowCount = 5;
		_layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
		_layout.Controls.Add(_heading, 0, 0);
		_layout.Controls.Add(_introduction, 0, 1);
		_layout.Controls.Add(_commands, 0, 2);
		_layout.Controls.Add(_statisticsLabel, 0, 3);
		_layout.Controls.Add(_editor, 0, 4);
		_layout.Name = "_layout";
		_heading.AutoSize = true;
		_heading.Dock = DockStyle.Fill;
		_heading.Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 17F, FontStyle.Bold);
		_heading.Margin = new Padding(0, 0, 0, 10);
		_heading.Name = "_heading";
		_heading.Text = "Hex Editor";
		_introduction.AutoSize = true;
		_introduction.Dock = DockStyle.Fill;
		_introduction.Margin = new Padding(0, 0, 0, 12);
		_introduction.Name = "_introduction";
		_introduction.Text = "Browse and overwrite bytes in any seekable stream. Open a file, the small writable sample, or a 512 MiB synthetic stream whose bytes are computed on demand: the counters below show that only the visible window and a bounded block cache are ever read. Type hex digits or text in overwrite mode, use Go To (Ctrl+G), Find (Ctrl+F, F3, Shift+F3), copy as hex or text, and read the value under the caret in the data inspector. Each block begins with a readable BLK header, so searching for the text \"BLK 000000001F\" or the bytes 42 4C 4B 20 30 shows streaming search across block boundaries.";
		_commands.AutoSize = true;
		_commands.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_commands.Controls.Add(_openFileButton);
		_commands.Controls.Add(_openSyntheticButton);
		_commands.Controls.Add(_openSampleButton);
		_commands.Controls.Add(_readOnlyCheckBox);
		_commands.Dock = DockStyle.Fill;
		_commands.Margin = new Padding(0, 0, 0, 8);
		_commands.Name = "_commands";
		_commands.WrapContents = true;
		_openFileButton.AutoSize = true;
		_openFileButton.Margin = new Padding(0, 0, 8, 0);
		_openFileButton.Name = "_openFileButton";
		_openFileButton.Padding = new Padding(8, 2, 8, 2);
		_openFileButton.Text = "Open &File...";
		_openFileButton.UseVisualStyleBackColor = true;
		_openFileButton.Click += OpenFileButtonClicked;
		_openSyntheticButton.AutoSize = true;
		_openSyntheticButton.Margin = new Padding(0, 0, 8, 0);
		_openSyntheticButton.Name = "_openSyntheticButton";
		_openSyntheticButton.Padding = new Padding(8, 2, 8, 2);
		_openSyntheticButton.Text = "Open 512 MiB &Synthetic Stream";
		_openSyntheticButton.UseVisualStyleBackColor = true;
		_openSyntheticButton.Click += OpenSyntheticButtonClicked;
		_openSampleButton.AutoSize = true;
		_openSampleButton.Margin = new Padding(0, 0, 16, 0);
		_openSampleButton.Name = "_openSampleButton";
		_openSampleButton.Padding = new Padding(8, 2, 8, 2);
		_openSampleButton.Text = "Open Sample &Bytes";
		_openSampleButton.UseVisualStyleBackColor = true;
		_openSampleButton.Click += OpenSampleButtonClicked;
		_readOnlyCheckBox.Anchor = AnchorStyles.Left;
		_readOnlyCheckBox.AutoSize = true;
		_readOnlyCheckBox.Name = "_readOnlyCheckBox";
		_readOnlyCheckBox.Text = "&Read-only";
		_readOnlyCheckBox.CheckedChanged += ReadOnlyCheckBoxChanged;
		_statisticsLabel.AutoSize = true;
		_statisticsLabel.Dock = DockStyle.Fill;
		_statisticsLabel.Margin = new Padding(0, 0, 0, 8);
		_statisticsLabel.Name = "_statisticsLabel";
		_statisticsLabel.Text = "No document open.";
		_editor.Dock = DockStyle.Fill;
		_editor.Margin = new Padding(0);
		_editor.Name = "_editor";
		Font = SystemFonts.MessageBoxFont;
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		BackColor = SystemColors.Window;
		Controls.Add(_layout);
		Name = "HexEditorTestScreen";
		Size = new Size(1100, 760);
		Text = "Hex Editor";
		_layout.ResumeLayout(false);
		_layout.PerformLayout();
		_commands.ResumeLayout(false);
		_commands.PerformLayout();
		ResumeLayout(false);
	}
}
