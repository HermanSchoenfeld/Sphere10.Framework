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
/// Required attribute to specify the field position and length
/// </summary>
[System.AttributeUsage(AttributeTargets.Property)]
public class FieldAttribute : System.Attribute {
	private int _fieldIndex;
	private int _length;

	/// <summary>
	/// Constructor.
	/// </summary>
	/// <param name="fieldIndex">Index of the field, 0 based. Each field must have a unique progressive index</param>
	/// <param name="length">Lenght of the field when readed and writed to the string.</param>
	public FieldAttribute(int fieldIndex, int length) {
		_fieldIndex = fieldIndex;
		_length = length;
	}

	public int FieldIndex {
		get {
			return _fieldIndex;
		}
	}

	public int Length {
		get {
			return _length;
		}
	}
}
