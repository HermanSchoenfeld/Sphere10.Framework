// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows;

/// <summary>Detached diagnostics; no ClrMD runtime, native handle or target process survives capture.</summary>
public sealed record ManagedRuntimeInfo(ProcessIdentity? Process, string Source, DateTime CapturedAtUtc, ManagedDiagnosticEntry[] Entries, ManagedHeapObject[] Objects, ManagedHeapType[] Types, ManagedHeapReference[] References, ManagedHeapRoot[] Roots, bool HeapIncluded, bool Truncated, string[] Issues) {
	public WindowsDumpInfo NativeDump { get; init; }
}
