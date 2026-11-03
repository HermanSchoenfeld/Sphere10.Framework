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
/// Attribute used to convert a specific value to another value
/// </summary>
[System.AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class ValueMappingAttribute : System.Attribute {
	private string _stringValue;
	private object _fieldValue;

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="stringValue">String value</param>
	/// <param name="fieldValue">Field typed value</param>
	public ValueMappingAttribute(string stringValue, object fieldValue) {
		StringValue = stringValue;
		FieldValue = fieldValue;
	}

	public string StringValue {
		get {
			return _stringValue;
		}
		set {
			_stringValue = value;
		}
	}

	public object FieldValue {
		get {
			return _fieldValue;
		}
		set {
			_fieldValue = value;
		}
	}
}
