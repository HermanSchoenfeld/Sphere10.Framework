// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Models;

/// <summary>
/// A Model interface specific to contain the value of the cell.
/// </summary>
public interface IValueModel : IModel {
	// GetValue, SetValue
	/// <summary>
	/// Get the value of the cell at the specified position
	/// </summary>
	/// <param name="cellContext"></param>
	/// <returns></returns>
	object GetValue(CellContext cellContext);

	/// <summary>
	/// Set the value of the cell at the specified position. This method must call OnValueChanging and OnValueChanged() event.
	/// </summary>
	/// <param name="cellContext"></param>
	/// <param name="value"></param>
	void SetValue(CellContext cellContext, object value);
}
