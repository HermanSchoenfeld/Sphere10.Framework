// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Drawing;
using System.Windows.Forms;
using Sphere10.Framework.Windows.Forms;

namespace Sphere10.Framework.Utils.WinFormsTester;

partial class AppointmentBookScreen {
	private TableLayoutPanel _layout;
	private Label _heading;
	private Label _introduction;
	private FlowLayoutPanel _actions;
	private Button _populateButton;
	private EnumComboBox _timeViewSelector;
	private EnumComboBox _columnFilterComboBox;
	private TableLayoutPanel _scheduleLayout;
	private Sphere10.Framework.Windows.Forms.AppointmentBook.AppointmentBook _appointmentBook;
	private Sphere10.Framework.Windows.Forms.AppointmentBook.UnallocatedBook _unallocatedBook;
	private TextBox _outputTextBox;

	// Static layout and named event wiring remain editable in the native Designer.
	private void InitializeComponent() {
		_layout = new TableLayoutPanel();
		_layout.Name = "_layout";
		_heading = new Label();
		_heading.Name = "_heading";
		_introduction = new Label();
		_introduction.Name = "_introduction";
		_actions = new FlowLayoutPanel();
		_actions.Name = "_actions";
		_populateButton = new Button();
		_populateButton.Name = "_populateButton";
		_timeViewSelector = new EnumComboBox();
		_columnFilterComboBox = new EnumComboBox();
		_scheduleLayout = new TableLayoutPanel();
		_scheduleLayout.Name = "_scheduleLayout";
		_appointmentBook = new Sphere10.Framework.Windows.Forms.AppointmentBook.AppointmentBook();
		_unallocatedBook = new Sphere10.Framework.Windows.Forms.AppointmentBook.UnallocatedBook();
		_outputTextBox = new TextBox();
		_layout.SuspendLayout();
		_scheduleLayout.SuspendLayout();
		SuspendLayout();

		// The heading and instructions remain with this screen when it is detached.
		_layout.Dock = DockStyle.Fill;
		_layout.Padding = new Padding(20);
		_layout.ColumnCount = 1;
		_layout.RowCount = 5;
		_layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_heading.AutoSize = true;
		_heading.Dock = DockStyle.Fill;
		_heading.Font = new Font(SystemFonts.MessageBoxFont.FontFamily, 17F, FontStyle.Bold);
		_heading.Margin = new Padding(0, 0, 0, 10);
		_heading.Text = "Appointment Book";
		_introduction.AutoSize = true;
		_introduction.Dock = DockStyle.Fill;
		_introduction.Margin = new Padding(0, 0, 0, 18);
		_introduction.Text = "Generate a synthetic resource schedule, choose a time period or column filter, and drag or resize appointments. The unallocated book and event log expose the complete scheduling workflow.";
		_layout.Controls.Add(_heading, 0, 0);
		_layout.Controls.Add(_introduction, 0, 1);

		_populateButton.AutoSize = true;
		_populateButton.MinimumSize = new Size(110, 34);
		_populateButton.Text = "Generate sample schedule";
		_populateButton.UseVisualStyleBackColor = true;
		_populateButton.Click += PopulateButton_Click;
		_timeViewSelector.Width = 200;
		_timeViewSelector.Name = "_timeViewSelector";
		_timeViewSelector.SelectedIndexChanged += TimeViewSelector_SelectedIndexChanged;
		_columnFilterComboBox.Width = 200;
		_columnFilterComboBox.Name = "_columnFilterComboBox";
		_columnFilterComboBox.SelectedIndexChanged += ColumnFilterComboBox_SelectedIndexChanged;
		_actions.AutoSize = true;
		_actions.AutoSizeMode = AutoSizeMode.GrowAndShrink;
		_actions.Dock = DockStyle.Fill;
		_actions.Margin = Padding.Empty;
		_actions.Controls.AddRange(new Control[] { _populateButton, _timeViewSelector, _columnFilterComboBox });
		_actions.Margin = new Padding(0, 0, 0, 10);
		_appointmentBook.Name = "AppointmentBook";
		_appointmentBook.Dock = DockStyle.Fill;
		_appointmentBook.Margin = new Padding(0, 0, 10, 0);
		_unallocatedBook.Name = "UnallocatedBook";
		_unallocatedBook.Dock = DockStyle.Fill;
		_unallocatedBook.Margin = Padding.Empty;

		// Unallocated work sits beside the schedule as a tray of the same row height, ready to drag into a resource column.
		_scheduleLayout.Dock = DockStyle.Fill;
		_scheduleLayout.Margin = new Padding(0, 0, 0, 10);
		_scheduleLayout.ColumnCount = 2;
		_scheduleLayout.RowCount = 1;
		_scheduleLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
		_scheduleLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240F));
		_scheduleLayout.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
		_scheduleLayout.Controls.Add(_appointmentBook, 0, 0);
		_scheduleLayout.Controls.Add(_unallocatedBook, 1, 0);
		_outputTextBox.Name = "_outputTextBox";
		_outputTextBox.Dock = DockStyle.Fill;
		_outputTextBox.Multiline = true;
		_outputTextBox.ReadOnly = true;
		_outputTextBox.ScrollBars = ScrollBars.Both;
		_outputTextBox.WordWrap = false;
		_outputTextBox.BackColor = SystemColors.Window;
		_unallocatedBook.AppointmentSelected += UnallocatedBook_AppointmentSelected;
		_unallocatedBook.AppointmentDoubleClicked += UnallocatedBook_AppointmentDoubleClicked;
		_unallocatedBook.AppointmentDeselected += UnallocatedBook_AppointmentDeselected;
		_unallocatedBook.AppointmentDragStarting += UnallocatedBook_AppointmentDragStarting;
		_unallocatedBook.AppointmentDragging += UnallocatedBook_AppointmentDragging;
		_unallocatedBook.AppointmentDragged += UnallocatedBook_AppointmentDrag;
		_unallocatedBook.AppointmentDropStarting += UnallocatedBook_AppointmentDropStarting;
		_unallocatedBook.AppointmentDrop += UnallocatedBook_AppointmentDrop;
		_appointmentBook.AppointmentBookFreeRegionSelected += AppointmentBook_AppointmentBookFreeRegionSelected;
		_appointmentBook.AppointmentSelected += AppointmentBook_AppointmentSelected;
		_appointmentBook.AppointmentDoubleClicked += AppointmentBook_AppointmentDoubleClicked;
		_appointmentBook.AppointmentDeselected += AppointmentBook_AppointmentDeselected;
		_appointmentBook.AppointmentResizingStarted += AppointmentBook_AppointmentResizingStarted;
		_appointmentBook.AppointmentResizing += AppointmentBook_AppointmentResizing;
		_appointmentBook.AppointmentResizingFinished += AppointmentBook_AppointmentResizingFinished;
		_appointmentBook.AppointmentDragStarting += AppointmentBook_AppointmentDragStarting;
		_appointmentBook.AppointmentDragging += AppointmentBook_AppointmentDragging;
		_appointmentBook.AppointmentDragged += AppointmentBook_AppointmentDrag;
		_appointmentBook.AppointmentDropStarting += AppointmentBook_AppointmentDropStarting;
		_appointmentBook.AppointmentDrop += AppointmentBook_AppointmentDrop;

		// Content-sized command rows keep their complete buttons visible at smaller widths.
		_layout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
		_layout.Controls.Add(_actions, 0, 2);
		_layout.RowStyles.Add(new RowStyle(SizeType.Percent, 72F));
		_layout.Controls.Add(_scheduleLayout, 0, 3);
		_layout.RowStyles.Add(new RowStyle(SizeType.Percent, 28F));
		_layout.Controls.Add(_outputTextBox, 0, 4);
		Font = SystemFonts.MessageBoxFont;
		AutoScaleDimensions = new SizeF(7F, 15F);
		AutoScaleMode = AutoScaleMode.Font;
		BackColor = SystemColors.Window;
		Name = "AppointmentBookScreen";
		Text = "Appointment Book";
		Size = new Size(1000, 720);
		Controls.Add(_layout);
		_scheduleLayout.ResumeLayout(false);
		_layout.ResumeLayout(false);
		_layout.PerformLayout();
		ResumeLayout(false);
	}
}
