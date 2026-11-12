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
/// RangeData class represents a range of data. Can represent a range of data in string format. Usually used for drag and drop and clipboard copy/paste operations.
/// See Controllers\Clipboard, Controllers\SelectionDrag and Controllers\SelectionDrop.
/// </summary>
[Serializable]
public class RangeData {
	[Obsolete] private Position _startDragPosition;
	private CellRange _sourceRange;
	private object[,] _sourceValues;
	[NonSerialized] private GridVirtual _sourceGrid;
	[Obsolete] private CutMode _cutMode = CutMode.None;
	[NonSerialized] private System.Windows.Forms.DataObject _clipboardDataObject = null;

	/// <summary>
	/// The string constant used with the System.Windows.Forms.DataFormats.GetFormat to register the clipboard format RangeData object.
	/// </summary>
	public const string RangeDataFormat = "SourceGrid.RangeData";

	/// <summary>
	/// Static constructor
	/// </summary>
	static RangeData() {
		//Register custom DataFormats
		System.Windows.Forms.DataFormats.GetFormat(RangeDataFormat);
	}

	/// <summary>
	/// Constructor
	/// </summary>
	public RangeData() {
	}

	public RangeData(GridVirtual sourceGrid)
		: this() {
		_sourceGrid = sourceGrid;
	}

	/// <summary>
	/// Range source
	/// </summary>
	public CellRange SourceRange => _sourceRange;

	/// <summary>
	/// String array for values.
	/// </summary>
	public object[,] SourceValues => _sourceValues;

	/// <summary>
	/// Starting drag position. Used only for calculating drop destination range.
	/// </summary>
	[Obsolete]
	public Position StartDragPosition => _startDragPosition;

	/// <summary>
	/// Working grid.
	/// </summary>
	public GridVirtual SourceGrid => _sourceGrid;

	/// <summary>
	/// Cut mode. Default is none.
	/// </summary>
	[Obsolete]
	public CutMode CutMode => _cutMode;

	#region Write data

	/// <summary>
	/// Write the current loaded array string in the specified grid range. This method use the cell editor to set the value.
	/// </summary>
	public void WriteData(GridVirtual sourceGrid, Position destinationPosition) {
		var sourceRow = this.SourceValues.GetUpperBound(0);
		var sourceColumn = this.SourceValues.GetUpperBound(1);
		var dataRow = 0;
		for (var r = destinationPosition.Row; r <= destinationPosition.Row + sourceRow; r++) {
			var dataColumn = 0;
			for (var c = destinationPosition.Column; c <= destinationPosition.Column + sourceColumn; c++) {
				var posCell = new Position(r, c);
				var cell = sourceGrid.GetCell(posCell);
				var cellContext = new CellContext(sourceGrid, posCell, cell);

				if (cell != null && cell.Editor != null && _sourceValues[dataRow, dataColumn] != null)
					cell.Editor.SetCellValue(cellContext, _sourceValues[dataRow, dataColumn]);
				dataColumn++;
			}
			dataRow++;
		}
		/*int sourceRow = this.SourceValues.Length - 1;
		int sourceColumn = this.SourceValues.Rank - 1;
		//Calculate the destination Range merging the source range
		var destinationRange = new CellRange(destinationPosition,
		                                 new Position(destinationPosition.Row + sourceRow, destinationPosition.Column + sourceColumn));
		Range newRange = mSourceRange;
		newRange.MoveTo(destinationRange.Start);
		if (newRange.End.Column > destinationRange.End.Column)
			newRange.End = new Position(newRange.End.Row, destinationRange.End.Column);
		if (newRange.End.Row > destinationRange.End.Row)
			newRange.End.Row = destinationRange.End.Row;

		for (int r = newRange.Start.Row; r <= newRange.End.Row; r++)
		{
			int dataColumn = 0;
			for (int c = newRange.Start.Column; c <= newRange.End.Column ; c++)
			{
				//if (sourceGrid.Columns.IsColumnVisible(c) == false)
				//	continue;
				Position posCell = new Position(r, c);
				Cells.ICellVirtual cell = sourceGrid.GetCell(posCell);
				CellContext cellContext = new CellContext(sourceGrid, posCell, cell);

				if (cell != null && cell.Editor != null && mSourceValues[r - newRange.Start.Row, dataColumn] != null)
					cell.Editor.SetCellValue(cellContext, mSourceValues[r - newRange.Start.Row, dataColumn] );
				dataColumn++;
			}
		}*/
	}

	#endregion

