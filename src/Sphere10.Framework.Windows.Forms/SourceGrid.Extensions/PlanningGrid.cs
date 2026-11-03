// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.ComponentModel;
using Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Controllers;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Planning;

/// <summary>
/// Displays appointments across day columns and configurable time slots.
/// </summary>
public partial class PlanningGrid : UserControlEx {
	public event AppointmentEventHandler AppointmentClick;

	public event AppointmentEventHandler AppointmentDoubleClick;

	private const int _rowsHeader = 2;

	private const int _columnsHeader = 2;

	public PlanningGrid() {
		// This call is required by the Windows.Forms Form Designer.
		InitializeComponent();
	}

	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public AppointmentCollection Appointments { get; } = new AppointmentCollection();

	public DateTime DateTimeStart { get; private set; }

	public DateTime DateTimeEnd { get; private set; }

	public int MinAppointmentLength { get; private set; }

	/// <summary>
	/// The grid used internally to display the planning (note that you usually don't need to access directly this class).
	/// </summary>
	[Browsable(false), DesignerSerializationVisibility(DesignerSerializationVisibility.Hidden)]
	public Grid Grid => _grid;

	/// <summary>
	/// Load the grid using the parameters specified
	/// </summary>
	/// <param name="dateTimeStart"></param>
	/// <param name="dateTimeEnd"></param>
	/// <param name="minAppointmentLength"></param>
	public void LoadPlanning(DateTime dateTimeStart, DateTime dateTimeEnd, int minAppointmentLength) {
		using var update = EnterUpdateScope(FinishedUpdateBehaviour.DoNothing);
		DateTimeStart = dateTimeStart;
		DateTimeEnd = dateTimeEnd;
		MinAppointmentLength = minAppointmentLength;

		if (dateTimeStart >= dateTimeEnd)
			throw new ApplicationException("Invalid Planning Range");
		if (dateTimeStart.TimeOfDay >= dateTimeEnd.TimeOfDay)
			throw new ApplicationException("Invalid Plannnin Range");
		if (dateTimeStart.TimeOfDay.Minutes != 0 || dateTimeEnd.TimeOfDay.Minutes != 0)
			throw new ApplicationException("Invalid Start or End hours must be with 0 minutes");
		if (minAppointmentLength <= 0 || minAppointmentLength > 60)
			throw new ApplicationException("Invalid Minimum Appointment Length");
		if (60 % minAppointmentLength != 0)
			throw new ApplicationException("Invalid Minimum Appointment Length must be multiple of 60");

		var dayInterval = dateTimeEnd - dateTimeStart;
		var timeInterval = dateTimeEnd.TimeOfDay - dateTimeStart.TimeOfDay;
		var partsForHour = 60 / minAppointmentLength;

		if (dayInterval.TotalDays > 30)
			throw new ApplicationException("Range too big");
		if (timeInterval.TotalMinutes < minAppointmentLength)
			throw new ApplicationException("Invalid Minimum Appointment Length for current Planning Range");

		//Redim Grid
		_grid.Redim((int)((timeInterval.TotalHours + 1) * partsForHour + _rowsHeader), (int)(dayInterval.TotalDays + 1 + _columnsHeader));

		//Load Header
		_grid[0, 0] = new Header00(null);
		_grid[0, 0].RowSpan = 2;
		_grid[0, 0].ColumnSpan = 2;
		//create day caption
		var captionDate = dateTimeStart;
		for (var c = _columnsHeader; c < _grid.ColumnsCount; c++) {
			_grid[0, c] = new HeaderDay1(captionDate.ToShortDateString());
			_grid[1, c] = new HeaderDay2(captionDate.ToString("dddd"));

			captionDate = captionDate.AddDays(1);
		}

		//create hour caption
		var hours = dateTimeStart.Hour;
		for (var r = _rowsHeader; r < _grid.RowsCount; r += partsForHour) {
			_grid[r, 0] = new HeaderHour1(hours);
			_grid[r, 0].RowSpan = partsForHour;

			var minutes = 0;
			for (var rs = r; rs < (r + partsForHour); rs++) {
				_grid[rs, 1] = new HeaderHour2(minutes);
				minutes += minAppointmentLength;
			}
			hours++;
		}

		_grid.FixedColumns = _columnsHeader;
		_grid.FixedRows = _rowsHeader;

		//Fix the width of the first 2 columns
		_grid.Columns[0].Width = 40;
		_grid.Columns[0].AutoSizeMode = SourceGrid.AutoSizeMode.None;
		_grid.Columns[1].Width = 40;
		_grid.Columns[1].AutoSizeMode = SourceGrid.AutoSizeMode.None;

		_grid.AutoStretchColumnsToFitWidth = true;
		_grid.AutoStretchRowsToFitHeight = true;
		_grid.AutoSizeCells();

		//Create Appointment Cells
		//Days
		for (var c = _columnsHeader; c < _grid.ColumnsCount; c++) {
			var currentTime = dateTimeStart.AddDays(c - _columnsHeader);
			var indexAppointment = -1;
			Cells.Cell appointmentCell = null;
			//Hours
			for (var r = _rowsHeader; r < _grid.RowsCount; r += partsForHour) {
				//Minutes
				for (var rs = r; rs < (r + partsForHour); rs++) {

					var found = false;
					//Appointments
					for (var index = 0; index < Appointments.Count; index++) {
						if (Appointments[index].ContainsDateTime(currentTime)) {
							found = true;

							if (indexAppointment != index) {
								appointmentCell = new CellAppointment(Appointments[index]);
								appointmentCell.View = Appointments[index].View;
								if (Appointments[index].Controller != null)
									appointmentCell.AddController(Appointments[index].Controller);
								_grid[rs, c] = appointmentCell;
								indexAppointment = index;
							} else {
								_grid[rs, c] = null;
								appointmentCell.RowSpan++;
							}

							break;
						}
					}
					if (found) {
					} else {
						_grid[rs, c] = new CellEmpty(currentTime, currentTime.AddMinutes(minAppointmentLength));
						indexAppointment = -1;
						appointmentCell = null;
					}

					currentTime = currentTime.AddMinutes(minAppointmentLength);
				}
			}
		}
	}

