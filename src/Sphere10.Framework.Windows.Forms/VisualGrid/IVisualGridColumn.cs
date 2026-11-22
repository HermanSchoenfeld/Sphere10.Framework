// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.Drawing;

namespace Sphere10.Framework.Windows.Forms;

public interface IVisualGridColumn {
	string ColumnName { get; set; }
	Type DataType { get; set; }
	VisualGridCellDisplayType DisplayType { get; }

	/// <summary>Alignment of a borrowed image within its cell; defaults to the center in both dimensions.</summary>
	Sphere10.Framework.ResizeAlignment ImageAlignment => Sphere10.Framework.ResizeAlignment.CenterCenter;

	/// <summary>Optional image scaling; null retains the original image dimensions.</summary>
	Sphere10.Framework.ResizeMethod? ImageResizeMethod => null;

	/// <summary>Stable preference identity; property and sort names provide the normal fallback.</summary>
	string PreferenceKey => PropertyName ?? SortName;

	string SortName { get; }

	string PropertyName => null;

	VisualGridReferenceBindingBase ReferenceBinding => null;

	/// <summary>Whether this column shares spare grid width with other explicitly configured stretch columns.</summary>
	bool ExpandsToFit { get; }

	/// <summary>Whether the grid omits this column until an accepted column preference shows it.</summary>
	bool Hidden => false;

	/// <summary>The narrowest width, in logical pixels, natural sizing gives the column; 0 sizes it to its header and the shown rows alone.
	/// A tree whose top-level rows leave a column empty uses it so the nested rows' values fit. A width the user drags is not limited.</summary>
	int MinimumWidth => 0;

	/// <summary>The widest width, in logical pixels, natural sizing gives the column; 0 leaves it unbounded. A column of a few very long values,
	/// such as joined hardware IDs, otherwise pushes the others out of view; longer values are cut, and stretching still widens the column to
	/// fill the grid. A width the user drags is not limited.</summary>
	int MaximumWidth => 0;

	bool CanEditCell { get; }

	string DropDownItemDisplayMember { get; }
	bool DropDownItemsIncludeNullItem { get; }
	string DropDownItemsNullDisplayString { get; }

	bool CellHasValue(object row);

	object GetCellValue(object row);

	void SetCellValue(object row, object @value);

	IEnumerable<object> GetDropDownItems(object row);

	void ButtonPressed(object row);

	Size GetButtonSize(object row);

	string GetButtonCaption(object row);

	Image GetButtonImage(object row);

	string GetDateTimeFormat(object row);
}
