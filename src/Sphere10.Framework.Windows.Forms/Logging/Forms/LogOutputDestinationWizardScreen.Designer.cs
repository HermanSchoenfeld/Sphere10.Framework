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
	private TableLayoutPanel _layoutPanel;
	private Label _titleLabel;
	private Label _introLabel;
	private RadioButton _fileRadioButton;
	private Label _fileDescriptionLabel;
	private RadioButton _applicationRadioButton;
	private Label _applicationDescriptionLabel;
	private RadioButton _standardOutputRadioButton;
	private Label _standardOutputDescriptionLabel;
	private RadioButton _standardErrorRadioButton;
	private Label _standardErrorDescriptionLabel;
	private RadioButton _screenRadioButton;
	private Label _screenDescriptionLabel;
	private Label _availabilityLabel;

	protected override void Dispose(bool disposing) {
		if (disposing) {
			components?.Dispose();
			_optionFont?.Dispose();
		}
		base.Dispose(disposing);
	}

	private void InitializeComponent() {
		components = new Container();
		_layoutPanel = new TableLayoutPanel();
		_titleLabel = new Label();
		_introLabel = new Label();
		_fileRadioButton = new RadioButton();
		_fileDescriptionLabel = new Label();
		_applicationRadioButton = new RadioButton();
		_applicationDescriptionLabel = new Label();
		_standardOutputRadioButton = new RadioButton();
		_standardOutputDescriptionLabel = new Label();
		_standardErrorRadioButton = new RadioButton();
		_standardErrorDescriptionLabel = new Label();
		_screenRadioButton = new RadioButton();
		_screenDescriptionLabel = new Label();
		_availabilityLabel = new Label();
		_layoutPanel.SuspendLayout();
		SuspendLayout();
		//
		// _layoutPanel
		//
		_layoutPanel.AutoSize = true;
		_layoutPanel.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_layoutPanel.ColumnCount = 2;
		_layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
		_layoutPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		_layoutPanel.Controls.Add(_titleLabel, 0, 0);
		_layoutPanel.Controls.Add(_introLabel, 0, 1);
		_layoutPanel.Controls.Add(_fileRadioButton, 0, 2);
		_layoutPanel.Controls.Add(_fileDescriptionLabel, 1, 2);
		_layoutPanel.Controls.Add(_applicationRadioButton, 0, 3);
		_layoutPanel.Controls.Add(_applicationDescriptionLabel, 1, 3);
		_layoutPanel.Controls.Add(_standardOutputRadioButton, 0, 4);
		_layoutPanel.Controls.Add(_standardOutputDescriptionLabel, 1, 4);
		_layoutPanel.Controls.Add(_standardErrorRadioButton, 0, 5);
		_layoutPanel.Controls.Add(_standardErrorDescriptionLabel, 1, 5);
		_layoutPanel.Controls.Add(_screenRadioButton, 0, 6);
		_layoutPanel.Controls.Add(_screenDescriptionLabel, 1, 6);
		_layoutPanel.Controls.Add(_availabilityLabel, 0, 7);
		_layoutPanel.Dock = DockStyle.Top;
		_layoutPanel.Location = new Point(16, 16);
		_layoutPanel.Name = "_layoutPanel";
		_layoutPanel.RowCount = 8;
		_layoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layoutPanel.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layoutPanel.Size = new Size(618, 302);
		_layoutPanel.TabIndex = 0;
		//
		// _titleLabel
		//
		_titleLabel.AutoSize = true;
		_layoutPanel.SetColumnSpan(_titleLabel, 2);
		_titleLabel.Dock = DockStyle.Fill;
		_titleLabel.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
		_titleLabel.ForeColor = SystemColors.Highlight;
		_titleLabel.Margin = new Padding(0, 0, 0, 8);
		_titleLabel.Name = "_titleLabel";
		_titleLabel.TabIndex = 0;
		_titleLabel.Text = "Choose The Logging Output";
		//
		// _introLabel
		//
		_introLabel.AutoSize = true;
		_layoutPanel.SetColumnSpan(_introLabel, 2);
		_introLabel.Dock = DockStyle.Fill;
		_introLabel.Margin = new Padding(0, 0, 0, 12);
		_introLabel.Name = "_introLabel";
		_introLabel.TabIndex = 1;
		_introLabel.Text = "Select where this control sends messages. Logging stays off until you finish the wizard.";
		//
		// _fileRadioButton
		//
		_fileRadioButton.AccessibleDescription = "Save messages to a text file. Choose append or rolling files in the next steps.";
		_fileRadioButton.AutoSize = true;
		_fileRadioButton.Checked = true;
		_fileRadioButton.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
		_fileRadioButton.Margin = new Padding(4, 7, 12, 7);
		_fileRadioButton.Name = "_fileRadioButton";
		_fileRadioButton.TabIndex = 2;
		_fileRadioButton.TabStop = true;
		_fileRadioButton.Text = "File";
		_fileRadioButton.UseVisualStyleBackColor = true;
		//
		// _fileDescriptionLabel
		//
		_fileDescriptionLabel.AutoSize = true;
		_fileDescriptionLabel.Cursor = Cursors.Hand;
		_fileDescriptionLabel.Dock = DockStyle.Fill;
		_fileDescriptionLabel.Margin = new Padding(0, 7, 0, 7);
		_fileDescriptionLabel.Name = "_fileDescriptionLabel";
		_fileDescriptionLabel.TabIndex = 3;
		_fileDescriptionLabel.Text = "Save messages to a text file. Choose append or rolling files in the next steps.";
		_fileDescriptionLabel.Click += OptionDescriptionLabel_Click;
		//
		// _applicationRadioButton
		//
		_applicationRadioButton.AccessibleDescription = "Send messages to the running application's diagnostic logger supplied by the host.";
		_applicationRadioButton.AutoSize = true;
		_applicationRadioButton.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
		_applicationRadioButton.Margin = new Padding(4, 7, 12, 7);
		_applicationRadioButton.Name = "_applicationRadioButton";
		_applicationRadioButton.TabIndex = 4;
		_applicationRadioButton.Text = "Windows Application Log";
		_applicationRadioButton.UseVisualStyleBackColor = true;
		//
		// _applicationDescriptionLabel
		//
		_applicationDescriptionLabel.AutoSize = true;
		_applicationDescriptionLabel.Cursor = Cursors.Hand;
		_applicationDescriptionLabel.Dock = DockStyle.Fill;
		_applicationDescriptionLabel.Margin = new Padding(0, 7, 0, 7);
		_applicationDescriptionLabel.Name = "_applicationDescriptionLabel";
		_applicationDescriptionLabel.TabIndex = 5;
		_applicationDescriptionLabel.Text = "Send messages to the running application's diagnostic logger supplied by the host.";
		_applicationDescriptionLabel.Click += OptionDescriptionLabel_Click;
		//
		// _standardOutputRadioButton
		//
		_standardOutputRadioButton.AccessibleDescription = "Write messages to the process's standard output stream (Console.Out).";
		_standardOutputRadioButton.AutoSize = true;
		_standardOutputRadioButton.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
		_standardOutputRadioButton.Margin = new Padding(4, 7, 12, 7);
		_standardOutputRadioButton.Name = "_standardOutputRadioButton";
		_standardOutputRadioButton.TabIndex = 6;
		_standardOutputRadioButton.Text = "Standard Output";
		_standardOutputRadioButton.UseVisualStyleBackColor = true;
		//
		// _standardOutputDescriptionLabel
		//
		_standardOutputDescriptionLabel.AutoSize = true;
		_standardOutputDescriptionLabel.Cursor = Cursors.Hand;
		_standardOutputDescriptionLabel.Dock = DockStyle.Fill;
		_standardOutputDescriptionLabel.Margin = new Padding(0, 7, 0, 7);
		_standardOutputDescriptionLabel.Name = "_standardOutputDescriptionLabel";
		_standardOutputDescriptionLabel.TabIndex = 7;
		_standardOutputDescriptionLabel.Text = "Write messages to the process's standard output stream (Console.Out).";
		_standardOutputDescriptionLabel.Click += OptionDescriptionLabel_Click;
		//
		// _standardErrorRadioButton
		//
		_standardErrorRadioButton.AccessibleDescription = "Write messages to the process's standard error stream (Console.Error).";
		_standardErrorRadioButton.AutoSize = true;
		_standardErrorRadioButton.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
		_standardErrorRadioButton.Margin = new Padding(4, 7, 12, 7);
		_standardErrorRadioButton.Name = "_standardErrorRadioButton";
		_standardErrorRadioButton.TabIndex = 8;
		_standardErrorRadioButton.Text = "Standard Error";
		_standardErrorRadioButton.UseVisualStyleBackColor = true;
		//
		// _standardErrorDescriptionLabel
		//
		_standardErrorDescriptionLabel.AutoSize = true;
		_standardErrorDescriptionLabel.Cursor = Cursors.Hand;
		_standardErrorDescriptionLabel.Dock = DockStyle.Fill;
		_standardErrorDescriptionLabel.Margin = new Padding(0, 7, 0, 7);
		_standardErrorDescriptionLabel.Name = "_standardErrorDescriptionLabel";
		_standardErrorDescriptionLabel.TabIndex = 9;
		_standardErrorDescriptionLabel.Text = "Write messages to the process's standard error stream (Console.Error).";
		_standardErrorDescriptionLabel.Click += OptionDescriptionLabel_Click;
		//
		// _screenRadioButton
		//
		_screenRadioButton.AccessibleDescription = "View messages in a separate Log Output screen that stays available for review.";
		_screenRadioButton.AutoSize = true;
		_screenRadioButton.Font = new Font("Segoe UI", 9F, FontStyle.Bold);
		_screenRadioButton.Margin = new Padding(4, 7, 12, 7);
		_screenRadioButton.Name = "_screenRadioButton";
		_screenRadioButton.TabIndex = 10;
		_screenRadioButton.Text = "Screen";
		_screenRadioButton.UseVisualStyleBackColor = true;
		//
		// _screenDescriptionLabel
		//
		_screenDescriptionLabel.AutoSize = true;
		_screenDescriptionLabel.Cursor = Cursors.Hand;
		_screenDescriptionLabel.Dock = DockStyle.Fill;
		_screenDescriptionLabel.Margin = new Padding(0, 7, 0, 7);
		_screenDescriptionLabel.Name = "_screenDescriptionLabel";
		_screenDescriptionLabel.TabIndex = 11;
		_screenDescriptionLabel.Text = "View messages in a separate Log Output screen that stays available for review.";
		_screenDescriptionLabel.Click += OptionDescriptionLabel_Click;
		//
		// _availabilityLabel
		//
		_availabilityLabel.AutoSize = true;
		_layoutPanel.SetColumnSpan(_availabilityLabel, 2);
		_availabilityLabel.Dock = DockStyle.Fill;
		_availabilityLabel.Margin = new Padding(4, 8, 0, 0);
		_availabilityLabel.Name = "_availabilityLabel";
		_availabilityLabel.TabIndex = 12;
		_availabilityLabel.Text = "Windows Application Log is available when the host supplies its logger.";
		//
		// LogOutputDestinationWizardScreen
		//
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		AutoScroll = true;
		Controls.Add(_layoutPanel);
		Name = "LogOutputDestinationWizardScreen";
		Padding = new Padding(16);
		Size = new Size(650, 362);
		_layoutPanel.ResumeLayout(false);
		_layoutPanel.PerformLayout();
		ResumeLayout(false);
		PerformLayout();
	}
}

