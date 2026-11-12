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

partial class HotKeyCommandEditor {
	private IContainer components = null;
	private TableLayoutPanel _layout;
	private ListView _commandsListView;
	private ColumnHeader _commandColumn;
	private ColumnHeader _shortcutColumn;
	private ColumnHeader _statusColumn;
	private FlowLayoutPanel _editPanel;
	private Label _shortcutLabel;
	private HotKeyEditor _shortcutEditor;
	private Button _clearButton;
	private Button _defaultButton;
	private Label _summaryLabel;

	protected override void Dispose(bool disposing) {
		if (disposing && components != null)
			components.Dispose();
		base.Dispose(disposing);
	}

	#region Component Designer generated code

	private void InitializeComponent() {
		_layout = new TableLayoutPanel();
		_commandsListView = new ListView();
		_commandColumn = new ColumnHeader();
		_shortcutColumn = new ColumnHeader();
		_statusColumn = new ColumnHeader();
		_editPanel = new FlowLayoutPanel();
		_shortcutLabel = new Label();
		_shortcutEditor = new HotKeyEditor();
		_clearButton = new Button();
		_defaultButton = new Button();
		_summaryLabel = new Label();
		_layout.SuspendLayout();
		_editPanel.SuspendLayout();
		SuspendLayout();
		//
		// _layout
		//
		_layout.ColumnCount = 1;
		_layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		_layout.Controls.Add(_commandsListView, 0, 0);
		_layout.Controls.Add(_shortcutLabel, 0, 1);
		_layout.Controls.Add(_editPanel, 0, 2);
		_layout.Controls.Add(_summaryLabel, 0, 3);
		_layout.Dock = DockStyle.Fill;
		_layout.Location = new Point(0, 0);
		_layout.Margin = new Padding(0);
		_layout.Name = "_layout";
		_layout.RowCount = 4;
		_layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
		_layout.RowStyles.Add(new RowStyle());
		_layout.RowStyles.Add(new RowStyle());
		_layout.RowStyles.Add(new RowStyle());
		_layout.Size = new Size(600, 360);
		_layout.TabIndex = 0;
		//
		// _commandsListView
		//
		_commandsListView.Columns.AddRange(new[] { _commandColumn, _shortcutColumn, _statusColumn });
		_commandsListView.Dock = DockStyle.Fill;
		_commandsListView.FullRowSelect = true;
		_commandsListView.HeaderStyle = ColumnHeaderStyle.Nonclickable;
		_commandsListView.HideSelection = false;
		_commandsListView.MultiSelect = false;
		_commandsListView.Name = "_commandsListView";
		_commandsListView.ShowGroups = true;
		_commandsListView.TabIndex = 0;
		_commandsListView.UseCompatibleStateImageBehavior = false;
		_commandsListView.View = View.Details;
		_commandsListView.SelectedIndexChanged += _commandsListView_SelectedIndexChanged;
		//
		// _commandColumn
		//
		_commandColumn.Text = "Command";
		_commandColumn.Width = 230;
		//
		// _shortcutColumn
		//
		_shortcutColumn.Text = "Shortcut";
		_shortcutColumn.Width = 150;
		//
		// _statusColumn
		//
		_statusColumn.Text = "Status";
		_statusColumn.Width = 200;
		//
		// _shortcutLabel
		//
		_shortcutLabel.AutoSize = true;
		_shortcutLabel.Margin = new Padding(3, 9, 3, 0);
		_shortcutLabel.Name = "_shortcutLabel";
		_shortcutLabel.Text = "Select a command to change its shortcut";
		//
		// _editPanel
		//
		_editPanel.AutoSize = true;
		_editPanel.Controls.Add(_shortcutEditor);
		_editPanel.Controls.Add(_clearButton);
		_editPanel.Controls.Add(_defaultButton);
		_editPanel.Dock = DockStyle.Fill;
		_editPanel.Margin = new Padding(0);
		_editPanel.Name = "_editPanel";
		_editPanel.TabIndex = 1;
		_editPanel.WrapContents = false;
		//
		// _shortcutEditor
		//
		_shortcutEditor.Anchor = AnchorStyles.Left;
		_shortcutEditor.Name = "_shortcutEditor";
		_shortcutEditor.TabIndex = 0;
		_shortcutEditor.ShortcutChanged += _shortcutEditor_ShortcutChanged;
		//
		// _clearButton
		//
		_clearButton.Anchor = AnchorStyles.Left;
		_clearButton.AutoSize = true;
		_clearButton.Margin = new Padding(12, 3, 3, 3);
		_clearButton.Name = "_clearButton";
		_clearButton.Padding = new Padding(8, 2, 8, 2);
		_clearButton.TabIndex = 1;
		_clearButton.Text = "Clear";
		_clearButton.UseVisualStyleBackColor = true;
		_clearButton.Click += _clearButton_Click;
		//
		// _defaultButton
		//
		_defaultButton.Anchor = AnchorStyles.Left;
		_defaultButton.AutoSize = true;
		_defaultButton.Name = "_defaultButton";
		_defaultButton.Padding = new Padding(8, 2, 8, 2);
		_defaultButton.TabIndex = 2;
		_defaultButton.Text = "Default";
		_defaultButton.UseVisualStyleBackColor = true;
		_defaultButton.Click += _defaultButton_Click;
		//
		// _summaryLabel
		//
		_summaryLabel.AutoSize = true;
		_summaryLabel.ForeColor = Color.ForestGreen;
		_summaryLabel.Margin = new Padding(3, 6, 3, 3);
		_summaryLabel.Name = "_summaryLabel";
		_summaryLabel.Text = "All shortcuts are registered.";
		//
		// HotKeyCommandEditor
		//
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		Controls.Add(_layout);
		Name = "HotKeyCommandEditor";
		Size = new Size(600, 360);
		_layout.ResumeLayout(false);
		_layout.PerformLayout();
		_editPanel.ResumeLayout(false);
		_editPanel.PerformLayout();
		ResumeLayout(false);
	}

	#endregion
}
