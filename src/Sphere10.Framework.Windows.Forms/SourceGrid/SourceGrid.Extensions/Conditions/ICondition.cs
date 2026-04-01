// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Dev Age
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using Sphere10.Framework.Windows.Forms.SourceGrid.Cells;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Conditions;

public interface ICondition {
	bool Evaluate(DataGridColumn column, int gridRow, object itemRow);

	ICellVirtual ApplyCondition(ICellVirtual cell);
}

