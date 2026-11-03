// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
// Original SourceGrid / DevAge code: Copyright (c) 2005-2012 Davide Icardi, Darius Damalakas.
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.Forms.SourceGrid.Cells.Editors;

/// <summary>
/// An editor to support Currency data type
/// </summary>
[System.ComponentModel.ToolboxItem(false)]
public class TextBoxCurrency : TextBoxNumeric {
	// Constructor
	/// <summary>
	/// Construct a Model. Based on the Type specified the constructor populate AllowNull, DefaultValue, TypeConverter, StandardValues, StandardValueExclusive
	/// </summary>
	/// <param name="type">The type of this model</param>
	public TextBoxCurrency(Type type)
		: base(type) {
		TypeConverter = new Sphere10.Framework.ComponentModel.CurrencyTypeConverter(type);
	}
}
