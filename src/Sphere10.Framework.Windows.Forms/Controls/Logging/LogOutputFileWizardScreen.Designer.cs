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

partial class LogOutputFileWizardScreen {
	private IContainer components;
	private Label _titleLabel;
	private Label _introLabel;
	private Label _fileLabel;
	private PathSelectorControl _fileSelector;
	private Label _appendExplanationLabel;

	protected override void Dispose(bool disposing) {
		if (disposing)
			components?.Dispose();
		base.Dispose(disposing);
	}

	private void InitializeComponent() {
		components = new Container();
		_titleLabel = new Label();
		_introLabel = new Label();
		_fileLabel = new Label();
		_fileSelector = new PathSelectorControl();
		_appendExplanationLabel = new Label();

		SuspendLayout();
		_titleLabel.AutoSize = false;
		_titleLabel.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
		_titleLabel.ForeColor = SystemColors.Highlight;
		_titleLabel.Location = new Point(16, 16);
		_titleLabel.Name = "_titleLabel";
		_titleLabel.Size = new Size(618, 32);
		_titleLabel.Text = "File";

		_introLabel.Location = new Point(16, 56);
		_introLabel.Name = "_introLabel";
		_introLabel.Size = new Size(618, 48);
		_titleLabel.Text = "Select A Log File";
		_introLabel.Text = "Choose a new or existing file in an existing directory. The file is opened only when you finish.";
		_fileLabel.Location = new Point(16, 112);
		_fileLabel.Name = "_fileLabel";
		_fileLabel.Size = new Size(150, 25);
		_fileLabel.Text = "Log File:";
		_fileSelector.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right;
		_fileSelector.Location = new Point(16, 143);
		_fileSelector.Mode = PathSelectionMode.SaveFile;
		_fileSelector.OverwritePrompt = false;
		_fileSelector.Name = "_fileSelector";
		_fileSelector.Size = new Size(618, 30);
		_fileSelector.TabIndex = 0;
		_appendExplanationLabel.Location = new Point(16, 193);
		_appendExplanationLabel.Name = "_appendExplanationLabel";
		_appendExplanationLabel.Size = new Size(618, 55);
		_appendExplanationLabel.Text = "Existing contents are retained. Selecting an existing file appends entries; it does not replace the file.";

		Controls.Add(_titleLabel);
		Controls.Add(_introLabel);
		Controls.Add(_fileLabel);
		Controls.Add(_fileSelector);
		Controls.Add(_appendExplanationLabel);
		Name = "LogOutputFileWizardScreen";
		Size = new Size(650, 330);

		ResumeLayout(false);
		PerformLayout();
	}
}

