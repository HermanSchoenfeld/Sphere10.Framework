// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows;

/// <summary>Raw native fault record. Flags is reserved by Windows and is not interpreted as hard/soft classification.</summary>
public sealed record WindowsWorkingSetFault(ulong InstructionPointer, ulong VirtualAddress, ulong ThreadId, ulong Flags);
