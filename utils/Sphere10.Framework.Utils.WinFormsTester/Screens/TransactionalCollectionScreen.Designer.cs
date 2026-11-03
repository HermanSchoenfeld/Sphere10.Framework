// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

partial class TransactionalCollectionScreen {
	private TableLayoutPanel _layout;
	private Label _heading;
	private Label _introduction;
	private FlowLayoutPanel _inputs;
	private FlowLayoutPanel _itemsInput;
	private FlowLayoutPanel _batchInput;
	private FlowLayoutPanel _itemSizeInput;
	private FlowLayoutPanel _clusterSizeInput;
	private FlowLayoutPanel _pageSizeInput;
	private FlowLayoutPanel _cacheSizeInput;
	private FlowLayoutPanel _backendInput;
	private FlowLayoutPanel _policyInput;
	private FlowLayoutPanel _commands;
	private TextBoxEx _outputTextBox;
	private Label _status;
	private Label _itemsIntBoxLabel;
	private IntBox _itemsIntBox;
	private Label _batchIntBoxLabel;
	private IntBox _batchIntBox;
	private Label _itemSizeIntBoxLabel;
	private IntBox _itemSizeIntBox;
	private Label _clusterSizeIntBoxLabel;
	private IntBox _clusterSizeIntBox;
	private Label _pageSizeIntBoxLabel;
	private IntBox _pageSizeIntBox;
	private Label _cacheSizeIntBoxLabel;
	private IntBox _cacheSizeIntBox;
	private Label _backendLabel;
	private EnumComboBox _listTypeComboBox;
	private CheckBox _commitCheckBox;
	private Label _policyLabel;
	private FlagsCheckedListBox _policyBox;
	private Button _listAppendTestButton;
	private Button _bufferTestButton;
	private Button _dictionaryAppend;
	private Button _storageStreamButton;
	private Button _rollbackButton;
	private Button _cancelButton;
	private Button _copyButton;
	private Button _clearButton;

