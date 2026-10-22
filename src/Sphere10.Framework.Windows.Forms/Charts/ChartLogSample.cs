// Copyright (c) Herman Schoenfeld 2018 - Present. All rights reserved. (https://sphere10.com)
// Author: Herman Schoenfeld
//
// Distributed under the MIT software license, see the accompanying file
// LICENSE or visit https://opensource.org/license/mit.
//
// This notice must not be removed when duplicating this file or its contents, in whole or in part.

using System;

namespace Sphere10.Framework.Windows.Forms;

/// <summary>A captured chart observation with a stable identity independent of the current viewport.</summary>
public sealed record ChartLogSample(string Series, string Identity, string Position, string[] Values) {
	/// <summary>The observation's original UTC capture time; absent values use the current UTC publication time.</summary>
	public DateTime? CapturedAtUtc { get; init; }

	public string Units { get; init; }

	public string PositionLabel { get; init; }
}
