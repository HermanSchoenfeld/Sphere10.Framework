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

partial class LogOutputSelectorControl {
	private IContainer components;
	private TableLayoutPanel _layoutPanel;
	private CheckBox _loggingCheckBox;
	private Button _configureButton;
	private ToolTip _toolTip;

	protected override void Dispose(bool disposing) {
		if (disposing) {
			SaveLogOutputSettings();
			DisposeLogger();
			components?.Dispose();
		}
		base.Dispose(disposing);
	}

	private void InitializeComponent() {
		components = new Container();
		_layoutPanel = new TableLayoutPanel();
		_loggingCheckBox = new CheckBox();
		_configureButton = new Button();
		_toolTip = new ToolTip(components);
		_layoutPanel.SuspendLayout();
		SuspendLayout();
		_layoutPanel.AutoSize = true;
		_layoutPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_layoutPanel.ColumnCount = 2;
		_layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
		_layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 30F));
		_layoutPanel.Controls.Add(_loggingCheckBox, 0, 0);
		_layoutPanel.Controls.Add(_configureButton, 1, 0);
		_layoutPanel.Dock = DockStyle.Left;
		_layoutPanel.Margin = new Padding(0);
		_layoutPanel.Name = "_layoutPanel";
		_layoutPanel.RowCount = 1;
		_layoutPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
		_loggingCheckBox.Anchor = AnchorStyles.Left;
		_loggingCheckBox.AutoSize = true;
		_loggingCheckBox.Margin = new Padding(0, 0, 5, 0);
		_loggingCheckBox.Name = "_loggingCheckBox";
		_loggingCheckBox.TabIndex = 0;
		_loggingCheckBox.Text = "Log Output";
		_loggingCheckBox.UseVisualStyleBackColor = true;
		_loggingCheckBox.CheckedChanged += LoggingCheckedChanged;
		_configureButton.AccessibleDescription = "View the current logging configuration or change output.";
		_configureButton.AccessibleName = "Configure Log Output";
		_configureButton.AccessibleRole = AccessibleRole.PushButton;
		_configureButton.Anchor = AnchorStyles.None;
		_configureButton.Margin = new Padding(0);
		_configureButton.Name = "_configureButton";
		_configureButton.Size = new Size(27, 20);
		_configureButton.TabIndex = 1;
		_configureButton.Text = "...";
		_configureButton.UseVisualStyleBackColor = true;
		_configureButton.Click += ConfigureButtonClick;
		_toolTip.SetToolTip(_configureButton, "Configure Log Output: view current settings or change output.");
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		Controls.Add(_layoutPanel);
		MinimumSize = new Size(140, 26);
		Name = "LogOutputSelectorControl";
		Size = new Size(140, 26);
		_layoutPanel.ResumeLayout(false);
		_layoutPanel.PerformLayout();
		ResumeLayout(false);
		PerformLayout();
	}
}