	/// <summary>
	/// Calculate the destination range for the drop or paste operations.
	/// </summary>
	/// <param name="destinationGrid"></param>
	/// <param name="dropDestination"></param>
	/// <returns></returns>
	[Obsolete("Completely not used. Will be removed in future versions")]
	public CellRange FindDestinationRange(GridVirtual destinationGrid, Position dropDestination) {
		if (dropDestination.IsEmpty())
			return CellRange.Empty;

		var destinationStart = new Position(dropDestination.Row + (_sourceRange.Start.Row - _startDragPosition.Row),
			dropDestination.Column + (_sourceRange.Start.Column - _startDragPosition.Column));

		destinationStart = Position.Max(destinationStart, new Position(0, 0));

		var destination = _sourceRange;
		destination.MoveTo(destinationStart);

		destination = destination.Intersect(destinationGrid.CompleteRange);

		return destination;
	}

	/// <summary>
	/// Copy the specified RangeData object the the clipboard
	/// </summary>
	/// <param name="rangeData"></param>
	public static void ClipboardSetData(RangeData rangeData) {
		if (rangeData._clipboardDataObject == null)
			throw new SourceGridException("No data loaded, use the LoadData method");
		System.Windows.Forms.Clipboard.SetDataObject(rangeData._clipboardDataObject);
	}

	/// <summary>
	/// Get a RangeData object from the clipboard. Return null if the clipboard doesn't contains valid data formats.
	/// </summary>
	/// <returns></returns>
	public static RangeData ClipboardGetData() {
		var dtObj = System.Windows.Forms.Clipboard.GetDataObject();
		RangeData rngData = null;
		if (dtObj.GetDataPresent(RangeDataFormat))
			rngData = (RangeData)dtObj.GetData(RangeDataFormat);

		// if RANGEDATA_FORMAT or GetData returns null use string buffer as rngData
		if (rngData == null) {
			// get unicode text instead of text
			if (dtObj.GetDataPresent(System.Windows.Forms.DataFormats.UnicodeText, true)) {
				var buffer = (string)dtObj.GetData(System.Windows.Forms.DataFormats.UnicodeText, true);
				rngData = new RangeData();
				rngData.LoadData(buffer);
			}
		}

		return rngData;
	}

	#region Loading data

	[Obsolete("Use LoadData method without startDragPosition")]
	public void LoadData(GridVirtual sourceGrid, CellRange sourceRange, Position startDragPosition, CutMode cutMode)
		=> LoadData(sourceGrid, sourceRange, Position.Empty, cutMode);

	/// <summary>
	/// Load the specified range data into a string array. This method use the cell editor to get the value.
	/// </summary>
	/// <param name="sourceGrid"></param>
	/// <param name="sourceRange"></param>
	/// <param name="cutMode">Cut mode. Can be used to remove the data from the source when pasting it to the destination or immediately.</param>
	public static RangeData LoadData(GridVirtual sourceGrid, CellRange sourceRange, CutMode cutMode) {
		var data = new RangeData(sourceGrid);
		//mCutMode = cutMode;
		data._sourceRange = sourceRange;
		data._sourceValues = new object[sourceRange.RowsCount, GetVisibleColumnCount(sourceGrid, sourceRange)];

		var arrayRow = 0;
		for (var r = sourceRange.Start.Row; r <= sourceRange.End.Row; r++, arrayRow++) {
			var arrayCol = 0;
			for (var c = sourceRange.Start.Column; c <= sourceRange.End.Column; c++) {
				if (sourceGrid.Columns.IsColumnVisible(c) == false)
					continue;
				var posCell = new Position(r, c);
				var cell = sourceGrid.GetCell(posCell);
				var cellContext = new CellContext(sourceGrid, posCell, cell);
				/*if (cell != null && cell.Editor != null && cell.Editor.IsStringConversionSupported())
					data.mSourceValues[arrayRow, arrayCol] = cell.Editor.ValueToString( cell.Model.ValueModel.GetValue(cellContext) );
				else if (cell != null)
					data.mSourceValues[arrayRow, arrayCol] = cellContext.DisplayText;*/
				if (cell != null)
					data._sourceValues[arrayRow, arrayCol] = cellContext.Value;
				arrayCol++;
			}
		}

		//Cut Data
		if (cutMode == CutMode.CutImmediately && sourceGrid != null)
			sourceGrid.ClearValues(new RangeRegion(sourceRange));

		data._clipboardDataObject = new System.Windows.Forms.DataObject();
		data._clipboardDataObject.SetData(RangeDataFormat, data);
		var values = DataToStringArray(sourceGrid, data._sourceRange);
		data._clipboardDataObject.SetData(typeof(string), StringArrayToString(values));
		return data;
	}

