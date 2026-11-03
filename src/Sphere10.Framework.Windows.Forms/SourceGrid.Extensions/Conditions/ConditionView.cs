// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework.Windows.Forms.SourceGrid.Cells;
using Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Views;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Conditions;

public class ConditionView : ICondition {
	private IView _view;

	public EvaluateFunctionDelegate EvaluateFunction;

	public ConditionView(IView view) {
		_view = view;
	}

	public IView View {
		get {
			return _view;
		}
	}

	// ICondition Members
	public bool Evaluate(DataGridColumn column, int gridRow, object itemRow) {
		if (EvaluateFunction == null)
			return false;

		return EvaluateFunction(column, gridRow, itemRow);
	}

	public ICellVirtual ApplyCondition(ICellVirtual cell) {
		var copied = cell.Copy();
		copied.View = View;

		return copied;
	}

	public delegate bool EvaluateFunctionDelegate(DataGridColumn column, int gridRow, object itemRow);
}
