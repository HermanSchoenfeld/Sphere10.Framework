// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework.Windows.Forms.SourceGrid.Cells;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Conditions;

public class ConditionCell : ICondition {
	private ICellVirtual _cell;

	public EvaluateFunctionDelegate EvaluateFunction;

	public ConditionCell(ICellVirtual cell) => _cell = cell;

	public ICellVirtual Cell => _cell;

	#region ICondition Members

	public bool Evaluate(DataGridColumn column, int gridRow, object itemRow) {
		if (EvaluateFunction == null)
			return false;

		return EvaluateFunction(column, gridRow, itemRow);
	}

	public ICellVirtual ApplyCondition(ICellVirtual cell) => Cell;

	#endregion

	public delegate bool EvaluateFunctionDelegate(DataGridColumn column, int gridRow, object itemRow);
}
