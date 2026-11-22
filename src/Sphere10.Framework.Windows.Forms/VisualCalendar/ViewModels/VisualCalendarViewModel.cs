// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Linq;

namespace Sphere10.Framework.Windows.Forms.VisualCalendar;

public class VisualCalendarViewModel : BaseVisualCalendarViewModel {
	private ColumnViewModel[] _allColumns;
	private ColumnViewModel[] _visibleColumns;
	private VisualCalendarViewModelFilter _columnFilter;
	protected IDictionary<Tuple<int, int>, AppointmentViewModel> _blockLookup;

	public VisualCalendarViewModel(IEnumerable<ColumnViewModel> appointments, DateTime timePeriodStart, TimePeriodType timeView, VisualCalendarViewModelFilter columnFilter = VisualCalendarViewModelFilter.All)
		: base(timePeriodStart, timeView) {
		SetData(appointments, timePeriodStart, timeView, columnFilter);
	}

	public VisualCalendarViewModelFilter ColumnFilter {
		get { return _columnFilter; }
		set {
			_columnFilter = value;
			SetVisibleColumns();
		}
	}

	public void SetData(IEnumerable<ColumnViewModel> appointments, DateTime timePeriodStart, TimePeriodType timeView, VisualCalendarViewModelFilter columnFilter = VisualCalendarViewModelFilter.All) {
		base.Initialize(timePeriodStart, timeView);
		_blockLookup = new Dictionary<Tuple<int, int>, AppointmentViewModel>();
		_allColumns = appointments.ToArray();
		_visibleColumns = new ColumnViewModel[0];
		_columnFilter = columnFilter;
		SetVisibleColumns();
	}

	internal override string GetColumnHeaderText(int col) {
		return _visibleColumns[col].Name;
	}

	// A day's heading covers the further columns of that day, which carry no name of their own.
	internal override int GetColumnHeaderSpan(int col) {
		if (_visibleColumns[col].ColumnDataObject is not DayColumn day)
			return 1;
		var span = 1;
		while (col + span < _visibleColumns.Length && _visibleColumns[col + span].ColumnDataObject is DayColumn next && next.Date == day.Date && string.IsNullOrEmpty(_visibleColumns[col + span].Name))
			span++;
		return span;
	}

	/// <summary>The displayed columns, in order, after the column filter.</summary>
	public ColumnViewModel[] Columns => (ColumnViewModel[])_visibleColumns.Clone();

	internal override TimeSpan GetColumnOffset(int col) => col >= 0 && col < _visibleColumns.Length ? GetColumnOffset(_visibleColumns[col]) : TimeSpan.Zero;

	// The monthly view lists days down its rows, so a column's own day has no meaning there.
	private TimeSpan GetColumnOffset(ColumnViewModel column)
		=> TimeView != TimePeriodType.Monthly && column.ColumnDataObject?.Date is { } date ? date.Date - TimePeriodStart.Date : TimeSpan.Zero;

	internal ColumnViewModel GetColumnAt(int col) {
		return _visibleColumns[col];
	}

	internal AppointmentViewModel GetAppointmentBlockAt(int col, int row) {
		var key = Tuple.Create(col, row);
		if (!_blockLookup.ContainsKey(key))
			return null;

		return _blockLookup[key];
	}

	internal AppointmentViewModel FindAppointmentByDataObject(Appointment appointment) {
		return _blockLookup.Values.FirstOrDefault(avm => avm.AppointmentDataObject.Equals(appointment));
	}

	internal bool ContainsColumn(AppointmentColumn column) {
		// In the week view the data source's columns are behind the appointments, not the displayed day columns.
		return _visibleColumns.Any(vc => vc.ColumnDataObject == column || vc.Appointments.Any(appointment => appointment.SourceColumn == column));
	}

	internal override int GetColumnCount() {
		return _visibleColumns.Length;
	}

	internal override CellViewModel GetCellDisplay(int col, int row) {
		if (col < 0 || col > GetColumnCount() - 1 || row < 0 || row > GetRowCount()) {
			return CellViewModel.Empty;
		}
		// The CellDisplay's ARE the data source, so we route call to datasource
		var display = CellViewModel.Empty;

		var block = GetAppointmentBlockAt(col, row);
		if (block != null) {
			var startRow = block.StartRow;
			var index = row - startRow;
			display = block.Lines[index];
		}
		return display;
	}

