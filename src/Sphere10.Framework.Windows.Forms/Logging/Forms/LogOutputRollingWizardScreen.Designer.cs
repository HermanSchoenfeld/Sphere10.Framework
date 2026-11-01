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

partial class LogOutputRollingWizardScreen {
	private IContainer components;
	private Label _titleLabel;
	private Label _introLabel;
	private CheckBox _rollingCheckBox;
	private Label _maxFilesLabel;
	private NumericUpDown _maxFilesNumber;
	private Label _maxFileSizeLabel;
	private NumericUpDown _maxFileSizeNumber;
	private Label _retentionLabel;

	protected override void Dispose(bool disposing) {
		if (disposing)
			components?.Dispose();
		base.Dispose(disposing);
	}

	private void InitializeComponent() {
		components = new Container();
		_titleLabel = new Label();
		_introLabel = new Label();
		_rollingCheckBox = new CheckBox();
		_maxFilesLabel = new Label();
		_maxFilesNumber = new NumericUpDown();
		_maxFileSizeLabel = new Label();
		_maxFileSizeNumber = new NumericUpDown();
		_retentionLabel = new Label();
		((ISupportInitialize)_maxFilesNumber).BeginInit();
		((ISupportInitialize)_maxFileSizeNumber).BeginInit();
		SuspendLayout();
		_titleLabel.AutoSize = false;
		_titleLabel.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
		_titleLabel.ForeColor = SystemColors.Highlight;
		_titleLabel.Location = new Point(16, 16);
		_titleLabel.Name = "_titleLabel";
		_titleLabel.Size = new Size(618, 32);
		_titleLabel.Text = "Rolling";

		_introLabel.Location = new Point(16, 56);
		_introLabel.Name = "_introLabel";
		_introLabel.Size = new Size(618, 48);
		_titleLabel.Text = "File Rolling And Retention";
		_introLabel.Text = "Optional rolling archives the active file at its byte limit and removes the oldest owned archives.";
		_rollingCheckBox.AutoSize = true;
		_rollingCheckBox.Location = new Point(20, 116);
		_rollingCheckBox.Name = "_rollingCheckBox";
		_rollingCheckBox.Text = "Roll Log Files";
		_rollingCheckBox.CheckedChanged += RollingCheckedChanged;
		_maxFilesLabel.Location = new Point(20, 161);
		_maxFilesLabel.Name = "_maxFilesLabel";
		_maxFilesLabel.Size = new Size(390, 25);
		_maxFilesLabel.Text = "Maximum Files (Including The Active File):";
		_maxFilesNumber.Location = new Point(425, 158);
		_maxFilesNumber.Minimum = 1;
		_maxFilesNumber.Maximum = 9999;
		_maxFilesNumber.Value = RollingFileLogger.DefaultMaxFiles;
		_maxFilesNumber.Name = "_maxFilesNumber";
		_maxFilesNumber.Size = new Size(180, 23);
		_maxFilesNumber.ThousandsSeparator = true;
		_maxFileSizeLabel.Location = new Point(20, 204);
		_maxFileSizeLabel.Name = "_maxFileSizeLabel";
		_maxFileSizeLabel.Size = new Size(390, 25);
		_maxFileSizeLabel.Text = "Maximum Encoded Bytes Per File:";
		_maxFileSizeNumber.Location = new Point(425, 201);
		_maxFileSizeNumber.Minimum = 32;
		_maxFileSizeNumber.Maximum = int.MaxValue;
		_maxFileSizeNumber.Value = RollingFileLogger.DefaultMaxFileSizeB;
		_maxFileSizeNumber.Name = "_maxFileSizeNumber";
		_maxFileSizeNumber.Size = new Size(180, 23);
		_maxFileSizeNumber.ThousandsSeparator = true;
		_retentionLabel.Location = new Point(20, 252);
		_retentionLabel.Name = "_retentionLabel";
		_retentionLabel.Size = new Size(598, 55);
		_retentionLabel.Text = "The byte limit includes encoding and line endings. Oversized entries may be clipped. Unrelated files are retained.";

		Controls.Add(_titleLabel);
		Controls.Add(_introLabel);
		Controls.Add(_rollingCheckBox);
		Controls.Add(_maxFilesLabel);
		Controls.Add(_maxFilesNumber);
		Controls.Add(_maxFileSizeLabel);
		Controls.Add(_maxFileSizeNumber);
		Controls.Add(_retentionLabel);
		Name = "LogOutputRollingWizardScreen";
		Size = new Size(650, 330);
		((ISupportInitialize)_maxFilesNumber).EndInit();
		((ISupportInitialize)_maxFileSizeNumber).EndInit();
		ResumeLayout(false);
		PerformLayout();
	}
}

