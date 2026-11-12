// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

namespace Sphere10.Framework.Windows;

/// <summary>Per-provider owned-session configuration. Stack requests may require privilege/provider support and do not guarantee frames on every event.</summary>
public sealed record EtwTraceProviderOptions {
	public ulong Keywords { get; init; }
	public byte Level { get; init; } = 5;
	public uint TimeoutMilliseconds { get; init; } = 1000;
	public bool CaptureStackTrace { get; init; }
}