	/// <summary>
	/// Load the data from a Tab delimited string of data. Each column is separated by a Tab and each row by a LineFeed character.
	/// </summary>
	public void LoadData(string data) {
		_sourceGrid = null;
		StringToData(data, out _sourceRange, out _sourceValues);

		_clipboardDataObject = new System.Windows.Forms.DataObject();
		_clipboardDataObject.SetData(RangeDataFormat, this);
		_clipboardDataObject.SetData(typeof(string), StringArrayToString(_sourceValues as string[,]));
	}

	#endregion

	/// <summary>
	/// Convert a string buffer into a Range object and an array of string.
	/// </summary>
	/// <param name="data"></param>
	/// <param name="range"></param>
	/// <param name="values"></param>
	protected virtual void StringToData(string data, out CellRange range, out object[,] values) {
		//tolgo uno dei due caratteri di a capo per usare lo split
		data = data.Replace("\x0D\x0A", "\x0A");
		var rowsData = data.Split('\x0A', '\x0D');

		//Check if the last row is not null (some application put a last \n character at the end of the cells, for example excel)
		var rows = rowsData.Length;
		if (rows > 0 &&
			(rowsData[rows - 1] == null ||
				rowsData[rows - 1].Length == 0))
			rows--;

		if (rows == 0) {
			range = CellRange.Empty;
			values = new string[0, 0];
			return;
		}

		//Calculate the columns based on the first rows! Note: probably is better to calculate the maximum columns.
		var firstColumnsData = rowsData[0].Split('\t');
		var cols = firstColumnsData.Length;

		range = new CellRange(0, 0, rows - 1, cols - 1);
		values = new string[rows, cols];

		var arrayRow = 0;
		for (var r = range.Start.Row; r < range.Start.Row + rows; r++, arrayRow++) {
			var rowData = rowsData[arrayRow];
			var columnsData = rowData.Split('\t');
			var arrayCol = 0;
			for (var c = range.Start.Column; c <= range.End.Column; c++, arrayCol++) {
				if (arrayCol < columnsData.Length)
					values[arrayRow, arrayCol] = columnsData[arrayCol];
				else
					values[arrayRow, arrayCol] = "";
			}
		}
	}

	/// <summary>
	/// Convert an array of strings into a string.
	/// Normally using a tab delimited for columns and a LineFeed for rows.
	/// </summary>
	/// <returns></returns>
	protected static string StringArrayToString(string[,] values) {
		var builder = new System.Text.StringBuilder();

		var nr = values.GetLength(0);
		var nc = values.GetLength(1);
		for (var r = 0; r < nr; ++r) {
			for (var c = 0; c < nc; ++c) {
				builder.Append(values[r, c]);
				if (c != nr - 1)
					builder.Append('\t');
			}

			if (r != nr - 1)
				builder.Append("\x0D\x0A");
		}

		return builder.ToString();
	}

	/// <summary>
	/// Convert a range and an array of string into a string. Normally using a tab delimited for columns and a LineFeed for rows.
	/// </summary>
	/// <returns></returns>
	protected static string[,] DataToStringArray(GridVirtual sourceGrid, CellRange range) {
		var numberOfRows = range.End.Row - range.Start.Row + 1;
		var numberOfCols = range.End.Column - range.Start.Column + 1;
		var values = new string[numberOfRows, numberOfCols];

		var arrayRow = 0;
		for (var r = range.Start.Row; r <= range.End.Row; r++, arrayRow++) {
			var arrayCol = 0;
			for (var c = range.Start.Column; c <= range.End.Column; c++, arrayCol++) {
				var val = String.Empty;

				var posCell = new Position(r, c);
				var cell = sourceGrid.GetCell(posCell);
				var cellContext = new CellContext(sourceGrid, posCell, cell);

				if (cell != null && cell.Editor != null && cell.Editor.IsStringConversionSupported())
					values[arrayRow, arrayCol] = cell.Editor.ValueToString(cell.Model.ValueModel.GetValue(cellContext));
				else if (cell != null)
					values[arrayRow, arrayCol] = cellContext.DisplayText;
			}
		}

		return values;
	}

	private static int GetVisibleColumnCount(GridVirtual sourceGrid, CellRange sourceRange) {
		var visibleCount = 0;
		for (var c = sourceRange.Start.Column; c <= sourceRange.End.Column; c++) {
			if (sourceGrid.Columns.IsColumnVisible(c))
				visibleCount++;
		}
		return visibleCount;
	}
}
