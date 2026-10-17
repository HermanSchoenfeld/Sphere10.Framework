// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.Files;

public sealed record FileHandleInfo {
	public ProcessIdentity Identity { get; init; }
	public int ProcessId => Identity.ProcessId;
	public string ProcessName { get; init; } = string.Empty;
	public ulong? HandleValue { get; init; }
	public ulong ObjectAddress { get; init; }
	public ushort ObjectTypeIndex { get; init; }
	public string Path { get; init; } = string.Empty;
	public uint GrantedAccess { get; init; }
	public string Availability { get; init; } = string.Empty;
}
