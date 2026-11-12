// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows;

/// <summary>A detached VirtualQueryEx region; raw native flags are preserved rather than interpreted as product captions.</summary>
public sealed record WindowsProcessMemoryRegion(ulong Address, ulong AllocationBase, ulong Size, uint State, uint Protection, uint AllocationProtection, uint Type, bool IsReadable);
