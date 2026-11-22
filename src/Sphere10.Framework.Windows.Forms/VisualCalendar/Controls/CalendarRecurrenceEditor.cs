// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms.VisualCalendar;

/// <summary>
/// Edits a <see cref="CalendarRecurrence"/>: how it repeats, when it starts and ends, on which weekdays, days, weeks and months,
/// and whether each occurrence repeats within itself. Only the choices that apply to the chosen frequency are enabled, and the
/// rule is shown in words, or what is wrong with it, as it is edited.
/// </summary>
/// <remarks>Set <see cref="Recurrence"/> to show a rule and read it back for the edited one. Choices the frequency does not use are
/// kept while editing, so switching frequency and back loses nothing, and left out of the rule that is read.</remarks>
public partial class CalendarRecurrenceEditor : UserControl {
	/// <summary>Raised after the user changed the rule.</summary>
	public event EventHandlerEx RecurrenceChanged;

	private bool _loading;

	public CalendarRecurrenceEditor() {
		InitializeComponent();
		_loading = true;
		_frequency.EnumType = typeof(CalendarFrequency);
		_start.Value = DateTime.Today.AddHours(DateTime.Now.Hour + 1);
		_end.Value = DateTime.Today.AddYears(1);
		_loading = false;
		ApplyFrequency();
	}

