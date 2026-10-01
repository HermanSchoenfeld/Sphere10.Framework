// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: David Price
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit http://www.opensource.org/licenses/mit-license.php.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Reflection;

namespace Sphere10.Framework.Web.AspNetCore.Blazor.UI.Controls.BlazorGrid;
/// <summary>Retained for legacy cell-based consumers. New grids use BlazorGridColumn and buffered editors.</summary>
public class Cell {
	public HeaderData Header { get; set; }
	public RowData Row { get; set; }
	public ObjectTypeInfo TypeInfo { get; set; }
	public string Text { get; set; }
	public object Tag { get; private set; } // this is the full row of data
	public int DataIndex { get; set; }

	public string Name {
		get { return Header.Name; }
	}

	public int Width {
		get { return Header.Width; }
	}

	public int Height {
		get { return Row.Height; }
	}

	public bool IsEnum { get; set; }

	public Cell(HeaderData header, RowData row, ObjectTypeInfo typeInfo, string text, object underlyingData, int dataIndex) {
		Header = header;
		Row = row;
		TypeInfo = typeInfo;
		Text = text;
		Tag = underlyingData;
		DataIndex = dataIndex;
		IsEnum = TypeInfo.IsEnum;
	}

	public string GetInputType() {
		switch (TypeInfo.Type.Name.ToLower()) {
			case "datetime": return "date";
			case "decimal":
			case "double":
			case "float":
			case "long":
			case "int32":
			case "int64": return "number";
			case "boolean": return "checkbox";
			default: return "text";
		}
	}

	public string GetListName() {
		if (TypeInfo.IsEnum) {
			return Name;
		}

		return string.Empty;
	}

	public static string GetCellText(object cellData, PropertyInfo property) {
		var value = property.GetValue(cellData);
		var typeName = property.PropertyType.Name.ToString();
		switch (typeName) {
			case "DateTime": return ((DateTime)value).ToString("yyyy-MM-dd");
			default: return value?.ToString() ?? string.Empty;
		}
	}
		public void UpdateData(string newValue) {
		var objectValue = Tools.Parser.Parse(TypeInfo.Type, newValue);
		TypeInfo.PropertyInfo.SetValue(Tag, objectValue);
	}
}
