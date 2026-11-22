// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Sphere10.Framework.Windows.Forms;
using Sphere10.Framework.Windows.Forms.AppointmentBook;

namespace Sphere10.Framework.Utils.WinFormsTester;

/// <summary>Explores resource scheduling, unallocated work and complete drag/resize events.</summary>
public partial class AppointmentBookScreen : ApplicationScreen {
	private static readonly string[] _subjects = new string[] { "Subject 1", "Subject 2", "Subject 3", "Subject 4", "Subject 5", "Subject 6", "Subject 7", "Subject 8", "Subject 9", "Subject 10" };
	private static readonly string[] _streets = new string[] { "Alpha", "Beta", "Gamma", "Delta", "Omega", "Phi", "Psi", "Andromeda", "Milky Way", "Ursur Minor" };
	private static readonly string[] _streetTypes = new string[] { "St", "Rd", "Way", "Pde", "Ct" };
	private static readonly string[] _states = new string[] { "ACT", "QLD", "NSW", "TAS", "VIC", "WA", "SA", "NT" };
	private static readonly string[] _cities = new string[] { "Vega", "Pleides", "Ceres", "Hyades", "Celestial Sphere", "Taurus", "Gemeni", "Mars", "Mercury", "Woodridge" };
	// Mid-tone category hues; the appointment book softens each into a pastel fill with matching text.
	private static readonly Color[] _appointmentColors = new[] { Color.SteelBlue, Color.SeaGreen, Color.Goldenrod, Color.IndianRed, Color.MediumPurple, Color.Coral, Color.Teal, Color.Orchid };
	private readonly TextBoxWriter _outputWriter;
	private MyAppointmentBookDataSource _dataSource;

	public AppointmentBookScreen() {
		InitializeComponent();
		Disposables.Add(_heading.Font);
		_timeViewSelector.EnumType = typeof(TimePeriodType);
		_columnFilterComboBox.EnumType = typeof(AppointmentBookViewModelFilter);
		_outputWriter = new TextBoxWriter(_outputTextBox);
	}

	/// <summary>Generates sample appointments only when the real example is displayed.</summary>
	protected override void OnLoad(EventArgs e) {
		base.OnLoad(e);
		if (!Tools.Runtime.IsDesignMode && _dataSource == null)
			GenerateData();
	}

	private void AppointmentBook_AppointmentBookFreeRegionSelected(AppointmentBookFreeRegionSelected arg)
		=> _outputWriter.WriteLine("{0}: free region selected - column '{1}' start time: '{2}' end time: '{3}'", arg.Source.Name, arg.Column.Name, arg.StartTime.ToShortTimeString(), arg.EndTime.ToShortTimeString());

	private void AppointmentBook_AppointmentDrag(AppointmentDraggedEvent arg)
		=> _outputWriter.WriteLine("{0}: appointment dragged - column '{1}' appointment '{2}'", arg.Source.Name, arg.SourceColumn.Name, arg.Appointment.Subject);

	private void AppointmentBook_AppointmentDragStarting(AppointmentDragStartingEvent arg) {
		_outputWriter.WriteLine("{0}: appointment drag starting - column '{1}' appointment '{2}'", arg.Source.Name, arg.SourceColumn.Name, arg.Appointment.Subject);
		// note you can cancel the drag here by settting
		// arg.Cancel = true;
		// arg.ErrorMessage = reason for cancel
	}

	private void AppointmentBook_AppointmentDrop(AppointmentDropEvent arg) {
		_outputWriter.WriteLine("{0}: appointment dropped - column '{1}' appointment '{2}' target column:'{3}' start time: '{4}' end time: '{5}'",
			arg.Source.Name,
			arg.SourceColumn.Name,
			arg.Appointment.Subject,
			arg.TargetColumn.Name,
			arg.StartTime.ToShortTimeString(),
			arg.EndTime.ToShortTimeString());
	}

	private void AppointmentBook_AppointmentDropStarting(AppointmentDropStartingEvent arg) {
		_outputWriter.WriteLine("{0}: appointment drop starting - column '{1}' appointment '{2}' target column:'{3}' start time: '{4}' end time: '{5}'",
			arg.Source.Name,
			arg.SourceColumn.Name,
			arg.Appointment.Subject,
			arg.TargetColumn.Name,
			arg.StartTime.ToShortTimeString(),
			arg.EndTime.ToShortTimeString());
		// note you can cancel the drag here by settting
		// arg.Cancel = true;
		// arg.ErrorMessage = reason for cancel
	}

	private void AppointmentBook_AppointmentResizing(AppointmentResizingEvent arg) {
		_outputWriter.WriteLine("{0}: appointment resizing - column '{1}' appointment '{2}' start time: '{3}' end time: '{4}'",
			arg.Source.Name,
			arg.SourceColumn.Name,
			arg.Appointment.Subject,
			arg.SelectedStartTime.ToShortTimeString(),
			arg.SelectedEndTime.ToShortTimeString());
	}

