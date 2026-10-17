// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows;

/// <summary>A detached ETW event. Header process identifiers describe the event writer, not ownership of its payload.</summary>
public sealed record EtwTraceEventRecord(Guid ProviderId, ushort EventId, byte Version, DateTime TimestampUtc,
	uint ProcessId, uint ThreadId, byte[] Data, EtwTraceProperty[] Properties);
