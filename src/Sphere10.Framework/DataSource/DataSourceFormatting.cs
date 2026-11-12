// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections;
using System.Globalization;
using System.Linq;

namespace Sphere10.Framework;

/// <summary>The value text an inventory searches and sorts by: "Unavailable" for null, local time for dates, joined sequences, invariant text otherwise.</summary>
public static class DataSourceFormatting {
	public static string Value(object value) {
		return value switch {
			null => "Unavailable",
			string text => text,
			DateTime time => time.ToLocalTime().ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture),
			IEnumerable sequence => string.Join("; ", sequence.Cast<object>().Select(Value)),
			_ => Convert.ToString(value, CultureInfo.InvariantCulture)
		};
	}
}
