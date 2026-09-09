// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld <herman@sphere10.com>
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Sphere10.VisualRenderer;

internal static class VisualFormatting {

	public static string Color(Color color) {
		var name = color.ToString();
		return name.EndsWith("Background", StringComparison.Ordinal)
			? name[..^10].ToLowerInvariant() + "-background"
			: name.ToLowerInvariant();
	}

	public static string Date(Date date) {
		if (date is null)
			return "";
		var formatValue = date.IncludeTime ? "yyyy-MM-dd HH:mm zzz" : "yyyy-MM-dd";
		string Format(DateTimeOffset? value) => value?.ToString(formatValue, System.Globalization.CultureInfo.InvariantCulture) ?? "Empty";
		return date.End is null ? Format(date.Start) : Format(date.Start) + " - " + Format(date.End);
	}
}
