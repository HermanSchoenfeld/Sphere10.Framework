// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows;

/// <summary>Detached QueryWorkingSetEx page flags. Invalid pages do not expose valid-only bit fields.</summary>
public sealed record WindowsWorkingSetPage(ProcessIdentity Process, DateTime CapturedAtUtc, ulong Address, ulong Flags) {
	public bool IsResident => (Flags & 1) != 0;

	public int? ShareCount => IsResident ? (int)((Flags >> 1) & 7) : null;

	public uint? Protection => IsResident ? (uint)((Flags >> 4) & 0x7ff) : null;

	public bool IsShared => (Flags & (1UL << 15)) != 0;

	public int? NumaNode => IsResident ? (int)((Flags >> 16) & 63) : null;

	public bool? IsLocked => IsResident ? (Flags & (1UL << 22)) != 0 : null;

	public bool? IsLargePage => IsResident ? (Flags & (1UL << 23)) != 0 : null;

	public bool IsBad => (Flags & (1UL << 31)) != 0;
}
