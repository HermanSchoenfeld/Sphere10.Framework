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

partial class ReviewTextDialog {
	private IContainer components;
	private TableLayoutPanel _layoutPanel;
	private Label _instructionsLabel;
	private TextBoxEx _previewTextBox;
	private TableLayoutPanel _footerPanel;
	private Label _statusLabel;
	private FlowLayoutPanel _buttonsPanel;
	private Button _copyButton;
	private Button _saveButton;
	private Button _closeButton;

	protected override void Dispose(bool disposing) {
		if (disposing && !_resourcesDisposed) {
			_resourcesDisposed = true;
			_lifetime.Cancel();
			_lifetime.Dispose();
			components?.Dispose();
		}
		base.Dispose(disposing);
	}

	private void InitializeComponent() {
		components = new Container();
		_layoutPanel = new TableLayoutPanel();
		_instructionsLabel = new Label();
		_previewTextBox = new TextBoxEx();
		_footerPanel = new TableLayoutPanel();
		_statusLabel = new Label();
		_buttonsPanel = new FlowLayoutPanel();
		_copyButton = new Button();
		_saveButton = new Button();
		_closeButton = new Button();
		_layoutPanel.SuspendLayout();
		_footerPanel.SuspendLayout();
		_buttonsPanel.SuspendLayout();
		SuspendLayout();
		_layoutPanel.ColumnCount = 1;
		_layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		_layoutPanel.Controls.Add(_instructionsLabel, 0, 0);
		_layoutPanel.Controls.Add(_previewTextBox, 0, 1);
		_layoutPanel.Controls.Add(_footerPanel, 0, 2);
		_layoutPanel.Dock = DockStyle.Fill;
		_layoutPanel.Name = "_layoutPanel";
		_layoutPanel.Padding = new Padding(12);
		_layoutPanel.RowCount = 3;
		_layoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
		_layoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_instructionsLabel.AutoSize = true;
		_instructionsLabel.Dock = DockStyle.Fill;
		_instructionsLabel.Margin = new Padding(0, 0, 0, 8);
		_instructionsLabel.Name = "_instructionsLabel";
		_previewTextBox.AcceptsReturn = true;
		_previewTextBox.AcceptsTab = true;
		_previewTextBox.BackColor = SystemColors.Window;
		_previewTextBox.Dock = DockStyle.Fill;
		_previewTextBox.Font = new Font("Consolas", 10F);
		_previewTextBox.HideSelection = false;
		_previewTextBox.Margin = new Padding(0);
		_previewTextBox.MaxLength = int.MaxValue;
		_previewTextBox.Multiline = true;
		_previewTextBox.Name = "_previewTextBox";
		_previewTextBox.ReadOnly = true;
		_previewTextBox.ScrollBars = ScrollBars.Both;
		_previewTextBox.TabIndex = 0;
		_previewTextBox.WordWrap = false;
		_footerPanel.AutoSize = true;
		_footerPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_footerPanel.ColumnCount = 3;
		_footerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
		_footerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		_footerPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
		_footerPanel.Controls.Add(_copyButton, 0, 0);
		_footerPanel.Controls.Add(_statusLabel, 1, 0);
		_footerPanel.Controls.Add(_buttonsPanel, 2, 0);
		_footerPanel.Dock = DockStyle.Fill;
		_footerPanel.Margin = new Padding(0, 8, 0, 0);
		_footerPanel.Name = "_footerPanel";
		_footerPanel.RowCount = 1;
		_footerPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_statusLabel.AutoSize = true;
		_statusLabel.Anchor = AnchorStyles.Left;
		_statusLabel.Name = "_statusLabel";
		_buttonsPanel.AutoSize = true;
		_buttonsPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_buttonsPanel.Controls.Add(_closeButton);
		_buttonsPanel.Controls.Add(_saveButton);
		_buttonsPanel.Dock = DockStyle.Fill;
		_buttonsPanel.FlowDirection = FlowDirection.RightToLeft;
		_buttonsPanel.Margin = new Padding(0);
		_buttonsPanel.Name = "_buttonsPanel";
		_buttonsPanel.WrapContents = false;
		_copyButton.AutoSize = true;
		_copyButton.Anchor = AnchorStyles.Left;
		_copyButton.Name = "_copyButton";
		_copyButton.Padding = new Padding(12, 3, 12, 3);
		_copyButton.TabIndex = 1;
		_copyButton.Text = "&Copy To Clipboard";
		_copyButton.UseVisualStyleBackColor = true;
		_copyButton.Click += CopyButtonClick;
		_saveButton.AutoSize = true;
		_saveButton.Name = "_saveButton";
		_saveButton.Padding = new Padding(12, 3, 12, 3);
		_saveButton.TabIndex = 2;
		_saveButton.Text = "&Save...";
		_saveButton.UseVisualStyleBackColor = true;
		_saveButton.Click += SaveButtonClick;
		_closeButton.AutoSize = true;
		_closeButton.DialogResult = DialogResult.Cancel;
		_closeButton.Name = "_closeButton";
		_closeButton.Padding = new Padding(12, 3, 12, 3);
		_closeButton.TabIndex = 3;
		_closeButton.Text = "&Close";
		_closeButton.UseVisualStyleBackColor = true;
		AcceptButton = _saveButton;
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		CancelButton = _closeButton;
		ClientSize = new Size(900, 600);
		Controls.Add(_layoutPanel);
		MinimizeBox = false;
		MinimumSize = new Size(500, 350);
		Name = "ReviewTextDialog";
		ShowInTaskbar = false;
		StartPosition = FormStartPosition.CenterParent;
		Text = "Review Text";
		_layoutPanel.ResumeLayout(false);
		_layoutPanel.PerformLayout();
		_footerPanel.ResumeLayout(false);
		_footerPanel.PerformLayout();
		_buttonsPanel.ResumeLayout(false);
		_buttonsPanel.PerformLayout();
		ResumeLayout(false);
	}
}
