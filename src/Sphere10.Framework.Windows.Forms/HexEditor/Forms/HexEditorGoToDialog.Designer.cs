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

partial class HexEditorGoToDialog {
	private IContainer components;
	private TableLayoutPanel _layoutPanel;
	private Label _offsetLabel;
	private TextBox _offsetTextBox;
	private FlowLayoutPanel _radixPanel;
	private RadioButton _hexRadioButton;
	private RadioButton _decimalRadioButton;
	private Label _errorLabel;
	private FlowLayoutPanel _buttonsPanel;
	private Button _okButton;
	private Button _cancelButton;

	protected override void Dispose(bool disposing) {
		if (disposing)
			components?.Dispose();
		base.Dispose(disposing);
	}

	private void InitializeComponent() {
		components = new Container();
		_layoutPanel = new TableLayoutPanel();
		_offsetLabel = new Label();
		_offsetTextBox = new TextBox();
		_radixPanel = new FlowLayoutPanel();
		_hexRadioButton = new RadioButton();
		_decimalRadioButton = new RadioButton();
		_errorLabel = new Label();
		_buttonsPanel = new FlowLayoutPanel();
		_okButton = new Button();
		_cancelButton = new Button();
		_layoutPanel.SuspendLayout();
		_radixPanel.SuspendLayout();
		_buttonsPanel.SuspendLayout();
		SuspendLayout();
		_layoutPanel.ColumnCount = 1;
		_layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		_layoutPanel.Controls.Add(_offsetLabel, 0, 0);
		_layoutPanel.Controls.Add(_offsetTextBox, 0, 1);
		_layoutPanel.Controls.Add(_radixPanel, 0, 2);
		_layoutPanel.Controls.Add(_errorLabel, 0, 3);
		_layoutPanel.Controls.Add(_buttonsPanel, 0, 4);
		_layoutPanel.Dock = DockStyle.Fill;
		_layoutPanel.Name = "_layoutPanel";
		_layoutPanel.Padding = new Padding(12);
		_layoutPanel.RowCount = 5;
		_layoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
		_layoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_offsetLabel.AutoSize = true;
		_offsetLabel.Margin = new Padding(0, 0, 0, 4);
		_offsetLabel.Name = "_offsetLabel";
		_offsetLabel.Text = "&Offset:";
		_offsetTextBox.Dock = DockStyle.Fill;
		_offsetTextBox.Font = new Font("Consolas", 10F);
		_offsetTextBox.Margin = new Padding(0, 0, 0, 8);
		_offsetTextBox.Name = "_offsetTextBox";
		_offsetTextBox.TabIndex = 0;
		_radixPanel.AutoSize = true;
		_radixPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_radixPanel.Controls.Add(_hexRadioButton);
		_radixPanel.Controls.Add(_decimalRadioButton);
		_radixPanel.Margin = new Padding(0);
		_radixPanel.Name = "_radixPanel";
		_radixPanel.WrapContents = false;
		_hexRadioButton.AutoSize = true;
		_hexRadioButton.Checked = true;
		_hexRadioButton.Name = "_hexRadioButton";
		_hexRadioButton.TabIndex = 1;
		_hexRadioButton.TabStop = true;
		_hexRadioButton.Text = "&Hexadecimal";
		_hexRadioButton.CheckedChanged += RadixChanged;
		_decimalRadioButton.AutoSize = true;
		_decimalRadioButton.Name = "_decimalRadioButton";
		_decimalRadioButton.TabIndex = 2;
		_decimalRadioButton.Text = "&Decimal";
		_errorLabel.AutoSize = true;
		_errorLabel.Dock = DockStyle.Fill;
		_errorLabel.ForeColor = Color.Firebrick;
		_errorLabel.Margin = new Padding(0, 4, 0, 4);
		_errorLabel.Name = "_errorLabel";
		_buttonsPanel.AutoSize = true;
		_buttonsPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_buttonsPanel.Controls.Add(_cancelButton);
		_buttonsPanel.Controls.Add(_okButton);
		_buttonsPanel.Dock = DockStyle.Fill;
		_buttonsPanel.FlowDirection = FlowDirection.RightToLeft;
		_buttonsPanel.Margin = new Padding(0);
		_buttonsPanel.Name = "_buttonsPanel";
		_buttonsPanel.WrapContents = false;
		_okButton.AutoSize = true;
		_okButton.MinimumSize = new Size(75, 0);
		_okButton.Name = "_okButton";
		_okButton.TabIndex = 3;
		_okButton.Text = "&Go";
		_okButton.UseVisualStyleBackColor = true;
		_okButton.Click += OkButtonClicked;
		_cancelButton.AutoSize = true;
		_cancelButton.DialogResult = DialogResult.Cancel;
		_cancelButton.MinimumSize = new Size(75, 0);
		_cancelButton.Name = "_cancelButton";
		_cancelButton.TabIndex = 4;
		_cancelButton.Text = "Cancel";
		_cancelButton.UseVisualStyleBackColor = true;
		AcceptButton = _okButton;
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		CancelButton = _cancelButton;
		ClientSize = new Size(360, 170);
		Controls.Add(_layoutPanel);
		FormBorderStyle = FormBorderStyle.FixedDialog;
		MaximizeBox = false;
		MinimizeBox = false;
		Name = "HexEditorGoToDialog";
		ShowInTaskbar = false;
		StartPosition = FormStartPosition.CenterParent;
		Text = "Go To Offset";
		_layoutPanel.ResumeLayout(false);
		_layoutPanel.PerformLayout();
		_radixPanel.ResumeLayout(false);
		_radixPanel.PerformLayout();
		_buttonsPanel.ResumeLayout(false);
		_buttonsPanel.PerformLayout();
		ResumeLayout(false);
	}
}