	protected override void RecreateCellDisplays() {
		_blockLookup.Clear();
		if (_visibleColumns != null) {
			for (int i = 0; i < _visibleColumns.Length; i++) {
				var appointmentColumn = _visibleColumns[i];
				appointmentColumn.Index = i;
				// A day column shows its own day: its window and rows are the calendar's, moved by the column's offset.
				var offset = GetColumnOffset(appointmentColumn);
				var windowStart = TimePeriodStart + offset;
				var windowEnd = TimePeriodEnd + offset;
				for (int j = 0; j < appointmentColumn.Appointments.Length; j++) {
					var appointmentBlock = appointmentColumn.Appointments[j];
					appointmentBlock.Column = appointmentColumn;

					if (appointmentBlock.AppointmentDataObject.EndTime <= windowStart || appointmentBlock.AppointmentDataObject.StartTime >= windowEnd)
						continue;

					// clip the appointment starttimes/endtimes (for the viewmodel)
					if (appointmentBlock.AppointmentDataObject.StartTime < windowStart)
						appointmentBlock.VisibleStartTime = windowStart;
					else
						appointmentBlock.VisibleStartTime = appointmentBlock.AppointmentDataObject.StartTime;

					if (appointmentBlock.AppointmentDataObject.EndTime > windowEnd)
						appointmentBlock.VisibleEndTime = windowEnd;
					else
						appointmentBlock.VisibleEndTime = appointmentBlock.AppointmentDataObject.EndTime;

					var startRow = StartTimeToRow(appointmentBlock.VisibleStartTime - offset);
					var endRow = FinishTimeToRow(appointmentBlock.VisibleEndTime - offset);

					// save the apppointment block in the lookup
					for (int x = startRow; x <= endRow; x++)
						_blockLookup[Tuple.Create(i, x)] = appointmentBlock;

					appointmentBlock.StartRow = startRow;
					appointmentBlock.EndRow = endRow;
					appointmentBlock.Lines = CellViewModel.CreateArray(endRow - startRow + 1);


					// Set the traits of the cells
					appointmentBlock.Lines.WithDescriptions().ForEach(cellDescription => {
						cellDescription.Item.Traits = 0;
						if (cellDescription.Description.HasFlag(EnumeratedItemDescription.First)) {
							cellDescription.Item.Traits = cellDescription.Item.Traits.CopyAndSetFlags(CellTraits.Top);
						}

						if (cellDescription.Description.HasFlag(EnumeratedItemDescription.Last)) {
							cellDescription.Item.Traits = cellDescription.Item.Traits.CopyAndSetFlags(CellTraits.Bottom);
						}

						if (cellDescription.Description.HasFlag(EnumeratedItemDescription.Interior)) {
							cellDescription.Item.Traits = cellDescription.Item.Traits.CopyAndSetFlags(CellTraits.Interior);
						}
					});
					appointmentBlock.RequestRender(appointmentBlock, appointmentBlock.Lines);
				}
			}
		}
	}

	protected virtual void SetVisibleColumns() {
		try {
			_blockLookup.Clear();

			//// filter out unnecessary appointment columns and blocks
			DateTime startTime;
			DateTime endTime;
			switch (TimeView) {
				case TimePeriodType.Monthly:
					startTime = TimePeriodStart.ToBeginningOfMonth();
					endTime = TimePeriodStart.ToEndOfMonth();
					break;
				case TimePeriodType.DailyHourly:
				case TimePeriodType.DailyHalfHourly:
				case TimePeriodType.DailyQuaterHourly:
				default:
					startTime = TimePeriodStart.ToMidnight();
					endTime = TimePeriodStart.To1159PM();
					break;
			}


			switch (ColumnFilter) {
				case VisualCalendarViewModelFilter.All:
					_visibleColumns = _allColumns;
					break;
				case VisualCalendarViewModelFilter.ExcludeEmptyColumns:
					_visibleColumns = (
						from appointment in _allColumns
						let offset = GetColumnOffset(appointment)
						where appointment.Appointments.Any(block => block.VisibleStartTime < endTime + offset && block.VisibleEndTime > startTime + offset)
						select appointment
					).ToArray();
					break;
				case VisualCalendarViewModelFilter.ShowOnlyEmptyColumns:
					_visibleColumns = (
						from appointment in _allColumns
						let offset = GetColumnOffset(appointment)
						where !appointment.Appointments.Any(block => block.VisibleStartTime <= endTime + offset && block.VisibleEndTime >= startTime + offset)
						select appointment
					).ToArray();
					;
					break;
			}

			RecreateCellDisplays();
		} catch (Exception error) {
			SystemLog.Exception(error);
			_ = ExceptionDialog.ShowAsync(error);
		}
	}

}

