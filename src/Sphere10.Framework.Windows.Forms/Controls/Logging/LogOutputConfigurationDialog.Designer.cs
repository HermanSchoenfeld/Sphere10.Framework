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

partial class LogOutputConfigurationDialog {
	private IContainer components;
	private TableLayoutPanel _layoutPanel;
	private Label _stateLabel;
	private TextBox _detailsTextBox;
	private FlowLayoutPanel _buttonsPanel;
	private Button _closeButton;
	private Button _changeButton;

	protected override void Dispose(bool disposing) {
		if (disposing) {
			DetachSelector();
			components?.Dispose();
		}
		base.Dispose(disposing);
	}

	private void InitializeComponent() {
		components = new Container();
		_layoutPanel = new TableLayoutPanel();
		_stateLabel = new Label();
		_detailsTextBox = new TextBox();
		_buttonsPanel = new FlowLayoutPanel();
		_closeButton = new Button();
		_changeButton = new Button();
		_layoutPanel.SuspendLayout();
		_buttonsPanel.SuspendLayout();
		SuspendLayout();
		_layoutPanel.ColumnCount = 1;
		_layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		_layoutPanel.RowCount = 3;
		_layoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 34F));
		_layoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
		_layoutPanel.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));
		_layoutPanel.Dock = DockStyle.Fill;
		_layoutPanel.Padding = new Padding(12);
		_layoutPanel.Name = "_layoutPanel";
		_layoutPanel.Controls.Add(_stateLabel, 0, 0);
		_layoutPanel.Controls.Add(_detailsTextBox, 0, 1);
		_layoutPanel.Controls.Add(_buttonsPanel, 0, 2);
		_stateLabel.Dock = DockStyle.Fill;
		_stateLabel.Font = new Font("Segoe UI", 10F, FontStyle.Bold);
		_stateLabel.Name = "_stateLabel";
		_stateLabel.Text = "Output Is Off";
		_stateLabel.TextAlign = ContentAlignment.MiddleLeft;
		_detailsTextBox.Dock = DockStyle.Fill;
		_detailsTextBox.Multiline = true;
		_detailsTextBox.ReadOnly = true;
		_detailsTextBox.ScrollBars = ScrollBars.Both;
		_detailsTextBox.WordWrap = false;
		_detailsTextBox.Name = "_detailsTextBox";
		_detailsTextBox.Text = "No logging output has been selected.";
		_detailsTextBox.TabIndex = 0;
		_buttonsPanel.Dock = DockStyle.Fill;
		_buttonsPanel.FlowDirection = FlowDirection.RightToLeft;
		_buttonsPanel.Padding = new Padding(0, 8, 0, 0);
		_buttonsPanel.Name = "_buttonsPanel";
		_buttonsPanel.WrapContents = false;
		_buttonsPanel.Controls.Add(_closeButton);
		_buttonsPanel.Controls.Add(_changeButton);
		_closeButton.DialogResult = DialogResult.OK;
		_closeButton.Name = "_closeButton";
		_closeButton.Size = new Size(90, 28);
		_closeButton.Text = "Close";
		_closeButton.TabIndex = 1;
		_closeButton.UseVisualStyleBackColor = true;
		_changeButton.Name = "_changeButton";
		_changeButton.Size = new Size(90, 28);
		_changeButton.Text = "Change...";
		_changeButton.TabIndex = 0;
		_changeButton.UseVisualStyleBackColor = true;
		_changeButton.Click += ChangeClick;
		AcceptButton = _closeButton;
		CancelButton = _closeButton;
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		ClientSize = new Size(620, 410);
		Controls.Add(_layoutPanel);
		MinimizeBox = false;
		MaximizeBox = false;
		MinimumSize = new Size(480, 300);
		Name = "LogOutputConfigurationDialog";
		ShowInTaskbar = false;
		StartPosition = FormStartPosition.CenterParent;
		Text = "Logging Configuration";
		_buttonsPanel.ResumeLayout(false);
		_layoutPanel.ResumeLayout(false);
		_layoutPanel.PerformLayout();
		ResumeLayout(false);
	}
}
