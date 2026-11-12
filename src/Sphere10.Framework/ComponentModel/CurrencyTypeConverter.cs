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
/// A TypeConverter that support string conversion from and to string with the currency symbol.
/// Support Conversion for Float, Double and Decimal, Int
/// </summary>
public class CurrencyTypeConverter : NumberTypeConverter {
	#region Constructors

	public CurrencyTypeConverter(Type baseType)
		: base(baseType) {
		Format = "C";
		NumberStyles = System.Globalization.NumberStyles.Currency;
	}

	public CurrencyTypeConverter(Type baseType, string format)
		: this(baseType) {
		Format = format;
	}

	#endregion
}
