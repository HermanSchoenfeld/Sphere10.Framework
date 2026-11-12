// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows;

/// <summary>One page fault drained from a process's working-set watch buffer.</summary>
public sealed record WorkingSetFaultRow(long Sequence, DateTime ObservedAtUtc, ulong InstructionPointer, ulong VirtualAddress, ulong ThreadId, ulong Flags);
