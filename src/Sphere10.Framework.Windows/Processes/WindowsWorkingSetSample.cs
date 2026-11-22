// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows;

/// <summary>One destructive native drain. Times are observation times, not per-fault timestamps; all faults may be soft or hard.</summary>
public sealed record WindowsWorkingSetSample(ProcessIdentity Process, DateTime ObservedAtUtc, WindowsWorkingSetFault[] Faults, ulong LostRecords, string Availability);
