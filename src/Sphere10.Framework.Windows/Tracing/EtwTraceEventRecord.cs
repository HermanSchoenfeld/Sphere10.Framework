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
	uint ProcessId, uint ThreadId, byte[] Data, EtwTraceProperty[] Properties) {
	public byte Opcode { get; init; }
	public ushort Task { get; init; }
	public ulong Keywords { get; init; }
	public Guid ActivityId { get; init; }
	public ushort HeaderFlags { get; init; }
	public EtwTraceExtendedData[] ExtendedData { get; init; } = Array.Empty<EtwTraceExtendedData>();
	public int ExtendedDataItemsOmitted { get; init; }
	/// <summary>Optional producing-session identity supplied by an owning controller; a detached header alone does not establish session ownership.</summary>
	public Guid TraceSessionGuid { get; init; }
}

/// <summary>Detached extended-header evidence. Stack addresses belong to the event writer context, not an inferred IO actor.</summary>
public sealed record EtwTraceExtendedData(ushort Type, byte[] Data, ulong? MatchId, ulong[] StackAddresses, string Availability);