	public void UnLoadPlanning() {
		using var update = EnterUpdateScope(FinishedUpdateBehaviour.DoNothing);
		_grid.Redim(0, 0);
	}

	/// <summary>
	/// Clean up any resources being used.
	/// </summary>
	protected override void Dispose(bool disposing) {
		if (disposing) {
			if (_components != null)
				_components.Dispose();
		}
		base.Dispose(disposing);
	}

	protected override void OnLoad(EventArgs args) {
		base.OnLoad(args);

		_grid.Controller.AddController(new AppointmentController(this));
	}

	protected virtual void OnAppointmentClick(AppointmentEventArgs args) {
		if (AppointmentClick != null)
			AppointmentClick(this, args);
	}

	protected virtual void OnAppointmentDoubleClick(AppointmentEventArgs args) {
		if (AppointmentDoubleClick != null)
			AppointmentDoubleClick(this, args);
	}

	public delegate void AppointmentEventHandler(object sender, AppointmentEventArgs args);

	class AppointmentController : ControllerBase {
		private PlanningGrid _planningGrid;

		public AppointmentController(PlanningGrid planningGrid) {
			_planningGrid = planningGrid;
		}

		public override void OnClick(CellContext sender, EventArgs args) {
			base.OnClick(sender, args);

			if (sender.Cell is CellAppointment) {
				var cell = (CellAppointment)sender.Cell;
				_planningGrid.OnAppointmentClick(new AppointmentEventArgs(cell.Appointment.DateTimeStart, cell.Appointment.DateTimeEnd, cell.Appointment));
			} else if (sender.Cell is CellEmpty) {
				var cell = (CellEmpty)sender.Cell;
				_planningGrid.OnAppointmentClick(new AppointmentEventArgs(cell.Start, cell.End, null));
			}
		}

		public override void OnDoubleClick(CellContext sender, EventArgs args) {
			base.OnDoubleClick(sender, args);

			if (sender.Cell is CellAppointment) {
				var cell = (CellAppointment)sender.Cell;
				_planningGrid.OnAppointmentDoubleClick(new AppointmentEventArgs(cell.Appointment.DateTimeStart, cell.Appointment.DateTimeEnd, cell.Appointment));
			} else if (sender.Cell is CellEmpty) {
				var cell = (CellEmpty)sender.Cell;
				_planningGrid.OnAppointmentDoubleClick(new AppointmentEventArgs(cell.Start, cell.End, null));
			}
		}
	}
}
