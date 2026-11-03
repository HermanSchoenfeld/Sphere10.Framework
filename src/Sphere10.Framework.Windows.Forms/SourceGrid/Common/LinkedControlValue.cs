// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System.Collections.Generic;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms.SourceGrid;

/// <summary>
/// Linked control value
/// </summary>
public class LinkedControlValue {
	private Control _control;
	private Position _position;
	private bool _useCellBorder;
	private LinkedControlScrollMode _scrollMode;

	/// <summary>
	/// Constructor
	/// </summary>
	public LinkedControlValue(Control control, Position position) {
		_control = control;
		_position = position;
		_useCellBorder = true;
		_scrollMode = LinkedControlScrollMode.BasedOnPosition;
	}

	public Control Control {
		get {
			return _control;
		}
	}

	/// <summary>
	/// Gets or sets the position of the linked control.
	/// </summary>
	public Position Position {
		get {
			return _position;
		}
		set {
			_position = value;
		}
	}

	/// <summary>
	/// Gets or sets if show the cell border. True to insert the editor control inside the border of the cell, false to put the editor control over the entire cell. If you use true remember to set EnableCellDrawOnEdit == true.
	/// </summary>
	public bool UseCellBorder {
		get {
			return _useCellBorder;
		}
		set {
			_useCellBorder = value;
		}
	}

	/// <summary>
	/// Gets or sets the scrolling mode of the control.
	/// </summary>
	public LinkedControlScrollMode ScrollMode {
		get {
			return _scrollMode;
		}
		set {
			_scrollMode = value;
		}
	}

	/// <summary>
	///
	/// </summary>
	/// <returns></returns>
	public override string ToString() {
		return Position.ToString();
	}
}
