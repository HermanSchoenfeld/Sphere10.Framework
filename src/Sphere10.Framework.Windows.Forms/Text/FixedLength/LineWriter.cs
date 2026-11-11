// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.Forms.Text.FixedLength;

/// <summary>
/// A class used to create fixed lenght string.
/// </summary>
public class LineWriter {
	private FieldList _fields;
	private IField[] _sortedList;
	private char _separator = '\0';
	private object[] _lineValues;

	public LineWriter(FieldList fields) => _fields = fields;

	public LineWriter(Type lineClassType) => _fields = Tools.FixedLength.ExtractFieldListFromType(lineClassType);

	public FieldList Fields => _fields;

	public char Separator {
		get => _separator;
		set => _separator = value;
	}

	public string CreateLineFromClass(object schemaClass) {
		// Capture only the schema properties that declare a fixed-length field.
		var properties = schemaClass.GetType().GetProperties(System.Reflection.BindingFlags.GetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

		foreach (var property in properties) {
			var attributes = property.GetCustomAttributes(typeof(FieldAttribute), true);
			//FieldAttribute fieldAttr = (FieldAttribute)attributes[0];
			if (attributes.Length > 0)
				SetValue(property.Name, property.GetValue(schemaClass, null));
		}

		return CreateLine();
	}

	public string CreateLine() {
		if (_lineValues == null)
			throw new ArgumentNullException("_lineValues", "SetValue not called");
		if (_sortedList == null)
			_sortedList = _fields.GetSortedList();

		// Write each value in field order with the optional separator.
		var builder = new System.Text.StringBuilder();
		for (var index = 0; index < _sortedList.Length; index++) {
			builder.Append(_sortedList[index].ValueToString(_lineValues[index]));

			if (Separator != '\0')
				builder.Append(Separator);
		}

		return builder.ToString();
	}

	public void SetValue(string fieldName, object val) {
		// Allocate the value slots and field ordering on first use.
		if (_lineValues == null)
			_lineValues = new object[_fields.Count];
		if (_sortedList == null)
			_sortedList = _fields.GetSortedList();

		_lineValues[_fields[fieldName].Index] = val;
	}

	public void Reset() => _lineValues = null;
}
