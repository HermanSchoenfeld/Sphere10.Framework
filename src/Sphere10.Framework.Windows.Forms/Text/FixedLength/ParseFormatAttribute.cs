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
/// Attribute used to specify additional parse options
/// </summary>
[System.AttributeUsage(AttributeTargets.Property)]
public class ParseFormatAttribute : System.Attribute {

	/// <summary>
	/// Constructor. Use one of these properties to customize the format: CultureInfo, DateTimeFormat, NumberFormat, TrimBeforeParse.
	/// Default is Invariant culture format.
	/// </summary>
	public ParseFormatAttribute() {
		var invariant = System.Globalization.CultureInfo.InvariantCulture;

		DateTimeFormat = invariant.DateTimeFormat.ShortDatePattern;
		NumberFormat = "+00000000.0000;-00000000.0000";
		TrimBeforeParse = true;
		CultureInfo = invariant;
	}

	public System.Globalization.CultureInfo CultureInfo { get; set; }

	public string DateTimeFormat { get; set; }

	public string NumberFormat { get; set; }

	public bool TrimBeforeParse { get; set; }
}
