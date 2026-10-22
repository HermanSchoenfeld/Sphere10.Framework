// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Utils.WinFormsTester;

partial class LoggingTestScreen {
	private System.ComponentModel.IContainer _components;
	private System.Windows.Forms.TableLayoutPanel _layout;
	private System.Windows.Forms.Label _instructions;
	private System.Windows.Forms.TableLayoutPanel _outputRow;
	private Sphere10.Framework.Windows.Forms.LogOutputSelectorControl _logOutputSelector;
	private System.Windows.Forms.Label _status;
	private System.Windows.Forms.GroupBox _messageGroup;
	private System.Windows.Forms.TableLayoutPanel _messageLayout;
	private System.Windows.Forms.Label _levelLabel;
	private System.Windows.Forms.ComboBox _level;
	private System.Windows.Forms.Label _messageLabel;
	private System.Windows.Forms.TextBox _message;
	private System.Windows.Forms.Button _send;
	private System.Windows.Forms.GroupBox _previewGroup;
	private System.Windows.Forms.TextBox _preview;
	private System.Windows.Forms.Button _clearPreview;

	protected override void Dispose(bool disposing) {
		if (disposing) {
			CloseOutput();
			_components?.Dispose();
		}
		base.Dispose(disposing);
	}

	private void InitializeComponent() {
		_components = new System.ComponentModel.Container();
		_layout = new System.Windows.Forms.TableLayoutPanel();
		_instructions = new System.Windows.Forms.Label();
		_outputRow = new System.Windows.Forms.TableLayoutPanel();
		_logOutputSelector = new Sphere10.Framework.Windows.Forms.LogOutputSelectorControl();
		_status = new System.Windows.Forms.Label();
		_messageGroup = new System.Windows.Forms.GroupBox();
		_messageLayout = new System.Windows.Forms.TableLayoutPanel();
		_levelLabel = new System.Windows.Forms.Label();
		_level = new System.Windows.Forms.ComboBox();
		_messageLabel = new System.Windows.Forms.Label();
		_message = new System.Windows.Forms.TextBox();
		_send = new System.Windows.Forms.Button();
		_previewGroup = new System.Windows.Forms.GroupBox();
		_preview = new System.Windows.Forms.TextBox();
		_clearPreview = new System.Windows.Forms.Button();
		_layout.SuspendLayout();
		_outputRow.SuspendLayout();
		_messageGroup.SuspendLayout();
		_messageLayout.SuspendLayout();
		_previewGroup.SuspendLayout();
		SuspendLayout();
		//
		// _layout
		//
		_layout.ColumnCount = 1;
		_layout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
		_layout.RowCount = 5;
		_layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 60F));
		_layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
		_layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 80F));
		_layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
		_layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 38F));
		_layout.Dock = System.Windows.Forms.DockStyle.Fill;
		_layout.Padding = new System.Windows.Forms.Padding(12);
		_layout.Name = "layout";
		_layout.Controls.Add(_instructions, 0, 0);
		_layout.Controls.Add(_outputRow, 0, 1);
		_layout.Controls.Add(_messageGroup, 0, 2);
		_layout.Controls.Add(_previewGroup, 0, 3);
		_layout.Controls.Add(_clearPreview, 0, 4);
		//
		// _instructions
		//
		_instructions.Dock = System.Windows.Forms.DockStyle.Fill;
		_instructions.Name = "instructions";
		_instructions.Text = "Choose File (append or rolling), Application, Screen, Standard Output or Standard Error in the Log Output wizard. Screen opens a detachable log view that stays available after output stops. Every Send also goes to the TextBoxLogger preview below. The selected output applies its own level and timestamp options.";
		//
		// _outputRow
		//
		_outputRow.ColumnCount = 2;
		_outputRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 215F));
		_outputRow.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
		_outputRow.RowCount = 1;
		_outputRow.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
		_outputRow.Dock = System.Windows.Forms.DockStyle.Fill;
		_outputRow.Name = "outputRow";
		_outputRow.Controls.Add(_logOutputSelector, 0, 0);
		_outputRow.Controls.Add(_status, 1, 0);
		//
		// _logOutputSelector
		//
		_logOutputSelector.Dock = System.Windows.Forms.DockStyle.Fill;
		_logOutputSelector.Name = "logOutputSelector";
		_logOutputSelector.TabIndex = 0;
		_logOutputSelector.LoggingStateChanged += LoggingStateChanged;
		_logOutputSelector.LoggingFailed += LoggingFailed;
		//
		// _status
		//
		_status.Dock = System.Windows.Forms.DockStyle.Fill;
		_status.Name = "status";
		_status.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
		_status.Text = "Output is off (NoOpLogger). Check Log Output to select a destination.";
		//
		// _messageGroup
		//
		_messageGroup.Dock = System.Windows.Forms.DockStyle.Fill;
		_messageGroup.Name = "messageGroup";
		_messageGroup.Text = "Write A Log Message";
		_messageGroup.Padding = new System.Windows.Forms.Padding(8);
		_messageGroup.Controls.Add(_messageLayout);
		_messageLayout.ColumnCount = 5;
		_messageLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 45F));
		_messageLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 110F));
		_messageLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 75F));
		_messageLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
		_messageLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 90F));
		_messageLayout.RowCount = 1;
		_messageLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
		_messageLayout.Dock = System.Windows.Forms.DockStyle.Fill;
		_messageLayout.Name = "messageLayout";
		_messageLayout.Controls.Add(_levelLabel, 0, 0);
		_messageLayout.Controls.Add(_level, 1, 0);
		_messageLayout.Controls.Add(_messageLabel, 2, 0);
		_messageLayout.Controls.Add(_message, 3, 0);
		_messageLayout.Controls.Add(_send, 4, 0);
		_levelLabel.Dock = System.Windows.Forms.DockStyle.Fill;
		_levelLabel.Name = "levelLabel";
		_levelLabel.Text = "Level";
		_levelLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
		_level.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		_level.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		_level.Name = "level";
		_level.Items.AddRange(new object[] { "Debug", "Info", "Warning", "Error", "Exception" });
		_level.TabIndex = 0;
		_messageLabel.Dock = System.Windows.Forms.DockStyle.Fill;
		_messageLabel.Name = "messageLabel";
		_messageLabel.Text = "Message";
		_messageLabel.TextAlign = System.Drawing.ContentAlignment.MiddleLeft;
		_message.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		_message.Name = "message";
		_message.Text = "Sphere10 logging demonstration";
		_message.TabIndex = 1;
		_send.Anchor = System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
		_send.Name = "send";
		_send.Text = "Send";
		_send.Size = new System.Drawing.Size(84, 28);
		_send.TabIndex = 2;
		_send.Click += SendClick;
		//
		// _previewGroup
		//
		_previewGroup.Dock = System.Windows.Forms.DockStyle.Fill;
		_previewGroup.Name = "previewGroup";
		_previewGroup.Text = "TextBoxLogger Preview (Independent Of Selected Output)";
		_previewGroup.Padding = new System.Windows.Forms.Padding(8);
		_previewGroup.Controls.Add(_preview);
		_preview.Dock = System.Windows.Forms.DockStyle.Fill;
		_preview.Name = "preview";
		_preview.Multiline = true;
		_preview.ReadOnly = true;
		_preview.ScrollBars = System.Windows.Forms.ScrollBars.Both;
		_preview.WordWrap = false;
		_preview.Font = new System.Drawing.Font("Consolas", 9F);
		//
		// _clearPreview
		//
		_clearPreview.Anchor = System.Windows.Forms.AnchorStyles.Right;
		_clearPreview.Name = "clearPreview";
		_clearPreview.Size = new System.Drawing.Size(120, 28);
		_clearPreview.Text = "Clear Preview";
		_clearPreview.Click += ClearPreviewClick;
		//
		// LoggingTestScreen
		//
		AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
		AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		Controls.Add(_layout);
		Name = "LoggingTestScreen";
		Title = "Logging";
		Size = new System.Drawing.Size(950, 580);
		_previewGroup.ResumeLayout(false);
		_previewGroup.PerformLayout();
		_messageLayout.ResumeLayout(false);
		_messageLayout.PerformLayout();
		_messageGroup.ResumeLayout(false);
		_outputRow.ResumeLayout(false);
		_layout.ResumeLayout(false);
		ResumeLayout(false);
	}
}