	/// <summary>The rule as edited. Reading it does not validate it; use <see cref="ValidateRecurrence"/>.</summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public CalendarRecurrence Recurrence {
		get {
			var frequency = Frequency;
			return new CalendarRecurrence {
				Frequency = frequency,
				Start = TrimToMinute(_start.Value),
				End = _ends.Checked ? TrimToMinute(_end.Value) : null,
				Interval = frequency is CalendarFrequency.Daily or CalendarFrequency.Weekly ? (int)_interval.Value : 1,
				DaysOfWeek = frequency is CalendarFrequency.Weekly or CalendarFrequency.MonthlyDayOfWeek ? (CalendarDaysOfWeek)ReadMask(_daysOfWeek) : CalendarDaysOfWeek.None,
				DaysOfMonth = frequency == CalendarFrequency.Monthly ? ParseDaysOfMonth() : Array.Empty<int>(),
				LastDayOfMonth = frequency == CalendarFrequency.Monthly && _lastDayOfMonth.Checked,
				Months = frequency is CalendarFrequency.Monthly or CalendarFrequency.MonthlyDayOfWeek ? (CalendarMonths)ReadMask(_months) : CalendarMonths.EveryMonth,
				WeeksOfMonth = frequency == CalendarFrequency.MonthlyDayOfWeek ? (CalendarWeeksOfMonth)ReadMask(_weeksOfMonth) : CalendarWeeksOfMonth.None,
				RepeatEvery = _repeatEvery.Value > 0 ? TimeSpan.FromMinutes((double)_repeatEvery.Value) : null,
				RepeatFor = _repeatEvery.Value > 0 && _repeatFor.Value > 0 ? TimeSpan.FromMinutes((double)_repeatFor.Value) : null
			};
		}
		set {
			Guard.ArgumentNotNull(value, nameof(value));
			_loading = true;
			try {
				_frequency.SelectedEnum = value.Frequency;
				_start.Value = Clamp(_start, value.Start);
				_ends.Checked = value.End.HasValue;
				if (value.End.HasValue)
					_end.Value = Clamp(_end, value.End.Value);
				_interval.Value = Math.Clamp(value.Interval, (int)_interval.Minimum, (int)_interval.Maximum);
				WriteMask(_daysOfWeek, (int)value.DaysOfWeek);
				_daysOfMonth.Text = string.Join(", ", (value.DaysOfMonth ?? Array.Empty<int>()).OrderBy(day => day).Select(day => day.ToString(CultureInfo.CurrentCulture)));
				_lastDayOfMonth.Checked = value.LastDayOfMonth;
				WriteMask(_months, (int)value.Months);
				WriteMask(_weeksOfMonth, (int)value.WeeksOfMonth);
				_repeatEvery.Value = Math.Clamp((decimal)Math.Round(value.RepeatEvery?.TotalMinutes ?? 0), _repeatEvery.Minimum, _repeatEvery.Maximum);
				_repeatFor.Value = Math.Clamp((decimal)Math.Round(value.RepeatFor?.TotalMinutes ?? 0), _repeatFor.Minimum, _repeatFor.Maximum);
			} finally {
				_loading = false;
			}
			ApplyFrequency();
		}
	}

	private CalendarFrequency Frequency => _frequency.SelectedEnum is CalendarFrequency frequency ? frequency : CalendarFrequency.Once;

	/// <summary>Reports everything that keeps the edited rule from occurring, including days of the month that are not numbers.</summary>
	public Result ValidateRecurrence() {
		var result = Recurrence.Validate();
		if (Frequency == CalendarFrequency.Monthly && SplitDaysOfMonth().Any(day => !int.TryParse(day, NumberStyles.Integer, CultureInfo.CurrentCulture, out _)))
			result.AddError("Days of the month are numbers from 1 to 31, separated by commas.");
		return result;
	}

	protected virtual void OnRecurrenceChanged() {
	}

	private void NotifyRecurrenceChanged() {
		OnRecurrenceChanged();
		RecurrenceChanged?.Invoke();
	}

	// Enables the choices the frequency uses and puts the rule, or what is wrong with it, into words.
	private void ApplyFrequency() {
		var frequency = Frequency;
		_interval.Enabled = _intervalLabel.Enabled = _intervalUnit.Enabled = frequency is CalendarFrequency.Daily or CalendarFrequency.Weekly;
		_intervalUnit.Text = frequency == CalendarFrequency.Weekly ? "Weeks" : "Days";
		_daysOfWeek.Enabled = _daysOfWeekLabel.Enabled = frequency is CalendarFrequency.Weekly or CalendarFrequency.MonthlyDayOfWeek;
		_daysOfMonth.Enabled = _daysOfMonthLabel.Enabled = _lastDayOfMonth.Enabled = frequency == CalendarFrequency.Monthly;
		_weeksOfMonth.Enabled = _weeksOfMonthLabel.Enabled = frequency == CalendarFrequency.MonthlyDayOfWeek;
		_months.Enabled = _monthsLabel.Enabled = frequency is CalendarFrequency.Monthly or CalendarFrequency.MonthlyDayOfWeek;
		_repeatFor.Enabled = _repeatForLabel.Enabled = _repeatHint.Enabled = _repeatEvery.Value > 0;
		_end.Enabled = _ends.Checked;
		var validation = ValidateRecurrence();
		_summary.Text = validation.IsSuccess ? Recurrence.Describe() : string.Join(" ", validation.ErrorMessages);
	}

	private string[] SplitDaysOfMonth() => _daysOfMonth.Text.Split(new[] { ',', ';', ' ' }, StringSplitOptions.RemoveEmptyEntries);

	// Entries that are not numbers are reported by validation and left out here.
	private int[] ParseDaysOfMonth()
		=> SplitDaysOfMonth().Select(day => int.TryParse(day, NumberStyles.Integer, CultureInfo.CurrentCulture, out var number) ? number : 0).Where(day => day != 0).Distinct().OrderBy(day => day).ToArray();

	// The lists hold their set's members in bit order, so item n is bit n.
	private static int ReadMask(CheckedListBox list) => list.CheckedIndices.Cast<int>().Aggregate(0, (mask, index) => mask | 1 << index);

	private static void WriteMask(CheckedListBox list, int mask) {
		for (var index = 0; index < list.Items.Count; index++)
			list.SetItemChecked(index, (mask & 1 << index) != 0);
	}

	private static DateTime Clamp(DateTimePicker picker, DateTime value) => value < picker.MinDate ? picker.MinDate : value > picker.MaxDate ? picker.MaxDate : value;

	private static DateTime TrimToMinute(DateTime value) => new(value.Year, value.Month, value.Day, value.Hour, value.Minute, 0);

	private void ChoiceChanged(object sender, EventArgs args) {
		if (_loading)
			return;
		ApplyFrequency();
		NotifyRecurrenceChanged();
	}

	// The check state changes after this event, so the rule is read once the list has applied it.
	private void ListItemChecked(object sender, ItemCheckEventArgs args) {
		if (!_loading && IsHandleCreated)
			BeginInvoke(new Action(() => ChoiceChanged(sender, EventArgs.Empty)));
	}
}
