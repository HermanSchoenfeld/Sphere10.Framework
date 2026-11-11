// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows;

/// <summary>ReadProcessMemory's actual result. ErrorCode is zero on success; partial bytes are retained on a failed read.</summary>
public sealed record WindowsProcessMemoryRead(ProcessIdentity Process, ulong Address, int RequestedLength, byte[] Bytes, int ErrorCode) {
	public bool IsPartial => Bytes.Length < RequestedLength;
}
