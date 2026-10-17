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

partial class LogOutputDestinationWizardScreen {
	private IContainer components;
	private Label _titleLabel;
	private Label _introLabel;
	private RadioButton _fileRadioButton;
	private RadioButton _applicationRadioButton;
	private RadioButton _standardOutputRadioButton;
	private RadioButton _standardErrorRadioButton;
	private Label _availabilityLabel;

	protected override void Dispose(bool disposing) {
		if (disposing)
			components?.Dispose();
		base.Dispose(disposing);
	}

	private void InitializeComponent() {
		components = new Container();
		_titleLabel = new Label();
		_introLabel = new Label();
		_fileRadioButton = new RadioButton();
		_applicationRadioButton = new RadioButton();
		_standardOutputRadioButton = new RadioButton();
		_standardErrorRadioButton = new RadioButton();
		_availabilityLabel = new Label();

		SuspendLayout();
		_titleLabel.AutoSize = false;
		_titleLabel.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
		_titleLabel.ForeColor = SystemColors.Highlight;
		_titleLabel.Location = new Point(16, 16);
		_titleLabel.Name = "_titleLabel";
		_titleLabel.Size = new Size(618, 32);
		_titleLabel.Text = "Destination";

		_introLabel.Location = new Point(16, 56);
		_introLabel.Name = "_introLabel";
		_introLabel.Size = new Size(618, 48);
		_titleLabel.Text = "Choose The Logging Output";
		_introLabel.Text = "Select where this control sends messages. Logging stays off until you finish the wizard.";
		_fileRadioButton.AutoSize = true;
		_fileRadioButton.Location = new Point(20, 116);
		_fileRadioButton.Name = "_fileRadioButton";
		_fileRadioButton.Text = "File";
		_fileRadioButton.Checked = true;
		_fileRadioButton.UseVisualStyleBackColor = true;
		_applicationRadioButton.AutoSize = true;
		_applicationRadioButton.Location = new Point(20, 154);
		_applicationRadioButton.Name = "_applicationRadioButton";
		_applicationRadioButton.Text = "Application";
		_applicationRadioButton.Checked = false;
		_applicationRadioButton.UseVisualStyleBackColor = true;
		_standardOutputRadioButton.AutoSize = true;
		_standardOutputRadioButton.Location = new Point(20, 192);
		_standardOutputRadioButton.Name = "_standardOutputRadioButton";
		_standardOutputRadioButton.Text = "Standard Output";
		_standardOutputRadioButton.Checked = false;
		_standardOutputRadioButton.UseVisualStyleBackColor = true;
		_standardErrorRadioButton.AutoSize = true;
		_standardErrorRadioButton.Location = new Point(20, 230);
		_standardErrorRadioButton.Name = "_standardErrorRadioButton";
		_standardErrorRadioButton.Text = "Standard Error";
		_standardErrorRadioButton.Checked = false;
		_standardErrorRadioButton.UseVisualStyleBackColor = true;
		_availabilityLabel.Location = new Point(20, 272);
		_availabilityLabel.Name = "_availabilityLabel";
		_availabilityLabel.Size = new Size(614, 40);
		_availabilityLabel.Text = "Application output is available when the host supplies its logger.";

		Controls.Add(_titleLabel);
		Controls.Add(_introLabel);
		Controls.Add(_fileRadioButton);
		Controls.Add(_applicationRadioButton);
		Controls.Add(_standardOutputRadioButton);
		Controls.Add(_standardErrorRadioButton);
		Controls.Add(_availabilityLabel);
		Name = "LogOutputDestinationWizardScreen";
		Size = new Size(650, 330);

		ResumeLayout(false);
		PerformLayout();
	}
}

