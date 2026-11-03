// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.ComponentModel;

/// <summary>
/// Conversion exception
/// </summary>
[Serializable]
public class ValueConversionException : ApplicationException {
	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="destinationType"></param>
	/// <param name="value"></param>
	public ValueConversionException(string destinationType, string value)
		: base("Cannot convert " + value + " to " + destinationType + ".") {
	}

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="destinationType"></param>
	/// <param name="value"></param>
	/// <param name="extendedMessage"></param>
	public ValueConversionException(string destinationType, string value, string extendedMessage)
		: base(extendedMessage + ", cannot convert " + value + " to " + destinationType + ".") {
	}

	/// <summary>
	/// Constructor
	/// </summary>
	/// <param name="destinationType"></param>
	/// <param name="value"></param>
	/// <param name="innerException"></param>
	public ValueConversionException(string destinationType, string value, Exception innerException)
		: base("Cannot convert " + value + " to " + destinationType + ".", innerException) {
	}
}
