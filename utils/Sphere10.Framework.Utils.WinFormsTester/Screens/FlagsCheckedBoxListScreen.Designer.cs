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

partial class FlagsCheckedBoxListScreen {
	private TableLayoutPanel _layout;
	private Label _heading;
	private Label _introduction;
	private FlagsCheckedListBox _flagsCheckedListBox;
	private FlowLayoutPanel _actions;
	private Button _noneButton;
	private Button _defaultButton;
	private Button _allButton;
	private Label _selectionSummary;
	private TextBox _outputTextBox;

	// Static layout and named event wiring remain editable in the native Designer.
	private void InitializeComponent() {
		_layout = new TableLayoutPanel();
		_layout.Name = "_layout";
		_heading = new Label();
		_heading.Name = "_heading";
		_introduction = new Label();
		_introduction.Name = "_introduction";
		_flagsCheckedListBox = new FlagsCheckedListBox();
		_actions = new FlowLayoutPanel();
		_actions.Name = "_actions";
		_noneButton = new Button();
		_noneButton.Name = "_noneButton";
		_defaultButton = new Button();
		_defaultButton.Name = "_defaultButton";
		_allButton = new Button();
		_allButton.Name = "_allButton";
		_selectionSummary = new Label();
		_selectionSummary.Name = "_selectionSummary";
		_outputTextBox = new TextBox();
		_layout.SuspendLayout();
		SuspendLayout();

		// The heading and instructions remain with this screen when it is detached.
		_layout.Dock = DockStyle.Fill;
		_layout.Padding = new Padding(20);
		_layout.ColumnCount = 1;
		_layout.RowCount = 6;
		_layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_heading.AutoSize = true;
		_heading.Dock = DockStyle.Fill;
		_heading.Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 17F, FontStyle.Bold);
		_heading.Margin = new Padding(0, 0, 0, 10);
		_heading.Text = "Flag Choices";
		_introduction.AutoSize = true;
		_introduction.Dock = DockStyle.Fill;
		_introduction.Margin = new Padding(0, 0, 0, 18);
		_introduction.Text = "Toggle individual and combined flags. The readout shows both the enum names and numeric bit mask, including the zero value.";
		_layout.Controls.Add(_heading, 0, 0);
		_layout.Controls.Add(_introduction, 0, 1);

		_flagsCheckedListBox.Name = "_flagsCheckedListBox";
		_flagsCheckedListBox.Dock = DockStyle.Fill;
		_flagsCheckedListBox.CheckOnClick = true;
		_flagsCheckedListBox.ItemChecked += FlagsItemChecked;
		_noneButton.AutoSize = true;
		_noneButton.MinimumSize = new Size(110, 34);
		_noneButton.Text = "No flags";
		_noneButton.UseVisualStyleBackColor = true;
		_noneButton.Click += ClearFlagsClicked;
		_defaultButton.AutoSize = true;
		_defaultButton.MinimumSize = new Size(110, 34);
		_defaultButton.Text = "Default flags";
		_defaultButton.UseVisualStyleBackColor = true;
		_defaultButton.Click += DefaultFlagsClicked;
		_allButton.AutoSize = true;
		_allButton.MinimumSize = new Size(110, 34);
		_allButton.Text = "All flags";
		_allButton.UseVisualStyleBackColor = true;
		_allButton.Click += AllFlagsClicked;
		_actions.AutoSize = true;
		_actions.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_actions.Dock = DockStyle.Fill;
		_actions.Margin = Padding.Empty;
		_actions.Controls.AddRange(new Control[] { _noneButton, _defaultButton, _allButton });
		_selectionSummary.AutoSize = true;
		_selectionSummary.Dock = DockStyle.Fill;
		_selectionSummary.Margin = new Padding(0, 8, 0, 8);
		_selectionSummary.Text = "No flags selected.";
		_outputTextBox.Name = "_outputTextBox";
		_outputTextBox.Dock = DockStyle.Fill;
		_outputTextBox.Multiline = true;
		_outputTextBox.ReadOnly = true;
		_outputTextBox.ScrollBars = ScrollBars.Both;
		_outputTextBox.WordWrap = false;
		_outputTextBox.BackColor = SystemColors.Window;

		// Content-sized command rows keep their complete buttons visible at smaller widths.
		_layout.RowStyles.Add(new RowStyle(SizeType.Percent, 45F));
		_layout.Controls.Add(_flagsCheckedListBox, 0, 2);
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.Controls.Add(_actions, 0, 3);
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.Controls.Add(_selectionSummary, 0, 4);
		_layout.RowStyles.Add(new RowStyle(SizeType.Percent, 55F));
		_layout.Controls.Add(_outputTextBox, 0, 5);
		Font = SystemFonts.MessageBoxFont;
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		BackColor = SystemColors.Window;
		Name = "FlagsCheckedBoxListScreen";
		Text = "Flag Choices";
		Size = new Size(1000, 720);
		Controls.Add(_layout);
		_layout.ResumeLayout(false);
		_layout.PerformLayout();
		ResumeLayout(false);
	}
}
