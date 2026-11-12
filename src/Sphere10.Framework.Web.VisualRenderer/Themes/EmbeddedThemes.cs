// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld <herman@sphere10.com>
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.IO.Compression;

namespace Sphere10.Framework.Web.VisualRenderer;

/// <summary>Provides the built-in theme archive without extracting it to disk.</summary>
public static class EmbeddedThemes {

	/// <summary>Opens an independent read-only archive. The caller owns and disposes it.</summary>
	public static ZipArchive OpenArchive() {
		var stream = typeof(EmbeddedThemes).Assembly.GetManifestResourceStream("Sphere10.Framework.Web.VisualRenderer.Themes.zip")
			?? throw new InvalidOperationException("The embedded theme archive is missing.");
		try {
			return new ZipArchive(stream, ZipArchiveMode.Read);
		} catch {
			stream.Dispose();
			throw;
		}
	}
}
