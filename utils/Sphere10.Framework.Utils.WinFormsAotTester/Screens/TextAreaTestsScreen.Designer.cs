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

partial class TextAreaTestsScreen {
	private TableLayoutPanel _layout;
	private Label _heading;
	private Label _introduction;
	private FlowLayoutPanel _actions;
	private NumericUpDown _sizeKilobytes;
	private Label _sizeLabel;
	private Button _fillStandardButton;
	private Button _fillLockedButton;
	private Button _fillLockedAsyncButton;
	private Button _genOnlyButton;
	private Button _epasaPwdCharsButton;
	private Button _epasaRegexButton;
	private TextBox _standardTextBox;
	private Label _timing;

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
		_sizeKilobytes = new NumericUpDown();
		_sizeLabel = new Label();
		_sizeLabel.Name = "_sizeLabel";
		_fillStandardButton = new Button();
		_fillStandardButton.Name = "_fillStandardButton";
		_fillLockedButton = new Button();
		_fillLockedButton.Name = "_fillLockedButton";
		_fillLockedAsyncButton = new Button();
		_fillLockedAsyncButton.Name = "_fillLockedAsyncButton";
		_genOnlyButton = new Button();
		_genOnlyButton.Name = "_genOnlyButton";
		_epasaPwdCharsButton = new Button();
		_epasaPwdCharsButton.Name = "_epasaPwdCharsButton";
		_epasaRegexButton = new Button();
		_epasaRegexButton.Name = "_epasaRegexButton";
		_standardTextBox = new TextBox();
		_timing = new Label();
		_timing.Name = "_timing";
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
		_heading.Text = "Text Area Laboratory";
		_introduction.AutoSize = true;
		_introduction.Dock = DockStyle.Fill;
		_introduction.Margin = new Padding(0, 0, 0, 18);
		_introduction.Text = "Compare ordinary appending, deferred rendering and background generation at a chosen size. The character and EPASA tools retain the original escaping and regular-expression examples.";
		_layout.Controls.Add(_heading, 0, 0);
		_layout.Controls.Add(_introduction, 0, 1);

		_sizeLabel.AutoSize = true;
		_sizeLabel.Dock = DockStyle.Fill;
		_sizeLabel.Margin = new Padding(0, 8, 0, 8);
		_sizeLabel.Text = "Output size (KiB):";
		_sizeKilobytes.Name = "textSizeKilobytes";
		_sizeKilobytes.Minimum = 1;
		_sizeKilobytes.Maximum = 1024;
		_sizeKilobytes.Value = 32;
		_sizeKilobytes.Width = 80;
		_fillStandardButton.AutoSize = true;
		_fillStandardButton.MinimumSize = new Size(110, 34);
		_fillStandardButton.Text = "Append normally";
		_fillStandardButton.UseVisualStyleBackColor = true;
		_fillStandardButton.Click += FillStandardButton_Click;
		_fillLockedButton.AutoSize = true;
		_fillLockedButton.MinimumSize = new Size(110, 34);
		_fillLockedButton.Text = "Append in rendering scope";
		_fillLockedButton.UseVisualStyleBackColor = true;
		_fillLockedButton.Click += FillLockedButton_Click;
		_fillLockedAsyncButton.AutoSize = true;
		_fillLockedAsyncButton.MinimumSize = new Size(110, 34);
		_fillLockedAsyncButton.Text = "Generate on worker";
		_fillLockedAsyncButton.UseVisualStyleBackColor = true;
		_fillLockedAsyncButton.Click += FillLockedAsyncButton_Click;
		_genOnlyButton.AutoSize = true;
		_genOnlyButton.MinimumSize = new Size(110, 34);
		_genOnlyButton.Text = "Generate without display";
		_genOnlyButton.UseVisualStyleBackColor = true;
		_genOnlyButton.Click += GenOnlyButton_Click;
		_epasaPwdCharsButton.AutoSize = true;
		_epasaPwdCharsButton.MinimumSize = new Size(110, 34);
		_epasaPwdCharsButton.Text = "Character escaping";
		_epasaPwdCharsButton.UseVisualStyleBackColor = true;
		_epasaPwdCharsButton.Click += EpasaPwdCharsButton_Click;
		_epasaRegexButton.AutoSize = true;
		_epasaRegexButton.MinimumSize = new Size(110, 34);
		_epasaRegexButton.Text = "EPASA regular expression";
		_epasaRegexButton.UseVisualStyleBackColor = true;
		_epasaRegexButton.Click += EpasaRegexButton_Click;
		_actions.AutoSize = true;
		_actions.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_actions.Dock = DockStyle.Fill;
		_actions.Margin = Padding.Empty;
		_actions.Controls.AddRange(new Control[] { _sizeLabel, _sizeKilobytes, _fillStandardButton, _fillLockedButton, _fillLockedAsyncButton, _genOnlyButton, _epasaPwdCharsButton, _epasaRegexButton });
		_standardTextBox.Name = "_standardTextBox";
		_standardTextBox.Dock = DockStyle.Fill;
		_standardTextBox.Multiline = true;
		_standardTextBox.ScrollBars = ScrollBars.Both;
		_standardTextBox.WordWrap = false;
		_timing.AutoSize = true;
		_timing.Dock = DockStyle.Fill;
		_timing.Margin = new Padding(0, 8, 0, 8);
		_timing.Text = "Choose an operation to compare text generation and native editor rendering.";

		// Content-sized command rows keep their complete buttons visible at smaller widths.
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.Controls.Add(_actions, 0, 2);
		_layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
		_layout.Controls.Add(_standardTextBox, 0, 3);
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.Controls.Add(_timing, 0, 4);
		Font = SystemFonts.MessageBoxFont;
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		BackColor = SystemColors.Window;
		Name = "TextAreaTestsScreen";
		Text = "Text Area Laboratory";
		Size = new Size(1000, 720);
		Controls.Add(_layout);
		_layout.ResumeLayout(false);
		_layout.PerformLayout();
		ResumeLayout(false);
	}
}
