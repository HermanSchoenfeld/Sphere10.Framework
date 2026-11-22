// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows.Forms.VisualCalendar;

partial class CalendarRecurrenceEditor {
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
		_frequencyLabel = new System.Windows.Forms.Label();
		_frequency = new Sphere10.Framework.Windows.Forms.EnumComboBox();
		_startLabel = new System.Windows.Forms.Label();
		_start = new System.Windows.Forms.DateTimePicker();
		_intervalLabel = new System.Windows.Forms.Label();
		_interval = new System.Windows.Forms.NumericUpDown();
		_intervalUnit = new System.Windows.Forms.Label();
		_daysOfWeekLabel = new System.Windows.Forms.Label();
		_daysOfWeek = new System.Windows.Forms.CheckedListBox();
		_daysOfMonthLabel = new System.Windows.Forms.Label();
		_daysOfMonth = new System.Windows.Forms.TextBox();
		_lastDayOfMonth = new System.Windows.Forms.CheckBox();
		_weeksOfMonthLabel = new System.Windows.Forms.Label();
		_weeksOfMonth = new System.Windows.Forms.CheckedListBox();
		_monthsLabel = new System.Windows.Forms.Label();
		_months = new System.Windows.Forms.CheckedListBox();
		_repeatEveryLabel = new System.Windows.Forms.Label();
		_repeatEvery = new System.Windows.Forms.NumericUpDown();
		_repeatForLabel = new System.Windows.Forms.Label();
		_repeatFor = new System.Windows.Forms.NumericUpDown();
		_repeatHint = new System.Windows.Forms.Label();
		_ends = new System.Windows.Forms.CheckBox();
		_end = new System.Windows.Forms.DateTimePicker();
		_summary = new System.Windows.Forms.Label();
		((System.ComponentModel.ISupportInitialize)_interval).BeginInit();
		((System.ComponentModel.ISupportInitialize)_repeatEvery).BeginInit();
		((System.ComponentModel.ISupportInitialize)_repeatFor).BeginInit();
		SuspendLayout();
		//
		// _frequencyLabel
		//
		_frequencyLabel.AutoSize = true;
		_frequencyLabel.Location = new System.Drawing.Point(3, 6);
		_frequencyLabel.Name = "_frequencyLabel";
		_frequencyLabel.Size = new System.Drawing.Size(51, 15);
		_frequencyLabel.TabIndex = 0;
		_frequencyLabel.Text = "Repeats:";
		//
		// _frequency
		//
		_frequency.Location = new System.Drawing.Point(110, 3);
		_frequency.Name = "_frequency";
		_frequency.Size = new System.Drawing.Size(170, 23);
		_frequency.TabIndex = 1;
		_frequency.SelectedIndexChanged += ChoiceChanged;
		//
		// _startLabel
		//
		_startLabel.AutoSize = true;
		_startLabel.Location = new System.Drawing.Point(300, 6);
		_startLabel.Name = "_startLabel";
		_startLabel.Size = new System.Drawing.Size(37, 15);
		_startLabel.TabIndex = 2;
		_startLabel.Text = "Starts:";
		//
		// _start
		//
		_start.CustomFormat = "yyyy-MM-dd HH:mm";
		_start.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
		_start.Location = new System.Drawing.Point(350, 3);
		_start.Name = "_start";
		_start.Size = new System.Drawing.Size(180, 23);
		_start.TabIndex = 3;
		_start.ValueChanged += ChoiceChanged;
		//
		// _intervalLabel
		//
		_intervalLabel.AutoSize = true;
		_intervalLabel.Location = new System.Drawing.Point(3, 35);
		_intervalLabel.Name = "_intervalLabel";
		_intervalLabel.Size = new System.Drawing.Size(38, 15);
		_intervalLabel.TabIndex = 4;
		_intervalLabel.Text = "Every:";
		//
		// _interval
		//
		_interval.Location = new System.Drawing.Point(110, 32);
		_interval.Maximum = new decimal(new int[] { 999, 0, 0, 0 });
		_interval.Minimum = new decimal(new int[] { 1, 0, 0, 0 });
		_interval.Name = "_interval";
		_interval.Size = new System.Drawing.Size(60, 23);
		_interval.TabIndex = 5;
		_interval.Value = new decimal(new int[] { 1, 0, 0, 0 });
		_interval.ValueChanged += ChoiceChanged;
		//
		// _intervalUnit
		//
		_intervalUnit.AutoSize = true;
		_intervalUnit.Location = new System.Drawing.Point(176, 35);
		_intervalUnit.Name = "_intervalUnit";
		_intervalUnit.Size = new System.Drawing.Size(32, 15);
		_intervalUnit.TabIndex = 6;
		_intervalUnit.Text = "Days";
		//
		// _daysOfWeekLabel
		//
		_daysOfWeekLabel.AutoSize = true;
		_daysOfWeekLabel.Location = new System.Drawing.Point(3, 64);
		_daysOfWeekLabel.Name = "_daysOfWeekLabel";
		_daysOfWeekLabel.Size = new System.Drawing.Size(62, 15);
		_daysOfWeekLabel.TabIndex = 7;
		_daysOfWeekLabel.Text = "Weekdays:";
		//
		// _daysOfWeek
		//
		_daysOfWeek.CheckOnClick = true;
		_daysOfWeek.ColumnWidth = 100;
		_daysOfWeek.FormattingEnabled = true;
		_daysOfWeek.Items.AddRange(new object[] { "Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday" });
		_daysOfWeek.Location = new System.Drawing.Point(110, 61);
		_daysOfWeek.MultiColumn = true;
		_daysOfWeek.Name = "_daysOfWeek";
		_daysOfWeek.Size = new System.Drawing.Size(420, 40);
		_daysOfWeek.TabIndex = 8;
		_daysOfWeek.ItemCheck += ListItemChecked;
		//
		// _daysOfMonthLabel
		//
		_daysOfMonthLabel.AutoSize = true;
		_daysOfMonthLabel.Location = new System.Drawing.Point(3, 110);
		_daysOfMonthLabel.Name = "_daysOfMonthLabel";
		_daysOfMonthLabel.Size = new System.Drawing.Size(92, 15);
		_daysOfMonthLabel.TabIndex = 9;
		_daysOfMonthLabel.Text = "Days Of Month:";
		//
		// _daysOfMonth
		//
		_daysOfMonth.Location = new System.Drawing.Point(110, 107);
		_daysOfMonth.Name = "_daysOfMonth";
		_daysOfMonth.PlaceholderText = "1, 15";
		_daysOfMonth.Size = new System.Drawing.Size(200, 23);
		_daysOfMonth.TabIndex = 10;
		_daysOfMonth.TextChanged += ChoiceChanged;
		//
		// _lastDayOfMonth
		//
		_lastDayOfMonth.AutoSize = true;
		_lastDayOfMonth.Location = new System.Drawing.Point(320, 109);
		_lastDayOfMonth.Name = "_lastDayOfMonth";
		_lastDayOfMonth.Size = new System.Drawing.Size(123, 19);
		_lastDayOfMonth.TabIndex = 11;
		_lastDayOfMonth.Text = "Last Day Of Month";
		_lastDayOfMonth.UseVisualStyleBackColor = true;
		_lastDayOfMonth.CheckedChanged += ChoiceChanged;
		//
		// _weeksOfMonthLabel
		//
		_weeksOfMonthLabel.AutoSize = true;
		_weeksOfMonthLabel.Location = new System.Drawing.Point(3, 139);
		_weeksOfMonthLabel.Name = "_weeksOfMonthLabel";
		_weeksOfMonthLabel.Size = new System.Drawing.Size(43, 15);
		_weeksOfMonthLabel.TabIndex = 12;
		_weeksOfMonthLabel.Text = "Weeks:";
		//
		// _weeksOfMonth
		//
		_weeksOfMonth.CheckOnClick = true;
		_weeksOfMonth.ColumnWidth = 80;
		_weeksOfMonth.FormattingEnabled = true;
		_weeksOfMonth.Items.AddRange(new object[] { "First", "Second", "Third", "Fourth", "Last" });
		_weeksOfMonth.Location = new System.Drawing.Point(110, 136);
		_weeksOfMonth.MultiColumn = true;
		_weeksOfMonth.Name = "_weeksOfMonth";
		_weeksOfMonth.Size = new System.Drawing.Size(420, 22);
		_weeksOfMonth.TabIndex = 13;
		_weeksOfMonth.ItemCheck += ListItemChecked;
		//
		// _monthsLabel
		//
		_monthsLabel.AutoSize = true;
		_monthsLabel.Location = new System.Drawing.Point(3, 167);
		_monthsLabel.Name = "_monthsLabel";
		_monthsLabel.Size = new System.Drawing.Size(51, 15);
		_monthsLabel.TabIndex = 14;
		_monthsLabel.Text = "Months:";
		//
		// _months
		//
		_months.CheckOnClick = true;
		_months.ColumnWidth = 100;
		_months.FormattingEnabled = true;
		_months.Items.AddRange(new object[] { "January", "February", "March", "April", "May", "June", "July", "August", "September", "October", "November", "December" });
		_months.Location = new System.Drawing.Point(110, 164);
		_months.MultiColumn = true;
		_months.Name = "_months";
		_months.Size = new System.Drawing.Size(420, 58);
		_months.TabIndex = 15;
		_months.ItemCheck += ListItemChecked;
		//
		// _repeatEveryLabel
		//
		_repeatEveryLabel.AutoSize = true;
		_repeatEveryLabel.Location = new System.Drawing.Point(3, 231);
		_repeatEveryLabel.Name = "_repeatEveryLabel";
		_repeatEveryLabel.Size = new System.Drawing.Size(77, 15);
		_repeatEveryLabel.TabIndex = 16;
		_repeatEveryLabel.Text = "Repeat Every:";
		//
		// _repeatEvery
		//
		_repeatEvery.Location = new System.Drawing.Point(110, 228);
		_repeatEvery.Maximum = new decimal(new int[] { 1440, 0, 0, 0 });
		_repeatEvery.Name = "_repeatEvery";
		_repeatEvery.Size = new System.Drawing.Size(70, 23);
		_repeatEvery.TabIndex = 17;
		_repeatEvery.ValueChanged += ChoiceChanged;
		//
		// _repeatForLabel
		//
		_repeatForLabel.AutoSize = true;
		_repeatForLabel.Location = new System.Drawing.Point(186, 231);
		_repeatForLabel.Name = "_repeatForLabel";
		_repeatForLabel.Size = new System.Drawing.Size(76, 15);
		_repeatForLabel.TabIndex = 18;
		_repeatForLabel.Text = "Minutes, For:";
		//
		// _repeatFor
		//
		_repeatFor.Location = new System.Drawing.Point(270, 228);
		_repeatFor.Maximum = new decimal(new int[] { 44640, 0, 0, 0 });
		_repeatFor.Name = "_repeatFor";
		_repeatFor.Size = new System.Drawing.Size(70, 23);
		_repeatFor.TabIndex = 19;
		_repeatFor.ValueChanged += ChoiceChanged;
		//
		// _repeatHint
		//
		_repeatHint.AutoSize = true;
		_repeatHint.Location = new System.Drawing.Point(346, 231);
		_repeatHint.Name = "_repeatHint";
		_repeatHint.Size = new System.Drawing.Size(163, 15);
		_repeatHint.TabIndex = 20;
		_repeatHint.Text = "Minutes (0 = Until The Next)";
		//
		// _ends
		//
		_ends.AutoSize = true;
		_ends.Location = new System.Drawing.Point(6, 259);
		_ends.Name = "_ends";
		_ends.Size = new System.Drawing.Size(54, 19);
		_ends.TabIndex = 21;
		_ends.Text = "Ends:";
		_ends.UseVisualStyleBackColor = true;
		_ends.CheckedChanged += ChoiceChanged;
		//
		// _end
		//
		_end.CustomFormat = "yyyy-MM-dd HH:mm";
		_end.Format = System.Windows.Forms.DateTimePickerFormat.Custom;
		_end.Location = new System.Drawing.Point(110, 257);
		_end.Name = "_end";
		_end.Size = new System.Drawing.Size(180, 23);
		_end.TabIndex = 22;
		_end.ValueChanged += ChoiceChanged;
		//
		// _summary
		//
		_summary.AutoEllipsis = true;
		_summary.Location = new System.Drawing.Point(3, 287);
		_summary.Name = "_summary";
		_summary.Size = new System.Drawing.Size(527, 34);
		_summary.TabIndex = 23;
		//
		// CalendarRecurrenceEditor
		//
		AutoScaleDimensions = new System.Drawing.SizeF(7F, 15F);
		AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
		Controls.Add(_frequencyLabel);
		Controls.Add(_frequency);
		Controls.Add(_startLabel);
		Controls.Add(_start);
		Controls.Add(_intervalLabel);
		Controls.Add(_interval);
		Controls.Add(_intervalUnit);
		Controls.Add(_daysOfWeekLabel);
		Controls.Add(_daysOfWeek);
		Controls.Add(_daysOfMonthLabel);
		Controls.Add(_daysOfMonth);
		Controls.Add(_lastDayOfMonth);
		Controls.Add(_weeksOfMonthLabel);
		Controls.Add(_weeksOfMonth);
		Controls.Add(_monthsLabel);
		Controls.Add(_months);
		Controls.Add(_repeatEveryLabel);
		Controls.Add(_repeatEvery);
		Controls.Add(_repeatForLabel);
		Controls.Add(_repeatFor);
		Controls.Add(_repeatHint);
		Controls.Add(_ends);
		Controls.Add(_end);
		Controls.Add(_summary);
		Name = "CalendarRecurrenceEditor";
		Size = new System.Drawing.Size(536, 324);
		((System.ComponentModel.ISupportInitialize)_interval).EndInit();
		((System.ComponentModel.ISupportInitialize)_repeatEvery).EndInit();
		((System.ComponentModel.ISupportInitialize)_repeatFor).EndInit();
		ResumeLayout(false);
		PerformLayout();
	}

	#endregion

	private System.Windows.Forms.Label _frequencyLabel;
	private Sphere10.Framework.Windows.Forms.EnumComboBox _frequency;
	private System.Windows.Forms.Label _startLabel;
	private System.Windows.Forms.DateTimePicker _start;
	private System.Windows.Forms.Label _intervalLabel;
	private System.Windows.Forms.NumericUpDown _interval;
	private System.Windows.Forms.Label _intervalUnit;
	private System.Windows.Forms.Label _daysOfWeekLabel;
	private System.Windows.Forms.CheckedListBox _daysOfWeek;
	private System.Windows.Forms.Label _daysOfMonthLabel;
	private System.Windows.Forms.TextBox _daysOfMonth;
	private System.Windows.Forms.CheckBox _lastDayOfMonth;
	private System.Windows.Forms.Label _weeksOfMonthLabel;
	private System.Windows.Forms.CheckedListBox _weeksOfMonth;
	private System.Windows.Forms.Label _monthsLabel;
	private System.Windows.Forms.CheckedListBox _months;
	private System.Windows.Forms.Label _repeatEveryLabel;
	private System.Windows.Forms.NumericUpDown _repeatEvery;
	private System.Windows.Forms.Label _repeatForLabel;
	private System.Windows.Forms.NumericUpDown _repeatFor;
	private System.Windows.Forms.Label _repeatHint;
	private System.Windows.Forms.CheckBox _ends;
	private System.Windows.Forms.DateTimePicker _end;
	private System.Windows.Forms.Label _summary;
}
