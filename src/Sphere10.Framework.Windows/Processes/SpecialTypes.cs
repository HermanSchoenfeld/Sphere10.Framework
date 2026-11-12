// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows;

public sealed record WindowsThreadWaitChainNode(string ObjectType, string Status, string Name, uint? ProcessId, uint? ThreadId, uint? WaitTime, uint? ContextSwitches);

public sealed record WindowsCpuSet(uint Id, ushort Group, byte LogicalProcessorIndex, byte CoreIndex, byte EfficiencyClass, bool Parked, bool Allocated, bool AllocatedToTargetProcess);

internal readonly record struct WindowsMitigationPolicyDescription(int InformationClass, string Name);

public sealed record WindowsProcessSuspension(ProcessIdentity Identity, System.DateTime CapturedAtUtc, ThreadIdentity[] Threads);

public sealed record WindowsProcessSuspensionRelease(ProcessIdentity Identity, int ReleasedThreads, ThreadIdentity[] RemainingThreads, string[] Errors);

public sealed record ManagedDiagnosticEntry(string Runtime, string Kind, string Name, ulong? Address, string Value);

public sealed record ManagedHeapObject(string Runtime, ulong Address, ulong Size, string TypeName, ulong MethodTable);

public sealed record ManagedHeapType(string Runtime, string TypeName, long Count, ulong Bytes);

public sealed record ManagedHeapReference(string Runtime, ulong Source, ulong Target);

public sealed record ManagedHeapRoot(string Runtime, string Kind, ulong Address, ulong ObjectAddress, bool IsPinned, bool IsInterior);

/// <summary>A backed virtual-memory range retained in a Windows minidump, with its validated file location.</summary>
public sealed record WindowsDumpMemoryRange(ulong Address, ulong Size, long FileOffset);

/// <summary>Offline dump revision and retained memory mappings; no live process identity or native resource is retained.</summary>
public sealed record WindowsDumpInfo(string FilePath, long FileLength, DateTime LastWriteTimeUtc, WindowsDumpMemoryRange[] MemoryRanges);

/// <summary>Raw native fault record. Flags is reserved by Windows and is not interpreted as hard/soft classification.</summary>
public sealed record WindowsWorkingSetFault(ulong InstructionPointer, ulong VirtualAddress, ulong ThreadId, ulong Flags);

/// <summary>Historical image evidence supplied by a capture, not a fresh live-process inventory.</summary>
public sealed record WindowsSymbolModule(ulong BaseAddress, uint ImageSize, string ImagePath, uint? TimeDateStamp, uint? Checksum);

/// <summary>One destructive native drain. Times are observation times, not per-fault timestamps; all faults may be soft or hard.</summary>
public sealed record WindowsWorkingSetSample(ProcessIdentity Process, DateTime ObservedAtUtc, WindowsWorkingSetFault[] Faults, ulong LostRecords, string Availability);

/// <summary>Detached address resolution, with explicit missing/mismatched image issues. PE identity matching is not a cryptographic authenticity check.</summary>
public sealed record WindowsResolvedAddresses(DateTime ResolvedAtUtc, WindowsStackFrame[] Frames, string[] Issues);

/// <summary>A detached, bounded user-mode stack captured from one verified native thread instance.</summary>
public sealed record WindowsThreadStack(ThreadIdentity Identity, DateTime CapturedAtUtc, WindowsStackFrame[] Frames, bool Truncated, string[] Issues);
