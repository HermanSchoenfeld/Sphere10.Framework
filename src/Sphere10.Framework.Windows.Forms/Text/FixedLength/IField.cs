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
/// Interface for defining a Field in the FixedLength string
/// </summary>
public interface IField {
	string RegularExpressionPattern {
		get;
	}

	/// <summary>
	/// Index of the field. 0 based.
	/// </summary>
	int Index {
		get;
	}

	/// <summary>
	/// Name of the field, used for retriving the field by its name.
	/// </summary>
	string Name {
		get;
	}

	/// <summary>
	/// Convert the specified value to a string value valid for this field
	/// </summary>
	/// <param name="val"></param>
	/// <returns></returns>
	string ValueToString(object val);

	/// <summary>
	/// Convert the specified string value to a value based on the field format.
	/// </summary>
	/// <param name="str"></param>
	/// <returns></returns>
	object StringToValue(string str);
}
