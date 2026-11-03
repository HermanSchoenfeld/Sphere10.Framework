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
/// A class for parsing fixed length string and loading the fields into a class.
/// </summary>
public class LineParser {
	private FieldList _fields;
	private System.Text.RegularExpressions.Regex _regEx;
	private char _separator = '\0';
	private System.Text.RegularExpressions.Match _regExMatch;

	/// <summary>
	/// Constructor. Fill the Fields list fot specify the columns.
	/// </summary>
	public LineParser() {
		_fields = new FieldList();
	}

	/// <summary>
	/// Load the parser fields with the properties specified in the type. You must use the FieldAttribute and ParseFormatAttribute to specify additional informations like the field length.
	/// </summary>
	/// <param name="lineClassType"></param>
	public LineParser(Type lineClassType) {
		_fields = Tools.FixedLength.ExtractFieldListFromType(lineClassType);
	}

	/// <summary>
	/// Gets a collection of fields.
	/// </summary>
	public FieldList Fields {
		get {
			return _fields;
		}
	}

	public char Separator {
		get {
			return _separator;
		}
		set {
			_separator = value;
		}
	}

	/// <summary>
	/// Load the specified line in the parser.
	/// </summary>
	/// <param name="line"></param>
	public void LoadLine(string line) {
		// Reuse the compiled field expression until the parser is reset.
		if (_regEx == null)
			_regEx = CreateLineRegExp();
		_regExMatch = _regEx.Match(line);
	}

	/// <summary>
	/// Fill the properties of the specified class with the values of the line has defined by the Fields collection.
	/// </summary>
	/// <param name="schemaClass"></param>
	/// <returns>Returns the same class specified in the schemaClass parameter, this is useful if you have struct or value types.</returns>
	public object FillLineClass(object schemaClass) {
		if (_regExMatch == null)
			throw new ArgumentNullException("_regExMatch", "LoadLine not called");

		// Populate only the schema properties that declare a fixed-length field.
		var properties = schemaClass.GetType().GetProperties(System.Reflection.BindingFlags.GetProperty | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public);

		foreach (var property in properties) {
			var attributes = property.GetCustomAttributes(typeof(FieldAttribute), true);
			//FieldAttribute fieldAttr = (FieldAttribute)attributes[0];
			if (attributes.Length > 0)
				property.SetValue(schemaClass, GetValue(property.Name), null);
		}

		return schemaClass;
	}

	/// <summary>
	/// Get a specified field value.
	/// </summary>
	/// <param name="fieldName"></param>
	/// <returns></returns>
	public object GetValue(string fieldName) {
		if (_regExMatch == null)
			throw new ArgumentNullException("_regExMatch");

		// Validate the captured group before converting it with the field validator.
		var group = _regExMatch.Groups[fieldName];
		if (group.Success == false)
			throw new RegExException(fieldName);

		return _fields[fieldName].StringToValue(group.Value);
	}

	/// <summary>
	/// Reset the parser
	/// </summary>
	public void Reset() {
		_regEx = null;
		_regExMatch = null;
	}

	private System.Text.RegularExpressions.Regex CreateLineRegExp() {
		//esempio: @"^(?<colA>.{6})(?<colB>.{8})(?<colC>.+)$"
		// * Specifies zero or more matches; for example, \w* or (abc)*. Equivalent to {0,}.
		// + Specifies one or more matches; for example, \w+ or (abc)+. Equivalent to {1,}.

		// Assemble capture groups in field order, retaining the configured separator.
		var fields = _fields.GetSortedList();
		var pattern = "^";
		for (var index = 0; index < fields.Length; index++) {
			pattern += fields[index].RegularExpressionPattern;
			pattern += Tools.FixedLength.ValidateRegExpSeparator(Separator);
		}

		//pattern += "$"; //commettato per non cercare necessariamente la fine riga, che quindi necessitava di specificare tutti i campi precisi.

		return new System.Text.RegularExpressions.Regex(pattern, System.Text.RegularExpressions.RegexOptions.Compiled | System.Text.RegularExpressions.RegexOptions.Singleline);
	}
}
