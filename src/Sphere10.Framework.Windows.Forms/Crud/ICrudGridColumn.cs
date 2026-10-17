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

public interface ICrudGridColumn {
	string ColumnName { get; set; }
	Type DataType { get; set; }
	CrudCellDisplayType DisplayType { get; }
	/// <summary>Alignment of a borrowed image within its cell; defaults to the center in both dimensions.</summary>
	Sphere10.Framework.ResizeAlignment ImageAlignment => Sphere10.Framework.ResizeAlignment.CenterCenter;

	/// <summary>Optional image scaling; null retains the original image dimensions.</summary>
	Sphere10.Framework.ResizeMethod? ImageResizeMethod => null;

	string SortName { get; }

	string? PropertyName => null;

	CrudReferenceBinding? ReferenceBinding => null;

	bool CellHasValue(object row);

	object GetCellValue(object row);

	void SetCellValue(object row, object @value);

	/// <summary>Whether this column shares spare grid width with other explicitly configured stretch columns.</summary>
	bool ExpandsToFit { get; }
	bool CanEditCell { get; }

	IEnumerable<object> GetDropDownItems(object row);

	string DropDownItemDisplayMember { get; }
	bool DropDownItemsIncludeNullItem { get; }
	string DropDownItemsNullDisplayString { get; }

	void ButtonPressed(object row);

	Size GetButtonSize(object row);

	string GetButtonCaption(object row);

	Image GetButtonImage(object row);

	string GetDateTimeFormat(object row);
}
