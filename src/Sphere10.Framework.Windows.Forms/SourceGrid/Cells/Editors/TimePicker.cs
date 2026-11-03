// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Editors;

/// <summary>
/// Create an Editor that use a DateTimePicker as control for time editing.
/// </summary>
[System.ComponentModel.ToolboxItem(false)]
public class TimePicker : DateTimePicker {
	/// <summary>
	/// Constructor
	/// </summary>
	public TimePicker()
		: this("T", new string[] { "T" }) {
	}

	/// <summary>
	/// Constructor
	/// </summary>
	public TimePicker(System.String toStringFormat, string[] parseFormats) {
		var timeConverter = new Sphere10.Framework.ComponentModel.DateTimeTypeConverter(toStringFormat, parseFormats);
		TypeConverter = timeConverter;
	}

	/// <summary>
	/// Gets the control used for editing the cell.
	/// </summary>
	public new System.Windows.Forms.DateTimePicker Control {
		get {
			return (System.Windows.Forms.DateTimePicker)base.Control;
		}
	}

	// Edit Control
	/// <summary>
	/// Create the editor control
	/// </summary>
	/// <returns></returns>
	protected override Control CreateControl() {
		var dtPicker = new System.Windows.Forms.DateTimePicker();
		dtPicker.Format = DateTimePickerFormat.Time;
		dtPicker.ShowUpDown = true;
		return dtPicker;
	}
}
