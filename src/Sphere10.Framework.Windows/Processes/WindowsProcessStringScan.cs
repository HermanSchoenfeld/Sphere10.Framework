// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows;

public sealed record WindowsProcessStringScan {
	public WindowsProcessString[] Strings { get; init; } = Array.Empty<WindowsProcessString>();
	public long BytesExamined { get; init; }
	public int SkippedRegions { get; init; }
	public bool Truncated { get; init; }
}

