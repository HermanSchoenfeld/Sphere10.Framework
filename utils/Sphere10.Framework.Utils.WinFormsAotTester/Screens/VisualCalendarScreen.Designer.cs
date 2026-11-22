// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Utils.WinFormsTester;

partial class VisualCalendarScreen {
	/// <summary>
	/// Required designer variable.
	/// </summary>
	private System.ComponentModel.IContainer components = null;

	/// <summary>
	/// Clean up any resources being used.
	/// </summary>
	/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
	protected override void Dispose(bool disposing) {
		if (disposing && (components != null)) {
			components.Dispose();
		}
		base.Dispose(disposing);
	}

	#region Component Designer generated code

	/// <summary>
	/// Required method for Designer support - do not modify
	/// the contents of this method with the code editor.
	/// </summary>
	private void InitializeComponent() {
		_layout = new System.Windows.Forms.TableLayoutPanel();
		_heading = new System.Windows.Forms.Label();
		_introduction = new System.Windows.Forms.Label();
		_actions = new System.Windows.Forms.FlowLayoutPanel();
		_populateButton = new System.Windows.Forms.Button();
		_timeViewSelector = new Sphere10.Framework.Windows.Forms.EnumComboBox();
		_columnFilterComboBox = new Sphere10.Framework.Windows.Forms.EnumComboBox();
		_viewSelector = new Sphere10.Framework.Windows.Forms.EnumComboBox();
		_dataSelector = new Sphere10.Framework.Windows.Forms.EnumComboBox();
		_styleSelector = new Sphere10.Framework.Windows.Forms.EnumComboBox();
		_scheduleLayout = new System.Windows.Forms.TableLayoutPanel();
		_visualCalendar = new Sphere10.Framework.Windows.Forms.VisualCalendar.VisualCalendar();
		_unallocatedCalendar = new Sphere10.Framework.Windows.Forms.VisualCalendar.UnallocatedCalendar();
		_outputTextBox = new System.Windows.Forms.TextBox();
		_layout.SuspendLayout();
		_scheduleLayout.SuspendLayout();
		SuspendLayout();
		//
		// _layout
		//
		_layout.ColumnCount = 1;
		_layout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
		_layout.Controls.Add(_heading, 0, 0);
		_layout.Controls.Add(_introduction, 0, 1);
		_layout.Controls.Add(_actions, 0, 2);
		_layout.Controls.Add(_scheduleLayout, 0, 3);
		_layout.Controls.Add(_outputTextBox, 0, 4);
		_layout.Dock = System.Windows.Forms.DockStyle.Fill;
		_layout.Location = new System.Drawing.Point(0, 0);
		_layout.Name = "_layout";
		_layout.Padding = new System.Windows.Forms.Padding(20);
		_layout.RowCount = 5;
		_layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
		_layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
		_layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.AutoSize));
		_layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 72F));
		_layout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 28F));
		_layout.Size = new System.Drawing.Size(1000, 720);
		_layout.TabIndex = 0;
		//
		// _heading
		//
		_heading.AutoSize = true;
		_heading.Dock = System.Windows.Forms.DockStyle.Fill;
		_heading.Font = new System.Drawing.Font("Segoe UI", 17F, System.Drawing.FontStyle.Bold);
		_heading.Location = new System.Drawing.Point(20, 20);
		_heading.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
		_heading.Name = "_heading";
		_heading.Size = new System.Drawing.Size(960, 36);
		_heading.TabIndex = 0;
		_heading.Text = "Visual Calendar";
		//
		// _introduction
		//
		_introduction.AutoSize = true;
		_introduction.Dock = System.Windows.Forms.DockStyle.Fill;
		_introduction.Location = new System.Drawing.Point(20, 66);
		_introduction.Margin = new System.Windows.Forms.Padding(0, 0, 0, 18);
		_introduction.Name = "_introduction";
		_introduction.Size = new System.Drawing.Size(960, 37);
		_introduction.TabIndex = 1;
		_introduction.Text = "Choose the day, week or month view, a row size, a column filter and a style, then drag or resize appointments. The unallocated calendar and event log expose the complete scheduling workflow. Switch the data to Recurring Calendars to see two calendars of repeating events in the same views; dragging one moves its whole event.";
		//
		// _actions
		//
		_actions.AutoSize = true;
		_actions.AutoSizeMode = System.Windows.Forms.AutoSizeMode.GrowAndShrink;
		_actions.Controls.Add(_populateButton);
		_actions.Controls.Add(_viewSelector);
		_actions.Controls.Add(_timeViewSelector);
		_actions.Controls.Add(_columnFilterComboBox);
		_actions.Controls.Add(_dataSelector);
		_actions.Controls.Add(_styleSelector);
		_actions.Dock = System.Windows.Forms.DockStyle.Fill;
		_actions.Location = new System.Drawing.Point(20, 121);
		_actions.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
		_actions.Name = "_actions";
		_actions.Size = new System.Drawing.Size(960, 69);
		_actions.TabIndex = 2;
		//
		// _populateButton
		//
		_populateButton.AutoSize = true;
		_populateButton.Location = new System.Drawing.Point(3, 3);
		_populateButton.MinimumSize = new System.Drawing.Size(110, 34);
		_populateButton.Name = "_populateButton";
		_populateButton.Size = new System.Drawing.Size(158, 34);
		_populateButton.TabIndex = 0;
		_populateButton.Text = "Generate sample schedule";
		_populateButton.UseVisualStyleBackColor = true;
		_populateButton.Click += PopulateButton_Click;
		//
		// _timeViewSelector
		//
		_timeViewSelector.Location = new System.Drawing.Point(273, 3);
		_timeViewSelector.Name = "_timeViewSelector";
		_timeViewSelector.Size = new System.Drawing.Size(200, 23);
		_timeViewSelector.TabIndex = 2;
		_timeViewSelector.SelectedIndexChanged += TimeViewSelector_SelectedIndexChanged;
		//
		// _columnFilterComboBox
		//
		_columnFilterComboBox.Location = new System.Drawing.Point(479, 3);
		_columnFilterComboBox.Name = "_columnFilterComboBox";
		_columnFilterComboBox.Size = new System.Drawing.Size(200, 23);
		_columnFilterComboBox.TabIndex = 3;
		_columnFilterComboBox.SelectedIndexChanged += ColumnFilterComboBox_SelectedIndexChanged;
		//
		// _viewSelector
		//
		_viewSelector.Location = new System.Drawing.Point(167, 3);
		_viewSelector.Name = "_viewSelector";
		_viewSelector.Size = new System.Drawing.Size(100, 23);
		_viewSelector.TabIndex = 1;
		_viewSelector.SelectedIndexChanged += ViewSelector_SelectedIndexChanged;
		//
		// _dataSelector
		//
		_dataSelector.Location = new System.Drawing.Point(685, 3);
		_dataSelector.Name = "_dataSelector";
		_dataSelector.Size = new System.Drawing.Size(170, 23);
		_dataSelector.TabIndex = 4;
		_dataSelector.SelectedIndexChanged += DataSelector_SelectedIndexChanged;
		//
		// _styleSelector
		//
		_styleSelector.Location = new System.Drawing.Point(3, 43);
		_styleSelector.Name = "_styleSelector";
		_styleSelector.Size = new System.Drawing.Size(140, 23);
		_styleSelector.TabIndex = 5;
		_styleSelector.SelectedIndexChanged += StyleSelector_SelectedIndexChanged;
		//
		// _scheduleLayout
		//
		_scheduleLayout.ColumnCount = 2;
		_scheduleLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
		_scheduleLayout.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 240F));
		_scheduleLayout.Controls.Add(_visualCalendar, 0, 0);
		_scheduleLayout.Controls.Add(_unallocatedCalendar, 1, 0);
		_scheduleLayout.Dock = System.Windows.Forms.DockStyle.Fill;
		_scheduleLayout.Location = new System.Drawing.Point(20, 200);
		_scheduleLayout.Margin = new System.Windows.Forms.Padding(0, 0, 0, 10);
		_scheduleLayout.Name = "_scheduleLayout";
		_scheduleLayout.RowCount = 1;
		_scheduleLayout.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
		_scheduleLayout.Size = new System.Drawing.Size(960, 350);
		_scheduleLayout.TabIndex = 3;
		//
		// _visualCalendar
		//
		_visualCalendar.Dock = System.Windows.Forms.DockStyle.Fill;
		_visualCalendar.Location = new System.Drawing.Point(0, 0);
		_visualCalendar.Margin = new System.Windows.Forms.Padding(0, 0, 10, 0);
		_visualCalendar.Name = "VisualCalendar";
		_visualCalendar.Size = new System.Drawing.Size(710, 350);
		_visualCalendar.TabIndex = 0;
		_visualCalendar.VisualCalendarFreeRegionSelected += VisualCalendar_VisualCalendarFreeRegionSelected;
		_visualCalendar.AppointmentSelected += VisualCalendar_AppointmentSelected;
		_visualCalendar.AppointmentDoubleClicked += VisualCalendar_AppointmentDoubleClicked;
		_visualCalendar.AppointmentDeselected += VisualCalendar_AppointmentDeselected;
		_visualCalendar.AppointmentResizingStarted += VisualCalendar_AppointmentResizingStarted;
		_visualCalendar.AppointmentResizing += VisualCalendar_AppointmentResizing;
		_visualCalendar.AppointmentResizingFinished += VisualCalendar_AppointmentResizingFinished;
		_visualCalendar.AppointmentDragStarting += VisualCalendar_AppointmentDragStarting;
		_visualCalendar.AppointmentDragging += VisualCalendar_AppointmentDragging;
		_visualCalendar.AppointmentDragged += VisualCalendar_AppointmentDrag;
		_visualCalendar.AppointmentDropStarting += VisualCalendar_AppointmentDropStarting;
		_visualCalendar.AppointmentDrop += VisualCalendar_AppointmentDrop;
		//
		// _unallocatedCalendar
		//
		_unallocatedCalendar.Dock = System.Windows.Forms.DockStyle.Fill;
		_unallocatedCalendar.Location = new System.Drawing.Point(720, 0);
		_unallocatedCalendar.Margin = new System.Windows.Forms.Padding(0);
		_unallocatedCalendar.Name = "UnallocatedCalendar";
		_unallocatedCalendar.Size = new System.Drawing.Size(240, 350);
		_unallocatedCalendar.TabIndex = 1;
		_unallocatedCalendar.AppointmentSelected += UnallocatedCalendar_AppointmentSelected;
		_unallocatedCalendar.AppointmentDoubleClicked += UnallocatedCalendar_AppointmentDoubleClicked;
		_unallocatedCalendar.AppointmentDeselected += UnallocatedCalendar_AppointmentDeselected;
		_unallocatedCalendar.AppointmentDragStarting += UnallocatedCalendar_AppointmentDragStarting;
		_unallocatedCalendar.AppointmentDragging += UnallocatedCalendar_AppointmentDragging;
		_unallocatedCalendar.AppointmentDragged += UnallocatedCalendar_AppointmentDrag;
		_unallocatedCalendar.AppointmentDropStarting += UnallocatedCalendar_AppointmentDropStarting;
		_unallocatedCalendar.AppointmentDrop += UnallocatedCalendar_AppointmentDrop;
		//
		// _outputTextBox
		//
		_outputTextBox.BackColor = System.Drawing.SystemColors.Window;
		_outputTextBox.Dock = System.Windows.Forms.DockStyle.Fill;
		_outputTextBox.Location = new System.Drawing.Point(23, 563);
		_outputTextBox.Multiline = true;
		_outputTextBox.Name = "_outputTextBox";
		_outputTextBox.ReadOnly = true;
		_outputTextBox.ScrollBars = System.Windows.Forms.ScrollBars.Both;
		_outputTextBox.Size = new System.Drawing.Size(954, 134);
		_outputTextBox.TabIndex = 4;
		_outputTextBox.WordWrap = false;
		//
		// VisualCalendarScreen
		//
		AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
		AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		BackColor = System.Drawing.SystemColors.Window;
		Controls.Add(_layout);
		Font = new System.Drawing.Font("Segoe UI", 9F);
		Name = "VisualCalendarScreen";
		Size = new System.Drawing.Size(1000, 720);
		Text = "Visual Calendar";
		_scheduleLayout.ResumeLayout(false);
		_layout.ResumeLayout(false);
		_layout.PerformLayout();
		ResumeLayout(false);
	}

	#endregion

	private System.Windows.Forms.TableLayoutPanel _layout;
	private System.Windows.Forms.Label _heading;
	private System.Windows.Forms.Label _introduction;
	private System.Windows.Forms.FlowLayoutPanel _actions;
	private System.Windows.Forms.Button _populateButton;
	private Sphere10.Framework.Windows.Forms.EnumComboBox _timeViewSelector;
	private Sphere10.Framework.Windows.Forms.EnumComboBox _columnFilterComboBox;
	private Sphere10.Framework.Windows.Forms.EnumComboBox _viewSelector;
	private Sphere10.Framework.Windows.Forms.EnumComboBox _dataSelector;
	private Sphere10.Framework.Windows.Forms.EnumComboBox _styleSelector;
	private System.Windows.Forms.TableLayoutPanel _scheduleLayout;
	private Sphere10.Framework.Windows.Forms.VisualCalendar.VisualCalendar _visualCalendar;
	private Sphere10.Framework.Windows.Forms.VisualCalendar.UnallocatedCalendar _unallocatedCalendar;
	private System.Windows.Forms.TextBox _outputTextBox;
}
