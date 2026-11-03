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

public class AppointmentBase : IAppointment {
	private DateTime _dateTimeStart;

	private DateTime _dateTimeEnd;

	private string _title;

	private Cells.Views.IView _view;

	private Cells.Controllers.IController _controller;

	public AppointmentBase()
		: this("", DateTime.Now, DateTime.Now) {
	}

	public AppointmentBase(string title, DateTime dateTimeStart, DateTime dateTimeEnd) {
		_title = title;
		_dateTimeEnd = dateTimeEnd;
		_dateTimeStart = dateTimeStart;

		_view = new Cells.Views.Cell();
		_view.Border = Sphere10.Framework.Drawing.RectangleBorder.RectangleBlack1Width;
	}

	public DateTime DateTimeStart {
		get {
			return _dateTimeStart;
		}
		set {
			_dateTimeStart = value;
		}
	}

	public DateTime DateTimeEnd {
		get {
			return _dateTimeEnd;
		}
		set {
			_dateTimeEnd = value;
		}
	}

	public virtual string Title {
		get {
			return _title;
		}
		set {
			_title = value;
		}
	}

	[Browsable(false)]
	public virtual Cells.Views.IView View {
		get {
			return _view;
		}
		set {
			_view = value;
		}
	}

	[Browsable(false)]
	public Cells.Controllers.IController Controller {
		get {
			return _controller;
		}
		set {
			_controller = value;
		}
	}

	public bool ContainsDateTime(DateTime dateTime) {
		return (_dateTimeStart <= dateTime && _dateTimeEnd > dateTime);
	}
}