	private void AppointmentBook_AppointmentResizingFinished(AppointmentResizingFinishedEvent arg) {
		_outputWriter.WriteLine("{0}: appointment resized - column '{1}' appointment '{2}' start time: '{3}' end time: '{4}'",
			arg.Source.Name,
			arg.SourceColumn.Name,
			arg.Appointment.Subject,
			arg.SelectedStartTime.ToShortTimeString(),
			arg.SelectedEndTime.ToShortTimeString());
	}

	private void AppointmentBook_AppointmentResizingStarted(AppointmentEvent arg)
		=> _outputWriter.WriteLine("{0}: appointment resizing started - column '{1}' appointment '{2}'", arg.Source.Name, arg.SourceColumn.Name, arg.Appointment.Subject);

	private void AppointmentBook_AppointmentDoubleClicked(AppointmentEvent arg)
		=> _outputWriter.WriteLine("{0}: appointment double clicked - column '{1}' appointment '{2}'", arg.Source.Name, arg.SourceColumn.Name, arg.Appointment.Subject);

	private void AppointmentBook_AppointmentSelected(AppointmentEvent arg)
		=> _outputWriter.WriteLine("{0}: appointment selected - column '{1}' appointment '{2}'", arg.Source.Name, arg.SourceColumn.Name, arg.Appointment.Subject);

	private void AppointmentBook_AppointmentDeselected(AppointmentEvent arg)
		=> _outputWriter.WriteLine("{0}: appointment deselected - column '{1}' appointment '{2}'", arg.Source.Name, arg.SourceColumn.Name, arg.Appointment.Subject);

	private void AppointmentBook_AppointmentDragging(AppointmentDraggingEvent arg)
		=> _outputWriter.WriteLine("{0}: appointment dragging - appointment '{1}' column '{2}' start: '{3}' finish: '{4}' ", arg.Source.Name, arg.Appointment.Subject, arg.DestinationColumn.Name, arg.DestinationStartTime, arg.DestinationEndTime);

	private void UnallocatedBook_AppointmentDrag(AppointmentDraggedEvent arg)
		=> _outputWriter.WriteLine("{0}: appointment dragged - column '{1}' appointment '{2}'", arg.Source.Name, arg.SourceColumn.Name, arg.Appointment.Subject);

	private void UnallocatedBook_AppointmentDragStarting(AppointmentDragStartingEvent arg) {
		_outputWriter.WriteLine("{0}: appointment drag starting - column '{1}' appointment '{2}'", arg.Source.Name, arg.SourceColumn.Name, arg.Appointment.Subject);
		// note you can cancel the drag here by settting
		// arg.Cancel = true;
		// arg.ErrorMessage = reason for cancel
	}

	private void UnallocatedBook_AppointmentDrop(AppointmentDropEvent arg) {
		_outputWriter.WriteLine("{0}: appointment dropped - column '{1}' appointment '{2}' target column:'{3}' start time: '{4}' end time: '{5}'",
			arg.Source.Name,
			arg.SourceColumn.Name,
			arg.Appointment.Subject,
			arg.TargetColumn.Name,
			arg.StartTime.ToShortTimeString(),
			arg.EndTime.ToShortTimeString());
	}

	private void UnallocatedBook_AppointmentDropStarting(AppointmentDropStartingEvent arg) {
		_outputWriter.WriteLine("{0}: appointment drop starting - column '{1}' appointment '{2}' target column:'{3}' start time: '{4}' end time: '{5}'",
			arg.Source.Name,
			arg.SourceColumn.Name,
			arg.Appointment.Subject,
			arg.TargetColumn.Name,
			arg.StartTime.ToShortTimeString(),
			arg.EndTime.ToShortTimeString());
		// note you can cancel the drag here by settting
		// arg.Cancel = true;
		// arg.ErrorMessage = reason for cancel
	}

	private void UnallocatedBook_AppointmentDoubleClicked(AppointmentEvent arg)
		=> _outputWriter.WriteLine("{0}: appointment double clicked - column '{1}' appointment '{2}'", arg.Source.Name, arg.SourceColumn.Name, arg.Appointment.Subject);

	private void UnallocatedBook_AppointmentSelected(AppointmentEvent arg)
		=> _outputWriter.WriteLine("{0}: appointment selected - column '{1}' appointment '{2}'", arg.Source.Name, arg.SourceColumn.Name, arg.Appointment.Subject);

	private void UnallocatedBook_AppointmentDeselected(AppointmentEvent arg)
		=> _outputWriter.WriteLine("{0}: appointment deselected - column '{1}' appointment '{2}'", arg.Source.Name, arg.SourceColumn.Name, arg.Appointment.Subject);

	private void UnallocatedBook_AppointmentDragging(AppointmentDraggingEvent arg)
		=> _outputWriter.WriteLine("{0}: appointment dragging - appointment '{1}' column '{2}' start: '{3}' finish: '{4}' ", arg.Source.Name, arg.Appointment.Subject, arg.DestinationColumn.Name, arg.DestinationStartTime, arg.DestinationEndTime);

	#region Form Event Handlers

	private void PopulateButton_Click(object sender, EventArgs args) => GenerateData();

