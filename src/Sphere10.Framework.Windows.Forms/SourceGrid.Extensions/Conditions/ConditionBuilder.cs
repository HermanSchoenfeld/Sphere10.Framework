// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Views;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Conditions;

public static class ConditionBuilder {
	public static ICondition AlternateView(IView view, System.Drawing.Color alternateBackcolor, System.Drawing.Color alternateForecolor) {
		// Keep the caller's view unchanged while preparing its alternating presentation.
		var viewAlternate = (SourceGrid.Cells.Views.IView)view.Clone();
		viewAlternate.BackColor = alternateBackcolor;
		viewAlternate.ForeColor = alternateForecolor;

		// Apply the cloned view only to odd grid rows.
		var condition = new SourceGrid.Conditions.ConditionView(viewAlternate);
		condition.EvaluateFunction = delegate (SourceGrid.DataGridColumn column, int gridRow, object itemRow) {
			return (gridRow & 1) == 1;
		};

		return condition;
	}
}
