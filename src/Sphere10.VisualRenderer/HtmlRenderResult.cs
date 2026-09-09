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

/// <summary>HTML and its immutable, independently publishable theme resource snapshot.</summary>
public sealed record HtmlRenderResult(string Html, IReadOnlyList<RenderAsset> Assets) {
	/// <summary>True when a theme used by the document requests that callers preserve its HTML without formatting.</summary>
	public bool SuppressFormatting { get; init; }
}
