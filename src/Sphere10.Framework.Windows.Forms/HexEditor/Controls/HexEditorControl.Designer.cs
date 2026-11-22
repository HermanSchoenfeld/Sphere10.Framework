// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows.Forms;

partial class HexEditorControl {
	/// <summary>
	/// Required designer variable.
	/// </summary>
	private System.ComponentModel.IContainer components = null;

	/// <summary>
	/// Clean up any resources being used.
	/// </summary>
	/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
	protected override void Dispose(bool disposing) {
		if (disposing) {
			CancelSearch();
			ReleaseDocument();
			components?.Dispose();
		}
		base.Dispose(disposing);
	}

	#region Component Designer generated code

	/// <summary>
	/// Required method for Designer support - do not modify
	/// the contents of this method with the code editor.
	/// </summary>
	private void InitializeComponent() {
		components = new System.ComponentModel.Container();
		_toolStrip = new System.Windows.Forms.ToolStrip();
		_saveButton = new System.Windows.Forms.ToolStripButton();
		_revertButton = new System.Windows.Forms.ToolStripButton();
		_navigationSeparator = new System.Windows.Forms.ToolStripSeparator();
		_goToButton = new System.Windows.Forms.ToolStripButton();
		_findButton = new System.Windows.Forms.ToolStripButton();
		_findNextButton = new System.Windows.Forms.ToolStripButton();
		_findPreviousButton = new System.Windows.Forms.ToolStripButton();
		_clipboardSeparator = new System.Windows.Forms.ToolStripSeparator();
		_copyHexButton = new System.Windows.Forms.ToolStripButton();
		_copyTextButton = new System.Windows.Forms.ToolStripButton();
		_selectAllButton = new System.Windows.Forms.ToolStripButton();
		_pasteButton = new System.Windows.Forms.ToolStripButton();
		_insertModeButton = new System.Windows.Forms.ToolStripButton();
		_layoutSeparator = new System.Windows.Forms.ToolStripSeparator();
		_bytesPerRowLabel = new System.Windows.Forms.ToolStripLabel();
		_bytesPerRowComboBox = new System.Windows.Forms.ToolStripComboBox();
		_inspectorButton = new System.Windows.Forms.ToolStripButton();
		_contentPanel = new System.Windows.Forms.TableLayoutPanel();
		_view = new Sphere10.Framework.Windows.Forms.HexEditorView();
		_inspectorPanel = new System.Windows.Forms.Panel();
		_inspectorListView = new System.Windows.Forms.ListView();
		_inspectorTypeColumn = new System.Windows.Forms.ColumnHeader();
		_inspectorValueColumn = new System.Windows.Forms.ColumnHeader();
		_bigEndianCheckBox = new System.Windows.Forms.CheckBox();
		_statusStrip = new System.Windows.Forms.StatusStrip();
		_offsetStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
		_selectionStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
		_lengthStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
		_changesStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
		_messageStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
		_modeStatusLabel = new System.Windows.Forms.ToolStripStatusLabel();
		_toolStrip.SuspendLayout();
		_contentPanel.SuspendLayout();
		_inspectorPanel.SuspendLayout();
		_statusStrip.SuspendLayout();
		SuspendLayout();
		//
		// _toolStrip
		//
		_toolStrip.Dock = System.Windows.Forms.DockStyle.Top;
		_toolStrip.GripStyle = System.Windows.Forms.ToolStripGripStyle.Hidden;
		_toolStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { _saveButton, _revertButton, _navigationSeparator, _goToButton, _findButton, _findNextButton, _findPreviousButton, _clipboardSeparator, _copyHexButton, _copyTextButton, _pasteButton, _selectAllButton, _insertModeButton, _layoutSeparator, _bytesPerRowLabel, _bytesPerRowComboBox, _inspectorButton });
		_toolStrip.Location = new System.Drawing.Point(0, 0);
		_toolStrip.Name = "_toolStrip";
		_toolStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
		_toolStrip.Size = new System.Drawing.Size(900, 25);
		_toolStrip.TabIndex = 2;
		_toolStrip.TabStop = false;
		//
		// _saveButton
		//
		_saveButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
		_saveButton.Name = "_saveButton";
		_saveButton.Size = new System.Drawing.Size(35, 22);
		_saveButton.Text = "&Save";
		_saveButton.ToolTipText = "Write pending edits to the stream (Ctrl+S)";
		_saveButton.Click += SaveButtonClicked;
		//
		// _revertButton
		//
		_revertButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
		_revertButton.Name = "_revertButton";
		_revertButton.Size = new System.Drawing.Size(44, 22);
		_revertButton.Text = "&Revert";
		_revertButton.ToolTipText = "Discard pending edits";
		_revertButton.Click += RevertButtonClicked;
		//
		// _navigationSeparator
		//
		_navigationSeparator.Name = "_navigationSeparator";
		_navigationSeparator.Size = new System.Drawing.Size(6, 25);
		//
		// _goToButton
		//
		_goToButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
		_goToButton.Name = "_goToButton";
		_goToButton.Size = new System.Drawing.Size(50, 22);
		_goToButton.Text = "&Go To...";
		_goToButton.ToolTipText = "Jump to an offset (Ctrl+G)";
		_goToButton.Click += GoToButtonClicked;
		//
		// _findButton
		//
		_findButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
		_findButton.Name = "_findButton";
		_findButton.Size = new System.Drawing.Size(43, 22);
		_findButton.Text = "&Find...";
		_findButton.ToolTipText = "Find a byte sequence or text (Ctrl+F)";
		_findButton.Click += FindButtonClicked;
		//
		// _findNextButton
		//
		_findNextButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
		_findNextButton.Name = "_findNextButton";
		_findNextButton.Size = new System.Drawing.Size(62, 22);
		_findNextButton.Text = "Find &Next";
		_findNextButton.ToolTipText = "Repeat the last search forward (F3)";
		_findNextButton.Click += FindNextButtonClicked;
		//
		// _findPreviousButton
		//
		_findPreviousButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
		_findPreviousButton.Name = "_findPreviousButton";
		_findPreviousButton.Size = new System.Drawing.Size(82, 22);
		_findPreviousButton.Text = "Find &Previous";
		_findPreviousButton.ToolTipText = "Repeat the last search backward (Shift+F3)";
		_findPreviousButton.Click += FindPreviousButtonClicked;
		//
		// _clipboardSeparator
		//
		_clipboardSeparator.Name = "_clipboardSeparator";
		_clipboardSeparator.Size = new System.Drawing.Size(6, 25);
		//
		// _copyHexButton
		//
		_copyHexButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
		_copyHexButton.Name = "_copyHexButton";
		_copyHexButton.Size = new System.Drawing.Size(63, 22);
		_copyHexButton.Text = "Copy &Hex";
		_copyHexButton.ToolTipText = "Copy the selection as hexadecimal pairs";
		_copyHexButton.Click += CopyHexButtonClicked;
		//
		// _copyTextButton
		//
		_copyTextButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
		_copyTextButton.Name = "_copyTextButton";
		_copyTextButton.Size = new System.Drawing.Size(63, 22);
		_copyTextButton.Text = "Copy &Text";
		_copyTextButton.ToolTipText = "Copy the selection as text";
		_copyTextButton.Click += CopyTextButtonClicked;
		//
		// _selectAllButton
		//
		_selectAllButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
		_selectAllButton.Name = "_selectAllButton";
		_selectAllButton.Size = new System.Drawing.Size(59, 22);
		_selectAllButton.Text = "Select &All";
		_selectAllButton.ToolTipText = "Select every byte (Ctrl+A)";
		_selectAllButton.Click += SelectAllButtonClicked;
		//
		// _pasteButton
		//
		_pasteButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
		_pasteButton.Name = "_pasteButton";
		_pasteButton.Size = new System.Drawing.Size(39, 22);
		_pasteButton.Text = "&Paste";
		_pasteButton.ToolTipText = "Paste clipboard hex pairs or text at the caret (Ctrl+V)";
		_pasteButton.Click += PasteButtonClicked;
		//
		// _insertModeButton
		//
		_insertModeButton.CheckOnClick = true;
		_insertModeButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
		_insertModeButton.Name = "_insertModeButton";
		_insertModeButton.Size = new System.Drawing.Size(40, 22);
		_insertModeButton.Text = "I&nsert";
		_insertModeButton.ToolTipText = "Insert new bytes while typing or pasting instead of overwriting (Insert key)";
		_insertModeButton.Click += InsertModeButtonClicked;
		//
		// _layoutSeparator
		//
		_layoutSeparator.Name = "_layoutSeparator";
		_layoutSeparator.Size = new System.Drawing.Size(6, 25);
		//
		// _bytesPerRowLabel
		//
		_bytesPerRowLabel.Name = "_bytesPerRowLabel";
		_bytesPerRowLabel.Size = new System.Drawing.Size(81, 22);
		_bytesPerRowLabel.Text = "Bytes per row:";
		//
		// _bytesPerRowComboBox
		//
		_bytesPerRowComboBox.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
		_bytesPerRowComboBox.Items.AddRange(new object[] { "8", "16", "24", "32", "48", "64" });
		_bytesPerRowComboBox.Name = "_bytesPerRowComboBox";
		_bytesPerRowComboBox.Size = new System.Drawing.Size(60, 25);
		_bytesPerRowComboBox.ToolTipText = "Bytes shown in each row. Drag the line between the hex and text panes for any other number.";
		_bytesPerRowComboBox.SelectedIndexChanged += BytesPerRowComboBoxSelectionChanged;
		//
		// _inspectorButton
		//
		_inspectorButton.Alignment = System.Windows.Forms.ToolStripItemAlignment.Right;
		_inspectorButton.CheckOnClick = true;
		_inspectorButton.Checked = true;
		_inspectorButton.DisplayStyle = System.Windows.Forms.ToolStripItemDisplayStyle.Text;
		_inspectorButton.Name = "_inspectorButton";
		_inspectorButton.Size = new System.Drawing.Size(60, 19);
		_inspectorButton.Text = "&Inspector";
		_inspectorButton.ToolTipText = "Show or hide the data inspector";
		_inspectorButton.Click += InspectorButtonClicked;
		//
		// _contentPanel
		//
		_contentPanel.ColumnCount = 2;
		_contentPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
		_contentPanel.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.AutoSize));
		_contentPanel.Controls.Add(_view, 0, 0);
		_contentPanel.Controls.Add(_inspectorPanel, 1, 0);
		_contentPanel.Dock = System.Windows.Forms.DockStyle.Fill;
		_contentPanel.Location = new System.Drawing.Point(0, 25);
		_contentPanel.Margin = new System.Windows.Forms.Padding(0);
		_contentPanel.Name = "_contentPanel";
		_contentPanel.RowCount = 1;
		_contentPanel.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
		_contentPanel.Size = new System.Drawing.Size(900, 451);
		_contentPanel.TabIndex = 0;
		//
		// _view
		//
		_view.Dock = System.Windows.Forms.DockStyle.Fill;
		_view.Location = new System.Drawing.Point(0, 0);
		_view.Margin = new System.Windows.Forms.Padding(0);
		_view.Name = "_view";
		_view.Size = new System.Drawing.Size(660, 451);
		_view.TabIndex = 0;
		_view.BytesPerRowRequested += ViewBytesPerRowRequested;
		//
		// _inspectorPanel
		//
		_inspectorPanel.Controls.Add(_inspectorListView);
		_inspectorPanel.Controls.Add(_bigEndianCheckBox);
		_inspectorPanel.Dock = System.Windows.Forms.DockStyle.Fill;
		_inspectorPanel.Location = new System.Drawing.Point(660, 0);
		_inspectorPanel.Margin = new System.Windows.Forms.Padding(0);
		_inspectorPanel.MinimumSize = new System.Drawing.Size(240, 0);
		_inspectorPanel.Name = "_inspectorPanel";
		_inspectorPanel.Padding = new System.Windows.Forms.Padding(4, 0, 0, 0);
		_inspectorPanel.Size = new System.Drawing.Size(240, 300);
		_inspectorPanel.TabIndex = 1;
		//
		// _inspectorListView
		//
		_inspectorListView.Columns.AddRange(new System.Windows.Forms.ColumnHeader[] { _inspectorTypeColumn, _inspectorValueColumn });
		_inspectorListView.Dock = System.Windows.Forms.DockStyle.Fill;
		_inspectorListView.FullRowSelect = true;
		_inspectorListView.HeaderStyle = System.Windows.Forms.ColumnHeaderStyle.Nonclickable;
		_inspectorListView.Location = new System.Drawing.Point(4, 0);
		_inspectorListView.MultiSelect = false;
		_inspectorListView.Name = "_inspectorListView";
		_inspectorListView.Size = new System.Drawing.Size(236, 425);
		_inspectorListView.TabIndex = 0;
		_inspectorListView.TabStop = false;
		_inspectorListView.UseCompatibleStateImageBehavior = false;
		_inspectorListView.View = System.Windows.Forms.View.Details;
		//
		// _inspectorTypeColumn
		//
		_inspectorTypeColumn.Name = "_inspectorTypeColumn";
		_inspectorTypeColumn.Text = "Type";
		_inspectorTypeColumn.Width = 70;
		//
		// _inspectorValueColumn
		//
		_inspectorValueColumn.Name = "_inspectorValueColumn";
		_inspectorValueColumn.Text = "Value";
		_inspectorValueColumn.Width = 150;
		//
		// _bigEndianCheckBox
		//
		_bigEndianCheckBox.AutoSize = true;
		_bigEndianCheckBox.Dock = System.Windows.Forms.DockStyle.Bottom;
		_bigEndianCheckBox.Location = new System.Drawing.Point(4, 425);
		_bigEndianCheckBox.Name = "_bigEndianCheckBox";
		_bigEndianCheckBox.Padding = new System.Windows.Forms.Padding(2);
		_bigEndianCheckBox.Size = new System.Drawing.Size(236, 26);
		_bigEndianCheckBox.TabIndex = 1;
		_bigEndianCheckBox.Text = "Big &endian";
		_bigEndianCheckBox.CheckedChanged += BigEndianCheckBoxChanged;
		//
		// _statusStrip
		//
		_statusStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] { _offsetStatusLabel, _selectionStatusLabel, _lengthStatusLabel, _changesStatusLabel, _messageStatusLabel, _modeStatusLabel });
		_statusStrip.Location = new System.Drawing.Point(0, 476);
		_statusStrip.Name = "_statusStrip";
		_statusStrip.RenderMode = System.Windows.Forms.ToolStripRenderMode.System;
		_statusStrip.Size = new System.Drawing.Size(900, 24);
		_statusStrip.SizingGrip = false;
		_statusStrip.TabIndex = 1;
		//
		// _offsetStatusLabel
		//
		_offsetStatusLabel.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Right;
		_offsetStatusLabel.Name = "_offsetStatusLabel";
		_offsetStatusLabel.Size = new System.Drawing.Size(85, 19);
		//
		// _selectionStatusLabel
		//
		_selectionStatusLabel.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Right;
		_selectionStatusLabel.Name = "_selectionStatusLabel";
		_selectionStatusLabel.Size = new System.Drawing.Size(4, 19);
		//
		// _lengthStatusLabel
		//
		_lengthStatusLabel.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Right;
		_lengthStatusLabel.Name = "_lengthStatusLabel";
		_lengthStatusLabel.Size = new System.Drawing.Size(4, 19);
		//
		// _changesStatusLabel
		//
		_changesStatusLabel.Name = "_changesStatusLabel";
		_changesStatusLabel.Size = new System.Drawing.Size(0, 19);
		//
		// _messageStatusLabel
		//
		_messageStatusLabel.Name = "_messageStatusLabel";
		_messageStatusLabel.Size = new System.Drawing.Size(785, 19);
		_messageStatusLabel.Spring = true;
		_messageStatusLabel.TextAlign = System.Drawing.ContentAlignment.MiddleRight;
		//
		// _modeStatusLabel
		//
		_modeStatusLabel.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
		_modeStatusLabel.Name = "_modeStatusLabel";
		_modeStatusLabel.Size = new System.Drawing.Size(4, 19);
		//
		// HexEditorControl
		//
		AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
		AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		Controls.Add(_contentPanel);
		Controls.Add(_statusStrip);
		Controls.Add(_toolStrip);
		Name = "HexEditorControl";
		Size = new System.Drawing.Size(900, 500);
		_toolStrip.ResumeLayout(false);
		_toolStrip.PerformLayout();
		_contentPanel.ResumeLayout(false);
		_inspectorPanel.ResumeLayout(false);
		_statusStrip.ResumeLayout(false);
		_statusStrip.PerformLayout();
		ResumeLayout(false);
		PerformLayout();
	}

	#endregion

	private System.Windows.Forms.ToolStrip _toolStrip;
	private System.Windows.Forms.ToolStripButton _saveButton;
	private System.Windows.Forms.ToolStripButton _revertButton;
	private System.Windows.Forms.ToolStripSeparator _navigationSeparator;
	private System.Windows.Forms.ToolStripButton _goToButton;
	private System.Windows.Forms.ToolStripButton _findButton;
	private System.Windows.Forms.ToolStripButton _findNextButton;
	private System.Windows.Forms.ToolStripButton _findPreviousButton;
	private System.Windows.Forms.ToolStripSeparator _clipboardSeparator;
	private System.Windows.Forms.ToolStripButton _copyHexButton;
	private System.Windows.Forms.ToolStripButton _copyTextButton;
	private System.Windows.Forms.ToolStripButton _selectAllButton;
	private System.Windows.Forms.ToolStripButton _pasteButton;
	private System.Windows.Forms.ToolStripButton _insertModeButton;
	private System.Windows.Forms.ToolStripSeparator _layoutSeparator;
	private System.Windows.Forms.ToolStripLabel _bytesPerRowLabel;
	private System.Windows.Forms.ToolStripComboBox _bytesPerRowComboBox;
	private System.Windows.Forms.ToolStripButton _inspectorButton;
	private System.Windows.Forms.TableLayoutPanel _contentPanel;
	private Sphere10.Framework.Windows.Forms.HexEditorView _view;
	private System.Windows.Forms.Panel _inspectorPanel;
	private System.Windows.Forms.ListView _inspectorListView;
	private System.Windows.Forms.ColumnHeader _inspectorTypeColumn;
	private System.Windows.Forms.ColumnHeader _inspectorValueColumn;
	private System.Windows.Forms.CheckBox _bigEndianCheckBox;
	private System.Windows.Forms.StatusStrip _statusStrip;
	private System.Windows.Forms.ToolStripStatusLabel _offsetStatusLabel;
	private System.Windows.Forms.ToolStripStatusLabel _selectionStatusLabel;
	private System.Windows.Forms.ToolStripStatusLabel _lengthStatusLabel;
	private System.Windows.Forms.ToolStripStatusLabel _changesStatusLabel;
	private System.Windows.Forms.ToolStripStatusLabel _messageStatusLabel;
	private System.Windows.Forms.ToolStripStatusLabel _modeStatusLabel;
}
