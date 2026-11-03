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

partial class ValidationIndicatorTestScreen {
	private TableLayoutPanel _layout;
	private Label _heading;
	private Label _introduction;
	private FlowLayoutPanel _manualRow;
	private Button _validButton;
	private Button _errorButton;
	private Button _validatingButton;
	private Button _disabledButton;
	private ValidationIndicator _validationIndicator1;
	private FlowLayoutPanel _inputRow;
	private ValidationIndicator _validationIndicator2;
	private TextBox _textBox;
	private Button _enableDisableButton;
	private Label _label1;
	private TextBox _outputTextBox;

	// Static layout and named event wiring remain editable in the native Designer.
	private void InitializeComponent() {
		_layout = new TableLayoutPanel();
		_layout.Name = "_layout";
		_heading = new Label();
		_heading.Name = "_heading";
		_introduction = new Label();
		_introduction.Name = "_introduction";
		_manualRow = new FlowLayoutPanel();
		_manualRow.Name = "_manualRow";
		_validButton = new Button();
		_validButton.Name = "_validButton";
		_errorButton = new Button();
		_errorButton.Name = "_errorButton";
		_validatingButton = new Button();
		_validatingButton.Name = "_validatingButton";
		_disabledButton = new Button();
		_disabledButton.Name = "_disabledButton";
		_validationIndicator1 = new ValidationIndicator();
		_inputRow = new FlowLayoutPanel();
		_inputRow.Name = "_inputRow";
		_validationIndicator2 = new ValidationIndicator();
		_textBox = new TextBox();
		_enableDisableButton = new Button();
		_enableDisableButton.Name = "_enableDisableButton";
		_label1 = new Label();
		_label1.Name = "_label1";
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
		_heading.Text = "Validation Feedback";
		_introduction.AutoSize = true;
		_introduction.Dock = DockStyle.Fill;
		_introduction.Margin = new Padding(0, 0, 0, 18);
		_introduction.Text = "Compare explicit visual states with the control’s real asynchronous PerformValidation callback. Completed states and results appear in the log.";
		_layout.Controls.Add(_heading, 0, 0);
		_layout.Controls.Add(_introduction, 0, 1);

		_validButton.AutoSize = true;
		_validButton.MinimumSize = new Size(110, 34);
		_validButton.Text = "Valid";
		_validButton.UseVisualStyleBackColor = true;
		_validButton.Click += ValidButton_Click;
		_errorButton.AutoSize = true;
		_errorButton.MinimumSize = new Size(110, 34);
		_errorButton.Text = "Error";
		_errorButton.UseVisualStyleBackColor = true;
		_errorButton.Click += ErrorButton_Click;
		_validatingButton.AutoSize = true;
		_validatingButton.MinimumSize = new Size(110, 34);
		_validatingButton.Text = "Validating";
		_validatingButton.UseVisualStyleBackColor = true;
		_validatingButton.Click += ValidatingButton_Click;
		_disabledButton.AutoSize = true;
		_disabledButton.MinimumSize = new Size(110, 34);
		_disabledButton.Text = "Disabled";
		_disabledButton.UseVisualStyleBackColor = true;
		_disabledButton.Click += DisabledButton_Click;
		_validationIndicator1.Name = "manualValidation";
		_validationIndicator1.Size = new Size(30, 30);
		_validationIndicator1.ValidationStateChanged += ValidationIndicator1_ValidationStateChanged;
		_manualRow.AutoSize = true;
		_manualRow.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_manualRow.Dock = DockStyle.Fill;
		_manualRow.Margin = Padding.Empty;
		_manualRow.Controls.AddRange(new Control[] { _validButton, _errorButton, _validatingButton, _disabledButton, _validationIndicator1 });
		_textBox.Name = "validationInput";
		_textBox.Width = 320;
		_textBox.TextChanged += TextBox_TextChanged;
		_validationIndicator2.Name = "inputValidation";
		_validationIndicator2.Size = new Size(30, 30);
		_validationIndicator2.PerformValidation += ValidationIndicator2_PerformValidation;
		_validationIndicator2.ValidationStateChanged += ValidationIndicator2_ValidationStateChanged;
		_enableDisableButton.AutoSize = true;
		_enableDisableButton.MinimumSize = new Size(110, 34);
		_enableDisableButton.Text = "Disable validator";
		_enableDisableButton.UseVisualStyleBackColor = true;
		_enableDisableButton.Click += EnableDisableButton_Click;
		_inputRow.AutoSize = true;
		_inputRow.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_inputRow.Dock = DockStyle.Fill;
		_inputRow.Margin = Padding.Empty;
		_inputRow.Controls.AddRange(new Control[] { _textBox, _validationIndicator2, _enableDisableButton });
		_label1.AutoSize = true;
		_label1.Dock = DockStyle.Fill;
		_label1.Margin = new Padding(0, 8, 0, 8);
		_label1.Text = "Input example: type Herman for a successful result. Validation uses an immutable text snapshot on its worker.";
		_outputTextBox.Name = "_outputTextBox";
		_outputTextBox.Dock = DockStyle.Fill;
		_outputTextBox.Multiline = true;
		_outputTextBox.ReadOnly = true;
		_outputTextBox.ScrollBars = ScrollBars.Both;
		_outputTextBox.WordWrap = false;
		_outputTextBox.BackColor = SystemColors.Window;

		// Content-sized command rows keep their complete buttons visible at smaller widths.
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.Controls.Add(_manualRow, 0, 2);
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.Controls.Add(_label1, 0, 3);
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.Controls.Add(_inputRow, 0, 4);
		_layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
		_layout.Controls.Add(_outputTextBox, 0, 5);
		Font = SystemFonts.MessageBoxFont;
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		BackColor = SystemColors.Window;
		Name = "ValidationIndicatorTestScreen";
		Text = "Validation Feedback";
		Size = new Size(1000, 720);
		Controls.Add(_layout);
		_layout.ResumeLayout(false);
		_layout.PerformLayout();
		ResumeLayout(false);
	}
}
