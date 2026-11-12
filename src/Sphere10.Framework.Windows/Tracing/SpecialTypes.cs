// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows;

public sealed record EtwStackEvent(Guid ProviderId, byte Type);

/// <summary>SampledProfile's instruction pointer and manifest thread ID; the ETW header PID is deliberately not promoted to the sampled actor.</summary>
public sealed record EtwCpuProfileSample(ulong? InstructionPointer, uint? ThreadId, string Availability);

/// <summary>Classic StackWalk payload, with its original timestamp retained unchanged for same-clock association.</summary>
public sealed record EtwCpuProfileStack(ulong? OriginalEventTimestamp, uint? ProcessId, uint? ThreadId, ulong[] StackAddresses, string Availability);
