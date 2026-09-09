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

public sealed class ThemeOptions {

	/// <summary>Optional directory containing named theme overrides. It need not exist.</summary>
	public string ThemesDirectory { get; init; }
}

/// <summary>Invalid or unavailable theme content; source files are never replaced silently.</summary>
public sealed class ThemeException : Exception {

	public ThemeException(string message)
		: base(message) { }

	public ThemeException(string message, Exception innerException)
		: base(message, innerException) { }
}
