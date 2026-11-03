// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections;
using System.Windows.Forms;

namespace Sphere10.Framework.Windows.Forms.SourceGrid;

/// <summary>
/// Cell Lost Focus event arguments with the old position and the new position. Extends PositionCancelEventArgs.
/// </summary>
public class ChangeActivePositionEventArgs : System.ComponentModel.CancelEventArgs {
	private Position _oldFocusPosition;
	private Position _newFocusPosition;

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="oldFocusPosition"></param>
	/// <param name="newFocusPosition">If Empty there isn't a cell that will receive the focus.</param>
	public ChangeActivePositionEventArgs(Position oldFocusPosition, Position newFocusPosition)
		: base(false) {
		_oldFocusPosition = oldFocusPosition;
		_newFocusPosition = newFocusPosition;
	}

	/// <summary>
	/// Position that had the focus
	/// </summary>
	public Position OldFocusPosition => _oldFocusPosition;

	/// <summary>
	/// Position that will receive the focus. If Empty there isn't a cell that will receive the focus.
	/// </summary>
	public Position NewFocusPosition => _newFocusPosition;
}
