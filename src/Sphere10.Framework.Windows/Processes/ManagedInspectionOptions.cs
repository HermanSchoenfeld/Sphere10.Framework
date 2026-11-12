// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows;

/// <summary>Explicit upper bounds, validated by the adapter. Heap enumeration is opt-in and never a refresh side effect.</summary>
public sealed class ManagedInspectionOptions {
	public bool IncludeHeap { get; init; }

	public bool IncludeStacks { get; init; }

	public int MaximumObjects { get; init; } = 100000;

	public int MaximumReferences { get; init; } = 200000;

	public int MaximumRoots { get; init; } = 10000;

	public int MaximumEntries { get; init; } = 20000;

	public int MaximumFramesPerThread { get; init; } = 64;

	public string[] LocalLibraryDirectories { get; init; } = Array.Empty<string>();
}
