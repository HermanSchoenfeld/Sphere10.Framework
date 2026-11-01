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

partial class LogOutputFormattingWizardScreen {
	private IContainer components;
	private Label _titleLabel;
	private Label _introLabel;
	private FlowLayoutPanel _levelsPanel;
	private CheckBox _debugCheckBox;
	private CheckBox _infoCheckBox;
	private CheckBox _warningCheckBox;
	private CheckBox _errorCheckBox;
	private CheckBox _exceptionDetailsCheckBox;
	private CheckBox _timestampCheckBox;
	private Label _encodingLabel;
	private ComboBox _encodingComboBox;
	private Label _encodingExplanationLabel;
	private CheckBox _rememberAfterCloseCheckBox;
	private Label _rememberExplanationLabel;

	protected override void Dispose(bool disposing) {
		if (disposing)
			components?.Dispose();
		base.Dispose(disposing);
	}

	private void InitializeComponent() {
		components = new Container();
		_titleLabel = new Label();
		_introLabel = new Label();
		_levelsPanel = new FlowLayoutPanel();
		_debugCheckBox = new CheckBox();
		_infoCheckBox = new CheckBox();
		_warningCheckBox = new CheckBox();
		_errorCheckBox = new CheckBox();
		_exceptionDetailsCheckBox = new CheckBox();
		_timestampCheckBox = new CheckBox();
		_encodingLabel = new Label();
		_encodingComboBox = new ComboBox();
		_encodingExplanationLabel = new Label();
		_rememberAfterCloseCheckBox = new CheckBox();
		_rememberExplanationLabel = new Label();
		_levelsPanel.SuspendLayout();
		SuspendLayout();
		_titleLabel.AutoSize = false;
		_titleLabel.Font = new Font("Segoe UI", 12F, FontStyle.Bold);
		_titleLabel.ForeColor = SystemColors.Highlight;
		_titleLabel.Location = new Point(16, 16);
		_titleLabel.Name = "_titleLabel";
		_titleLabel.Size = new Size(618, 32);
		_titleLabel.Text = "Formatting";

		_introLabel.Location = new Point(16, 56);
		_introLabel.Name = "_introLabel";
		_introLabel.Size = new Size(618, 48);
		_titleLabel.Text = "Levels And Formatting";
		_introLabel.Text = "Choose the messages and detail this output receives. The host may apply additional filtering to its application logger.";
		_levelsPanel.Location = new Point(16, 112);
		_levelsPanel.Name = "_levelsPanel";
		_levelsPanel.Size = new Size(618, 34);
		_levelsPanel.Controls.Add(_debugCheckBox);
		_levelsPanel.Controls.Add(_infoCheckBox);
		_levelsPanel.Controls.Add(_warningCheckBox);
		_levelsPanel.Controls.Add(_errorCheckBox);
		_debugCheckBox.AutoSize = true;
		_debugCheckBox.Name = "_debugCheckBox";
		_debugCheckBox.Text = "Debug";
		_infoCheckBox.AutoSize = true;
		_infoCheckBox.Name = "_infoCheckBox";
		_infoCheckBox.Text = "Information";
		_warningCheckBox.AutoSize = true;
		_warningCheckBox.Name = "_warningCheckBox";
		_warningCheckBox.Text = "Warning";
		_errorCheckBox.AutoSize = true;
		_errorCheckBox.Name = "_errorCheckBox";
		_errorCheckBox.Text = "Error";
		_timestampCheckBox.AutoSize = true;
		_timestampCheckBox.Location = new Point(20, 158);
		_timestampCheckBox.Name = "_timestampCheckBox";
		_timestampCheckBox.Text = "Include Timestamp (Local Time, Milliseconds And UTC Offset)";
		_exceptionDetailsCheckBox.AutoSize = true;
		_exceptionDetailsCheckBox.Location = new Point(20, 192);
		_exceptionDetailsCheckBox.Name = "_exceptionDetailsCheckBox";
		_exceptionDetailsCheckBox.Text = "Include Exception Details And Stack Traces";
		_encodingLabel.Location = new Point(20, 235);
		_encodingLabel.Name = "_encodingLabel";
		_encodingLabel.Size = new Size(120, 25);
		_encodingLabel.Text = "File Encoding:";
		_encodingComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
		_encodingComboBox.Items.AddRange(new object[] { "UTF-8", "UTF-16", "ASCII" });
		_encodingComboBox.Location = new Point(150, 232);
		_encodingComboBox.Name = "_encodingComboBox";
		_encodingComboBox.Size = new Size(150, 23);
		_encodingComboBox.SelectedIndex = 0;
		_encodingExplanationLabel.Location = new Point(20, 274);
		_encodingExplanationLabel.Name = "_encodingExplanationLabel";
		_encodingExplanationLabel.Size = new Size(598, 45);
		_encodingExplanationLabel.Text = "";
		_rememberAfterCloseCheckBox.AutoSize = true;
		_rememberAfterCloseCheckBox.Location = new Point(20, 324);
		_rememberAfterCloseCheckBox.Name = "_rememberAfterCloseCheckBox";
		_rememberAfterCloseCheckBox.Text = "Remember After Close";
		_rememberExplanationLabel.Location = new Point(20, 351);
		_rememberExplanationLabel.Name = "_rememberExplanationLabel";
		_rememberExplanationLabel.Size = new Size(598, 34);
		_rememberExplanationLabel.Text = "Restore this output and its on/off choice the next time this screen opens.";

		Controls.Add(_titleLabel);
		Controls.Add(_introLabel);
		Controls.Add(_levelsPanel);
		Controls.Add(_timestampCheckBox);
		Controls.Add(_exceptionDetailsCheckBox);
		Controls.Add(_encodingLabel);
		Controls.Add(_encodingComboBox);
		Controls.Add(_encodingExplanationLabel);
		Controls.Add(_rememberAfterCloseCheckBox);
		Controls.Add(_rememberExplanationLabel);
		Name = "LogOutputFormattingWizardScreen";
		Size = new Size(650, 400);
		_levelsPanel.ResumeLayout(false);
		_levelsPanel.PerformLayout();
		ResumeLayout(false);
		PerformLayout();
	}
}