	// The complete static demonstration layout remains editable on the designer surface.
	private void InitializeComponent() {
		_layout = new TableLayoutPanel();
		_heading = new Label();
		_introduction = new Label();
		_inputs = new FlowLayoutPanel();
		_itemsInput = new FlowLayoutPanel();
		_batchInput = new FlowLayoutPanel();
		_itemSizeInput = new FlowLayoutPanel();
		_clusterSizeInput = new FlowLayoutPanel();
		_pageSizeInput = new FlowLayoutPanel();
		_cacheSizeInput = new FlowLayoutPanel();
		_backendInput = new FlowLayoutPanel();
		_policyInput = new FlowLayoutPanel();
		_commands = new FlowLayoutPanel();
		_outputTextBox = new TextBoxEx();
		_status = new Label();
		_itemsIntBoxLabel = new Label();
		_itemsIntBox = new IntBox();
		_batchIntBoxLabel = new Label();
		_batchIntBox = new IntBox();
		_itemSizeIntBoxLabel = new Label();
		_itemSizeIntBox = new IntBox();
		_clusterSizeIntBoxLabel = new Label();
		_clusterSizeIntBox = new IntBox();
		_pageSizeIntBoxLabel = new Label();
		_pageSizeIntBox = new IntBox();
		_cacheSizeIntBoxLabel = new Label();
		_cacheSizeIntBox = new IntBox();
		_backendLabel = new Label();
		_listTypeComboBox = new EnumComboBox();
		_commitCheckBox = new CheckBox();
		_policyLabel = new Label();
		_policyBox = new FlagsCheckedListBox();
		_listAppendTestButton = new Button();
		_bufferTestButton = new Button();
		_dictionaryAppend = new Button();
		_storageStreamButton = new Button();
		_rollbackButton = new Button();
		_cancelButton = new Button();
		_copyButton = new Button();
		_clearButton = new Button();
		_layout.SuspendLayout();
		_inputs.SuspendLayout();
		_itemsInput.SuspendLayout();
		_batchInput.SuspendLayout();
		_itemSizeInput.SuspendLayout();
		_clusterSizeInput.SuspendLayout();
		_pageSizeInput.SuspendLayout();
		_cacheSizeInput.SuspendLayout();
		_backendInput.SuspendLayout();
		_policyInput.SuspendLayout();
		_commands.SuspendLayout();
		SuspendLayout();

		_layout.Dock = DockStyle.Fill;
		_layout.ColumnCount = 1;
		_layout.RowCount = 6;
		_layout.Padding = new Padding(20);
		_layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.Controls.Add(_heading, 0, 0);
		_layout.Controls.Add(_introduction, 0, 1);
		_layout.Controls.Add(_inputs, 0, 2);
		_layout.Controls.Add(_commands, 0, 3);
		_layout.Controls.Add(_outputTextBox, 0, 4);
		_layout.Controls.Add(_status, 0, 5);

		_heading.Text = "Transactional Collections";
		_heading.AutoSize = true;
		_heading.Font = new Font("Segoe UI", 17F, FontStyle.Bold);
		_heading.Margin = new Padding(0, 0, 0, 12);
		_introduction.Text = "Compare real list, buffer, dictionary and clustered-stream writes. All work uses an owned temporary directory, bounded input, explicit commit choices and cancellable worker execution.";
		_introduction.AutoSize = true;
		_introduction.Dock = DockStyle.Fill;
		_introduction.Margin = new Padding(0, 0, 0, 16);

		_inputs.AutoSize = true;
		_inputs.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_inputs.Dock = DockStyle.Fill;
		_inputs.Margin = new Padding(0, 0, 0, 10);

		_commands.AutoSize = true;
		_commands.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_commands.Dock = DockStyle.Fill;
		_commands.Margin = new Padding(0, 0, 0, 10);

		_itemsIntBoxLabel.Name = "_itemsIntBoxLabel";
		_itemsIntBoxLabel.Text = "Items";
		_itemsIntBoxLabel.AutoSize = true;
		_itemsIntBoxLabel.Padding = new Padding(0, 5, 0, 0);
		_itemsIntBoxLabel.Margin = new Padding(0, 3, 12, 3);
		_itemsInput.AutoSize = true;
		_itemsInput.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_itemsInput.WrapContents = false;
		_itemsInput.Margin = new Padding(0, 0, 12, 0);
		_itemsInput.Controls.Add(_itemsIntBoxLabel);
		_inputs.Controls.Add(_itemsInput);

		_itemsIntBox.Name = "_itemsIntBox";
		_itemsIntBox.Value = 1000;
		_itemsIntBox.Width = 95;
		_itemsIntBox.Margin = new Padding(0, 3, 0, 3);
		_itemsInput.Controls.Add(_itemsIntBox);
		_itemsIntBox.ValueChanged += PreferencesEdited;

		_batchIntBoxLabel.Name = "_batchIntBoxLabel";
		_batchIntBoxLabel.Text = "Batch size";
		_batchIntBoxLabel.AutoSize = true;
		_batchIntBoxLabel.Padding = new Padding(0, 5, 0, 0);
		_batchIntBoxLabel.Margin = new Padding(0, 3, 12, 3);
		_batchInput.AutoSize = true;
		_batchInput.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_batchInput.WrapContents = false;
		_batchInput.Margin = new Padding(0, 0, 12, 0);
		_batchInput.Controls.Add(_batchIntBoxLabel);
		_inputs.Controls.Add(_batchInput);

		_batchIntBox.Name = "_batchIntBox";
		_batchIntBox.Value = 100;
		_batchIntBox.Width = 95;
		_batchIntBox.Margin = new Padding(0, 3, 0, 3);
		_batchInput.Controls.Add(_batchIntBox);
		_batchIntBox.ValueChanged += PreferencesEdited;

		_itemSizeIntBoxLabel.Name = "_itemSizeIntBoxLabel";
		_itemSizeIntBoxLabel.Text = "Bytes per item";
		_itemSizeIntBoxLabel.AutoSize = true;
		_itemSizeIntBoxLabel.Padding = new Padding(0, 5, 0, 0);
		_itemSizeIntBoxLabel.Margin = new Padding(0, 3, 12, 3);
		_itemSizeInput.AutoSize = true;
		_itemSizeInput.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_itemSizeInput.WrapContents = false;
		_itemSizeInput.Margin = new Padding(0, 0, 12, 0);
		_itemSizeInput.Controls.Add(_itemSizeIntBoxLabel);
		_inputs.Controls.Add(_itemSizeInput);

		_itemSizeIntBox.Name = "_itemSizeIntBox";
		_itemSizeIntBox.Value = 64;
		_itemSizeIntBox.Width = 95;
		_itemSizeIntBox.Margin = new Padding(0, 3, 0, 3);
		_itemSizeInput.Controls.Add(_itemSizeIntBox);
		_itemSizeIntBox.ValueChanged += PreferencesEdited;

		_clusterSizeIntBoxLabel.Name = "_clusterSizeIntBoxLabel";
		_clusterSizeIntBoxLabel.Text = "Cluster bytes";
		_clusterSizeIntBoxLabel.AutoSize = true;
		_clusterSizeIntBoxLabel.Padding = new Padding(0, 5, 0, 0);
		_clusterSizeIntBoxLabel.Margin = new Padding(0, 3, 12, 3);
		_clusterSizeInput.AutoSize = true;
		_clusterSizeInput.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_clusterSizeInput.WrapContents = false;
		_clusterSizeInput.Margin = new Padding(0, 0, 12, 0);
		_clusterSizeInput.Controls.Add(_clusterSizeIntBoxLabel);
		_inputs.Controls.Add(_clusterSizeInput);

		_clusterSizeIntBox.Name = "_clusterSizeIntBox";
		_clusterSizeIntBox.Value = 1024;
		_clusterSizeIntBox.Width = 95;
		_clusterSizeIntBox.Margin = new Padding(0, 3, 0, 3);
		_clusterSizeInput.Controls.Add(_clusterSizeIntBox);
		_clusterSizeIntBox.ValueChanged += PreferencesEdited;

		_pageSizeIntBoxLabel.Name = "_pageSizeIntBoxLabel";
		_pageSizeIntBoxLabel.Text = "Page bytes";
		_pageSizeIntBoxLabel.AutoSize = true;
		_pageSizeIntBoxLabel.Padding = new Padding(0, 5, 0, 0);
		_pageSizeIntBoxLabel.Margin = new Padding(0, 3, 12, 3);
		_pageSizeInput.AutoSize = true;
		_pageSizeInput.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_pageSizeInput.WrapContents = false;
		_pageSizeInput.Margin = new Padding(0, 0, 12, 0);
		_pageSizeInput.Controls.Add(_pageSizeIntBoxLabel);
		_inputs.Controls.Add(_pageSizeInput);

		_pageSizeIntBox.Name = "_pageSizeIntBox";
		_pageSizeIntBox.Value = 65536;
		_pageSizeIntBox.Width = 95;
		_pageSizeIntBox.Margin = new Padding(0, 3, 0, 3);
		_pageSizeInput.Controls.Add(_pageSizeIntBox);
		_pageSizeIntBox.ValueChanged += PreferencesEdited;

		_cacheSizeIntBoxLabel.Name = "_cacheSizeIntBoxLabel";
		_cacheSizeIntBoxLabel.Text = "Cache bytes";
		_cacheSizeIntBoxLabel.AutoSize = true;
		_cacheSizeIntBoxLabel.Padding = new Padding(0, 5, 0, 0);
		_cacheSizeIntBoxLabel.Margin = new Padding(0, 3, 12, 3);
		_cacheSizeInput.AutoSize = true;
		_cacheSizeInput.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_cacheSizeInput.WrapContents = false;
		_cacheSizeInput.Margin = new Padding(0, 0, 12, 0);
		_cacheSizeInput.Controls.Add(_cacheSizeIntBoxLabel);
		_inputs.Controls.Add(_cacheSizeInput);

		_cacheSizeIntBox.Name = "_cacheSizeIntBox";
		_cacheSizeIntBox.Value = 8388608;
		_cacheSizeIntBox.Width = 95;
		_cacheSizeIntBox.Margin = new Padding(0, 3, 0, 3);
		_cacheSizeInput.Controls.Add(_cacheSizeIntBox);
		_cacheSizeIntBox.ValueChanged += PreferencesEdited;

		_backendLabel.Name = "_backendLabel";
		_backendLabel.Text = "List / buffer backend";
		_backendLabel.AutoSize = true;
		_backendLabel.Padding = new Padding(0, 5, 0, 0);
		_backendLabel.Margin = new Padding(0, 3, 12, 3);
		_backendInput.AutoSize = true;
		_backendInput.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_backendInput.WrapContents = false;
		_backendInput.Margin = new Padding(0, 0, 12, 0);
		_backendInput.Controls.Add(_backendLabel);
		_inputs.Controls.Add(_backendInput);

		_listTypeComboBox.Name = "_listTypeComboBox";
		_listTypeComboBox.Width = 185;
		_listTypeComboBox.Margin = new Padding(0, 3, 0, 3);
		_backendInput.Controls.Add(_listTypeComboBox);
		_listTypeComboBox.SelectedIndexChanged += BackendChanged;

		_commitCheckBox.Name = "_commitCheckBox";
		_commitCheckBox.Text = "Commit each batch when supported";
		_commitCheckBox.AutoSize = true;
		_commitCheckBox.Margin = new Padding(0, 3, 12, 3);
		_inputs.Controls.Add(_commitCheckBox);
		_commitCheckBox.CheckedChanged += PreferencesEdited;

		_policyLabel.Name = "_policyLabel";
		_policyLabel.Text = "Cluster policy";
		_policyLabel.AutoSize = true;
		_policyLabel.Padding = new Padding(0, 5, 0, 0);
		_policyLabel.Margin = new Padding(0, 3, 12, 3);
		_policyInput.AutoSize = true;
		_policyInput.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_policyInput.WrapContents = false;
		_policyInput.Margin = new Padding(0, 0, 12, 0);
		_policyInput.Controls.Add(_policyLabel);
		_inputs.Controls.Add(_policyInput);

		_policyBox.Name = "_policyBox";
		_policyBox.Width = 230;
		_policyBox.Height = 100;
		_policyBox.CheckOnClick = true;
		_policyBox.Margin = new Padding(0, 3, 0, 3);
		_policyInput.Controls.Add(_policyBox);

		_listAppendTestButton.Name = "_listAppendTestButton";
		_listAppendTestButton.Text = "Append list";
		_listAppendTestButton.AutoSize = true;
		_listAppendTestButton.MinimumSize = new Size(110, 32);
		_listAppendTestButton.Margin = new Padding(0, 0, 8, 0);
		_listAppendTestButton.Click += ListClicked;
		_commands.Controls.Add(_listAppendTestButton);

		_bufferTestButton.Name = "_bufferTestButton";
		_bufferTestButton.Text = "Append buffer";
		_bufferTestButton.AutoSize = true;
		_bufferTestButton.MinimumSize = new Size(110, 32);
		_bufferTestButton.Margin = new Padding(0, 0, 8, 0);
		_bufferTestButton.Click += BufferClicked;
		_commands.Controls.Add(_bufferTestButton);

		_dictionaryAppend.Name = "_dictionaryAppend";
		_dictionaryAppend.Text = "Transactional dictionary";
		_dictionaryAppend.AutoSize = true;
		_dictionaryAppend.MinimumSize = new Size(110, 32);
		_dictionaryAppend.Margin = new Padding(0, 0, 8, 0);
		_dictionaryAppend.Click += DictionaryClicked;
		_commands.Controls.Add(_dictionaryAppend);

		_storageStreamButton.Name = "_storageStreamButton";
		_storageStreamButton.Text = "Clustered stream";
		_storageStreamButton.AutoSize = true;
		_storageStreamButton.MinimumSize = new Size(110, 32);
		_storageStreamButton.Margin = new Padding(0, 0, 8, 0);
		_storageStreamButton.Click += StreamClicked;
		_commands.Controls.Add(_storageStreamButton);

		_rollbackButton.Name = "_rollbackButton";
		_rollbackButton.Text = "Verify commit / rollback";
		_rollbackButton.AutoSize = true;
		_rollbackButton.MinimumSize = new Size(110, 32);
		_rollbackButton.Margin = new Padding(0, 0, 8, 0);
		_rollbackButton.Click += RollbackClicked;
		_commands.Controls.Add(_rollbackButton);

		_cancelButton.Name = "_cancelButton";
		_cancelButton.Text = "Cancel";
		_cancelButton.AutoSize = true;
		_cancelButton.MinimumSize = new Size(110, 32);
		_cancelButton.Margin = new Padding(0, 0, 8, 0);
		_cancelButton.Click += CancelClicked;
		_commands.Controls.Add(_cancelButton);
		_cancelButton.Enabled = false;

		_copyButton.Name = "_copyButton";
		_copyButton.Text = "Copy results";
		_copyButton.AutoSize = true;
		_copyButton.MinimumSize = new Size(110, 32);
		_copyButton.Margin = new Padding(0, 0, 8, 0);
		_copyButton.Click += CopyClicked;
		_commands.Controls.Add(_copyButton);

		_clearButton.Name = "_clearButton";
		_clearButton.Text = "Clear results";
		_clearButton.AutoSize = true;
		_clearButton.MinimumSize = new Size(110, 32);
		_clearButton.Margin = new Padding(0, 0, 8, 0);
		_clearButton.Click += ClearClicked;
		_commands.Controls.Add(_clearButton);

		_outputTextBox.Name = "_outputTextBox";
		_outputTextBox.Dock = DockStyle.Fill;
		_outputTextBox.Multiline = true;
		_outputTextBox.ReadOnly = true;
		_outputTextBox.ScrollBars = ScrollBars.Both;
		_outputTextBox.WordWrap = false;
		_outputTextBox.BackColor = SystemColors.Window;
		_outputTextBox.Font = new Font("Consolas", 9F);
		_outputTextBox.Margin = new Padding(0, 0, 0, 10);
		_status.Name = "showcaseStatus";
		_status.AutoSize = true;
		_status.Dock = DockStyle.Fill;
		_status.Text = "Ready. Choose an example to see its results.";

		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		Font = SystemFonts.MessageBoxFont;
		BackColor = SystemColors.Window;
		Name = "TransactionalCollectionScreen";
		Size = new Size(1000, 720);
		Controls.Add(_layout);
		_commands.ResumeLayout(false);
		_commands.PerformLayout();
		_itemsInput.ResumeLayout(false);
		_itemsInput.PerformLayout();
		_batchInput.ResumeLayout(false);
		_batchInput.PerformLayout();
		_itemSizeInput.ResumeLayout(false);
		_itemSizeInput.PerformLayout();
		_clusterSizeInput.ResumeLayout(false);
		_clusterSizeInput.PerformLayout();
		_pageSizeInput.ResumeLayout(false);
		_pageSizeInput.PerformLayout();
		_cacheSizeInput.ResumeLayout(false);
		_cacheSizeInput.PerformLayout();
		_backendInput.ResumeLayout(false);
		_backendInput.PerformLayout();
		_policyInput.ResumeLayout(false);
		_policyInput.PerformLayout();
		_inputs.ResumeLayout(false);
		_inputs.PerformLayout();
		_layout.ResumeLayout(false);
		_layout.PerformLayout();
		ResumeLayout(false);
	}
}