	#endregion

	#region Data Generation

	private void GenerateData() {
		const int numResources = 10;
		const int maxAppointmentsPerDay = 4;

		var year = DateTime.Now.Year;
		var month = DateTime.Now.Month;
		var startDay = 1;
		var endDay = DateTime.Now.ToEndOfMonth().Day;

		_dataSource = new MyAppointmentBookDataSource();

		// Create unallocated appointments
		for (var day = startDay; day <= endDay; day++) {
			var numAppointments = Tools.Maths.RNG.Next(maxAppointmentsPerDay - 1) + 1;
			var appointmentBoundaries = GenerateRandomBoundaries(maxAppointmentsPerDay, 0, 24).ToArray();
			for (var appointmentNo = 0; appointmentNo < numAppointments; appointmentNo++)
				_dataSource.NewAppointment(
					new UnallocatedColumn(),
					Guid.NewGuid().ToStrictAlphaString(),
					new DateTime(year, month, day, appointmentBoundaries[appointmentNo].Item1, 0, 0),
					new DateTime(year, month, day, appointmentBoundaries[appointmentNo].Item2, 0, 0),
					RandomSubject(),
					RandomLocation1(),
					RandomLocation2(),
					RandomNotes(),
					RandomAppointmentColor(),
					null
				);
		}

		// Create appointments
		for (var resource = 0; resource < numResources; resource++) {
			var column = _dataSource.NewColumn(string.Format("Resource {0}", resource + 1), null);
			for (var day = startDay; day <= endDay; day++) {
				var numAppointments = Tools.Maths.RNG.Next(maxAppointmentsPerDay + 1);
				var appointmentBoundaries = GenerateRandomBoundaries(maxAppointmentsPerDay, 0, 24).ToArray();
				for (var appointmentNo = 0; appointmentNo < numAppointments; appointmentNo++)
					_dataSource.NewAppointment(
						column,
						Guid.NewGuid().ToStrictAlphaString(),
						new DateTime(year, month, day, appointmentBoundaries[appointmentNo].Item1, 0, 0),
						new DateTime(year, month, day, appointmentBoundaries[appointmentNo].Item2, 0, 0),
						RandomSubject(),
						RandomLocation1(),
						RandomLocation2(),
						RandomNotes(),
						RandomAppointmentColor(),
						null
					);
			}
		}
		_appointmentBook.DataSource = _dataSource;
		_unallocatedBook.DataSource = _dataSource;
	}

	private IEnumerable<Tuple<int, int>> GenerateRandomBoundaries(int numberToGenerate, int startValueInclusive, int endValueExclusive) {
		if (endValueExclusive - startValueInclusive - 1 < numberToGenerate / 2)
			throw new SoftwareException("Insufficient number of options to generate random boundaries. Generate less or increase boundary");

		var values = new HashSet<int>();

		for (var index = 0; index < numberToGenerate * 2; index++) {
			var generatedValue = 0;
			do {
				generatedValue = Tools.Maths.RNG.Next(startValueInclusive, endValueExclusive);
			} while (values.Contains(generatedValue));
			values.Add(generatedValue);
		}

		var sortedValues = values.ToList();
		sortedValues.Sort();

		for (var i = 0; i < sortedValues.Count; i += 2) {
			yield return Tuple.Create(sortedValues[i], sortedValues[i + 1]);
		}
	}

	private Color RandomAppointmentColor() => _appointmentColors[Tools.Maths.RNG.Next(_appointmentColors.Length)];

	private string RandomSubject() => _subjects.Randomize().First();

	private string RandomLocation1() => string.Format("{0} {1} {2}", RandomStreetNumber(), RandomStreetName(), RandomStreetType());

	private string RandomStreetName() => _streets.Randomize().First();

	private string RandomStreetNumber() => Tools.Maths.RNG.Next(1, 100).ToString();

	private string RandomStreetType() => _streetTypes.Randomize().First();

	private string RandomLocation2() => string.Format("{0}, {1} {2}", RandomCity(), RandomState(), RandomPostCode());

	private string RandomCity() => _cities.Randomize().First();

	private string RandomState() => _states.Randomize().First();

	private string RandomPostCode() => Tools.Maths.RNG.Next(1001, 10000).ToString();

	private string RandomNotes() {
		if (Tools.Maths.Gamble(0.2))
			return Tools.Text.GenerateRandomString(Tools.Maths.RNG.Next(5, 100));

		return string.Empty;
	}

	#endregion

	private void TimeViewSelector_SelectedIndexChanged(object sender, EventArgs args) {
		_appointmentBook.SetTimePeriodType((TimePeriodType)_timeViewSelector.SelectedEnum);
		_unallocatedBook.SetTimePeriodType((TimePeriodType)_timeViewSelector.SelectedEnum);
	}

	private void ColumnFilterComboBox_SelectedIndexChanged(object sender, EventArgs args)
		=> _appointmentBook.ColumnFilter = (AppointmentBookViewModelFilter)_columnFilterComboBox.SelectedEnum;
}
