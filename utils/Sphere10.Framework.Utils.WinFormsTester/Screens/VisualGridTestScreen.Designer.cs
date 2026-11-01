// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework.Windows.Forms;
using System.Drawing;
using System.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester {
	partial class CrudTestScreen {
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			var disposeLive = disposing && !_liveDisposed;
			if (disposeLive) {
				_liveDisposed = true;
				_liveCancellation.Cancel();
				components?.Dispose();
			}
			base.Dispose(disposing);
			if (disposeLive) {
				_liveCancellation.Dispose();
				if (_liveImages != null)
					foreach (var image in _liveImages)
						image.Dispose();
			}
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			components = new System.ComponentModel.Container();
			_outputTextBox = new System.Windows.Forms.TextBox();
			_generateDeleteErrorCheckBox = new System.Windows.Forms.CheckBox();
			_crudDialogButton = new System.Windows.Forms.Button();
			_generateCreateErrorCheckBox = new System.Windows.Forms.CheckBox();
			_generateUpdateErrorCheckBox = new System.Windows.Forms.CheckBox();
			_selectFirstEntityButton = new System.Windows.Forms.Button();
			_rightClickCheckBox = new System.Windows.Forms.CheckBox();
			_leftClickCheckBox = new System.Windows.Forms.CheckBox();
			_refreshGrid = new System.Windows.Forms.Button();
			_autoSizeCheckBox = new System.Windows.Forms.CheckBox();
			_autoSelectOnCreateCheckBox = new System.Windows.Forms.CheckBox();
			_crudComboBox = new Windows.Forms.Crud.CrudComboBox();
			_flagsCheckedListBox = new FlagsCheckedListBox();
			_crudGrid = new CrudGrid();
			_allowCellEditingCheckBox = new System.Windows.Forms.CheckBox();
			_refreshEntireGridOnUpdateCheckBox = new System.Windows.Forms.CheckBox();
			_refreshEntireGridOnDeleteCheckBox = new System.Windows.Forms.CheckBox();
			_suppressLoadingIndicatorCheckBox = new System.Windows.Forms.CheckBox();
			_optionsPanel = new Panel();
			_contentTabs = new TabControl();
			_editingTab = new TabPage();
			_liveTab = new TabPage();
			_liveGrid = new CrudGrid();
			_liveActions = new FlowLayoutPanel();
			_liveOptions = new FlowLayoutPanel();
			_liveUpdatesCheckBox = new CheckBox();
			_liveRefreshButton = new Button();
			_refreshSelectedLiveButton = new Button();
			_removeSelectedLiveButton = new Button();
			_selectFirstLiveButton = new Button();
			_livePageModeLabel = new Label();
			_livePageMode = new ComboBox();
			_liveTitleLabel = new Label();
			_liveTitleAlignment = new ComboBox();
			_liveImageAlignmentLabel = new Label();
			_liveImageAlignment = new ComboBox();
			_liveImageResizeLabel = new Label();
			_liveImageResize = new ComboBox();
			_prepareLiveCellsCheckBox = new CheckBox();
			_measureLiveRowsCheckBox = new CheckBox();
			_liveLoadingFeedbackCheckBox = new CheckBox();
			_liveMouseUpCheckBox = new CheckBox();
			_liveToggleSelectionCheckBox = new CheckBox();
			_liveStatus = new Label();
			_liveTimer = new System.Windows.Forms.Timer(components);
			_optionsPanel.SuspendLayout();
			_contentTabs.SuspendLayout();
			_editingTab.SuspendLayout();
			_liveTab.SuspendLayout();
			_liveActions.SuspendLayout();
			_liveOptions.SuspendLayout();
			SuspendLayout();
			// 
			// _outputTextBox
			// 
			_outputTextBox.Anchor = System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
			_outputTextBox.Location = new System.Drawing.Point(14, 680);
			_outputTextBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			_outputTextBox.Multiline = true;
			_outputTextBox.Dock = DockStyle.Bottom;
			_outputTextBox.ReadOnly = true;
			_outputTextBox.ScrollBars = ScrollBars.Vertical;
			_outputTextBox.Name = "_outputTextBox";
			_outputTextBox.Size = new System.Drawing.Size(1077, 94);
			_outputTextBox.TabIndex = 2;
			// 
			// _generateDeleteErrorCheckBox
			// 
			_generateDeleteErrorCheckBox.AutoSize = true;
			_generateDeleteErrorCheckBox.Location = new System.Drawing.Point(19, 248);
			_generateDeleteErrorCheckBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			_generateDeleteErrorCheckBox.Name = "_generateDeleteErrorCheckBox";
			_generateDeleteErrorCheckBox.Size = new System.Drawing.Size(136, 19);
			_generateDeleteErrorCheckBox.TabIndex = 3;
			_generateDeleteErrorCheckBox.Text = "Generate delete error";
			_generateDeleteErrorCheckBox.UseVisualStyleBackColor = true;
			_generateDeleteErrorCheckBox.CheckedChanged += _generateDeleteErrorCheckBox_CheckedChanged;
			// 
			// _crudDialogButton
			// 
			_crudDialogButton.Location = new System.Drawing.Point(20, 614);
			_crudDialogButton.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			_crudDialogButton.Name = "_crudDialogButton";
			_crudDialogButton.Size = new System.Drawing.Size(147, 27);
			_crudDialogButton.TabIndex = 4;
			_crudDialogButton.Text = "Test CRUD Dialog";
			_crudDialogButton.UseVisualStyleBackColor = true;
			_crudDialogButton.Click += _crudDialogButton_Click;
			// 
			// _generateCreateErrorCheckBox
			// 
			_generateCreateErrorCheckBox.AutoSize = true;
			_generateCreateErrorCheckBox.Location = new System.Drawing.Point(19, 275);
			_generateCreateErrorCheckBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			_generateCreateErrorCheckBox.Name = "_generateCreateErrorCheckBox";
			_generateCreateErrorCheckBox.Size = new System.Drawing.Size(136, 19);
			_generateCreateErrorCheckBox.TabIndex = 6;
			_generateCreateErrorCheckBox.Text = "Generate create error";
			_generateCreateErrorCheckBox.UseVisualStyleBackColor = true;
			_generateCreateErrorCheckBox.CheckedChanged += _generateCreateErrorCheckBox_CheckedChanged;
			// 
			// _generateUpdateErrorCheckBox
			// 
			_generateUpdateErrorCheckBox.AutoSize = true;
			_generateUpdateErrorCheckBox.Location = new System.Drawing.Point(19, 301);
			_generateUpdateErrorCheckBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			_generateUpdateErrorCheckBox.Name = "_generateUpdateErrorCheckBox";
			_generateUpdateErrorCheckBox.Size = new System.Drawing.Size(141, 19);
			_generateUpdateErrorCheckBox.TabIndex = 7;
			_generateUpdateErrorCheckBox.Text = "Generate update error";
			_generateUpdateErrorCheckBox.UseVisualStyleBackColor = true;
			_generateUpdateErrorCheckBox.CheckedChanged += _createUpdateErrorCheckBox_CheckedChanged;
			// 
			// _selectFirstEntityButton
			// 
			_selectFirstEntityButton.Location = new System.Drawing.Point(20, 581);
			_selectFirstEntityButton.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			_selectFirstEntityButton.Name = "_selectFirstEntityButton";
			_selectFirstEntityButton.Size = new System.Drawing.Size(147, 27);
			_selectFirstEntityButton.TabIndex = 8;
			_selectFirstEntityButton.Text = "Select First Entity";
			_selectFirstEntityButton.UseVisualStyleBackColor = true;
			_selectFirstEntityButton.Click += _selectFirstEntityButton_Click;
			// 
			// _rightClickCheckBox
			// 
			_rightClickCheckBox.AutoSize = true;
			_rightClickCheckBox.Location = new System.Drawing.Point(19, 354);
			_rightClickCheckBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			_rightClickCheckBox.Name = "_rightClickCheckBox";
			_rightClickCheckBox.Size = new System.Drawing.Size(133, 19);
			_rightClickCheckBox.TabIndex = 10;
			_rightClickCheckBox.Text = "Right click for menu";
			_rightClickCheckBox.UseVisualStyleBackColor = true;
			_rightClickCheckBox.CheckedChanged += _rightClickCheckBox_CheckedChanged;
			// 
			// _leftClickCheckBox
			// 
			_leftClickCheckBox.AutoSize = true;
			_leftClickCheckBox.Checked = true;
			_leftClickCheckBox.Location = new System.Drawing.Point(19, 328);
			_leftClickCheckBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			_leftClickCheckBox.Name = "_leftClickCheckBox";
			_leftClickCheckBox.Size = new System.Drawing.Size(133, 19);
			_leftClickCheckBox.TabIndex = 9;
			_leftClickCheckBox.Text = "Left click to deselect";
			_leftClickCheckBox.UseVisualStyleBackColor = true;
			_leftClickCheckBox.CheckedChanged += _leftClickCheckBox_CheckedChanged;
			// 
			// _refreshGrid
			// 
			_refreshGrid.Location = new System.Drawing.Point(20, 547);
			_refreshGrid.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			_refreshGrid.Name = "_refreshGrid";
			_refreshGrid.Size = new System.Drawing.Size(147, 27);
			_refreshGrid.TabIndex = 11;
			_refreshGrid.Text = "Refresh Grid";
			_refreshGrid.UseVisualStyleBackColor = true;
			_refreshGrid.Click += _refreshGrid_Click;
			// 
			// _autoSizeCheckBox
			// 
			_autoSizeCheckBox.AutoSize = true;
			_autoSizeCheckBox.Location = new System.Drawing.Point(19, 378);
			_autoSizeCheckBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			_autoSizeCheckBox.Name = "_autoSizeCheckBox";
			_autoSizeCheckBox.Size = new System.Drawing.Size(103, 19);
			_autoSizeCheckBox.TabIndex = 12;
			_autoSizeCheckBox.Text = "Auto page size";
			_autoSizeCheckBox.UseVisualStyleBackColor = true;
			_autoSizeCheckBox.CheckedChanged += _autoSizeCheckBox_CheckedChanged;
			// 
			// _autoSelectOnCreateCheckBox
			// 
			_autoSelectOnCreateCheckBox.AutoSize = true;
			_autoSelectOnCreateCheckBox.Location = new System.Drawing.Point(19, 405);
			_autoSelectOnCreateCheckBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			_autoSelectOnCreateCheckBox.Name = "_autoSelectOnCreateCheckBox";
			_autoSelectOnCreateCheckBox.Size = new System.Drawing.Size(137, 19);
			_autoSelectOnCreateCheckBox.TabIndex = 13;
			_autoSelectOnCreateCheckBox.Text = "Auto select on create";
			_autoSelectOnCreateCheckBox.UseVisualStyleBackColor = true;
			_autoSelectOnCreateCheckBox.CheckedChanged += _autoSelectOnCreateCheckBox_CheckedChanged_1;
			// 
			// _crudComboBox
			// 
			_crudComboBox.AllowResizeDropDown = true;
			_crudComboBox.ControlSize = new System.Drawing.Size(524, 306);
			_crudComboBox.DropDownSizeMode = CustomComboBox.SizeMode.UseInitialControlSize;
			_crudComboBox.DropSize = new System.Drawing.Size(121, 106);
			_crudComboBox.Location = new System.Drawing.Point(20, 648);
			_crudComboBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			_crudComboBox.Name = "_crudComboBox";
			_crudComboBox.PlaceHolderText = "Select Entity";
			_crudComboBox.Size = new System.Drawing.Size(146, 23);
			_crudComboBox.TabIndex = 5;
			_crudComboBox.EntitySelectionChanged += _crudComboBox_EntitySelectionChanged;
			// 
			// _flagsCheckedListBox
			// 
			_flagsCheckedListBox.BackColor = System.Drawing.SystemColors.Control;
			_flagsCheckedListBox.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			_flagsCheckedListBox.CheckOnClick = true;
			_flagsCheckedListBox.FormattingEnabled = true;
			_flagsCheckedListBox.Location = new System.Drawing.Point(14, 14);
			_flagsCheckedListBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			_flagsCheckedListBox.Name = "_flagsCheckedListBox";
			_flagsCheckedListBox.Size = new System.Drawing.Size(194, 218);
			_flagsCheckedListBox.TabIndex = 1;
			_flagsCheckedListBox.SelectedValueChanged += _flagsCheckedListBox_SelectedValueChanged;
			// 
			// _crudGrid
			// 
			_crudGrid.Anchor = System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left | System.Windows.Forms.AnchorStyles.Right;
			_crudGrid.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			_crudGrid.Dock = DockStyle.Fill;
			_crudGrid.GridTitle = "Employees";
			_crudGrid.GridTitleAlignment = ContentAlignment.MiddleLeft;
			_crudGrid.Capabilities = DataSourceCapabilities.CanCreate | DataSourceCapabilities.CanRead;
			_crudGrid.Location = new System.Drawing.Point(217, 15);
			_crudGrid.Margin = new System.Windows.Forms.Padding(5, 3, 5, 3);
			_crudGrid.MinimumSize = new System.Drawing.Size(372, 100);
			_crudGrid.Name = "_crudGrid";
			_crudGrid.RightClickForContextMenu = false;
			_crudGrid.Size = new System.Drawing.Size(876, 659);
			_crudGrid.TabIndex = 0;
			_crudGrid.EntitySelected += _crudGrid_EntitySelected;
			_crudGrid.EntityDeselected += _crudGrid_EntityDeselected;
			_crudGrid.EntityCreated += _crudGrid_EntityCreated;
			_crudGrid.EntityUpdated += _crudGrid_EntityUpdated;
			_crudGrid.EntityDeleted += _crudGrid_EntityDeleted;
			_crudGrid.EntityPropertyChanged += _crudGrid_EntityEditing;
			// 
			// _allowCellEditingCheckBox
			// 
			_allowCellEditingCheckBox.AutoSize = true;
			_allowCellEditingCheckBox.Location = new System.Drawing.Point(19, 432);
			_allowCellEditingCheckBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			_allowCellEditingCheckBox.Name = "_allowCellEditingCheckBox";
			_allowCellEditingCheckBox.Size = new System.Drawing.Size(117, 19);
			_allowCellEditingCheckBox.TabIndex = 14;
			_allowCellEditingCheckBox.Text = "Allow cell editing";
			_allowCellEditingCheckBox.UseVisualStyleBackColor = true;
			_allowCellEditingCheckBox.CheckedChanged += _allowCellEditingCheckBox_CheckedChanged;
			// 
			// _refreshEntireGridOnUpdateCheckBox
			// 
			_refreshEntireGridOnUpdateCheckBox.AutoSize = true;
			_refreshEntireGridOnUpdateCheckBox.Location = new System.Drawing.Point(19, 458);
			_refreshEntireGridOnUpdateCheckBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			_refreshEntireGridOnUpdateCheckBox.Name = "_refreshEntireGridOnUpdateCheckBox";
			_refreshEntireGridOnUpdateCheckBox.Size = new System.Drawing.Size(179, 19);
			_refreshEntireGridOnUpdateCheckBox.TabIndex = 15;
			_refreshEntireGridOnUpdateCheckBox.Text = "Refresh entire grid on update";
			_refreshEntireGridOnUpdateCheckBox.UseVisualStyleBackColor = true;
			_refreshEntireGridOnUpdateCheckBox.CheckedChanged += _refreshEntireGridOnUpdateCheckBox_CheckedChanged;
			// 
			// _refreshEntireGridOnDeleteCheckBox
			// 
			_refreshEntireGridOnDeleteCheckBox.AutoSize = true;
			_refreshEntireGridOnDeleteCheckBox.Location = new System.Drawing.Point(19, 485);
			_refreshEntireGridOnDeleteCheckBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			_refreshEntireGridOnDeleteCheckBox.Name = "_refreshEntireGridOnDeleteCheckBox";
			_refreshEntireGridOnDeleteCheckBox.Size = new System.Drawing.Size(174, 19);
			_refreshEntireGridOnDeleteCheckBox.TabIndex = 16;
			_refreshEntireGridOnDeleteCheckBox.Text = "Refresh entire grid on delete";
			_refreshEntireGridOnDeleteCheckBox.UseVisualStyleBackColor = true;
			_refreshEntireGridOnDeleteCheckBox.CheckedChanged += _refreshEntireGridOnDeleteCheckBox_CheckedChanged;
			//
			// _suppressLoadingIndicatorCheckBox
			//
			_suppressLoadingIndicatorCheckBox.AutoSize = true;
			_suppressLoadingIndicatorCheckBox.Location = new System.Drawing.Point(19, 511);
			_suppressLoadingIndicatorCheckBox.Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			_suppressLoadingIndicatorCheckBox.Name = "_suppressLoadingIndicatorCheckBox";
			_suppressLoadingIndicatorCheckBox.Size = new System.Drawing.Size(180, 19);
			_suppressLoadingIndicatorCheckBox.TabIndex = 17;
			_suppressLoadingIndicatorCheckBox.Text = "Suppress loading indicator";
			_suppressLoadingIndicatorCheckBox.UseVisualStyleBackColor = true;
			_suppressLoadingIndicatorCheckBox.CheckedChanged += SuppressLoadingIndicatorCheckedChanged;
			//
			// Editable Records And Live Snapshot Workspaces
			//
			_optionsPanel.Name = "_optionsPanel";
			_optionsPanel.Dock = DockStyle.Left;
			_optionsPanel.Width = 215;
			_optionsPanel.AutoScroll = true;
			_optionsPanel.Enabled = false;
			_contentTabs.Name = "_contentTabs";
			_contentTabs.Dock = DockStyle.Fill;
			_contentTabs.Controls.Add(_editingTab);
			_contentTabs.Controls.Add(_liveTab);
			_editingTab.Name = "_editingTab";
			_editingTab.Text = "Editable Records";
			_editingTab.BackColor = SystemColors.Control;
			_editingTab.Padding = new Padding(3);
			_editingTab.Controls.Add(_crudGrid);
			_liveTab.Name = "_liveTab";
			_liveTab.Text = "Live Snapshots";
			_liveTab.BackColor = SystemColors.Control;
			_liveTab.Padding = new Padding(3);
			_liveGrid.Name = "_liveGrid";
			_liveGrid.Dock = DockStyle.Fill;
			_liveGrid.GridTitle = "Background Snapshot Samples";
			_liveGrid.GridTitleAlignment = ContentAlignment.MiddleLeft;
			_liveGrid.PersistSettings = false;
			_liveGrid.PageMode = CrudGridPageMode.Auto;
			_liveGrid.PrepareReadOnlyCellsInBackground = true;
			_liveGrid.SuppressLoadingIndicator = true;
			_liveGrid.AutoSizeRowsOnRefresh = false;
			_liveGrid.UseEntityReferenceForLookup = true;
			_liveGrid.EntitySelected += LiveSelectionChanged;
			_liveGrid.EntityDeselected += LiveSelectionChanged;
			_liveActions.Name = "_liveActions";
			_liveActions.Dock = DockStyle.Top;
			_liveActions.AutoSize = true;
			_liveActions.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			_liveActions.Padding = new Padding(3);
			_liveActions.WrapContents = true;
			_liveUpdatesCheckBox.Name = "_liveUpdatesCheckBox";
			_liveUpdatesCheckBox.Text = "Live Updates (750 ms)";
			_liveUpdatesCheckBox.AutoSize = true;
			_liveUpdatesCheckBox.Anchor = AnchorStyles.Left;
			_liveUpdatesCheckBox.CheckedChanged += LiveUpdatesChanged;
			_liveRefreshButton.Name = "_liveRefreshButton";
			_liveRefreshButton.Text = "Capture Snapshot";
			_liveRefreshButton.AutoSize = true;
			_liveRefreshButton.Click += LiveRefreshClicked;
			_refreshSelectedLiveButton.Name = "_refreshSelectedLiveButton";
			_refreshSelectedLiveButton.Text = "Refresh Selected Row";
			_refreshSelectedLiveButton.AutoSize = true;
			_refreshSelectedLiveButton.Enabled = false;
			_refreshSelectedLiveButton.Click += RefreshSelectedLiveClicked;
			_removeSelectedLiveButton.Name = "_removeSelectedLiveButton";
			_removeSelectedLiveButton.Text = "Simulate Disappearance";
			_removeSelectedLiveButton.AutoSize = true;
			_removeSelectedLiveButton.Enabled = false;
			_removeSelectedLiveButton.Click += RemoveSelectedLiveClicked;
			_selectFirstLiveButton.Name = "_selectFirstLiveButton";
			_selectFirstLiveButton.Text = "Select First Visible Row";
			_selectFirstLiveButton.AutoSize = true;
			_selectFirstLiveButton.Click += SelectFirstLiveClicked;
			_liveActions.Controls.Add(_liveUpdatesCheckBox);
			_liveActions.Controls.Add(_liveRefreshButton);
			_liveActions.Controls.Add(_refreshSelectedLiveButton);
			_liveActions.Controls.Add(_removeSelectedLiveButton);
			_liveActions.Controls.Add(_selectFirstLiveButton);
			_liveOptions.Name = "_liveOptions";
			_liveOptions.Dock = DockStyle.Top;
			_liveOptions.AutoSize = true;
			_liveOptions.AutoSizeMode = AutoSizeMode.GrowAndShrink;
			_liveOptions.Padding = new Padding(3);
			_liveOptions.WrapContents = true;
			_livePageModeLabel.Text = "Page Mode";
			_livePageModeLabel.AutoSize = true;
			_livePageModeLabel.Anchor = AnchorStyles.Left;
			_livePageMode.Name = "_livePageMode";
			_livePageMode.DropDownStyle = ComboBoxStyle.DropDownList;
			_livePageMode.Width = 115;
			_livePageMode.Items.AddRange(new object[] { "Paged", "Auto Paged", "Single Paged" });
			_livePageMode.SelectedIndexChanged += LivePageModeChanged;
			_livePageMode.DropDown += LivePageModeDropDown;
			_liveTitleLabel.Text = "Title";
			_liveTitleLabel.AutoSize = true;
			_liveTitleLabel.Anchor = AnchorStyles.Left;
			_liveTitleAlignment.Name = "_liveTitleAlignment";
			_liveTitleAlignment.DropDownStyle = ComboBoxStyle.DropDownList;
			_liveTitleAlignment.Width = 90;
			_liveTitleAlignment.Items.AddRange(new object[] { "Left", "Center", "Right" });
			_liveTitleAlignment.SelectedIndexChanged += LivePresentationChanged;
			_liveImageAlignmentLabel.Text = "Image Alignment";
			_liveImageAlignmentLabel.AutoSize = true;
			_liveImageAlignmentLabel.Anchor = AnchorStyles.Left;
			_liveImageAlignment.Name = "_liveImageAlignment";
			_liveImageAlignment.DropDownStyle = ComboBoxStyle.DropDownList;
			_liveImageAlignment.Width = 120;
			_liveImageAlignment.Items.AddRange(new object[] { "Top Left", "Top Center", "Top Right", "Center Left", "Center Center", "Center Right", "Bottom Left", "Bottom Center", "Bottom Right" });
			_liveImageAlignment.SelectedIndexChanged += LivePresentationChanged;
			_liveImageResizeLabel.Text = "Image Layout";
			_liveImageResizeLabel.AutoSize = true;
			_liveImageResizeLabel.Anchor = AnchorStyles.Left;
			_liveImageResize.Name = "_liveImageResize";
			_liveImageResize.DropDownStyle = ComboBoxStyle.DropDownList;
			_liveImageResize.Width = 130;
			_liveImageResize.Items.AddRange(new object[] { "Original Size", "Stretch", "Aspect Fill", "Aspect Fit Padded", "Aspect Fit" });
			_liveImageResize.SelectedIndexChanged += LivePresentationChanged;
			_prepareLiveCellsCheckBox.Name = "_prepareLiveCellsCheckBox";
			_prepareLiveCellsCheckBox.Text = "Prepare Cells On Worker";
			_prepareLiveCellsCheckBox.AutoSize = true;
			_prepareLiveCellsCheckBox.Checked = true;
			_prepareLiveCellsCheckBox.CheckedChanged += LivePresentationChanged;
			_measureLiveRowsCheckBox.Name = "_measureLiveRowsCheckBox";
			_measureLiveRowsCheckBox.Text = "Measure Rows On Each Refresh";
			_measureLiveRowsCheckBox.AutoSize = true;
			_measureLiveRowsCheckBox.CheckedChanged += LivePresentationChanged;
			_liveLoadingFeedbackCheckBox.Name = "_liveLoadingFeedbackCheckBox";
			_liveLoadingFeedbackCheckBox.Text = "Show Loading Feedback";
			_liveLoadingFeedbackCheckBox.AutoSize = true;
			_liveLoadingFeedbackCheckBox.CheckedChanged += LivePresentationChanged;
			_liveMouseUpCheckBox.Name = "_liveMouseUpCheckBox";
			_liveMouseUpCheckBox.Text = "Select On Mouse Up";
			_liveMouseUpCheckBox.AutoSize = true;
			_liveMouseUpCheckBox.CheckedChanged += LivePresentationChanged;
			_liveToggleSelectionCheckBox.Name = "_liveToggleSelectionCheckBox";
			_liveToggleSelectionCheckBox.Text = "Left Click To Deselect";
			_liveToggleSelectionCheckBox.AutoSize = true;
			_liveToggleSelectionCheckBox.Checked = true;
			_liveToggleSelectionCheckBox.CheckedChanged += LivePresentationChanged;
			_liveOptions.Controls.Add(_livePageModeLabel);
			_liveOptions.Controls.Add(_livePageMode);
			_liveOptions.Controls.Add(_liveTitleLabel);
			_liveOptions.Controls.Add(_liveTitleAlignment);
			_liveOptions.Controls.Add(_liveImageAlignmentLabel);
			_liveOptions.Controls.Add(_liveImageAlignment);
			_liveOptions.Controls.Add(_liveImageResizeLabel);
			_liveOptions.Controls.Add(_liveImageResize);
			_liveOptions.Controls.Add(_prepareLiveCellsCheckBox);
			_liveOptions.Controls.Add(_measureLiveRowsCheckBox);
			_liveOptions.Controls.Add(_liveLoadingFeedbackCheckBox);
			_liveOptions.Controls.Add(_liveMouseUpCheckBox);
			_liveOptions.Controls.Add(_liveToggleSelectionCheckBox);
			_liveStatus.Name = "_liveStatus";
			_liveStatus.Dock = DockStyle.Bottom;
			_liveStatus.AutoSize = true;
			_liveStatus.Height = 62;
			_liveStatus.Padding = new Padding(3);
			_liveStatus.Text = "Synthetic samples are collected and queried on background workers. No operating-system telemetry is collected by this demonstration.";
			_liveTab.Controls.Add(_liveGrid);
			_liveTab.Controls.Add(_liveStatus);
			_liveTab.Controls.Add(_liveOptions);
			_liveTab.Controls.Add(_liveActions);
			_liveGrid.BringToFront();
			_liveTimer.Interval = 750;
			_liveTimer.Tick += LiveTimerTick;
			// 
			// CrudTestScreen
			// 
			AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
			AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			_optionsPanel.Controls.Add(_refreshEntireGridOnDeleteCheckBox);
			_optionsPanel.Controls.Add(_suppressLoadingIndicatorCheckBox);
			_optionsPanel.Controls.Add(_refreshEntireGridOnUpdateCheckBox);
			_optionsPanel.Controls.Add(_allowCellEditingCheckBox);
			_optionsPanel.Controls.Add(_autoSelectOnCreateCheckBox);
			_optionsPanel.Controls.Add(_autoSizeCheckBox);
			_optionsPanel.Controls.Add(_refreshGrid);
			_optionsPanel.Controls.Add(_rightClickCheckBox);
			_optionsPanel.Controls.Add(_leftClickCheckBox);
			_optionsPanel.Controls.Add(_selectFirstEntityButton);
			_optionsPanel.Controls.Add(_generateUpdateErrorCheckBox);
			_optionsPanel.Controls.Add(_generateCreateErrorCheckBox);
			_optionsPanel.Controls.Add(_crudComboBox);
			_optionsPanel.Controls.Add(_crudDialogButton);
			_optionsPanel.Controls.Add(_generateDeleteErrorCheckBox);
			_optionsPanel.Controls.Add(_flagsCheckedListBox);
			Controls.Add(_outputTextBox);
			Controls.Add(_optionsPanel);
			Controls.Add(_contentTabs);
			_contentTabs.BringToFront();
			_optionsPanel.ResumeLayout(false);
			_optionsPanel.PerformLayout();
			_contentTabs.ResumeLayout(false);
			_editingTab.ResumeLayout(false);
			_liveTab.ResumeLayout(false);
			_liveTab.PerformLayout();
			_liveActions.ResumeLayout(false);
			_liveActions.PerformLayout();
			_liveOptions.ResumeLayout(false);
			_liveOptions.PerformLayout();
			Margin = new System.Windows.Forms.Padding(4, 3, 4, 3);
			Name = "CrudTestScreen";
			Size = new System.Drawing.Size(1106, 789);
			ResumeLayout(false);
			PerformLayout();
		}

		#endregion

		private Sphere10.Framework.Windows.Forms.CrudGrid _crudGrid;
		private Sphere10.Framework.Windows.Forms.FlagsCheckedListBox _flagsCheckedListBox;
		private System.Windows.Forms.TextBox _outputTextBox;
		private System.Windows.Forms.CheckBox _generateDeleteErrorCheckBox;
		private System.Windows.Forms.Button _crudDialogButton;
		private Sphere10.Framework.Windows.Forms.Crud.CrudComboBox _crudComboBox;
		private System.Windows.Forms.CheckBox _generateCreateErrorCheckBox;
		private System.Windows.Forms.CheckBox _generateUpdateErrorCheckBox;
		private System.Windows.Forms.Button _selectFirstEntityButton;
		private System.Windows.Forms.CheckBox _rightClickCheckBox;
		private System.Windows.Forms.CheckBox _leftClickCheckBox;
		private System.Windows.Forms.Button _refreshGrid;
		private System.Windows.Forms.CheckBox _autoSizeCheckBox;
		private System.Windows.Forms.CheckBox _autoSelectOnCreateCheckBox;
		private System.Windows.Forms.CheckBox _allowCellEditingCheckBox;
		private System.Windows.Forms.CheckBox _refreshEntireGridOnUpdateCheckBox;
		private System.Windows.Forms.CheckBox _refreshEntireGridOnDeleteCheckBox;
		private System.Windows.Forms.CheckBox _suppressLoadingIndicatorCheckBox;
		private Panel _optionsPanel;
		private TabControl _contentTabs;
		private TabPage _editingTab;
		private TabPage _liveTab;
		private CrudGrid _liveGrid;
		private FlowLayoutPanel _liveActions;
		private FlowLayoutPanel _liveOptions;
		private CheckBox _liveUpdatesCheckBox;
		private Button _liveRefreshButton;
		private Button _refreshSelectedLiveButton;
		private Button _removeSelectedLiveButton;
		private Button _selectFirstLiveButton;
		private Label _livePageModeLabel;
		private ComboBox _livePageMode;
		private Label _liveTitleLabel;
		private ComboBox _liveTitleAlignment;
		private Label _liveImageAlignmentLabel;
		private ComboBox _liveImageAlignment;
		private Label _liveImageResizeLabel;
		private ComboBox _liveImageResize;
		private CheckBox _prepareLiveCellsCheckBox;
		private CheckBox _measureLiveRowsCheckBox;
		private CheckBox _liveLoadingFeedbackCheckBox;
		private CheckBox _liveMouseUpCheckBox;
		private CheckBox _liveToggleSelectionCheckBox;
		private Label _liveStatus;
		private System.Windows.Forms.Timer _liveTimer;

	}
}


