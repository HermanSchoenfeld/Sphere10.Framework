// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows;

/// <summary>Detached PDH raw values. A nonvalid Status is unavailable data, not a zero measurement.</summary>
public sealed record WindowsPerformanceCounterSample {
	public string CounterPath { get; init; }
	public string InstanceName { get; init; }
	public uint CounterType { get; init; }
	public uint Status { get; init; }
	/// <summary>Query collection time converted from PDH local wall time; null when the instant is unavailable or ambiguous.</summary>
	public DateTime? TimestampUtc { get; init; }
	/// <summary>Unmodified provider FILETIME passed back to native PDH calculations.</summary>
	public long RawTimestampFileTime { get; init; }
	public long FirstValue { get; init; }
	public long SecondValue { get; init; }
	public uint MultiCount { get; init; }
	public long TimeBase { get; init; }
	public bool IsValid => Status is 0 or 1;
	internal object Owner { get; init; }
	internal IntPtr Counter { get; init; }
}

