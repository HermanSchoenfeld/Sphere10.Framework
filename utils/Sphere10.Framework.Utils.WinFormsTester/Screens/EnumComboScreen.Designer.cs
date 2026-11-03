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

partial class EnumComboScreen {
	private TableLayoutPanel _layout;
	private Label _heading;
	private Label _introduction;
	private EnumComboBox _enumComboBox;
	private FlowLayoutPanel _actions;
	private Button _setNullButton;
	private Button _setEnumVal1Button;
	private Button _setEnumValue2Button;
	private Button _getValueButton;
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
		_enumComboBox = new EnumComboBox();
		_actions = new FlowLayoutPanel();
		_actions.Name = "_actions";
		_setNullButton = new Button();
		_setNullButton.Name = "_setNullButton";
		_setEnumVal1Button = new Button();
		_setEnumVal1Button.Name = "_setEnumVal1Button";
		_setEnumValue2Button = new Button();
		_setEnumValue2Button.Name = "_setEnumValue2Button";
		_getValueButton = new Button();
		_getValueButton.Name = "_getValueButton";
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
		_heading.Text = "Enum Choices";
		_introduction.AutoSize = true;
		_introduction.Dock = DockStyle.Fill;
		_introduction.Margin = new Padding(0, 0, 0, 18);
		_introduction.Text = "Compare a described enum member with its ordinary name, clear the selection, and inspect the actual typed value.";
		_layout.Controls.Add(_heading, 0, 0);
		_layout.Controls.Add(_introduction, 0, 1);

		_enumComboBox.Name = "_enumComboBox";
		_enumComboBox.Dock = DockStyle.Top;
		_enumComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
		_enumComboBox.SelectedIndexChanged += SelectionChanged;
		_setNullButton.AutoSize = true;
		_setNullButton.MinimumSize = new Size(110, 34);
		_setNullButton.Text = "Clear selection";
		_setNullButton.UseVisualStyleBackColor = true;
		_setNullButton.Click += SetNullButton_Click;
		_setEnumVal1Button.AutoSize = true;
		_setEnumVal1Button.MinimumSize = new Size(110, 34);
		_setEnumVal1Button.Text = "Described value";
		_setEnumVal1Button.UseVisualStyleBackColor = true;
		_setEnumVal1Button.Click += SetEnumVal1Button_Click;
		_setEnumValue2Button.AutoSize = true;
		_setEnumValue2Button.MinimumSize = new Size(110, 34);
		_setEnumValue2Button.Text = "Plain value";
		_setEnumValue2Button.UseVisualStyleBackColor = true;
		_setEnumValue2Button.Click += SetEnumValue2Button_Click;
		_getValueButton.AutoSize = true;
		_getValueButton.MinimumSize = new Size(110, 34);
		_getValueButton.Text = "Inspect value";
		_getValueButton.UseVisualStyleBackColor = true;
		_getValueButton.Click += GetValueButton_Click;
		_actions.AutoSize = true;
		_actions.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_actions.Dock = DockStyle.Fill;
		_actions.Margin = Padding.Empty;
		_actions.Controls.AddRange(new Control[] { _setNullButton, _setEnumVal1Button, _setEnumValue2Button, _getValueButton });
		_selectionSummary.AutoSize = true;
		_selectionSummary.Dock = DockStyle.Fill;
		_selectionSummary.Margin = new Padding(0, 8, 0, 8);
		_selectionSummary.Text = "No enum value selected.";
		_outputTextBox.Name = "_outputTextBox";
		_outputTextBox.Dock = DockStyle.Fill;
		_outputTextBox.Multiline = true;
		_outputTextBox.ReadOnly = true;
		_outputTextBox.ScrollBars = ScrollBars.Both;
		_outputTextBox.WordWrap = false;
		_outputTextBox.BackColor = SystemColors.Window;

		// Content-sized command rows keep their complete buttons visible at smaller widths.
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.Controls.Add(_enumComboBox, 0, 2);
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.Controls.Add(_actions, 0, 3);
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.Controls.Add(_selectionSummary, 0, 4);
		_layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
		_layout.Controls.Add(_outputTextBox, 0, 5);
		Font = SystemFonts.MessageBoxFont;
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		BackColor = SystemColors.Window;
		Name = "EnumComboScreen";
		Text = "Enum Choices";
		Size = new Size(1000, 720);
		Controls.Add(_layout);
		_layout.ResumeLayout(false);
		_layout.PerformLayout();
		ResumeLayout(false);
	}
}
