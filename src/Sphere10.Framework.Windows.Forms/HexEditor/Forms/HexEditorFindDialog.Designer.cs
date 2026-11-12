// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms;

partial class HexEditorFindDialog {
	private IContainer components;
	private TableLayoutPanel _layoutPanel;
	private Label _patternLabel;
	private TextBox _patternTextBox;
	private FlowLayoutPanel _kindPanel;
	private RadioButton _hexRadioButton;
	private RadioButton _textRadioButton;
	private FlowLayoutPanel _directionPanel;
	private Label _directionLabel;
	private RadioButton _forwardRadioButton;
	private RadioButton _backwardRadioButton;
	private Label _errorLabel;
	private FlowLayoutPanel _buttonsPanel;
	private Button _findButton;
	private Button _cancelButton;

	protected override void Dispose(bool disposing) {
		if (disposing)
			components?.Dispose();
		base.Dispose(disposing);
	}

	private void InitializeComponent() {
		components = new Container();
		_layoutPanel = new TableLayoutPanel();
		_patternLabel = new Label();
		_patternTextBox = new TextBox();
		_kindPanel = new FlowLayoutPanel();
		_hexRadioButton = new RadioButton();
		_textRadioButton = new RadioButton();
		_directionPanel = new FlowLayoutPanel();
		_directionLabel = new Label();
		_forwardRadioButton = new RadioButton();
		_backwardRadioButton = new RadioButton();
		_errorLabel = new Label();
		_buttonsPanel = new FlowLayoutPanel();
		_findButton = new Button();
		_cancelButton = new Button();
		_layoutPanel.SuspendLayout();
		_kindPanel.SuspendLayout();
		_directionPanel.SuspendLayout();
		_buttonsPanel.SuspendLayout();
		SuspendLayout();
		_layoutPanel.ColumnCount = 1;
		_layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		_layoutPanel.Controls.Add(_patternLabel, 0, 0);
		_layoutPanel.Controls.Add(_patternTextBox, 0, 1);
		_layoutPanel.Controls.Add(_kindPanel, 0, 2);
		_layoutPanel.Controls.Add(_directionPanel, 0, 3);
		_layoutPanel.Controls.Add(_errorLabel, 0, 4);
		_layoutPanel.Controls.Add(_buttonsPanel, 0, 5);
		_layoutPanel.Dock = DockStyle.Fill;
		_layoutPanel.Name = "_layoutPanel";
		_layoutPanel.Padding = new Padding(12);
		_layoutPanel.RowCount = 6;
		_layoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
		_layoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_patternLabel.AutoSize = true;
		_patternLabel.Margin = new Padding(0, 0, 0, 4);
		_patternLabel.Name = "_patternLabel";
		_patternLabel.Text = "Hexadecimal &bytes (e.g. DE AD BE EF):";
		_patternTextBox.Dock = DockStyle.Fill;
		_patternTextBox.Font = new Font("Consolas", 10F);
		_patternTextBox.Margin = new Padding(0, 0, 0, 8);
		_patternTextBox.Name = "_patternTextBox";
		_patternTextBox.TabIndex = 0;
		_kindPanel.AutoSize = true;
		_kindPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_kindPanel.Controls.Add(_hexRadioButton);
		_kindPanel.Controls.Add(_textRadioButton);
		_kindPanel.Margin = new Padding(0);
		_kindPanel.Name = "_kindPanel";
		_kindPanel.WrapContents = false;
		_hexRadioButton.AutoSize = true;
		_hexRadioButton.Checked = true;
		_hexRadioButton.Name = "_hexRadioButton";
		_hexRadioButton.TabIndex = 1;
		_hexRadioButton.TabStop = true;
		_hexRadioButton.Text = "&Hexadecimal bytes";
		_hexRadioButton.CheckedChanged += PatternKindChanged;
		_textRadioButton.AutoSize = true;
		_textRadioButton.Name = "_textRadioButton";
		_textRadioButton.TabIndex = 2;
		_textRadioButton.Text = "&UTF-8 text";
		_directionPanel.AutoSize = true;
		_directionPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_directionPanel.Controls.Add(_directionLabel);
		_directionPanel.Controls.Add(_forwardRadioButton);
		_directionPanel.Controls.Add(_backwardRadioButton);
		_directionPanel.Margin = new Padding(0);
		_directionPanel.Name = "_directionPanel";
		_directionPanel.WrapContents = false;
		_directionLabel.AutoSize = true;
		_directionLabel.Margin = new Padding(0, 6, 8, 0);
		_directionLabel.Name = "_directionLabel";
		_directionLabel.Text = "Direction:";
		_forwardRadioButton.AutoSize = true;
		_forwardRadioButton.Checked = true;
		_forwardRadioButton.Name = "_forwardRadioButton";
		_forwardRadioButton.TabIndex = 3;
		_forwardRadioButton.TabStop = true;
		_forwardRadioButton.Text = "&Forward";
		_backwardRadioButton.AutoSize = true;
		_backwardRadioButton.Name = "_backwardRadioButton";
		_backwardRadioButton.TabIndex = 4;
		_backwardRadioButton.Text = "Back&ward";
		_errorLabel.AutoSize = true;
		_errorLabel.Dock = DockStyle.Fill;
		_errorLabel.ForeColor = Color.Firebrick;
		_errorLabel.Margin = new Padding(0, 4, 0, 4);
		_errorLabel.Name = "_errorLabel";
		_buttonsPanel.AutoSize = true;
		_buttonsPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_buttonsPanel.Controls.Add(_cancelButton);
		_buttonsPanel.Controls.Add(_findButton);
		_buttonsPanel.Dock = DockStyle.Fill;
		_buttonsPanel.FlowDirection = FlowDirection.RightToLeft;
		_buttonsPanel.Margin = new Padding(0);
		_buttonsPanel.Name = "_buttonsPanel";
		_buttonsPanel.WrapContents = false;
		_findButton.AutoSize = true;
		_findButton.MinimumSize = new Size(75, 0);
		_findButton.Name = "_findButton";
		_findButton.TabIndex = 5;
		_findButton.Text = "&Find";
		_findButton.UseVisualStyleBackColor = true;
		_findButton.Click += FindButtonClicked;
		_cancelButton.AutoSize = true;
		_cancelButton.DialogResult = DialogResult.Cancel;
		_cancelButton.MinimumSize = new Size(75, 0);
		_cancelButton.Name = "_cancelButton";
		_cancelButton.TabIndex = 6;
		_cancelButton.Text = "Cancel";
		_cancelButton.UseVisualStyleBackColor = true;
		AcceptButton = _findButton;
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		CancelButton = _cancelButton;
		ClientSize = new Size(420, 200);
		Controls.Add(_layoutPanel);
		FormBorderStyle = FormBorderStyle.FixedDialog;
		MaximizeBox = false;
		MinimizeBox = false;
		Name = "HexEditorFindDialog";
		ShowInTaskbar = false;
		StartPosition = FormStartPosition.CenterParent;
		Text = "Find";
		_layoutPanel.ResumeLayout(false);
		_layoutPanel.PerformLayout();
		_kindPanel.ResumeLayout(false);
		_kindPanel.PerformLayout();
		_directionPanel.ResumeLayout(false);
		_directionPanel.PerformLayout();
		_buttonsPanel.ResumeLayout(false);
		_buttonsPanel.PerformLayout();
		ResumeLayout(false);
	}
}
