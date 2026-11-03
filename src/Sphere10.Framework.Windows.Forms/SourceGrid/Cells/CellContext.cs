// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.Forms.SourceGrid;

/// <summary>
/// Structure that represents a logical cell, composed by a ICellVirtual, a Position and a GridVirtual.
/// This is an important structure used to manipulate the cell object, both virtual and real.
/// </summary>
public struct CellContext {
	/// <summary>
	/// An empty CellContext instance.
	/// </summary>
	public static readonly CellContext Empty;
	public Position Position;
	public Cells.ICellVirtual Cell;
	public GridVirtual Grid;

	static CellContext() {
		Empty = new CellContext(null, Position.Empty, null);
	}

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="gridVirtual"></param>
	/// <param name="position"></param>
	/// <param name="cell"></param>
	public CellContext(GridVirtual gridVirtual, Position position, Cells.ICellVirtual cell) {
		Position = position;
		Cell = cell;
		Grid = gridVirtual;
	}

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="gridVirtual"></param>
	/// <param name="position"></param>
	public CellContext(GridVirtual gridVirtual, Position position) {
		Position = position;
		Grid = gridVirtual;
		Cell = Grid.GetCell(Position);
	}

	/// <summary>
	/// Gets the string representation of the Model.ValueModel.GetValue method (default ToString())
	/// </summary>
	/// <returns></returns>
	public string DisplayText {
		get {
			if (Cell == null)
				return null;

			try {
				var val = Cell.Model.ValueModel.GetValue(this);
				if (Cell.Editor == null) {
					if (val == null)
						return string.Empty;
					return val.ToString();
				}
				return Cell.Editor.ValueToDisplayString(val);
			} catch (Exception exception) {
				return "Error:" + exception.Message;
			}
		}
	}

	/// <summary>
	/// Gets or sets the cell value.
	/// </summary>
	public object Value {
		get {
			return Cell.Model.ValueModel.GetValue(this);
		}
		set {
			Cell.Model.ValueModel.SetValue(this, value);
		}
	}

	/// <summary>
	/// Calculate the Range occupied by the current cell. Usually it is simply the Position property, only if RowSpan or ColumnSpan is used this property returns a larger range.
	/// Internally use the Grid.PositionToCellRange method.
	/// </summary>
	public CellRange CellRange {
		get {
			return Grid.PositionToCellRange(Position);
		}
	}

	// Methods
	/// <summary>
	/// If the cell is not linked to a grid the result is not accurate (Font can be null). Call InternalGetRequiredSize with RowSpan and ColSpan = 1.
	/// </summary>
	/// <param name="maxLayoutArea">SizeF structure that specifies the maximum layout area for the text. If width or height are zero the value is set to a default maximum value.</param>
	/// <returns></returns>
	public System.Drawing.Size Measure(System.Drawing.Size maxLayoutArea) {
		if (Cell == null)
			return System.Drawing.Size.Empty;
		if (Grid == null)
			throw new SourceGridException("Grid is null");

		var requiredSize = Cell.View.Measure(this, maxLayoutArea);
		var range = Grid.PositionToCellRange(Position);

		//Approximate the width and Height value if ColSpan or RowSpan are grater than 1
		// round to the greater value
		requiredSize.Width = (int)Math.Ceiling((float)requiredSize.Width / range.ColumnsCount);
		requiredSize.Height = (int)Math.Ceiling((float)requiredSize.Height / range.RowsCount);

		return requiredSize;
	}

	/// <summary>
	/// Start the edit operation with the current editor specified in the Model property.
	/// </summary>
	public void StartEdit() {
		if (Cell == null)
			throw new SourceGridException("No cell at position " + Position.ToString());
		if (Grid == null)
			throw new SourceGridException("Grid is null");

		// if no editor exist, then no editing is possible
		if (Cell.Editor == null)
			return;
		// editing can be disabled explicitly in editor
		if (Cell.Editor.EnableEdit == false)
			return;
		// if cell is already in editing mode, return
		if (IsEditing() == true)
			return;
		// set focus to true on this cell. If it is not possible to set
		// then edit is not possible as well
		if (Grid.Selection.Focus(Position, true) == false)
			return;
		var cancelEventArgs = new System.ComponentModel.CancelEventArgs();
		Grid.Controller.OnEditStarting(this, cancelEventArgs);
		if (cancelEventArgs.Cancel == false) {
			Cell.Editor.InternalStartEdit(this);
			Grid.Controller.OnEditStarted(this, EventArgs.Empty);
		}
	}

	/// <summary>
	/// Terminate the edit operation.
	/// </summary>
	/// <param name="cancel">If true undo all the changes</param>
	/// <returns>Returns true if the edit operation is successfully terminated, otherwise false</returns>
	public bool EndEdit(bool cancel) {
		if (Cell == null)
			return true;
		if (Grid == null)
			return true;

		if (Cell.Editor != null && Cell.Editor.IsEditing) {
			var editCellContext = Cell.Editor.EditCellContext;
			var success = Cell.Editor.InternalEndEdit(cancel);
			if (success)
				Grid.Controller.OnEditEnded(editCellContext, EventArgs.Empty);

			return success;
		}
		return true;
	}

	/// <summary>
	/// True is the cell can be drawn (usually if the cell is in editing state the drawing code is disabled)
	/// </summary>
	/// <returns></returns>
	public bool CanBeDrawn() {
		return Cell != null &&
				(Cell.Editor == null ||
				Cell.Editor.EnableCellDrawOnEdit ||
				IsEditing() == false);
	}

	/// <summary>
	/// True if this cell is currently in edit state, otherwise false.
	/// </summary>
	/// <returns></returns>
	public bool IsEditing() {
		if (Cell == null)
			return false;
		if (Grid == null)
			return false;

		if (Cell.Editor != null)
			return (Cell.Editor.IsEditing && Cell.Editor.EditCellContext == this);
		return false;
	}

	/// <summary>
	/// Invalidate this cell
	/// </summary>
	public void Invalidate() {
		if (Cell == null)
			return;
		if (Grid == null)
			return;

		Grid.InvalidateCell(Position);
	}

	// Standard methods for equality and comparison
	/// <summary>
	/// Returns true if the current struct is empty
	/// </summary>
	/// <returns></returns>
	public bool IsEmpty() {
		return this.Equals(Empty);
	}

	/// <summary>
	/// GetHashCode
	/// </summary>
	/// <returns></returns>
	public override int GetHashCode() {
		return Position.GetHashCode();
	}

	/// <summary>
	///
	/// </summary>
	/// <param name="other"></param>
	/// <returns></returns>
	public bool Equals(CellContext other) {
		return (Position == other.Position && Cell == other.Cell && Grid == other.Grid);
	}

	/// <summary>
	///
	/// </summary>
	/// <param name="obj"></param>
	/// <returns></returns>
	public override bool Equals(object obj) {
		return Equals((CellContext)obj);
	}

	/// <summary>
	///
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public static bool operator ==(CellContext left, CellContext right) {
		return left.Equals(right);
	}

	/// <summary>
	///
	/// </summary>
	/// <param name="left"></param>
	/// <param name="right"></param>
	/// <returns></returns>
	public static bool operator !=(CellContext left, CellContext right) {
		return !left.Equals(right);
	}

	/// <summary>
	///
	/// </summary>
	/// <returns></returns>
	public override string ToString() {
		return Position.ToString();
	}
}
