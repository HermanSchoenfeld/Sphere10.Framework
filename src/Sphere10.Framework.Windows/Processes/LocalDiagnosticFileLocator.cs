// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.IO;
using System.Reflection.PortableExecutable;
using System.Runtime.InteropServices;
using Microsoft.Diagnostics.Runtime;

namespace Sphere10.Framework.Windows;

/// <summary>Prevents ClrMD's default public-symbol-server lookup. DAC signature/version checks remain enabled.</summary>
internal sealed class LocalDiagnosticFileLocator(string[] directories) : IFileLocator {
	public string FindPEImage(string fileName, int buildTimeStamp, int imageSize, bool checkProperties) {
		Debug.AssertNotMainThread();
		if (IsLocalPath(fileName) && Matches(fileName, buildTimeStamp, imageSize, checkProperties))
			return fileName;
		foreach (var directory in directories) {
			var candidate = Path.Combine(directory, Path.GetFileName(fileName));
			if (Matches(candidate, buildTimeStamp, imageSize, checkProperties))
				return candidate;
		}

		return null;
	}

	public string FindPEImage(string fileName, SymbolProperties archivedUnder, ImmutableArray<byte> buildIdOrUuid, OSPlatform originalPlatform, bool checkProperties) {
		// This overload is indexed by an ELF/Mach-O identity, not a Windows PE tuple.
		// Preserve the adapter's existing local-only lookup; it does not add cross-platform build-ID validation.
		return FindPEImage(fileName, 0, 0, false);
	}

	private static bool Matches(string path, int timestamp, int imageSize, bool checkProperties) {
		if (!File.Exists(path))
			return false;
		if (!checkProperties)
			return true;
		var matches = false;
		Tools.Exceptions.ExecuteCapturingException(() => {
			using var stream = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read);
			using var reader = new PEReader(stream);
			var headers = reader.PEHeaders;
			matches = headers.CoffHeader.TimeDateStamp == timestamp && headers.PEHeader?.SizeOfImage == imageSize;
		}, out _, error => error is IOException or UnauthorizedAccessException or BadImageFormatException or ArgumentException);
		return matches;
	}

	internal static bool IsLocalPath(string path)
		=> !string.IsNullOrWhiteSpace(path) && Path.IsPathFullyQualified(path) && !path.StartsWith(@"\\", StringComparison.Ordinal);

	internal static DataTargetOptions CreateOptions(string[] directories = null) {
		directories ??= Array.Empty<string>();
		foreach (var directory in directories)
			Guard.Argument(IsLocalPath(directory), nameof(directories), "Diagnostic library paths must be explicit local directories.");
		return new DataTargetOptions {
			SymbolPaths = Array.Empty<string>(),
			FileLocator = new LocalDiagnosticFileLocator(directories),
			VerifyDacOnWindows = true,
			Limits = new DataTargetLimits {
				MaxThreads = 20000,
				MaxStackFrames = 512,
				MaxModules = 20000,
				MaxAppDomains = 1000,
				MaxMinidumpStreams = 4096,
				MaxMinidumpMemoryRanges = 1000000,
				MaxPESections = 1024,
				MaxPERelocations = 1000000,
				MaxPEExportNames = 100000,
				MaxPEDebugDirectories = 1024
			}
		};
	}
}
