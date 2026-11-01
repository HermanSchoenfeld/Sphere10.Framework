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

partial class LogOutputReviewWizardScreen {
	private IContainer components;
	private Label _titleLabel;
	private Label _introLabel;
	private TextBox _detailsTextBox;
	private Label _finishExplanationLabel;

	protected override void Dispose(bool disposing) {
		if (disposing)
			components?.Dispose();
		base.Dispose(disposing);
	}

	private void InitializeComponent() {
		components = new Container();
		_titleLabel = new Label();
		_introLabel = new Label();
		_detailsTextBox = new TextBox();
		_finishExplanationLabel = new Label();

		SuspendLayout();
		_titleLabel.AutoSize = false;
		_titleLabel.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
		_titleLabel.ForeColor = SystemColors.Highlight;
		_titleLabel.Location = new Point(16, 16);
		_titleLabel.Name = "_titleLabel";
		_titleLabel.Size = new Size(618, 32);
		_titleLabel.Text = "Review";

		_introLabel.Location = new Point(16, 56);
		_introLabel.Name = "_introLabel";
		_introLabel.Size = new Size(618, 48);
		_titleLabel.Text = "Review The Logging Output";
		_introLabel.Text = "Check the selected output before enabling it. Use Previous to change any option.";
		_detailsTextBox.Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right | AnchorStyles.Bottom;
		_detailsTextBox.Location = new Point(16, 112);
		_detailsTextBox.Multiline = true;
		_detailsTextBox.Name = "_detailsTextBox";
		_detailsTextBox.ReadOnly = true;
		_detailsTextBox.ScrollBars = ScrollBars.Vertical;
		_detailsTextBox.Size = new Size(618, 160);
		_finishExplanationLabel.Location = new Point(16, 285);
		_finishExplanationLabel.Name = "_finishExplanationLabel";
		_finishExplanationLabel.Size = new Size(618, 35);
		_finishExplanationLabel.Text = "Finish opens the selected output and checks Log Output. Cancel leaves the current output unchanged.";

		Controls.Add(_titleLabel);
		Controls.Add(_introLabel);
		Controls.Add(_detailsTextBox);
		Controls.Add(_finishExplanationLabel);
		Name = "LogOutputReviewWizardScreen";
		Size = new Size(650, 330);

		ResumeLayout(false);
		PerformLayout();
	}
}

