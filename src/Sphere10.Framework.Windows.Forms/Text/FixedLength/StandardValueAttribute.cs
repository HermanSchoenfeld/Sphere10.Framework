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
/// Attribute used to specify the standard value (mandatory value) for a specific field.
/// You can use this attribute for example when you want a particular field to only accept one or more standard values.
/// </summary>
[System.AttributeUsage(AttributeTargets.Property, AllowMultiple = true)]
public class StandardValueAttribute : System.Attribute {

	/// <summary>
	/// Construcotr
	/// </summary>
	/// <param name="standardValue">Required value</param>
	public StandardValueAttribute(object standardValue) {
		StandardValue = standardValue;
	}

	public object StandardValue { get; set; }
}
