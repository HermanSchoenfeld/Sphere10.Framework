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

public class VisualGridColumn<TEntity> : IVisualGridColumn {
	public VisualGridColumn() => DropDownItemsNullDisplayString = string.Empty;

	public string ColumnName { get; set; }

	/// <summary>Stable preference identity for projected columns without a property or sort name.</summary>
	public string PreferenceKey {
		get => field ?? PropertyName ?? SortName;
		set;
	}

	public VisualGridCellDisplayType DisplayType { get; set; }
	public Sphere10.Framework.ResizeAlignment ImageAlignment { get; set; } = Sphere10.Framework.ResizeAlignment.CenterCenter;
	public Sphere10.Framework.ResizeMethod? ImageResizeMethod { get; set; }
	public Type DataType { get; set; }
	public string SortName { get; set; }
	public string PropertyName { get; set; }
	public VisualGridReferenceBindingBase ReferenceBinding { get; set; }

	/// <summary>Uses spare grid width for this column while retaining its content width as the minimum.</summary>
	public bool ExpandsToFit { get; set; }

	/// <summary>Omits this column from the grid by default; an accepted column preference can show it.</summary>
	public bool Hidden { get; set; }

	/// <summary>The narrowest width, in logical pixels, natural sizing gives the column; 0 sizes it to its header and the shown rows alone.</summary>
	public int MinimumWidth { get; set; }

	/// <summary>The widest width, in logical pixels, natural sizing gives the column; 0 leaves it unbounded.</summary>
	public int MaximumWidth { get; set; }

	public bool CanEditCell { get; set; }
	public Func<TEntity, bool> PropertyHasValue { get; set; }
	public Func<TEntity, object> PropertyValue { get; set; }
	public Action<TEntity, object> SetPropertyValue { get; set; }
	public Func<TEntity, IEnumerable<object>> DropDownItems { get; set; }
	public string DropDownItemDisplayMember { get; set; }
	public bool DropDownItemsIncludeNullItem { get; set; }
	public string DropDownItemsNullDisplayString { get; set; }
	public Action<TEntity> ButtonAction { get; set; }
	public Func<TEntity, Size> ButtonSize { get; set; }
	public Func<TEntity, string> ButtonCaption { get; set; }
	public Func<TEntity, Image> ButtonImage { get; set; }
	public Func<TEntity, string> DateTimeFormat { get; set; }

	public bool CellHasValue(object row) => PropertyHasValue?.Invoke((TEntity)row) ?? true;

	public object GetCellValue(object row) => PropertyValue?.Invoke((TEntity)row);

	public void SetCellValue(object row, object @value) => SetPropertyValue((TEntity)row, @value);

	public IEnumerable<object> GetDropDownItems(object row) => DropDownItems((TEntity)row);

	public void ButtonPressed(object row) => ButtonAction((TEntity)row);

	public Size GetButtonSize(object row) => ButtonSize?.Invoke((TEntity)row) ?? Size.Empty;

	public string GetButtonCaption(object row) => ButtonCaption == null ? string.Empty : ButtonCaption((TEntity)row);

	public Image GetButtonImage(object row) => ButtonImage?.Invoke((TEntity)row);

	public string GetDateTimeFormat(object row)
		=> DateTimeFormat != null ? DateTimeFormat((TEntity)row) : DisplayType switch {
			VisualGridCellDisplayType.Date => "yyyy-MM-dd",
			VisualGridCellDisplayType.Time => "HH:mm:ss.fff",
			_ => "yyyy-MM-dd HH:mm:ss.fff"
		};
}
